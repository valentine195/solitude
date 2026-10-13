using System;
using NUnit.Framework;
using SOLITUDE.Vitals;

namespace SOLITUDE.Tests
{
    public class VitalsRecoveryRulesTests
    {
        private static readonly VitalsRecoverySettings Defaults = new VitalsRecoverySettings();
        private const double Epsilon = 1e-8;

        private static void Values(VitalsRecoveryResult result, double health, double hunger, double delay)
        {
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(health, result.Values.Health, Epsilon);
            Assert.AreEqual(hunger, result.Values.Hunger, Epsilon);
            Assert.AreEqual(delay, result.State.DelayRemaining, Epsilon);
        }

        // S1, S11: authored values clamp, while malformed configuration stays invalid.
        [Test] public void InitializationClampsFiniteAuthoredValuesAndClearsTiming()
        {
            Assert.IsFalse(default(VitalsRecoveryResult).Accepted, "An unprepared result cannot authorize a mutation.");
            Values(VitalsRecoveryRules.Initialize(Defaults, new VitalsValues(-4, 120)), 0, 100, 0);
            Values(VitalsRecoveryRules.Initialize(Defaults, new VitalsValues(140, -4)), 100, 0, 0);
            Assert.IsFalse(VitalsRecoveryRules.Initialize(Defaults, new VitalsValues(double.NaN, 80)).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.Initialize(null, new VitalsValues(30, 80)).Accepted);
        }

