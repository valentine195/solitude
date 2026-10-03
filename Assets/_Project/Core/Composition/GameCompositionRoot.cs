using System;
using System.Collections.Generic;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Composition
{
    public enum RuntimeStartupState { Unready, Ready, Failed }
    [DefaultExecutionOrder(-2000)]
    public sealed class GameCompositionRoot : MonoBehaviour, IContainerScreenRequests
    {
        private static GameCompositionRoot active;
        [SerializeField] private GameRuntimeConfig config;
        [SerializeField] private SaveGameService persistence;
        [SerializeField] private UnityInputHost input;
        [SerializeField] private TimeSystem clock;
        [SerializeField] private GameManager game;
        private ContainerSaveSession session;
        private PickupCollectionService pickups;
        private ItemCatalog catalog;
        private ItemPresentationCatalog presentations;
        private Dictionary<LootTable, RuntimeLootTable> loot;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private ContainerGestureCoordinator gestures;
        private SceneBindings interactive;
        private readonly Dictionary<Scene, SceneBindings> scopes = new();
        private IDisposable failedPause;
        private bool initialized;
        public RuntimeStartupState State { get; private set; }
        private void Awake()
        {
            if (active != null && active != this)
            { enabled = false; Destroy(gameObject); return; } // Only dedicated bootstrap objects carry this component.
            active = this; DontDestroyOnLoad(gameObject);
            try
            {
                if (config == null || config.database == null || persistence == null || input == null || clock == null || game == null)
                    throw new InvalidOperationException("Bootstrap authoring/adapter references are incomplete.");
                catalog = config.database.BuildRuntimeCatalog(); presentations = config.database.BuildPresentationCatalog();
                loot = new Dictionary<LootTable, RuntimeLootTable>();
                foreach (var table in config.lootTables)
                { if (table == null || loot.ContainsKey(table)) throw new InvalidOperationException("Null or duplicate configured loot table."); loot.Add(table, table.Compile(catalog)); }
                pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause);
                clock.Initialize(pause); game.Initialize(pause); input.Initialize(policy);
                var data = persistence.Load(config.worldSeed, catalog, out var error);
                if (data == null) throw new InvalidOperationException(error);
                CreateSession(data);
                SceneManager.sceneLoaded += Loaded; SceneManager.sceneUnloaded += Unloaded; initialized = true;
                for (int i = 0; i < SceneManager.sceneCount; i++) BindScene(SceneManager.GetSceneAt(i));
                // The initial scene can still be loading during bootstrap Awake.
                // sceneLoaded binds it before input is allowed.
                if (interactive != null) { FinishStartup(); }
            }
            catch (Exception failure) { Fail(failure); }
        }
        private void FinishStartup()
        {
            persistence.CompleteRecovery();
            State = RuntimeStartupState.Ready; policy.SetReady(true);
            if (persistence.RecoveryNoticePending) interactive.ShowRecoveryNotice(persistence.AcknowledgeRecoveryNotice);
        }
        private void CreateSession(SaveGameData data)
        {
            session = new ContainerSaveSession(data, catalog); session.Commands.ObserverError += Debug.LogException;
            pickups = new PickupCollectionService(session);
            gestures = new ContainerGestureCoordinator(new ContainerInteractionController(session.Commands), policy);
            persistence.Initialize(session, StartNewSave);
        }
        private void Fail(Exception error)
        {
            if(error is System.IO.InvalidDataException) persistence?.RejectRestore(error.Message);
            State = RuntimeStartupState.Failed; policy?.SetReady(false);
            if (pause != null && failedPause == null) failedPause = pause.Acquire();
            Debug.LogError("[GameCompositionRoot] Runtime unavailable; save preserved: " + error.Message);
            // Recovery remains explicit even when save validation prevented session construction.
            persistence?.Initialize(session, StartNewSave, false);
        }
        private void Loaded(Scene scene, LoadSceneMode mode)
        {
            try { BindScene(scene);
                if (mode == LoadSceneMode.Single && interactive == null) throw new InvalidOperationException("No composed player scene is available.");
                if (interactive != null && failedPause == null) { FinishStartup(); policy.SetTransition(false); } }
            catch (Exception error) { Fail(error); }
        }
        private void BindScene(Scene scene)
        {
            if (!scene.isLoaded || scopes.ContainsKey(scene) || session == null) return;
            SceneBindings entry = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var entries = root.GetComponentsInChildren<SceneBindings>(true);
                foreach (var candidate in entries)
                { if (entry != null) throw new InvalidOperationException("Duplicate SceneBindings in " + scene.name); entry = candidate; }
            }
            if (entry == null) return;
            if (entry.HasPlayer && interactive != null) throw new InvalidOperationException("Only one local player scene may be composed.");
            entry.Initialize(session, pickups, presentations, loot, gestures, policy, pause, this, input, game, () => ReleaseScene(scene));
            scopes.Add(scene, entry);
            if (entry.HasPlayer) interactive = entry;
        }
        private void Unloaded(Scene scene) => ReleaseScene(scene);
        private void ReleaseScene(Scene scene)
        {
            if (!scopes.TryGetValue(scene, out var scope)) return;
            scopes.Remove(scene);
            if (ReferenceEquals(interactive, scope)) { policy.SetReady(false); input.Bind(null, null, null, null, null, null); interactive = null; }
            scope.Release();
        }
        public bool Open(LockerContainer source) => State == RuntimeStartupState.Ready && policy.Ready && interactive != null && interactive.Open(source);
        public void Close(LockerContainer source) => interactive?.Close(source);
        public AsyncOperation TransitionTo(string scenePath)
        {
            if (State != RuntimeStartupState.Ready || !UnityEngine.Application.CanStreamedLevelBeLoaded(scenePath))
                throw new InvalidOperationException("Scene transition is unavailable: " + scenePath);
            policy.SetTransition(true); interactive?.CloseForTransition();
            try { return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single); }
            catch { policy.SetTransition(false); throw; }
        }
        [ContextMenu("Start New Save")]
        public void StartNewSave()
        {
            if (catalog == null || pause == null || loot == null) return;
            policy.SetTransition(true); failedPause?.Dispose(); failedPause = null;
            foreach (var scope in new List<SceneBindings>(scopes.Values)) scope.Release();
            scopes.Clear(); interactive = null; input.Bind(null, null, null, null, null, null); game.ResumeGame();
            if (session == null) CreateSession(new SaveGameData { worldSeed = config.worldSeed });
            persistence.BeginNewSave();
            session.Reset(config.worldSeed, releaseOwners: true); persistence.Initialize(session, StartNewSave); persistence.Flush();
            if (!initialized) { SceneManager.sceneLoaded += Loaded; SceneManager.sceneUnloaded += Unloaded; initialized = true; }
            var scene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path)) SceneManager.LoadScene(scene.path);
        }
        private void OnDestroy()
        {
            if (active != this) return;
            policy?.SetTransition(true); persistence?.Flush();
            foreach (var scope in new List<SceneBindings>(scopes.Values)) scope.Release(); scopes.Clear();
            input?.Release(); persistence?.Release(); game?.Release(); failedPause?.Dispose(); policy?.Dispose(); clock?.Release();
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneUnloaded -= Unloaded; active = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetStatics() => active = null;
    }
}
