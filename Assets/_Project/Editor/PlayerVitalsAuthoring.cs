using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class PlayerVitalsAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Wakeup.unity";
        public const string HudPath = "Assets/_Project/Features/Player/Vitals/Views/PlayerVitalsHud.prefab";
        [MenuItem("SOLITUDE/Vitals/Author Wakeup HUD")]
        public static void AuthorWakeup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author vitals outside Play Mode.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Resolve existing Wakeup edits before authoring vitals.");
                ConfigureScene(scene); All<SceneBindings>(scene).Single().Validate();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        public static void ConfigureScene(Scene scene)
        {
            if (EditorApplication.isPlaying || scene.path != ScenePath) throw new InvalidOperationException("Expected Wakeup in edit mode.");
            var scope = All<SceneBindings>(scene).Single();
            var player = All<PlayerVitalsController>(scene).Single();
            var canvas = All<Canvas>(scene).Single(c => c.name == "HUDCanvas");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            if (prefab == null) throw new InvalidOperationException("Authored vitals HUD is missing.");
            var legacy = canvas.transform.Find("Vitals");
            if (legacy == null) throw new InvalidOperationException("Existing Wakeup vitals hierarchy is missing.");
            var hud = canvas.transform.Find("PlayerVitalsHud");
            if (hud == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                instance.name = "PlayerVitalsHud"; hud = instance.transform;
            }
            // Retain the original HP/O2 instances and their identities as an inactive legacy group.
            legacy.gameObject.SetActive(false);
            hud.gameObject.SetActive(true);
            var rect = (RectTransform)hud;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(12, 12);
            rect.sizeDelta = new Vector2(560, 100);
            var settings = new SerializedObject(player);
            foreach (var name in new[] { "maxHealth", "currentHealth", "maxHunger", "currentHunger" }) settings.FindProperty(name).floatValue = 100;
            settings.FindProperty("healthSegments").intValue = 4;
            settings.FindProperty("fedThresholdFraction").doubleValue = .2;
            settings.FindProperty("hungerDepletionRate").doubleValue = .1;
            settings.FindProperty("recoveryDelay").doubleValue = 5;
            settings.FindProperty("recoveryRate").doubleValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureComposition(scope, player);
            EditorUtility.SetDirty(player);
        }

        internal static void ConfigureComposition(SceneBindings scope, PlayerVitalsController player)
        {
            var feature = scope.GetComponent<PlayerVitalsComposition>() ?? scope.gameObject.AddComponent<PlayerVitalsComposition>();
            feature.Configure(player);
            var data = new SerializedObject(scope); var features = data.FindProperty("features");
            if (!Enumerable.Range(0, features.arraySize).Any(i => features.GetArrayElementAtIndex(i).objectReferenceValue == feature))
            { int index = features.arraySize++; features.GetArrayElementAtIndex(index).objectReferenceValue = feature; }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature);
        }
        private static T[] All<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