        [Test] public void HungerDepletesAtFullHealthAndClampsWithoutDamagingHealth()
        {
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(100, 100), default, 10, true), 100, 99, 0);
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(100, .1), default, 100, true), 100, 0, 0);
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(0, .1), default, 100, true), 0, 0, 0);
        }

        [Test] public void IneligibleOrZeroTimeDoesNotDepleteOrSpendDelayAndNeverCatchesUp()
        {
            var values = new VitalsValues(30, 80); var state = new VitalsRecoveryState(5);
            Values(VitalsRecoveryRules.Advance(Defaults, values, state, 1000, false), 30, 80, 5);
            Values(VitalsRecoveryRules.Advance(Defaults, values, state, 0, true), 30, 80, 5);
            Values(VitalsRecoveryRules.Advance(Defaults, values, state, 1, true), 30, 79.9, 4);
        }

        // S2: boundary values are data examples, not a reproduction of ceiling implementation.
        [TestCase(0d, 0d)] [TestCase(24d, 25d)] [TestCase(25d, 25d)]
        [TestCase(26d, 50d)] [TestCase(30d, 50d)] [TestCase(50d, 50d)]
        [TestCase(74d, 75d)] [TestCase(100d, 100d)]
        public void FourSegmentsHaveTheSpecifiedRecoveryCeilings(double health, double ceiling)
            => Assert.AreEqual(ceiling, VitalsRecoveryRules.Ceiling(Defaults, health), Epsilon);

        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void BelowOnAndAboveAnInternalBoundaryNeverCreepAcrossIt(int count)
        {
            var settings = new VitalsRecoverySettings(segmentCount: count, hungerDepletionRate: 0);
            double boundary = 100d / count;
            double tolerance = 100 * VitalsRecoveryRules.BoundaryRelativeTolerance;
            var below = new VitalsValues(boundary - tolerance * 2, 100);
            var on = new VitalsValues(boundary, 100);
            var above = new VitalsValues(boundary + tolerance * 2, 100);
            Assert.AreEqual(boundary, VitalsRecoveryRules.Ceiling(settings, below.Health), Epsilon);
            Assert.AreEqual(on.Health, VitalsRecoveryRules.Ceiling(settings, on.Health), Epsilon);
            Assert.AreEqual(2 * boundary, VitalsRecoveryRules.Ceiling(settings, above.Health), Epsilon);
            var step = VitalsRecoveryRules.Advance(settings, below, default, 1000, true);
            Values(step, boundary, 100, 0);
            for (int i = 0; i < 100; i++) step = VitalsRecoveryRules.Advance(settings, step.Values, step.State, .01, true);
            Values(step, boundary, 100, 0);
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void FloatRoundedBoundariesAreReachedWithoutGrantingAnotherSegment(int count)
        {
            var settings = new VitalsRecoverySettings(segmentCount: count, hungerDepletionRate: 0);
            double rounded = (float)(100d / count);
            var result = VitalsRecoveryRules.Advance(settings, new VitalsValues(rounded, 100), default, 1000, true);
            Values(result, rounded, 100, 0);
            Assert.AreEqual(RecoveryReason.AtCeiling, result.Reason);
        }

        [Test] public void TinyPositiveHealthIsNotSnappedToZeroAndCanRecover()
        {
            foreach (double health in new[] { 1e-10, double.Epsilon })
            {
                var result = VitalsRecoveryRules.Advance(Defaults, new VitalsValues(health, 100), default, 1, true);
                Assert.Greater(result.Values.Health, 1);
            }
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(0, 100), default, 100, true), 0, 90, 0);
        }

        // S3: damage is explicit; ordinary initialization/restoration cannot reset its delay.
        [Test] public void ActualDamageStartsAndRestartsDelayAndRejectsDamageAtZero()
        {
            var hit = VitalsRecoveryRules.Damage(Defaults, new VitalsValues(50, 100), default, 10);
            Values(hit, 40, 100, 5);
            var elapsed = VitalsRecoveryRules.Advance(Defaults, hit.Values, hit.State, 2, true);
            var repeat = VitalsRecoveryRules.Damage(Defaults, elapsed.Values, elapsed.State, 1);
            Values(repeat, 39, 99.8, 5);
            Values(VitalsRecoveryRules.Damage(Defaults, repeat.Values, repeat.State, 1000), 0, 99.8, 5);
            var zero = VitalsRecoveryRules.Damage(Defaults, new VitalsValues(0, 70), new VitalsRecoveryState(2), 1);
            Assert.IsFalse(zero.Accepted); Assert.AreEqual(2, zero.State.DelayRemaining);
        }

        [TestCase(0d)] [TestCase(-1d)] [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidDamageAndRestorationPreserveAllInputs(double amount)
        {
            var values = new VitalsValues(30, 50); var state = new VitalsRecoveryState(2);
            foreach (var result in new[] { VitalsRecoveryRules.Damage(Defaults, values, state, amount),
                VitalsRecoveryRules.RestoreFood(Defaults, values, state, amount),
                VitalsRecoveryRules.RestoreHealth(Defaults, values, state, amount) })
            {
                Assert.IsFalse(result.Accepted); Assert.AreEqual(30, result.Values.Health);
                Assert.AreEqual(50, result.Values.Hunger); Assert.AreEqual(2, result.State.DelayRemaining);
            }
        }

        // S4, S5: delay/fed time overlap must be evaluated inside each advance.
        [Test] public void DelayExpiryWithinAdvanceSpendsOnlyRemainingEligibleTime()
            => Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 100),
                new VitalsRecoveryState(5), 6, true), 32, 99.4, 0);

        [Test] public void ThresholdCrossingWithinAdvanceSpendsOnlyFedTime()
        {
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 20.1), default, 2, true), 32, 19.9, 0);
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 20.1), new VitalsRecoveryState(.5), 2, true), 31, 19.9, 0);
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 20.1), new VitalsRecoveryState(1.5), 2, true), 30, 19.9, 0);
        }

        [Test] public void ThresholdEqualityIsFedButDepletionLeavesNoPositiveFedInterval()
        {
            var values = new VitalsValues(30, 20);
            Assert.AreEqual(RecoveryReason.Recovering, VitalsRecoveryRules.Describe(Defaults, values, default, true));
            Values(VitalsRecoveryRules.Advance(Defaults, values, default, 1, true), 30, 19.9, 0);
            var fixedHunger = new VitalsRecoverySettings(hungerDepletionRate: 0);
            Values(VitalsRecoveryRules.Advance(fixedHunger, values, default, 1, true), 32, 20, 0);
        }

        [Test] public void HungerBlocksHealingWhileDelayContinuesAndFoodDoesNotEraseIt()
        {
            var hungry = VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 0), new VitalsRecoveryState(5), 2, true);
            Values(hungry, 30, 0, 3);
            var food = VitalsRecoveryRules.RestoreFood(Defaults, hungry.Values, hungry.State, 25);
            Values(food, 30, 25, 3);
            Values(VitalsRecoveryRules.Advance(Defaults, food.Values, food.State, 4, true), 32, 24.6, 0);
            var expired = VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 0), new VitalsRecoveryState(5), 10, true);
            var fed = VitalsRecoveryRules.RestoreFood(Defaults, expired.Values, expired.State, 25);
            Values(VitalsRecoveryRules.Advance(Defaults, fed.Values, fed.State, 1, true), 32, 24.9, 0);
        }

        [Test] public void FoodAndMedicineClampOnlyTheirTargetAndKeepDelay()
        {
            Values(VitalsRecoveryRules.RestoreFood(Defaults, new VitalsValues(30, 90), new VitalsRecoveryState(3), 25), 30, 100, 3);
            Values(VitalsRecoveryRules.RestoreHealth(Defaults, new VitalsValues(90, 0), new VitalsRecoveryState(3), 25), 100, 0, 3);
            var medicine = VitalsRecoveryRules.RestoreHealth(Defaults, new VitalsValues(30, 0), new VitalsRecoveryState(3), 25);
            Values(medicine, 55, 0, 3); Assert.AreEqual(75, medicine.RecoveryCeiling);
            Assert.AreEqual(RecoveryReason.Hungry, medicine.Reason);
        }

        [Test] public void FullTargetsZeroHealthAndIneligibleRestorationAreRejected()
        {
            Assert.IsFalse(VitalsRecoveryRules.RestoreFood(Defaults, new VitalsValues(30, 100), default, 25).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.RestoreHealth(Defaults, new VitalsValues(100, 0), default, 25).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.RestoreFood(Defaults, new VitalsValues(0, 0), default, 25).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.RestoreHealth(Defaults, new VitalsValues(0, 0), default, 25).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.RestoreFood(Defaults, new VitalsValues(30, 0), default, 25, false).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.RestoreHealth(Defaults, new VitalsValues(30, 0), default, 25, false).Accepted);
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void LongAndSubdividedIntervalsMatchWithDelayThresholdAndCeilingLimits(int count)
        {
            var settings = new VitalsRecoverySettings(segmentCount: count);
            foreach (double hunger in new[] { 100d, 20.1, 20.7, 20d, 0d })
            foreach (double health in new[] { 24d, 30d, 34d, 49.999, 50d, 74d, 100d, 0d })
            foreach (double delay in new[] { 0d, .5, 5d })
            {
                var values = new VitalsValues(health, hunger); var state = new VitalsRecoveryState(delay);
                var whole = VitalsRecoveryRules.Advance(settings, values, state, 12, true);
                var part = VitalsRecoveryRules.Advance(settings, values, state, 0, true);
                for (int i = 0; i < 1200; i++) part = VitalsRecoveryRules.Advance(settings, part.Values, part.State, .01, true);
                Assert.AreEqual(whole.Values.Health, part.Values.Health, Epsilon, $"health {health}, hunger {hunger}, delay {delay}, count {count}");
                Assert.AreEqual(whole.Values.Hunger, part.Values.Hunger, Epsilon);
                Assert.AreEqual(whole.State.DelayRemaining, part.State.DelayRemaining, Epsilon);
                Assert.LessOrEqual(part.Values.Health, VitalsRecoveryRules.Ceiling(settings, health));
            }
        }

        [Test] public void ZeroRatesAndDelayAreValidAndHugeFiniteAdvancesStayBounded()
        {
            var settings = new VitalsRecoverySettings(hungerDepletionRate: 0, damageDelay: 0, recoveryRate: 0);
            Assert.IsTrue(settings.IsValid);
            Values(VitalsRecoveryRules.Advance(settings, new VitalsValues(30, 20), default, 100, true), 30, 20, 0);
            var noThreshold = new VitalsRecoverySettings(fedThresholdFraction: 0, recoveryRate: double.MaxValue);
            Values(VitalsRecoveryRules.Advance(noThreshold, new VitalsValues(30, 0), default, double.MaxValue, true), 50, 0, 0);
            Values(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 100), default, double.MaxValue, true), 50, 0, 0);
        }

        [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidElapsedTimePreservesValuesAndDelay(double seconds)
        {
            var result = VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 50), new VitalsRecoveryState(3), seconds, true);
            Assert.IsFalse(result.Accepted); Assert.AreEqual(30, result.Values.Health);
            Assert.AreEqual(50, result.Values.Hunger); Assert.AreEqual(3, result.State.DelayRemaining);
        }

        [Test] public void InvalidSettingsOrInputSnapshotsCannotCorruptAnyOperation()
        {
            var invalid = new[] { new VitalsRecoverySettings(maxHealth: 0), new VitalsRecoverySettings(maxHunger: -1),
                new VitalsRecoverySettings(maxHealth: double.NaN), new VitalsRecoverySettings(maxHunger: double.PositiveInfinity),
                new VitalsRecoverySettings(segmentCount: 0), new VitalsRecoverySettings(fedThresholdFraction: -1),
                new VitalsRecoverySettings(fedThresholdFraction: 1.1), new VitalsRecoverySettings(fedThresholdFraction: double.NaN),
                new VitalsRecoverySettings(hungerDepletionRate: -1), new VitalsRecoverySettings(hungerDepletionRate: double.PositiveInfinity),
                new VitalsRecoverySettings(damageDelay: -1), new VitalsRecoverySettings(damageDelay: double.NaN),
                new VitalsRecoverySettings(recoveryRate: -1), new VitalsRecoverySettings(recoveryRate: double.PositiveInfinity) };
            foreach (var settings in invalid)
            {
                Assert.IsFalse(settings.IsValid);
                var values = new VitalsValues(30, 50); var state = new VitalsRecoveryState(3);
                foreach (var result in new[] { VitalsRecoveryRules.Advance(settings, values, state, 1, true),
                    VitalsRecoveryRules.Damage(settings, values, state, 1), VitalsRecoveryRules.RestoreFood(settings, values, state, 25),
                    VitalsRecoveryRules.RestoreHealth(settings, values, state, 25) })
                {
                    Assert.IsFalse(result.Accepted); Assert.AreEqual(30, result.Values.Health);
                    Assert.AreEqual(50, result.Values.Hunger); Assert.AreEqual(3, result.State.DelayRemaining);
                    Assert.AreEqual(RecoveryReason.Unavailable, result.Reason);
                }
            }
            foreach (var values in new[] { new VitalsValues(-1, 50), new VitalsValues(101, 50),
                new VitalsValues(30, -1), new VitalsValues(30, 101), new VitalsValues(double.NaN, 50), new VitalsValues(30, double.PositiveInfinity) })
                Assert.IsFalse(VitalsRecoveryRules.Advance(Defaults, values, default, 1, true).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 50), new VitalsRecoveryState(-1), 1, true).Accepted);
            Assert.IsFalse(VitalsRecoveryRules.Advance(Defaults, new VitalsValues(30, 50), new VitalsRecoveryState(double.NaN), 1, true).Accepted);
        }

        [Test] public void RecoveryReasonHasTheApprovedPrecedenceWithoutChangingValues()
        {
            Assert.AreEqual(RecoveryReason.Unavailable, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(0, 0), new VitalsRecoveryState(3), false));
            Assert.AreEqual(RecoveryReason.AtCeiling, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(25, 0), new VitalsRecoveryState(3), false));
            Assert.AreEqual(RecoveryReason.Paused, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(30, 0), new VitalsRecoveryState(3), false));
            Assert.AreEqual(RecoveryReason.Hungry, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(30, 0), new VitalsRecoveryState(3), true));
            Assert.AreEqual(RecoveryReason.Delayed, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(30, 20), new VitalsRecoveryState(3), true));
            Assert.AreEqual(RecoveryReason.Recovering, VitalsRecoveryRules.Describe(Defaults, new VitalsValues(30, 20), default, true));
        }

        [Test] public void SuppliedSettingsChangesAreNotDamageAndRecomputeCeilingAndReason()
        {
            var values = new VitalsValues(30, 50); var state = new VitalsRecoveryState(2);
            var thirds = new VitalsRecoverySettings(segmentCount: 3);
            var result = VitalsRecoveryRules.Advance(thirds, values, state, 0, true);
            Values(result, 30, 50, 2); Assert.AreEqual(100d / 3, result.RecoveryCeiling, Epsilon);
            var higherThreshold = new VitalsRecoverySettings(fedThresholdFraction: .8);
            Assert.AreEqual(RecoveryReason.Hungry, VitalsRecoveryRules.Describe(higherThreshold, values, state, true));
        }
    }
}
