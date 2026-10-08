using System;
using SOLITUDE.Application;
using SOLITUDE.Core.Events;
using SOLITUDE.Player;
using SOLITUDE.Tasks;
using UnityEngine;

namespace SOLITUDE.World.ScrubberEncounter
{
    public sealed class ScrubberTaskSource : MonoBehaviour
    {
        public const string LocalTaskId = "restore-atmosphere";
        [SerializeField] private string sourceId;
        [SerializeField] private string location;
        [SerializeField] private ScrubberEquipment equipment;
        [SerializeField] private ScrubberAccess access;
        private TaskJournal journal;
        private string scopeId;
        private InputPolicyCoordinator policy;
        private PlayerInteractor player;
        private ITaskPublisher publisher;
        private bool boundEquipment, watching;
        public string SourceId => sourceId;
        public ScrubberEquipment Equipment => equipment;
        public ScrubberAccess Access => access;
        public TaskKey Key => new TaskKey(sourceId, LocalTaskId);
        public bool IsRegistered => publisher != null;
        public void Configure(string identity, string room, ScrubberEquipment unit, ScrubberAccess target)
        { sourceId = identity; location = room; equipment = unit; access = target; }
        public void Initialize(TaskJournal tasks, string scope, InputPolicyCoordinator inputPolicy, PlayerInteractor interactor, bool equipmentBound)
        {
            ReleaseBinding(); journal = tasks; scopeId = scope; policy = inputPolicy; player = interactor; boundEquipment = equipmentBound;
            Attach();
        }
        private void Attach()
        {
            if (watching || journal == null || !boundEquipment || equipment == null || access == null || !isActiveAndEnabled) return;
            var result = journal.RegisterSource(sourceId, scopeId, out publisher);
            if (result != TaskWriteResult.Applied) { Debug.LogWarning("[ScrubberTaskSource] Registration unavailable: " + result, this); return; }
            equipment.Changed += Reconcile; access.AccessSucceeded += SuccessfulAccess; InteractionEventBus.OnFocusChanged += Focus;
            watching = true; Reconcile();
        }
        private bool Eligible => publisher != null && equipment != null && equipment.isActiveAndEnabled && access != null && access.isActiveAndEnabled && boundEquipment;
        private void Focus(InteractionFocusChangedEvent evt)
        {
            if (!Eligible || evt.interactable != access || policy?.Gameplay != true || player == null || !player.isActiveAndEnabled ||
                player.gameObject.scene != gameObject.scene || !access.CanInteract(player) || equipment.State.Operational) return;
            Publish(true);
        }
        private void SuccessfulAccess(PlayerInteractor actor)
        { if (Eligible && actor == player) Publish(true); }
        private void Reconcile() => Publish(false);
        private void Publish(bool discover)
        {
            if (!Eligible) return;
            try
            {
                // Discovery lives in the accepted registry, so reattachment cannot revoke it.
                bool discovered = discover || journal.ReadSnapshot().Find(Key) != null;
                var state = equipment.State;
                var outcome = state.Operational ? TaskOutcome.Completed : TaskOutcome.Active;
                var stage = state.Operational ? "complete" : state.Running ? "recover-air" : state.FuseInstalled ? "restart" : "replace-fuse";
                var instruction = state.Running ? "Wait for room air to recover." : state.FuseInstalled ? "Restart the scrubber." : "Replace the scrubber fuse.";
                var condition = state.Operational ? TaskCondition.Normal : state.Running ? TaskCondition.Recovering : TaskCondition.Fault;
                var snapshot = new TaskSnapshot(discovered, outcome, "RESTORE ROOM ATMOSPHERE", stage, location, condition,
                    state.OperatingStatus + " / ROOM AIR " + state.RoomCondition, instruction,
                    state.Operational || state.Running ? null : !state.FuseInstalled ? "Blown scrubber fuse" : null,
                    state.Operational ? "Room atmosphere restored." : null);
                var result = publisher.Publish(LocalTaskId, snapshot);
                if (result != TaskWriteResult.Applied && result != TaskWriteResult.Unchanged)
                    Debug.LogWarning("[ScrubberTaskSource] Snapshot rejected: " + result, this);
            }
            catch (Exception error) { Debug.LogException(error, this); }
        }
        private void Detach()
        {
            if (watching)
            {
                if (equipment != null) equipment.Changed -= Reconcile;
                if (access != null) access.AccessSucceeded -= SuccessfulAccess;
                InteractionEventBus.OnFocusChanged -= Focus; watching = false;
            }
            publisher?.Release(); publisher = null;
        }
        public void ReleaseBinding()
        { Detach(); journal = null; scopeId = null; policy = null; player = null; boundEquipment = false; }
        private void OnEnable() => Attach();
        private void OnDisable() => Detach();
        private void OnDestroy() => ReleaseBinding();
    }
}
