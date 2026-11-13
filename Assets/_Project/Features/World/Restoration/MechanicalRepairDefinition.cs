using System;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using UnityEngine;

namespace SOLITUDE.World.Restoration
{
    [CreateAssetMenu(menuName = "SOLITUDE/Restoration/Mechanical Repair")]
    public sealed class MechanicalRepairDefinition : ScriptableObject
    {
        [SerializeField] private GameplayTagRegistryAsset registry;
        [SerializeField] private GameplayTagDefinition toolRequirement;
        [SerializeField] private ItemDefinition part;
        public ItemDefinition Part => part;
        public void Configure(GameplayTagRegistryAsset registry, GameplayTagDefinition toolRequirement, ItemDefinition part)
        { this.registry = registry; this.toolRequirement = toolRequirement; this.part = part; }
        public MechanicalRepairRequirement Compile(string targetId, GameplayTagRegistry compiled)
        {
            if (registry == null || part == null || toolRequirement == null) throw new InvalidOperationException("Repair requirements are incomplete.");
            return new MechanicalRepairRequirement(targetId, part.ItemId, compiled, registry.Resolve(toolRequirement, compiled));
        }
        public void ValidateAuthoring() => Compile("authoring.validation", registry != null ? registry.Compile() : null);
    }
}
