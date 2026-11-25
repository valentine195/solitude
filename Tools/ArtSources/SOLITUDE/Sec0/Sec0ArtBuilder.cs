using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class Sec0ArtBuilder
{
    const string Root="Assets/_Project/Features/Npcs/Sec0";
    [Serializable] public class Manifest { public Clip[] clips; }
    [Serializable] public class Clip { public string name; public string[] frames; public float fps; public bool loop; public float[] pivot; }
    public static object Main()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before art import.");
        AssetDatabase.Refresh();
        var manifest=Newtonsoft.Json.JsonConvert.DeserializeObject<Manifest>(File.ReadAllText("Tools/ArtSources/SOLITUDE/Sec0/asset_manifest.json"));
        foreach(var file in Directory.GetFiles(Root+"/Sprites","*.png"))
        {
            string path=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType==TextureImporterType.Sprite && importer.spritePixelsPerUnit==32 && importer.filterMode==FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression==TextureImporterCompression.Uncompressed) continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spriteMeshType=SpriteMeshType.FullRect;
            settings.spritePivot=path.Contains("backup_fx")?new Vector2(7f/32,22f/32):path.Contains("stun_fx")?new Vector2(9f/32,.5f):new Vector2(.5f,3f/32);
            settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        Directory.CreateDirectory(Root+"/Animations");AssetDatabase.Refresh();
        foreach(var spec in manifest.clips)
        {
            string path=Root+"/Animations/"+spec.name+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.frameRate=spec.fps;
            var keys=spec.frames.Select((file,i)=>new ObjectReferenceKeyframe{time=i/spec.fps,value=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/"+file)}).ToList();
            keys.Add(new ObjectReferenceKeyframe{time=spec.frames.Length/spec.fps,value=keys[spec.loop?0:keys.Count-1].value});
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys.ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=spec.loop;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
        }
        BuildPrefab("PF_Sec0Visual",manifest.clips.Where(c=>!c.name.StartsWith("Fx_")).ToArray(),"Idle_routine_south");
        BuildPrefab("PF_Sec0BackupFx",manifest.clips.Where(c=>c.name=="Fx_backup").ToArray(),"Fx_backup");
        BuildPrefab("PF_Sec0StunFx",manifest.clips.Where(c=>c.name=="Fx_stun").ToArray(),"Fx_stun");
        AssetDatabase.SaveAssets();
        return new { sprites=Directory.GetFiles(Root+"/Sprites","*.png").Length,clips=manifest.clips.Length,prefabs=3 };
    }
    static void BuildPrefab(string name,Clip[] clips,string defaultName)
    {
        string path=Root+"/Animations/"+name+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm=controller.layers[0].stateMachine;
        foreach(var clip in clips)
        {
            var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==clip.name)??sm.AddState(clip.name);
            state.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/"+clip.name+".anim");
            if(clip.name==defaultName)sm.defaultState=state;
        }
        var go=new GameObject(name);var renderer=go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName="Player";
        if(name.Contains("Fx")) { var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Sec0Signal.mat");if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(material,Root+"/Sec0Signal.mat");}renderer.sharedMaterial=material;}
        renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/"+clips.First(c=>c.name==defaultName).frames[0]);
        go.AddComponent<Animator>().runtimeAnimatorController=controller;
        PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);
    }
    public static object Review()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            string[] states={"routine","alert","hostile"},directions={"south","west","east","north"};
            for(int s=0;s<3;s++)for(int d=0;d<4;d++)
            {
                var go=new GameObject(states[s]+"_"+directions[d]);go.layer=30;
                go.transform.position=new Vector3(-4.5f+s*4+d,0,0);
                go.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/sec0_"+states[s]+"_"+directions[d]+"_0.png");
            }
            var caretaker=new GameObject("ApprovedCaretaker");caretaker.layer=30;caretaker.transform.position=new Vector3(-3,-2,0);
            caretaker.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/SOLITUDE/Characters/solitude_caretaker_south_0_v01.png");
            var bot=new GameObject("ScaleComparison");bot.layer=30;bot.transform.position=new Vector3(-2,-2,0);
            bot.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/sec0_routine_south_0.png");
            var floor=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/SOLITUDE/Tiles/solitude_floor_quiet_normal_v01.png");
            if(floor!=null)for(int x=-6;x<8;x++)for(int y=-3;y<2;y++)
            { var tile=new GameObject("Floor");tile.layer=30;tile.transform.position=new Vector3(x,y,0);var sr=tile.AddComponent<SpriteRenderer>();sr.sprite=floor;sr.sortingOrder=-10; }
            var reviewMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/SOLITUDE/ReferenceScenes/Sec0ReviewUnlit.mat");
            foreach(var sr in scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<SpriteRenderer>()))sr.sharedMaterial=reviewMat;
            var camGo=new GameObject("ArtReviewCamera");camGo.transform.position=new Vector3(1,-.5f,-10);
            var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=3;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.2f);cam.cullingMask=1<<30;
            cam.allowHDR=false;cam.allowMSAA=false;cam.allowDynamicResolution=false;
            var ppc=camGo.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();ppc.assetsPPU=32;ppc.refResolutionX=448;ppc.refResolutionY=192;ppc.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;ppc.cropFrame=PixelPerfectCamera.CropFrame.None;
            var rt=new RenderTexture(896,384,24,RenderTextureFormat.ARGB32);rt.filterMode=FilterMode.Point;rt.antiAliasing=1;rt.Create();
            cam.targetTexture=rt;ppc.enabled=false;cam.rect=new Rect(0,0,1,1);
            var request=new UniversalRenderPipeline.SingleCameraRequest { destination=rt };
            RenderPipeline.SubmitRenderRequest(cam,request);
            var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(896,384,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,896,384),0,0);tex.Apply();RenderTexture.active=old;
            Directory.CreateDirectory("work/Reviews/Sec0");File.WriteAllBytes("work/Reviews/Sec0/unity_scale_review.png",tex.EncodeToPNG());
            cam.targetTexture=null;ppc.enabled=true;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.SaveScene(scene,"Assets/_Project/Art/SOLITUDE/ReferenceScenes/Sec0ArtValidation.unity");
            return new { scene="Assets/_Project/Art/SOLITUDE/ReferenceScenes/Sec0ArtValidation.unity",capture="work/Reviews/Sec0/unity_scale_review.png",pipeline=GraphicsSettings.currentRenderPipeline.GetType().FullName };
        }
        finally { SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true); }
    }

    public static object Capture()
    {
        var scene=SceneManager.GetSceneByPath("Assets/_Project/Art/SOLITUDE/ReferenceScenes/Sec0ArtValidation.unity");
        var cam=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Camera>()).First();
        var ppc=cam.GetComponent<PixelPerfectCamera>();
        ppc.enabled=false;cam.rect=new Rect(0,0,1,1);cam.aspect=896f/384f;cam.orthographicSize=3;cam.ResetProjectionMatrix();
        var rt=new RenderTexture(896,384,24,RenderTextureFormat.ARGB32);rt.filterMode=FilterMode.Point;rt.antiAliasing=1;rt.Create();cam.targetTexture=rt;
        RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
        var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(896,384,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,896,384),0,0);tex.Apply();RenderTexture.active=old;
        File.WriteAllBytes("work/Reviews/Sec0/unity_scale_review.png",tex.EncodeToPNG());
        cam.targetTexture=null;ppc.enabled=true;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
        return "Captured neutral review and restored original scene";
    }
}
