using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class WorldPickupMigration
    {
        [MenuItem("SOLITUDE/Items/Migrate Legacy Pickups")]
        public static void Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Migration requires Edit Mode.");
            // Retain each prefab GUID, transform, renderer, collider and interaction prompt.
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!LegacyPickups(asset).Any()) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ReplaceLegacy(root);
                    foreach (var pickup in root.GetComponentsInChildren<WorldPickup>(true)) SetId(pickup, "");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            // Process existing scenes in place, and other scenes additively, preserving the user's setup.
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool changed = false;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        changed |= ReplaceLegacy(root);
                        foreach (var pickup in root.GetComponentsInChildren<WorldPickup>(true))
                        {
                            // Explicitly assign and persist scene-instance identities.
                            SetId(pickup, string.IsNullOrWhiteSpace(pickup.PickupId)
                                ? Guid.NewGuid().ToString("N") : pickup.PickupId);
                            changed = true;
                        }
                    }
                    if (changed) EditorSceneManager.SaveScene(scene);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
            PickupIdentityValidator.ValidateForCI();
            Debug.Log("[WorldPickupMigration] Legacy pickups migrated; world identities validated.");
        }

        private static bool ReplaceLegacy(GameObject root)
        {
            bool changed = false;
            foreach (var legacy in LegacyPickups(root))
            {
                var old = new SerializedObject(legacy);
                var item = old.FindProperty("itemDefinition").objectReferenceValue as ItemDefinition;
                if (item == null)
                {
                    string name = legacy.GetType().Name;
                    if (name != null) item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                        $"Assets/_Project/Features/Items/Definitions/{name}/{name}.asset");
                }
                if (item == null) throw new InvalidOperationException($"Cannot resolve reward for '{legacy.name}'.");
                string definitionPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(item), null) + "Pickup.asset";
                var definition = AssetDatabase.LoadAssetAtPath<PickupDefinition>(definitionPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<PickupDefinition>();
                    var config = new SerializedObject(definition);
                    config.FindProperty("item").objectReferenceValue = item;
                    config.FindProperty("quantity").intValue = 1;
                    config.FindProperty("successText").stringValue = legacy.GetType().Name == "Battery"
                        ? "Picked up battery" : "Picked up O2 Canister";
                    config.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }
                var pickup = legacy.gameObject.GetComponent<WorldPickup>() ?? legacy.gameObject.AddComponent<WorldPickup>();
                var view = new SerializedObject(pickup);
                view.FindProperty("definition").objectReferenceValue = definition;
                view.FindProperty("prompt").stringValue = old.FindProperty("prompt").stringValue;
                view.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(legacy, true);
                changed = true;
            }
            return changed;
        }

        private static IEnumerable<MonoBehaviour> LegacyPickups(GameObject root) =>
            root.GetComponentsInChildren<MonoBehaviour>(true).Where(component => component != null &&
                (component.GetType().FullName == "SOLITUDE.Items.Battery" ||
                 component.GetType().FullName == "SOLITUDE.Items.O2Canister"));

        private static void SetId(WorldPickup pickup, string id)
        {
            var identity = pickup.GetComponent<SaveableId>();
            var serialized = new SerializedObject(identity);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(identity))
                PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
            EditorUtility.SetDirty(identity);
        }
    }
}
