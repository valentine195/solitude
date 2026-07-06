using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace SOLITUDE.Containers
{
    public class ContainerView : MonoBehaviour
    {
        [Tooltip("Prefab with a ContainerSlotView component; one is instantiated per slot.")]
        [SerializeField] private ContainerSlotView slotViewPrefab;

        [Tooltip("Parent transform slot views are instantiated under (usually holds a GridLayoutGroup).")]
        [SerializeField] private Transform slotsParent;

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

        public void Bind(IContainer container)
        {
            Unbind();

            this.container = container;
            container.SlotChanged += OnContainerSlotChanged;

            label.text = container.Label;
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

            var containerSlots = container.GetSlots();

            for (int i = 0; i < containerSlots.Count; i++)
            {
                var slotView = Instantiate(slotViewPrefab, slotsParent);
                int index = i;

                slotView.Bind(index, () => container.GetSlot(index));
                slotView.Refresh(containerSlots[index]);

                slotView.Hovered += OnSlotViewHovered;
                slotView.Unhovered += OnSlotViewUnhovered;
                slotView.Clicked += OnSlotViewClicked;

                slotViews.Add(slotView);
            }
        }

        private void ClearSlots()
        {
            foreach (var slotView in slotViews)
            {
                if (slotView == null) continue;

                slotView.Hovered -= OnSlotViewHovered;
                slotView.Unhovered -= OnSlotViewUnhovered;
                slotView.Clicked -= OnSlotViewClicked;
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