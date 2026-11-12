using System;
using System.Collections.Generic;
using SOLITUDE.Application;
using UnityEngine;
namespace SOLITUDE.Items
{
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Starting Loadout")]
    public sealed class StartingLoadoutDefinition : ScriptableObject
    {
        [Serializable] public sealed class Grant
        { public ItemDefinition Item; public int Quantity = 1; }
        public Grant[] Hotbar = Array.Empty<Grant>();
        public int SelectedSlot;
        public StartingLoadoutSpec Compile(IItemCatalog catalog)
        {
            if (Hotbar == null) throw new InvalidOperationException("Starting loadout grants are missing.");
            var grants = new List<PickupReward>();
            foreach (var grant in Hotbar)
            {
                if (grant?.Item == null) throw new InvalidOperationException("Starting loadout item reference is missing.");
                grants.Add(new PickupReward(grant.Item.ItemId, grant.Quantity));
            }
            return new StartingLoadoutSpec(catalog, grants, SelectedSlot);
        }
    }
}
