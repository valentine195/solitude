using SOLITUDE.Application;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Containers
{
    public sealed partial class InventoryScreenView
    {
        private const string DragOwner = "InventoryPointer";
        private ContainerSlotView weaponDragView, toolDragView;
        private bool pointerDrag;
        private PlayerEquipmentSlot draggedEquipment;

        private void BindSlotDrag()
        {
            carriedView.BeginDrag += BeginCarriedDrag;
            carriedView.EndDrag += EndSlotDrag;
            carriedView.Drop += DropOnCarried;
            if (lockerView != null)
            {
                lockerView.BeginDrag += BeginLockerDrag;
                lockerView.EndDrag += EndSlotDrag;
                lockerView.Drop += DropOnLocker;
            }
            if (equipment == null) return;
            weaponDragView = weaponCard.GetComponent<ContainerSlotView>();
            toolDragView = toolCard.GetComponent<ContainerSlotView>();
            BindEquipmentDrag(weaponDragView, PlayerEquipmentSlot.Weapon);
            BindEquipmentDrag(toolDragView, PlayerEquipmentSlot.Tool);
        }

        private void BindEquipmentDrag(ContainerSlotView view, PlayerEquipmentSlot slot)
        {
            view.Bind((int)slot, 1, catalog);
            view.BeginDrag += BeginEquipmentDrag;
            view.EndDrag += EndSlotDrag;
            view.Dropped += DropOnEquipment;
        }

        private void UnbindSlotDrag()
        {
            carriedView.BeginDrag -= BeginCarriedDrag;
            carriedView.EndDrag -= EndSlotDrag;
            carriedView.Drop -= DropOnCarried;
            if (lockerView != null)
            {
                lockerView.BeginDrag -= BeginLockerDrag;
                lockerView.EndDrag -= EndSlotDrag;
                lockerView.Drop -= DropOnLocker;
            }
            UnbindEquipmentDrag(weaponDragView);
            UnbindEquipmentDrag(toolDragView);
        }

        private void UnbindEquipmentDrag(ContainerSlotView view)
        {
            if (view == null) return;
            view.BeginDrag -= BeginEquipmentDrag;
            view.EndDrag -= EndSlotDrag;
            view.Dropped -= DropOnEquipment;
            view.Unbind();
        }

        private void BeginCarriedDrag(SlotIntent intent)
        {
            var slot = carried.GetSlot(intent.Index);
            if (slot?.Stack != null && (transferTarget != null ? intent.Binding == transferGeneration : equipment != null))
                BeginPointerDrag(slot);
        }

        private void BeginLockerDrag(SlotIntent intent)
        {
            if (transferTarget != null && intent.Binding == transferGeneration)
                BeginPointerDrag(transferTarget.GetSlot(intent.Index));
        }

        private void BeginEquipmentDrag(SlotIntent intent)
        {
            var slot = (PlayerEquipmentSlot)intent.Index;
            BeginPointerDrag(equipment.GetSlot(slot), slot);
        }

        private void BeginPointerDrag(SlotSnapshot slot, PlayerEquipmentSlot equipmentSlot = PlayerEquipmentSlot.None)
        {
            if (!isActiveAndEnabled || !policy.Pointer || handling != Handling.None || gestures.Interaction.IsHolding ||
                slot?.IsEmpty != false) return;
            gestures.Begin(DragOwner, slot);
            pointerDrag = gestures.Interaction.IsHolding;
            draggedEquipment = equipmentSlot;
            if (!pointerDrag) return;
            if (transferTarget != null)
            {
                itemPopover.gameObject.SetActive(false);
                transferReceipt = "Drop on a highlighted slot.";
                RenderTransfer();
            }
            else
            {
                context.text = "Drop on a highlighted destination.";
                Refresh();
            }
        }

        private void EndSlotDrag(SlotIntent _)
        {
            if (pointerDrag) RejectDrop("Drop canceled. Item stayed in place.");
        }

        private void RejectDrop(string message)
        {
            EndPointerDrag();
            ShowPlacementFeedback(message);
        }

        private void ShowPlacementFeedback(string message)
        {
            if (transferTarget != null)
            {
                transferReceipt = message;
                RenderTransfer();
            }
            else context.text = message;
        }

        private bool CanPlaceInCarried(SlotSnapshot target)
        {
            if (target == null || gestures.Interaction.OriginAddress is not SlotAddress origin) return false;
            return commands.CanTransfer(origin, target.Address, one: pointerDrag && draggedEquipment != PlayerEquipmentSlot.None);
        }

        private void RefreshEquipmentDropTargets()
        {
            string id = pointerDrag ? gestures.Interaction.HeldItem?.ItemId : null;
            weaponDragView.SetDropDestination(id != null && draggedEquipment != PlayerEquipmentSlot.Weapon &&
                equipment.CanEquip(id, PlayerEquipmentSlot.Weapon));
            toolDragView.SetDropDestination(id != null && draggedEquipment != PlayerEquipmentSlot.Tool &&
                equipment.CanEquip(id, PlayerEquipmentSlot.Tool));
        }

        private void EndPointerDrag()
        {
            if (!pointerDrag) return;
            pointerDrag = false;
            if (transferTarget != null) transferReceipt = null;
            else context.text = string.Empty;
            gestures.End(DragOwner);
        }

        private void DropOnCarried(SlotIntent intent)
        {
            if (transferTarget != null)
            {
                DropInTransfer(carried, intent);
                return;
            }
            if (!pointerDrag || !policy.Pointer) return;
            var target = carried.GetSlot(intent.Index);
            if (target == null) return;
            var origin = gestures.Interaction.OriginAddress.Value;
            bool unequip = draggedEquipment != PlayerEquipmentSlot.None;
            if (!unequip && origin.Index == target.Address.Index)
            {
                EndPointerDrag();
                return;
            }
            if (!CanPlaceInCarried(target))
            {
                RejectDrop(unequip ? "Choose a free Carried slot." : "That Carried slot is unavailable.");
                return;
            }
            var itemName = ItemName(gestures.Interaction.HeldItem.ItemId);
            EndPointerDrag();
            var result = unequip ? commands.TransferOne(origin, target.Address) : commands.Transfer(origin, target.Address);
            if (result.Moved == 0)
            {
                ShowPlacementFeedback("Could not move the item. Try again.");
                return;
            }
            Inspect(false, intent.Index);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(carriedView.SlotViews[intent.Index].gameObject);
            ShowPlacementFeedback(unequip ? $"Unequipped {itemName}." : $"Moved {itemName}.");
        }

        private void DropOnEquipment(SlotIntent intent)
        {
            if (!pointerDrag || !policy.Pointer || transferTarget != null || equipment == null) return;
            var target = (PlayerEquipmentSlot)intent.Index;
            var id = gestures.Interaction.HeldItem.ItemId;
            if (target == draggedEquipment)
            {
                EndPointerDrag();
                return;
            }
            if (!equipment.CanEquip(id, target))
            {
                RejectDrop(target == PlayerEquipmentSlot.Weapon
                    ? $"Cannot equip {ItemName(id)} as a weapon."
                    : $"Cannot equip {ItemName(id)} as a tool.");
                return;
            }
            var origin = gestures.Interaction.OriginAddress.Value;
            EndPointerDrag();
            var result = equipment.Equip(origin, target);
            if (result.Moved == 0)
            {
                ShowPlacementFeedback(result.Status == CommandStatus.InsufficientSpace ? "Carried is full." : "Equipment change unavailable.");
                return;
            }
            InspectEquipment(target);
        }

        private void DropOnLocker(SlotIntent intent) => DropInTransfer(transferTarget, intent);

        private void DropInTransfer(IContainerReader destination, SlotIntent intent)
        {
            if (!pointerDrag || !policy.Pointer || transferTarget == null || intent.Binding != transferGeneration) return;
            var target = destination.GetSlot(intent.Index);
            if (target == null) return;
            var origin = gestures.Interaction.OriginAddress.Value;
            if (origin.Container.Equals(target.Address.Container) && origin.Index == target.Address.Index)
            {
                EndPointerDrag();
                return;
            }
            if (!CanDropInTransfer(origin, target))
            {
                RejectDrop("That slot cannot take this item.");
                return;
            }
            var name = ItemName(gestures.Interaction.HeldItem.ItemId);
            var equippedSlot = draggedEquipment;
            EndPointerDrag();
            var result = equippedSlot == PlayerEquipmentSlot.None
                ? commands.Transfer(origin, target.Address)
                : commands.TransferOne(origin, target.Address);
            if (!result.Succeeded || result.Moved == 0)
            {
                ShowPlacementFeedback("Could not move the item. Try again.");
                return;
            }
            InspectTransfer(destination == transferTarget, intent);
            transferFocus = true;
            ShowPlacementFeedback($"{name} ×{result.Moved} moved.");
        }
    }
}
