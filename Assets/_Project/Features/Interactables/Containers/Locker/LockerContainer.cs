using System;
using System.Collections.Generic;
using UnityEngine;
using SOLITUDE.Containers;

namespace SOLITUDE.Features.Interactables
{
    /// <summary>
    /// Thin MonoBehaviour adapter over the shared Container model. A Chest or
    /// Locker should look almost identical to this - only the interaction
    /// trigger (always-open vs. requires-interact vs. requires-key) differs.
    /// </summary>
    public class LockerContainer : MonoBehaviour, IContainerSource
    {
        [SerializeField] private int capacity = 12;

        private Container container;
        public Container Container => container ??= new Container(capacity);
        public string Label => "Locker";

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
    }
}
