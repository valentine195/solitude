using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace SOLITUDE.Containers
{
    public class ContainerView : MonoBehaviour, SOLITUDE.Application.IContainerDisplay
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
        private SOLITUDE.Items.ItemPresentationCatalog presentations;
        public event Action<SOLITUDE.Application.SlotIntent> Hovered, Unhovered, Selected, BeginDrag, EndDrag, Drop;
        public void ValidateAuthoring()
        {
            if (useExistingSlotViews)
            {
                var unique = new HashSet<ContainerSlotView>();
                if (existingSlotViews.Count == 0 || existingSlotViews.Exists(slot => slot == null || !unique.Add(slot)))
                    throw new InvalidOperationException("Pre-placed container slots are missing or duplicated.");
            }
            else if (slotViewPrefab == null || slotsParent == null)
                throw new InvalidOperationException("Container slot template/parent is missing.");
        }
        public void Initialize(SOLITUDE.Items.ItemPresentationCatalog catalog) => presentations = catalog;
        public void Build(string text, int capacity, long generation)
        {
            Clear();
            if (label != null) label.text = text;
            if (gridLayoutGroup != null)
            {
                gridLayoutGroup.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = Mathf.Max(1, columns);
            }
            if (useExistingSlotViews)
                for (int i = 0; i < existingSlotViews.Count; i++) existingSlotViews[i].gameObject.SetActive(i < capacity);
            for (int i = 0; i < capacity; i++)
            {
                ContainerSlotView slot;
                if (useExistingSlotViews && i < existingSlotViews.Count) slot = existingSlotViews[i];
                else
                {
                    var template = slotViewPrefab != null ? slotViewPrefab : useExistingSlotViews && existingSlotViews.Count > 0 ? existingSlotViews[0] : null;
                    if (template == null || (!useExistingSlotViews && slotsParent == null)) throw new InvalidOperationException("Container slot template/parent is missing.");
                    slot = Instantiate(template, slotsParent != null ? slotsParent : template.transform.parent);
                    slot.gameObject.SetActive(true);
                }
                slot.Bind(i, generation, presentations);
                slot.Hovered += Hover; slot.Unhovered += Unhover; slot.Clicked += Select;
                slot.BeginDrag += Begin; slot.EndDrag += End; slot.Dropped += Dropped;
                slotViews.Add(slot);
            }
        }
        public void Render(SOLITUDE.Application.SlotPresentation slot)
        { if (slot.Index >= 0 && slot.Index < slotViews.Count) slotViews[slot.Index].Render(slot); }
        public void Clear()
        {
            foreach (var slot in slotViews)
            {
                if (slot == null) continue;
                slot.Hovered -= Hover; slot.Unhovered -= Unhover; slot.Clicked -= Select;
                slot.BeginDrag -= Begin; slot.EndDrag -= End; slot.Dropped -= Dropped;
                slot.Unbind();
                if (!useExistingSlotViews || !existingSlotViews.Contains(slot)) Destroy(slot.gameObject);
            }
            slotViews.Clear();
        }
        private void Hover(SOLITUDE.Application.SlotIntent intent) => Hovered?.Invoke(intent);
        private void Unhover(SOLITUDE.Application.SlotIntent intent) => Unhovered?.Invoke(intent);
        private void Select(SOLITUDE.Application.SlotIntent intent) => Selected?.Invoke(intent);
        private void Begin(SOLITUDE.Application.SlotIntent intent) => BeginDrag?.Invoke(intent);
        private void End(SOLITUDE.Application.SlotIntent intent) => EndDrag?.Invoke(intent);
        private void Dropped(SOLITUDE.Application.SlotIntent intent) => Drop?.Invoke(intent);
        private void OnDestroy() => Clear();
    }
}
