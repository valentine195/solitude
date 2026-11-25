using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Editor;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Tests
{
    public class PlayerVitalsAuthoringTests
    {
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [Test] public void SavedWakeupHasRegisteredVitalsAndAuthoredHudWithApprovedDefaults()
        {
            var scene = EditorSceneManager.OpenScene(PlayerVitalsAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var feature = All<PlayerVitalsComposition>(scene).Single(); var player = All<PlayerVitalsController>(scene).Single();
                Assert.AreSame(player, feature.Controller); feature.Validate();
                var scope = All<SceneBindings>(scene).Single(); var features = new SerializedObject(scope).FindProperty("features");
                Assert.AreEqual(1, Enumerable.Range(0, features.arraySize).Count(i => features.GetArrayElementAtIndex(i).objectReferenceValue == feature)); scope.Validate();
                var hud = All<Canvas>(scene).Single(c => c.name == "HUDCanvas").transform.Find("PlayerVitalsHud");
                Assert.AreEqual(PlayerVitalsAuthoring.HudPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud));
                Assert.AreEqual(3, hud.GetComponentsInChildren<VitalBarView>().Length); Assert.AreEqual(1, hud.GetComponentsInChildren<VitalRecoveryStatusView>().Length);
                Assert.IsFalse(hud.parent.Find("Vitals").gameObject.activeSelf);
                Assert.AreEqual(Vector2.zero, ((RectTransform)hud).anchorMin); Assert.AreEqual(new Vector2(12,12), ((RectTransform)hud).anchoredPosition);
                foreach (var graphic in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) Assert.IsFalse(graphic.raycastTarget);
                var settings = new SerializedObject(player);
                foreach (var name in new[] { "maxHealth", "currentHealth", "maxHunger", "currentHunger", "maxOxygen", "currentOxygen" }) Assert.AreEqual(100, settings.FindProperty(name).floatValue);
                Assert.AreEqual(4, settings.FindProperty("healthSegments").intValue); Assert.AreEqual(.2, settings.FindProperty("fedThresholdFraction").doubleValue);
                Assert.AreEqual(.1, settings.FindProperty("hungerDepletionRate").doubleValue); Assert.AreEqual(5, settings.FindProperty("recoveryDelay").doubleValue); Assert.AreEqual(2, settings.FindProperty("recoveryRate").doubleValue);
                var scaler = hud.GetComponentInParent<UnityEngine.UI.CanvasScaler>(); Assert.AreEqual(new Vector2(1920,1080), scaler.referenceResolution); Assert.AreEqual(UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize,scaler.uiScaleMode);
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        [Test] public void RepeatedAuthoringKeepsPlayerHudAndOtherFeatureIdentities()
        {
            var scene = EditorSceneManager.OpenScene(PlayerVitalsAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = All<PlayerVitalsController>(scene).Single(); var feature = All<PlayerVitalsComposition>(scene).Single();
                var status = All<VitalRecoveryStatusView>(scene).Single(); var other = All<SOLITUDE.Player.Succession.SurvivorSuccessionComposition>(scene).Single();
                for (int i = 0; i < 3; i++) PlayerVitalsAuthoring.ConfigureScene(scene);
                Assert.AreSame(player, All<PlayerVitalsController>(scene).Single()); Assert.AreSame(feature, All<PlayerVitalsComposition>(scene).Single());
                Assert.AreSame(status, All<VitalRecoveryStatusView>(scene).Single()); Assert.AreSame(other, All<SOLITUDE.Player.Succession.SurvivorSuccessionComposition>(scene).Single());
                All<SceneBindings>(scene).Single().Validate();
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        [Test] public void SceneValidationRejectsUnregisteredVitalsFeature()
        {
            var scene = EditorSceneManager.OpenScene(PlayerVitalsAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var scope = All<SceneBindings>(scene).Single(); var feature = All<PlayerVitalsComposition>(scene).Single();
                var data = new SerializedObject(scope); var features = data.FindProperty("features");
                for (int i = features.arraySize-1; i>=0; i--) if(features.GetArrayElementAtIndex(i).objectReferenceValue == feature)
                { features.GetArrayElementAtIndex(i).objectReferenceValue=null; features.DeleteArrayElementAtIndex(i); }
                data.ApplyModifiedPropertiesWithoutUndo(); Assert.Throws<System.InvalidOperationException>(()=>scope.Validate());
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
