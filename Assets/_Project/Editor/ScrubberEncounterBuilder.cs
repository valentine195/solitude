using System;
using System.IO;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Core.Interaction;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.ScrubberEncounter;
using SOLITUDE.World.SolitudeStart;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor
{
    public static class ScrubberEncounterBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private const string Source = "Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity";
        private const string Feature = "Assets/_Project/Features/World/ScrubberEncounter";
        private static readonly Color Amber = new Color(.95f, .66f, .23f);
        private static readonly Color White = new Color(.9f,.91f,.87f);
        private static TMP_FontAsset font;
        private static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        [MenuItem("SOLITUDE/Demos/Build Atmospheric Scrubber Encounter")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new InvalidOperationException("Encounter already exists; refusing to overwrite.");
            Directory.CreateDirectory(Feature + "/Demo"); Directory.CreateDirectory(Feature + "/UI"); AssetDatabase.Refresh();
            if (!AssetDatabase.CopyAsset(Source, ScenePath)) throw new InvalidOperationException("Cannot copy reference scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var socket = scene.GetRootGameObjects().Single(g => g.name == "Auxiliary power socket").GetComponent<SpriteRenderer>();
                var sprite = socket.sprite; var material = socket.sharedMaterial;
                string[] keep = { "GameManager", "PF_Player", "Global Light 2D", "EventSystem", "UICanvas", "SceneBindings", "RecoveryNoticeCanvas", "Opening Ship Structure", "Main Camera" };
                foreach (var root in scene.GetRootGameObjects()) if (!keep.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
                MakeRoom(scene);
                new GameObject("DisposableScrubberSession").AddComponent<RestorationDemoSession>();
                var rootConfig = new SerializedObject(All<GameCompositionRoot>(scene).Single());
                var old = (GameRuntimeConfig)rootConfig.FindProperty("config").objectReferenceValue;
                var database = UnityEngine.Object.Instantiate(old.database);
                AssetDatabase.CreateAsset(database, Feature + "/Demo/ScrubberItemDatabase.asset");
                var config = UnityEngine.Object.Instantiate(old); config.database = database;
                AssetDatabase.CreateAsset(config, Feature + "/Demo/ScrubberRuntimeConfig.asset");
                rootConfig.FindProperty("config").objectReferenceValue = config; rootConfig.ApplyModifiedPropertiesWithoutUndo();
                var scope = All<SceneBindings>(scene).Single();
                var binding = new SerializedObject(scope);
                foreach (var field in new[] { "lockers", "worldPickups", "restorationJunctions" }) binding.FindProperty(field).arraySize = 0;
                binding.ApplyModifiedPropertiesWithoutUndo();
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/pixel_operator/FontAssets/PixelOperator SDF.asset");
                if (font == null) throw new InvalidOperationException("Existing UI font is missing.");
                var equipmentGO = new GameObject("AtmosphericScrubber", typeof(SpriteRenderer), typeof(ScrubberEquipment), typeof(ScrubberAccess));
                equipmentGO.transform.position = new Vector3(23, 6.5f);
                var renderer = equipmentGO.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = 20;
                var sensorGO = new GameObject("InteractionRange", typeof(BoxCollider2D), typeof(InteractableProximitySensor));
                sensorGO.transform.SetParent(equipmentGO.transform, false); var sensor = sensorGO.GetComponent<BoxCollider2D>(); sensor.isTrigger = true; sensor.size = new Vector2(2,2);
                var equipment = equipmentGO.GetComponent<ScrubberEquipment>();
                var screen = Screen(equipment);
                equipmentGO.GetComponent<ScrubberAccess>().Configure(screen);
                var warning = new GameObject("RoomAtmosphereWarning", typeof(TextMeshPro)); warning.transform.position = new Vector3(23,7.4f); warning.transform.localScale = Vector3.one;
                var label = warning.GetComponent<TextMeshPro>(); label.font = font; label.fontSize = 3; label.color = Amber; label.alignment = TextAlignmentOptions.Center; label.text = "CO₂ ELEVATED"; label.rectTransform.sizeDelta = new Vector2(5,.6f);
                var audio = equipmentGO.AddComponent<AudioSource>(); audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Feature + "/Audio/co2_alert.wav");
                audio.loop = true; audio.playOnAwake = false; audio.volume = .12f; audio.spatialBlend = 0;
                if (audio.clip == null) throw new InvalidOperationException("Alert clip is missing.");
                equipmentGO.AddComponent<ScrubberRoomFeedback>().Configure(audio,label);
                var player = All<PlayerInteractor>(scene).Single(); player.transform.position = new Vector3(23,4.5f);
                var camera = All<Camera>(scene).Single(); camera.transform.position = new Vector3(23,4.5f,-10);
                camera.GetComponent<SolitudeCameraFollow2D>().Configure(player.transform,new Vector2(23,23),new Vector2(4.5f,4.5f));
                scope.Validate(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene,true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
        private static void MakeRoom(Scene scene)
        {
            var maps = All<Tilemap>(scene);
            foreach (var map in maps)
            {
                TileBase sample = null;
                foreach (var position in map.cellBounds.allPositionsWithin)
                { sample = map.GetTile(position); if (sample != null) break; }
                map.ClearAllTiles();
                if (sample == null) continue;
                if (map.name == "Floor")
                    for (int x=19;x<28;x++) for(int y=1;y<8;y++) map.SetTile(new Vector3Int(x,y,0),sample);
                else if(map.name == "North Wall Faces")
                    for(int x=19;x<28;x++) map.SetTile(new Vector3Int(x,8,0),sample);
                else
                {
                    for(int x=18;x<=28;x++) {map.SetTile(new Vector3Int(x,0,0),sample);map.SetTile(new Vector3Int(x,9,0),sample);}
                    for(int y=1;y<9;y++) {map.SetTile(new Vector3Int(18,y,0),sample);map.SetTile(new Vector3Int(28,y,0),sample);}
                }
                map.CompressBounds();
            }
            var structure = scene.GetRootGameObjects().Single(g=>g.name=="Opening Ship Structure");
            foreach(var c in structure.GetComponentsInChildren<Collider2D>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach(var edge in new[] { new Vector4(18.5f,4.5f,1,9),new Vector4(28.5f,4.5f,1,9),new Vector4(23.5f,.5f,11,1),new Vector4(23.5f,8.5f,11,1) })
            { var go=new GameObject("RoomBoundary",typeof(BoxCollider2D));go.transform.SetParent(structure.transform);go.transform.position=new Vector3(edge.x,edge.y);go.GetComponent<BoxCollider2D>().size=new Vector2(edge.z,edge.w); }
        }
        private static RectTransform Rect(string name,Transform parent, Vector2 pos, Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); if(parent!=null)go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;
        }
        private static TextMeshProUGUI Text(string name,Transform parent,string text,Vector2 pos,Vector2 size,float points,Color color)
        {
            var rect=Rect(name,parent,pos,size);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=points;label.color=color;label.raycastTarget=false;return label;
        }
        private static UnityEngine.UI.Button Button(string name,Transform parent,string text,Vector2 pos,bool enabled)
        {
            var rect=Rect(name,parent,pos,new Vector2(320,48));var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=enabled?Amber:new Color(.16f,.18f,.18f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.interactable=enabled;
            var label=Text(name+"Label",rect,text,Vector2.zero,new Vector2(300,40),24,enabled?Color.black:White);label.alignment=TextAlignmentOptions.Center;return button;
        }
        private static ScrubberScreenView Screen(ScrubberEquipment equipment)
        {
            var canvasGO=Rect("ScrubberScreen",null,Vector2.zero,new Vector2(1280,720)).gameObject;
            var canvas=canvasGO.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=150;
            var scaler=canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var panel=Rect("Panel",canvasGO.transform,Vector2.zero,new Vector2(1100,630));var bg=panel.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.035f,.05f,.055f,.98f);
            Text("Title",panel,"ATMOSPHERIC SCRUBBER",new Vector2(-225,260),new Vector2(600,50),34,White);
            Text("Location",panel,"SERVICE ROOM",new Vector2(320,260),new Vector2(360,50),24,White);
            Text("Intake",panel,"INTAKE\nFLOW STOPPED",new Vector2(-330,115),new Vector2(240,80),26,White);
            Text("Scrubber",panel,"SCRUBBER\n● OFFLINE",new Vector2(0,115),new Vector2(220,80),28,new Color(.9f,.26f,.24f));
            Text("Distribution",panel,"DISTRIBUTION\nFLOW STOPPED",new Vector2(330,115),new Vector2(260,80),26,White);
            Text("Selection",panel,"[ SELECTED UNIT ]",new Vector2(0,175),new Vector2(260,35),22,Amber);
            Text("Flow",panel,"→ INTERRUPTED →",new Vector2(0,15),new Vector2(340,40),24,White);
            var telemetry=Text("Telemetry",panel,"",new Vector2(-245,-110),new Vector2(500,180),25,White);
            var objective=Text("Objective",panel,"",new Vector2(285,-25),new Vector2(360,70),24,Amber);
            Button("InstallFuse",panel,"INSTALL FUSE",new Vector2(285,-105),false);
            Text("InstallReason",panel,"Requires 1 replacement fuse in inventory",new Vector2(285,-155),new Vector2(350,50),21,White);
            Button("Restart",panel,"RESTART — UNAVAILABLE",new Vector2(285,-205),false);
            var feedback=Text("Feedback",panel,"",new Vector2(-230,-245),new Vector2(530,55),23,White);
            var close=Button("Close",panel,"CLOSE",new Vector2(285,-275),true);
            var view=canvasGO.AddComponent<ScrubberScreenView>();view.Configure(panel.gameObject,null,telemetry,objective,feedback,close);panel.gameObject.SetActive(false);
            var prefab=PrefabUtility.SaveAsPrefabAsset(canvasGO,Feature+"/UI/ScrubberScreen.prefab"); UnityEngine.Object.DestroyImmediate(canvasGO);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var result=instance.GetComponent<ScrubberScreenView>();
            var serialized=new SerializedObject(result);serialized.FindProperty("equipment").objectReferenceValue=equipment;serialized.ApplyModifiedPropertiesWithoutUndo();return result;
        }
    }
}
