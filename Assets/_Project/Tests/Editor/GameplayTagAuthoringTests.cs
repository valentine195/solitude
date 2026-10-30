using System;
using System.Collections.Generic;
using NUnit.Framework;
using SOLITUDE.GameplayTags;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class GameplayTagAuthoringTests
    {
        private string folder;
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameplayTagDefinition tool, mechanical;
        private GameplayTagRegistryAsset registry;

        [SetUp] public void SetUp()
        {
            folder = "Assets/_Project/Tests/Editor/GeneratedGameplayTags_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests/Editor", folder.Substring(folder.LastIndexOf('/') + 1));
            tool = Definition("Tool", "Tool"); mechanical = Definition("Mechanical", "Mechanical", tool);
            registry = ScriptableObject.CreateInstance<GameplayTagRegistryAsset>();
            registry.Configure(new[] { mechanical, tool });
            AssetDatabase.CreateAsset(registry, folder + "/Registry.asset"); AssetDatabase.SaveAssets();
        }

        [TearDown] public void TearDown()
        {
            foreach (var value in objects) if (value != null) Object.DestroyImmediate(value);
            objects.Clear();
            if (!string.IsNullOrEmpty(folder)) AssetDatabase.DeleteAsset(folder);
        }

        private GameplayTagDefinition Definition(string filename, string segment, GameplayTagDefinition parent = null)
        {
            var definition = ScriptableObject.CreateInstance<GameplayTagDefinition>();
            AssetDatabase.CreateAsset(definition, folder + "/" + filename + ".asset");
            definition.Configure(segment, parent); EditorUtility.SetDirty(definition); return definition;
        }

        private GameplayTagSource Source(GameplayTagAuthoring assignments)
        {
            var actor = new GameObject("Gameplay tag fixture"); objects.Add(actor);
            var source = actor.AddComponent<GameplayTagSource>(); source.Configure(registry, assignments); return source;
        }

        [Test] public void SerializedReferencesSurviveAssetMoveRenameAndReadableNameChange()
        {
            string id = mechanical.TagId;
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(folder + "/Mechanical.asset"), id);
            var source = Source(new GameplayTagAuthoring(mechanical, tool));
            string prefabPath = folder + "/TaggedEntity.prefab";
            PrefabUtility.SaveAsPrefabAsset(source.gameObject, prefabPath);
            Object.DestroyImmediate(source.gameObject);
            Assert.AreEqual("", AssetDatabase.RenameAsset(folder + "/Mechanical.asset", "RenamedMechanical"));
            AssetDatabase.CreateFolder(folder, "Moved");
            string moved = folder + "/Moved/RenamedMechanical.asset";
            Assert.AreEqual("", AssetDatabase.MoveAsset(folder + "/RenamedMechanical.asset", moved));
            mechanical.Configure("Engineering", tool); EditorUtility.SetDirty(mechanical);
            AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<GameplayTagDefinition>(moved);
            Assert.AreEqual(id, loaded.TagId); Assert.AreEqual(id, AssetDatabase.AssetPathToGUID(moved));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); objects.Add(instance);
            var reloaded = instance.GetComponent<GameplayTagSource>(); var compiled = registry.Compile();
            reloaded.Initialize(registry, compiled);
            Assert.AreEqual(2, reloaded.Tags.Tags.Count);
            Assert.IsTrue(reloaded.Tags.HasTag(compiled.GetTag(id), GameplayTagMatch.Exact));
            Assert.AreEqual("Tool.Engineering", compiled.GetTag(id).Name);
        }

        [Test] public void RegistryRejectsMissingDuplicateAndUnregisteredAuthoring()
        {
            registry.Configure(new[] { tool, (GameplayTagDefinition)null });
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
            registry.Configure(new[] { tool, tool });
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
            registry.Configure(new[] { mechanical });
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
            registry.Configure(new[] { tool, mechanical }); var compiled = registry.Compile();
            var external = Definition("External", "Electrical", tool);
            Assert.Throws<ArgumentException>(() => new GameplayTagAuthoring(external).Compile(registry, compiled));
            Assert.Throws<ArgumentException>(() => new GameplayTagAuthoring(tool, tool).Compile(registry, compiled));
            Assert.Throws<ArgumentException>(() => new GameplayTagAuthoring(new GameplayTagDefinition[] { null }).Compile(registry, compiled));
        }

        [Test] public void ClonedDefinitionCannotSilentlyReuseAnotherAssetIdentity()
        {
            var duplicate = Object.Instantiate(mechanical);
            AssetDatabase.CreateAsset(duplicate, folder + "/Duplicate.asset");
            Assert.AreEqual(mechanical.TagId, duplicate.TagId);
            Assert.AreNotEqual(AssetDatabase.AssetPathToGUID(folder + "/Duplicate.asset"), duplicate.TagId);
            registry.Configure(new[] { tool, mechanical, duplicate });
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
        }

        [Test] public void LostChildParentIsNotReinterpretedAsARoot()
        {
            var serialized = new SerializedObject(mechanical);
            serialized.FindProperty("parent").objectReferenceValue = null; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(mechanical.IsRoot);
            StringAssert.Contains("missing parent", string.Join("\n", registry.CollectErrors()));
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
        }

        [Test] public void RootParentContradictionAndCyclesAreRejected()
        {
            var serialized = new SerializedObject(tool);
            serialized.FindProperty("parent").objectReferenceValue = mechanical; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => registry.Compile());
            serialized.FindProperty("isRoot").boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo();
            StringAssert.Contains("cycle", string.Join("\n", registry.CollectErrors()));
        }

        [Test] public void EmptySourcesAndQueryGroupsCompileAgainstOneSnapshot()
        {
            var compiled = registry.Compile(); var source = Source(new GameplayTagAuthoring());
            source.ValidateAuthoring(); source.Initialize(registry, compiled);
            Assert.IsTrue(new GameplayTagQueryAuthoring().Compile(registry, compiled).Matches(source.Tags));
            var query = new GameplayTagQueryAuthoring(GameplayTagMatch.Hierarchical,
                new GameplayTagAuthoring(tool), new GameplayTagAuthoring(mechanical), new GameplayTagAuthoring());
            Assert.IsFalse(query.Compile(registry, compiled).Matches(source.Tags));
            source.Configure(registry, new GameplayTagAuthoring(mechanical)); source.Initialize(registry, compiled);
            Assert.IsTrue(query.Compile(registry, compiled).Matches(source.Tags));
        }

        [Test] public void SourceReleaseAndReconfigurationDoNotMutateSharedDefinitions()
        {
            var compiled = registry.Compile(); var a = Source(new GameplayTagAuthoring(mechanical));
            var b = Source(new GameplayTagAuthoring(mechanical));
            a.Initialize(registry, compiled); b.Initialize(registry, compiled);
            a.Release(); a.Release();
            Assert.IsFalse(a.IsInitialized); Assert.IsTrue(b.Tags.HasTag(compiled.GetTag(tool.TagId)));
            Assert.Throws<InvalidOperationException>(() => { var unused = a.Tags; });
            a.Configure(registry, new GameplayTagAuthoring(new GameplayTagDefinition[] { null }));
            Assert.Throws<ArgumentException>(() => a.Initialize(registry, compiled));
            Assert.IsFalse(a.IsInitialized); Assert.IsTrue(b.IsInitialized);
        }

        [Test] public void InvalidQueryGroupsAndMatchModeAreNotEmptyQueries()
        {
            var compiled = registry.Compile();
            Assert.Throws<InvalidOperationException>(() => new GameplayTagQueryAuthoring(GameplayTagMatch.Exact,
                null, new GameplayTagAuthoring(), new GameplayTagAuthoring()).Compile(registry, compiled));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayTagQueryAuthoring((GameplayTagMatch)99,
                new GameplayTagAuthoring(), new GameplayTagAuthoring(), new GameplayTagAuthoring()).Compile(registry, compiled));
        }

        [Test] public void InitialAuthoredRegistryContainsExactlyTheRequiredWrenchClassifications()
        {
            var authored = AssetDatabase.LoadAssetAtPath<GameplayTagRegistryAsset>(SOLITUDE.Editor.GameplayTagValidator.RegistryPath);
            Assert.IsNotNull(authored); Assert.AreEqual(10, authored.Count);
            var compiled = authored.Compile(); var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tag in compiled.Tags) names.Add(tag.Name);
            CollectionAssert.AreEquivalent(new[] { "Item", "Weapon", "Damage", "Tool", "Item.Weapon", "Item.Tool", "Item.Improvised",
                "Weapon.Melee", "Damage.Blunt", "Tool.Mechanical" }, names);
        }
    }
}
