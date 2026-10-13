using System;
using System.Collections.Generic;
namespace SOLITUDE.Vitals
{
    public enum VitalConsumableKind { None = 0, Food = 1, Medical = 2 }
    public sealed class VitalConsumableSpec
    {
        public string ItemId { get; }
        public VitalConsumableKind Kind { get; }
        public double Amount { get; }
        public bool Supported => Kind == VitalConsumableKind.Food || Kind == VitalConsumableKind.Medical;
        public bool IsValid => !string.IsNullOrWhiteSpace(ItemId) && Supported && Amount > 0
            && !double.IsNaN(Amount) && !double.IsInfinity(Amount);
        public VitalConsumableSpec(string itemId, VitalConsumableKind kind = VitalConsumableKind.None, double amount = 0)
        { ItemId = itemId; Kind = kind; Amount = amount; }
    }
    public sealed class VitalConsumableCatalog
    {
        private readonly Dictionary<string, VitalConsumableSpec> items = new Dictionary<string, VitalConsumableSpec>(StringComparer.Ordinal);
        public VitalConsumableCatalog(IEnumerable<VitalConsumableSpec> specifications)
        {
            if (specifications == null) throw new ArgumentNullException(nameof(specifications));
            foreach (var item in specifications)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || items.ContainsKey(item.ItemId))
                    throw new ArgumentException("Null, missing or duplicate consumable identity.");
                // Invalid effect metadata remains inspectable/rejectable, never a different valid effect.
                items.Add(item.ItemId, item);
            }
        }
        public VitalConsumableSpec Resolve(string id) => id != null && items.TryGetValue(id, out var item) ? item : null;
    }
}
