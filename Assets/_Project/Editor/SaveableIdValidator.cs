#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.SaveLoad.Editor
{
    /// <summary>
    /// SaveableId.OnValidate only fills in an EMPTY id - copy-pasting a
    /// GameObject that already has one keeps the original value, so two
    /// lockers can silently end up sharing one save slot with no error at
    /// runtime (whichever loads/saves last just wins). This scans every
    /// open scene for duplicates so that gets caught at edit time instead
    /// of discovered as "why did locker B's contents vanish".
    /// </summary>
    public static class SaveableIdValidator
    {
        [MenuItem("SOLITUDE/Save/Validate SaveableId Duplicates")]
        public static void Validate()
        {
            var seen = new Dictionary<string, SaveableId>();
            int duplicateCount = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var saveable in root.GetComponentsInChildren<SaveableId>(true))
                    {
                        if (string.IsNullOrEmpty(saveable.Value))
                        {
                            Debug.LogError($"[SaveableIdValidator] Empty id on '{saveable.name}' in scene '{scene.name}'.", saveable);
                            continue;
                        }

                        if (seen.TryGetValue(saveable.Value, out var existing))
                        {
                            Debug.LogError(
                                $"[SaveableIdValidator] Duplicate SaveableId '{saveable.Value}' on " +
                                $"'{saveable.name}' and '{existing.name}' (scene '{scene.name}'). " +
                                "One of these needs a new id - clear the field and it will regenerate on next save.",
                                saveable);
                            duplicateCount++;
                        }
                        else
                        {
                            seen[saveable.Value] = saveable;
                        }
                    }
                }
            }

            if (duplicateCount == 0)
                Debug.Log($"[SaveableIdValidator] Checked {seen.Count} SaveableId components across all open scenes - no duplicates found.");
        }
    }
}
#endif