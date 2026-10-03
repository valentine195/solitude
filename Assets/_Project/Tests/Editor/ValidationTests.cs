using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    }
}
