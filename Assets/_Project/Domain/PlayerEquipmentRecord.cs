using System;

namespace SOLITUDE.SaveLoad
{
    public enum PlayerEquipmentSlot { None, Weapon, Tool }

    [Serializable]
    public sealed class PlayerEquipmentRecord
    {
        public const int QuickUseCount = 5;
        public string weaponItemId;
        public string toolItemId;
        public PlayerEquipmentSlot activeSlot;
        public string pinnedItemId;
        public string[] additionalQuickUseItems;
        public bool separateStorage;

        public string ItemIn(PlayerEquipmentSlot slot) => slot == PlayerEquipmentSlot.Weapon ? weaponItemId
            : slot == PlayerEquipmentSlot.Tool ? toolItemId : null;

        public string ActiveItemId => activeSlot == PlayerEquipmentSlot.Weapon ? weaponItemId
            : activeSlot == PlayerEquipmentSlot.Tool ? toolItemId : null;

        public string QuickUseItem(int index) => index == 0 ? pinnedItemId :
            index > 0 && index < QuickUseCount && additionalQuickUseItems != null ? additionalQuickUseItems[index - 1] : null;

        public void SetQuickUseItem(int index, string id)
        {
            if (index < 0 || index >= QuickUseCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0) pinnedItemId = id;
            else if (id != null || additionalQuickUseItems != null)
            {
                additionalQuickUseItems ??= new string[QuickUseCount - 1];
                additionalQuickUseItems[index - 1] = id;
            }
        }

        public bool HasQuickUseItem(string id)
        {
            if (id == null) return false;
            for (int i = 0; i < QuickUseCount; i++) if (QuickUseItem(i) == id) return true;
            return false;
        }

        public PlayerEquipmentRecord Clone()
        {
            var copy = (PlayerEquipmentRecord)MemberwiseClone();
            if (additionalQuickUseItems != null) copy.additionalQuickUseItems = (string[])additionalQuickUseItems.Clone();
            return copy;
        }

        internal void ResolveActiveSlot()
        {
            if (ActiveItemId != null) return;
            activeSlot = weaponItemId != null ? PlayerEquipmentSlot.Weapon
                : toolItemId != null ? PlayerEquipmentSlot.Tool : PlayerEquipmentSlot.None;
        }
    }
}
