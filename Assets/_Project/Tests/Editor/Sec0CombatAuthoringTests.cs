using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Combat;
using SOLITUDE.Editor;
using SOLITUDE.Items;
using SOLITUDE.Npcs.Sec0;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class Sec0CombatAuthoringTests
    {
        private string folder, output;
        private readonly Dictionary<string, byte[]> production = new();
        private readonly Dictionary<string, string> identities = new();
        private const string Root = "Assets/_Project/Features/Npcs/Sec0";
        [SetUp] public void Prepare()
        {
            production.Clear(); identities.Clear();
            foreach (string path in new[] { Sec0NpcBuilder.PrefabPath, Root + "/Animations/PF_Sec0Visual.controller",
                Root + "/Animations/Charge_south.anim", Root + "/Animations/Charge_west.anim",
                Root + "/Animations/Charge_east.anim", Root + "/Animations/Charge_north.anim" })
            { production.Add(path, File.ReadAllBytes(path)); identities.Add(path, AssetDatabase.AssetPathToGUID(path)); }
            folder = "Assets/_Project/Tests/Sec0Authoring" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(folder)); output = folder + "/GeneratedSec0.prefab";
        }
        [TearDown] public void Cleanup()
        {
            if (folder != null) AssetDatabase.DeleteAsset(folder);
            foreach (var asset in production)
            {
                CollectionAssert.AreEqual(asset.Value, File.ReadAllBytes(asset.Key), "Disposable rebuild changed " + asset.Key);
                Assert.AreEqual(identities[asset.Key], AssetDatabase.AssetPathToGUID(asset.Key));
            }
        }
        [Test] public void RebuildingDefaultPrefabRetainsFeedbackHealthAndIdentityWithoutTouchingProductionAssets()
        {
            Sec0NpcBuilder.BuildPrefabAtPath(output); string guid = AssetDatabase.AssetPathToGUID(output);
            Assert.IsNotEmpty(guid);
            Sec0NpcBuilder.BuildPrefabAtPath(output); Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(output));
            var contents = PrefabUtility.LoadPrefabContents(output);
            try
            {
                var bot = contents.GetComponentInChildren<Sec0Controller>(); bot.ValidateAuthoring();
                Assert.AreEqual(40, bot.GetComponent<Sec0DamageRecipient>().MaximumHealth);
                Assert.IsInstanceOf<Sec0HitFeedback>(bot.GetComponent<IMeleeHitFeedback>());
                Assert.AreEqual(1, bot.GetComponents<Sec0HitFeedback>().Length);
                Assert.AreEqual(RigidbodyType2D.Kinematic, bot.GetComponent<Rigidbody2D>().bodyType);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        [Test] public void GeneratedRecipientAppliesDamageAndConfiguredFlashAndKnockbackWithOwnedCleanup()
        {
            Sec0NpcBuilder.BuildPrefabAtPath(output);
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(output));
            var actor = new GameObject("Generated SEC-0 attacker");
            var policy = new InputPolicyCoordinator(new PauseCoordinator()); policy.SetReady(true);
            IDisposable lease = null; var owner = new object();
            var bot = instance.GetComponentInChildren<Sec0Controller>();
            try
            {
                var stun = actor.AddComponent<PlayerStunReceiver>();
                bot.Bind(owner, actor.transform, stun, policy, () => true);
                var recipient = bot.GetComponent<Sec0DamageRecipient>(); recipient.Bind(owner, policy);
                var database = Resources.Load<ItemDatabase>("Database"); var catalog = database.BuildCapabilityCatalog();
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
                var action = catalog.Resolve(item.ItemId).Resolve(ItemActionSlot.Primary); var spec = (MeleeAttackSpec)action.Configuration;
                var contact = new DamageContext(spec.BaseDamage, actor, recipient, item.ItemId, action.Definition,
                    bot.transform.position, Vector2.right, spec.DamageTags);
                var applied = new DamageDelivery(catalog.Registry).Deliver(contact);
                Assert.IsTrue(applied.WasApplied); Assert.AreEqual(30, recipient.CurrentHealth);
                var sprite = bot.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>(); var original = sprite.color;
                lease = bot.GetComponent<IMeleeHitFeedback>().Present(contact, spec, 1);
                Assert.NotNull(lease); Assert.AreNotEqual(original, sprite.color);
                Assert.NotNull(Field<object>(bot, "knockbackOwner"));
                Assert.AreEqual(spec.KnockbackTiles, Field<float>(bot, "knockbackDistance"));
                Assert.AreEqual(spec.KnockbackDuration, Field<float>(bot, "knockbackTime"));
                lease.Dispose(); Assert.AreEqual(original, sprite.color); Assert.IsNull(Field<object>(bot, "knockbackOwner"));
            }
            finally
            {
                lease?.Dispose(); bot.Release(owner); policy.Dispose();
                Object.DestroyImmediate(instance); Object.DestroyImmediate(actor);
            }
        }
        private static T Field<T>(object value, string name) =>
            (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    }
}
