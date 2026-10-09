using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace SOLITUDE.Tests
{
    public class ValidationTests
    {
        [Test] public void MissingCompositionCannotPassAsDirectEntry()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            try{Assert.IsTrue(WakeupValidation.ValidateScene(scene,true).Any(f=>f.code=="SCENE_COMPOSITION"));}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void InvalidEndpointHasStableCodeAndField()
        {
            var scene=EditorSceneManager.NewPreviewScene();var one=new GameObject("Scope").AddComponent<SceneBindings>();var two=new GameObject("DuplicateScope").AddComponent<SceneBindings>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(one.gameObject,scene);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(two.gameObject,scene);
            try{var finding=WakeupValidation.ValidateScene(scene,true).Single(f=>f.code=="SCENE_ENDPOINTS");Assert.AreEqual("SceneBindings",finding.field);StringAssert.Contains("scope",finding.message.ToLower());}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void ProjectValidationIsDeterministicAndDoesNotChangeSceneDirtiness()
        {
            bool dirty=EditorSceneManager.GetActiveScene().isDirty;var a=WakeupValidation.Collect();var b=WakeupValidation.Collect();
            CollectionAssert.AreEqual(a.findings.Select(f=>f.code+f.path+f.field+f.message),b.findings.Select(f=>f.code+f.path+f.field+f.message));Assert.AreEqual(dirty,EditorSceneManager.GetActiveScene().isDirty);
        }
        [TestCase("Assets/_Project/Scenes/Wakeup.unity")]
        [TestCase("Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity")]
        [TestCase("Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity")]
        [TestCase("Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity")]
        [TestCase("Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity")]
        [TestCase("Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Expedition.unity")]
        public void AuthoredSceneKeepsValidCompositionAndScriptReferences(string path)
        {
            var previous = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                bool dirty = scene.isDirty;
                var validate = typeof(CompositionValidator).GetMethod("ValidateScene", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { scene }), path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                            path + ": missing script on " + transform.name);
                Assert.AreEqual(dirty, scene.isDirty, "Validation must not rewrite authoring.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
