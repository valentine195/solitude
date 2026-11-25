using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.GameplayTags;

namespace SOLITUDE.Tests
{
    public sealed class GameplayTagTests
    {
        private GameplayTagRegistry registry;
        private GameplayTagSet wrench;
        [SetUp] public void SetUp()
        {
            registry = new GameplayTagRegistry(Specs());
            wrench = new GameplayTagSet(registry, Tags("itemWeapon", "itemTool", "improvised", "melee", "blunt", "mechanical"));
        }

        private static GameplayTagSpec[] Specs() => new[]
        {
            new GameplayTagSpec("melee", "Melee", "weapon"), // Deliberately child-first.
            new GameplayTagSpec("item", "Item"), new GameplayTagSpec("weapon", "Weapon"),
            new GameplayTagSpec("damage", "Damage"), new GameplayTagSpec("tool", "Tool"), new GameplayTagSpec("state", "State"),
            new GameplayTagSpec("itemWeapon", "Weapon", "item"), new GameplayTagSpec("itemTool", "Tool", "item"),
            new GameplayTagSpec("improvised", "Improvised", "item"), new GameplayTagSpec("blunt", "Blunt", "damage"),
            new GameplayTagSpec("mechanical", "Mechanical", "tool"), new GameplayTagSpec("electrical", "Electrical", "tool"),
            new GameplayTagSpec("incapacitated", "Incapacitated", "state")
        };

        private GameplayTag[] Tags(params string[] ids)
        {
            var tags = new GameplayTag[ids.Length];
            for (int i = 0; i < ids.Length; i++) tags[i] = registry.GetTag(ids[i]);
            return tags;
        }

        [Test] public void MultipleClassificationsRetainExplicitAssignmentsAndAncestors()
        {
            Assert.AreEqual(6, wrench.Tags.Count);
            Assert.IsTrue(wrench.HasAll(Tags("itemWeapon", "itemTool", "improvised", "melee", "blunt", "mechanical"), GameplayTagMatch.Exact));
            Assert.IsTrue(wrench.HasAll(Tags("item", "weapon", "damage", "tool")));
            Assert.IsFalse(wrench.HasTag(registry.GetTag("weapon"), GameplayTagMatch.Exact));
            Assert.AreEqual("Weapon.Melee", registry.GetTag("melee").Name);
            Assert.AreEqual("melee", registry.GetTag("melee").Id);
        }

        [TestCase("melee", "melee", true, true)]
        [TestCase("melee", "weapon", false, true)]
        [TestCase("weapon", "melee", false, false)]
        [TestCase("mechanical", "weapon", false, false)]
        public void ExactAndHierarchicalMatchingAreDirectional(string owned, string requested, bool exact, bool hierarchical)
        {
            var set = new GameplayTagSet(registry, Tags(owned));
            Assert.AreEqual(exact, set.HasTag(registry.GetTag(requested), GameplayTagMatch.Exact));
            Assert.AreEqual(hierarchical, set.HasTag(registry.GetTag(requested), GameplayTagMatch.Hierarchical));
        }

        [Test] public void RequiredAnyAndExcludedConditionsComposeWithoutBehaviorDispatch()
        {
            var allowed = new GameplayTagQuery(registry, required: Tags("itemWeapon", "weapon"),
                anyOf: Tags("electrical", "mechanical"), excluded: Tags("incapacitated"));
            Assert.IsTrue(allowed.Matches(wrench));
            Assert.IsFalse(allowed.Matches(registry.EmptySet));
            Assert.IsFalse(new GameplayTagQuery(registry, required: Tags("electrical")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, anyOf: Tags("electrical", "incapacitated")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, excluded: Tags("tool")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, required: Tags("melee"), excluded: Tags("melee")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, GameplayTagMatch.Exact, required: Tags("weapon")).Matches(wrench));
        }

        [Test] public void QueryUsesOneExplicitMatchModeForAllGroups()
        {
            Assert.IsTrue(new GameplayTagQuery(registry, anyOf: Tags("weapon")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, GameplayTagMatch.Exact, anyOf: Tags("weapon")).Matches(wrench));
            Assert.IsFalse(new GameplayTagQuery(registry, excluded: Tags("weapon")).Matches(wrench));
            Assert.IsTrue(new GameplayTagQuery(registry, GameplayTagMatch.Exact, excluded: Tags("weapon")).Matches(wrench));
        }

