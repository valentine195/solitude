using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Vitals;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class PlayerVitalsRuntimeTests
    {
        private GameObject host;
        private PlayerVitalsController player;
        private PlayerVitalsComposition feature;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private float previousScale;
        private SceneFeatureContext Context => new SceneFeatureContext(null, null, null, policy,
            null, null, "vitals-fixture", null, null);

        [SetUp] public void Prepare()
        {
            previousScale = Time.timeScale;
            Time.timeScale = 1;
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause);
            host = new GameObject("PlayerVitalsRuntimeFixture");
            player = host.AddComponent<PlayerVitalsController>();
            feature = host.AddComponent<PlayerVitalsComposition>();
            feature.Configure(player); feature.Initialize(Context); policy.SetReady(true);
        }
        [TearDown] public void Cleanup()
        {
            if (host != null) Object.DestroyImmediate(host);
            policy.Dispose(); Time.timeScale = previousScale;
        }
        private static void Field(object owner, string name, object value)
            => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private void ValidateFields()
            => typeof(PlayerVitalsController).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
        private void PendingRecovery()
        {
            player.Vitals.Health.Set(40); player.Vitals.Hunger.Set(60);
            Assert.IsTrue(player.Damage(10).Accepted);
        }
        private void AssertFrozen()
        {
            player.Advance(1000);
            Assert.AreEqual(30, player.Vitals.Health.Current);
            Assert.AreEqual(60, player.Vitals.Hunger.Current);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
            Assert.IsNull(player.PrepareFood(25));
            Assert.AreEqual(RecoveryReason.Paused, player.RecoveryStatus);
        }

        // S9: readiness/transition/screen eligibility is independent of Unity time scale.
        [Test] public void EveryGameplayGateFreezesValuesAndDelayWithoutCatchup()
        {
            PendingRecovery();
            policy.SetReady(false); AssertFrozen(); policy.SetReady(true);
            policy.SetTransition(true); AssertFrozen(); policy.SetTransition(false);
            using (policy.AcquirePrimaryScreen())
            { Assert.AreEqual(1, Time.timeScale); AssertFrozen(); }
            using (pause.Acquire()) AssertFrozen();
            var modal = typeof(InputPolicyCoordinator).GetMethod("SetModal", BindingFlags.Instance | BindingFlags.NonPublic);
            modal.Invoke(policy, new object[] { true }); AssertFrozen(); modal.Invoke(policy, new object[] { false });
            player.Advance(1);
            Assert.AreEqual(59.9f, player.Vitals.Hunger.Current, .00001f);
            Assert.AreEqual(4, player.RecoveryState.DelayRemaining);
            Assert.AreEqual(30, player.Vitals.Health.Current);
            feature.Release();
            player.Advance(1000);
            Assert.AreEqual(59.9f, player.Vitals.Hunger.Current, .00001f);
            Assert.AreEqual(4, player.RecoveryState.DelayRemaining);
            Assert.IsNull(player.PrepareMedical(25));
        }

        // S1/S4/S5: retained runtime values implement the same interval examples.
        [Test] public void DamageDelayAndFedIntervalsApplyPreparedRuntimeValues()
        {
            player.Vitals.Health.Set(40); player.Damage(10);
            player.Advance(6);
            Assert.AreEqual(32, player.Vitals.Health.Current);
            Assert.AreEqual(99.4f, player.Vitals.Hunger.Current, .00001f);
            Assert.AreEqual(0, player.RecoveryState.DelayRemaining);
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(20.1f);
            player.Advance(2);
            Assert.AreEqual(32, player.Vitals.Health.Current, .00001f);
            Assert.AreEqual(19.9f, player.Vitals.Hunger.Current, .00001f);
            Assert.AreEqual(RecoveryReason.Hungry, player.RecoveryStatus);
            Assert.AreEqual(100, player.Vitals.Oxygen.Current);
        }

        [Test] public void ManySmallRuntimeAdvancesMatchOneLongAdvance()
        {
            player.Vitals.Health.Set(40); player.Vitals.Hunger.Set(20.7f); player.Damage(10);
            var whole = player.Advance(12);
            player.Vitals.Health.Set(40); player.Vitals.Hunger.Set(20.7f); player.Damage(10);
            VitalsRecoveryResult part = default;
            for (int i = 0; i < 1200; i++) part = player.Advance(.01);
            Assert.AreEqual(whole.Values.Health, part.Values.Health, 1e-8);
            Assert.AreEqual(whole.Values.Hunger, part.Values.Hunger, 1e-8);
            Assert.AreEqual(whole.State.DelayRemaining, part.State.DelayRemaining, 1e-8);
        }

        [Test] public void ExactDefaultThresholdIsInclusive()
        {
            player.Vitals.Health.Set(30); player.Vitals.Hunger.Set(20);
            Assert.AreEqual(20, player.Settings.FedThreshold);
            Assert.AreEqual(RecoveryReason.Recovering, player.RecoveryStatus);
            player.Advance(1);
            Assert.AreEqual(30, player.Vitals.Health.Current);
            Assert.AreEqual(RecoveryReason.Hungry, player.RecoveryStatus);
        }

        // S10: initialization retains identity and clears only the previous recovery history.
        [Test] public void AwakeningRetainsVitalObjectsAndResetsAuthoredValuesAndTiming()
        {
            var health = player.Vitals.Health; var oxygen = player.Vitals.Oxygen; var hunger = player.Vitals.Hunger;
            PendingRecovery(); player.Vitals.Oxygen.Set(9);
            Field(player, "currentHealth", 22f); Field(player, "currentOxygen", 14f); Field(player, "currentHunger", 61f);
            player.ResetForAwakening();
            Assert.AreSame(health, player.Vitals.Health); Assert.AreSame(oxygen, player.Vitals.Oxygen); Assert.AreSame(hunger, player.Vitals.Hunger);
            Assert.AreEqual(22, health.Current); Assert.AreEqual(14, oxygen.Current); Assert.AreEqual(61, hunger.Current);
            Assert.AreEqual(0, player.RecoveryState.DelayRemaining);
            Assert.AreEqual(RecoveryReason.Recovering, player.RecoveryStatus);
            player.Advance(1); Assert.AreEqual(24, health.Current);
        }

        [Test] public void RebindingHasOnePolicySubscriptionAndReleaseDetachesIt()
        {
            feature.Initialize(Context); feature.Initialize(Context); feature.Initialize(Context);
            int changes = 0; player.PresentationChanged += () => changes++;
            policy.SetTransition(true); Assert.AreEqual(1, changes);
            policy.SetTransition(false); Assert.AreEqual(2, changes);
            player.Vitals.Hunger.Set(60); player.Advance(1);
            Assert.AreEqual(59.9f, player.Vitals.Hunger.Current, .00001f);
            feature.Release(); int released = changes;
            policy.SetReady(false); policy.SetTransition(true);
            Assert.AreEqual(released, changes, "Released policy must not notify the old player binding.");
        }

        [Test] public void DuplicateGameplayOwnerIsRejectedWithoutDetachingTheFirst()
        {
            var duplicateObject = new GameObject("DuplicateVitalsOwner");
            try
            {
                var duplicate = duplicateObject.AddComponent<PlayerVitalsComposition>(); duplicate.Configure(player);
                Assert.Throws<InvalidOperationException>(() => duplicate.Initialize(Context));
                Assert.IsTrue(player.GameplayEligible);
            }
            finally { Object.DestroyImmediate(duplicateObject); }
            Assert.IsTrue(player.GameplayEligible);
        }

        [Test] public void DisabledFeatureOrControllerStopsAdvancementAndPreparation()
        {
            PendingRecovery(); feature.enabled = false; AssertFrozen(); feature.enabled = true;
            player.enabled = false; AssertFrozen(); player.enabled = true;
            player.Advance(1); Assert.AreEqual(4, player.RecoveryState.DelayRemaining);
        }

        // S3/S11/S12/S13: maximum changes notify without healing or counting as damage.
        [Test] public void ConfigurationAndMaximumOnlyChangesPublishCoherentStateWithoutHealing()
        {
            PendingRecovery(); int healthChanges = 0, presentations = 0; bool coherent = true;
            player.Vitals.Health.Changed += _ =>
            {
                healthChanges++;
                coherent &= player.Vitals.Hunger.Max == 200 && player.Settings.MaxHealth == 200;
            };
            player.PresentationChanged += () => presentations++;
            Assert.IsTrue(player.Configure(new VitalsRecoverySettings(maxHealth: 200, maxHunger: 200, segmentCount: 2)));
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(60, player.Vitals.Hunger.Current);
            Assert.AreEqual(100, player.PassiveRecoveryCeiling);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
            Assert.AreEqual(1, healthChanges); Assert.AreEqual(1, presentations);
            Assert.IsTrue(coherent, "Observers must see both values and settings already committed.");
            Assert.AreEqual(100, player.Vitals.Oxygen.Max); Assert.AreEqual(100, player.Vitals.Oxygen.Current);
        }

        [Test] public void LegacyMaximumAndInspectorSettingChangesRefreshWithoutInventingDamage()
        {
            PendingRecovery(); int changes = 0; player.PresentationChanged += () => changes++;
            player.Vitals.Health.SetMax(200);
            Assert.AreEqual(200, player.Settings.MaxHealth); Assert.AreEqual(30, player.Vitals.Health.Current);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining); Assert.AreEqual(1, changes);
            Field(player, "healthSegments", 3); Field(player, "maxHealth", 200f); ValidateFields();
            Assert.AreEqual(3, player.Settings.SegmentCount); Assert.AreEqual(30, player.Vitals.Health.Current);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining); Assert.Greater(changes, 1);
            player.Configure(new VitalsRecoverySettings(maxHealth: 25, maxHunger: 50));
            Assert.AreEqual(25, player.Vitals.Health.Current); Assert.AreEqual(50, player.Vitals.Hunger.Current);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
        }

        [Test] public void InvalidConfigurationAndElapsedInputsPreserveExistingVitals()
        {
            PendingRecovery();
            foreach (var invalid in new[] { new VitalsRecoverySettings(maxHealth: 0), new VitalsRecoverySettings(segmentCount: 0),
                new VitalsRecoverySettings(recoveryRate: double.NaN), new VitalsRecoverySettings(maxHealth: double.MaxValue) })
            {
                Assert.IsFalse(player.Configure(invalid)); Assert.IsNotNull(player.ConfigurationError);
                Assert.AreEqual(RecoveryReason.Unavailable, player.RecoveryStatus);
                Assert.IsFalse(player.Advance(100).Accepted); Assert.IsFalse(player.Damage(5).Accepted);
                Assert.IsNull(player.PrepareFood(25)); Assert.IsNull(player.PrepareMedical(25));
                Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(60, player.Vitals.Hunger.Current);
                Assert.AreEqual(100, player.Vitals.Oxygen.Current); Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
            }
            Assert.IsTrue(player.Configure(new VitalsRecoverySettings()));
            foreach (var seconds in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.IsFalse(player.Advance(seconds).Accepted);
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(60, player.Vitals.Hunger.Current);
            Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
        }

        [Test] public void HungerCooldownAndPolicyNotifyWhileHealthStaysConstant()
        {
            player.Vitals.Health.Set(40); player.Damage(10); player.Vitals.Hunger.Set(0);
            int changes = 0; player.PresentationChanged += () => changes++;
            player.Advance(5);
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(0, player.RecoveryState.DelayRemaining);
            Assert.AreEqual(RecoveryReason.Hungry, player.RecoveryStatus); Assert.AreEqual(1, changes);
            Assert.IsTrue(player.TryApplyPrepared(player.PrepareFood(25)));
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(RecoveryReason.Recovering, player.RecoveryStatus);
            Assert.AreEqual(2, changes);
            using (policy.AcquirePrimaryScreen()) Assert.AreEqual(RecoveryReason.Paused, player.RecoveryStatus);
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(4, changes);
        }

        // Prepared state is revision-bound; observer exceptions happen after committed values.
        [Test] public void PreparedChangesRejectStaleSnapshotsAndCannotReplay()
        {
            PendingRecovery(); var prepared = player.PrepareFood(25);
            Assert.IsNotNull(prepared); Assert.AreEqual(60, player.Vitals.Hunger.Current, "Preparation is read-only.");
            player.Damage(1); Assert.IsFalse(player.TryApplyPrepared(prepared)); Assert.AreEqual(60, player.Vitals.Hunger.Current);
            var fresh = player.PrepareFood(25); Assert.IsTrue(player.TryApplyPrepared(fresh));
            Assert.AreEqual(85, player.Vitals.Hunger.Current); Assert.IsFalse(player.TryApplyPrepared(fresh));
            var gated = player.PrepareFood(25);
            using (policy.AcquirePrimaryScreen()) Assert.IsFalse(player.TryApplyPrepared(gated));
            Assert.IsFalse(player.TryApplyPrepared(gated), "A policy change invalidates an old prepared use after reopening.");
            var released = player.PrepareFood(25); feature.Release(); Assert.IsFalse(player.TryApplyPrepared(released));
        }

        [Test] public void ObserverFailuresCannotInterruptCommittedEffectsOrAllowReentrantMutation()
        {
            PendingRecovery(); int diagnosed = 0, delivered = 0;
            player.ObserverError += _ => throw new Exception("diagnostic observer");
            player.ObserverError += _ => diagnosed++;
            player.Vitals.Hunger.Changed += _ => throw new Exception("vital observer");
            player.PresentationChanged += () => throw new Exception("presentation observer");
            player.PresentationChanged += () =>
            {
                delivered++;
                Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(85, player.Vitals.Hunger.Current);
                Assert.AreEqual(5, player.RecoveryState.DelayRemaining);
                player.Vitals.Health.Set(0); player.Vitals.Hunger.Set(0);
                Assert.IsFalse(player.Damage(1).Accepted); Assert.IsFalse(player.Advance(1).Accepted);
                Assert.IsFalse(player.Configure(new VitalsRecoverySettings()));
                player.ResetForAwakening();
            };
            var prepared = player.PrepareFood(25);
            Assert.IsTrue(player.TryApplyPrepared(prepared));
            Assert.AreEqual(30, player.Vitals.Health.Current); Assert.AreEqual(85, player.Vitals.Hunger.Current);
            Assert.AreEqual(2, diagnosed); Assert.AreEqual(1, delivered);
            Assert.IsFalse(player.TryApplyPrepared(prepared));
        }

        [UnityTest] public IEnumerator ProductionUpdateUsesScaledDeltaExactlyOnceAndUnboundControllersDoNotTick()
        {
            player.Vitals.Hunger.Set(60);
            feature.Initialize(Context); feature.Initialize(Context);
            Time.timeScale = .5f;
            yield return null;
            float start = Time.time; float before = player.Vitals.Hunger.Current;
            yield return new WaitForSecondsRealtime(.2f);
            float scaled = Time.time - start;
            Assert.Greater(scaled, 0);
            Assert.AreEqual(before - .1f * scaled, player.Vitals.Hunger.Current, .001f);
            feature.Release(); float frozen = player.Vitals.Hunger.Current;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(frozen, player.Vitals.Hunger.Current);
        }
    }
}
