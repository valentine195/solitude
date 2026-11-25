#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SOLITUDE.Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SOLITUDE.Tests
{
    public sealed class MeleeActiveWindowTests
    {
        private MeleeHitTestFixture f;
        [SetUp] public void Prepare() => f = new MeleeHitTestFixture();
        [UnityTearDown] public IEnumerator Cleanup() { var scene = f.Scene; f.Dispose(); yield return SceneManager.UnloadSceneAsync(scene); }
        [Test] public void WindupAndRecoveryNeverDeliverAndActiveSamplesDeduplicate()
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f);
            Assert.IsTrue(f.Start()); f.Advance(.119); Assert.AreEqual(0, target.Calls);
            f.Advance(.001); Assert.AreEqual(1, target.Calls); f.Advance(.02); f.Advance(.081); Assert.AreEqual(1, target.Calls);
            Assert.AreEqual(MeleeAttackPhase.Recovery, f.Execution.Timing.Phase);
            var late = f.Box(new Vector2(.9f, 0), Vector2.one * .2f); f.Advance(.33);
            Assert.AreEqual(0, late.Calls); Assert.IsFalse(f.Runner.IsExecuting);
            Assert.IsTrue(f.Start()); f.Advance(.55); Assert.AreEqual(2, target.Calls); Assert.AreEqual(1, late.Calls);
        }
        [TestCase(1)] [TestCase(5)] [TestCase(55)]
        public void CoarseAndSubdividedStaticWorldProduceOneHitAndIdle(int steps)
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f); f.Start();
            for (int i = 0; i < steps; i++) f.Advance(.55 / steps);
            Assert.AreEqual(1, target.Calls); Assert.IsFalse(f.Runner.IsExecuting);
        }
        [Test] public void ReentrantEquipmentInvalidationStopsTheRestOfTheCleaveAndRetainsLockout()
        {
            var a = f.Box(new Vector2(.6f, -.2f), Vector2.one * .2f); var b = f.Box(new Vector2(.8f, .2f), Vector2.one * .2f);
            a.OnApplied = b.OnApplied = () => f.Selection.Select(-1);
            f.Start(); f.Advance(.12); Assert.AreEqual(1, a.Calls + b.Calls); Assert.IsFalse(f.Runner.IsExecuting);
            f.Selection.Select(0); Assert.IsFalse(f.Start()); f.Advance(.43); Assert.IsTrue(f.Start());
        }
        [Test] public void PausingDuringDeliveryFreezesRemainingEffectsWithoutCancelingExecution()
        {
            var a = f.Box(new Vector2(.6f, -.2f), Vector2.one * .2f); var b = f.Box(new Vector2(.8f, .2f), Vector2.one * .2f);
            a.OnApplied = b.OnApplied = () => f.Gameplay = false;
            f.Start(); f.Advance(.12); Assert.AreEqual(1, a.Calls + b.Calls); Assert.IsTrue(f.Runner.IsExecuting);
            f.Advance(100); Assert.That(f.Runner.LockoutRemaining, Is.EqualTo(.43).Within(.000001)); Assert.AreEqual(1, a.Calls + b.Calls);
            f.Gameplay = true; f.Advance(.02); Assert.AreEqual(2, a.Calls + b.Calls);
        }
        [TestCase(.05)] [TestCase(.15)] [TestCase(.30)]
        public void ActorInvalidationAndInterruptionCannotLeaveFutureHits(double elapsed)
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f); f.Start(); f.Advance(elapsed);
            int calls = target.Calls; f.Available = false; f.Advance(0); f.Advance(1);
            Assert.AreEqual(calls, target.Calls); Assert.IsFalse(f.Runner.IsExecuting); Assert.IsTrue(f.Execution.IsComplete);
        }
        [Test] public void ActiveObserverCanInterruptBeforeAnyGeometryDelivery()
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f);
            f.ActiveSample = execution => execution.Interrupt(); f.Start(); f.Advance(.12);
            Assert.AreEqual(0, target.Calls); Assert.IsFalse(f.Runner.IsExecuting);
        }
        [Test] public void ConfiguredOriginAndTileScaleControlReachWithoutAnAimSystem()
        {
            f.UnitsPerTile = 2; f.OriginOffset = new Vector2(0, 1);
            var target = f.Box(new Vector2(2.5f, 2), Vector2.one * .1f); var miss = f.Box(new Vector2(2.7f, 2), Vector2.one * .1f);
            f.Start(); f.Advance(.55); Assert.AreEqual(1, target.Calls); Assert.AreEqual(0, miss.Calls);
            Assert.That(Vector2.Distance(target.Last.ImpactPosition, new Vector2(0, 2)), Is.LessThanOrEqualTo(2.5001f));
        }
        [Test] public void MasksCanExcludeTargetsAndIncludeExistingCoverLayers()
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f);
            f.Configuration.TargetLayers = 0; f.Start(); f.Advance(.55); Assert.AreEqual(0, target.Calls);
            f.Configuration.TargetLayers = 1; var wall = f.Cover(new Vector2(.4f, 0), Vector2.one * .2f); wall.layer = 2;
            f.Configuration.BlockingLayers = (1 << 0) | (1 << 2); f.Start(); f.Advance(.55); Assert.AreEqual(0, target.Calls);
        }
        [Test] public void SharedConfigurationHasNoSharedHitTrackingBetweenActors()
        {
            var a = f.Box(new Vector2(.8f, 0), Vector2.one * .2f); f.Start(); f.Advance(.55); Assert.AreEqual(1, a.Calls);
            f.Start(); f.Advance(.55); Assert.AreEqual(2, a.Calls);
            // Independent runtime ownership with the same compiled asset references.
            var second = new MeleeHitTestFixture(f.Configuration);
            try
            {
                var b = second.Box(new Vector2(.8f, 0), Vector2.one * .2f); second.Start(); second.Advance(.55);
                Assert.AreEqual(1, b.Calls); Assert.AreEqual(2, a.Calls);
            }
            finally { var scene = second.Scene; second.Dispose(); SceneManager.UnloadSceneAsync(scene); }
        }
    }
}
#endif
