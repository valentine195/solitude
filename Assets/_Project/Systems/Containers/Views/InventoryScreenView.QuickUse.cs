using System;
using SOLITUDE.Application;
using SOLITUDE.SaveLoad;
using SOLITUDE.UI;
using SOLITUDE.UI.Exploration;
using UnityEngine;
using UnityEngine.Events;

namespace SOLITUDE.Containers
{
    public sealed partial class InventoryScreenView
    {
        [SerializeField] private GameObject inventoryQuickUse;
        [SerializeField] private ExplorationHudView.QuickUseSlot[] inventoryQuickUseSlots;
        private UnityAction[] quickSlotSelection;

        private void BindInventoryQuickUse()
        {
            if (inventoryQuickUse == null || inventoryQuickUseSlots?.Length != PlayerEquipmentRecord.QuickUseCount)
                throw new InvalidOperationException("Inventory needs five Quick Use slots.");

            quickSlotSelection = new UnityAction[inventoryQuickUseSlots.Length];
            for (int i = 0; i < inventoryQuickUseSlots.Length; i++)
            {
                int index = i;
                var button = inventoryQuickUseSlots[i].Button;
                quickSlotSelection[i] = () =>
                {
                    if (isActiveAndEnabled && policy.Pointer && transferTarget == null && !IsHandling)
                        equipment.SelectQuickUse(index);
                };
                button.onClick.AddListener(quickSlotSelection[i]);
                var dropTarget = button.GetComponent<ContainerSlotView>();
                dropTarget.Bind(i, 1, catalog);
                dropTarget.Dropped += DropOnQuickUse;
            }
            equipment.QuickUseSelectionChanged += EquipmentUpdated;
        }

        private void UnbindInventoryQuickUse()
        {
            equipment.QuickUseSelectionChanged -= EquipmentUpdated;
            if (quickSlotSelection == null) return;
            for (int i = 0; i < quickSlotSelection.Length; i++)
            {
                var button = inventoryQuickUseSlots[i].Button;
                button.onClick.RemoveListener(quickSlotSelection[i]);
                var dropTarget = button.GetComponent<ContainerSlotView>();
                dropTarget.Dropped -= DropOnQuickUse;
                dropTarget.Unbind();
            }
            quickSlotSelection = null;
        }

        private void RefreshInventoryQuickUse(PlayerEquipmentRecord state)
        {
            for (int i = 0; i < inventoryQuickUseSlots.Length; i++)
            {
                var id = state.QuickUseItem(i);
                inventoryQuickUseSlots[i].Button.GetComponent<ContainerSlotView>().SetDropDestination(pointerDrag &&
                    draggedEquipment == PlayerEquipmentSlot.None && equipment.CanPin(gestures.Interaction.HeldItem?.ItemId));
                inventoryQuickUseSlots[i].Show(catalog.Resolve(id)?.Icon, equipment.Quantity(id),
                    equipment.SelectedQuickUse == i, ShipboardUiColors.QuickUseRim, ShipboardUiColors.QuickUseSelection);
            }
        }

        private void DropOnQuickUse(SlotIntent intent)
        {
            if (!pointerDrag || !policy.Pointer || transferTarget != null) return;
            var id = gestures.Interaction.HeldItem.ItemId;
            if (draggedEquipment != PlayerEquipmentSlot.None || !equipment.CanPin(id))
            {
                RejectDrop($"{ItemName(id)} cannot go in Quick Use.");
                return;
            }
            EndPointerDrag();
            if (equipment.Pin(intent.Index, id)) equipment.SelectQuickUse(intent.Index);
            else ShowPlacementFeedback("Could not assign Quick Use. Try again.");
        }
    }
}
