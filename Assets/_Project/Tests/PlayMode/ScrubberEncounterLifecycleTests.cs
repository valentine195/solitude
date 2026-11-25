#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.Systems;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.ScrubberEncounter;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class FailingSceneFeature : SceneFeatureComposition
    {
        public ScrubberScreenView Screen;
        public bool ShouldThrow = true;
        public int Releases, BackCalls;
        public override void Validate() { }
        public override void Initialize(SceneFeatureContext context)
        {
            if (!ShouldThrow) return;
            if (!Screen.Open()) throw new InvalidOperationException("Fixture screen failed to open.");
            throw new InvalidOperationException("Injected later feature failure.");
        }
        public override bool TryBack() { BackCalls++; return false; }
        public override void CloseForTransition() { }
        public override void Release() { Releases++; }
    }
    public class ScrubberEncounterLifecycleTests
    {
        private const string Demo="Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private string savePath; private byte[] original;
        private static T[] All<T>() where T:Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        private static AsyncOperation Load(string path) => EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
        [UnitySetUp] public IEnumerator Prepare()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-solitudeSaveDirectory");Assert.GreaterOrEqual(i,0,"Requires disposable save directory.");
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);yield return null;
            savePath=Path.Combine(args[i+1],"solitude-save.json");original=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);yield return null;Time.timeScale=1;
            if(original==null) { if(File.Exists(savePath)) File.Delete(savePath); } else File.WriteAllBytes(savePath,original);
        }
        [UnityTest] public IEnumerator LaterFeatureFailureUnwindsAllBoundFeaturesAndSceneLeases()
        {
            yield return Load(Demo); yield return null;
            var root = All<GameCompositionRoot>().Single(); var scope = All<SceneBindings>().Single();
            var feature = All<ScrubberComposition>().Single(); var screen = All<ScrubberScreenView>().Single();
            var policy = Field<InputPolicyCoordinator>(root, "policy"); var input = Field<UnityInputHost>(root, "input");
            var authoredFeatures = Field<SceneFeatureComposition[]>(scope, "features");
            scope.Release();
            var failure = scope.gameObject.AddComponent<FailingSceneFeature>(); failure.Screen = screen;
            typeof(SceneBindings).GetField("features", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(scope, authoredFeatures.Concat(new SceneFeatureComposition[] { failure }).ToArray());
            Assert.Throws<InvalidOperationException>(() => scope.Initialize(
                Field<ContainerSaveSession>(root, "session"), Field<PickupCollectionService>(root, "pickups"),
                Field<ItemPresentationCatalog>(root, "presentations"), Field<Dictionary<LootTable, RuntimeLootTable>>(root, "loot"),
                Field<ContainerGestureCoordinator>(root, "gestures"), policy, Field<PauseCoordinator>(root, "pause"),
                root, input, Field<GameManager>(root, "game"), null, (TaskJournal)root.Tasks));
            Assert.AreEqual(1, failure.Releases); Assert.IsFalse(screen.IsOpen); Assert.IsFalse(screen.Open());
            Assert.IsFalse(policy.PrimaryScreenActive); Assert.IsFalse(Field<PauseCoordinator>(root, "pause").IsPaused);
            Assert.IsFalse(All<ScrubberTaskSource>().Single().IsRegistered);
            Assert.IsNull(All<PlayerInventory>().Single().Container);
            scope.Release(); Assert.AreEqual(1, failure.Releases);
            Object.Destroy(failure); yield return null;
            yield return Load(Demo); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, root.State); Assert.IsTrue(All<ScrubberScreenView>().Single().Open());
        }
        [UnityTest] public IEnumerator BackRoutesJournalThenFeatureThenContainerAndActivationKeepsSingleBindings()
        {
            yield return Load(Demo); yield return null;
            var root = All<GameCompositionRoot>().Single(); var scope = All<SceneBindings>().Single();
            var feature = All<ScrubberComposition>().Single(); var screen = All<ScrubberScreenView>().Single();
            var source = All<ScrubberTaskSource>().Single(); var journal = All<SOLITUDE.Tasks.UI.TaskJournalView>().Single();
            var modal = Field<ContainerModalCoordinator>(scope, "modal");
            var policy = Field<InputPolicyCoordinator>(root, "policy");
            var probe = scope.gameObject.AddComponent<FailingSceneFeature>(); probe.ShouldThrow = false;
            Field<List<SceneFeatureComposition>>(scope, "boundFeatures").Add(probe);
            bool Back() => (bool)typeof(SceneBindings).GetMethod("Back", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(scope, null);
            Assert.AreEqual(OpenResult.Opened, journal.Screen.Open()); Assert.IsTrue(Back()); Assert.AreEqual(0, probe.BackCalls);
            Assert.IsFalse(journal.IsOpen);
            for (int i = 0; i < 3; i++)
            {
                feature.enabled = false; feature.enabled = true;
                source.enabled = false; source.enabled = true;
                screen.enabled = false; screen.enabled = true;
                Assert.IsTrue(source.IsRegistered); Assert.IsTrue(screen.Open()); Assert.IsTrue(Back());
                Assert.IsFalse(screen.IsOpen); Assert.AreEqual(0, probe.BackCalls, "The open feature screen wins before later features.");
            }
            Assert.AreEqual(OpenResult.Opened, modal.ToggleInventory()); Assert.IsTrue(Back());
            Assert.AreEqual(1, probe.BackCalls, "Features run before modal Back."); Assert.IsTrue(policy.Gameplay);
            Assert.IsFalse(Back()); Assert.AreEqual(2, probe.BackCalls);
            scope.Release(); scope.Release(); Assert.AreEqual(1, probe.Releases); Assert.IsFalse(source.IsRegistered);
            Assert.IsFalse(screen.Open()); Assert.IsFalse(policy.PrimaryScreenActive); Assert.IsFalse(Field<PauseCoordinator>(root, "pause").IsPaused);
        }
        [UnityTest] public IEnumerator ReloadResetsEveryStateTogetherAndReleasesOldEndpoints()
        {
            File.WriteAllText(savePath,"SCRUBBER-LIFECYCLE-SAVE-SENTINEL");var bytes=File.ReadAllBytes(savePath);
            for(int stage=0;stage<5;stage++)
            {
                yield return Load(Demo);yield return null;
                var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsTrue(root.UsesDisposableSession);
                var equipment=All<ScrubberEquipment>().Single();var screen=All<ScrubberScreenView>().Single();var room=All<ScrubberRoomFeedback>().Single();
                if(stage>=1) Assert.IsTrue(All<WorldPickup>().Single().Interact(null).IsSuccess);
                if(stage>=2) { equipment.Install();Assert.IsTrue(equipment.State.FuseInstalled); }
                if(stage>=3) { equipment.Restart();yield return new WaitForSeconds(.1f);Assert.Greater(equipment.State.RecoverySeconds,0); }
                if(stage>=4) { equipment.State.Advance(5);Assert.IsTrue(equipment.State.Operational); }
                Assert.IsTrue(screen.Open());Field<SaveGameService>(root,"persistence").Flush();CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
                var oldRunning=room.RunningSound;var policy=Field<InputPolicyCoordinator>(root,"policy");Assert.IsTrue(policy.PrimaryScreenActive);
                yield return Load(Demo);yield return null;
                Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.IsTrue(equipment==null);Assert.IsTrue(screen==null);Assert.IsTrue(oldRunning==null);Assert.IsFalse(policy.PrimaryScreenActive);Assert.IsTrue(policy.Gameplay);
                var fresh=All<ScrubberEquipment>().Single();Assert.IsFalse(fresh.State.FuseInstalled);Assert.IsFalse(fresh.State.Running);Assert.AreEqual(0,fresh.State.RecoverySeconds);Assert.AreEqual("ELEVATED",fresh.State.RoomCondition);
                Assert.IsTrue(All<WorldPickup>().Single().gameObject.activeSelf);Assert.IsTrue(All<PlayerInventory>().Single().Container.GetSlots().All(slot=>slot.IsEmpty));
                Assert.IsTrue(All<ScrubberRoomFeedback>().Single().IsWarningVisible);Assert.IsTrue(All<ScrubberRoomFeedback>().Single().Alert.isPlaying);Assert.IsFalse(All<ScrubberRoomFeedback>().Single().RunningSound.isPlaying);
                var notification=All<ScrubberTaskNotificationView>().Single();Assert.IsFalse(notification.IsVisible);Assert.AreEqual(0,notification.PresentationCount);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
            }
        }
        [UnityTest] public IEnumerator DestroyingTaskSourceDetachesFromTheStillLiveSceneScope()
        {
            yield return Load(Demo); yield return null;
            var root = All<GameCompositionRoot>().Single(); var scope = All<SceneBindings>().Single();
            var interactions = Field<SceneInteractionScope>(scope, "interactions"); var events = interactions.Events;
            var source = All<ScrubberTaskSource>().Single(); var access = source.Access; var key = source.Key;
            var journal = (TaskJournal)root.Tasks;
            int Listeners() => Field<Delegate>(events, "FocusChanged")?.GetInvocationList().Length ?? 0;
            int boundCount = Listeners(); Assert.GreaterOrEqual(boundCount, 1); Assert.IsTrue(source.IsRegistered);
            events.Publish(new InteractionFocusChangedEvent(access));
            Assert.IsTrue(journal.ReadSnapshot().Find(key).SourceAvailable);
            for (int i = 0; i < 3; i++)
            {
                source.enabled = false; Assert.AreEqual(boundCount - 1, Listeners());
                Assert.IsFalse(journal.ReadSnapshot().Find(key).SourceAvailable);
                source.enabled = true; Assert.AreEqual(boundCount, Listeners()); Assert.IsTrue(source.IsRegistered);
            }
            Object.Destroy(source); yield return null;
            Assert.IsTrue(source == null); Assert.AreSame(events, interactions.Events);
            Assert.AreEqual(boundCount - 1, Listeners());
            var detached = journal.ReadSnapshot().Find(key); Assert.IsFalse(detached.SourceAvailable);
            events.Publish(new InteractionFocusChangedEvent(access));
            Assert.AreEqual(detached.Revision, journal.ReadSnapshot().Find(key).Revision);
            scope.Release(); scope.Release(); Assert.IsNull(journal.ReadSnapshot().Find(key));
            Assert.AreEqual(0, Listeners()); Assert.IsNull(events.CurrentFocus);
        }
        [UnityTest] public IEnumerator ScrubberSessionRejectsProductionWithoutWritingEncounterItems()
        {
            File.WriteAllText(savePath,"SCRUBBER-TO-PRODUCTION-SENTINEL");var bytes=File.ReadAllBytes(savePath);
            yield return Load(Demo);yield return null;var root=All<GameCompositionRoot>().Single();var session=Field<ContainerSaveSession>(root,"session");
            Assert.IsTrue(All<WorldPickup>().Single().Interact(null).IsSuccess);
            LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Demo and production sessions cannot be mixed"));
            yield return Load("Assets/_Project/Scenes/Wakeup.unity");yield return null;
            Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.IsTrue(root.UsesDisposableSession);Assert.AreEqual(RuntimeStartupState.Failed,root.State);Assert.AreSame(session,Field<ContainerSaveSession>(root,"session"));
            Assert.IsNull(All<PlayerInventory>().Single().Container);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
        }
        [UnityTest] public IEnumerator ProductionSessionRejectsScrubberWithoutChangingInventoryOrSave()
        {
            var catalog = Resources.Load<ItemDatabase>("Database").BuildRuntimeCatalog();
            var codec = new SaveJsonCodec(catalog);
            File.WriteAllText(savePath, codec.Encode(new SaveGameData { startingLoadoutInitialized = true }));
            yield return Load("Assets/_Project/Scenes/Wakeup.unity");yield return null;
            var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsFalse(root.UsesDisposableSession);
            var session=Field<ContainerSaveSession>(root,"session");var inventory=All<PlayerInventory>().Single().Container;
            session.Commands.Grant(inventory.Handle,"c756a156ba7f34cb2b65e32264413a60",1);Field<SaveGameService>(root,"persistence").Flush();var bytes=File.ReadAllBytes(savePath);
            LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Demo and production sessions cannot be mixed"));
            yield return Load(Demo);yield return null;Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.AreEqual(RuntimeStartupState.Failed,root.State);
            Assert.AreSame(session,Field<ContainerSaveSession>(root,"session"));Assert.IsFalse(All<ScrubberEquipment>().Single().State.FuseInstalled);Assert.IsFalse(All<ScrubberScreenView>().Single().Open());CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
        }
    }
}
#endif
