using System;
using SOLITUDE.Application;
using SOLITUDE.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
namespace SOLITUDE.Containers
{
    public class ContainerSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public int Index { get; private set; }
        private long generation;
        private ItemPresentationCatalog presentations;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TextMeshProUGUI count;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject navigationFocus;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private UnityEngine.UI.Image surface;
        [SerializeField] private GameObject dropDestination, equippedLabel;
        [SerializeField] private RectTransform equippedIconFrame;
        private Vector2 defaultIconPosition, defaultIconSize;
        private bool iconLayoutCaptured;
        private bool eligible = true, isDestination;

        internal void SetDropDestination(bool value)
        {
            isDestination = value;
            if (dropDestination != null) dropDestination.SetActive(value);
        }
        public event Action<SlotIntent> Focused;
        public void SetEligible(bool value, float disabledOpacity = .15f)
        {
            eligible = value;
            var group = GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = value ? 1 : disabledOpacity; group.blocksRaycasts = value; }
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = value;
        }
        public void OnSelect(BaseEventData _)
        {
            if (navigationFocus != null) navigationFocus.SetActive(generation != 0 && eligible);
            if (generation != 0 && eligible) Focused?.Invoke(Intent);
        }
        public void OnDeselect(BaseEventData _) => ClearFocus();
        private void OnDisable() => ClearFocus();
        private void ClearFocus()
        {
            if (navigationFocus != null) navigationFocus.SetActive(false);
        }
        public void OnSubmit(BaseEventData _) { if (generation != 0 && eligible) Clicked?.Invoke(Intent); }
        public event Action<SlotIntent> Hovered, Unhovered, Clicked, BeginDrag, EndDrag, Dropped;
        public void Bind(int index, long generation, ItemPresentationCatalog presentations)
        { Index = index; this.generation = generation; this.presentations = presentations; }
        public void Unbind() { generation = 0; presentations = null; SetDropDestination(false); Render(new SlotPresentation(Index, null, 0)); }
        public void Render(SlotPresentation slot, bool isEquipped = false)
        {
            if (equippedLabel != null && equippedLabel.activeSelf != isEquipped)
            {
                equippedLabel.SetActive(isEquipped);
                if (equippedIconFrame != null && icon != null)
                {
                    var frame = icon.rectTransform;
                    if (!iconLayoutCaptured)
                    {
                        defaultIconPosition = frame.anchoredPosition;
                        defaultIconSize = frame.sizeDelta;
                        iconLayoutCaptured = true;
                    }
                    frame.anchoredPosition = isEquipped ? equippedIconFrame.anchoredPosition : defaultIconPosition;
                    frame.sizeDelta = isEquipped ? equippedIconFrame.sizeDelta : defaultIconSize;
                }
            }
            if (icon != null) { icon.sprite = presentations?.Resolve(slot.ItemId)?.Icon; icon.enabled = icon.sprite != null; }
            if (itemName != null) itemName.text = presentations?.Resolve(slot.ItemId)?.DisplayName ?? string.Empty;
            if (surface != null) surface.color = slot.Selected && slot.ItemId != null ? new Color(.30f,.265f,.17f) :
                isDestination ? new Color32(25, 47, 57, 255) :
                slot.ItemId == null ? new Color(.025f,.045f,.055f,.38f) : new Color(.105f,.155f,.17f);
            if (count != null) count.text = slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
            if (selectionHighlight != null) selectionHighlight.SetActive(slot.Selected);
            if (navigationFocus != null) navigationFocus.SetActive(generation != 0 && eligible && EventSystem.current?.currentSelectedGameObject == gameObject);
            GetComponent<SOLITUDE.Hotbar.HotbarSlotDecorator>()?.Render(slot.Index, slot.Active);
        }
        private SlotIntent Intent => new SlotIntent(Index, generation);
        public void OnPointerEnter(PointerEventData _) { if (generation != 0 && eligible) Hovered?.Invoke(Intent); }
        public void OnPointerExit(PointerEventData _) { if (generation != 0 && eligible) Unhovered?.Invoke(Intent); }
        public void OnPointerClick(PointerEventData _) { if (generation != 0 && eligible) Clicked?.Invoke(Intent); }
        public void OnBeginDrag(PointerEventData pointer)
        {
            if (pointer.button != PointerEventData.InputButton.Left || generation == 0 || !eligible) return;
            pointer.eligibleForClick = false;
            BeginDrag?.Invoke(Intent);
        }
        public void OnDrag(PointerEventData _) { }
        public void OnEndDrag(PointerEventData pointer) { if (pointer.button == PointerEventData.InputButton.Left && generation != 0) EndDrag?.Invoke(Intent); }
        public void OnDrop(PointerEventData pointer) { if (pointer.button == PointerEventData.InputButton.Left && generation != 0 && eligible) Dropped?.Invoke(Intent); }
    }
}
