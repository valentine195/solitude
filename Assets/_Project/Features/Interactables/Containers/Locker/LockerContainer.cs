using System;
using System.Collections.Generic;
using UnityEngine;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Features.Interactables
{
    /// <summary>
    /// Thin MonoBehaviour adapter over the shared Container model. A Chest or
    /// Locker should look almost identical to this - only the interaction
    /// trigger (always-open vs. requires-interact vs. requires-key) differs.
    /// </summary>
    [RequireComponent(typeof(SaveableId))]
    public class LockerContainer : MonoBehaviour, IContainerSource, ISaveable<ContainerSaveData>
    {
        [SerializeField] private int capacity = 12;
        [SerializeField] private SaveableId saveableId;
        [SerializeField] private LootTable lootTable;

        private Container container;
        private bool initialized;
        private bool observingChanges;
        public Container Container => container ??= new Container(capacity);
        public string Label => "Locker";

        public int Capacity => capacity;

        public event Action<int> SlotChanged
        {
            add => Container.SlotChanged += value;
            remove => Container.SlotChanged -= value;
        }

        private void Awake()
        {
            container = new Container(capacity);
            if (saveableId == null) saveableId = GetComponent<SaveableId>();
        }

        /// <summary>
        /// Called by Locker immediately before the first open. Existing save data
        /// wins; otherwise deterministic loot is generated once and persisted.
        /// </summary>
        public bool EnsureInitialized()
        {
            if (initialized) return true;
            if (saveableId == null || string.IsNullOrEmpty(saveableId.Value))
            {
                Debug.LogError($"[{nameof(LockerContainer)}] A {nameof(SaveableId)} is required for persistent loot.", this);
                return false;
            }

            var saveGame = SaveGameService.Instance;
            if (saveGame == null)
            {
                Debug.LogError($"[{nameof(LockerContainer)}] No {nameof(SaveGameService)} is active. Add one to the persistent game bootstrap.", this);
                return false;
            }

            if (saveGame.TryGetContainer(saveableId.Value, out var savedState))
            {
                RestoreState(savedState);
                initialized = true;
                BeginPersistingChanges();
                return true;
            }

            if (lootTable != null)
            {
                var random = new System.Random(SeedUtility.DeriveSeed(saveGame.WorldSeed, saveableId.Value));
                PopulateOnlyDropsThatFit(lootTable.Roll(random));
            }

            initialized = true;
            PersistCurrentState();
            BeginPersistingChanges();
            return true;
        }

        private void BeginPersistingChanges()
        {
            if (observingChanges) return;
            Container.SlotChanged += OnSlotChanged;
            observingChanges = true;
        }

        private void OnSlotChanged(int _) => PersistCurrentState();

        private void PersistCurrentState()
        {
            if (!initialized || saveableId == null || string.IsNullOrEmpty(saveableId.Value)) return;
            SaveGameService.Instance?.SetContainer(saveableId.Value, CaptureState());
        }

        public ContainerSaveData CaptureState() => ContainerSaveSerializer.Capture(Container);

        public void RestoreState(ContainerSaveData state) =>
            ContainerSaveSerializer.Restore(Container, state, ItemDatabase.Instance);

        private void OnDestroy()
        {
            if (observingChanges)
                Container.SlotChanged -= OnSlotChanged;
        }

        private void PopulateOnlyDropsThatFit(List<(ItemDefinition item, int quantity)> candidates)
        {
            var simulated = new Container(Container.Capacity);
            foreach (var candidate in candidates)
            {
                if (!simulated.TryAdd(candidate.item, candidate.quantity)) continue;
                Container.TryAdd(candidate.item, candidate.quantity);
            }
        }
    }
}
