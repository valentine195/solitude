using System;
using System.Collections.Generic;
using SOLITUDE.Items;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SOLITUDE.Editor
{
    public sealed class PickupIdentityValidator
    {
        public int callbackOrder => 0;
        

        [MenuItem("SOLITUDE/Validation/Validate World Pickups")]
        public static void ValidateForCI() => WakeupValidation.ValidateForCI();
        public static void AuditAllAssets()
        {
            var ids = new Dictionary<string, string>(StringComparer.Ordinal);
            var errors = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var pickup in root.GetComponentsInChildren<WorldPickup>(true))
                        {
                            string location = $"{path}: {pickup.name}";
                            ValidateDefinition(pickup, location, errors);
                            if (string.IsNullOrWhiteSpace(pickup.PickupId)) errors.Add($"Missing pickup identity at {location}.");
                            else if (ids.TryGetValue(pickup.PickupId, out var previous))
                                errors.Add($"Duplicate pickup '{pickup.PickupId}' at {previous} and {location}.");
                            else ids.Add(pickup.PickupId, location);
                        }
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var pickup in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<WorldPickup>(true))
                {
                    ValidateDefinition(pickup, path, errors);
                    if (!string.IsNullOrEmpty(pickup.PickupId)) errors.Add($"Prefab {path} carries a shared pickup identity.");
                }
            }
            if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
            Debug.Log($"[PickupIdentityValidator] Validated {ids.Count} world pickup identities across project scenes.");
        }

        private static void ValidateDefinition(WorldPickup pickup, string location, List<string> errors)
        {
            var definition = pickup.Definition;
            if (definition?.Item == null || string.IsNullOrWhiteSpace(definition.Item.ItemId) || definition.Quantity < 1)
            {
                errors.Add($"Invalid pickup definition at {location}.");
                return;
            }
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Database.asset");
            if (database == null || database.Resolve(definition.Item.ItemId) != definition.Item)
                errors.Add($"Pickup item at {location} is not registered in Resources/Database.asset.");
        }
    }
}
