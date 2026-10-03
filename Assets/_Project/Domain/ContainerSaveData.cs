using System;
using System.Collections.Generic;
using SOLITUDE.Containers;
namespace SOLITUDE.SaveLoad
{
    [Serializable] public class ContainerSaveData { public List<ContainerSlotSaveData> slots = new List<ContainerSlotSaveData>(); }
    [Serializable] public class ContainerSlotSaveData { public int index; public string itemId; public int quantity; }
    public static class ContainerSaveSerializer
    {
        public static ContainerSaveData Capture(IContainerReader container)
        {
            var data = new ContainerSaveData();
            if (container == null) return data;
            foreach (var slot in container.GetSlots())
                if (!slot.IsEmpty) data.slots.Add(new ContainerSlotSaveData { index = slot.Address.Index, itemId = slot.Stack.ItemId, quantity = slot.Stack.Quantity });
            return data;
        }
        public static ContainerSaveData Clone(ContainerSaveData source)
        {
            if (source == null) return null;
            var result = new ContainerSaveData { slots = source.slots == null ? null : new List<ContainerSlotSaveData>() };
            if (source.slots != null) foreach (var slot in source.slots)
                result.slots.Add(slot == null ? null : new ContainerSlotSaveData { index = slot.index, itemId = slot.itemId, quantity = slot.quantity });
            return result;
        }
    }
}
