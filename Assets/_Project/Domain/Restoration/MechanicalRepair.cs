using System;
using SOLITUDE.GameplayTags;

namespace SOLITUDE.Restoration
{
    public enum MechanicalRepairOutcome { Committed, AlreadyRepaired, MissingTool, MissingPart, Unavailable }

    /// <summary>Immutable authored requirement compiled against the session capability registry.</summary>
    public sealed class MechanicalRepairRequirement
    {
        public string TargetId { get; }
        public string PartItemId { get; }
        public GameplayTagQuery ToolQuery { get; }
        public MechanicalRepairRequirement(string targetId, string partItemId, GameplayTagRegistry registry, GameplayTag tool)
        {
            if (string.IsNullOrWhiteSpace(targetId) || targetId != targetId.Trim() ||
                string.IsNullOrWhiteSpace(partItemId) || partItemId != partItemId.Trim())
                throw new ArgumentException("Repair identity and part identity are required.");
            if (tool == null || (tool.Name != "Tool.Mechanical" && !tool.Name.StartsWith("Tool.Mechanical.", StringComparison.Ordinal)))
                throw new ArgumentException("A registered mechanical tool requirement is required.");
            TargetId = targetId; PartItemId = partItemId;
            ToolQuery = new GameplayTagQuery(registry, GameplayTagMatch.Hierarchical, required: new[] { tool });
        }
    }
}
