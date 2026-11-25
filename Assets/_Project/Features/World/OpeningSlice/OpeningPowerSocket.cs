using System.Linq;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.World.SolitudeStart;
using UnityEngine;
namespace SOLITUDE.World.OpeningSlice
{
    // Scene-local bridge: reads existing inventory and delegates animation/collision to the existing door.
    public sealed class OpeningPowerSocket : InteractableBase
    {
        public SolitudeSlidingDoor door;
        public string batteryItemId;
        public bool Powered { get; private set; }
        public override string GetPrompt() => Powered ? "Door powered" : "Connect battery to door";
        public override bool CanInteract(PlayerInteractor player) => !Powered;
        public override InteractionResult Interact(PlayerInteractor player)
        {
            var inventory = player.GetComponentInChildren<PlayerInventory>();
            bool battery = inventory?.Container?.GetSlots().Any(s => !s.IsEmpty && s.Stack.ItemId == batteryItemId) == true;
            if (!battery) return InteractionResult.Blocked("No power. Find a battery by the service locker.");
            if (door == null) return InteractionResult.Blocked("Door unavailable");
            var result = door.Interact(player);
            Powered = true;
            return InteractionResult.Success("Auxiliary power restored. Door opening.");
        }
    }
}
