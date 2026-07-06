using UnityEngine;

namespace SOLITUDE.Containers
{
    public enum ContainerPlaceResultType
    {
        Moved,
        Swapped,
        Merged,
        PartialMerged,
        Failed
    }

    public struct ContainerPlaceResult
    {
        public ContainerPlaceResultType Type;

        // Non-null whenever something is still held after the placement
        // (a partial merge leftover, or the stack displaced by a swap).
        public ItemStack Remaining;
    }

    /// <summary>
    /// Pure C# rules for placing a held item into a slot. This is the single
    /// place that decides move vs merge vs swap - nothing else should
    /// hand-roll that logic.
    /// </summary>
    public class ContainerService
    {
        public ContainerPlaceResult PlaceHeld(ItemStack held, ContainerSlot target)
        {
            if (held == null || held.IsEmpty || target == null)
                return new ContainerPlaceResult { Type = ContainerPlaceResultType.Failed, Remaining = held };

            // MOVE
            if (target.IsEmpty)
            {
                target.Set(held);
                return new ContainerPlaceResult { Type = ContainerPlaceResultType.Moved, Remaining = null };
            }

            // MERGE
            if (target.Stack.Definition == held.Definition && held.Definition.Stackable)
            {
                int space = target.Stack.MaxStack - target.Stack.Quantity;
                int transfer = Mathf.Min(space, held.Quantity);

                if (transfer > 0)
                {
                    target.Stack.Add(transfer);
                    held.Remove(transfer);
                }

                if (held.IsEmpty)
                    return new ContainerPlaceResult { Type = ContainerPlaceResultType.Merged, Remaining = null };

                return new ContainerPlaceResult { Type = ContainerPlaceResultType.PartialMerged, Remaining = held };
            }

            // SWAP
            var displaced = target.Take();
            target.Set(held);

            return new ContainerPlaceResult { Type = ContainerPlaceResultType.Swapped, Remaining = displaced };
        }

        /// <summary>Returns a held item to an (assumed empty) slot, e.g. on cancel.</summary>
        public void Restore(ContainerSlot slot, ItemStack item)
        {
            if (slot != null && slot.IsEmpty && item != null)
                slot.Set(item);
        }
    }
}