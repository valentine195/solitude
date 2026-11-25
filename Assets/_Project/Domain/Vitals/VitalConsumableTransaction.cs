using SOLITUDE.Containers;
namespace SOLITUDE.Vitals
{
    public enum VitalUseOutcome { Unavailable, Ready, Committed, NoSelection, Empty, Unsupported, InvalidEffect, Dead, Full, Busy, InvalidConfiguration, InventoryRejected }
    public readonly struct VitalUseResult
    {
        public VitalUseOutcome Outcome { get; }
        public CommandStatus InventoryStatus { get; }
        public bool Committed => Outcome == VitalUseOutcome.Committed;
        public VitalUseResult(VitalUseOutcome outcome, CommandStatus inventoryStatus = CommandStatus.NoOp)
        { Outcome = outcome; InventoryStatus = inventoryStatus; }
    }
    public sealed class VitalConsumableTransaction
    {
        public VitalUseResult TryUse(ContainerCommandService commands, SlotAddress address,
            VitalConsumableSpec effect, IVitalEffectTarget target)
        {
            if (commands == null || target == null) return new VitalUseResult(VitalUseOutcome.Unavailable);
            if (effect == null || !effect.Supported) return new VitalUseResult(VitalUseOutcome.Unsupported);
            if (!effect.IsValid) return new VitalUseResult(VitalUseOutcome.InvalidEffect);
            return commands.ConsumeVital(address, effect, target);
        }
    }
}
