using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;
using System;

namespace SOLITUDE.Containers
{
    public class ContainerSlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public int Index { get; private set; }

        // Resolves this view's own domain slot on demand. A closure rather than
        // a stored IContainer/ContainerView reference, so this view stays
        // decoupled from any particular owner type - it just knows how to ask
        // for "my slot" when a drag actually needs one.
        private Func<ContainerSlot> resolveSlot;

        // Index-only - the view layer never passes a ContainerSlot/domain
        // reference across the click/hover boundary. A click only toggles
        // selection; moving an item is a drag gesture (see below), handled
        // separately because it can span two different containers.
        public event Action<int> Hovered;
        public event Action<int> Unhovered;
        public event Action<int> Clicked;

        // Fired once, at the end of Bind(), with this slot's real index.
        // ContainerView instantiates one shared prefab per slot, so any
        // sibling component that needs to know "which slot am I" (e.g. a
        // hotbar number label/active-highlight decorator) can't rely on a
        // serialized field baked into the prefab - every clone would report
        // the same value. Subscribing to this in Awake (before Bind runs)
        // is the reliable way to learn it instead.
        public event Action<int> Bound;

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI count;

        [Tooltip("Enabled while this slot is the selected slot (set via SetSelected).")]
        [SerializeField] private GameObject selectionHighlight;

        public void Bind(int index, Func<ContainerSlot> resolveSlot)
        {
            Index = index;
            this.resolveSlot = resolveSlot;
            Bound?.Invoke(index);
        }

        /// <summary>Updates the visuals for this slot. Safe to call with an empty slot.</summary>
        public void Refresh(ContainerSlot slot)
        {
            bool hasItem = slot != null && !slot.IsEmpty;

            if (icon != null)
            {
                icon.enabled = hasItem;
                icon.sprite = hasItem ? slot.Definition.Icon : null;
            }

            if (count != null)
            {
                count.text = hasItem && slot.Stack.Quantity > 1
                    ? slot.Stack.Quantity.ToString()
                    : string.Empty;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }

        // --------------------------------------------------
        // Hover / click - purely local UI concerns, routed through
        // ContainerView/ContainerController as before.
        // --------------------------------------------------
        public void OnPointerEnter(PointerEventData eventData)
            => Hovered?.Invoke(Index);

        public void OnPointerExit(PointerEventData eventData)
            => Unhovered?.Invoke(Index);

        public void OnPointerClick(PointerEventData eventData)
            => Clicked?.Invoke(Index);

        // --------------------------------------------------
        // Drag / drop - talks directly to the shared hub rather than through
        // ContainerController, since a drop's source and target slot can
        // belong to two entirely different containers (e.g. inventory ->
        // chest), and no single per-container controller has visibility into
        // both sides of that.
        // --------------------------------------------------
        public void OnBeginDrag(PointerEventData eventData)
        {
            var hub = ContainerInteractionHub.Instance;
            if (hub == null) return;

            hub.Interaction.BeginDrag(resolveSlot?.Invoke());
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Intentionally empty - the held-item ghost (ContainerHeldItemView)
            // follows the cursor on its own via HeldItemChanged + Update().
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var hub = ContainerInteractionHub.Instance;
            if (hub == null) return;

            // No-op if OnDrop already resolved the placement - Unity calls
            // IDropHandler before IEndDragHandler, so by the time this runs
            // Cancel() only does something if nothing accepted the drop.
            hub.Interaction.Cancel();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var hub = ContainerInteractionHub.Instance;
            if (hub == null) return;

            hub.Interaction.Drop(resolveSlot?.Invoke());
        }
    }
}