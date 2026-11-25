using System;
using SOLITUDE.Containers;
using SOLITUDE.GameplayTags;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Items
{
    public sealed class CarriedEquipment
    {
        private readonly ContainerSaveSession session;
        private readonly IContainerReader carried, gear;
        private readonly ItemCapabilityCatalog capabilities;
        private readonly ItemPresentationCatalog presentations;
        private const string WeaponTagId = "bb7275a470a484c55833806c3b08364e";
        private const string ToolTagId = "deb055d7138914bd6b850fc9bae263e6";

        public CarriedEquipment(ContainerSaveSession session, IContainerReader carried, IContainerReader gear,
            ItemCapabilityCatalog capabilities, ItemPresentationCatalog presentations, int normalCapacity)
        {
            this.session = session;
            this.carried = carried;
            this.gear = gear;
            this.capabilities = capabilities;
            this.presentations = presentations;
            NormalCapacity = normalCapacity;
        }

        public int NormalCapacity { get; }
        public int SelectedQuickUse { get; private set; }
        public string SelectedQuickUseItem => State.QuickUseItem(SelectedQuickUse);
        public event Action QuickUseSelectionChanged;

        public void SelectQuickUse(int index)
        {
            if (index < 0 || index >= PlayerEquipmentRecord.QuickUseCount || index == SelectedQuickUse) return;
            SelectedQuickUse = index;
            QuickUseSelectionChanged?.Invoke();
        }
        public PlayerEquipmentRecord State => session.Equipment;
        public event Action Changed
        {
            add => session.EquipmentChanged += value;
            remove => session.EquipmentChanged -= value;
        }
        public static bool Eligible(ItemCapabilityCatalog catalog, string id, PlayerEquipmentSlot slot)
        {
            var item = catalog?.Resolve(id);
            if (item == null) return false;
            bool primary = item.Resolve(ItemActionSlot.Primary) != null;
            return slot switch
            {
                PlayerEquipmentSlot.Weapon => primary && Matches(catalog.Registry, item, WeaponTagId),
                PlayerEquipmentSlot.Tool => (primary || item.Resolve(ItemActionSlot.Contextual) != null)
                    && Matches(catalog.Registry, item, ToolTagId),
                _ => false
            };
        }

        private static bool Matches(GameplayTagRegistry registry, ItemCapabilities item, string tagId) =>
            registry.TryGetTag(tagId, out var tag) && item.Tags.HasTag(tag);

        public bool CanEquip(string id, PlayerEquipmentSlot slot) => Eligible(capabilities, id, slot);
        public bool CanPin(string id) => id != null && presentations.Consumables?.Resolve(id)?.IsValid == true;

        public void ValidateRestored()
        {
            var state = State;
            if (state.weaponItemId != null && !CanEquip(state.weaponItemId, PlayerEquipmentSlot.Weapon)) Unequip(PlayerEquipmentSlot.Weapon);
            if (state.toolItemId != null && !CanEquip(state.toolItemId, PlayerEquipmentSlot.Tool)) Unequip(PlayerEquipmentSlot.Tool);
            var next = State;
            for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
                if (!CanPin(next.QuickUseItem(i))) next.SetQuickUseItem(i, null);
            session.TrySetEquipment(next);
        }

        public SlotSnapshot GetSlot(PlayerEquipmentSlot slot) => slot switch
        {
            PlayerEquipmentSlot.Weapon => gear.GetSlot(0),
            PlayerEquipmentSlot.Tool => gear.GetSlot(1),
            _ => null
        };

        public CommandResult Equip(SlotAddress source, PlayerEquipmentSlot slot)
        {
            var target = GetSlot(slot);
            var item = session.Commands.Read(source.Container)?.GetSlot(source.Index)?.Stack;
            if (target == null || !CanEquip(item?.ItemId, slot)) return default;
            return session.Commands.EquipOne(source, target.Address, carried.Handle);
        }

        public CommandResult Unequip(PlayerEquipmentSlot slot)
        {
            var source = GetSlot(slot);
            return source?.Stack == null ? default
                : session.Commands.TransferToContainer(source.Address, carried.Handle);
        }

        public void Switch()
        {
            var next = State;
            if (next.weaponItemId == null || next.toolItemId == null) return;
            next.activeSlot = next.activeSlot == PlayerEquipmentSlot.Weapon ? PlayerEquipmentSlot.Tool : PlayerEquipmentSlot.Weapon;
            session.TrySetEquipment(next);
        }

        public void Pin(string id) => Pin(SelectedQuickUse, id);

        public bool Pin(int index, string id)
        {
            if (index < 0 || index >= PlayerEquipmentRecord.QuickUseCount || (id != null && !CanPin(id))) return false;
            var next = State;
            if (id != null)
                for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
                    if (next.QuickUseItem(i) == id) next.SetQuickUseItem(i, null);
            next.SetQuickUseItem(index, id);
            return session.TrySetEquipment(next);
        }

        public int Quantity(string id)
        {
            if (id == null) return 0;
            int quantity = 0;
            for (int i = 0; i < carried.Capacity; i++)
            {
                var stack = carried.GetSlot(i)?.Stack;
                if (stack?.ItemId == id) quantity += stack.Quantity;
            }
            return quantity;
        }
    }
}
