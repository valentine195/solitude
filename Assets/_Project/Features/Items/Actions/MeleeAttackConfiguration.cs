using System;
using SOLITUDE.GameplayTags;
using UnityEngine;

namespace SOLITUDE.Items
{
    [CreateAssetMenu(menuName = "SOLITUDE/Items/Melee Attack Configuration")]
    public sealed class MeleeAttackConfiguration : ScriptableObject
    {
        public float BaseDamage = 10;
        public float RangeTiles = 1.25f;
        public float ArcDegrees = 90;
        public float Windup = .12f;
        public float ActiveDuration = .10f;
        public float Recovery = .33f;
        public float MovementFactor = .65f;
        public float KnockbackTiles = .10f;
        public float KnockbackDuration = .08f;
        public float FlashDuration = .08f;
        public GameplayTagAuthoring DamageTags = new GameplayTagAuthoring();
        public GameplayTagQueryAuthoring TargetEligibility = new GameplayTagQueryAuthoring();
        public AudioClip SwingSound;
        public AudioClip ImpactSound;
        public GameObject ImpactEffect;

        public MeleeAttackSpec Compile(GameplayTagRegistryAsset registry, GameplayTagRegistry compiled)
        {
            Positive(BaseDamage, nameof(BaseDamage)); Positive(RangeTiles, nameof(RangeTiles));
            Positive(ArcDegrees, nameof(ArcDegrees));
            if (ArcDegrees > 360) throw new InvalidOperationException("ArcDegrees must be at most 360.");
            Nonnegative(Windup, nameof(Windup)); Positive(ActiveDuration, nameof(ActiveDuration));
            Nonnegative(Recovery, nameof(Recovery)); Nonnegative(MovementFactor, nameof(MovementFactor));
            if (MovementFactor > 1) throw new InvalidOperationException("MovementFactor must be at most 1.");
            Nonnegative(KnockbackTiles, nameof(KnockbackTiles)); Nonnegative(KnockbackDuration, nameof(KnockbackDuration));
            if (KnockbackTiles > 0) Positive(KnockbackDuration, nameof(KnockbackDuration));
            Nonnegative(FlashDuration, nameof(FlashDuration));
            if (DamageTags == null || TargetEligibility == null) throw new InvalidOperationException("Melee damage tags/query are missing.");
            return new MeleeAttackSpec(this, DamageTags.Compile(registry, compiled), TargetEligibility.Compile(registry, compiled));
        }

        private static void Nonnegative(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new InvalidOperationException(field + " must be finite and nonnegative.");
        }
        private static void Positive(float value, string field)
        { Nonnegative(value, field); if (value == 0) throw new InvalidOperationException(field + " must be positive."); }
    }

    public sealed class MeleeAttackSpec : IItemActionConfiguration
    {
        public float BaseDamage { get; }
        public float RangeTiles { get; }
        public float ArcDegrees { get; }
        public double Windup { get; }
        public double ActiveDuration { get; }
        public double Recovery { get; }
        public double TotalDuration => Windup + ActiveDuration + Recovery;
        public float MovementFactor { get; }
        public float KnockbackTiles { get; }
        public float KnockbackDuration { get; }
        public float FlashDuration { get; }
        public GameplayTagSet DamageTags { get; }
        public GameplayTagQuery TargetEligibility { get; }
        public AudioClip SwingSound { get; }
        public AudioClip ImpactSound { get; }
        public GameObject ImpactEffect { get; }
        internal MeleeAttackSpec(MeleeAttackConfiguration source, GameplayTagSet damageTags, GameplayTagQuery eligibility)
        {
            BaseDamage = source.BaseDamage; RangeTiles = source.RangeTiles; ArcDegrees = source.ArcDegrees;
            Windup = source.Windup; ActiveDuration = source.ActiveDuration; Recovery = source.Recovery;
            MovementFactor = source.MovementFactor; KnockbackTiles = source.KnockbackTiles;
            KnockbackDuration = source.KnockbackDuration; FlashDuration = source.FlashDuration;
            DamageTags = damageTags; TargetEligibility = eligibility;
            SwingSound = source.SwingSound; ImpactSound = source.ImpactSound; ImpactEffect = source.ImpactEffect;
        }
        public float RangeWorld(float unitsPerTile)
        {
            if (float.IsNaN(unitsPerTile) || float.IsInfinity(unitsPerTile) || unitsPerTile <= 0 ||
                float.IsInfinity(RangeTiles * unitsPerTile)) throw new ArgumentOutOfRangeException(nameof(unitsPerTile));
            return RangeTiles * unitsPerTile;
        }
    }
}
