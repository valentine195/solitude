using System;
using System.Collections.Generic;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Items
{
    public enum CollectResult { Collected, AlreadyCollected, InventoryFull, InvalidPickup, NotReady }

    /// <summary>Immutable reward copied from authoring data when the scene is bound.</summary>
    public readonly struct PickupReward
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public PickupReward(string itemId, int quantity) { ItemId = itemId; Quantity = quantity; }
    }

    public sealed class PickupCollectionService
    {
        private readonly ContainerSaveSession session;
        private readonly Dictionary<string, Registration> pickups = new(StringComparer.Ordinal);
        private readonly HashSet<string> conflicts = new(StringComparer.Ordinal);
        public PickupCollectionService(ContainerSaveSession session) =>
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        public bool IsCollected(string id) => session.IsPickupCollected(id);
        public bool IsValid(string id) => id != null && pickups.ContainsKey(id) && !conflicts.Contains(id);

        public IDisposable Register(string id, PickupReward reward)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(reward.ItemId) || reward.Quantity < 1)
                throw new ArgumentException("A pickup needs an instance identity and a valid reward.");
            if (pickups.ContainsKey(id))
            {
                conflicts.Add(id); // Both owners are unavailable until the scene is rebound.
                throw new InvalidOperationException($"Duplicate pickup identity '{id}'.");
            }
            var registration = new Registration(this, id, reward);
            pickups.Add(id, registration);
            return registration;
        }

        public CollectResult TryCollect(string pickupId, string inventoryId, string itemId, int quantity)
        {
            if (!IsValid(pickupId) || string.IsNullOrWhiteSpace(inventoryId)) return CollectResult.InvalidPickup;
            var reward = pickups[pickupId].Reward;
            if (reward.ItemId != itemId || reward.Quantity != quantity) return CollectResult.InvalidPickup;
            return session.Collect(pickupId, inventoryId, itemId, quantity);
        }

        private sealed class Registration : IDisposable
        {
            private PickupCollectionService owner;
            private readonly string id;
            public PickupReward Reward { get; }
            public Registration(PickupCollectionService owner, string id, PickupReward reward)
            { this.owner = owner; this.id = id; Reward = reward; }
            public void Dispose()
            {
                if (owner == null) return;
                owner.pickups.Remove(id);
                owner.conflicts.Remove(id);
                owner = null;
            }
        }
    }
}
