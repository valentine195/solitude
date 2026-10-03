using System;
namespace SOLITUDE.Containers
{
    public sealed class ContainerInteractionController
    {
        private readonly ITransferCommands commands;
        private IContainerReader origin;
        public event Action<ItemStack> HeldItemChanged;
        public event Action<SlotSnapshot> Rejected;
        public bool IsHolding => origin != null;
        public ItemStack HeldItem { get; private set; }
        public SlotAddress? OriginAddress { get; private set; }
        public ContainerInteractionController(ITransferCommands commands) => this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
        public void BeginDrag(SlotSnapshot slot)
        {
            if (IsHolding || slot == null || slot.IsEmpty) return;
            var reader = commands.Read(slot.Address.Container); var current = reader?.GetSlot(slot.Address.Index);
            if (current == null || current.Address.Revision != slot.Address.Revision) return;
            origin = reader; HeldItem = slot.Stack; OriginAddress = slot.Address;
            origin.SlotChanged += OnChanged; origin.CapacityChanged += Cancel;
            HeldItemChanged?.Invoke(HeldItem);
        }
        private void OnChanged(int index) { if (OriginAddress?.Index == index) Cancel(); }
        public void Drop(SlotSnapshot target)
        {
            if (!IsHolding || target == null) return;
            var source = OriginAddress.Value; Cancel();
            if (!commands.Transfer(source, target.Address).Succeeded) Rejected?.Invoke(target);
        }
        public void Cancel()
        {
            if (!IsHolding) return;
            origin.SlotChanged -= OnChanged; origin.CapacityChanged -= Cancel;
            origin = null; HeldItem = null; OriginAddress = null; HeldItemChanged?.Invoke(null);
        }
    }
}
