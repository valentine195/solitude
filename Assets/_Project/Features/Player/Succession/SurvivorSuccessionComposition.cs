using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;
using UnityEngine;
namespace SOLITUDE.Player.Succession
{
    public enum SurvivorSceneEntry { FreshAwakening, SavedResume, SceneArrival, DisposableAwakening }
    public sealed class SurvivorSuccessionComposition : SceneFeatureComposition, ISurvivorSuccessionPorts
    {
        [SerializeField] private SurvivorAwakeningPoint awakeningPoint;
        [SerializeField] private SurvivorAvatarAdapter avatar;
        [SerializeField] private SurvivorRecoveryContainer recoveryPrefab;
        private SceneFeatureContext context;
        private readonly HashSet<Guid> acknowledged = new();
        private readonly List<SurvivorRecoveryContainer> recoveryContainers = new();
        private Guid preparedTransition;
        private SurvivorRecoveryContainer preparedRecovery;
        private ContainerHandoffPlan preparedHandoff;
        private ContainerHandoffReceipt committedHandoff;
        private SurvivorEquipmentReceipt preparedReceipt;
        public SurvivorSuccessionCoordinator Coordinator { get; private set; }
        public SurvivorSceneEntry EntryKind { get; private set; }
        public SurvivorAwakeningPoint AwakeningPoint => awakeningPoint;
        public int AcknowledgmentCount => acknowledged.Count;
        public SurvivorRecoveryContainer RecoveryPrefab => recoveryPrefab;
        public IReadOnlyList<SurvivorRecoveryContainer> RecoveryContainers => recoveryContainers.AsReadOnly();
        public void ConfigureRecovery(SurvivorRecoveryContainer prefab) => recoveryPrefab = prefab;
        public void Configure(SurvivorAwakeningPoint point, SurvivorAvatarAdapter adapter)
        { awakeningPoint = point; avatar = adapter; }
        public void ValidateResumeAuthoring() { Validate(); avatar.ValidateResumePoint(awakeningPoint); }
        public override void Validate()
        {
            if (awakeningPoint == null || avatar == null || awakeningPoint.gameObject.scene != gameObject.scene ||
                avatar.gameObject.scene != gameObject.scene || !awakeningPoint.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Survivor awakening configuration is missing, inactive or belongs to another scene.");
            if (gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SurvivorSuccessionComposition>(true)).Count() != 1)
                throw new InvalidOperationException("Expected one survivor succession composition per scene.");
            avatar.Validate();
            if (recoveryPrefab == null || recoveryPrefab.gameObject.activeSelf)
                throw new InvalidOperationException("An inactive survivor recovery prefab is required.");
            recoveryPrefab.ValidateAuthoring();
        }
        public override void Initialize(SceneFeatureContext source)
        {
            Release(); Validate();
            if (source?.Hotbar == null || source.Selection == null || source.ClosePlayerScreens == null || source.Feedback == null ||
                source.Input == null || source.Policy == null || source.Inventory?.IsAvailable != true || source.Hotbar.IsAvailable != true ||
                source.Interactor != avatar.Interactor || source.OpenContainer == null || source.CloseContainer == null)
                throw new InvalidOperationException("Survivor succession requires retained composed player bindings.");
            context = source;
            SurvivorState first;
            if (context.LifecycleSession == null)
            {
                EntryKind = SurvivorSceneEntry.DisposableAwakening;
                first = new SurvivorState(); first.TryActivate(); avatar.Awaken(awakeningPoint);
            }
            else
            {
                // Only the root opens readiness after the complete scene has been restored.
                context.Policy.SetReady(false); context.Input.ResetSurvivorInput();
                ValidateResumeAuthoring();
                var saved = context.LifecycleSession.Capture();
                if (saved.pendingDeath != null)
                    throw new System.IO.InvalidDataException("A committed succession requires forward recovery before scene control.");
                if (saved.survivor == null)
                {
                    EntryKind = SurvivorSceneEntry.FreshAwakening;
                    first = new SurvivorState(); first.TryActivate(); avatar.Awaken(awakeningPoint);
                    if (!context.LifecycleSession.TryInitializeLifecycle(avatar.Capture(first.Id),
                        new ExpeditionState { id = Guid.NewGuid().ToString("D") }))
                        throw new InvalidOperationException("The production lifecycle was already initialized.");
                }
                else
                {
                    EntryKind = context.IsSceneArrival ? SurvivorSceneEntry.SceneArrival : SurvivorSceneEntry.SavedResume;
                    avatar.Restore(saved.survivor, awakeningPoint, !context.IsSceneArrival);
                    first = new SurvivorState(Guid.Parse(saved.survivor.id)); first.TryActivate();
                    context.LifecycleSession.StoreLifecycle(avatar.Capture(first.Id), saved.expedition);
                }
            }
            Coordinator = new SurvivorSuccessionCoordinator(first, this); context.Selection.Select(-1);
        }
        public SuccessionOutcome RequestDeath(Guid expectedSurvivor) => Coordinator?.RequestDeath(expectedSurvivor) ?? SuccessionOutcome.Failed;
        [ContextMenu("Debug/Retire Current Survivor")]
        private void DebugDeath()
        {
            if (Coordinator?.Current == null) return;
            var result = RequestDeath(Coordinator.Current.Id);
            if (result == SuccessionOutcome.Failed) Debug.LogWarning("Survivor succession rejected: " + Coordinator.LastError, this);
        }
        [ContextMenu("Debug/Retry Survivor Awakening")]
        private void DebugRetry()
        { if (Coordinator?.Pending != null) Coordinator.Retry(); }
        public void SetTransitionGated(bool gated)
        {
            context.Policy.SetTransition(gated);
            if (gated) { context.Input.ResetSurvivorInput(); context.ClosePlayerScreens(); }
        }
        public bool TryPrepare(SurvivorTransition transition, out string error)
        {
            Validate();
            if (context.Commands.IsBusy || !context.Inventory.IsAvailable || !context.Hotbar.IsAvailable)
            { error = "Carried equipment owners are busy or unavailable."; return false; }
            preparedTransition = transition.Id;
            // Allocate the application receipt before the irreversible domain commit.
            preparedReceipt = new SurvivorEquipmentReceipt(transition.Id);
            if (!EmptyEquipment())
            {
                var position = avatar.Interactor.transform.position;
                var bodyPosition = avatar.Interactor.GetComponent<Rigidbody2D>().position;
                position.x = bodyPosition.x; position.y = bodyPosition.y;
                preparedRecovery = SurvivorRecoveryContainer.Prepare(recoveryPrefab, context, transition.Departing.Id,
                    position, checked(context.Inventory.Capacity + context.Hotbar.Capacity));
                recoveryContainers.Add(preparedRecovery);
                var result = context.Commands.PrepareHandoff(transition.Id, context.Inventory.Handle, context.Hotbar.Handle,
                    preparedRecovery.Container.Handle, out preparedHandoff);
                if (result != CommandStatus.Applied) { error = "Equipment handoff preparation rejected: " + result; return false; }
            }
            error = null; return true;
        }
        private bool EmptyEquipment() => context.Inventory.IsAvailable && context.Hotbar.IsAvailable &&
            context.Inventory.GetSlots().All(s => s.IsEmpty) && context.Hotbar.GetSlots().All(s => s.IsEmpty);
        public void CancelPreparation(SurvivorTransition transition)
        {
            if (preparedTransition != transition.Id) return;
            if (committedHandoff != null) throw new InvalidOperationException("Committed recovery ownership cannot be canceled.");
            if (preparedRecovery != null)
            {
                recoveryContainers.Remove(preparedRecovery);
                preparedRecovery.Release(); Destroy(preparedRecovery.gameObject);
            }
            ClearPreparation();
        }
        public bool TryCommitEquipment(SurvivorTransition transition, out SurvivorEquipmentReceipt receipt, out string error)
        {
            receipt = null;
            if (preparedTransition != transition.Id || preparedReceipt == null || context.Commands.IsBusy)
            { error = "Equipment handoff preparation is missing or busy."; return false; }
            if (preparedHandoff == null)
            {
                if (!EmptyEquipment()) { error = "Carried equipment changed during preparation."; return false; }
            }
            else if (committedHandoff == null)
            {
                var result = context.Commands.CommitHandoff(preparedHandoff);
                if (result.Status != CommandStatus.Applied && result.Status != CommandStatus.NoOp)
                { error = "Equipment handoff rejected: " + result.Status; return false; }
                // Only assignments follow the commit; activation/presentation happens later.
                committedHandoff = result.Receipt;
            }
            receipt = preparedReceipt; error = null; return true;
        }
        public bool TryActivate(SurvivorTransition transition, SurvivorEquipmentReceipt receipt, out string error)
        {
            Validate();
            avatar.Awaken(awakeningPoint); context.Selection.Select(-1);
            if (preparedRecovery != null && !preparedRecovery.IsPublished) preparedRecovery.Publish();
            error = null; return true;
        }
        public bool TryAcknowledge(SurvivorTransition transition, out string error)
        {
            // Reserve before presentation: an uncertain delivery must never replay the same message.
            if (acknowledged.Add(transition.Id))
                context.Feedback.Handle(InteractionResult.Success("AI: Survivor lost. New colonist awakened. Restoration continues."));
            ClearPreparation();
            error = null; return true;
        }
        public override bool TryBack() => false;
        public override void CloseForTransition() => CaptureLivingState();
        private void CaptureLivingState()
        {
            if (context?.LifecycleSession == null || Coordinator?.Current?.Status != SurvivorLifeStatus.Active
                || avatar == null || !avatar.IsAvailable || !gameObject.scene.isLoaded) return;
            var saved = context.LifecycleSession.Capture();
            if (saved.pendingDeath == null)
                context.LifecycleSession.StoreLifecycle(avatar.Capture(Coordinator.Current.Id), saved.expedition);
        }
        private void OnDestroy() => Release();
        private void ClearPreparation()
        {
            preparedTransition = Guid.Empty; preparedReceipt = null;
            preparedRecovery = null; preparedHandoff = null; committedHandoff = null;
        }
        public override void Release()
        {
            CaptureLivingState();
            foreach (var recovery in recoveryContainers)
                if (recovery != null) { recovery.Release(); Destroy(recovery.gameObject); }
            recoveryContainers.Clear(); ClearPreparation();
            Coordinator = null; context = null; acknowledged.Clear();
        }
    }
}
