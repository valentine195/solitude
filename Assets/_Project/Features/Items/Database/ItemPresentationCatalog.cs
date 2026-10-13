using System;
using System.Collections.Generic;
using UnityEngine;
using SOLITUDE.Vitals;
namespace SOLITUDE.Items
{
    public sealed class ItemPresentation
    {
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public GameObject PickupPrefab { get; }
        internal ItemPresentation(ItemDefinition item)
        { DisplayName = item.DisplayName; Description = item.Description; Icon = item.Icon; PickupPrefab = item.PickupPrefab; }
    }
    public sealed class ItemPresentationCatalog
    {
        public VitalConsumableCatalog Consumables { get; }
        private readonly Dictionary<string, ItemPresentation> items = new(StringComparer.Ordinal);
        internal ItemPresentationCatalog(IEnumerable<ItemDefinition> definitions, VitalConsumableCatalog consumables)
        { Consumables = consumables; foreach (var item in definitions) items.Add(item.ItemId, new ItemPresentation(item)); }
        public ItemPresentation Resolve(string id) => id != null && items.TryGetValue(id, out var result) ? result : null;
    }
}
