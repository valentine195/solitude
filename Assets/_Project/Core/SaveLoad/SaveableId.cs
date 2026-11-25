using UnityEngine;

namespace SOLITUDE.SaveLoad
{
    /// <summary>
    /// Stable per-instance identity for anything that needs its own save
    /// slot - a specific locker in a specific room, not "lockers in general".
    /// Generated once in the editor and never regenerated afterward, so a
    /// GameObject keeps the same id across every future load, capacity
    /// change, or content edit.
    ///
    /// This does NOT protect against copy-pasting an existing instance -
    /// Explicit assignment only fills an empty id, so a paste keeps the original's
    /// value (Unity doesn't distinguish "duplicated" from "still the same
    /// object" at this level). Two instances silently sharing one save slot
    /// is a real, easy-to-miss failure mode - see the editor validator this
    /// is meant to be paired with (scans the scene for duplicate Values and
    /// flags them) before relying on this at scale.
    /// </summary>
    public class SaveableId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Value => id;

        public void AssignRuntimeIdentity(string value)
        {
            if (!string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(value))
                throw new System.InvalidOperationException("Assign a nonempty spawn identity before binding an unidentified instance.");
            id = value;
        }

#if UNITY_EDITOR
        [ContextMenu("Assign Missing Scene Identity")]
        public void AssignMissingSceneIdentity()
        {
            if (UnityEngine.Application.isPlaying || !string.IsNullOrEmpty(id) ||
                UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) ||
                UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
            id = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this)) UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
