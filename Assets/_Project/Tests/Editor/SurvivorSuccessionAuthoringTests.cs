using System;
using System.Linq;
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
                Assert.AreEqual(before, new SerializedObject(scope).FindProperty("features").arraySize);
                Assert.AreEqual(player.transform.position, point.transform.position);
                scope.Validate();
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
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
