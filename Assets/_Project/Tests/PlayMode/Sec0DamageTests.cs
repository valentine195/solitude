#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.Movement;
using SOLITUDE.Npcs;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class Sec0DamageTests
    {
        private GameObject host, player;
        private Sec0Controller bot;
        private Sec0DamageRecipient recipient;
        private Sec0Composition composition;
        private InputPolicyCoordinator policy;
        private ItemDefinition item;
        private CompiledItemAction action;
        private MeleeAttackSpec melee;
        private DamageDelivery delivery;
        [SetUp] public void Prepare()
        {
            host = new GameObject("SEC-0 incoming damage fixture");
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab"));
            instance.transform.SetParent(host.transform); bot = instance.GetComponentInChildren<Sec0Controller>();
            recipient = bot.GetComponent<Sec0DamageRecipient>(); Assert.NotNull(recipient);
            player = new GameObject("Player"); player.transform.SetParent(host.transform); player.transform.position = Vector2.down;
            player.AddComponent<Rigidbody2D>().gravityScale = 0; player.AddComponent<BoxCollider2D>().size = new Vector2(.5f, .5f);
            player.AddComponent<Sec0TestAnimation>(); player.AddComponent<MovementBehavior>();
            var stun = player.AddComponent<PlayerStunReceiver>(); var vitals = player.AddComponent<PlayerVitalsController>();
            var interactor = player.AddComponent<PlayerInteractor>();
            policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true);
            composition = host.AddComponent<Sec0Composition>(); composition.Configure(new[] { bot }, stun, vitals);
            composition.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "sec0-damage", interactor, null));
            var catalog = Resources.Load<ItemDatabase>("Database").BuildCapabilityCatalog(); delivery = new DamageDelivery(catalog.Registry);
            item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            action = catalog.Resolve(item.ItemId).Resolve(ItemActionSlot.Primary); melee = (MeleeAttackSpec)action.Configuration;
            Physics2D.SyncTransforms();
        }
        [TearDown] public void Cleanup() { composition?.Release(); Object.DestroyImmediate(host); policy?.Dispose(); }
        private DamageContext Context(double amount) => new DamageContext(amount, player, recipient,
            item.ItemId, action.Definition, bot.transform.position, Vector2.up, melee.DamageTags);
        private void Charge()
        { bot.Tick(.4f); bot.Tick(.02f); Assert.AreEqual(SecurityBotState.Charging, bot.State); }
        [Test] public void FourDefaultHitsDefeatOnceClampOverkillAndRejectFurtherDamage()
        {
            int deaths = 0; recipient.Defeated += () => deaths++;
            Assert.AreEqual(40, recipient.MaximumHealth); Assert.AreEqual(40, recipient.CurrentHealth);
            for (int hit = 1; hit <= 4; hit++)
            {
                var result = delivery.Deliver(Context(melee.BaseDamage));
                Assert.IsTrue(result.WasApplied); Assert.AreEqual(10, result.AppliedAmount);
                Assert.AreEqual(40 - hit * 10, recipient.CurrentHealth);
            }
            Assert.AreEqual(1, deaths); Assert.IsTrue(recipient.IsDefeated); Assert.IsFalse(recipient.IsAvailable);
            Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(100)).Outcome);
            Assert.AreEqual(0, recipient.CurrentHealth); Assert.AreEqual(1, deaths);
            composition.Release(); composition.Release(); Assert.IsFalse(bot.HasBinding);
        }
        [Test] public void DefeatDuringChargeCancelsPendingFirePhysicsAndTargetParticipation()
        {
            Charge(); Assert.IsTrue(delivery.Deliver(Context(double.MaxValue)).WasApplied);
            bot.Tick(10); Assert.AreEqual(0, bot.ShotsFired); Assert.AreEqual(0, bot.StunsLanded);
            Assert.IsFalse(bot.CanSeeTarget()); Assert.IsFalse(bot.HasBinding); Assert.AreEqual(0, recipient.CurrentHealth);
            Assert.IsFalse(bot.GetComponent<Rigidbody2D>().simulated);
            Assert.IsTrue(bot.GetComponentsInChildren<Collider2D>(true).All(shape => !shape.enabled));
            Assert.IsFalse(player.GetComponent<PlayerStunReceiver>().IsStunned);
            composition.Release(); composition.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "sec0-damage", player.GetComponent<PlayerInteractor>(), null));
            bot.Tick(10); Assert.IsFalse(bot.HasBinding); Assert.IsFalse(recipient.IsAvailable); Assert.AreEqual(0, recipient.CurrentHealth);
        }
        [UnityTest] public IEnumerator DefeatCancelsExistingPulseObjectsAndReleaseIsIdempotent()
        {
            Charge(); bot.Tick(1); Assert.AreEqual(1, bot.ShotsFired); Assert.AreEqual(1, bot.ActivePulseCount);
            var effects = ((List<GameObject>)typeof(Sec0Controller).GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(bot)).ToArray();
            Assert.IsTrue(delivery.Deliver(Context(40)).WasApplied); Assert.AreEqual(0, bot.ActivePulseCount);
            Assert.IsTrue(effects.All(effect => !effect.activeSelf)); composition.Release(); composition.Release();
            yield return null; Assert.IsTrue(effects.All(effect => effect == null));
            bot.Tick(10); Assert.AreEqual(1, bot.ShotsFired);
        }
        [TestCase(0d)] [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDamageLeavesLiveHealthAndBrainUntouched(double amount)
        {
            Charge(); Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(Context(amount)).Outcome);
            Assert.AreEqual(40, recipient.CurrentHealth); Assert.IsFalse(bot.IsDefeated); Assert.AreEqual(SecurityBotState.Charging, bot.State);
            bot.Tick(1); Assert.AreEqual(1, bot.ShotsFired); Assert.AreEqual(1, bot.StunsLanded);
        }
        [Test] public void DisabledReleasedAndPolicyGatedRecipientsRejectDamage()
        {
            recipient.enabled = false; Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10)).Outcome);
            recipient.enabled = true; policy.SetTransition(true);
            Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10)).Outcome); policy.SetTransition(false);
            composition.Release(); Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10)).Outcome);
            Assert.AreEqual(40, recipient.CurrentHealth);
        }
        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidAuthoredMaximumIsDiagnosedWithoutChangingRuntimeHealth(float maximum)
        {
            var authoring = new SerializedObject(recipient);
            authoring.FindProperty("maximumHealth").floatValue = maximum;
            authoring.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.InvalidOperationException>(() => recipient.ValidateAuthoring());
            Assert.AreEqual(40, recipient.CurrentHealth);
        }
    }
}
#endif
