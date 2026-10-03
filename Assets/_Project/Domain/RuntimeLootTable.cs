using System;
using System.Collections.Generic;
namespace SOLITUDE.Items
{
    public readonly struct LootEntry
    {
        public string ItemId { get; }
        public float Weight { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public LootEntry(string id, float weight, int minimum, int maximum)
        { ItemId = id; Weight = weight; Minimum = minimum; Maximum = maximum; }
    }
    public sealed class RuntimeLootTable
    {
        private readonly LootEntry[] entries;
        private readonly int minimumRolls, maximumRolls;
        private readonly float totalWeight;
        public RuntimeLootTable(IItemCatalog catalog, IEnumerable<LootEntry> source, int minimumRolls, int maximumRolls)
        {
            if (catalog == null || source == null || minimumRolls < 0 || maximumRolls < minimumRolls || maximumRolls == int.MaxValue)
                throw new ArgumentException("Invalid loot roll range.");
            var copy = new List<LootEntry>(); float total = 0;
            foreach (var entry in source)
            {
                if (!catalog.TryGet(entry.ItemId, out _) || entry.Weight <= 0 || float.IsNaN(entry.Weight) || float.IsInfinity(entry.Weight) ||
                    entry.Minimum < 1 || entry.Maximum < entry.Minimum || entry.Maximum == int.MaxValue)
                    throw new ArgumentException("Invalid loot entry.");
                copy.Add(entry); total += entry.Weight;
            }
            if (float.IsInfinity(total)) throw new ArgumentException("Loot weights overflow.");
            entries = copy.ToArray(); totalWeight = total; this.minimumRolls = minimumRolls; this.maximumRolls = maximumRolls;
        }
        public IReadOnlyList<PickupReward> Roll(Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var rewards = new List<PickupReward>();
            if (entries.Length == 0) return rewards.AsReadOnly();
            int rolls = rng.Next(minimumRolls, maximumRolls + 1);
            for (int i = 0; i < rolls; i++)
            {
                double point = rng.NextDouble() * totalWeight; var chosen = entries[entries.Length - 1];
                foreach (var entry in entries) { point -= entry.Weight; if (point <= 0) { chosen = entry; break; } }
                rewards.Add(new PickupReward(chosen.ItemId, rng.Next(chosen.Minimum, chosen.Maximum + 1)));
            }
            return rewards.AsReadOnly();
        }
    }
}
