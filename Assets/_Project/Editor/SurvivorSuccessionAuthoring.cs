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
        public static void AuthorWakeup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author succession outside Play Mode.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save or resolve existing Wakeup edits before authoring succession.");
                var scope = All<SceneBindings>(scene).Single();
                var player = All<PlayerInteractor>(scene).Single();
                var point = All<SurvivorAwakeningPoint>(scene).SingleOrDefault();
                if (point == null)
                {
                    var go = new GameObject("CryobayAwakeningPoint"); SceneManager.MoveGameObjectToScene(go, scene);
                    // Preserve the original authored cryobay player placement as the awakening point.
                    go.transform.position = player.transform.position;
                    point = go.AddComponent<SurvivorAwakeningPoint>();
                }
                var adapter = scope.GetComponent<SurvivorAvatarAdapter>() ?? scope.gameObject.AddComponent<SurvivorAvatarAdapter>();
                adapter.Configure(player);
                var feature = scope.GetComponent<SurvivorSuccessionComposition>() ?? scope.gameObject.AddComponent<SurvivorSuccessionComposition>();
                feature.Configure(point, adapter);
                var data = new SerializedObject(scope); var list = data.FindProperty("features");
                if (!Enumerable.Range(0, list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == feature))
                { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature; }
                data.ApplyModifiedPropertiesWithoutUndo();
                scope.Validate(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    }
}
