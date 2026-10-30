using System;
using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Items
{
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Melee Attack Action")]
    public sealed class MeleeAttackAction : ItemActionDefinition
    {
        public MeleeAttackConfiguration Configuration;
        public override double GetCommittedDuration(IItemActionConfiguration configuration) => ((MeleeAttackSpec)configuration).TotalDuration;
        public override IItemActionExecution CreateExecution(ItemActionContext context, IItemActionConfiguration configuration)
            => new SOLITUDE.Combat.MeleeAttackExecution(context, (MeleeAttackSpec)configuration);
        public override IItemActionConfiguration Compile(GameplayTagRegistryAsset registry, GameplayTagRegistry compiled)
        {
            if (Configuration == null) throw new InvalidOperationException("Melee action requires an attack configuration: " + name);
            return Configuration.Compile(registry, compiled);
        }
    }
}
