using System;

namespace SOLITUDE.Vitals
{
    /// <summary>Pure rules: callers supply current authoritative values and commit prepared results.</summary>
    public static class VitalsRecoveryRules
    {
        // One part in ten million of maximum health (0.00001 HP at 100 HP) accommodates
        // the runtime's float values, including thirds. Within this band a boundary is
        // treated as reached; it never grants healing toward the next segment, nor
        // lowers a supplied value just above it. Zero is never snapped from positive health.
        public const double BoundaryRelativeTolerance = 1e-7;

        public static VitalsRecoveryResult Initialize(VitalsRecoverySettings settings, VitalsValues authored)
        {
            if (settings == null || !settings.IsValid || !VitalsRecoverySettings.Finite(authored.Health)
                || !VitalsRecoverySettings.Finite(authored.Hunger))
                return Result(VitalsRecoveryOutcome.Rejected, settings, authored, default, false);
            return Result(VitalsRecoveryOutcome.Applied, settings,
                new VitalsValues(Clamp(authored.Health, settings.MaxHealth), Clamp(authored.Hunger, settings.MaxHunger)),
                default, false);
        }

        public static VitalsRecoveryResult Damage(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, double amount, bool gameplayEligible = true)
        {
            if (!Valid(settings, values, state) || !VitalsRecoverySettings.Positive(amount))
                return Result(VitalsRecoveryOutcome.Rejected, settings, values, state, gameplayEligible);
            double health = Math.Max(0, values.Health - amount);
            if (health == values.Health)
                return Result(VitalsRecoveryOutcome.Rejected, settings, values, state, gameplayEligible);
            return Result(VitalsRecoveryOutcome.Applied, settings, new VitalsValues(health, values.Hunger),
                new VitalsRecoveryState(settings.DamageDelay), gameplayEligible);
        }

        public static VitalsRecoveryResult Advance(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, double elapsedSeconds, bool gameplayEligible)
        {
            if (!Valid(settings, values, state) || !VitalsRecoverySettings.Nonnegative(elapsedSeconds))
                return Result(VitalsRecoveryOutcome.Rejected, settings, values, state, gameplayEligible);
            if (!gameplayEligible || elapsedSeconds == 0)
                return Result(VitalsRecoveryOutcome.Unchanged, settings, values, state, gameplayEligible);

            double hunger = Math.Max(0, values.Hunger - settings.HungerDepletionRate * elapsedSeconds);
            double delay = Math.Max(0, state.DelayRemaining - elapsedSeconds);
            double health = values.Health;
            double ceiling = Ceiling(settings, health);

            // Recovery time is the overlap of [delay expiry, interval end] and fed time.
            // Counting either condition only at interval start would over-heal large frames.
            double fedSeconds = values.Hunger < settings.FedThreshold ? 0
                : settings.HungerDepletionRate == 0 || settings.FedThreshold == 0 ? elapsedSeconds
                : Math.Min(elapsedSeconds, Math.Max(0,
                    (values.Hunger - settings.FedThreshold) / settings.HungerDepletionRate));
            double recoverySeconds = Math.Max(0, fedSeconds - state.DelayRemaining);
            if (health > 0 && ceiling > health && recoverySeconds > 0)
                health = Math.Min(ceiling, health + settings.RecoveryRate * recoverySeconds);

            var next = new VitalsValues(health, hunger);
            var timing = new VitalsRecoveryState(delay);
            var outcome = health != values.Health || hunger != values.Hunger || delay != state.DelayRemaining
                ? VitalsRecoveryOutcome.Applied : VitalsRecoveryOutcome.Unchanged;
            return Result(outcome, settings, next, timing, gameplayEligible);
        }

        public static VitalsRecoveryResult RestoreFood(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, double amount, bool gameplayEligible = true)
            => Restore(settings, values, state, amount, true, gameplayEligible);

        public static VitalsRecoveryResult RestoreHealth(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, double amount, bool gameplayEligible = true)
            => Restore(settings, values, state, amount, false, gameplayEligible);

        private static VitalsRecoveryResult Restore(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, double amount, bool food, bool gameplayEligible)
        {
            if (!Valid(settings, values, state) || !VitalsRecoverySettings.Positive(amount)
                || !gameplayEligible || values.Health <= 0)
                return Result(VitalsRecoveryOutcome.Rejected, settings, values, state, gameplayEligible);
            double before = food ? values.Hunger : values.Health;
            double after = Math.Min(food ? settings.MaxHunger : settings.MaxHealth, before + amount);
            if (after == before)
                return Result(VitalsRecoveryOutcome.Rejected, settings, values, state, gameplayEligible);
            var next = food ? new VitalsValues(values.Health, after) : new VitalsValues(after, values.Hunger);
            return Result(VitalsRecoveryOutcome.Applied, settings, next, state, gameplayEligible);
        }

        public static RecoveryReason Describe(VitalsRecoverySettings settings, VitalsValues values,
            VitalsRecoveryState state, bool gameplayEligible)
        {
            if (!Valid(settings, values, state) || values.Health <= 0) return RecoveryReason.Unavailable;
            if (Ceiling(settings, values.Health) <= values.Health) return RecoveryReason.AtCeiling;
            if (!gameplayEligible) return RecoveryReason.Paused;
            if (values.Hunger < settings.FedThreshold) return RecoveryReason.Hungry;
            if (state.DelayRemaining > 0) return RecoveryReason.Delayed;
            return RecoveryReason.Recovering;
        }

        public static double Ceiling(VitalsRecoverySettings settings, double health)
        {
            if (settings == null || !settings.IsValid || !VitalsRecoverySettings.Nonnegative(health)
                || health > settings.MaxHealth) return 0;
            if (health == 0) return 0;
            double position = health / settings.MaxHealth * settings.SegmentCount;
            double nearestIndex = Math.Round(position);
            double nearest = settings.MaxHealth * (nearestIndex / settings.SegmentCount);
            if (nearestIndex > 0 && Math.Abs(health - nearest) <= settings.MaxHealth * BoundaryRelativeTolerance)
                return health;
            // A positive subnormal health / maximum can underflow to zero; it is
            // still living health and belongs to the first segment, not depleted.
            return settings.MaxHealth * (Math.Max(1, Math.Ceiling(position)) / settings.SegmentCount);
        }

        private static bool Valid(VitalsRecoverySettings settings, VitalsValues values, VitalsRecoveryState state)
            => settings != null && settings.IsValid
                && VitalsRecoverySettings.Nonnegative(values.Health) && values.Health <= settings.MaxHealth
                && VitalsRecoverySettings.Nonnegative(values.Hunger) && values.Hunger <= settings.MaxHunger
                && VitalsRecoverySettings.Nonnegative(state.DelayRemaining);

        private static double Clamp(double value, double maximum) => Math.Max(0, Math.Min(maximum, value));

        private static VitalsRecoveryResult Result(VitalsRecoveryOutcome outcome, VitalsRecoverySettings settings,
            VitalsValues values, VitalsRecoveryState state, bool gameplayEligible)
            => new VitalsRecoveryResult(outcome, values, state, Describe(settings, values, state, gameplayEligible),
                Valid(settings, values, state) ? Ceiling(settings, values.Health) : 0);
    }
}
