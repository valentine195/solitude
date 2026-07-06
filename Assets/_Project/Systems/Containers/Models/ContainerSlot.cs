using System;
using UnityEngine;

namespace SOLITUDE.Containers
{
    [System.Serializable]
    public class ContainerSlot
    {
        private ItemStack stack;

        public ItemStack Stack => stack;

        // Null-safe: an empty slot has no Stack, so this must not assume one exists.
        public ItemDefinition Definition => stack?.Definition;

        public bool IsEmpty => stack == null || stack.IsEmpty;

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

        private void OnStackChanged() => Changed?.Invoke();
    }
}