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

        /// <summary>
        /// Rolls this table once, returning (item, quantity) pairs to add to
        /// a container. Can return the same item more than once across
        /// multiple rolls - that's fine, TryAdd stacks it.
        /// </summary>
        public List<(ItemDefinition item, int quantity)> Roll(System.Random rng)
        {
            var results = new List<(ItemDefinition, int)>();

            if (rng == null || entries.Count == 0) return results;

            int minimumRolls = Mathf.Max(0, minRolls);
            int maximumRolls = Mathf.Max(minimumRolls, maxRolls);

            var validEntries = new List<Entry>();

            float totalWeight = 0f;
            foreach (var entry in entries)
            {
                if (!IsValid(entry)) continue;
                validEntries.Add(entry);
                totalWeight += entry.weight;
            }

            if (totalWeight <= 0f) return results;

            int rollCount = rng.Next(minimumRolls, maximumRolls + 1);

            for (int i = 0; i < rollCount; i++)
            {
                var entry = PickWeighted(rng, validEntries, totalWeight);

                int minimumQuantity = Mathf.Max(1, entry.minQuantity);
                int maximumQuantity = Mathf.Max(minimumQuantity, entry.maxQuantity);
                int quantity = rng.Next(minimumQuantity, maximumQuantity + 1);

                results.Add((entry.item, quantity));
            }

            return results;
        }

        private static bool IsValid(Entry entry) =>
            entry.item != null && entry.weight > 0f && entry.maxQuantity >= 1;

        private static Entry PickWeighted(System.Random rng, List<Entry> validEntries, float totalWeight)
        {
            double point = rng.NextDouble() * totalWeight;

            foreach (var entry in validEntries)
            {
                point -= entry.weight;
                if (point <= 0) return entry;
            }

            // Floating-point rounding can leave `point` fractionally above
            // zero even after subtracting every weight - fall back to the
            // last entry rather than returning a default(Entry) with a null
            // item, which Roll() would otherwise just silently skip.
            return validEntries[validEntries.Count - 1];
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
