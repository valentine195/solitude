using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Composition;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Survivors;
using UnityEngine;
namespace SOLITUDE.Player.Succession
{
    public sealed class SurvivorSuccessionComposition : SceneFeatureComposition, ISurvivorSuccessionPorts
    {
        [SerializeField] private SurvivorAwakeningPoint awakeningPoint;
        [SerializeField] private SurvivorAvatarAdapter avatar;
        private SceneFeatureContext context;
        private readonly HashSet<Guid> acknowledged = new();
        public SurvivorSuccessionCoordinator Coordinator { get; private set; }
        public SurvivorAwakeningPoint AwakeningPoint => awakeningPoint;
        public int AcknowledgmentCount => acknowledged.Count;
        public void Configure(SurvivorAwakeningPoint point, SurvivorAvatarAdapter adapter)
        { awakeningPoint = point; avatar = adapter; }
        public override void Validate()
        {
            if (awakeningPoint == null || avatar == null || awakeningPoint.gameObject.scene != gameObject.scene ||
                avatar.gameObject.scene != gameObject.scene || !awakeningPoint.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Survivor awakening configuration is missing, inactive or belongs to another scene.");
            if (gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SurvivorSuccessionComposition>(true)).Count() != 1)
                throw new InvalidOperationException("Expected one survivor succession composition per scene.");
            avatar.Validate();
        }
        public override void Initialize(SceneFeatureContext source)
        {
            Release(); Validate();
            if (source?.Hotbar == null || source.Selection == null || source.ClosePlayerScreens == null || source.Feedback == null ||
                source.Input == null || source.Policy == null || source.Inventory?.IsAvailable != true || source.Hotbar.IsAvailable != true ||
                source.Interactor != avatar.Interactor)
                throw new InvalidOperationException("Survivor succession requires retained composed player bindings.");
            context = source;
            var first = new SurvivorState(); first.TryActivate();
            Coordinator = new SurvivorSuccessionCoordinator(first, this);
            avatar.Awaken(awakeningPoint); context.Selection.Select(-1);
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
            if (!EmptyEquipment()) { error = "Equipped death requires the recovery-container phase."; return false; }
            error = null; return true;
        }
        private bool EmptyEquipment() => context.Inventory.IsAvailable && context.Hotbar.IsAvailable &&
            context.Inventory.GetSlots().All(s => s.IsEmpty) && context.Hotbar.GetSlots().All(s => s.IsEmpty);
        public void CancelPreparation(SurvivorTransition transition) { }
        public bool TryCommitEquipment(SurvivorTransition transition, out SurvivorEquipmentReceipt receipt, out string error)
        {
            receipt = null;
            if (!EmptyEquipment()) { error = "Carried equipment changed during preparation."; return false; }
            receipt = new SurvivorEquipmentReceipt(transition.Id); error = null; return true;
        }
        public bool TryActivate(SurvivorTransition transition, SurvivorEquipmentReceipt receipt, out string error)
        {
            avatar.Awaken(awakeningPoint); context.Selection.Select(-1);
            error = null; return true;
        }
        public bool TryAcknowledge(SurvivorTransition transition, out string error)
        {
            // Reserve before presentation: an uncertain delivery must never replay the same message.
            if (acknowledged.Add(transition.Id))
                context.Feedback.Handle(InteractionResult.Success("AI: Survivor lost. New colonist awakened. Restoration continues."));
            error = null; return true;
        }
        public override bool TryBack() => false;
        public override void CloseForTransition() { }
        private void OnDestroy() => Release();
        public override void Release() { Coordinator = null; context = null; acknowledged.Clear(); }
    }
}
