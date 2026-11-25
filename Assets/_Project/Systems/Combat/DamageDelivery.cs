using System;
using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Combat
{
    // One immutable registry snapshot per composition; no geometry or weapon dispatch.
    public sealed class DamageDelivery
    {
        private readonly GameplayTagSet untagged;
        public DamageDelivery(GameplayTagRegistry registry)
        { untagged = (registry ?? throw new ArgumentNullException(nameof(registry))).EmptySet; }

        public DamageResult Deliver(in DamageContext context, GameplayTagQuery eligibility = null)
        {
            if (!IsValid(context)) return new DamageResult(DamageOutcome.InvalidContext);
            if (!context.Target.IsAvailable) return new DamageResult(DamageOutcome.Unavailable);
            if (eligibility != null)
            {
                try
                {
                    if (!eligibility.Matches(context.Target.Tags ?? untagged))
                        return new DamageResult(DamageOutcome.Ineligible);
                }
                catch (ArgumentException) { return new DamageResult(DamageOutcome.InvalidContext); }
            }
            double amount = context.BaseAmount;
            if (context.SourceAction is SOLITUDE.Items.MeleeAttackAction &&
                context.Target.Entity.TryGetComponent<EnemyMeleeResistance>(out var resistance) && resistance.isActiveAndEnabled)
            {
                amount = resistance.ReceivedDamage(amount);
                if (amount == 0) return new DamageResult(DamageOutcome.Resisted);
            }
            return context.Target.ApplyDamage(context, amount);
        }

        public static bool Positive(double amount) => !double.IsNaN(amount) && !double.IsInfinity(amount) && amount > 0;
        private static bool Finite(Vector2 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        public static bool IsValid(in DamageContext context) => Positive(context.BaseAmount)
            && context.Attacker != null && context.Target != null && context.Target.Entity != null
            && !string.IsNullOrWhiteSpace(context.SourceItemId) && context.SourceAction != null && context.DamageTags != null
            && Finite(context.ImpactPosition) && Finite(context.Direction)
            && Positive(context.Direction.sqrMagnitude);
    }
}
