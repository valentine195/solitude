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
        public double Windup = .12;
        public double ActiveDuration = .10;
        public double Recovery = .33;
        public float MovementFactor = .65f;
        public float KnockbackTiles = .10f;
        public float KnockbackDuration = .08f;
        public float FlashDuration = .08f;
        public GameplayTagAuthoring DamageTags = new GameplayTagAuthoring();
        public GameplayTagQueryAuthoring TargetEligibility = new GameplayTagQueryAuthoring();
        [Header("Shared Swing Motion")]
        public float RestAngle = 0;
        public float StartAngle = -45;
        public float EndAngle = 45;
        public float HeldDistanceTiles = .3f;
        public LayerMask TargetLayers = Physics2D.DefaultRaycastLayers;
        public LayerMask BlockingLayers = Physics2D.DefaultRaycastLayers;

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
            Finite(RestAngle, nameof(RestAngle));
            Finite(StartAngle, nameof(StartAngle)); Finite(EndAngle, nameof(EndAngle));
            Nonnegative(HeldDistanceTiles, nameof(HeldDistanceTiles));
            if (DamageTags == null || TargetEligibility == null) throw new InvalidOperationException("Melee damage tags/query are missing.");
            return new MeleeAttackSpec(this, DamageTags.Compile(registry, compiled), TargetEligibility.Compile(registry, compiled));
        }

        private static void Nonnegative(double value, string field)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new InvalidOperationException(field + " must be finite and nonnegative.");
        }
        private static void Finite(float value, string field)
        { if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidOperationException(field + " must be finite."); }
        private static void Positive(double value, string field)
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
        public float RestAngle { get; }
        public float StartAngle { get; }
        public float EndAngle { get; }
        public float HeldDistanceTiles { get; }
        public int TargetLayers { get; }
        public int BlockingLayers { get; }
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
            RestAngle = source.RestAngle; StartAngle = source.StartAngle;
            EndAngle = source.EndAngle; HeldDistanceTiles = source.HeldDistanceTiles;
            TargetLayers = source.TargetLayers; BlockingLayers = source.BlockingLayers;
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
