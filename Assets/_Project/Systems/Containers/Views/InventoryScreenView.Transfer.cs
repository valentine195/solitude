using System;
using SOLITUDE.Application;
using SOLITUDE.UI;
using SOLITUDE.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SOLITUDE.Containers
{
    public sealed partial class InventoryScreenView
    {
        [SerializeField] private ContainerView lockerView;
        [SerializeField] private TMP_Text lockerCapacity, lockerEmpty, transferTitle, transferLabel, transferDirection, transferNote;
        [SerializeField] private Button transferAction;
        [SerializeField] private RectTransform transferPanel;
        [SerializeField] private Image transferDimmer;
        [SerializeField] private GameObject[] transferShow, transferHide;

        [SerializeField] private RectTransform[] transferCarriedLayout, transferEquipmentLayout;
        [SerializeField] private RectTransform transferPopoverFace;
        [SerializeField] private PopoverPlacement[] transferPopoverLayout;

        [Serializable]
        private struct PopoverPlacement
        {
            public RectTransform target;
            public Rect bounds;
        }

        private const float TransferCarriedShift = 160;
        private const float TransferEquipmentShift = 1180;
        private IContainerReader transferTarget;
        private GameObject[] transferVisibility;
        private bool[] transferPriorVisibility;
        private Vector2 transferPriorPosition;
        private Vector3 transferPriorScale;
        private Color transferPriorDimmer;
        private string transferPriorTitle, transferReceipt;
        private bool transferFromLocker, transferDirty, transferRebuild, transferFocus;
        private int transferIndex, transferPriorPopoverOrder;
        private Vector2 transferPriorPopoverPosition;
        private Rect[] transferPriorPopoverLayout;
        private PlayerEquipmentSlot transferEquipment;
        private long transferGeneration;

        public bool SupportsTransfer => lockerView != null;

        private void ValidateTransferAuthoring()
        {
            if (!SupportsTransfer) return;
            lockerView.ValidateAuthoring();
            if (lockerCapacity == null || lockerEmpty == null || transferTitle == null || transferLabel == null ||
                transferDirection == null || transferNote == null || transferAction == null ||
                transferPanel == null || transferDimmer == null || itemPopover == null || itemArtField == null ||
                transferPopoverFace == null || transferPopoverLayout == null || transferPopoverLayout.Length == 0 ||
                Array.Exists(transferPopoverLayout, placement => placement.target == null) ||
                transferShow == null || transferHide == null || transferCarriedLayout == null || transferEquipmentLayout == null ||
                Array.Exists(transferCarriedLayout, rect => rect == null) || Array.Exists(transferEquipmentLayout, rect => rect == null) ||
                Array.Exists(transferShow, element => element == null) || Array.Exists(transferHide, element => element == null))
                throw new InvalidOperationException("Inventory transfer authored endpoints are incomplete.");
        }

        public void BeginTransfer(IContainerReader target, string label)
        {
            if (!initialized || !SupportsTransfer || target?.IsAvailable != true)
                throw new InvalidOperationException("Inventory transfer is unavailable.");

            CancelHandling();
            if (transferTarget == null)
            {
                var visible = new System.Collections.Generic.List<GameObject>(transferShow);
                visible.AddRange(transferHide);
                visible.Add(itemPopover.gameObject);
                var menuBacking = transferDimmer.transform.Find("ShipMenuBody");
                if (menuBacking != null) visible.Add(menuBacking.gameObject);
                if (inventoryQuickUse != null) visible.Add(inventoryQuickUse);
                transferVisibility = visible.ToArray();
                transferPriorVisibility = Array.ConvertAll(transferVisibility, element => element.activeSelf);
                transferPriorPosition = transferPanel.anchoredPosition;
                transferPriorScale = transferPanel.localScale;
                transferPriorDimmer = transferDimmer.color;
                transferPriorTitle = transferTitle.text;
                transferPriorPopoverOrder = itemPopover.GetSiblingIndex();
                transferPriorPopoverPosition = itemPopover.anchoredPosition;
                itemPopover.SetAsLastSibling();
                ApplyTransferPopoverLayout();
                ShiftTransferColumns(1);
                lockerView.Initialize(catalog);
                lockerView.Focused += FocusLocker;
                lockerView.Selected += SelectLocker;
                transferAction.onClick.AddListener(TransferStack);
            }
            else DetachTransferTarget();

            transferTarget = target;
            target.SlotChanged += TransferSlotChanged;
            target.CapacityChanged += TransferCapacityChanged;
            transferReceipt = null;
            transferPanel.anchoredPosition = Vector2.zero;
            transferPanel.localScale = Vector3.one;
            transferDimmer.color = new Color(.025f, .036f, .043f, .82f);
            for (int i = 0; i < transferVisibility.Length; i++)
                transferVisibility[i].SetActive(i < transferShow.Length);
            transferTitle.text = label.ToUpperInvariant();
            SetHotbarInput(false);
            hotbarView.SetInspectionVisible(false);
            foreach (var button in actionButtons)
                if (button != closeButton) button.gameObject.SetActive(false);

            BuildTransferSlots();
            transferIndex = Mathf.Max(0, FirstOccupied(target));
            transferFromLocker = target.Capacity > 0;
            transferEquipment = PlayerEquipmentSlot.None;
            transferFocus = true;
            RenderTransfer();
        }

        public void EndTransfer()
        {
            if (transferTarget == null) return;
            EndPointerDrag();
            DetachTransferTarget();
            transferTarget = null;
            ShiftTransferColumns(-1);
            RestoreTransferPopoverLayout();
            itemPopover.SetSiblingIndex(transferPriorPopoverOrder);
            itemPopover.anchoredPosition = transferPriorPopoverPosition;
            if (lockerView != null)
            {
                lockerView.Focused -= FocusLocker;
                lockerView.Selected -= SelectLocker;
                lockerView.Clear();
            }
            if (transferAction != null)
            {
                transferAction.onClick.RemoveListener(TransferStack);
                transferAction.gameObject.SetActive(false);
            }
            if (carriedView != null) carriedView.Clear();
            for (int i = 0; i < transferVisibility.Length; i++)
                if (transferVisibility[i] != null) transferVisibility[i].SetActive(transferPriorVisibility[i]);
            if (transferTitle != null) transferTitle.text = transferPriorTitle;
            if (transferPanel != null)
            {
                transferPanel.anchoredPosition = transferPriorPosition;
                transferPanel.localScale = transferPriorScale;
            }
            if (transferDimmer != null) transferDimmer.color = transferPriorDimmer;
            transferVisibility = null;
            transferPriorVisibility = null;
            transferDirty = transferRebuild = transferFocus = false;
        }

        private void ShiftTransferColumns(int direction)
        {
            foreach (var rect in transferCarriedLayout)
                rect.anchoredPosition += Vector2.right * (direction * TransferCarriedShift);
            foreach (var rect in transferEquipmentLayout)
                rect.anchoredPosition += Vector2.right * (direction * TransferEquipmentShift);
        }

        private void ApplyTransferPopoverLayout()
        {
            transferPriorPopoverLayout = new Rect[transferPopoverLayout.Length];
            for (int i = 0; i < transferPopoverLayout.Length; i++)
            {
                var placement = transferPopoverLayout[i];
                var target = placement.target;
                transferPriorPopoverLayout[i] = new Rect(target.anchoredPosition, target.sizeDelta);
                target.anchoredPosition = placement.bounds.position;
                target.sizeDelta = placement.bounds.size;
            }
        }

        private void RestoreTransferPopoverLayout()
        {
            for (int i = 0; i < transferPriorPopoverLayout.Length; i++)
            {
                var rect = transferPriorPopoverLayout[i];
                var target = transferPopoverLayout[i].target;
                target.anchoredPosition = rect.position;
                target.sizeDelta = rect.size;
            }
            transferPriorPopoverLayout = null;
        }

        private void DetachTransferTarget()
        {
            transferTarget.SlotChanged -= TransferSlotChanged;
            transferTarget.CapacityChanged -= TransferCapacityChanged;
        }

        private void TransferSlotChanged(int _) => transferDirty = true;
        private void TransferCapacityChanged() => transferRebuild = true;
        private void BuildTransferSlots()
        {
            EndPointerDrag();
            transferGeneration++;
            lockerView.Build(string.Empty, transferTarget.Capacity, transferGeneration);
            lockerEmpty.rectTransform.anchoredPosition = new Vector2(0, -70 - lockerView.VisibleGridHeight);
            carriedView.Build("Carried", carried.Capacity, transferGeneration);
            foreach (var slot in carriedView.SlotViews) slot.SetEligible(true);
            transferRebuild = false;
        }

        private static int FirstOccupied(IContainerReader reader)
        {
            for (int i = 0; i < reader.Capacity; i++)
                if (reader.GetSlot(i)?.IsEmpty == false) return i;
            return -1;
        }

        private void FocusLocker(SlotIntent intent) => InspectTransfer(true, intent);
        private void SelectLocker(SlotIntent intent) => SelectTransfer(true, intent);
        private void InspectTransfer(bool fromLocker, SlotIntent intent)
        {
            if (!isActiveAndEnabled || !policy.Pointer || intent.Binding != transferGeneration) return;
            if (transferEquipment != PlayerEquipmentSlot.None || transferFromLocker != fromLocker || transferIndex != intent.Index)
            {
                itemPopover.gameObject.SetActive(false);
                transferReceipt = null;
            }
            transferEquipment = PlayerEquipmentSlot.None;
            transferFromLocker = fromLocker;
            transferIndex = intent.Index;
            (fromLocker ? lockerView : carriedView).RevealSlot(intent.Index);
            RenderTransfer();
        }

        private void SelectTransfer(bool fromLocker, SlotIntent intent)
        {
            if (intent.Binding != transferGeneration || !policy.Pointer || pointerDrag) return;
            InspectTransfer(fromLocker, intent);
            OpenTransferPopover();
        }

        private void FocusTransferEquipment(PlayerEquipmentSlot slot)
        {
            if (transferEquipment == slot) return;
            itemPopover.gameObject.SetActive(false);
            transferEquipment = slot;
            transferFromLocker = false;
            transferReceipt = null;
            RenderTransfer();
        }

        private void OpenTransferPopover()
        {
            if (pointerDrag || TransferSlot()?.Stack == null) return;
            transferReceipt = null;
            itemPopover.gameObject.SetActive(true);
            RenderTransfer();
            PositionTransferPopover();
            if (transferAction.interactable)
                EventSystem.current?.SetSelectedGameObject(transferAction.gameObject);
        }

        private bool CloseTransferPopover()
        {
            if (transferTarget == null || !itemPopover.gameObject.activeSelf) return false;
            itemPopover.gameObject.SetActive(false);
            transferFocus = true;
            RenderTransfer();
            return true;
        }

        private GameObject TransferFocusObject()
        {
            if (transferEquipment != PlayerEquipmentSlot.None)
                return (transferEquipment == PlayerEquipmentSlot.Weapon ? weaponCard : toolCard).gameObject;
            var slots = (transferFromLocker ? lockerView : carriedView).SlotViews;
            return transferIndex >= 0 && transferIndex < slots.Count && slots[transferIndex].GetComponent<Button>().interactable
                ? slots[transferIndex].gameObject : closeButton.gameObject;
        }

        private void PositionTransferPopover()
        {
            if (!itemPopover.gameObject.activeSelf) return;
            var sourceRect = (RectTransform)TransferFocusObject().transform;
            sourceRect.GetWorldCorners(popoverCorners);
            var panelRect = transferPanel.rect;
            var low = transferPanel.InverseTransformPoint(popoverCorners[0]);
            var high = transferPanel.InverseTransformPoint(popoverCorners[2]);
            const float inset = 36, gap = 24;
            float bottom = transferNote.rectTransform.anchoredPosition.y + transferPopoverFace.rect.height
                - transferPopoverFace.anchoredPosition.y + 6;
            if (transferFromLocker)
            {
                var column = (RectTransform)lockerView.transform;
                float belowGrid = column.anchoredPosition.y + lockerEmpty.rectTransform.anchoredPosition.y - 4;
                if (belowGrid >= bottom)
                {
                    itemPopover.anchoredPosition = new Vector2(column.anchoredPosition.x, belowGrid);
                    return;
                }
            }
            float x = high.x - panelRect.xMin + gap;
            if (x + itemPopover.rect.width > panelRect.width - inset)
                x = low.x - panelRect.xMin - gap - itemPopover.rect.width;
            x = Mathf.Clamp(x, inset, panelRect.width - inset - itemPopover.rect.width);
            float y = Mathf.Clamp(high.y - panelRect.yMax, bottom, -116);
            itemPopover.anchoredPosition = new Vector2(x, y);
        }

        private SlotSnapshot TransferSlot() => transferEquipment != PlayerEquipmentSlot.None
            ? equipment?.GetSlot(transferEquipment)
            : (transferFromLocker ? transferTarget : carried).GetSlot(transferIndex);

        private IContainerReader TransferDestination() => transferFromLocker ? carried : transferTarget;

        private void TransferStack()
        {
            if (!isActiveAndEnabled || !policy.Pointer || transferTarget == null || pointerDrag) return;
            input.ConsumeUIActivation();
            var slot = TransferSlot();
            int quantity = slot?.Stack?.Quantity ?? 0;
            if (quantity == 0) return;

            var result = commands.TransferToContainer(slot.Address, TransferDestination().Handle, quantity);
            if (result.Moved == 0)
            {
                transferReceipt = result.Status == CommandStatus.InsufficientSpace ? DestinationFull() : "Transfer unavailable.";
                RenderTransfer();
                return;
            }
            if (transferTarget == null) return;

            itemPopover.gameObject.SetActive(false);
            string itemName = ItemName(slot.Stack.ItemId);
            transferReceipt = result.Remaining > 0
                ? $"{itemName} ×{result.Moved} moved. {result.Remaining} left."
                : itemName + (result.Moved == 1 ? "" : $" ×{result.Moved}") +
                  (transferFromLocker ? " moved to Carried." : " stored.");
            transferFocus = transferDirty = true;
        }

        private string DestinationFull() => transferFromLocker ? "Carried inventory is full." : "Container is full.";

        private void UpdateTransfer()
        {
            if (transferRebuild)
            {
                BuildTransferSlots();
                transferDirty = transferFocus = true;
            }
            if (transferDirty) RenderTransfer();
            if (transferFocus)
            {
                transferFocus = false;
                EventSystem.current?.SetSelectedGameObject(TransferFocusObject());
            }

            var selection = EventSystem.current?.currentSelectedGameObject;
            if (equipment != null && !pointerDrag)
            {
                if (selection == weaponCard.gameObject) FocusTransferEquipment(PlayerEquipmentSlot.Weapon);
                else if (selection == toolCard.gameObject) FocusTransferEquipment(PlayerEquipmentSlot.Tool);
            }
            PositionTransferPopover();

            bool focused = transferAction.interactable && EventSystem.current?.currentSelectedGameObject == transferAction.gameObject;
            transferAction.image.color = focused ? ShipboardUiColors.Amber : new Color32(35, 53, 62, 255);
            transferLabel.color = focused ? ShipboardUiColors.PanelInk :
                transferAction.interactable ? new Color32(220, 229, 226, 255) : new Color32(165, 181, 179, 255);
            transferDirection.color = transferLabel.color;
            bool closeFocused = EventSystem.current?.currentSelectedGameObject == closeButton.gameObject;
            closeButton.image.color = closeFocused ? (Color)ShipboardUiColors.Amber : Color.clear;
            backLabel.color = closeFocused ? ShipboardUiColors.PanelInk : new Color32(220, 229, 226, 255);
            if (actionFocus[0] != null) actionFocus[0].SetActive(closeFocused);
        }

        private void RenderTransfer()
        {
            transferDirty = false;
            var state = equipment?.State;
            RefreshCarriedCapacity();
            int lockerUsed = RenderTransferGrid(lockerView, transferTarget, transferFromLocker);
            RenderTransferGrid(carriedView, carried, !transferFromLocker && transferEquipment == PlayerEquipmentSlot.None);
            lockerCapacity.text = $"{lockerUsed} / {transferTarget.Capacity}";
            lockerEmpty.gameObject.SetActive(lockerUsed == 0);

            if (state != null)
            {
                ShowEquipment(weaponName, weaponIcon, weaponStatus, state.weaponItemId,
                    state.activeSlot == PlayerEquipmentSlot.Weapon && state.weaponItemId != null ? "In hand" : "");
                ShowEquipment(toolName, toolIcon, toolStatus, state.toolItemId,
                    state.activeSlot == PlayerEquipmentSlot.Tool && state.toolItemId != null ? "In hand" : "");
                weaponCard.interactable = toolCard.interactable = true;
                weaponCardMarker.SetActive(transferEquipment == PlayerEquipmentSlot.Weapon);
                toolCardMarker.SetActive(transferEquipment == PlayerEquipmentSlot.Tool);
            }

            var selected = TransferSlot();
            int quantity = selected?.Stack?.Quantity ?? 0;
            if (quantity == 0 && itemPopover.gameObject.activeSelf)
            {
                itemPopover.gameObject.SetActive(false);
                transferFocus = true;
            }
            bool inspecting = itemPopover.gameObject.activeSelf;
            backLabel.text = input.DeviceBindingLabel("Container/Cancel") + (inspecting ? "  Back" : "  Close");

            var stack = quantity > 0 ? selected.Stack : null;
            var item = catalog.Resolve(stack?.ItemId);
            itemTitle.text = item?.DisplayName ?? "";
            itemIcon.sprite = item?.Icon;
            itemIcon.enabled = item?.Icon != null;
            itemArtField.SetActive(itemIcon.enabled);
            metadata.gameObject.SetActive(stack != null);
            metadata.text = transferEquipment != PlayerEquipmentSlot.None
                ? (transferEquipment == PlayerEquipmentSlot.Weapon ? "Weapon" : "Tool") +
                    (state?.activeSlot == transferEquipment ? "  ·  In hand" : "")
                : stack == null ? "" : $"{(transferFromLocker ? "In container" : "Carried")}  ·  {quantity} in stack";
            description.text = item?.Description ?? "";
            if (actionDivider != null) actionDivider.SetActive(stack != null);

            int amount = stack == null ? 0 : commands.ContainerTransferQuantity(selected.Address, TransferDestination().Handle, quantity);
            bool clearsPin = !transferFromLocker && transferEquipment == PlayerEquipmentSlot.None && amount > 0 && state?.HasQuickUseItem(stack.ItemId) == true &&
                amount == equipment.Quantity(stack.ItemId);
            transferAction.gameObject.SetActive(inspecting && stack != null);
            transferAction.interactable = amount > 0 && !pointerDrag;
            transferLabel.text = transferFromLocker ? "Take to Carried" :
                transferEquipment != PlayerEquipmentSlot.None ? "Store equipped" : "Store in container";
            transferDirection.text = transferFromLocker ? "→" : "←";

            string note = !inspecting ? (quantity > 0 ? TransferInspectHint() : "") :
                amount == 0 ? (TransferDestination().CanAccept(stack.Definition) ? DestinationFull() : "This container cannot accept this item.") :
                clearsPin ? "Storing the last copy clears Quick Use." :
                amount < quantity ? $"Only {amount} of {quantity} will fit." : "";
            transferNote.color = inspecting && (amount < quantity || clearsPin) ? ShipboardUiColors.Amber : new Color32(195, 207, 205, 255);
            transferNote.text = transferReceipt ?? note;
        }

        private string TransferInspectHint() => input.LastInputWasGamepad
            ? (UnityEngine.InputSystem.Gamepad.current?.buttonSouth.shortDisplayName ?? "A") + "  Inspect"
            : "Click or press Enter to inspect";

        private bool CanDropInTransfer(SlotAddress origin, SlotSnapshot target) =>
            target != null && commands.CanTransfer(origin, target.Address, one: draggedEquipment != PlayerEquipmentSlot.None);

        private int RenderTransferGrid(ContainerView view, IContainerReader reader, bool selected)
        {
            int occupied = 0;
            var origin = pointerDrag ? gestures.Interaction.OriginAddress : null;
            for (int i = 0; i < view.SlotViews.Count; i++)
            {
                var slot = reader.GetSlot(i);
                int quantity = slot?.Stack?.Quantity ?? 0;
                if (quantity > 0) occupied++;
                var cell = view.SlotViews[i];
                cell.SetDropDestination(origin.HasValue && CanDropInTransfer(origin.Value, slot));
                cell.Render(new SlotPresentation(i, quantity > 0 ? slot.Stack.ItemId : null,
                    quantity, selected && transferIndex == i && quantity > 0));
            }
            return occupied;
        }
    }
}
