#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class MeleeFeedbackTests
    {
        private MeleeHitTestFixture f;
        private MeleeImpactFeedback feedback;
        private GameObject host, player;
        private Sec0Controller bot;
        private Sec0DamageRecipient recipient;
        private Sec0Composition composition;
        private InputPolicyCoordinator policy;
        private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        [SetUp] public void Prepare()
        { f = new MeleeHitTestFixture(); feedback = f.Actor.AddComponent<MeleeImpactFeedback>(); }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            composition?.Release(); if (host != null) Object.DestroyImmediate(host); policy?.Dispose();
            var scene = f.Scene; f.Dispose(); yield return SceneManager.UnloadSceneAsync(scene);
        }
        private void Begin() { Assert.IsTrue(f.Start()); feedback.Begin(f.Execution); }
        private List<GameObject> Effects => Field<List<GameObject>>(feedback, "effects");
        [Test] public void AcceptedContactCreatesOneSmallMarkerAtReportedPointAndInterruptionReleasesIt()
        {
            var target = f.Box(new Vector2(.8f, 0), Vector2.one * .2f);
            Begin(); f.Advance(.12); Assert.AreEqual(1, target.Calls); Assert.AreEqual(1, Effects.Count);
            var marker = Effects[0]; Assert.AreEqual((Vector3)target.Last.ImpactPosition, marker.transform.position);
            Assert.AreEqual(Vector3.one, marker.transform.localScale);
            f.Advance(.02); Assert.AreEqual(1, target.Calls); Assert.AreEqual(1, Effects.Count);
            feedback.End(f.Execution); Assert.IsFalse(marker.activeSelf); Assert.AreEqual(0, Effects.Count);
        }
        [Test] public void MissAndRejectedEligibilityProduceNoMarker()
        {
            var target = f.Box(Vector2.right * 3, Vector2.one * .2f); Begin(); f.Advance(.55);
            Assert.AreEqual(0, target.Calls); Assert.AreEqual(0, Effects.Count); feedback.End(f.Execution);
            f.Configuration.TargetEligibility = new SOLITUDE.GameplayTags.GameplayTagQueryAuthoring(
                SOLITUDE.GameplayTags.GameplayTagMatch.Hierarchical,
                new SOLITUDE.GameplayTags.GameplayTagAuthoring(new[] { AssetDatabase.LoadAssetAtPath<SOLITUDE.GameplayTags.GameplayTagDefinition>("Assets/_Project/Systems/GameplayTags/Assets/ToolMechanical.asset") }),
                new SOLITUDE.GameplayTags.GameplayTagAuthoring(), new SOLITUDE.GameplayTags.GameplayTagAuthoring());
            target.transform.position = Vector2.right * .8f; Begin(); f.Advance(.55);
            Assert.AreEqual(0, target.Calls); Assert.AreEqual(0, Effects.Count);
        }
        [Test] public void MissingEveryOptionalAssetAndUnsupportedRecipientStillTakesDamage()
        {
            f.Configuration.ImpactEffect = null; f.Configuration.SwingSound = null; f.Configuration.ImpactSound = null;
            var target = f.Box(Vector2.right * .8f, Vector2.one * .2f); Begin(); f.Advance(.55);
            Assert.AreEqual(10, target.Total); Assert.AreEqual(0, Effects.Count); Assert.IsNull(f.Actor.GetComponentInChildren<AudioSource>());
        }
        [UnityTest] public IEnumerator ConfiguredAudioUsesOneOwnedSourceAndStopsOnDisable()
        {
            var swing = AudioClip.Create("Fixture swing", 44100, 1, 44100, false);
            var impact = AudioClip.Create("Fixture impact", 44100, 1, 44100, false);
            try
            {
                f.Configuration.SwingSound = swing; f.Configuration.ImpactSound = impact;
                f.Box(Vector2.right * .8f, Vector2.one * .2f); Begin();
                var source = f.Actor.GetComponentInChildren<AudioSource>(); Assert.NotNull(source); Assert.IsFalse(source.playOnAwake);
                yield return null; Assert.IsTrue(source.isPlaying);
                f.Advance(.12); f.Advance(.02); Assert.AreEqual(1, Effects.Count);
                Assert.AreEqual(1, f.Actor.GetComponentsInChildren<AudioSource>().Length);
                feedback.enabled = false; Assert.IsFalse(source.isPlaying); Assert.AreEqual(0, Effects.Count);
            }
            finally { Object.DestroyImmediate(swing); Object.DestroyImmediate(impact); }
        }
        private Sec0HitFeedback Robot()
        {
            host = new GameObject("SEC-0 feedback fixture");
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab"));
            instance.transform.SetParent(host.transform); bot = instance.GetComponentInChildren<Sec0Controller>(); recipient = bot.GetComponent<Sec0DamageRecipient>();
            player = new GameObject("Player"); player.transform.SetParent(host.transform); player.transform.position = Vector2.down;
            player.AddComponent<Rigidbody2D>().gravityScale = 0; player.AddComponent<BoxCollider2D>().size = Vector2.one * .5f;
            player.AddComponent<Sec0TestAnimation>(); player.AddComponent<MovementBehavior>();
            var stun = player.AddComponent<PlayerStunReceiver>(); var vitals = player.AddComponent<PlayerVitalsController>();
            var interactor = player.AddComponent<PlayerInteractor>(); policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true);
            composition = host.AddComponent<Sec0Composition>(); composition.Configure(new[] { bot }, stun, vitals);
            composition.Initialize(new SceneFeatureContext(null, null, null, policy, null, null, "sec0-feedback", interactor, null));
            Physics2D.SyncTransforms(); return bot.GetComponent<Sec0HitFeedback>();
        }
        private DamageContext Contact() => new DamageContext(10, player, recipient, "fixture", AssetDatabase.LoadAssetAtPath<MeleeAttackAction>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset"), bot.transform.position, Vector2.right,
            f.Configuration.Compile(f.Database.TagRegistry, f.Registry).DamageTags);
        [UnityTest] public IEnumerator FlashRestoresOriginalColorAfterConfiguredDurationWithoutLeakingAcrossHits()
        {
            var hit = Robot(); bot.GetComponentInChildren<Animator>().enabled = false;
            var sprite = bot.GetComponentInChildren<SpriteRenderer>(); var original = new Color(.6f, .7f, .8f, 1); sprite.color = original;
            var spec = f.Configuration.Compile(f.Database.TagRegistry, f.Registry);
            var first = hit.Present(Contact(), spec, 1); Assert.AreNotEqual(original, sprite.color);
            var second = hit.Present(Contact(), spec, 1); first.Dispose(); Assert.AreNotEqual(original, sprite.color);
            yield return new WaitForSeconds(spec.FlashDuration + .05f); Assert.AreEqual(original, sprite.color);
            second.Dispose(); Assert.AreEqual(original, sprite.color);
        }
        [TestCase("release")] [TestCase("disable")] [TestCase("defeat")] [TestCase("interrupt")]
        public void LifecycleCleanupRestoresColorAndCancelsDisplacement(string operation)
        {
            var hit = Robot(); bot.GetComponentInChildren<Animator>().enabled = false;
            var sprite = bot.GetComponentInChildren<SpriteRenderer>(); var original = sprite.color;
            var lease = hit.Present(Contact(), f.Configuration.Compile(f.Database.TagRegistry, f.Registry), 1);
            Assert.NotNull(Field<object>(bot, "knockbackOwner"));
            if (operation == "release") composition.Release();
            else if (operation == "disable") bot.enabled = false;
            else if (operation == "defeat")
            {
                var context = new DamageContext(40, player, recipient, "fixture", AssetDatabase.LoadAssetAtPath<MeleeAttackAction>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeAttack.asset"), bot.transform.position, Vector2.right, Contact().DamageTags);
                new DamageDelivery(f.Registry).Deliver(context);
                Assert.IsNull(hit.Present(Contact(), f.Configuration.Compile(f.Database.TagRegistry, f.Registry), 1));
            }
            else lease.Dispose();
            Assert.AreEqual(original, sprite.color); Assert.IsNull(Field<object>(bot, "knockbackOwner")); lease.Dispose();
        }
        [Test] public void KnockbackUsesExistingCollisionAuthorityAndCannotCrossCover()
        {
            Robot(); bot.GetComponentInChildren<Animator>().enabled = false;
            var body = bot.GetComponent<Rigidbody2D>(); Vector2 start = body.position;
            var cover = new GameObject("Knockback cover"); cover.transform.SetParent(host.transform);
            var box = bot.GetComponent<BoxCollider2D>();
            cover.transform.position = start + box.offset + Vector2.right * (box.size.x / 2 + .07f);
            cover.AddComponent<BoxCollider2D>().size = new Vector2(.02f, 2); Physics2D.SyncTransforms();
            SimulateKnockback(.08f, () => bot.RequestKnockback(this, Vector2.right, .1f, .08f));
            Assert.LessOrEqual(body.position.x - start.x, .051f); Assert.GreaterOrEqual(body.position.x - start.x, 0);
            Assert.IsFalse(box.IsTouching(cover.GetComponent<Collider2D>()));
            Assert.IsNull(Field<object>(bot, "knockbackOwner"));
        }
        private void SimulateKnockback(float seconds, System.Action prepare)
        {
            var mode = Physics2D.simulationMode;
            try { Physics2D.simulationMode = SimulationMode2D.Script; prepare(); bot.Tick(seconds); Physics2D.Simulate(seconds); }
            finally { Physics2D.simulationMode = mode; }
        }
        [Test] public void UnobstructedKnockbackTravelsConfiguredDistanceAndPauseFreezesIt()
        {
            Robot(); bot.GetComponentInChildren<Animator>().enabled = false;
            var body = bot.GetComponent<Rigidbody2D>(); Vector2 start = body.position;
            bot.RequestKnockback(this, Vector2.right, .1f, .08f); policy.SetTransition(true);
            SimulateKnockback(.1f, () => { }); Assert.AreEqual(start, body.position);
            policy.SetTransition(false); SimulateKnockback(.08f, () => { });
            Assert.That(body.position.x - start.x, Is.EqualTo(.1f).Within(.002f));
        }
    }
}
#endif
