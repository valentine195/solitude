using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.Items;

namespace SOLITUDE.Items
{
    public abstract class InteractableItem : InteractableBase
    {
        [SerializeField]
        private ItemDefinition itemDefinition;

        public abstract string SuccessMessage();

        public override InteractionResult Interact(PlayerInteractor player)

        {
            var inventory = player.GetComponentInChildren<PlayerInventory>();
            if (inventory.Add(itemDefinition))
            {
                Destroy(gameObject);
                return InteractionResult.Success(SuccessMessage());
            }
            else
            {
                return InteractionResult.Blocked("Inventory full");
            }

        }
    }
}