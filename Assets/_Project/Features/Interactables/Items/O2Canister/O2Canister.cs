using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Player;

namespace SOLITUDE.Features.Interactables.Items
{
    public class O2Canister : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ItemDefinition itemDefinition;
        public string GetPrompt() => "Pick up O2 Canister";

        public bool CanInteract(PlayerInteractor player) => true;

        public InteractionResult Interact(PlayerInteractor player)

        {
            var inventory = player.GetComponentInChildren<PlayerInventory>();
            inventory.Add(itemDefinition);

            Destroy(gameObject);

            return InteractionResult.Success("Picked up O2 Canister");
        }
    }
}