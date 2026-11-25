using System;
using NUnit.Framework;
using SOLITUDE.Items;
namespace SOLITUDE.Tests
{
    public sealed class MeleeAttackTimingTests
    {
        [Test] public void DefaultBoundariesAndActiveProcessingAreExplicit()
        {
            var timing = new MeleeAttackTiming(.12, .10, .33);
            Assert.AreEqual(MeleeAttackPhase.Windup, timing.Phase);
            Assert.IsFalse(timing.Advance(.11)); Assert.AreEqual(MeleeAttackPhase.Windup, timing.Phase);
            Assert.IsTrue(timing.Advance(.01)); Assert.AreEqual(MeleeAttackPhase.Active, timing.Phase);
            Assert.IsTrue(timing.Advance(.10)); Assert.AreEqual(MeleeAttackPhase.Recovery, timing.Phase);
            Assert.IsFalse(timing.Advance(.33)); Assert.AreEqual(MeleeAttackPhase.Idle, timing.Phase);
            Assert.IsFalse(timing.Advance(1));
        }
        [Test] public void CoarseAdvanceCannotSkipActiveAndMatchesSubdividedFinalPhase()
        {
            var coarse = new MeleeAttackTiming(.12, .1, .33); var fine = new MeleeAttackTiming(.12, .1, .33);
            Assert.IsTrue(coarse.Advance(.55)); bool sampled = false;
            sampled |= fine.Advance(.12); sampled |= fine.Advance(.1); sampled |= fine.Advance(.33);
            Assert.IsTrue(sampled); Assert.AreEqual(coarse.Phase, fine.Phase); Assert.AreEqual(coarse.Elapsed, fine.Elapsed);
            Assert.AreEqual(MeleeAttackPhase.Idle, coarse.Phase);
        }
        [Test] public void ZeroWindupRecoveryAndInterruptionHaveDefinedPhases()
        {
            var timing = new MeleeAttackTiming(0, .1, 0);
            Assert.AreEqual(MeleeAttackPhase.Active, timing.Phase); Assert.IsFalse(timing.Advance(0));
            Assert.IsTrue(timing.Advance(.1)); Assert.AreEqual(MeleeAttackPhase.Idle, timing.Phase);
            timing.Interrupt(); Assert.IsFalse(timing.Advance(.1));
        }
        [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedCannotMutateTiming(double value)
        {
            var timing = new MeleeAttackTiming(.12, .1, .33);
            Assert.Throws<ArgumentOutOfRangeException>(() => timing.Advance(value)); Assert.AreEqual(0, timing.Elapsed);
            Assert.Throws<ArgumentOutOfRangeException>(() => new MeleeAttackTiming(value, .1, .33));
        }
        [Test] public void InvalidActiveAndOverflowingDurationAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MeleeAttackTiming(0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MeleeAttackTiming(double.MaxValue, double.MaxValue, 0));
        }
    }
}
