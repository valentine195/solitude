using System;
using SOLITUDE.Containers;
using SOLITUDE.Vitals;
namespace SOLITUDE.Application
{
    public sealed class SelectedVitalItemUseCoordinator : IDisposable
    {
        private readonly ContainerCommandService commands;
        private readonly IContainerReader hotbar;
        private readonly HotbarSelectionState selection;
        private readonly VitalConsumableCatalog effects;
        private readonly IVitalEffectTarget target;
        private readonly InputPolicyCoordinator policy;
        private readonly VitalConsumableTransaction transaction = new VitalConsumableTransaction();
        private readonly Func<string> pinnedItem;
        private bool usingItem, released;
        public SelectedVitalItemUseCoordinator(ContainerCommandService commands, IContainerReader hotbar,
            HotbarSelectionState selection, VitalConsumableCatalog effects, IVitalEffectTarget target, InputPolicyCoordinator policy, Func<string> pinnedItem = null)
        {
            this.commands = commands; this.hotbar = hotbar; this.selection = selection;
            this.effects = effects; this.target = target; this.policy = policy; this.pinnedItem = pinnedItem;
        }
        public VitalUseResult TryUse()
        {
            if (usingItem || (commands?.IsBusy ?? false)) return new VitalUseResult(VitalUseOutcome.Busy);
            if (released || commands == null || hotbar == null || (selection == null && pinnedItem == null) || effects == null
                || target == null || policy == null || !policy.Gameplay || !hotbar.IsAvailable
                || !ReferenceEquals(commands.Read(hotbar.Handle), hotbar)) return new VitalUseResult(VitalUseOutcome.Unavailable);
            SlotSnapshot slot = null;
            if (pinnedItem == null)
            {
                if (selection.ActiveIndex < 0) return new VitalUseResult(VitalUseOutcome.NoSelection);
                slot = hotbar.GetSlot(selection.ActiveIndex);
            }
            else
            {
                string id = pinnedItem();
                if (id == null) return new VitalUseResult(VitalUseOutcome.NoSelection);
                for (int i = 0; i < hotbar.Capacity; i++)
                {
                    var candidate = hotbar.GetSlot(i);
                    if (candidate?.Stack?.ItemId != id) continue;
                    slot = candidate;
                    break;
                }
            }
            if (slot == null) return new VitalUseResult(VitalUseOutcome.Unavailable);
            if (slot.IsEmpty) return new VitalUseResult(VitalUseOutcome.Empty);
            usingItem = true;
            try { return transaction.TryUse(commands, slot.Address, effects.Resolve(slot.Stack.ItemId), target); }
            finally { usingItem = false; }
        }
        public void Dispose() => released = true;
    }
}
