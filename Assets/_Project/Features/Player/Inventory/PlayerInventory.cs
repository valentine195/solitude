using System;
using System.Collections.Generic;
using UnityEngine;
using SOLITUDE.Containers;

namespace SOLITUDE.Player
{
    /// <summary>
    /// Thin MonoBehaviour adapter over the shared Container model. A Chest or
    /// Locker should look almost identical to this - only the interaction
    /// trigger (always-open vs. requires-interact vs. requires-key) differs.
    /// </summary>
    public class PlayerInventory : MonoBehaviour, IContainer
    {
        [SerializeField] private int capacity = 24;

        private Container container;

        public int Capacity => capacity;

        public event Action<int> SlotChanged
        {
            add => container.SlotChanged += value;
            remove => container.SlotChanged -= value;
        }

        private void Awake()
        {
            container = new Container(capacity);
        }

        public string Label => "Inventory";
        public ContainerSlot GetSlot(int index) => container.GetSlot(index);

        public IReadOnlyList<ContainerSlot> GetSlots() => container.GetSlots();

        public bool Add(ItemDefinition item, int quantity = 1) => container.TryAdd(item, quantity);
    }
}
