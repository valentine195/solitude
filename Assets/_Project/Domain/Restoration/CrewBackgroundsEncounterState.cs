using System;
using SOLITUDE.Containers;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.World.CrewBackgrounds
{
    public enum CrewBackgroundsFault { LossOfPower, BrokenIntakeLinkage }
    public enum CrewRepairOperation { Inspect, ResetBreaker, FitIntakeLinkage, InstallJumper, ReclaimJumper }

    public sealed class CrewBackgroundsEncounterState
    {
        public const double RecoveryDuration = 5;

        private readonly IContainerReader jumperPanel;
        private readonly string jumperId;

        public CrewBackgroundsFault Fault { get; }
        public bool NormalFeedAvailable { get; private set; }
        public bool IntakeLinkageIntact { get; private set; }
        public bool JumperInstalled => jumperPanel?.IsAvailable == true && jumperPanel.Capacity == 1 && jumperPanel.GetSlot(0).Stack?.ItemId == jumperId;
        public bool BypassActive => !NormalFeedAvailable && JumperInstalled;
        public bool CorridorLightsOn => !BypassActive;
        public bool Airflow => (NormalFeedAvailable || BypassActive) && IntakeLinkageIntact;
        public double RecoverySeconds { get; private set; }
        public bool RoomRecovered => RecoverySeconds >= RecoveryDuration;

        public CrewBackgroundsEncounterState(CrewBackgroundsFault fault, IContainerReader jumperPanel = null, string jumperId = null)
        {
            if (jumperPanel != null && (jumperPanel.Capacity != 1 || string.IsNullOrWhiteSpace(jumperId)))
                throw new ArgumentException("The jumper requires a dedicated one-slot panel.");
            this.jumperPanel = jumperPanel;
            this.jumperId = jumperId;
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

        public CommandStatus CheckJumper(CrewRepairOperation operation, ContainerCommandService commands, IContainerReader inventory)
            => PlanJumper(operation, commands, inventory, out _, out _);

        public CommandStatus TryMoveJumper(CrewRepairOperation operation, ContainerCommandService commands, IContainerReader inventory)
        {
            var result = PlanJumper(operation, commands, inventory, out var source, out var destination);
            return result == CommandStatus.Applied ? commands.Transfer(source, destination).Status : result;
        }

        private CommandStatus PlanJumper(CrewRepairOperation operation, ContainerCommandService commands,
            IContainerReader inventory, out SlotAddress source, out SlotAddress destination)
        {
            source = default;
            destination = default;
            if (Fault != CrewBackgroundsFault.LossOfPower || commands == null || inventory?.IsAvailable != true
                || inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId
                || !ReferenceEquals(commands.Read(inventory.Handle), inventory)
                || jumperPanel?.IsAvailable != true || jumperPanel.Capacity != 1
                || !ReferenceEquals(commands.Read(jumperPanel.Handle), jumperPanel)
                || jumperPanel.Handle.Equals(inventory.Handle))
                return CommandStatus.Unavailable;

            var panel = jumperPanel.GetSlot(0);
            if (operation == CrewRepairOperation.InstallJumper && !NormalFeedAvailable && panel.IsEmpty)
            {
                var part = FindPart(inventory, jumperId);
                if (!part.HasValue) return CommandStatus.InsufficientQuantity;
                source = part.Value;
                destination = panel.Address;
                return CommandStatus.Applied;
            }

            if (operation != CrewRepairOperation.ReclaimJumper || !NormalFeedAvailable || !JumperInstalled)
                return CommandStatus.Unavailable;

            foreach (var slot in inventory.GetSlots())
            {
                if (!slot.IsEmpty) continue;
                source = panel.Address;
                destination = slot.Address;
                return CommandStatus.Applied;
            }
            return CommandStatus.InsufficientSpace;
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
