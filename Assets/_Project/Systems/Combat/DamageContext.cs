using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using UnityEngine;

namespace SOLITUDE.Combat
{
    public readonly struct DamageContext
    {
        public double BaseAmount { get; }
        public GameObject Attacker { get; }
        public IDamageRecipient Target { get; }
        public string SourceItemId { get; }
        public ItemActionDefinition SourceAction { get; }
        public Vector2 ImpactPosition { get; }
        public Vector2 Direction { get; }
        public GameplayTagSet DamageTags { get; }

        public DamageContext(double baseAmount, GameObject attacker, IDamageRecipient target,
            string sourceItemId, ItemActionDefinition sourceAction, Vector2 impactPosition,
            Vector2 direction, GameplayTagSet damageTags)
        {
            BaseAmount = baseAmount; Attacker = attacker; Target = target;
            SourceItemId = sourceItemId; SourceAction = sourceAction;
            ImpactPosition = impactPosition; Direction = direction; DamageTags = damageTags;
        }
    }

    public enum DamageOutcome { InvalidContext, Unavailable, Ineligible, Rejected, Applied }

    public readonly struct DamageResult
    {
        public DamageOutcome Outcome { get; }
        public double AppliedAmount { get; }
        public bool WasApplied => Outcome == DamageOutcome.Applied;
        public DamageResult(DamageOutcome outcome, double appliedAmount = 0)
        { Outcome = outcome; AppliedAmount = appliedAmount; }
    }
}
