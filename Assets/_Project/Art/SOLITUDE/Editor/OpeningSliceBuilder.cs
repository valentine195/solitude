using System;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Core.Interaction;
using SOLITUDE.World.SolitudeStart;
using SOLITUDE.World.OpeningSlice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
namespace SOLITUDE.Art.Editor
{
    public static class OpeningSliceBuilder
    {
        public const string ScenePath=OpeningArtImport.Root+"/ReferenceScenes/SOLITUDE_OpeningSlice.unity";
        const string Tiles="Assets/_Project/Features/World/SolitudeStart/Generated/Tile_";
        static Tilemap floor,wall,cap;static Transform geometry;
        static T[] All<T>() where T:Component => UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        [MenuItem("SOLITUDE/Art/Build Opening Review Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            var original=EditorSceneManager.GetActiveScene();
            if(original.isDirty)throw new InvalidOperationException("Save current scene edits before opening review scene.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)throw new InvalidOperationException("Review scene exists; edit it without overwriting.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));AssetDatabase.Refresh();
            if(!AssetDatabase.CopyAsset("Assets/Scenes/Wakeup.unity",ScenePath))throw new InvalidOperationException("Could not copy composed Wakeup.");
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            foreach(string name in new[]{"Grid","Cryopod","PF_Door","Camera"}){var go=GameObject.Find(name);if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
            var oxy=GameObject.Find("PF_O2Canister");if(oxy!=null)oxy.SetActive(false);
            geometry=new GameObject("Opening Ship Structure").transform;
            var grid=new GameObject("Opening Grid",typeof(Grid));grid.transform.SetParent(geometry);
            floor=Map(grid.transform,"Floor",0);wall=Map(grid.transform,"North Wall Faces",20);cap=Map(grid.transform,"Boundary Caps",2000);
            // Two rooms joined by a short service corridor. Door opens north into an AI alcove.
            Fill(1,1,10,7);Fill(11,3,17,4);Fill(18,1,27,7);Fill(20,10,25,13);Fill(22,8,23,9);
            North(1,10,8);North(18,21,8);North(24,27,8);North(20,25,14);
            for(int y=1;y<=9;y++){Cap(0,y,"west");if(y<3||y>4)Cap(11,y,"east");Cap(28,y,"east");if(y<3||y>4)Cap(17,y,"west");}
            for(int x=1;x<=10;x++)Cap(x,0,"south");for(int x=18;x<=27;x++)Cap(x,0,"south");
            for(int x=11;x<=17;x++){Cap(x,2,"south");wall.SetTile(new Vector3Int(x,5),Tile("plain_bulkhead_0_1"));wall.SetTile(new Vector3Int(x,6),Tile("plain_bulkhead_0_0"));}
            for(int y=10;y<=15;y++){Cap(19,y,"west");Cap(26,y,"east");}
            for(int x=20;x<=25;x++)if(x<22||x>23)Cap(x,9,"south");
            Cap(0,0,"southwest");Cap(11,0,"southeast");Cap(0,9,"northwest");Cap(11,9,"northeast");
            Cap(17,0,"southwest");Cap(28,0,"southeast");Cap(17,9,"northwest");Cap(28,9,"northeast");
            Cap(19,9,"southwest");Cap(26,9,"southeast");Cap(19,15,"northwest");Cap(26,15,"northeast");
            Box("Cryobay west",new Vector2(.875f,4.5f),new Vector2(.25f,8));Box("Cryobay south",new Vector2(6,.875f),new Vector2(10.5f,.25f));
            Box("Cryobay north",new Vector2(6,8.2f),new Vector2(10.5f,.4f));Box("Cryobay east lower",new Vector2(11.125f,2),new Vector2(.25f,2));Box("Cryobay east upper",new Vector2(11.125f,6.5f),new Vector2(.25f,3));
            Box("Corridor south",new Vector2(14.5f,2.875f),new Vector2(7,.25f));Box("Corridor north",new Vector2(14.5f,5.125f),new Vector2(7,.25f));
            Box("Service west lower",new Vector2(17.875f,2),new Vector2(.25f,2));Box("Service west upper",new Vector2(17.875f,6.5f),new Vector2(.25f,3));
            Box("Service east",new Vector2(28.125f,4.5f),new Vector2(.25f,8));Box("Service south",new Vector2(23,.875f),new Vector2(10.5f,.25f));
            Box("Service north left",new Vector2(20,8.2f),new Vector2(4,.4f));Box("Service north right",new Vector2(26,8.2f),new Vector2(4,.4f));
            Box("Passage west",new Vector2(21.875f,9),new Vector2(.25f,2));Box("Passage east",new Vector2(24.125f,9),new Vector2(.25f,2));
            Box("AI west",new Vector2(19.875f,12),new Vector2(.25f,4));Box("AI east",new Vector2(26.125f,12),new Vector2(.25f,4));Box("AI north",new Vector2(23,14.2f),new Vector2(6.5f,.4f));
            Box("AI south left",new Vector2(21,9.875f),new Vector2(2,.25f));Box("AI south right",new Vector2(25,9.875f),new Vector2(2,.25f));
            Prop("Cryopod 01","cryopod",3,5.5f,true);Prop("Cryopod 02","cryopod",5,5.5f,true);Prop("Personal shelf","personal_shelf",7.5f,6.5f,true);Prop("Cryobay clipboard","clipboard",9.5f,8.2f,false);
            var player=All<PlayerInteractor>().Single().gameObject;player.transform.position=new Vector3(4,3,0);player.transform.localScale=Vector3.one;
            var ps=player.GetComponent<SpriteRenderer>();ps.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(OpeningArtImport.Root+"/Characters/solitude_caretaker_south_0_v01.png");ps.sharedMaterial=new Material(Shader.Find("Sprites/Default"));ps.sortingLayerID=0;ps.color=Color.white;
            // Persist a shared material once; never leave a transient material reference in a saved scene.
            string matPath=OpeningArtImport.Root+"/Characters/OpeningSprites.mat";AssetDatabase.CreateAsset(ps.sharedMaterial,matPath);
            player.GetComponent<Animator>().runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(OpeningArtImport.Root+"/Characters/OpeningCaretaker.controller");
            var foot=player.GetComponent<BoxCollider2D>();foot.size=new Vector2(.5f,.25f);foot.offset=new Vector2(0,.125f);player.AddComponent<OpeningDepthSort>();
            var locker=All<Locker>().Single();locker.transform.position=new Vector3(20,6.5f,0);locker.transform.localScale=Vector3.one;
            var ls=locker.GetComponentInChildren<SpriteRenderer>();ls.transform.localPosition=Vector3.zero;ls.transform.localScale=Vector3.one;ls.sprite=OpeningArtImport.Sprite("locker_closed");ls.sortingLayerID=0;ls.sortingOrder=792;ls.color=Color.white;
            foreach(var box in locker.GetComponentsInChildren<BoxCollider2D>()){box.offset=Vector2.zero;box.size=box.isTrigger?new Vector2(2.5f,2.5f):new Vector2(.8f,.4f);}
            var battery=All<WorldPickup>().Single(p=>p.Definition.Item.ItemId=="c756a156ba7f34cb2b65e32264413a60");battery.transform.position=new Vector3(21.5f,6,0);battery.transform.localScale=Vector3.one;
            var bs=battery.GetComponent<SpriteRenderer>();bs.sprite=OpeningArtImport.Sprite("battery");bs.sortingLayerID=0;bs.sortingOrder=808;bs.color=Color.white;
            foreach(var box in battery.GetComponentsInChildren<BoxCollider2D>()){box.offset=new Vector2(0,.3f);box.size=new Vector2(1.4f,1.4f);box.isTrigger=true;}
            var doorGO=new GameObject("Opening Powered Door");doorGO.transform.position=new Vector3(23,8,0);
            var ds=doorGO.AddComponent<SpriteRenderer>();ds.sortingOrder=744;var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Features/World/SolitudeStart/Art/door-opening-spritesheet.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();ds.sprite=frames[0];
            var blocker=doorGO.AddComponent<BoxCollider2D>();blocker.offset=new Vector2(0,.55f);blocker.size=new Vector2(1.9f,1.1f);
            var door=doorGO.AddComponent<SolitudeSlidingDoor>();door.Configure(ds,frames,blocker);
            var socketGO=Prop("Auxiliary power socket","intercom",24.75f,8.25f,false);var socket=socketGO.AddComponent<OpeningPowerSocket>();socket.door=door;socket.batteryItemId=battery.Definition.Item.ItemId;
            Sensor(socketGO,new Vector2(0,-.75f),new Vector2(2,2.5f));
            Prop("AI intercom","intercom",23,14.2f,false);
            var contact=new GameObject("First fragmented AI contact");contact.transform.position=new Vector3(23,10.6f,0);var trigger=contact.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(2,1);contact.AddComponent<OpeningFirstContact>().socket=socket;
            var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGO.tag="MainCamera";cameraGO.transform.position=new Vector3(9,5.625f,-10);
            var cam=cameraGO.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=5.625f;cam.backgroundColor=new Color32(24,43,53,255);cam.clearFlags=CameraClearFlags.SolidColor;cam.allowHDR=false;cam.allowMSAA=false;cam.allowDynamicResolution=false;
            var extra=cameraGO.AddComponent<UniversalAdditionalCameraData>();extra.antialiasing=AntialiasingMode.None;extra.renderPostProcessing=false;
            var pixel=cameraGO.AddComponent<PixelPerfectCamera>();pixel.assetsPPU=32;pixel.refResolutionX=640;pixel.refResolutionY=360;pixel.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;pixel.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;
            cameraGO.AddComponent<SolitudeCameraFollow2D>().Configure(player.transform,new Vector2(9,20),new Vector2(5.625f,10.625f));
            foreach(var light in All<Light2D>())if(light.lightType==Light2D.LightType.Global){light.intensity=1;light.color=Color.white;}
            var review=new GameObject("Opening Art Review Session").AddComponent<OpeningReviewSession>();review.persistence=All<SaveGameService>().Single();review.database=AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/Database.asset");
            var feedback=All<SOLITUDE.Core.UI.InteractionFeedbackView>().Single();SetFloat(feedback,"duration",3f);
            RefineCurrent();
            All<SceneBindings>().Single().Validate();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("SOLITUDE opening review built from composed Wakeup, with isolated in-memory save.");
        }
        [MenuItem("SOLITUDE/Art/Apply Opening Review Refinements")]
        public static void RefineCurrent()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open review scene in Edit mode.");
            var quiet=AssetDatabase.LoadAssetAtPath<TileBase>(OpeningArtImport.Root+"/Tiles/solitude_floor_quiet_normal_v01.asset");
            if(quiet!=null)All<Tilemap>().Single(t=>t.name=="Floor").SwapTile(Tile("floor_composite"),quiet);
            var locker=All<Locker>().Single();var visual=locker.GetComponent<OpeningLockerVisual>()??locker.gameObject.AddComponent<OpeningLockerVisual>();
            visual.visual=locker.GetComponentInChildren<SpriteRenderer>();visual.closedSprite=OpeningArtImport.Sprite("locker_closed");visual.openSprite=OpeningArtImport.Sprite("locker_open");visual.modal=All<SOLITUDE.Containers.ContainerTransferModalView>().Single();
            foreach(var fill in All<UnityEngine.UI.Image>().Where(i=>i.name=="Fill" && i.transform.parent.name=="Bar" && i.color.r>.8f && i.color.g<.3f))
                fill.color=new Color32(130,153,154,255);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        static void SetFloat(UnityEngine.Object target,string key,float value){var s=new SerializedObject(target);s.FindProperty(key).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static TileBase Tile(string name)=>AssetDatabase.LoadAssetAtPath<TileBase>(Tiles+name+".asset") ?? throw new InvalidOperationException("Missing tile "+name);
        static Tilemap Map(Transform parent,string name,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();}
        static void Fill(int x0,int y0,int x1,int y1){for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)floor.SetTile(new Vector3Int(x,y),Tile("floor_composite"));}
        static void North(int x0,int x1,int y){for(int x=x0;x<=x1;x++){wall.SetTile(new Vector3Int(x,y),Tile("plain_bulkhead_0_1"));wall.SetTile(new Vector3Int(x,y+1),Tile("plain_bulkhead_0_0"));}}
        static void Cap(int x,int y,string type)=>cap.SetTile(new Vector3Int(x,y),Tile(type));
        static void Box(string name,Vector2 position,Vector2 size){var g=new GameObject(name,typeof(BoxCollider2D));g.transform.SetParent(geometry);g.transform.position=position;g.GetComponent<BoxCollider2D>().size=size;}
        static GameObject Prop(string name,string sprite,float x,float y,bool collision){var go=new GameObject(name,typeof(SpriteRenderer));go.transform.position=new Vector3(x,y,0);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=OpeningArtImport.Sprite(sprite);sr.sortingOrder=1000-Mathf.RoundToInt(y*32);if(collision){var box=go.AddComponent<BoxCollider2D>();box.offset=new Vector2(0,.25f);box.size=new Vector2(.8f,.5f);}return go;}
        static void Sensor(GameObject owner,Vector2 offset,Vector2 size){var go=new GameObject("Interaction range",typeof(BoxCollider2D),typeof(InteractableProximitySensor));go.transform.SetParent(owner.transform,false);var box=go.GetComponent<BoxCollider2D>();box.isTrigger=true;box.offset=offset;box.size=size;}
    }
}
