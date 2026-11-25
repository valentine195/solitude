using System;
using SOLITUDE.Composition;
using SOLITUDE.Movement;
using SOLITUDE.SaveLoad;
using SOLITUDE.Tasks;
using UnityEngine;

namespace SOLITUDE.UI.Exploration
{
    public sealed class ExplorationHudComposition : SceneFeatureComposition
    {
        [SerializeField] private ExplorationHudView view;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private MovementBehavior player;
        [SerializeField] private string locationName;
        [SerializeField] private Rect mapWorldBounds;

        private SceneFeatureContext context;
        private IDisposable taskObservation;
        private int previousQuickUse = -1;
        private string previousQuickUseItem;
        private float quickUseDetailUntil;

        public override void Validate()
        {
            if (view == null || visibility == null || equipmentPanel == null || player == null
                || string.IsNullOrWhiteSpace(locationName) || mapWorldBounds.width <= 0 || mapWorldBounds.height <= 0)
                throw new InvalidOperationException("The exploration HUD needs its view, player and minimap bounds.");
        }

        public override void Initialize(SceneFeatureContext bindings)
        {
            if (bindings.Equipment == null)
                throw new InvalidOperationException("The exploration HUD needs carried equipment.");

            context = bindings;
            context.Policy.Changed += RefreshVisibility;
            context.Input.InputDeviceChanged += RefreshEquipment;
            context.Equipment.Changed += RefreshEquipment;
            context.Equipment.QuickUseSelectionChanged += RefreshQuickUse;
            context.Inventory.SlotChanged += OnInventoryChanged;
            context.Inventory.CapacityChanged += RefreshEquipment;
            TaskJournalSnapshot tasks = null;
            if (context.Tasks != null)
                taskObservation = context.Tasks.ObserveWithSnapshot(change => RefreshObjective(change.After), out tasks);
            RefreshObjective(tasks);
            RefreshEquipment();
            RefreshVisibility();
            UpdatePlayerMarker();
        }

        public override void Release()
        {
            if (context == null) return;
            taskObservation?.Dispose();
            taskObservation = null;
            context.Policy.Changed -= RefreshVisibility;
            context.Input.InputDeviceChanged -= RefreshEquipment;
            context.Equipment.Changed -= RefreshEquipment;
            context.Equipment.QuickUseSelectionChanged -= RefreshQuickUse;
            previousQuickUse = -1;
            previousQuickUseItem = null;
            context.Inventory.SlotChanged -= OnInventoryChanged;
            context.Inventory.CapacityChanged -= RefreshEquipment;
            context = null;
            CloseForTransition();
        }

        public override bool TryBack() => false;
        public override void CloseForTransition()
        {
            if (visibility != null) visibility.alpha = 0;
        }

        private void RefreshVisibility()
        {
            if (visibility != null) visibility.alpha = context.Policy.Gameplay ? 1 : 0;
        }

        private void LateUpdate()
        {
            if (context == null || !context.Policy.Gameplay) return;
            UpdatePlayerMarker();
            if (quickUseDetailUntil > 0 && Time.unscaledTime >= quickUseDetailUntil)
            {
                quickUseDetailUntil = 0;
                view.QuickUseReadout.ShowDetail(string.Empty, string.Empty, false, 0);
            }
        }

        private void UpdatePlayerMarker()
        {
            var position = (Vector2)player.transform.position;
            var normalized = new Vector2(
                Mathf.InverseLerp(mapWorldBounds.xMin, mapWorldBounds.xMax, position.x),
                Mathf.InverseLerp(mapWorldBounds.yMax, mapWorldBounds.yMin, position.y));
            view.NavigationReadout.ShowPlayer(normalized, Vector2.SignedAngle(player.FacingDirection, Vector2.up));
        }

        private void RefreshObjective(TaskJournalSnapshot tasks)
        {
            TaskSnapshot objective = null;
            if (tasks != null)
            {
                foreach (var record in tasks.Active)
                {
                    if (record.ScopeId != context.TaskScope || !record.SourceAvailable) continue;
                    objective = record.State;
                    break;
                }
            }
            view.NavigationReadout.ShowLocation(locationName, objective?.Title, objective?.NextStep, null);
        }

        private void OnInventoryChanged(int _) => RefreshEquipment();

        private void RefreshEquipment()
        {
            var state = context.Equipment.State;
            var held = context.Presentations.Resolve(state.ActiveItemId);
            equipmentPanel.SetActive(held != null);
            if (held != null)
            {
                var canSwitch = state.weaponItemId != null && state.toolItemId != null;
                var detail = canSwitch ? context.Input.DeviceBindingLabel("Gameplay/SwitchEquipment") + "  Switch" : "In hand";
                view.EquipmentReadout.ShowItem(held.Icon, held.DisplayName, detail);
            }

            RefreshQuickUse();
        }

        private void RefreshQuickUse()
        {
            var state = context.Equipment.State;
            int selected = context.Equipment.SelectedQuickUse;
            for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
            {
                var id = state.QuickUseItem(i);
                var item = context.Presentations.Resolve(id);
                view.QuickUseReadout.ShowSlot(i, item?.Icon, context.Equipment.Quantity(id), i == selected);
            }

            var selectedId = state.QuickUseItem(selected);
            if (previousQuickUse != selected || previousQuickUseItem != selectedId)
            {
                quickUseDetailUntil = Time.unscaledTime + 3f;
                previousQuickUse = selected;
                previousQuickUseItem = selectedId;
            }

            var selectedItem = context.Presentations.Resolve(selectedId);
            bool canUse = selectedItem != null && context.Equipment.Quantity(selectedId) > 0;
            view.QuickUseReadout.ShowDetail(selectedItem?.DisplayName ?? string.Empty,
                context.Input.DeviceBindingLabel("Gameplay/Use"), canUse, canUse && quickUseDetailUntil > 0 ? 1f : 0f);
        }
    }
}
