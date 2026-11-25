using System;
using SOLITUDE.Containers;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Application
{
    public readonly struct EquippedItemBinding
    {
        internal EquippedItemView Owner { get; }
        internal long Version { get; }
        public SlotAddress Address { get; }
        public string ItemId { get; }
        internal EquippedItemBinding(EquippedItemView owner, long version, SlotAddress address, string itemId)
        { Owner = owner; Version = version; Address = address; ItemId = itemId; }
    }

    /// <summary>Read-only equipment derived from the current hotbar selection.</summary>
    public sealed class EquippedItemView : IDisposable
    {
        private readonly ITransferCommands commands;
        private readonly IContainerReader hotbar;
        private readonly HotbarSelectionState selection;
        private readonly ContainerSaveSession session;
        private string activeId;
        private PlayerEquipmentSlot activeSlot;
        private long version;
        private bool released;
        private SlotSnapshot current;
        public event Action Changed;

        public EquippedItemView(ITransferCommands commands, IContainerReader hotbar, HotbarSelectionState selection)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            this.hotbar = hotbar ?? throw new ArgumentNullException(nameof(hotbar));
            this.selection = selection ?? throw new ArgumentNullException(nameof(selection));
            hotbar.SlotChanged += SlotChanged;
            hotbar.CapacityChanged += Refresh;
            selection.Changed += SelectionChanged;
            Refresh();
        }

        public EquippedItemView(ITransferCommands commands, IContainerReader equipmentStorage, ContainerSaveSession session)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            hotbar = equipmentStorage ?? throw new ArgumentNullException(nameof(equipmentStorage));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.EquipmentChanged += Refresh;
            Refresh();
        }

        private bool Available => !released && hotbar.IsAvailable &&
            ReferenceEquals(commands.Read(hotbar.Handle), hotbar);

        public bool TryCapture(out EquippedItemBinding binding)
        {
            binding = default;
            if (session != null)
            {
                if (!Available || activeId == null) return false;
                binding = new EquippedItemBinding(this, version, default, activeId);
                return true;
            }
            if (!Available || current == null || current.IsEmpty || selection.ActiveIndex != current.Address.Index)
                return false;
            binding = new EquippedItemBinding(this, version, current.Address, current.Stack.ItemId);
            return true;
        }

        public bool IsCurrent(EquippedItemBinding binding)
        {
            if (session != null) return Available && binding.Owner == this && binding.Version == version
                && activeId != null && activeId == binding.ItemId;
            return Available && binding.Owner == this &&
            binding.Version == version && current != null && !current.IsEmpty &&
            selection.ActiveIndex == binding.Address.Index && current.Address.Container.Equals(binding.Address.Container) &&
            current.Address.Revision == binding.Address.Revision && current.Stack.ItemId == binding.ItemId;
        }

        private void SelectionChanged(int _) => Refresh();
        private void SlotChanged(int index) { if (index == selection.ActiveIndex) Refresh(); }
        private void Refresh()
        {
            if (session != null)
            {
                var state = Available ? session.Equipment : null;
                var id = state?.ActiveItemId;
                var slot = state?.activeSlot ?? PlayerEquipmentSlot.None;
                if (id == activeId && slot == activeSlot) return;
                activeId = id;
                activeSlot = slot;
                version++;
                Changed?.Invoke();
                return;
            }
            version++;
            current = Available ? hotbar.GetSlot(selection.ActiveIndex) : null;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (released) return;
            released = true;
            hotbar.SlotChanged -= SlotChanged;
            hotbar.CapacityChanged -= Refresh;
            if (session != null) session.EquipmentChanged -= Refresh;
            else selection.Changed -= SelectionChanged;
            Refresh();
        }
    }
}
