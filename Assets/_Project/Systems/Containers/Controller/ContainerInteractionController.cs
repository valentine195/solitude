using System;
using UnityEngine;

namespace SOLITUDE.Containers
{
    /// <summary>
    /// Held-item state machine for drag-based container interaction. Plain C# -
    /// no MonoBehaviour, no Unity lifecycle - so it can be constructed directly
    /// (see ContainerInteractionHub) and unit-tested on its own.
    ///
    /// Exactly one instance of this should exist per "session" (see
    /// ContainerInteractionHub) - not one per container - so an item picked up
    /// in one container can be placed into another.
    ///
    /// A plain click is NOT handled here - it's a pure UI selection concern
    /// (see ContainerView/ContainerSlotView). This class only reacts to an
    /// actual drag gesture (BeginDrag -> Drop / Cancel).
    /// </summary>
    public class ContainerInteractionController
    {
        private readonly ContainerService service;

        private InteractionState state = InteractionState.Idle;
        private ItemStack heldItem;
        private ContainerSlot originSlot;

        // Optional: UI hover (purely informational for now)
        private ContainerSlot hovered;

        /// <summary>
        /// Fires whenever the held item changes - picked up, placed, cleared,
        /// or partially consumed by a merge. Null means "nothing is held".
        /// Drive a cursor-following ghost icon off this rather than off any
        /// one container's events, since the hold spans containers.
        /// </summary>
        public event Action<ItemStack> HeldItemChanged;

        public bool IsHolding => state == InteractionState.HoldingItem;
        public ItemStack HeldItem => heldItem;

        public ContainerInteractionController(ContainerService service)
        {
            this.service = service;
        }

        public void SetHover(ContainerSlot slot)
        {
            hovered = slot;
        }

        // --------------------------------------------------
        // Idle -> Holding (fires from IBeginDragHandler)
        // --------------------------------------------------
        public void BeginDrag(ContainerSlot slot)
        {
            Debug.Log("[ContainerInteractionController] BeginDrag started");
            if (slot == null || slot.IsEmpty) return;

            // Already holding something (shouldn't normally happen since a
            // click can't begin a second drag mid-hold, but guard anyway).
            if (state == InteractionState.HoldingItem) return;

            // Take() clears the slot AND hands us the stack we're now holding -
            // there is no second, stale reference to the slot's old contents.
            Debug.Log("[ContainerInteractionController] BeginDrag Held set");
            SetHeld(slot.Take(), slot);
        }

        // --------------------------------------------------
        // Holding -> resolve (fires from IDropHandler on the target slot)
        // --------------------------------------------------
        public void Drop(ContainerSlot target)
        {
            if (state != InteractionState.HoldingItem) return;
            if (target == null) return;

            // Dropped back where it came from - just restore it.
            if (target == originSlot)
            {
                ReturnToOrigin();
                return;
            }

            var result = service.PlaceHeld(heldItem, target);

            switch (result.Type)
            {
                case ContainerPlaceResultType.Moved:
                case ContainerPlaceResultType.Merged:
                    Clear();
                    break;

                case ContainerPlaceResultType.PartialMerged:
                    // Some of the held stack didn't fit - keep holding the rest,
                    // still tied to the same origin slot for a cancel/return.
                    SetHeld(result.Remaining, originSlot);
                    break;

                case ContainerPlaceResultType.Swapped:
                    // Continue holding the item that was displaced from target.
                    SetHeld(result.Remaining, target);
                    break;

                case ContainerPlaceResultType.Failed:
                    break;
            }
        }

        /// <summary>
        /// Cancels the current hold and returns the item to its origin slot.
        /// Called both by an explicit cancel input (e.g. Escape) and by
        /// IEndDragHandler as a catch-all for "dropped on nothing" - if a
        /// Drop already succeeded by the time this runs, IsHolding is false
        /// and this is a no-op (Unity calls IDropHandler before IEndDragHandler).
        /// </summary>
        public void Cancel()
        {
            if (state == InteractionState.HoldingItem)
                ReturnToOrigin();
        }

        private void ReturnToOrigin()
        {
            service.Restore(originSlot, heldItem);
            Clear();
        }

        private void SetHeld(ItemStack item, ContainerSlot origin)
        {
            heldItem = item;
            originSlot = origin;
            state = InteractionState.HoldingItem;
            HeldItemChanged?.Invoke(heldItem);
        }

        private void Clear()
        {
            heldItem = null;
            originSlot = null;
            state = InteractionState.Idle;
            HeldItemChanged?.Invoke(null);
        }
    }
}