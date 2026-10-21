using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [InitializeOnLoad]
    public static class RoomAuthoringEditLifecycle
    {
        static RoomAuthoringEditLifecycle()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode) return;
                for (var i = 0; i < SceneManager.sceneCount; i++) PrepareForRuntime(SceneManager.GetSceneAt(i));
            };
            EditorSceneManager.sceneSaving += (scene, path) => PrepareForRuntime(scene);
        }

        public static void PrepareForRuntime(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            RoomAuthoringOperations.CancelActiveEdit(scene);
            foreach (var room in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)))
            {
                var mask = room.Occupancy;
                if (mask == null || mask.transform.parent != room.transform) continue;
                if (mask.gameObject.activeSelf) mask.gameObject.SetActive(false);
                if (!mask.CompareTag("EditorOnly")) mask.tag = "EditorOnly";
            }
        }
    }

    public sealed class RoomAuthoringBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => -32000;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            RoomAuthoringEditLifecycle.PrepareForRuntime(scene);
            if (report == null) return;
            StripAuthoringMask(scene);
        }

        public static void StripAuthoringMask(Scene scene)
        {
            foreach (var room in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)))
            {
                var mask = room.Occupancy;
                if (mask != null && mask.transform.parent == room.transform)
                    Object.DestroyImmediate(mask.gameObject);
            }
        }
    }
}
