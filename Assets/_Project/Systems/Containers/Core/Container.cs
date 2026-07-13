using System;
using System.Collections.Generic;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Plain C# container model. Holds no Unity lifecycle logic - any MonoBehaviour
    /// (PlayerInventory, Chest, Locker, ...) that needs to be an IContainer should
    /// own one of these and forward calls to it, rather than re-implementing slot
    /// bookkeeping per container type.
    /// </summary>
    public class Container : IContainer
    {
        private readonly List<ContainerSlot> slots;

        // Optional - null means "accepts anything" (inventory, chest, locker).
        // A generator/furnace/etc. passes a predicate here; it's threaded into
        // every slot at creation time (including ones added by Resize) so
        // ContainerService.PlaceHeld can ask the slot directly, without
        // needing a back-reference to this Container.
        private readonly Func<ItemDefinition, bool> acceptFilter;

        public int Capacity => slots.Count;

        public event Action<int> SlotChanged;

        // Fires when Resize() changes the slot count - ContainerView listens
        // and rebuilds its slot views. Not relevant to hotbar-style pre-placed
        // slots, which have no supported resize path.
        public event Action CapacityChanged;

        public Container(int capacity, Func<ItemDefinition, bool> acceptFilter = null)
        {
            this.acceptFilter = acceptFilter;
            slots = new List<ContainerSlot>(capacity);

            for (int i = 0; i < capacity; i++)
                slots.Add(CreateSlot(i));
        }

        private ContainerSlot CreateSlot(int index)
        {
            var slot = new ContainerSlot(acceptFilter);
            slot.Changed += () => SlotChanged?.Invoke(index);
            return slot;
        }

        public ContainerSlot GetSlot(int index) => slots[index];

        public IReadOnlyList<ContainerSlot> GetSlots() => slots;

        public bool IsEmpty(int index)
        {
            return slots[index].IsEmpty;
        }

        public bool CanAccept(ItemDefinition item) => acceptFilter == null || acceptFilter(item);

        /// <summary>
        /// Grows or shrinks the container. Shrinking refuses (logs a warning,
        /// no-op) rather than discarding an occupied slot's contents - matches
        /// TryAdd's own contract of never silently losing items.
        /// </summary>
        public void Resize(int newCapacity)
        {
            if (newCapacity == slots.Count) return;

            if (newCapacity < slots.Count)
            {
                for (int i = newCapacity; i < slots.Count; i++)
                {
                    if (!slots[i].IsEmpty)
                    {
                        Debug.LogWarning($"{nameof(Container)}: cannot shrink to {newCapacity} - slot {i} is occupied.");
                        return;
                    }
                }
                slots.RemoveRange(newCapacity, slots.Count - newCapacity);
            }
            else
            {
                int startIndex = slots.Count;
                for (int i = startIndex; i < newCapacity; i++)
                    slots.Add(CreateSlot(i));
            }

            CapacityChanged?.Invoke();
        }

        /// <summary>
        /// Best-effort add: fills existing stacks first, then empty slots.
        /// Returns false if the container couldn't fit the full quantity
        /// (whatever did fit remains added - this is not transactional), or
        /// if the item is rejected outright by this container's accept filter.
        /// </summary>
        public bool TryAdd(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0 || !CanAccept(item)) return false;

            if (item.Stackable)
            {
                foreach (var slot in slots)
                {
                    if (quantity <= 0) break;
                    if (slot.IsEmpty || slot.Definition != item || slot.Stack.IsFull) continue;

                    int space = slot.Stack.MaxStack - slot.Stack.Quantity;
                    int add = Mathf.Min(space, quantity);

                    slot.Stack.Add(add);
                    quantity -= add;
                }
            }

            foreach (var slot in slots)
            {
                if (quantity <= 0) break;
                if (!slot.IsEmpty) continue;

                int add = item.Stackable ? Mathf.Min(item.MaxStackSize, quantity) : 1;
                slot.Set(new ItemStack(item, add));
                quantity -= add;
            }

            return quantity <= 0;
        }

        /// <summary>Clears every slot. Used by save restoration before exact slots are applied.</summary>
        public void Clear()
        {
            foreach (var slot in slots)
                slot.Clear();
        }

        /// <summary>
        /// Restores one exact slot without compacting or re-stacking the
        /// container. Invalid saved data is rejected rather than coerced.
        /// </summary>
        public bool TrySetSlot(int index, ItemDefinition item, int quantity)
        {
            if (index < 0 || index >= slots.Count || item == null || quantity < 1 ||
                !CanAccept(item) || (item.Stackable && quantity > item.MaxStackSize) ||
                (!item.Stackable && quantity != 1))
                return false;

            slots[index].Set(new ItemStack(item, quantity));
            return true;
        }
    }
}
