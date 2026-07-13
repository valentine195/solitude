using System;
using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.SaveLoad
{
    [Serializable]
    public class ContainerSaveData
    {
        public List<ContainerSlotSaveData> slots = new();
    }

    [Serializable]
    public class ContainerSlotSaveData
    {
        public int index;
        public string itemId;
        public int quantity;
    }

    /// <summary>Boundary between JSON-friendly save data and live item assets.</summary>
    public static class ContainerSaveSerializer
    {
        public static ContainerSaveData Capture(Container container)
        {
            var data = new ContainerSaveData();
            if (container == null) return data;

            var slots = container.GetSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                var stack = slots[i].Stack;
                if (stack == null || stack.IsEmpty || string.IsNullOrEmpty(stack.Definition.ItemId)) continue;

                data.slots.Add(new ContainerSlotSaveData
                {
                    index = i,
                    itemId = stack.Definition.ItemId,
                    quantity = stack.Quantity
                });
            }

            return data;
        }

        public static void Restore(Container container, ContainerSaveData data, ItemDatabase database)
        {
            if (container == null) return;
            container.Clear();

            if (data?.slots == null || database == null)
            {
                if (database == null)
                    Debug.LogError("[ContainerSaveSerializer] Cannot restore without an ItemDatabase.");
                return;
            }

            foreach (var savedSlot in data.slots)
            {
                if (savedSlot == null) continue;
                var item = database.Resolve(savedSlot.itemId);
                if (!container.TrySetSlot(savedSlot.index, item, savedSlot.quantity))
                    Debug.LogWarning($"[ContainerSaveSerializer] Skipped invalid saved item '{savedSlot.itemId}' in slot {savedSlot.index}.");
            }
        }
    }
}
