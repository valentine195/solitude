using System;
using SOLITUDE.Survivors;

namespace SOLITUDE.Application.Survivors
{
    public sealed class SurvivorTransition
    {
        public Guid Id { get; } = Guid.NewGuid();
        public SurvivorState Departing { get; }
        public SurvivorState Successor { get; }
        internal SurvivorTransition(SurvivorState departing, SurvivorState successor)
        { Departing = departing; Successor = successor; }
    }

    // Proof of the irrevocable equipment boundary, including an empty handoff.
    public sealed class SurvivorEquipmentReceipt
    {
        public Guid TransitionId { get; }
        public SurvivorEquipmentReceipt(Guid transitionId)
        {
            if (transitionId == Guid.Empty) throw new ArgumentException("Transition identity is required.", nameof(transitionId));
            TransitionId = transitionId;
        }
    }

    public interface ISurvivorSuccessionPorts
    {
        void SetTransitionGated(bool gated);
        // Preparation may allocate resources, but must not change equipment or life status.
        bool TryPrepare(SurvivorTransition transition, out string error);
        void CancelPreparation(SurvivorTransition transition);
        // False/exception means no commit. Success returns a matching receipt before any
        // failable presentation work. Never throw after committing equipment.
        bool TryCommitEquipment(SurvivorTransition transition, out SurvivorEquipmentReceipt receipt, out string error);
        // Retryable/idempotent for this transition. False leaves gameplay gated.
        bool TryActivate(SurvivorTransition transition, SurvivorEquipmentReceipt receipt, out string error);
        // Deduplicate by transition.Id, including retry after uncertain delivery.
        bool TryAcknowledge(SurvivorTransition transition, out string error);
    }
}
