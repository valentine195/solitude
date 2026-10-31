using System;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Npcs.Sec0;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
namespace SOLITUDE.Editor
{
    public static class Sec0NpcBuilder
    {
        private const string Root="Assets/_Project/Features/Npcs/Sec0";
        public const string ScenePath="Assets/_Project/Scenes/Demos/Sec0Patrol.unity";
        public const string PrefabPath=Root+"/PF_Sec0.prefab";
        private static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("SOLITUDE/NPCs/Build SEC-0 Prefab")]
        public static void BuildPrefab()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring SEC-0.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/Animations/PF_Sec0Visual.controller");
            if(controller==null) throw new InvalidOperationException("SEC-0 art must be imported first.");
            foreach(string direction in new[]{"south","west","east","north"})
            {
                string path=Root+"/Animations/Charge_"+direction+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
                clip.frameRate=8;
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/sec0_hostile_"+direction+"_0.png");
                AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},new[]{new ObjectReferenceKeyframe{time=0,value=sprite},new ObjectReferenceKeyframe{time=.25f,value=sprite}});
                foreach(var channel in new[]{"r","g","b"})
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(SpriteRenderer),"m_Color."+channel),AnimationCurve.Linear(0,1,.125f,channel=="r"?1:.45f));
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                var sm=controller.layers[0].stateMachine;
                var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Charge_"+direction)??sm.AddState("Charge_"+direction);state.motion=clip;
                EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(controller);
            BuildPrefabAtPath(PrefabPath);
            AssetDatabase.SaveAssets();
        }
        /// <summary>Save the default unit using prepared art; accepts a disposable destination for authoring validation.</summary>
        public static void BuildPrefabAtPath(string outputPath)
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring SEC-0.");
            var host=new GameObject("PF_Sec0");
            try
            {
                var rover=new GameObject("SEC-0");rover.transform.SetParent(host.transform,false);
                var body=rover.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.useFullKinematicContacts=true;
                var collider=rover.AddComponent<BoxCollider2D>();collider.size=new Vector2(.6f,.34f);collider.offset=new Vector2(0,.17f);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PF_Sec0Visual.prefab"));visual.transform.SetParent(rover.transform,false);
                var path=new GameObject("PatrolRoute");path.transform.SetParent(host.transform,false);
                var positions=new[]{Vector2.zero,new Vector2(4,0),new Vector2(4,3),new Vector2(0,3)};
                var points=positions.Select((p,i)=>{var g=new GameObject("Waypoint"+i);g.transform.SetParent(path.transform,false);g.transform.localPosition=p;return g.transform;}).ToArray();
                rover.AddComponent<Sec0Controller>().Configure(visual.GetComponent<Animator>(),AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/PF_Sec0StunFx.prefab"),points);
                rover.AddComponent<Sec0DamageRecipient>();
                rover.AddComponent<Sec0HitFeedback>();
                PrefabUtility.SaveAsPrefabAsset(host,outputPath);
            }
            finally {UnityEngine.Object.DestroyImmediate(host);}
        }
        [MenuItem("SOLITUDE/NPCs/Build SEC-0 Patrol Demo")]
        public static void BuildDemo()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring SEC-0.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null) throw new InvalidOperationException("SEC-0 demo already exists; refusing to overwrite.");
            BuildPrefab();
            if(!AssetDatabase.CopyAsset("Assets/_Project/Scenes/Wakeup.unity",ScenePath)) throw new InvalidOperationException("Cannot copy player scene.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                new GameObject("Disposable SEC-0 demo session").AddComponent<RestorationDemoSession>();
                var player=All<PlayerMovement>(scene).Single();player.transform.position=new Vector3(50,-2,0);
                foreach(var point in All<SurvivorAwakeningPoint>(scene))point.transform.position=player.transform.position;
                var stun=player.gameObject.AddComponent<PlayerStunReceiver>();
                var host=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);host.name="SEC-0 Patrol Unit";host.transform.position=new Vector3(52,1,0);
                var bot=host.GetComponentInChildren<Sec0Controller>();
                var feature=new GameObject("SEC-0 Encounter").AddComponent<Sec0Composition>();feature.Configure(new[]{bot},stun,player.GetComponent<PlayerVitalsController>());
                var bindings=All<SceneBindings>(scene).Single();var serialized=new SerializedObject(bindings);var features=serialized.FindProperty("features");int at=features.arraySize;features.arraySize++;features.GetArrayElementAtIndex(at).objectReferenceValue=feature;serialized.ApplyModifiedPropertiesWithoutUndo();
                Arena(scene);
                var camera=All<Camera>(scene).Single(c=>c.CompareTag("MainCamera"));
                foreach(var behaviour in camera.GetComponents<MonoBehaviour>())if(behaviour.GetType().Namespace?.StartsWith("Unity.Cinemachine")==true)behaviour.enabled=false;
                camera.gameObject.AddComponent<SolitudeCameraFollow2D>().Configure(player.transform,new Vector2(54,54),new Vector2(.5f,.5f));camera.transform.position=new Vector3(54,.5f,-10);
                camera.orthographic=true;camera.orthographicSize=4.5f;camera.allowMSAA=false;
                var pixel=camera.GetComponent<PixelPerfectCamera>();if(pixel!=null){pixel.assetsPPU=32;pixel.refResolutionX=448;pixel.refResolutionY=288;pixel.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;pixel.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;}
                foreach(var existing in All<Light2D>(scene)) if(existing.lightType==Light2D.LightType.Global) existing.enabled=false;
                var light=new GameObject("SEC-0 Neutral Lighting").AddComponent<Light2D>();light.lightType=Light2D.LightType.Global;light.intensity=1;
                var lighting=new SerializedObject(light);var layers=lighting.FindProperty("m_ApplyToSortingLayers");layers.arraySize=SortingLayer.layers.Length;for(int i=0;i<layers.arraySize;i++)layers.GetArrayElementAtIndex(i).intValue=SortingLayer.layers[i].id;lighting.ApplyModifiedPropertiesWithoutUndo();
                bindings.Validate();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            Debug.Log("SEC-0 demo ready: "+ScenePath);
        }
        // Capture the actual composed demo during isolated PlayMode verification.
        public static void CapturePreview(string output)
        {
            var scene=SceneManager.GetSceneByPath(ScenePath);var camera=All<Camera>(scene).Single(c=>c.CompareTag("MainCamera"));
            var pixel=camera.GetComponent<PixelPerfectCamera>();bool pixelEnabled=pixel!=null && pixel.enabled;
            var oldTarget=camera.targetTexture;var oldRect=camera.rect;var oldSize=camera.orthographicSize;var oldAspect=camera.aspect;var oldProjection=camera.projectionMatrix;
            var rt=new RenderTexture(896,576,24,RenderTextureFormat.ARGB32);rt.filterMode=FilterMode.Point;rt.antiAliasing=1;rt.Create();
            var oldActive=RenderTexture.active;Texture2D image=null;
            try
            {
                if(pixel!=null)pixel.enabled=false;camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);camera.orthographicSize=4.5f;camera.aspect=896f/576;camera.ResetProjectionMatrix();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;image=new Texture2D(896,576,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,896,576),0,0);image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllBytes(output,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=oldActive;camera.targetTexture=oldTarget;camera.rect=oldRect;camera.orthographicSize=oldSize;camera.aspect=oldAspect;camera.projectionMatrix=oldProjection;if(pixel!=null)pixel.enabled=pixelEnabled;
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        private static void Arena(Scene scene)
        {
            var grid=new GameObject("SEC-0 Training Floor").AddComponent<Grid>();
            var floorGO=new GameObject("ArenaFloor");floorGO.transform.SetParent(grid.transform,false);var map=floorGO.AddComponent<Tilemap>();floorGO.AddComponent<TilemapRenderer>().sortingOrder=-20;
            var tile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Project/Art/SOLITUDE/Tiles/solitude_floor_quiet_normal_v01.asset");
            if(tile==null)throw new InvalidOperationException("Approved floor tile is missing.");
            for(int x=48;x<62;x++)for(int y=-4;y<6;y++)map.SetTile(new Vector3Int(x,y),tile);
            // Solid boundaries and two sight-breaking cover panels, outside the route.
            Barrier("NorthBarrier",new Vector2(55,5.8f),new Vector2(14,.3f));
            Barrier("SouthBarrier",new Vector2(55,-4.2f),new Vector2(14,.3f));
            Barrier("WestBarrier",new Vector2(47.8f,.8f),new Vector2(.3f,10));
            Barrier("EastBarrier",new Vector2(62.2f,.8f),new Vector2(.3f,10));
            Barrier("SightCoverA",new Vector2(53,-1),new Vector2(1,1));
            Barrier("SightCoverB",new Vector2(58,2),new Vector2(1,1));
        }
        private static void Barrier(string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name);go.transform.position=position;go.AddComponent<BoxCollider2D>().size=size;
            var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);visual.transform.localPosition=new Vector3(0,-size.y*.5f,0);
            var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/SOLITUDE/Props/solitude_locker_closed_normal_v01.png");
            sr.drawMode=SpriteDrawMode.Tiled;sr.size=size;sr.sortingLayerName="Player";sr.sortingOrder=-1;
        }
    }
}
