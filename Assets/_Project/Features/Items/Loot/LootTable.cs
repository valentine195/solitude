using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.Items
{
    /// <summary>
    /// A reusable weighted loot pool - shared across every container of a
    /// given "kind" (e.g. one LootTable asset for all Lockers, another for
    /// all Crates). Deliberately holds nothing per-instance: a specific
    /// locker that should always contain a keycard needs that expressed as
    /// a guaranteed drop on that locker's own spawner component, not baked
    /// into this shared asset - otherwise every locker using this table
    /// would need its own dedicated LootTable just to express one override.
    ///
    /// Takes a System.Random rather than owning one, so callers control
    /// determinism (see SeedUtility) - this class has no opinion about
    /// where its rng came from.
    /// </summary>
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ItemDefinition item;

            [Tooltip("Relative weight - higher rolls more often. Doesn't need to sum to any particular total.")]
            public float weight;

            public int minQuantity;
            public int maxQuantity;
        }

        [SerializeField] private List<Entry> entries = new();

        [Tooltip("How many random picks this table makes per roll. Actual count is itself randomized between these (inclusive) using the same seeded rng, so it stays reproducible rather than always rolling a fixed number.")]
        [SerializeField] private int minRolls = 1;
        [SerializeField] private int maxRolls = 3;

        public RuntimeLootTable Compile(IItemCatalog catalog)
        {
            var compiled = new List<LootEntry>();
            foreach (var entry in entries)
            {
                if (entry.item == null) throw new InvalidOperationException("Loot table contains a null item.");
                compiled.Add(new LootEntry(entry.item.ItemId, entry.weight, entry.minQuantity, entry.maxQuantity));
            }
            return new RuntimeLootTable(catalog, compiled, minRolls, maxRolls);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minRolls = Mathf.Max(0, minRolls);
            maxRolls = Mathf.Max(minRolls, maxRolls);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.weight = Mathf.Max(0f, entry.weight);
                entry.minQuantity = Mathf.Max(1, entry.minQuantity);
                entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
                entries[i] = entry;
            }
        }
#endif
    }
}
