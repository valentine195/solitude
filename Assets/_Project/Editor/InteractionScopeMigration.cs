using System;
using System.Linq;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class InteractionScopeMigration
    {
        [MenuItem("SOLITUDE/Migrations/Compose Standalone Interaction Scopes")]
        public static void Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before migration.");
            foreach (var path in new[] {
                "Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity",
                "Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Expedition.unity" })
            {
                var previous = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (scene.isDirty) throw new InvalidOperationException("Save the scene before migration: " + path);
                    var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UIController>(true)).Single();
                    var owner = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SceneInteractionBindings>(true)).SingleOrDefault();
                    bool added = owner == null;
                    if (added) owner = ui.gameObject.AddComponent<SceneInteractionBindings>();
                    owner.Validate();
                    if (added) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                }
                finally
                {
                    if (opened) EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }
    }
}
