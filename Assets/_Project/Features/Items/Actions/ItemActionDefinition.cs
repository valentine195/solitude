using System;
using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Items
{
    public abstract class ItemActionDefinition : ScriptableObject
    {
        public abstract double GetCommittedDuration(IItemActionConfiguration configuration);
        public abstract IItemActionExecution CreateExecution(ItemActionContext context, IItemActionConfiguration configuration);
        public abstract IItemActionConfiguration Compile(GameplayTagRegistryAsset registry, GameplayTagRegistry compiled);
    }

    [Serializable]
    public sealed class ItemActionBinding
    {
        public ItemActionSlot Slot;
        public ItemActionDefinition Action;
        public ItemActionBinding(ItemActionSlot slot, ItemActionDefinition action) { Slot = slot; Action = action; }
    }
}
