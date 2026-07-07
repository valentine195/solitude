using System;
using System.Collections.Generic;
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

        public string Label => "Container";

        public int Capacity => slots.Count;

        public event Action<int> SlotChanged;

        public Container(int capacity)
        {
            slots = new List<ContainerSlot>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                var slot = new ContainerSlot();
                int index = i; // capture for the closure
                slot.Changed += () => SlotChanged?.Invoke(index);
                slots.Add(slot);
            }
        }

        public ContainerSlot GetSlot(int index) => slots[index];

        public IReadOnlyList<ContainerSlot> GetSlots() => slots;

        /// <summary>
        /// Best-effort add: fills existing stacks first, then empty slots.
        /// Returns false if the container couldn't fit the full quantity
        /// (whatever did fit remains added - this is not transactional).
        /// </summary>
        public bool TryAdd(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

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
    }
}
