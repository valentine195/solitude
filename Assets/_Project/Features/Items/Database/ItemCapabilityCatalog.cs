using System;
using System.Collections.Generic;
using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Items
{
    public sealed class CompiledItemAction
    {
        public ItemActionSlot Slot { get; }
        public ItemActionDefinition Definition { get; }
        public IItemActionConfiguration Configuration { get; }
        internal CompiledItemAction(ItemActionSlot slot, ItemActionDefinition definition, IItemActionConfiguration configuration)
        { Slot = slot; Definition = definition; Configuration = configuration; }
    }

    public sealed class ItemCapabilities
    {
        public string ItemId { get; }
        public GameplayTagSet Tags { get; }
        public Sprite HeldSprite { get; }
        public IReadOnlyList<CompiledItemAction> Actions { get; }
        internal ItemCapabilities(ItemDefinition item, GameplayTagSet tags, CompiledItemAction[] actions)
        { ItemId = item.ItemId; Tags = tags; HeldSprite = item.HeldSprite; Actions = Array.AsReadOnly(actions); }
        public CompiledItemAction Resolve(ItemActionSlot slot)
        {
            for (int i = 0; i < Actions.Count; i++) if (Actions[i].Slot == slot) return Actions[i];
            return null;
        }
    }

    /// <summary>Session-owned immutable snapshots, alongside inventory and presentation catalogs.</summary>
    public sealed class ItemCapabilityCatalog
    {
        private readonly Dictionary<string, ItemCapabilities> items = new(StringComparer.Ordinal);
        public GameplayTagRegistry Registry { get; }
        internal ItemCapabilityCatalog(IEnumerable<ItemDefinition> definitions, GameplayTagRegistryAsset registry,
            GameplayTagRegistry compiled)
        {
            Registry = compiled ?? throw new ArgumentNullException(nameof(compiled));
            foreach (var item in definitions)
            {
                if (item.GameplayTags == null || item.Actions == null)
                    throw new InvalidOperationException("Item tag/action assignments are missing: " + item.name);
                if (registry == null && (item.GameplayTags.Length != 0 || item.Actions.Length != 0))
                    throw new InvalidOperationException("Item capabilities require a gameplay tag registry: " + item.name);
                var tags = new GameplayTag[item.GameplayTags.Length];
                for (int i = 0; i < tags.Length; i++) tags[i] = registry.Resolve(item.GameplayTags[i], compiled);
                var actions = new CompiledItemAction[item.Actions.Length];
                var slots = new HashSet<ItemActionSlot>();
                for (int i = 0; i < actions.Length; i++)
                {
                    var binding = item.Actions[i];
                    if (binding == null || binding.Action == null || !Enum.IsDefined(typeof(ItemActionSlot), binding.Slot) || !slots.Add(binding.Slot))
                        throw new InvalidOperationException("Missing, invalid or duplicate action slot: " + item.name);
                    var configuration = binding.Action.Compile(registry, compiled);
                    if (configuration == null) throw new InvalidOperationException("Action compiled no configuration: " + binding.Action.name);
                    actions[i] = new CompiledItemAction(binding.Slot, binding.Action, configuration);
                }
                items.Add(item.ItemId, new ItemCapabilities(item, new GameplayTagSet(compiled, tags), actions));
            }
        }
        public ItemCapabilities Resolve(string itemId) => itemId != null && items.TryGetValue(itemId, out var item) ? item : null;
    }
}
