using System;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEngine;

namespace SOLITUDE.Items
{
    [RequireComponent(typeof(SaveableId))]
    public sealed class WorldPickup : InteractableBase
    {
        [SerializeField] private PickupDefinition definition;
        private PickupCollectionService collection;
        private IDisposable binding;
        private string pickupId;
        private PickupReward reward;
        public PickupDefinition Definition => definition;
        public string PickupId => GetComponent<SaveableId>()?.Value;

        public bool Initialize(PickupCollectionService service)
        {
            if (binding != null && ReferenceEquals(collection, service)) return true;
            binding?.Dispose();
            binding = null;
            collection = null;
            pickupId = PickupId;
            if (service == null || definition?.Item == null)
            {
                Debug.LogError($"[WorldPickup] '{name}' has no session or pickup definition.", this);
                return false;
            }
            reward = new PickupReward(definition.Item.ItemId, definition.Quantity);
            try
            {
                binding = service.Register(pickupId, reward);
                collection = service;
                if (service.IsCollected(pickupId)) gameObject.SetActive(false);
                return true;
            }
            catch (Exception exception) { Debug.LogError($"[WorldPickup] {exception.Message}", this); return false; }
        }

        public override bool CanInteract(PlayerInteractor player) =>
            collection != null && collection.IsValid(pickupId) && !collection.IsCollected(pickupId);

        public override InteractionResult Interact(PlayerInteractor player)
        {
            if (collection == null) return InteractionResult.Blocked("Pickup unavailable");
            var result = collection.TryCollect(pickupId, ContainerSaveSession.PlayerInventoryId,
                reward.ItemId, reward.Quantity);
            if (result == CollectResult.Collected || result == CollectResult.AlreadyCollected)
            {
                gameObject.SetActive(false);
                return InteractionResult.Success(result == CollectResult.Collected ? definition.SuccessText : null);
            }
            return InteractionResult.Blocked(result == CollectResult.InventoryFull ? "Inventory full" : "Pickup unavailable");
        }

        public void ReleaseBinding()
        {
            binding?.Dispose();
            binding = null;
            collection = null;
        }

        private void OnDestroy() => ReleaseBinding();
    }
}
