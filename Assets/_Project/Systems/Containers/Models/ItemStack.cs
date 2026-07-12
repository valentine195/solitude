using System;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Containers
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemDefinition Definition { get; private set; }

        // Quantity can only change through Add/Remove so cleanup (e.g. clearing
        // Definition at zero) can never be bypassed, and so Changed always fires.
        public int Quantity { get; private set; }

        public event Action Changed;

        public int MaxStack => Definition != null ? Definition.MaxStackSize : 0;
        public bool IsEmpty => Definition == null || Quantity <= 0;
        public bool IsFull => Definition != null && Quantity >= Definition.MaxStackSize;

        public ItemStack(ItemDefinition definition, int quantity = 1)
        {
            Definition = definition;
            Quantity = quantity;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Quantity += amount;
            Changed?.Invoke();
        }

        public void Remove(int amount)
        {
            if (amount <= 0) return;

            Quantity -= amount;

            if (Quantity <= 0)
            {
                Definition = null;
                Quantity = 0;
            }

            Changed?.Invoke();
        }
    }
}