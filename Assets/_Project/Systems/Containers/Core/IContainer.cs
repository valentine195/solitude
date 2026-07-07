using System;
using System.Collections.Generic;

namespace SOLITUDE.Containers
{
    public interface IContainer
    {
        int Capacity { get; }

        // Raised with the changed slot's index whenever a slot's contents change,
        // so any bound view can refresh just that slot instead of polling.
        event Action<int> SlotChanged;

        public string Label { get; }
        ContainerSlot GetSlot(int index);
        IReadOnlyList<ContainerSlot> GetSlots();
    }
}