        [Test] public void EmptyPredicatesAndWholeQueriesHaveDifferentAnySemantics()
        {
            var empty = Array.Empty<GameplayTag>();
            foreach (var set in new[] { registry.EmptySet, wrench })
            {
                Assert.IsTrue(set.HasAll(empty)); Assert.IsFalse(set.HasAny(empty)); Assert.IsTrue(set.HasNone(empty));
                Assert.IsTrue(new GameplayTagQuery(registry).Matches(set));
            }
            Assert.IsFalse(new GameplayTagQuery(registry, required: Tags("melee")).Matches(registry.EmptySet));
            Assert.IsFalse(new GameplayTagQuery(registry, anyOf: Tags("melee")).Matches(registry.EmptySet));
            Assert.IsTrue(new GameplayTagQuery(registry, excluded: Tags("melee")).Matches(registry.EmptySet));
        }

        [Test] public void DirectCollectionPredicatesMatchTheirConditions()
        {
            Assert.IsTrue(wrench.HasAll(Tags("melee", "blunt")));
            Assert.IsFalse(wrench.HasAll(Tags("melee", "electrical")));
            Assert.IsTrue(wrench.HasAny(Tags("electrical", "mechanical")));
            Assert.IsFalse(wrench.HasAny(Tags("electrical")));
            Assert.IsTrue(wrench.HasNone(Tags("electrical", "incapacitated")));
            Assert.IsFalse(wrench.HasNone(Tags("blunt")));
        }

        [Test] public void MissingAndForeignReferencesCannotMasqueradeAsEmptyConditions()
        {
            Assert.Throws<ArgumentNullException>(() => wrench.HasTag(null));
            Assert.Throws<ArgumentNullException>(() => wrench.HasAll(null));
            Assert.Throws<ArgumentNullException>(() => wrench.HasAny(new[] { registry.GetTag("melee"), null }));
            Assert.Throws<ArgumentNullException>(() => wrench.HasAll(new[] { registry.GetTag("electrical"), null }));
            Assert.Throws<ArgumentNullException>(() => new GameplayTagQuery(registry, required: new GameplayTag[] { null }));
            Assert.Throws<ArgumentNullException>(() => new GameplayTagQuery(registry).Matches(null));
            var other = new GameplayTagRegistry(Specs());
            Assert.Throws<ArgumentException>(() => wrench.HasTag(other.GetTag("melee")));
            Assert.Throws<ArgumentException>(() => new GameplayTagSet(registry, new[] { other.GetTag("melee") }));
            Assert.Throws<ArgumentException>(() => new GameplayTagQuery(registry, excluded: new[] { other.GetTag("melee") }));
            Assert.Throws<ArgumentException>(() => new GameplayTagQuery(registry).Matches(other.EmptySet));
        }

