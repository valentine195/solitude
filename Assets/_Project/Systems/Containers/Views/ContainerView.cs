using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace SOLITUDE.Containers
{
    public class ContainerView : MonoBehaviour
    {
        [Tooltip("Prefab with a ContainerSlotView component; one is instantiated per slot. Not used when Use Existing Slot Views is enabled.")]
        [SerializeField] private ContainerSlotView slotViewPrefab;

        [Tooltip("Parent transform slot views are instantiated under (usually holds a GridLayoutGroup). Not used when Use Existing Slot Views is enabled.")]
        [SerializeField] private Transform slotsParent;

        [Header("Pre-placed Slots (e.g. Hotbar)")]
        [Tooltip("Enable for a fixed-size, always-visible container whose slots are hand-authored in the scene/prefab rather than built at runtime from an arbitrary-capacity container (e.g. a 9-slot hotbar vs. a variable-size chest). When true, Bind() uses Existing Slot Views below instead of instantiating slotViewPrefab, and Unbind() does not destroy them.")]
        [SerializeField] private bool useExistingSlotViews;

        [Tooltip("Only used when Use Existing Slot Views is enabled. Assign in scene order - index 0 first. Count must match the bound container's capacity.")]
        [SerializeField] private List<ContainerSlotView> existingSlotViews = new();

        [Header("Grid Layout (optional)")]
        [Tooltip("If assigned, its column count is set from Columns below on every Bind - lets one prefab type serve containers of different shapes (e.g. a 9-wide inventory vs. a 5-wide locker).")]
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private int columns;

        [Tooltip("Label of the container")]
        [SerializeField] private TextMeshProUGUI label;
        private readonly List<ContainerSlotView> slotViews = new();
        private IContainer container;
        private int selectedIndex = -1;

        // Index-based, per the architecture: the view never hands out a
        // ContainerSlot or ContainerSlotView reference - only which index changed.
        public event Action<int> SlotHovered;
        public event Action<int> SlotUnhovered;

        // Fires when the user clicks a slot to select it (click is selection
        // only - moving items is a drag gesture, handled by ContainerSlotView
        // talking directly to ContainerInteractionHub).
        public event Action<int> SlotSelected;

        public void Bind(IContainerSource containerSource)
        {
            Unbind();

            this.container = containerSource.Container;
            container.SlotChanged += OnContainerSlotChanged;

            label.text = containerSource.Label;
            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = Mathf.Max(1, columns);
            }

            BuildSlots();
        }

        public void Unbind()
        {
            if (container != null)
            {
                container.SlotChanged -= OnContainerSlotChanged;
                container = null;
            }

            selectedIndex = -1;
            ClearSlots();
        }

        public ContainerSlot GetSlot(int index) => container?.GetSlot(index);

        private void BuildSlots()
        {
            var containerSlots = container.GetSlots();

            if (useExistingSlotViews)
            {
                if (existingSlotViews.Count != containerSlots.Count)
                {
                    Debug.LogError(
                        $"{nameof(ContainerView)}: Existing Slot Views count ({existingSlotViews.Count}) " +
                        $"does not match the bound container's capacity ({containerSlots.Count}).",
                        this);
                    return;
                }

                for (int i = 0; i < existingSlotViews.Count; i++)
                {
                    BindSlotView(existingSlotViews[i], i, containerSlots);
                    slotViews.Add(existingSlotViews[i]);
                }

                return;
            }

            if (slotViewPrefab == null)
            {
                Debug.LogError($"{nameof(ContainerView)}: Slot View Prefab is not assigned (or its reference is broken).", this);
                return;
            }

            if (slotsParent == null)
            {
                Debug.LogError($"{nameof(ContainerView)}: Slots Parent is not assigned.", this);
                return;
            }

            for (int i = 0; i < containerSlots.Count; i++)
            {
                var slotView = Instantiate(slotViewPrefab, slotsParent);
                BindSlotView(slotView, i, containerSlots);
                slotViews.Add(slotView);
            }
        }

        private void BindSlotView(ContainerSlotView slotView, int index, IReadOnlyList<ContainerSlot> containerSlots)
        {
            slotView.Bind(index, () => container.GetSlot(index));
            slotView.Refresh(containerSlots[index]);

            slotView.Hovered += OnSlotViewHovered;
            slotView.Unhovered += OnSlotViewUnhovered;
            slotView.Clicked += OnSlotViewClicked;
        }

        private void ClearSlots()
        {
            foreach (var slotView in slotViews)
            {
                if (slotView == null) continue;

                slotView.Hovered -= OnSlotViewHovered;
                slotView.Unhovered -= OnSlotViewUnhovered;
                slotView.Clicked -= OnSlotViewClicked;

                // Pre-placed slots (hotbar) are hand-authored in the scene/
                // prefab - Unbind() must not destroy them the way it destroys
                // runtime-instantiated inventory/chest slots.
                if (!useExistingSlotViews)
                    Destroy(slotView.gameObject);
            }

            slotViews.Clear();
        }

        private void OnContainerSlotChanged(int index)
        {
            if (index < 0 || index >= slotViews.Count) return;
            slotViews[index].Refresh(container.GetSlot(index));
        }

        private void OnSlotViewHovered(int index) => SlotHovered?.Invoke(index);
        private void OnSlotViewUnhovered(int index) => SlotUnhovered?.Invoke(index);

        private void OnSlotViewClicked(int index)
        {
            // Click toggles selection: clicking the already-selected slot
            // deselects it, clicking a different slot moves the highlight.
            SetSelected(index == selectedIndex ? -1 : index);
            SlotSelected?.Invoke(selectedIndex);
        }

        private void SetSelected(int index)
        {
            if (selectedIndex >= 0 && selectedIndex < slotViews.Count)
                slotViews[selectedIndex].SetSelected(false);

            selectedIndex = index;

            if (selectedIndex >= 0 && selectedIndex < slotViews.Count)
                slotViews[selectedIndex].SetSelected(true);
        }
    }
}