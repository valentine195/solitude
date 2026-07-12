using System;
using SOLITUDE.Items;

namespace SOLITUDE.Containers
{
    [System.Serializable]
    public class ContainerSlot
    {
        private ItemStack stack;
        private readonly Func<ItemDefinition, bool> acceptFilter;

        public ItemStack Stack => stack;

        // Null-safe: an empty slot has no Stack, so this must not assume one exists.
        public ItemDefinition Definition => stack?.Definition;

        public bool IsEmpty => stack == null || stack.IsEmpty;

        public ContainerSlot(Func<ItemDefinition, bool> acceptFilter = null)
        {
            this.acceptFilter = acceptFilter;
        }

        // No filter means "accepts anything" - the common case (inventory,
        // chest, locker). A generator/furnace/etc. passes one in via its
        // Container's constructor instead of every slot needing its own rule.
        public bool CanAccept(ItemDefinition item) => acceptFilter == null || acceptFilter(item);

        // Fires whenever this slot's contents change, whether the slot itself
        // was Set/Cleared or the ItemStack inside it changed quantity.
        // Container subscribes to this per-slot and re-broadcasts it as an
        // index-based event, so views never need a direct slot reference.
        public event Action Changed;

        public void Set(ItemStack newStack)
        {
            if (stack != null) stack.Changed -= OnStackChanged;

            stack = newStack;

            if (stack != null) stack.Changed += OnStackChanged;

            Changed?.Invoke();
        }

        public void Clear()
        {
            if (stack != null) stack.Changed -= OnStackChanged;

            stack = null;

            Changed?.Invoke();
        }

        /// <summary>Removes and returns the stack in this slot, leaving it empty.</summary>
        public ItemStack Take()
        {
            var taken = stack;
            Clear();
            return taken;
        }

        private void OnStackChanged()
        {
            // Canonicalize "empty" as a null Stack rather than leaving a
            // zeroed-out ItemStack sitting in the slot - anything that reads
            // slot.Stack directly instead of checking IsEmpty first (a
            // future TryAdd tweak, a debug view, etc.) sees one consistent
            // "nothing here" state instead of two ("Stack == null" and
            // "Stack != null but Definition == null / Quantity == 0").
            if (stack != null && stack.IsEmpty)
            {
                Clear();
                return;
            }

            Changed?.Invoke();
        }
    }
}