        [Test] public void DuplicateAssignmentsAndInvalidMatchModesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new GameplayTagSet(registry, Tags("melee", "melee")));
            Assert.Throws<ArgumentNullException>(() => new GameplayTagSet(registry, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => wrench.HasTag(registry.GetTag("melee"), (GameplayTagMatch)55));
            Assert.Throws<ArgumentOutOfRangeException>(() => wrench.HasAny(Array.Empty<GameplayTag>(), (GameplayTagMatch)55));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayTagQuery(registry, (GameplayTagMatch)55));
        }

        [Test] public void CompiledRegistrySetsAndQueriesFreezeTheirInputs()
        {
            var specs = Specs(); var compiled = new GameplayTagRegistry(specs);
            specs[0] = new GameplayTagSpec("melee", "Changed");
            Assert.AreEqual("Weapon.Melee", compiled.GetTag("melee").Name);
            var assigned = Tags("melee"); var set = new GameplayTagSet(registry, assigned);
            assigned[0] = registry.GetTag("electrical");
            Assert.IsTrue(set.HasTag(registry.GetTag("melee"), GameplayTagMatch.Exact));
            var required = new List<GameplayTag>(Tags("melee")); var query = new GameplayTagQuery(registry, required: required);
            required.Clear(); required.Add(registry.GetTag("electrical"));
            Assert.IsTrue(query.Matches(wrench));
            Assert.Throws<NotSupportedException>(() => ((ICollection<GameplayTag>)set.Tags).Add(registry.GetTag("electrical")));
        }

        [TestCase("missing")]
        [TestCase("emptyId")]
        [TestCase("duplicateId")]
        [TestCase("badSegment")]
        [TestCase("duplicateName")]
        [TestCase("missingParent")]
        [TestCase("selfParent")]
        [TestCase("cycle")]
        public void InvalidDefinitionsProduceDeterministicDiagnosticsAndNoRegistry(string kind)
        {
            GameplayTagSpec[] specs;
            switch (kind)
            {
                case "missing": specs = new GameplayTagSpec[] { null }; break;
                case "emptyId": specs = new[] { new GameplayTagSpec(" ", "Weapon") }; break;
                case "duplicateId": specs = new[] { new GameplayTagSpec("a", "Weapon"), new GameplayTagSpec("a", "Tool") }; break;
                case "badSegment": specs = new[] { new GameplayTagSpec("a", "Weapon.Melee") }; break;
                case "duplicateName": specs = new[] { new GameplayTagSpec("a", "Weapon"), new GameplayTagSpec("b", "Weapon") }; break;
                case "missingParent": specs = new[] { new GameplayTagSpec("a", "Melee", "absent") }; break;
                case "selfParent": specs = new[] { new GameplayTagSpec("a", "Weapon", "a") }; break;
                default: specs = new[] { new GameplayTagSpec("a", "Weapon", "b"), new GameplayTagSpec("b", "Melee", "a") }; break;
            }
            var errors = GameplayTagRegistry.Validate(specs);
            Assert.Greater(errors.Count, 0);
            CollectionAssert.AreEqual(errors, GameplayTagRegistry.Validate(specs));
            Assert.Throws<ArgumentException>(() => new GameplayTagRegistry(specs));
        }

        [TestCase(null)] [TestCase("")] [TestCase("1Weapon")] [TestCase("Bad Name")] [TestCase("Bad-Name")]
        public void MalformedSegmentsAreRejected(string segment)
            => Assert.Greater(GameplayTagRegistry.Validate(new[] { new GameplayTagSpec("id", segment) }).Count, 0);

        [Test] public void FullNameUniquenessIncludesParentsAndIsCaseSensitive()
        {
            var valid = new GameplayTagRegistry(new[]
            {
                new GameplayTagSpec("a", "Item"), new GameplayTagSpec("b", "Weapon"),
                new GameplayTagSpec("c", "Melee", "a"), new GameplayTagSpec("d", "Melee", "b"),
                new GameplayTagSpec("e", "melee", "b")
            });
            Assert.AreEqual("Item.Melee", valid.GetTag("c").Name);
            Assert.AreEqual("Weapon.Melee", valid.GetTag("d").Name);
            Assert.AreEqual("Weapon.melee", valid.GetTag("e").Name);
            Assert.Throws<ArgumentException>(() => new GameplayTagRegistry(new[]
            {
                new GameplayTagSpec("a", "Item"), new GameplayTagSpec("c", "Weapon", "a"), new GameplayTagSpec("d", "Weapon", "a")
            }));
        }

        [Test] public void EmptyRegistryIsValidButMissingIdentityLookupIsNot()
        {
            var empty = new GameplayTagRegistry(Array.Empty<GameplayTagSpec>());
            Assert.IsTrue(new GameplayTagQuery(empty).Matches(empty.EmptySet));
            Assert.IsFalse(empty.TryGetTag(null, out _)); Assert.IsFalse(empty.TryGetTag("unknown", out _));
            Assert.Throws<ArgumentException>(() => empty.GetTag("unknown"));
            Assert.Throws<ArgumentNullException>(() => new GameplayTagRegistry(null));
            Assert.Greater(GameplayTagRegistry.Validate(null).Count, 0);
        }
    }
}
