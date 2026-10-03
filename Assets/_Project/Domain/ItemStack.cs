using SOLITUDE.Items;
namespace SOLITUDE.Containers
{
    public sealed class ItemStack
    {
        public ItemSpec Definition { get; }
        public string ItemId => Definition.ItemId;
        public int Quantity { get; }
        public int MaxStack => Definition.MaxStackSize;
        public bool IsFull => Quantity == MaxStack;
        internal ItemStack(ItemSpec item, int quantity) { Definition = item; Quantity = quantity; }
    }
}
