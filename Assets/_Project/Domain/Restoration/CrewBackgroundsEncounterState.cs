using System;
using SOLITUDE.Containers;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.World.CrewBackgrounds
{
    public enum CrewBackgroundsFault { LossOfPower, BrokenIntakeLinkage }
    public enum CrewRepairOperation { Inspect, ResetBreaker, FitIntakeLinkage }

    public sealed class CrewBackgroundsEncounterState
    {
        public const double RecoveryDuration = 5;

        public CrewBackgroundsFault Fault { get; }
        public bool NormalFeedAvailable { get; private set; }
        public bool IntakeLinkageIntact { get; private set; }
        public bool Airflow => NormalFeedAvailable && IntakeLinkageIntact;
        public double RecoverySeconds { get; private set; }
        public bool RoomRecovered => RecoverySeconds >= RecoveryDuration;

        public CrewBackgroundsEncounterState(CrewBackgroundsFault fault)
        {
            Fault = fault;
            switch (fault)
            {
                case CrewBackgroundsFault.LossOfPower:
                    IntakeLinkageIntact = true;
                    break;
                case CrewBackgroundsFault.BrokenIntakeLinkage:
                    NormalFeedAvailable = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fault));
            }
        }

        public RestorationOutcome Check(CrewRepairOperation operation, ContainerCommandService commands,
            IContainerReader inventory, string materialId)
        {
            if (operation == CrewRepairOperation.ResetBreaker && Fault == CrewBackgroundsFault.LossOfPower)
                return NormalFeedAvailable ? RestorationOutcome.AlreadyRestored : RestorationOutcome.Restored;

            if (operation != CrewRepairOperation.FitIntakeLinkage || Fault != CrewBackgroundsFault.BrokenIntakeLinkage)
                return RestorationOutcome.Unavailable;

            if (IntakeLinkageIntact) return RestorationOutcome.AlreadyRestored;
            if (commands == null || inventory?.IsAvailable != true || string.IsNullOrWhiteSpace(materialId)
                || inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId
                || !ReferenceEquals(commands.Read(inventory.Handle), inventory))
                return RestorationOutcome.Unavailable;

            return FindPart(inventory, materialId).HasValue ? RestorationOutcome.Restored : RestorationOutcome.MissingItem;
        }

        public RestorationOutcome TryRepair(CrewRepairOperation operation, ContainerCommandService commands,
            IContainerReader inventory, string materialId)
        {
            var result = Check(operation, commands, inventory, materialId);
            if (result != RestorationOutcome.Restored) return result;
            if (operation == CrewRepairOperation.ResetBreaker)
            {
                NormalFeedAvailable = true;
                return RestorationOutcome.Restored;
            }

            if (commands.IsBusy) return RestorationOutcome.Busy;
            var part = FindPart(inventory, materialId);
            if (!part.HasValue) return RestorationOutcome.MissingItem;
            var removed = commands.Remove(part.Value, 1, () => IntakeLinkageIntact = true);
            return removed.Status == CommandStatus.Applied ? RestorationOutcome.Restored : RestorationOutcome.Rejected;
        }

        public void Advance(double gameplaySeconds)
        {
            if (!Airflow || !double.IsFinite(gameplaySeconds) || gameplaySeconds <= 0) return;
            RecoverySeconds = Math.Min(RecoveryDuration, RecoverySeconds + gameplaySeconds);
        }

        private static SlotAddress? FindPart(IContainerReader inventory, string itemId)
        {
            foreach (var slot in inventory.GetSlots())
                if (!slot.IsEmpty && slot.Stack.ItemId == itemId) return slot.Address;
            return null;
        }
    }
}
