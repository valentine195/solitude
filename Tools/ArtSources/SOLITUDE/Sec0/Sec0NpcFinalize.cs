using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class Sec0NpcFinalize
{
    public static object Main()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before authoring demo.");
        var original=SceneManager.GetActiveScene();
        var demo=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Demos/Sec0Patrol.unity",OpenSceneMode.Additive);
        int changed=0;
        try
        {
            SceneManager.SetActiveScene(demo);
            foreach(var go in demo.GetRootGameObjects().Where(g=>g.name.EndsWith("Barrier") || g.name.StartsWith("SightCover")))
            {
                var sr=go.GetComponent<SpriteRenderer>();if(sr==null)continue;
                var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);visual.transform.localPosition=new Vector3(0,-go.GetComponent<BoxCollider2D>().size.y*.5f,0);
                UnityEditorInternal.ComponentUtility.CopyComponent(sr);UnityEditorInternal.ComponentUtility.PasteComponentAsNew(visual);UnityEngine.Object.DestroyImmediate(sr);changed++;
            }
            foreach(var camera in demo.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))camera.rect=new Rect(0,0,1,1);
            EditorSceneManager.SaveScene(demo);
        }
        finally {SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(demo,true);}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab");Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
        return new {alignedCover=changed,originalScene=original.path,originalDirty=original.isDirty,prefabSelected=prefab!=null};
    }
}
