using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.GameplayTags;
using SOLITUDE.Items;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class ItemActionAuthoringTests
    {
        private readonly List<Object> temporary = new();
        private GameplayTagRegistryAsset registry;
        private T Temp<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); temporary.Add(value); return value; }
        private ItemDatabase Database(params ItemDefinition[] items)
        {
            var database = Temp<ItemDatabase>(); database.TagRegistry = registry;
            var serialized = new SerializedObject(database); var list = serialized.FindProperty("items"); list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); return database;
        }
        private ItemDefinition Item(string id)
        {
            var item = Temp<ItemDefinition>();
            var serialized = new SerializedObject(item); serialized.FindProperty("itemId").stringValue = id; serialized.ApplyModifiedPropertiesWithoutUndo(); return item;
        }
        private MeleeAttackAction Action()
        { var action = Temp<MeleeAttackAction>(); action.Configuration = Temp<MeleeAttackConfiguration>(); return action; }
        [SetUp] public void Setup()
        { registry = AssetDatabase.LoadAssetAtPath<GameplayTagRegistryAsset>("Assets/_Project/Systems/GameplayTags/Assets/Registry.asset"); Assert.NotNull(registry); }
        [TearDown] public void Cleanup() { foreach (var value in temporary) Object.DestroyImmediate(value); temporary.Clear(); }

        [Test] public void ShippedWrenchHasConfiguredPrimaryAndSixTags()
        {
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/Items/Resources/Database.asset");
            var wrench = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
            Assert.NotNull(wrench); Assert.IsFalse(wrench.Stackable); Assert.AreEqual(1, wrench.MaxStackSize);
            Assert.AreSame(wrench, database.Resolve(wrench.ItemId));
            var caps = database.BuildCapabilityCatalog().Resolve(wrench.ItemId);
            CollectionAssert.AreEquivalent(new[] { "Item.Weapon", "Item.Tool", "Item.Improvised", "Weapon.Melee", "Damage.Blunt", "Tool.Mechanical" }, Names(caps.Tags));
            Assert.AreEqual(1, caps.Actions.Count); Assert.IsNull(caps.Resolve(ItemActionSlot.Secondary));
            var spec = (MeleeAttackSpec)caps.Resolve(ItemActionSlot.Primary).Configuration;
            Assert.AreEqual(10, spec.BaseDamage); Assert.AreEqual(1.25f, spec.RangeTiles); Assert.AreEqual(90, spec.ArcDegrees);
            Assert.That(spec.Windup, Is.EqualTo(.12).Within(.000001)); Assert.That(spec.ActiveDuration, Is.EqualTo(.10).Within(.000001));
            Assert.That(spec.Recovery, Is.EqualTo(.33).Within(.000001)); Assert.That(spec.TotalDuration, Is.EqualTo(.55).Within(.000001));
            Assert.That(spec.MovementFactor, Is.EqualTo(.65f)); Assert.AreEqual(.65f, spec.KnockbackTiles);
            Assert.AreEqual(.14f, spec.KnockbackDuration); Assert.AreEqual(.16f, spec.FlashDuration);
            CollectionAssert.AreEqual(new[] { "Damage.Blunt" }, Names(spec.DamageTags)); Assert.IsTrue(spec.TargetEligibility.Matches(caps.Tags));
            Assert.AreEqual(2.5f, spec.RangeWorld(2)); Assert.Throws<ArgumentOutOfRangeException>(() => spec.RangeWorld(0));
        }
        private static string[] Names(GameplayTagSet set)
        { var names = new string[set.Tags.Count]; for (int i = 0; i < names.Length; i++) names[i] = set.Tags[i].Name; return names; }

        [Test] public void LegacyActionlessItemsAndConsumablesStillCompileWithoutRegistry()
        {
            var item = Item("legacy"); item.ConsumableKind = SOLITUDE.Vitals.VitalConsumableKind.Food; item.RestorationAmount = 12;
            var database = Database(item); database.TagRegistry = null;
            Assert.NotNull(database.BuildRuntimeCatalog()); Assert.NotNull(database.BuildPresentationCatalog());
            Assert.NotNull(database.BuildVitalConsumableCatalog().Resolve("legacy"));
            var caps = database.BuildCapabilityCatalog().Resolve("legacy"); Assert.AreEqual(0, caps.Tags.Tags.Count);
            Assert.IsNull(caps.Resolve(ItemActionSlot.Primary)); Assert.IsNull(database.BuildCapabilityCatalog().Resolve("missing"));
        }
        [Test] public void ReusableActionsCompileIndependentImmutableConfigurations()
        {
            var first = Item("first"); var second = Item("second"); var action = Action();
            first.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, action) };
            second.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, action) };
            var database = Database(first, second); var catalog = database.BuildCapabilityCatalog();
            var a = (MeleeAttackSpec)catalog.Resolve("first").Resolve(ItemActionSlot.Primary).Configuration;
            var b = (MeleeAttackSpec)catalog.Resolve("second").Resolve(ItemActionSlot.Primary).Configuration;
            Assert.AreNotSame(a, b); action.Configuration.BaseDamage = 22;
            Assert.AreEqual(10, a.BaseDamage); Assert.AreEqual(10, b.BaseDamage);
            second.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, Action()) };
            ((MeleeAttackAction)second.Actions[0].Action).Configuration.BaseDamage = 7;
            var next = database.BuildCapabilityCatalog(); Assert.AreEqual(7, ((MeleeAttackSpec)next.Resolve("second").Resolve(ItemActionSlot.Primary).Configuration).BaseDamage);
            Assert.AreEqual(22, ((MeleeAttackSpec)next.Resolve("first").Resolve(ItemActionSlot.Primary).Configuration).BaseDamage);
        }
        [Test] public void InvalidAssignmentsNeverProducePartialCatalogs()
        {
            var item = Item("invalid"); var database = Database(item); var action = Action();
            item.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, null) }; Assert.Throws<InvalidOperationException>(() => database.BuildCapabilityCatalog());
            item.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, action), new ItemActionBinding(ItemActionSlot.Primary, action) }; Assert.Throws<InvalidOperationException>(() => database.BuildCapabilityCatalog());
            item.Actions = new[] { new ItemActionBinding((ItemActionSlot)99, action) }; Assert.Throws<InvalidOperationException>(() => database.BuildCapabilityCatalog());
            item.Actions = new[] { new ItemActionBinding(ItemActionSlot.Primary, action) }; action.Configuration = null; Assert.Throws<InvalidOperationException>(() => database.BuildPresentationCatalog());
            item.Actions = Array.Empty<ItemActionBinding>(); item.GameplayTags = new GameplayTagDefinition[] { null }; Assert.Throws<ArgumentException>(() => database.BuildCapabilityCatalog());
            item.GameplayTags = new[] { registry.GetDefinition(0), registry.GetDefinition(0) }; Assert.Throws<ArgumentException>(() => database.BuildCapabilityCatalog());
            item.GameplayTags = new[] { registry.GetDefinition(0) }; database.TagRegistry = null; Assert.Throws<InvalidOperationException>(() => database.BuildCapabilityCatalog());
        }
        [TestCase("BaseDamage", 0f)] [TestCase("BaseDamage", float.NaN)]
        [TestCase("RangeTiles", -1f)] [TestCase("RangeTiles", float.PositiveInfinity)]
        [TestCase("ArcDegrees", 0f)] [TestCase("ArcDegrees", 361f)]
        [TestCase("Windup", -1f)] [TestCase("ActiveDuration", 0f)] [TestCase("Recovery", float.NaN)]
        [TestCase("MovementFactor", -1f)] [TestCase("MovementFactor", 1.1f)]
        [TestCase("KnockbackTiles", -1f)] [TestCase("KnockbackDuration", 0f)] [TestCase("FlashDuration", -1f)]
        public void InvalidNumbersFailBeforeExecution(string field, float value)
        {
            var action = Action(); typeof(MeleeAttackConfiguration).GetField(field).SetValue(action.Configuration, value);
            Assert.Throws<InvalidOperationException>(() => action.Compile(registry, registry.Compile()));
        }
        [Test] public void OptionalPresentationIsSafeAndZeroWindupRecoveryAreAllowed()
        {
            var action = Action(); action.Configuration.Windup = 0; action.Configuration.Recovery = 0;
            action.Configuration.KnockbackTiles = 0; action.Configuration.KnockbackDuration = 0;
            var spec = (MeleeAttackSpec)action.Compile(registry, registry.Compile());
            Assert.IsNull(spec.SwingSound); Assert.IsNull(spec.ImpactSound); Assert.IsNull(spec.ImpactEffect);
            Assert.That(spec.TotalDuration, Is.EqualTo(.1).Within(.000001));
        }
    }
}
