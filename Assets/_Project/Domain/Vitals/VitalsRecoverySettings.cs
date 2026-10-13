using System;

namespace SOLITUDE.Vitals
{
    /// <summary>Independent, immutable authoring values; invalid settings remain visibly invalid.</summary>
    public sealed class VitalsRecoverySettings
    {
        public double MaxHealth { get; }
        public double MaxHunger { get; }
        public int SegmentCount { get; }
        public double FedThresholdFraction { get; }
        public double HungerDepletionRate { get; }
        public double DamageDelay { get; }
        public double RecoveryRate { get; }
        public double FedThreshold => MaxHunger * FedThresholdFraction;

        public VitalsRecoverySettings(double maxHealth = 100, double maxHunger = 100,
            int segmentCount = 4, double fedThresholdFraction = .2,
            double hungerDepletionRate = .1, double damageDelay = 5, double recoveryRate = 2)
        {
            MaxHealth = maxHealth;
            MaxHunger = maxHunger;
            SegmentCount = segmentCount;
            FedThresholdFraction = fedThresholdFraction;
            HungerDepletionRate = hungerDepletionRate;
            DamageDelay = damageDelay;
            RecoveryRate = recoveryRate;
        }

        public bool IsValid => Positive(MaxHealth) && Positive(MaxHunger) && SegmentCount > 0
            && Finite(FedThresholdFraction) && FedThresholdFraction >= 0 && FedThresholdFraction <= 1
            && Nonnegative(HungerDepletionRate) && Nonnegative(DamageDelay) && Nonnegative(RecoveryRate);

        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static bool Positive(double value) => Finite(value) && value > 0;
        internal static bool Nonnegative(double value) => Finite(value) && value >= 0;
    }
}
