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
        public const string JournalFolder = "Assets/_Project/Systems/Tasks/UI";
        [MenuItem("SOLITUDE/Demos/Add Task Journal To Scrubber Encounter")]
        public static void AddTaskJournal()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            AuthorJournalPrefabs();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try { EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); }
            finally { EditorSceneManager.CloseScene(scene,true); if(previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
        private static void EnsureJournal(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JournalFolder + "/TaskJournal.prefab");
            if (prefab == null) { AuthorJournalPrefabs(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JournalFolder + "/TaskJournal.prefab"); }
            var view = All<SOLITUDE.Tasks.UI.TaskJournalView>(scene).SingleOrDefault();
            if (view == null) view = ((GameObject)PrefabUtility.InstantiatePrefab(prefab,scene)).GetComponent<SOLITUDE.Tasks.UI.TaskJournalView>();
            var equipment = All<ScrubberEquipment>(scene).Single();
            var source = equipment.GetComponent<ScrubberTaskSource>();
            if (source == null) source = equipment.gameObject.AddComponent<ScrubberTaskSource>();
            source.Configure("scrubber-room-01", "DECK 01 / SCRUBBER ROOM", equipment, equipment.GetComponent<ScrubberAccess>());
            var data = new SerializedObject(All<SceneBindings>(scene).Single());
            data.FindProperty("journalView").objectReferenceValue = view;
            var sources = data.FindProperty("taskSources"); sources.arraySize = 1; sources.GetArrayElementAtIndex(0).objectReferenceValue = source;
            data.ApplyModifiedPropertiesWithoutUndo(); view.ValidateAuthoring();
        }
        private static RectTransform JRect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            var go = new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = low; rect.offsetMax = high; return rect;
        }
        private static void JSet(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static TextMeshProUGUI JText(string name, Transform parent, string value, float size, Color color)
        {
            var rect = JRect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = size; label.color = color;
            label.text = value; label.richText = false; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
        private static UnityEngine.UI.Image JImage(RectTransform rect, Color color, bool hit = false)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = hit; return image; }
        private static UnityEngine.UI.Button JButton(RectTransform rect, string value, float size = 24)
        {
            var image = JImage(rect,new Color(.08f,.10f,.10f),true); var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image; button.transition = UnityEngine.UI.Selectable.Transition.None;
            var label = JText("Label",rect,value,size,White); label.alignment = TextAlignmentOptions.Center;
            return button;
        }
        private static UnityEngine.UI.ScrollRect JScroll(RectTransform root)
        {
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 36;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = JRect("Viewport",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            JImage(viewport,Color.white,true); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = JRect("Content",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); content.pivot = new Vector2(.5f,1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 16; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; return scroll;
        }
        private static TMP_Text JDetail(UnityEngine.Object view, string field, Transform parent, string initial, float size, Color color)
        {
            var label = JText(field,parent,initial,size,color); label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().minHeight = size + 8;
            JSet(view,field,label); return label;
        }
        private static void JRule(Transform parent)
        {
            var rect = JRect("Divider",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            JImage(rect,new Color(.25f,.32f,.33f)); rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 1;
        }
        private static void JKey(UnityEngine.Object view, string field, Transform parent, string name, float x)
        {
            var rect = JRect(name,parent,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(x,-17),new Vector2(x+(name == "BackKey" ? 92 : 44),17));
            JImage(rect,Amber); var label = JText("Key",rect,"?",22,Color.black); label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = 22; JSet(view,field,label);
        }
        private static void JRowSymbols(SOLITUDE.Tasks.UI.TaskJournalRowView view)
        {
            var outcome = view.transform.Find("outcome").GetComponent<TMP_Text>(); outcome.margin = new Vector4(18,0,0,0);
            JSet(view,"outcomeIcon",JOutcomeIcon(outcome.transform));
            var layout = view.gameObject.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null) layout = view.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(18,18,12,12); layout.spacing = 4; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var sizing = view.GetComponent<UnityEngine.UI.LayoutElement>(); sizing.preferredHeight = -1; sizing.minHeight = 140;
            var selection = view.transform.Find("Selection").gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); selection.ignoreLayout = true;
        }
        private static UnityEngine.UI.Image JOutcomeIcon(Transform parent)
        {
            var icon = JImage(JRect("OutcomeIcon",parent,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(0,-5),new Vector2(10,5)),SOLITUDE.Tasks.UI.TaskJournalView.Operational);
            icon.enabled = false; return icon;
        }
        private static void AuthorJournalPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(JournalFolder + "/TaskJournal.prefab") != null) return;
            Directory.CreateDirectory(JournalFolder); AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/pixel_operator/FontAssets/PixelOperator SDF.asset");
            if (font == null) throw new InvalidOperationException("Existing journal font is missing.");
            var row = new GameObject("TaskJournalRow",typeof(RectTransform));
            GameObject rowPrefab;
            try
            {
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(460,170);
                row.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 170;
                var button = JButton(row.GetComponent<RectTransform>(),"",22); UnityEngine.Object.DestroyImmediate(button.GetComponentInChildren<TMP_Text>().gameObject);
                var view = row.AddComponent<SOLITUDE.Tasks.UI.TaskJournalRowView>(); JSet(view,"select",button);
                var selection = JImage(JRect("Selection",row.transform,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(4,0)),Amber); selection.enabled = false; JSet(view,"selection",selection);
                string[] fields = { "title", "location", "condition", "outcome", "availability" };
                for (int i = 0; i < fields.Length; i++)
                {
                    var label = JText(fields[i],row.transform,"",i == 0 ? 25 : 20,i == 1 ? new Color(.6f,.67f,.68f) : White);
                    var r = label.rectTransform; r.anchorMin = new Vector2(0,1); r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f,1);
                    r.anchoredPosition = new Vector2(0,-12-i*24); r.sizeDelta = new Vector2(-36,i == 0 ? 50 : 24);
                    if (i > 0) r.anchoredPosition = new Vector2(0,-64-(i-1)*20);
                    JSet(view,fields[i],label);
                }
                JRowSymbols(view); view.ValidateAuthoring(); rowPrefab = PrefabUtility.SaveAsPrefabAsset(row,JournalFolder+"/TaskJournalRow.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(row); }
            var canvasGO = new GameObject("TaskJournal",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            try
            {
                canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvasGO.GetComponent<Canvas>().sortingOrder = 180;
                var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
                var view = canvasGO.AddComponent<SOLITUDE.Tasks.UI.TaskJournalView>();
                var panel = JRect("Panel",canvasGO.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); JImage(panel,new Color(.035f,.05f,.055f,1),true); JSet(view,"panel",panel.gameObject);
                var header = JRect("Header",panel,new Vector2(0,1),Vector2.one,new Vector2(32,-76),new Vector2(-32,-20));
                var heading = JText("Heading",header,"TASK JOURNAL",38,White);
                var brand = JText("Brand",header,"SOLITUDE",22,new Color(.6f,.67f,.68f)); brand.alignment = TextAlignmentOptions.TopRight;
                var tabs = JRect("Tabs",panel,new Vector2(0,1),Vector2.one,new Vector2(32,-126),new Vector2(-32,-78));
                var a = JButton(JRect("Active",tabs,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(170,0)),"ACTIVE  0"); JSet(view,"activeTab",a); JSet(view,"activeCount",a.GetComponentInChildren<TMP_Text>());
                var c = JButton(JRect("Completed",tabs,Vector2.zero,new Vector2(0,1),new Vector2(180,0),new Vector2(390,0)),"COMPLETED  0"); JSet(view,"completedTab",c); JSet(view,"completedCount",c.GetComponentInChildren<TMP_Text>());
                JSet(view,"activeUnderline",JImage(JRect("ActiveUnderline",tabs,Vector2.zero,Vector2.zero,new Vector2(0,-1),new Vector2(170,3)),Amber));
                JSet(view,"completedUnderline",JImage(JRect("CompletedUnderline",tabs,Vector2.zero,Vector2.zero,new Vector2(180,-1),new Vector2(390,3)),Amber));
                var body = JRect("Body",panel,Vector2.zero,Vector2.one,new Vector2(32,80),new Vector2(-32,-146));
                var list = JScroll(JRect("TaskList",body,Vector2.zero,new Vector2(.42f,1),Vector2.zero,new Vector2(-18,0))); JSet(view,"listScroll",list);
                var empty = JText("Empty",list.viewport,"NO ACTIVE TASKS",24,new Color(.6f,.67f,.68f)); JSet(view,"empty",empty);
                JImage(JRect("ColumnRule",body,new Vector2(.42f,0),new Vector2(.42f,1),Vector2.zero,new Vector2(1,0)),new Color(.25f,.32f,.33f));
                var detail = JScroll(JRect("Details",body,new Vector2(.42f,0),Vector2.one,new Vector2(32,0),Vector2.zero)); JSet(view,"detailScroll",detail);
                detail.content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 10;
                var gray = new Color(.6f,.67f,.68f);
                JDetail(view,"title",detail.content,"NO TASK SELECTED",34,White); JDetail(view,"location",detail.content,"",22,gray); JRule(detail.content);
                var outcome = JDetail(view,"outcome",detail.content,"",22,White); outcome.margin = new Vector4(18,0,0,0); JSet(view,"outcomeIcon",JOutcomeIcon(outcome.transform));
                var confirmed = JText("Confirmed",detail.content,"CONFIRMED CONDITION",20,gray); confirmed.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 28;
                JSet(view,"confirmed",confirmed);
                JDetail(view,"condition",detail.content,"",28,SOLITUDE.Tasks.UI.TaskJournalView.Fault); JDetail(view,"availability",detail.content,"",20,gray); JRule(detail.content);
                JDetail(view,"stepLabel",detail.content,"NEXT STEP",20,gray); JDetail(view,"stepText",detail.content,"",30,White); JDetail(view,"blocker",detail.content,"",22,gray);
                var footer = JRect("Footer",panel,Vector2.zero,new Vector2(1,0),new Vector2(32,20),new Vector2(-32,64));
                var close = JRect("CloseControls",footer,new Vector2(1,0),Vector2.one,new Vector2(-360,0),Vector2.zero);
                JKey(view,"journalKey",close,"JournalKey",0); var closeText = JText("CloseLabel",close,"CLOSE",22,White); closeText.rectTransform.offsetMin = new Vector2(54,0); closeText.rectTransform.offsetMax = new Vector2(-200,0);
                JKey(view,"backKey",close,"BackKey",180); var back = JText("BackLabel",close,"BACK",22,White); back.rectTransform.offsetMin = new Vector2(284,0);
                JSet(view,"rowPrefab",rowPrefab.GetComponent<SOLITUDE.Tasks.UI.TaskJournalRowView>()); view.ValidateAuthoring(); panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(canvasGO,JournalFolder+"/TaskJournal.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(canvasGO); }
        }
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
                var warning = new GameObject("RoomAtmosphereWarning", typeof(TextMeshPro)); warning.transform.position = new Vector3(23,7.7f); warning.transform.localScale = Vector3.one;
                var label = warning.GetComponent<TextMeshPro>(); label.font = font; label.fontSize = 4.5f; label.color = Amber; label.alignment = TextAlignmentOptions.Center; label.text = "CO2 ELEVATED"; label.rectTransform.sizeDelta = new Vector2(5,.6f);
                label.renderer.sortingOrder = 2500;
                var audio = equipmentGO.AddComponent<AudioSource>(); audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Feature + "/Audio/co2_alert.wav");
                audio.loop = true; audio.playOnAwake = false; audio.volume = .12f; audio.spatialBlend = 0;
                if (audio.clip == null) throw new InvalidOperationException("Alert clip is missing.");
                equipmentGO.AddComponent<ScrubberRoomFeedback>().Configure(audio,label,EquipmentStatus(equipment));
                var player = All<PlayerInteractor>(scene).Single(); player.transform.position = new Vector3(23,4.5f);
                var camera = All<Camera>(scene).Single(); camera.transform.position = new Vector3(23,4.5f,-10);
                camera.GetComponent<SolitudeCameraFollow2D>().Configure(player.transform,new Vector2(23,23),new Vector2(4.5f,4.5f));
                scope.Validate(); EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene,true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
        public const string FuseFolder = "Assets/_Project/Features/Items/Definitions/ReplacementFuse";
        [MenuItem("SOLITUDE/Demos/Add Replacement Fuse To Scrubber Encounter")]
        public static void AddFuse()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            Directory.CreateDirectory(FuseFolder); AssetDatabase.Refresh();
            var sceneGuid = AssetDatabase.AssetPathToGUID(ScenePath);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                if (All<WorldPickup>(scene).Length != 0) throw new InvalidOperationException("Encounter pickup already exists; refusing to overwrite.");
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FuseFolder + "/ReplacementFuse.asset");
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    item.DisplayName = "Replacement fuse"; item.Description = "Replacement fuse for the atmospheric scrubber.";
                    item.Stackable = true; item.MaxStackSize = 99;
                    item.Icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/SOLITUDE/Props/solitude_battery_normal_v01.png");
                    if (item.Icon == null) throw new InvalidOperationException("Existing electrical component icon missing.");
                    AssetDatabase.CreateAsset(item, FuseFolder + "/ReplacementFuse.asset");
                    var data = new SerializedObject(item); data.FindProperty("itemId").stringValue = AssetDatabase.AssetPathToGUID(FuseFolder + "/ReplacementFuse.asset"); data.ApplyModifiedPropertiesWithoutUndo();
                }
                var definition = AssetDatabase.LoadAssetAtPath<PickupDefinition>(FuseFolder + "/ReplacementFusePickup.asset");
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<PickupDefinition>();
                    AssetDatabase.CreateAsset(definition, FuseFolder + "/ReplacementFusePickup.asset");
                    var data = new SerializedObject(definition); data.FindProperty("item").objectReferenceValue = item;
                    data.FindProperty("quantity").intValue = 1; data.FindProperty("successText").stringValue = "Picked up replacement fuse"; data.ApplyModifiedPropertiesWithoutUndo();
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FuseFolder + "/PF_ReplacementFuse.prefab");
                if (prefab == null)
                {
                    var go = new GameObject("PF_ReplacementFuse", typeof(SpriteRenderer), typeof(WorldPickup));
                    try
                    {
                        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = item.Icon;
                        renderer.sharedMaterial = All<ScrubberEquipment>(scene).Single().GetComponent<SpriteRenderer>().sharedMaterial;
                        renderer.sortingOrder = 20;
                        var data = new SerializedObject(go.GetComponent<WorldPickup>()); data.FindProperty("definition").objectReferenceValue = definition;
                        data.FindProperty("prompt").stringValue = "Collect replacement fuse"; data.ApplyModifiedPropertiesWithoutUndo();
                        var sensor = new GameObject("InteractionRange", typeof(BoxCollider2D), typeof(InteractableProximitySensor));
                        sensor.transform.SetParent(go.transform, false); sensor.GetComponent<BoxCollider2D>().isTrigger = true; sensor.GetComponent<BoxCollider2D>().size = Vector2.one;
                        prefab = PrefabUtility.SaveAsPrefabAsset(go, FuseFolder + "/PF_ReplacementFuse.prefab");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                }
                item.PickupPrefab = prefab; EditorUtility.SetDirty(item);
                var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(Feature + "/Demo/ScrubberItemDatabase.asset");
                var catalog = new SerializedObject(database); var list = catalog.FindProperty("items");
                if (!Enumerable.Range(0,list.arraySize).Any(i => list.GetArrayElementAtIndex(i).objectReferenceValue == item))
                { int index = list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = item; catalog.ApplyModifiedPropertiesWithoutUndo(); }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); instance.name = "Replacement fuse"; instance.transform.position = new Vector3(21,4);
                instance.GetComponent<SOLITUDE.SaveLoad.SaveableId>().AssignMissingSceneIdentity();
                var binding = new SerializedObject(All<SceneBindings>(scene).Single()); var pickups = binding.FindProperty("worldPickups");
                pickups.arraySize = 1; pickups.GetArrayElementAtIndex(0).objectReferenceValue = instance.GetComponent<WorldPickup>(); binding.ApplyModifiedPropertiesWithoutUndo();
                EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                if (AssetDatabase.AssetPathToGUID(ScenePath) != sceneGuid) throw new InvalidOperationException("Scene GUID changed.");
            }
            finally { EditorSceneManager.CloseScene(scene,true); if(previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
        private static TMP_Text EquipmentStatus(ScrubberEquipment equipment)
        {
            var existing=equipment.transform.Find("EquipmentStatus");
            if(existing!=null) return existing.GetComponent<TMP_Text>();
            var go=new GameObject("EquipmentStatus",typeof(TextMeshPro));go.transform.SetParent(equipment.transform,false);
            var label=go.GetComponent<TextMeshPro>();label.rectTransform.anchoredPosition=new Vector2(0,.6f);label.rectTransform.sizeDelta=new Vector2(4,.6f);
            label.font=font;label.fontSize=4.5f;label.alignment=TextAlignmentOptions.Center;label.text="SCRUBBER OFFLINE";label.color=new Color(.9f,.26f,.24f);label.raycastTarget=false;
            label.renderer.sortingOrder=2500;return label;
        }
        [MenuItem("SOLITUDE/Demos/Repair Scrubber Review Presentation")]
        public static void RepairReviewPresentation()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Author presentation only in a clean Edit-mode scene.");
            var path=Feature+"/UI/ScrubberScreen.prefab";var contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var button in contents.GetComponentsInChildren<UnityEngine.UI.Button>(true)) StyleAction(button);
                PrefabUtility.SaveAsPrefabAsset(contents,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/pixel_operator/FontAssets/PixelOperator SDF.asset");
            if(font==null) throw new InvalidOperationException("Existing UI font is missing.");
            var equipment=All<ScrubberEquipment>(scene).Single();var data=new SerializedObject(equipment.GetComponent<ScrubberRoomFeedback>());
            data.FindProperty("equipmentStatus").objectReferenceValue=EquipmentStatus(equipment);data.ApplyModifiedPropertiesWithoutUndo();
            // Pixel Perfect can adjust the Edit-mode viewport to the current dock size.
            // Retain the encounter's authored viewport when saving presentation changes.
            All<Camera>(scene).Single().rect=new Rect(0,.125f,1,.75000006f);
            EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static void StyleAction(UnityEngine.UI.Button button)
        {
            button.transition=UnityEngine.UI.Selectable.Transition.None;
            if(button.image!=null) button.image.color=new Color(.16f,.18f,.18f);
            var label=button.GetComponentInChildren<TMP_Text>();if(label!=null) label.color=White;
        }
        [MenuItem("SOLITUDE/Demos/Add Scrubber Running Audio")]
        public static void AddRunningAudio()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                var equipment=All<ScrubberEquipment>(scene).Single();var feedback=equipment.GetComponent<ScrubberRoomFeedback>();var data=new SerializedObject(feedback);
                var source=data.FindProperty("runningSound").objectReferenceValue as AudioSource;
                if(source==null) { source=equipment.gameObject.AddComponent<AudioSource>();source.loop=true;source.playOnAwake=false;source.volume=.15f;source.spatialBlend=0;data.FindProperty("runningSound").objectReferenceValue=source; }
                source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Feature+"/Audio/scrubber_running.wav");if(source.clip==null) throw new InvalidOperationException("Running clip missing.");
                data.ApplyModifiedPropertiesWithoutUndo();EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()) SceneManager.SetActiveScene(previous); }
        }
        [MenuItem("SOLITUDE/Demos/Add Scrubber Completion Notification")]
        public static void AddCompletionNotification()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring.");
            string path=Feature+"/UI/ScrubberScreen.prefab";
            var contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(contents.GetComponent<ScrubberTaskNotificationView>()==null)
                {
                    font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/pixel_operator/FontAssets/PixelOperator SDF.asset");
                    var rect=Rect("TaskCompletion",contents.transform,Vector2.zero,new Vector2(300,72));
                    rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-18,-18);
                    var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.035f,.05f,.055f,.98f);image.raycastTarget=false;
                    var text=Text("TaskCompletionText",rect,"TASK COMPLETE\nRoom atmosphere restored",new Vector2(-150,-36),new Vector2(276,60),22,new Color(.3f,.85f,.42f));
                    text.rectTransform.anchorMin=text.rectTransform.anchorMax=Vector2.one; text.rectTransform.pivot=new Vector2(.5f,.5f);
                    var view=contents.AddComponent<ScrubberTaskNotificationView>();var data=new SerializedObject(view);
                    data.FindProperty("notice").objectReferenceValue=rect.gameObject;data.FindProperty("screen").objectReferenceValue=contents.GetComponent<ScrubberScreenView>();data.ApplyModifiedPropertiesWithoutUndo();
                    rect.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(contents,path);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                var notification=All<ScrubberTaskNotificationView>(scene).Single();var data=new SerializedObject(notification);
                data.FindProperty("equipment").objectReferenceValue=All<ScrubberEquipment>(scene).Single();data.ApplyModifiedPropertiesWithoutUndo();
                EnsureJournal(scene); EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()) SceneManager.SetActiveScene(previous); }
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
            var label=Text(name+"Label",rect,text,Vector2.zero,new Vector2(300,40),24,White);label.alignment=TextAlignmentOptions.Center;StyleAction(button);return button;
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
            Text("Scrubber",panel,"SCRUBBER\nOFFLINE",new Vector2(0,115),new Vector2(220,80),28,new Color(.9f,.26f,.24f));
            Text("Distribution",panel,"DISTRIBUTION\nFLOW STOPPED",new Vector2(330,115),new Vector2(260,80),26,White);
            Text("Selection",panel,"[ SELECTED UNIT ]",new Vector2(0,175),new Vector2(260,35),22,Amber);
            Text("Flow",panel,"-> INTERRUPTED ->",new Vector2(0,15),new Vector2(340,40),24,White);
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
