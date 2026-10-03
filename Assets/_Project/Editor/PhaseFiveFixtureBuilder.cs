using SOLITUDE.Composition;
using SOLITUDE.Features.Interactables;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SOLITUDE.Editor
{
    public static class PhaseFiveFixtureBuilder
    {
        public const string Context="Assets/_Project/Tests/Fixtures/Context.unity", Unrelated="Assets/_Project/Tests/Fixtures/Unrelated.unity";
        public static void Prepare()
        {
            System.IO.Directory.CreateDirectory("Assets/_Project/Tests/Fixtures");AssetDatabase.Refresh();
            foreach(var path in new[]{Context,Unrelated})
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var scope=new GameObject("ContextScope").AddComponent<SceneBindings>();
                if(path==Context)
                {
                    var locker=new GameObject("ContextLocker").AddComponent<LockerContainer>();var id=new SerializedObject(locker.GetComponent<SaveableId>());id.FindProperty("id").stringValue="verification.context.locker";id.ApplyModifiedPropertiesWithoutUndo();
                    var authoring=new SerializedObject(scope);var list=authoring.FindProperty("lockers");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=locker;authoring.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.SaveScene(scene,path);
                var findings=WakeupValidation.ValidateScene(scene,false);
                if(findings.Count!=0)throw new System.InvalidOperationException("Invalid context fixture: "+findings[0].code+" "+findings[0].message);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Wakeup.unity",true),new EditorBuildSettingsScene(Context,true),new EditorBuildSettingsScene(Unrelated,true)};
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);AssetDatabase.SaveAssets();
        }
        public static void RestoreBuildScope()=>EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Wakeup.unity",true)};
    }
}
