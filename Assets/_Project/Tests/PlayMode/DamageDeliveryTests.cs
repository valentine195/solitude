#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Composition;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using SOLITUDE.Player;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class TestDamageRecipient : MonoBehaviour, IDamageRecipient
    {
        public GameObject Entity => this != null ? gameObject : null;
        public bool Available = true;
        public bool IsAvailable => this != null && isActiveAndEnabled && Available;
        public GameplayTagSet AssignedTags;
        public GameplayTagSet Tags => AssignedTags;
        public int Calls;
        public DamageContext LastContext;
        public DamageResult ApplyDamage(in DamageContext context, double amount)
        { Calls++; LastContext = context; return new DamageResult(DamageOutcome.Applied, amount); }
    }

    public sealed class DamageDeliveryTests
    {
        private GameObject attacker, target;
        private TestDamageRecipient recipient;
        private GameplayTagRegistry registry;
        private ItemDefinition item;
        private CompiledItemAction action;
        private MeleeAttackSpec melee;
        private DamageDelivery delivery;
        private PlayerVitalsComposition feature;
        private InputPolicyCoordinator policy;
        private PauseCoordinator pause;

        [SetUp] public void Prepare()
        {
            attacker = new GameObject("Damage source"); target = new GameObject("Damage target");
            recipient = target.AddComponent<TestDamageRecipient>();
            item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            var catalog = Resources.Load<ItemDatabase>("Database").BuildCapabilityCatalog();
            registry = catalog.Registry; action = catalog.Resolve(item.ItemId).Resolve(ItemActionSlot.Primary);
            melee = (MeleeAttackSpec)action.Configuration; delivery = new DamageDelivery(registry);
        }
        [TearDown] public void Cleanup()
        { if (target != null) Object.DestroyImmediate(target); Object.DestroyImmediate(attacker); policy?.Dispose(); }
        private DamageContext Context(double amount = 10, IDamageRecipient to = null) => new DamageContext(amount,
            attacker, to ?? recipient, item.ItemId, action.Definition, new Vector2(1, 2), Vector2.right, melee.DamageTags);
        private GameplayTag Tag(string name) => registry.Tags.Single(tag => tag.Name == name);

        [Test] public void ContextRetainsSourceContactAndExplicitDamageTags()
        {
            var context = Context(); var result = delivery.Deliver(context);
            Assert.AreEqual(DamageOutcome.Applied, result.Outcome); Assert.AreEqual(10, result.AppliedAmount);
            Assert.AreSame(attacker, recipient.LastContext.Attacker); Assert.AreSame(recipient, context.Target);
            Assert.AreEqual(item.ItemId, context.SourceItemId); Assert.AreSame(action.Definition, context.SourceAction);
            Assert.AreEqual(new Vector2(1, 2), context.ImpactPosition); Assert.AreEqual(Vector2.right, context.Direction);
            Assert.AreSame(melee.DamageTags, context.DamageTags); Assert.AreEqual(1, context.DamageTags.Tags.Count);
            Assert.IsTrue(context.DamageTags.HasTag(Tag("Damage.Blunt")));
            Assert.IsFalse(context.DamageTags.HasTag(Tag("Item.Tool")));
            Assert.IsFalse(context.DamageTags.HasTag(Tag("Tool.Mechanical")));
        }
        [TestCase(0d)] [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void InvalidAmountsNeverReachRecipient(double amount)
        { Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(Context(amount)).Outcome); Assert.AreEqual(0, recipient.Calls); }
        [Test] public void MissingSourceRecipientOrInvalidContactIsRejected()
        {
            foreach (var context in new[] { default(DamageContext),
                new DamageContext(10, null, recipient, item.ItemId, action.Definition, Vector2.zero, Vector2.right, melee.DamageTags),
                new DamageContext(10, attacker, recipient, null, action.Definition, Vector2.zero, Vector2.right, melee.DamageTags),
                new DamageContext(10, attacker, recipient, item.ItemId, null, Vector2.zero, Vector2.right, melee.DamageTags),
                new DamageContext(10, attacker, recipient, item.ItemId, action.Definition, Vector2.zero, Vector2.zero, melee.DamageTags),
                new DamageContext(10, attacker, recipient, item.ItemId, action.Definition, new Vector2(float.NaN, 0), Vector2.right, melee.DamageTags),
                new DamageContext(10, attacker, recipient, item.ItemId, action.Definition, Vector2.zero, new Vector2(float.PositiveInfinity, 0), melee.DamageTags),
                new DamageContext(10, attacker, recipient, item.ItemId, action.Definition, Vector2.zero, Vector2.right, null) })
                Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(context).Outcome);
            Assert.AreEqual(0, recipient.Calls);
        }
        [Test] public void UntaggedAndTaggedTargetsUseOptionalRulesWithoutBehaviorDispatch()
        {
            Assert.IsTrue(delivery.Deliver(Context(), new GameplayTagQuery(registry)).WasApplied);
            var mechanical = Tag("Tool.Mechanical");
            var required = new GameplayTagQuery(registry, required: new[] { mechanical });
            Assert.AreEqual(DamageOutcome.Ineligible, delivery.Deliver(Context(), required).Outcome);
            Assert.IsTrue(delivery.Deliver(Context(), new GameplayTagQuery(registry, excluded: new[] { mechanical })).WasApplied);
            recipient.AssignedTags = new GameplayTagSet(registry, new[] { mechanical });
            Assert.IsTrue(delivery.Deliver(Context(), required).WasApplied);
            Assert.AreEqual(DamageOutcome.Ineligible, delivery.Deliver(Context(), new GameplayTagQuery(registry, excluded: new[] { mechanical })).Outcome);
            var foreign = new GameplayTagRegistry(Array.Empty<GameplayTagSpec>());
            Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(Context(), new GameplayTagQuery(foreign)).Outcome);
            Assert.AreEqual(3, recipient.Calls);
        }
        [Test] public void UnavailableDisabledAndDestroyedTargetsCannotReceiveDamage()
        {
            recipient.Available = false; Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context()).Outcome);
            recipient.Available = true; recipient.enabled = false;
            Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context()).Outcome);
            var context = Context(); Object.DestroyImmediate(target);
            Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(context).Outcome);
        }
        private PlayerDamageRecipient Player(out PlayerVitalsController vitals)
        {
            vitals = target.AddComponent<PlayerVitalsController>(); feature = target.AddComponent<PlayerVitalsComposition>();
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            feature.Configure(vitals); feature.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "damage", null, null));
            return target.AddComponent<PlayerDamageRecipient>();
        }
        [Test] public void PlayerUsesExistingHealthAndRecoveryDelayWithoutASecondStore()
        {
            var player = Player(out var vitals); var authoritative = vitals.Vitals;
            Assert.IsTrue(delivery.Deliver(Context(10, player)).WasApplied);
            Assert.AreSame(authoritative, vitals.Vitals); Assert.AreEqual(90, vitals.Vitals.Health.Current);
            Assert.AreEqual(vitals.Settings.DamageDelay, vitals.RecoveryState.DelayRemaining);
            vitals.Advance(vitals.Settings.DamageDelay); Assert.AreEqual(90, vitals.Vitals.Health.Current);
            vitals.Advance(1); Assert.AreEqual(92, vitals.Vitals.Health.Current);
            using (pause.Acquire())
            { Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10, player)).Outcome); Assert.AreEqual(92, vitals.Vitals.Health.Current); }
            feature.Release(); Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10, player)).Outcome);
        }
        [Test] public void PlayerInvalidDeadDisabledAndUninitializedTagRecipientsRejectWithoutMutation()
        {
            var player = Player(out var vitals); long revision = vitals.Revision;
            Assert.AreEqual(DamageOutcome.InvalidContext, delivery.Deliver(Context(double.NaN, player)).Outcome);
            player.enabled = false; Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10, player)).Outcome);
            player.enabled = true; var tags = target.AddComponent<GameplayTagSource>();
            Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10, player)).Outcome);
            Object.DestroyImmediate(tags); Assert.AreEqual(revision, vitals.Revision);
            var result = delivery.Deliver(Context(double.MaxValue, player));
            Assert.IsTrue(result.WasApplied); Assert.AreEqual(100, result.AppliedAmount); Assert.AreEqual(0, vitals.Vitals.Health.Current);
            revision = vitals.Revision; Assert.AreEqual(DamageOutcome.Unavailable, delivery.Deliver(Context(10, player)).Outcome);
            Assert.AreEqual(revision, vitals.Revision);
        }
    }
}
#endif
