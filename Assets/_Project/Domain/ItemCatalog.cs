using System;
using System.Collections.Generic;
namespace SOLITUDE.Items
{
    public sealed class ItemSpec
    {
        public string ItemId { get; }
        public bool Stackable { get; }
        public int MaxStackSize { get; }
        public ItemSpec(string itemId, bool stackable, int maxStackSize)
        {
            if (string.IsNullOrWhiteSpace(itemId) || (stackable && maxStackSize < 1))
                throw new ArgumentException("Item identity and stack limit must be valid.");
            ItemId = itemId; Stackable = stackable; MaxStackSize = stackable ? maxStackSize : 1;
        }
    }
    public interface IItemCatalog { bool TryGet(string id, out ItemSpec item); IReadOnlyList<ItemSpec> Items { get; } }
    public sealed class ItemCatalog : IItemCatalog
    {
        private readonly Dictionary<string, ItemSpec> items = new Dictionary<string, ItemSpec>(StringComparer.Ordinal);
        public IReadOnlyList<ItemSpec> Items { get; }
        public ItemCatalog(IEnumerable<ItemSpec> specifications)
        {
            if (specifications == null) throw new ArgumentNullException(nameof(specifications));
            foreach (var item in specifications)
            {
                if (item == null || items.ContainsKey(item.ItemId)) throw new ArgumentException("Null or duplicate item specification.");
                items.Add(item.ItemId, item);
            }
            Items = new List<ItemSpec>(items.Values).AsReadOnly();
        }
        public bool TryGet(string id, out ItemSpec item)
        { item = null; return id != null && items.TryGetValue(id, out item); }
    }
}
