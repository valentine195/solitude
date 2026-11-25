using System;
using SOLITUDE.Application;
using SOLITUDE.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
namespace SOLITUDE.Containers
{
    public class ContainerSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public int Index { get; private set; }
        private long generation;
        private ItemPresentationCatalog presentations;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private TextMeshProUGUI count;
        [SerializeField] private GameObject selectionHighlight;
        public event Action<SlotIntent> Hovered, Unhovered, Clicked, BeginDrag, EndDrag, Dropped;
        public void Bind(int index, long generation, ItemPresentationCatalog presentations)
        { Index = index; this.generation = generation; this.presentations = presentations; }
        public void Unbind() { generation = 0; presentations = null; Render(new SlotPresentation(Index, null, 0)); }
        public void Render(SlotPresentation slot)
        {
            if (icon != null) { icon.enabled = slot.ItemId != null; icon.sprite = presentations?.Resolve(slot.ItemId)?.Icon; }
            if (count != null) count.text = slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
            if (selectionHighlight != null) selectionHighlight.SetActive(slot.Selected);
            GetComponent<SOLITUDE.Hotbar.HotbarSlotDecorator>()?.Render(slot.Index, slot.Active);
        }
        private SlotIntent Intent => new SlotIntent(Index, generation);
        public void OnPointerEnter(PointerEventData _) { if (generation != 0) Hovered?.Invoke(Intent); }
        public void OnPointerExit(PointerEventData _) { if (generation != 0) Unhovered?.Invoke(Intent); }
        public void OnPointerClick(PointerEventData _) { if (generation != 0) Clicked?.Invoke(Intent); }
        public void OnBeginDrag(PointerEventData _) { if (generation != 0) BeginDrag?.Invoke(Intent); }
        public void OnDrag(PointerEventData _) { }
        public void OnEndDrag(PointerEventData _) { if (generation != 0) EndDrag?.Invoke(Intent); }
        public void OnDrop(PointerEventData _) { if (generation != 0) Dropped?.Invoke(Intent); }
    }
}
