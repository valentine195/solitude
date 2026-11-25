using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Editor;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Tests
{
    public class SurvivorSuccessionAuthoringTests
    {
        [Test] public void RecoveryPrefabAuthoringIsIdempotentAndHasNoPlayerOrSaveableOwner()
        {
            var guid = AssetDatabase.AssetPathToGUID(SurvivorSuccessionAuthoring.RecoveryPrefabPath);
            Assert.IsNotEmpty(guid);
            SurvivorSuccessionAuthoring.AuthorRecoveryPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurvivorSuccessionAuthoring.RecoveryPrefabPath);
            prefab.GetComponent<SurvivorRecoveryContainer>().ValidateAuthoring();
            Assert.IsFalse(prefab.activeSelf);
            Assert.IsEmpty(prefab.GetComponentsInChildren<PlayerInteractor>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<SOLITUDE.SaveLoad.SaveableId>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<SOLITUDE.Features.Interactables.LockerContainer>(true));
            SurvivorSuccessionAuthoring.AuthorRecoveryPrefab();
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(SurvivorSuccessionAuthoring.RecoveryPrefabPath));
        }
        [Test] public void AuthoringIsIdempotentAndPreservesScenePlayerAndFeatureBindings()
        {
            var guid = AssetDatabase.AssetPathToGUID(SurvivorSuccessionAuthoring.ScenePath);
            SurvivorSuccessionAuthoring.AuthorWakeup();
            var scene = EditorSceneManager.OpenScene(SurvivorSuccessionAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var scope = All<SceneBindings>(scene).Single(); var player = All<PlayerInteractor>(scene).Single();
                var feature = All<SurvivorSuccessionComposition>(scene).Single(); var point = All<SurvivorAwakeningPoint>(scene).Single();
                var before = new SerializedObject(scope).FindProperty("features").arraySize;
                SurvivorSuccessionAuthoring.AuthorWakeup();
                Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(SurvivorSuccessionAuthoring.ScenePath));
                Assert.AreSame(player, All<PlayerInteractor>(scene).Single());
                Assert.AreSame(scope, All<SceneBindings>(scene).Single());
                Assert.AreSame(point, feature.AwakeningPoint);
                Assert.AreEqual(SurvivorSuccessionAuthoring.RecoveryPrefabPath, AssetDatabase.GetAssetPath(feature.RecoveryPrefab));
                Assert.AreEqual(before, new SerializedObject(scope).FindProperty("features").arraySize);
                Assert.AreEqual(player.transform.position, point.transform.position);
                scope.Validate(); AssertFullAuthoredViewport(SurvivorSuccessionAuthoring.ScenePath);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        [Test] public void ScrubberAuthoringAndBuilderPreserveSuccessionAndShipBindings()
        {
            var path = ScrubberEncounterBuilder.ScenePath;
            var guid = AssetDatabase.AssetPathToGUID(path);
            SurvivorSuccessionAuthoring.AuthorScrubberDemo();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var scope = All<SceneBindings>(scene).Single(); var player = All<PlayerInteractor>(scene).Single();
                var feature = All<SurvivorSuccessionComposition>(scene).Single(); var point = feature.AwakeningPoint;
                var equipment = All<SOLITUDE.World.ScrubberEncounter.ScrubberEquipment>(scene).Single();
                SurvivorSuccessionAuthoring.AuthorScrubberDemo(); ScrubberEncounterBuilder.MigrateComposition();
                Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
                Assert.AreSame(feature, All<SurvivorSuccessionComposition>(scene).Single()); Assert.AreSame(point, feature.AwakeningPoint);
                Assert.AreSame(equipment, All<SOLITUDE.World.ScrubberEncounter.ScrubberEquipment>(scene).Single());
                Assert.AreSame(player, All<PlayerInteractor>(scene).Single()); Assert.AreEqual(player.transform.position, point.transform.position);
                Assert.AreEqual(2, new SerializedObject(scope).FindProperty("features").arraySize);
                Assert.AreEqual(SurvivorSuccessionAuthoring.RecoveryPrefabPath, AssetDatabase.GetAssetPath(feature.RecoveryPrefab));
                scope.Validate(); AssertFullAuthoredViewport(path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        [TestCase("missing")][TestCase("wrong-scene")][TestCase("inactive")]
        public void InvalidAwakeningConfigurationIsRejected(string defect)
        {
            var scene = EditorSceneManager.OpenScene(SurvivorSuccessionAuthoring.ScenePath, OpenSceneMode.Additive);
            var other = EditorSceneManager.NewPreviewScene();
            try
            {
                var feature = All<SurvivorSuccessionComposition>(scene).Single();
                var avatar = All<SurvivorAvatarAdapter>(scene).Single();
                if (defect == "missing") feature.Configure(null, avatar);
                if (defect == "inactive") feature.AwakeningPoint.gameObject.SetActive(false);
                if (defect == "wrong-scene")
                {
                    var go = new GameObject("ForeignPoint"); SceneManager.MoveGameObjectToScene(go, other);
                    feature.Configure(go.AddComponent<SurvivorAwakeningPoint>(), avatar);
                }
                Assert.Throws<InvalidOperationException>(() => feature.Validate());
            }
            finally { EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(other); }
        }
        private static void AssertFullAuthoredViewport(string path)
        {
            var root = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var text = File.ReadAllText(Path.Combine(root, path));
            Assert.IsTrue(Regex.IsMatch(text, @"m_NormalizedViewPortRect:\s+serializedVersion: 2\s+x: 0\s+y: 0\s+width: 1\s+height: 1\s"),
                "Authoring must not serialize the Editor's temporary letterbox viewport.");
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
