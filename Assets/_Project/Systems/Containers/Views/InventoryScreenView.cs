using System;
using SOLITUDE.Application;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using TMPro;
using SOLITUDE.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SOLITUDE.Containers
{
    /// <summary>Authored inventory presentation. Domain container commands remain the owner of item changes.</summary>
    public sealed partial class InventoryScreenView : MonoBehaviour
    {
        [SerializeField] private ContainerView carriedView, hotbarView;
        [SerializeField] private TMP_Text capacity, itemTitle, metadata, description, context, equipped, backLabel;
        [SerializeField] private UnityEngine.UI.Image itemIcon;
        [SerializeField] private GameObject itemArtField;
        [SerializeField] private RectTransform itemPopover;
        [SerializeField] private UnityEngine.UI.Button closeButton, equipButton, assignButton, moveButton, useButton, splitButton, dropButton;
        [SerializeField] private UnityEngine.UI.Button[] filters;
        [SerializeField] private UnityEngine.UI.Image[] filterUnderlines;
        [SerializeField] private CanvasGroup hotbarInputGroup;
        [SerializeField] private bool distilledLayout;
        [SerializeField] private TMP_Text weaponName, replacement;
        [SerializeField] private UnityEngine.UI.Image weaponIcon, weaponResponse;
        [SerializeField] private RectTransform quickAccessDock;
        [SerializeField] private TMP_Text toolName, inHandHud, quickUseHud;
        private CarriedEquipment equipment;
        [SerializeField] private UnityEngine.UI.Button weaponCard, toolCard;
        private GameObject weaponCardMarker, toolCardMarker;
        private PlayerEquipmentSlot inspectedEquipment;
        [SerializeField] private TMP_Text weaponStatus, toolStatus;
        [SerializeField] private UnityEngine.UI.Image toolIcon;
        [SerializeField] private GameObject actionDivider;
        private GameObject[] equipmentKeycaps;
        private Transform hotbarHome;
        private RectTransform dockedHotbarTray;
        private Vector2 hotbarPosition;
        private Vector3 hotbarScale;
        private float responseStarted = -1, equipStarted = -1;
        [SerializeField] private GameObject[] quietWhileOpen = Array.Empty<GameObject>();
        private bool[] quietWasActive;
        private IContainerReader carried, hotbar;
        private ContainerCommandService commands;
        private ContainerGestureCoordinator gestures;
        private ItemPresentationCatalog catalog;
        private ItemCapabilityCatalog capabilities;
        private InputPolicyCoordinator policy;
        private UnityInputHost input;
        private HotbarSelectionState selection;
        private Action close;
        private IDisposable equipShortcut, assignShortcut, moveShortcut;
        private UnityEngine.UI.Button[] actionButtons;
        private TMP_Text[] actionLabels, filterLabels;
        private GameObject[] actionFocus, filterFocus;
        private readonly Vector3[] popoverCorners = new Vector3[4];
        private Vector2 descriptionPosition;
        private bool descriptionPositionCaptured;
        private int index, category;
        private bool inspectingHotbar, initialized, selectOnNextFrame;
        private SlotSnapshot source;
        private enum Handling { None, Move, Assign, Equip }
        private Handling handling;
        internal GameObject MenuFocusFallback { get; set; }
        public bool IsHandling => handling != Handling.None || pointerDrag;
        internal bool HandlesCarriedDrag => equipment != null;
        public void Initialize(IContainerReader carried, IContainerReader hotbar, ContainerCommandService commands,
            ItemPresentationCatalog catalog, ItemCapabilityCatalog capabilities, ContainerGestureCoordinator gestures,
            InputPolicyCoordinator policy, HotbarSelectionState selection, UnityInputHost input, Action close, CarriedEquipment equipment = null)
        {
            Release();
            actionButtons = new[] { closeButton, equipButton, useButton, assignButton, moveButton, splitButton, dropButton };
            ValidateAuthoring();
            actionLabels = new TMP_Text[actionButtons.Length]; actionFocus = new GameObject[actionButtons.Length];
            for (int i = 0; i < actionButtons.Length; i++)
            { actionLabels[i] = actionButtons[i].GetComponentInChildren<TMP_Text>(true); actionFocus[i] = actionButtons[i].transform.Find("FocusCorners")?.gameObject; }
            filterLabels = new TMP_Text[filters.Length]; filterFocus = new GameObject[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            { filterLabels[i] = filters[i].GetComponentInChildren<TMP_Text>(true); filterFocus[i] = filters[i].transform.Find("FocusCorners")?.gameObject; }
            if (!descriptionPositionCaptured) { descriptionPosition = description.rectTransform.anchoredPosition; descriptionPositionCaptured = true; }
            this.carried = carried; this.hotbar = hotbar; this.commands = commands; this.catalog = catalog;
            this.capabilities = capabilities; this.gestures = gestures; this.policy = policy; this.selection = selection;
            this.input = input; this.close = close; this.equipment = equipment;
            if (inventoryQuickUse != null) inventoryQuickUse.SetActive(equipment != null);
            if (equipment != null)
            {
                if (toolName == null || inHandHud == null || weaponCard == null || toolCard == null)
                    throw new InvalidOperationException("Carried equipment UI endpoints are incomplete.");
                equipmentKeycaps = new[]
                {
                    equipButton.transform.Find("Keycap").gameObject,
                    assignButton.transform.Find("Keycap").gameObject,
                    moveButton.transform.Find("Keycap").gameObject
                };
                weaponCardMarker = weaponCard.transform.Find("EquippedSelection").gameObject;
                toolCardMarker = toolCard.transform.Find("EquippedSelection").gameObject;
                weaponCard.onClick.AddListener(ClickWeaponCard);
                toolCard.onClick.AddListener(ClickToolCard);
                input.InputDeviceChanged += OnInputDeviceChanged;
                equipment.Changed += EquipmentUpdated;
                BindInventoryQuickUse();
                useButton.onClick.AddListener(Pin);
            }
            hotbarView.SetInspectionVisible(false);
            SetHotbarInput(false);
            carried.SlotChanged += Changed; hotbar.SlotChanged += Changed;
            carried.CapacityChanged += Rebuild; hotbar.CapacityChanged += Rebuild;
            selection.Changed += EquipmentChanged;
            carriedView.Focused += FocusCarried; carriedView.Selected += ClickCarried;
            hotbarView.Focused += FocusHotbar; hotbarView.Selected += ClickHotbar;
            BindSlotDrag();
            gestures.Interaction.HeldItemChanged += HeldChanged;
            closeButton.onClick.AddListener(Close); equipButton.onClick.AddListener(Equip);
            assignButton.onClick.AddListener(Assign); moveButton.onClick.AddListener(Move);
            for (int i = 0; i < filters.Length; i++) { int value = i; filters[i].onClick.AddListener(() => Filter(value)); }
            equipShortcut = input.BindInventoryCommand("Equip", () => ActivateShortcut(equipButton));
            assignShortcut = input.BindInventoryCommand("Assign", () => ActivateShortcut(assignButton));
            moveShortcut = input.BindInventoryCommand("Move", () => ActivateShortcut(moveButton));
            equipButton.transform.Find("Keycap/Glyph").GetComponent<TMP_Text>().text = input.BindingLabel("Container/Equip");
            assignButton.transform.Find("Keycap/Glyph").GetComponent<TMP_Text>().text = input.BindingLabel("Container/Assign");
            moveButton.transform.Find("Keycap/Glyph").GetComponent<TMP_Text>().text = input.BindingLabel("Container/Move");
            initialized = true;
            RefreshInputHints();
            EquipmentUpdated();
        }
        public void ValidateAuthoring()
        {
            if (carriedView == null || hotbarView == null || itemIcon == null || hotbarInputGroup == null ||
                capacity == null || itemTitle == null || metadata == null || description == null || context == null || equipped == null || backLabel == null ||
                closeButton == null || equipButton == null || useButton == null || assignButton == null || moveButton == null || splitButton == null || dropButton == null ||
                filters == null || filters.Length != 4 || filterUnderlines == null || filterUnderlines.Length != 4)
                throw new InvalidOperationException("Inventory screen authored endpoints are incomplete.");
            if (distilledLayout && (weaponName == null || weaponIcon == null || weaponResponse == null || replacement == null || quickAccessDock == null || itemPopover == null))
                throw new InvalidOperationException("Distilled inventory equipment/details endpoints are incomplete.");
            carriedView.ValidateAuthoring(); hotbarView.ValidateAuthoring();
            ValidateTransferAuthoring();
        }
        private void OnEnable()
        {
            if (!initialized) return;
            if (transferTarget != null) { QuietWorld(); UpdateEquipmentHud(); return; }
            SetHotbarInput(true);
            if (equipment == null && distilledLayout && quickAccessDock != null)
            {
                var tray = (RectTransform)hotbarView.transform.Find("Tray");
                dockedHotbarTray = tray;
                hotbarHome = tray.parent; hotbarPosition = tray.anchoredPosition; hotbarScale = tray.localScale;
                tray.SetParent(quickAccessDock, false); tray.anchoredPosition = Vector2.zero; tray.localScale = Vector3.one * 1.25f;
            }
            hotbarView.SetInspectionVisible(equipment == null);
            UpdateEquipmentHud();
            QuietWorld();
            handling = Handling.None; context.text = string.Empty;
            Filter(category);
            if (equipment != null && Current()?.IsEmpty != false)
            {
                inspectedEquipment = equipment.State.activeSlot;
                Refresh();
            }
            selectOnNextFrame = true;
        }
        private void QuietWorld()
        {
            if (quietWasActive != null) return;
            quietWasActive = new bool[quietWhileOpen.Length];
            for (int i = 0; i < quietWhileOpen.Length; i++)
                if (quietWhileOpen[i] != null) { quietWasActive[i] = quietWhileOpen[i].activeSelf; quietWhileOpen[i].SetActive(false); }
        }
        private void LateUpdate()
        {
            if (!initialized || !policy.Pointer) return;
            if (transferTarget != null) { UpdateTransfer(); return; }
            if (distilledLayout)
            {
                float pulse = responseStarted < 0 ? 0 : Mathf.Clamp01(1 - (Time.unscaledTime - responseStarted) / .3f);
                if (weaponResponse != null) weaponResponse.color = new Color(.91f,.92f,.90f,pulse * .20f);
                float settle = equipStarted < 0 ? 0 : Mathf.Clamp01(1 - (Time.unscaledTime - equipStarted) / .22f);
                if (weaponIcon != null) weaponIcon.rectTransform.localScale = Vector3.one * (1 + .10f * settle * settle);
            }
            var focused = EventSystem.current?.currentSelectedGameObject;
            if (equipment != null && handling == Handling.None && !selectOnNextFrame)
            {
                if (focused == weaponCard.gameObject && weaponCard.interactable) InspectEquipment(PlayerEquipmentSlot.Weapon);
                if (focused == toolCard.gameObject && toolCard.interactable) InspectEquipment(PlayerEquipmentSlot.Tool);
            }
            Color amber = equipment != null ? (Color)ShipboardUiColors.Amber : new Color(.937f, .733f, .431f);
            for (int i = 0; i < actionButtons.Length; i++)
            {
                var button = actionButtons[i];
                bool lit = button.interactable && focused == button.gameObject;
                bool primary = button == equipButton || button == useButton;
                if (equipment != null)
                {
                    button.image.color = lit ? amber : button == closeButton ? Color.clear : new Color32(35, 53, 62, 255);
                    actionLabels[i].color = lit ? ShipboardUiColors.PanelInk : new Color32(220, 229, 226, 255);
                }
                else
                {
                    button.image.color = lit || (distilledLayout && primary && button.interactable) ? amber : primary && !distilledLayout ? new Color(.20f,.235f,.225f) : Color.clear;
                    actionLabels[i].color = !button.interactable ? new Color(.48f,.54f,.50f) : lit || (distilledLayout && primary) ? new Color(.086f,.125f,.102f) : primary ? amber : new Color(.906f,.925f,.902f);
                }
                if (actionFocus[i] != null) actionFocus[i].SetActive(lit);
            }
            for (int i = 0; i < filters.Length; i++)
            {
                bool lit = filters[i].interactable && focused == filters[i].gameObject;
                filters[i].image.color = lit ? amber : i == category ? new Color(.19f,.23f,.24f) : Color.clear;
                filterLabels[i].color = lit ? new Color(.086f,.125f,.102f) : i == category ? amber : new Color(.663f,.718f,.702f);
                if (filterFocus[i] != null) filterFocus[i].SetActive(lit);
            }
            PositionPopover();
            if (!selectOnNextFrame) return;
            selectOnNextFrame = false;
            var views = inspectingHotbar ? hotbarView.SlotViews : carriedView.SlotViews;
            if (inspectedEquipment != PlayerEquipmentSlot.None)
                EventSystem.current?.SetSelectedGameObject((inspectedEquipment == PlayerEquipmentSlot.Weapon ? weaponCard : toolCard).gameObject);
            else if (index >= 0 && index < views.Count && views[index].GetComponent<UnityEngine.UI.Button>().interactable)
                EventSystem.current?.SetSelectedGameObject(views[index].gameObject);
            else EventSystem.current?.SetSelectedGameObject(MenuFocusFallback != null ? MenuFocusFallback :
                equipment == null ? filters[category].gameObject : closeButton.gameObject);
        }
        private void PositionPopover()
        {
            if (distilledLayout) return;
            if (itemPopover == null || !itemPopover.gameObject.activeSelf) return;
            var slots = inspectingHotbar ? hotbarView.SlotViews : carriedView.SlotViews;
            if (index < 0 || index >= slots.Count) return;
            var canvasRect = (RectTransform)transform;
            var corners = popoverCorners;
            ((RectTransform)slots[index].transform).GetWorldCorners(corners);
            var top = (Vector2)canvasRect.InverseTransformPoint(corners[2]);
            float x = top.x - canvasRect.rect.xMin + 18;
            float y = top.y - canvasRect.rect.yMax;
            if (!inspectingHotbar)
            {
                ((RectTransform)carriedView.transform.Find("Panel")).GetWorldCorners(corners);
                x = canvasRect.InverseTransformPoint(corners[2]).x - canvasRect.rect.xMin - 18;
            }
            x = Mathf.Clamp(x,48,canvasRect.rect.width-itemPopover.rect.width-48);
            y = Mathf.Clamp(y,-canvasRect.rect.height+itemPopover.rect.height+144,-96);
            itemPopover.anchoredPosition = new Vector2(x,y);
        }
        private void OnDisable()
        {
            if (!initialized) return;
            CancelHandling();
            if (distilledLayout && hotbarHome != null)
            {
                var tray = dockedHotbarTray;
                tray.SetParent(hotbarHome, false); tray.anchoredPosition = hotbarPosition; tray.localScale = hotbarScale;
                hotbarHome = null; dockedHotbarTray = null;
            }
            SetHotbarInput(false);
            hotbarView.SetInspectionVisible(false);
            for (int i = 0; i < hotbarView.SlotViews.Count; i++) Render(hotbarView.SlotViews[i], hotbar.GetSlot(i), false, selection.ActiveIndex == i);
            if (quietWasActive != null) for (int i = 0; i < quietWhileOpen.Length; i++)
                if (quietWhileOpen[i] != null) quietWhileOpen[i].SetActive(quietWasActive[i]);
            quietWasActive = null;
            UpdateEquipmentHud();
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && (selected.transform.IsChildOf(transform) || selected.transform.IsChildOf(hotbarView.transform))) EventSystem.current.SetSelectedGameObject(null);
        }
        private void SetHotbarInput(bool enabled)
        { if (hotbarInputGroup != null) { hotbarInputGroup.interactable = enabled; hotbarInputGroup.blocksRaycasts = enabled; } }
        private void ActivateShortcut(UnityEngine.UI.Button button)
        {
            if (!initialized || !isActiveAndEnabled || IsHandling || !button.gameObject.activeInHierarchy || !button.IsInteractable()) return;
            input.ConsumeUIActivation(); button.onClick.Invoke();
        }
        private void Close() { input?.ConsumeUIActivation(); if (!CancelHandling()) close?.Invoke(); }
        public bool CancelHandling()
        {
            if (pointerDrag) { EndPointerDrag(); return true; }
            if (CloseTransferPopover()) return true;
            if (handling == Handling.None) return false;
            handling = Handling.None; source = null; gestures?.Cancel();
            if (initialized) { context.text = string.Empty; Filter(category); Refresh(); }
            return true;
        }
        private void HeldChanged(ItemStack stack)
        {
            if (stack == null && pointerDrag)
            {
                pointerDrag = false;
                if (transferTarget != null) transferReceipt = null;
                else context.text = string.Empty;
            }
            if (!isActiveAndEnabled) return;
            if (transferTarget != null) { RenderTransfer(); return; }
            if (stack == null && handling != Handling.None) { handling = Handling.None; source = null; context.text = string.Empty; Filter(category); }
            Refresh();
        }
        private void FocusCarried(SlotIntent intent)
        {
            if (transferTarget != null) InspectTransfer(false, intent);
            else Inspect(false, intent.Index);
        }
        private void FocusHotbar(SlotIntent intent) => Inspect(true, intent.Index);
        private void ClickCarried(SlotIntent intent)
        {
            if (transferTarget != null) SelectTransfer(false, intent);
            else Click(false, intent.Index);
        }
        private void ClickHotbar(SlotIntent intent) => Click(true, intent.Index);
        private void Inspect(bool bar, int value)
        {
            if (!isActiveAndEnabled || !initialized || !policy.Pointer || handling != Handling.None) return;
            bool changed = inspectedEquipment != PlayerEquipmentSlot.None || inspectingHotbar != bar || index != value;
            inspectedEquipment = PlayerEquipmentSlot.None;
            if (changed) context.text = string.Empty;
            inspectingHotbar = bar; index = value; Refresh();
            if (distilledLayout && changed && Current()?.Stack != null && Category(Current().Stack.ItemId) == 1)
                responseStarted = Time.unscaledTime;
        }
        private void ClickWeaponCard() => ClickEquipmentCard(PlayerEquipmentSlot.Weapon);
        private void ClickToolCard() => ClickEquipmentCard(PlayerEquipmentSlot.Tool);

        private void InspectEquipment(PlayerEquipmentSlot slot)
        {
            if (inspectedEquipment == slot) return;
            inspectedEquipment = slot;
            inspectingHotbar = false;
            context.text = string.Empty;
            Refresh();
        }

        private void ClickEquipmentCard(PlayerEquipmentSlot slot)
        {
            if (!isActiveAndEnabled || !policy.Pointer || IsHandling) return;
            if (transferTarget != null)
            {
                FocusTransferEquipment(slot);
                OpenTransferPopover();
                return;
            }
            InspectEquipment(slot);
            var action = slot == PlayerEquipmentSlot.Weapon ? equipButton : assignButton;
            if (action.gameObject.activeInHierarchy && action.IsInteractable())
                EventSystem.current?.SetSelectedGameObject(action.gameObject);
        }

        private void Click(bool bar, int value)
        {
            if (!isActiveAndEnabled || !initialized || !policy.Pointer) return;
            if (handling == Handling.None)
            {
                Inspect(bar, value);
                if (Current()?.IsEmpty == false)
                {
                    var first = equipButton.gameObject.activeInHierarchy ? equipButton : assignButton;
                    if (equipment != null)
                    {
                        first = equipButton.gameObject.activeInHierarchy ? equipButton :
                            assignButton.gameObject.activeInHierarchy ? assignButton :
                            useButton.gameObject.activeInHierarchy ? useButton : moveButton;
                    }
                    if (first.IsInteractable()) EventSystem.current?.SetSelectedGameObject(first.gameObject);
                }
                return;
            }
            if (!bar && handling != Handling.Move) { context.text = "Choose a hotbar slot. Back cancels."; return; }
            var target = (bar ? hotbar : carried).GetSlot(value);
            if (handling == Handling.Move && equipment != null && !bar && !CanPlaceInCarried(target))
            {
                context.text = "Choose a highlighted Carried slot.";
                return;
            }
            var previous = handling; var from = source;
            handling = Handling.None; source = null; gestures.Cancel();
            var result = from == null || target == null ? default : commands.Transfer(from.Address, target.Address);
            if (result.Succeeded && previous == Handling.Equip && bar) selection.Select(value);
            inspectingHotbar = bar; index = value; Filter(category); Refresh();
            context.text = result.Succeeded ? previous == Handling.Equip ? "Tool equipped." : "Item placed." : "Placement rejected. Items remain unchanged.";
        }
        private void Move() => Begin(Handling.Move);
        private void Assign()
        {
            if (equipment == null) Begin(Handling.Assign);
            else ToggleEquipment(PlayerEquipmentSlot.Tool);
        }

        private void ToggleEquipment(PlayerEquipmentSlot slot)
        {
            var item = Current();
            if (item?.Stack == null || IsHandling || !policy.Pointer) return;
            bool unequipping = inspectedEquipment == slot;
            var result = unequipping ? equipment.Unequip(slot) : equipment.Equip(item.Address, slot);
            if (result.Moved == 0)
            {
                context.text = result.Status == CommandStatus.InsufficientSpace ? "Carried is full." : "Equipment change unavailable.";
                return;
            }

            inspectedEquipment = unequipping ? PlayerEquipmentSlot.None : slot;
            if (unequipping)
                foreach (var address in result.Addresses)
                    if (address.Container.Equals(carried.Handle) && carried.GetSlot(address.Index)?.Stack?.ItemId == item.Stack.ItemId)
                    { index = address.Index; break; }
            context.text = "";
            Refresh();
            selectOnNextFrame = true;
        }

        private void Pin()
        {
            var id = Current()?.Stack?.ItemId;
            if (id == null || IsHandling || !policy.Pointer) return;
            equipment.Pin(equipment.SelectedQuickUseItem == id ? null : id);
        }
        private void Equip()
        {
            if (equipment != null)
            {
                ToggleEquipment(PlayerEquipmentSlot.Weapon);
                return;
            }
            if (distilledLayout)
            {
                if (Current()?.IsEmpty != false || Category(Current().Stack.ItemId) != 1) return;
                if (inspectingHotbar) { selection.Select(index); equipStarted = Time.unscaledTime; Refresh(); return; }
                int destination = selection.ActiveIndex;
                if (destination < 0 || destination >= hotbar.Capacity) destination = 0;
                var target = hotbar.GetSlot(destination);
                if (target == null) { context.text = "No quick-access slot available."; return; }
                var result = commands.Transfer(Current().Address, target.Address);
                if (result.Succeeded) { selection.Select(destination); equipStarted = Time.unscaledTime; }
                Refresh();
                context.text = result.Succeeded ? "Tool equipped." : "Equip unavailable. Items unchanged.";
                return;
            }
            if (inspectingHotbar && Current()?.IsEmpty == false) { selection.Select(index); Refresh(); context.text = "Tool equipped."; }
            else Begin(Handling.Equip);
        }
        private void Begin(Handling value)
        {
            if (!isActiveAndEnabled || !policy.Pointer || IsHandling || Current()?.IsEmpty != false) return;
            source = Current(); handling = value; category = 0;
            gestures.Begin("InventoryScreen", source);
            if (!gestures.Interaction.IsHolding) { handling = Handling.None; source = null; return; }
            Filter(0); Refresh();
            var item = catalog.Resolve(source.Stack.ItemId);
            context.text = item.DisplayName + " ×" + source.Stack.Quantity + " · " + (value == Handling.Move ? "Choose a destination; Back cancels." : "Choose a hotbar slot; Back cancels.");
            if (value == Handling.Move && equipment != null)
            {
                context.text = "Choose a highlighted Carried slot.";
                selectOnNextFrame = true;
            }
            else if (value != Handling.Move && hotbarView.SlotViews.Count > 0)
                EventSystem.current?.SetSelectedGameObject(hotbarView.SlotViews[0].gameObject);
        }
        private SlotSnapshot Current()
        {
            if (inspectingHotbar) return hotbar?.GetSlot(index);
            if (inspectedEquipment != PlayerEquipmentSlot.None) return equipment?.GetSlot(inspectedEquipment);
            return carried?.GetSlot(index);
        }
        private int Category(string id) => catalog.Consumables?.Resolve(id)?.Supported == true ? 2 : capabilities?.Resolve(id)?.Actions.Count > 0 ? 1 : 3;
        private void Filter(int value)
        {
            if (category != value && handling == Handling.None) context.text = string.Empty;
            category = value;
            for (int i = 0; i < filters.Length; i++) { filterUnderlines[i].enabled = i == value; filters[i].interactable = handling == Handling.None; }
            if (equipment == null)
                for (int i = 0; i < carriedView.SlotViews.Count; i++)
                {
                    var stack = carried.GetSlot(i)?.Stack;
                    carriedView.SlotViews[i].SetEligible(handling != Handling.None || value == 0 || stack == null || Category(stack.ItemId) == value);
                }
            if (handling == Handling.None && inspectedEquipment == PlayerEquipmentSlot.None && Current()?.Stack != null && value != 0 && Category(Current().Stack.ItemId) != value)
            { inspectingHotbar = false; index = -1; }
            Refresh();
        }
        private void Changed(int _)
        {
            UpdateEquipmentHud();
            if (transferTarget != null) { transferDirty = true; return; }
            if (isActiveAndEnabled) Filter(category);
        }
        private void EquipmentUpdated()
        {
            UpdateEquipmentHud();
            if (transferTarget != null) { transferDirty = true; return; }
            if (isActiveAndEnabled) Refresh();
        }

        private string ItemName(string id) => id == null ? "Empty" : catalog.Resolve(id)?.DisplayName ?? id;

        public void UpdateEquipmentHud()
        {
            if (!initialized || equipment == null) return;
            var state = equipment.State;
            bool exploration = !gameObject.activeInHierarchy;
            var quickItem = state.QuickUseItem(equipment.SelectedQuickUse);
            string switchHint = state.weaponItemId != null && state.toolItemId != null
                ? "   " + input.DeviceBindingLabel("Gameplay/SwitchEquipment") : "";
            if (inHandHud != null)
            {
                inHandHud.gameObject.SetActive(exploration && state.ActiveItemId != null);
                inHandHud.text = "In hand · " + ItemName(state.ActiveItemId) + switchHint;
            }
            if (quickUseHud != null)
            {
                quickUseHud.gameObject.SetActive(exploration && quickItem != null);
                quickUseHud.text = ItemName(quickItem) + " ×" + equipment.Quantity(quickItem)
                    + "   " + input.DeviceBindingLabel("Gameplay/Use");
            }
        }
        private void OnInputDeviceChanged()
        {
            if (transferTarget != null) transferDirty = true;
            else RefreshInputHints();
            UpdateEquipmentHud();
        }

        private void RefreshInputHints()
        {
            if (equipment == null) return;
            backLabel.text = input.DeviceBindingLabel("Container/Cancel") + (IsHandling ? "  Cancel move" : "  Close");
            foreach (var cap in equipmentKeycaps) cap.SetActive(!input.LastInputWasGamepad);
        }

        private void ShowEquipment(TMP_Text name, UnityEngine.UI.Image icon, TMP_Text status, string id, string state)
        {
            name.text = ItemName(id);
            name.color = id == null ? new Color(.663f, .718f, .702f) : new Color(.906f, .925f, .902f);
            if (icon != null)
            {
                icon.sprite = id == null ? null : catalog.Resolve(id)?.Icon;
                icon.enabled = icon.sprite != null;
            }
            if (status != null) status.text = state;
        }

        private void Rebuild()
        {
            EndPointerDrag();
            if (transferTarget != null) { transferRebuild = true; return; }
            if (isActiveAndEnabled) { Filter(category); Refresh(); selectOnNextFrame = true; }
        }
        private void EquipmentChanged(int _) { if (isActiveAndEnabled) Refresh(); }
        private int RefreshCarriedCapacity()
        {
            int occupied = 0, overflow = 0;
            int normalCapacity = equipment?.NormalCapacity ?? carried.Capacity;
            for (int i = 0; i < carried.Capacity; i++)
            {
                if (carried.GetSlot(i)?.Stack == null) continue;
                if (i < normalCapacity) occupied++;
                else overflow++;
            }
            capacity.text = (occupied + " / " + normalCapacity) +
                (overflow > 0 ? "  +" + overflow + " overflow" : "");
            return occupied + overflow;
        }

        private void Refresh()
        {
            if (!initialized || transferTarget != null) return;
            var state = equipment?.State;
            int occupied = RefreshCarriedCapacity();
            if (inspectedEquipment != PlayerEquipmentSlot.None && state?.ItemIn(inspectedEquipment) == null)
                inspectedEquipment = PlayerEquipmentSlot.None;
            if (equipment == null)
                backLabel.text = input.BindingLabel("Container/Cancel") + (distilledLayout ? "  Close" : " / B");
            var active = hotbar.GetSlot(selection.ActiveIndex)?.Stack;
            equipped.text = active == null ? "No tool equipped" : "Equipped: " + catalog.Resolve(active.ItemId)?.DisplayName + " · Slot " + (selection.ActiveIndex + 1);
            var stack = (source ?? Current())?.Stack;
            var item = catalog.Resolve(stack?.ItemId);
            if (itemPopover != null)
            {
                itemPopover.gameObject.SetActive(distilledLayout || stack != null && handling == Handling.None);
                if (!distilledLayout) itemPopover.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, stack != null && Category(stack.ItemId) == 1 ? 428 : 380);
            }
            itemTitle.text = item?.DisplayName ?? (occupied == 0 ? "No items carried" : "Select an item");
            itemIcon.enabled = item?.Icon != null; itemIcon.sprite = item?.Icon;
            if (itemArtField != null) itemArtField.SetActive(itemIcon.enabled);
            metadata.text = stack != null && stack.MaxStack > 1 ? "Stack " + stack.Quantity + " / " + stack.MaxStack : string.Empty;
            metadata.gameObject.SetActive(metadata.text.Length != 0);
            description.rectTransform.anchoredPosition = descriptionPosition + (metadata.text.Length == 0 ? new Vector2(0,28) : Vector2.zero);
            description.text = item?.Description ?? string.Empty;
            bool has = stack != null && !IsHandling;
            equipButton.gameObject.SetActive(has && Category(stack.ItemId) == 1);
            useButton.gameObject.SetActive(false);
            useButton.interactable = false; splitButton.gameObject.SetActive(false); splitButton.interactable = false;
            assignButton.gameObject.SetActive(has); moveButton.gameObject.SetActive(has); dropButton.gameObject.SetActive(false); dropButton.interactable = false;
            if (distilledLayout)
            {
                metadata.gameObject.SetActive(false);
                description.rectTransform.anchoredPosition = descriptionPosition;
                assignButton.gameObject.SetActive(false); moveButton.gameObject.SetActive(false);
                dropButton.gameObject.SetActive(has); dropButton.interactable = false;
                bool activeTool = active != null && Category(active.ItemId) == 1;
                if (weaponName != null) weaponName.text = activeTool ? catalog.Resolve(active.ItemId)?.DisplayName : "No tool selected";
                if (weaponIcon != null) { weaponIcon.sprite = activeTool ? catalog.Resolve(active.ItemId)?.Icon : null; weaponIcon.enabled = weaponIcon.sprite != null; }
                if (replacement != null) replacement.text = has && Category(stack.ItemId) == 1
                    ? inspectingHotbar && index == selection.ActiveIndex ? "Currently in hand" : active == null ? "Equip to quick access" : "Replaces " + catalog.Resolve(active.ItemId)?.DisplayName
                    : string.Empty;
            }
            if (equipment != null)
            {
                var id = stack?.ItemId;
                bool inspectingEquipment = inspectedEquipment != PlayerEquipmentSlot.None;
                int quantity = stack?.Quantity ?? 0;
                bool weapon = has && equipment.CanEquip(id, PlayerEquipmentSlot.Weapon);
                bool tool = has && equipment.CanEquip(id, PlayerEquipmentSlot.Tool);
                int quickSlot = equipment.SelectedQuickUse;
                bool pinned = has && id == state.QuickUseItem(quickSlot);
                equipButton.gameObject.SetActive(weapon && (inspectingEquipment || id != state.weaponItemId));
                assignButton.gameObject.SetActive(tool && (inspectingEquipment || id != state.toolItemId));
                moveButton.gameObject.SetActive(has && !inspectingEquipment);
                weaponCard.interactable = state.weaponItemId != null && handling == Handling.None;
                toolCard.interactable = state.toolItemId != null && handling == Handling.None;
                weaponCardMarker.SetActive(inspectedEquipment == PlayerEquipmentSlot.Weapon);
                toolCardMarker.SetActive(inspectedEquipment == PlayerEquipmentSlot.Tool);
                useButton.gameObject.SetActive(has && !inspectingEquipment && (pinned || equipment.CanPin(id)));
                useButton.interactable = true;
                dropButton.gameObject.SetActive(false);
                actionLabels[1].text = inspectedEquipment == PlayerEquipmentSlot.Weapon ? "Unequip weapon" : "Equip as weapon";
                actionLabels[2].text = pinned ? $"Clear Quick Use {quickSlot + 1}" : $"Assign to Quick Use {quickSlot + 1}";
                actionLabels[3].text = inspectedEquipment == PlayerEquipmentSlot.Tool ? "Unequip tool" : "Equip as tool";
                ShowEquipment(weaponName, weaponIcon, weaponStatus, state.weaponItemId,
                    state.activeSlot == PlayerEquipmentSlot.Weapon && state.weaponItemId != null ? "In hand" : string.Empty);
                ShowEquipment(toolName, toolIcon, toolStatus, state.toolItemId,
                    state.activeSlot == PlayerEquipmentSlot.Tool && state.toolItemId != null ? "In hand" : string.Empty);
                RefreshInventoryQuickUse(state);
                RefreshEquipmentDropTargets();
                replacement.text = string.Empty;
                metadata.gameObject.SetActive(stack != null);
                metadata.text = stack == null ? string.Empty : state.HasQuickUseItem(stack.ItemId)
                    ? "Pinned for quick use  ·  " + quantity + " carried"
                    : !inspectingEquipment && stack.MaxStack > 1 ? quantity + " / " + stack.MaxStack + " in stack"
                    : weapon && tool ? "Weapon / Tool" : weapon ? "Weapon" : tool ? "Tool" : string.Empty;
                RefreshInputHints();
                if (actionDivider != null) actionDivider.SetActive(has);
            }
            for (int i = 0; i < carriedView.SlotViews.Count; i++)
            {
                var view = carriedView.SlotViews[i];
                var slot = carried.GetSlot(i);
                bool selected = inspectedEquipment == PlayerEquipmentSlot.None && !inspectingHotbar && index == i;
                if (equipment == null) Render(view, slot, selected, false);
                else
                {
                    int quantity = slot?.Stack?.Quantity ?? 0;
                    bool matchesFilter = IsHandling || category == 0 || quantity == 0 || Category(slot.Stack.ItemId) == category;
                    view.SetEligible(matchesFilter);
                    view.SetDropDestination((pointerDrag || handling == Handling.Move) && CanPlaceInCarried(slot));
                    view.Render(new SlotPresentation(view.Index, quantity > 0 ? slot.Stack.ItemId : null,
                        quantity, selected && quantity > 0));
                }
            }
            for (int i = 0; i < hotbarView.SlotViews.Count; i++) Render(hotbarView.SlotViews[i], hotbar.GetSlot(i), inspectingHotbar && index == i, selection.ActiveIndex == i);
        }
        private void Render(ContainerSlotView view, SlotSnapshot slot, bool selected, bool active, bool isEquipped = false)
        { view.Render(new SlotPresentation(view.Index, slot?.Stack?.ItemId, slot?.Stack?.Quantity ?? 0, selected, active), isEquipped); }
        public void Release()
        {
            if (!initialized) return;
            EndTransfer();
            CancelHandling();
            UnbindSlotDrag();
            if (equipment != null)
            {
                weaponCard.onClick.RemoveListener(ClickWeaponCard);
                toolCard.onClick.RemoveListener(ClickToolCard);
                inspectedEquipment = PlayerEquipmentSlot.None;
                equipment.Changed -= EquipmentUpdated;
                UnbindInventoryQuickUse();
                input.InputDeviceChanged -= OnInputDeviceChanged;
                useButton.onClick.RemoveListener(Pin);
                if (inHandHud != null) inHandHud.gameObject.SetActive(false);
                if (quickUseHud != null) quickUseHud.gameObject.SetActive(false);
                equipment = null;
            }
            equipShortcut?.Dispose(); assignShortcut?.Dispose(); moveShortcut?.Dispose();
            equipShortcut = assignShortcut = moveShortcut = null;
            carried.SlotChanged -= Changed; hotbar.SlotChanged -= Changed;
            carried.CapacityChanged -= Rebuild; hotbar.CapacityChanged -= Rebuild; selection.Changed -= EquipmentChanged;
            carriedView.Focused -= FocusCarried; carriedView.Selected -= ClickCarried; hotbarView.Focused -= FocusHotbar; hotbarView.Selected -= ClickHotbar;
            gestures.Interaction.HeldItemChanged -= HeldChanged;
            closeButton.onClick.RemoveListener(Close); equipButton.onClick.RemoveListener(Equip); assignButton.onClick.RemoveListener(Assign); moveButton.onClick.RemoveListener(Move);
            foreach (var filter in filters) filter.onClick.RemoveAllListeners();
            initialized = false;
        }
        private void OnDestroy() => Release();
    }
}
