using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Editor
{
    public static class SurvivorSuccessionAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Wakeup.unity";
        public const string RecoveryPrefabPath = "Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab";
        [MenuItem("SOLITUDE/Survivors/Author Recovery Prefab")]
        public static void AuthorRecoveryPrefab()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author recovery outside Play Mode.");
            var go = new GameObject("SurvivorRecoveryContainer"); go.SetActive(false);
            try
            {
                var owner = go.AddComponent<SurvivorRecoveryContainer>();
                var visual = new GameObject("EquipmentCache"); visual.transform.SetParent(go.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/SOLITUDE/Props/solitude_locker_closed_normal_v01.png");
                renderer.sortingOrder = 5;
                var trigger = new GameObject("Proximity"); trigger.transform.SetParent(go.transform, false);
                var collider = trigger.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = 1.25f;
                var sensor = trigger.AddComponent<SOLITUDE.Core.Interaction.InteractableProximitySensor>();
                owner.Configure(sensor, renderer); owner.ValidateAuthoring();
                PrefabUtility.SaveAsPrefabAsset(go, RecoveryPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [MenuItem("SOLITUDE/Survivors/Author Wakeup Succession")]
        public static void AuthorWakeup() => AuthorScene(ScenePath, "CryobayAwakeningPoint");
        [MenuItem("SOLITUDE/Survivors/Author Scrubber Test Succession")]
        public static void AuthorScrubberDemo() => AuthorScene(ScrubberEncounterBuilder.ScenePath, "ScrubberTestAwakeningPoint");
        private static void AuthorScene(string path, string pointName)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author succession outside Play Mode.");
            var scene = SceneManager.GetSceneByPath(path);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save or resolve existing scene edits before authoring succession.");
                ConfigureScene(scene, pointName);
                All<SceneBindings>(scene).Single().Validate();
                SaveConfiguredScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        internal static void SaveConfiguredScene(Scene scene)
        {
            // These authored scenes use a full viewport. Pixel-perfect preview may
            // temporarily letterbox Camera.rect for the current Editor resolution.
            if (scene.path != ScenePath && scene.path != ScrubberEncounterBuilder.ScenePath)
                throw new InvalidOperationException("Unexpected succession authoring scene.");
            foreach (var camera in All<Camera>(scene)) camera.rect = new Rect(0, 0, 1, 1);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        internal static void ConfigureScene(Scene scene, string pointName)
        {
            var scope = All<SceneBindings>(scene).Single();
            var player = All<PlayerInteractor>(scene).Single();
            var point = All<SurvivorAwakeningPoint>(scene).SingleOrDefault();
            if (point == null)
            {
                var go = new GameObject(pointName); SceneManager.MoveGameObjectToScene(go, scene);
                // Use the scene's original authored player placement, including demo test locations.
                go.transform.position = player.transform.position;
                point = go.AddComponent<SurvivorAwakeningPoint>();
            }
            var adapter = scope.GetComponent<SurvivorAvatarAdapter>() ?? scope.gameObject.AddComponent<SurvivorAvatarAdapter>();
            adapter.Configure(player);
            var feature = scope.GetComponent<SurvivorSuccessionComposition>() ?? scope.gameObject.AddComponent<SurvivorSuccessionComposition>();
            feature.Configure(point, adapter);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RecoveryPrefabPath);
            if (prefab == null) throw new InvalidOperationException("Survivor recovery prefab is missing.");
            feature.ConfigureRecovery(prefab.GetComponent<SurvivorRecoveryContainer>());
            var data = new SerializedObject(scope); var list = data.FindProperty("features");
            if (!Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == feature))
            { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature; }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feature); EditorUtility.SetDirty(adapter);
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
