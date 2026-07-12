using System;
using System.Collections.Generic;
using SOLITUDE.Items;

namespace SOLITUDE.Containers
{
    public interface IContainer
    {
        int Capacity { get; }

        // Raised with the changed slot's index whenever a slot's contents change,
        // so any bound view can refresh just that slot instead of polling.
        event Action<int> SlotChanged;

        // Raised when the slot count itself changes (Resize) - views rebuild
        // entirely rather than trying to patch in the delta.
        event Action CapacityChanged;

        ContainerSlot GetSlot(int index);
        IReadOnlyList<ContainerSlot> GetSlots();

        bool CanAccept(ItemDefinition item);
    }
}