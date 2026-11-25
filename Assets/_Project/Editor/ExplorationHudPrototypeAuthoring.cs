using System;
using SOLITUDE.Demos;
using SOLITUDE.Player.UI;
using SOLITUDE.UI;
using SOLITUDE.UI.Exploration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SOLITUDE.Editor
{
    public static class ExplorationHudPrototypeAuthoring
    {
        private const string Root = "Assets/_Project/";
        private const string Demo = Root + "Scenes/Demos/HudPrototype/";
        private const string PrefabPath = Root + "Systems/ExplorationHud/ExplorationHud.prefab";
        private const string MaterialPath = Root + "Systems/ExplorationHud/ExplorationHudText.mat";
        private const string WorldMaterialPath = Root + "Systems/ExplorationHud/ExplorationHudWorldText.mat";
        public const string ScenePath = Demo + "ExplorationHudPrototype.unity";
        private static readonly Vector2 ReferenceSize = new(1920, 1080);
        private static readonly Color Ink = new Color32(9, 20, 30, 255);
        private static readonly Color DeepInk = new Color32(7, 16, 26, 255);
        private static readonly Color Rim = new Color32(72, 99, 123, 255);
        private static readonly Color QuietRim = new Color32(58, 82, 106, 255);
        private static readonly Color Mint = new Color32(112, 250, 185, 255);
        private static readonly Color Lavender = new Color32(165, 179, 248, 255);
        private static readonly Color Amber = new Color32(255, 194, 75, 255);
        private static readonly Color Text = new Color32(245, 248, 252, 255);
        private static readonly Color Secondary = new Color32(207, 219, 230, 255);
        private static readonly Color Track = new Color32(42, 62, 80, 255);
        private static readonly Vector2 TopLeft = new(0, 1);
        private static readonly Vector2 TopRight = new(1, 1);
        private static readonly Vector2 BottomRight = new(1, 0);
        private static readonly Vector2 BottomCenter = new(.5f, 0);
        private static readonly (string art, string name, string description, int quantity)[] Items =
        {
            ("bandages", "Bandages", "Heals 25 HP", 3),
            ("parts", "Spare parts", "Repair supplies", 2),
            ("battery", "Battery", "Portable power", 1),
            ("medkit", "Medkit", "Heals 50 HP", 2),
            ("oxygen", "Oxygen canister", "Restores 35% O<sub>2</sub>", 1)
        };

        private static TMP_FontAsset font;
        private static Material plainText;
        private static Material worldLabel;

        [MenuItem("SOLITUDE/Demos/Rebuild Exploration HUD Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before rebuilding the HUD prototype.");

            var original = SceneManager.GetActiveScene();
            var openPrototype = SceneManager.GetSceneByPath(ScenePath);
            if (openPrototype.IsValid() && openPrototype.isLoaded)
                throw new InvalidOperationException("Close the HUD prototype scene before rebuilding it.");

            bool replaceEmptyScene = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(original.path)
                && original.rootCount == 0;
            if (!replaceEmptyScene)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                        throw new InvalidOperationException("Save or close untitled scenes before rebuilding the HUD prototype.");
                }
            }

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "UI/Fonts/Exo_2/Exo2-Regular SDF.asset");
            if (font == null)
                throw new InvalidOperationException("The project's Exo 2 font asset is missing.");

            ImportPreviewSprites();
            plainText = SaveTextMaterial(MaterialPath);
            worldLabel = SaveTextMaterial(WorldMaterialPath);
            worldLabel.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            worldLabel.SetColor(ShaderUtilities.ID_UnderlayColor, new Color32(4, 12, 20, 255));
            worldLabel.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.16f);
            worldLabel.SetFloat(ShaderUtilities.ID_UnderlayDilate, .60f);
            worldLabel.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .20f);
            EditorUtility.SetDirty(worldLabel);

            var mode = replaceEmptyScene ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);
            try
            {
                GameObject prefab = BuildPrefab();
                BuildScene(prefab);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException($"Unable to save {ScenePath}");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (!replaceEmptyScene)
                {
                    SceneManager.SetActiveScene(original);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static Material SaveTextMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }

            material.CopyPropertiesFromMaterial(font.material);
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            material.SetFloat(ShaderUtilities.ID_WeightBold, .48f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildPrefab()
        {
            RectTransform root = Place("ExplorationHud", null, 0, 0, 0, 0);
            Stretch(root);
            var view = root.gameObject.AddComponent<ExplorationHudView>();
            var serialized = new SerializedObject(view);
            BuildVitals(root, serialized.FindProperty("vitals"));
            BuildNavigation(root, serialized.FindProperty("navigation"));
            BuildEquipment(root, serialized.FindProperty("equipment"));
            BuildQuickUse(root, serialized.FindProperty("quickUse"));
            BuildColonist(root, serialized.FindProperty("colonist"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            if (prefab == null) throw new InvalidOperationException($"Unable to save {PrefabPath}");
            return prefab;
        }

        private static void BuildVitals(RectTransform root, SerializedProperty view)
        {
            var panel = Anchored("Vitals", root, TopLeft, TopLeft, 57, -44, 330, 126);
            Shape("Rim", panel, 0, 0, 330, 126, Rim,
                (9, 0), (298, 0), (330, 29), (274, 126), (9, 126), (0, 117), (0, 9));
            Shape("Face", panel, 3, 3, 323, 120, Ink,
                (7, 0), (294, 0), (323, 27), (269, 120), (7, 120), (0, 113), (0, 7));
            Shape("Accent", panel, 0, 0, 6, 88, Mint, (6, 0), (6, 88), (0, 81), (0, 8));
            Cross(panel, 27, 16, 26, Mint);
            Label("HealthLabel", panel, "HEALTH", 67, 9, 134, 33, 23, Text, true);
            Ref(view, "health", Meter("HealthMeter", panel, 29, 47, 266, 20, Mint,
                new Color32(30, 65, 65, 255), 4));
            var oxygenLabel = Label("OxygenLabel", panel, "O<sub>2</sub>", 28, 76, 52, 36, 27, Text, true);
            oxygenLabel.overflowMode = TextOverflowModes.Overflow;
            Ref(view, "oxygen", Meter("OxygenMeter", panel, 84, 89, 180, 19,
                Lavender, new Color32(37, 52, 77, 255), 0));
            Ref(view, "healthWarning", Warning(panel, "LowHealth", "LOW HEALTH"));
            Ref(view, "oxygenWarning", Warning(panel, "LowOxygen", "LOW OXYGEN"));
        }

        private static GameObject Warning(RectTransform parent, string name, string text)
        {
            var row = Place(name, parent, 1, 137, 250, 27);
            Shape("WarningTriangle", row, 4, 3, 23, 21, Amber, (11.5f, 0), (23, 21), (0, 21));
            Box("ExclamationStem", row, 14.5f, 9, 2, 7, DeepInk);
            Box("ExclamationDot", row, 14.5f, 18, 2, 2, DeepInk);
            Label("WarningText", row, text, 36, 0, 168, 27, 21, Amber, true, worldText: true);
            row.gameObject.SetActive(false);
            return row.gameObject;
        }

        private static void BuildNavigation(RectTransform root, SerializedProperty view)
        {
            var nav = Anchored("Navigation", root, TopRight, TopRight, -49, -41, 346, 310);
            Bevel("MapFrame", nav, 0, 0, 346, 219, 17, Ink, Rim, 3);
            Ref(view, "location", Label("Location", nav, "DECK 3 — MEDBAY", 24, 8, 304, 30, 22, Text, true));
            Box("HeaderRule", nav, 20, 41, 306, 2, Rim);

            var map = Place("Map", nav, 20, 48, 306, 160);
            var mapLine = new Color32(78, 119, 144, 255);
            var explored = new Color32(27, 54, 71, 255);
            MapRoom("WestPassage", map, 4, 72, 82, 25, 0, explored, mapLine);
            Box("WestEnd", map, 4, 51, 2, 63, mapLine);
            Box("WestCapTop", map, 1, 51, 11, 2, mapLine);
            Box("WestCapBottom", map, 1, 112, 11, 2, mapLine);
            MapRoom("EastPassage", map, 184, 79, 69, 19, 0, explored, mapLine);
            MapRoom("NorthAlcove", map, 116, 1, 42, 25, 0, explored, mapLine);
            Shape("MedbayOutline", map, 56, 17, 139, 135, mapLine,
                (16, 0), (139, 0), (139, 34), (100, 34), (100, 123), (89, 135), (0, 135), (0, 16));
            Shape("MedbayExplored", map, 58, 19, 135, 131, explored,
                (14, 0), (135, 0), (135, 30), (96, 30), (96, 120), (86, 131), (0, 131), (0, 14));
            MapRoom("CurrentRoom", map, 115, 48, 81, 65, 0,
                new Color32(17, 39, 55, 255), mapLine);
            MapRoom("UnexploredRoom", map, 239, 42, 56, 70, 8, DeepInk, QuietRim);
            MapRoom("RegulatorRoom", map, 239, 70, 56, 42, 0, explored, mapLine);
            Box("RoomTick", map, 113, 84, 8, 2, mapLine);
            Box("LowerTick", map, 114, 150, 2, 11, mapLine);

            var player = Marker("PlayerMarker", map, new Vector2(.52f, .52f), 24);
            Shape("ArrowLeft", player, 0, 0, 24, 24, Mint, (12, 0), (12, 17), (0, 24));
            Shape("ArrowRight", player, 0, 0, 24, 24, Mint, (12, 0), (24, 24), (12, 17));
            Ref(view, "playerMarker", player);
            var marker = Marker("ObjectiveMarker", map, new Vector2(.9f, .56f), 30);
            Diamond(marker, 0, 0, 30, 5);
            Ref(view, "objectiveMarker", marker);

            var block = Place("Objective", nav, 0, 239, 346, 63);
            Box("ObjectiveRule", block, 0, 2, 2, 58, Mint);
            Diamond(block, 18, 1, 34, 6);
            Ref(view, "objectiveBlock", block.gameObject);
            Ref(view, "objective", Label("ObjectiveTitle", block, "RESTORE LIFE SUPPORT", 65, 0, 287, 31,
                23, Text, true, worldText: true));
            Ref(view, "task", Label("CurrentTask", block, "Find O<sub>2</sub> regulator", 65, 34, 287, 30,
                22, Secondary, worldText: true));
        }

        private static void BuildEquipment(RectTransform root, SerializedProperty view)
        {
            var panel = Anchored("Equipment", root, Vector2.zero, Vector2.zero, 57, 91, 381, 123);
            Shape("Rim", panel, 0, 0, 381, 123, Rim,
                (10, 0), (345, 0), (381, 30), (334, 123), (10, 123), (0, 114), (0, 10));
            Shape("Face", panel, 3, 3, 374, 117, Ink,
                (8, 0), (341, 0), (374, 28), (329, 117), (8, 117), (0, 109), (0, 8));
            Shape("Accent", panel, 0, 0, 8, 123, Mint, (8, 0), (8, 123), (0, 114), (0, 10));
            Ref(view, "icon", Picture("WrenchIcon", panel, 29, 22, 73, 72, Sprite("wrench")));
            Ref(view, "itemName", Label("WeaponName", panel, "WRENCH", 125, 10, 205, 40, 24, Text, true));
            Ref(view, "durability", Meter("Durability", panel, 125, 58, 210, 15, Secondary, Track, 4));
            Ref(view, "condition", Label("Condition", panel, "DURABLE", 126, 79, 145, 31, 19, Secondary));
        }

        private static void BuildQuickUse(RectTransform root, SerializedProperty view)
        {
            var bar = Anchored("QuickUse", root, BottomCenter, new Vector2(.5f, 0), 0, 92, 562, 184);
            var slots = view.FindPropertyRelative("slots");
            slots.arraySize = Items.Length;
            view.FindPropertyRelative("quietRim").colorValue = ShipboardUiColors.QuickUseRim;
            view.FindPropertyRelative("selectionRim").colorValue = ShipboardUiColors.QuickUseSelection;
            for (int i = 0; i < Items.Length; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                var item = Items[i];
                var cell = Place($"Slot{i + 1}", bar, i * 116, 92, 98, 92);
                var rim = Bevel("Rim", cell, 0, 0, 98, 92, 11, Ink, i == 0 ? Mint : QuietRim, 3);
                var face = cell.Find("Rim/Face").GetComponent<Graphic>();
                face.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color32(210, 239, 255, 255);
                colors.pressedColor = new Color32(145, 215, 181, 255);
                colors.selectedColor = Color.white;
                colors.disabledColor = Color.white;
                button.colors = colors;
                button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
                Ref(slot, "button", button);
                Ref(slot, "rim", rim);
                Label("SlotNumber" + (i + 1), bar, (i + 1).ToString(), i * 116, 56, 98, 31, 24,
                    Text, true, TextAlignmentOptions.Center, true);
                Ref(slot, "icon", Picture("ItemIcon", cell, 23, 13, 51, 49, Sprite(item.art)));
                Ref(slot, "quantity", Label("Quantity", cell, $"×{item.quantity}", 47, 54, 38, 31, 20,
                    Text, true, TextAlignmentOptions.MidlineRight));
            }

            // Right-align the copy against the fixed prompt so longer item names grow into free space.
            var detail = Place("SelectedItemDetail", bar, 0, 3, 562, 43);
            var group = detail.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Ref(view, "detail", group);
            Ref(view, "description", Label("Description", detail, "<b>Bandages</b>  ·  Heals 25 HP",
                -85, 1, 415, 39, 23, Text, false, TextAlignmentOptions.MidlineRight, true));
            var prompt = Place("UsePrompt", detail, 360, 3, 143, 35);
            Box("Divider", prompt, 0, 2, 2, 33, Rim);
            Bevel("Keycap", prompt, 37, 2, 33, 34, 5, Secondary, Secondary, 0);
            Ref(view, "inputGlyph", Label("InputGlyph", prompt, "F", 37, 2, 33, 34, 22, DeepInk, true,
                TextAlignmentOptions.Center));
            Label("UseAction", prompt, "Use", 83, 1, 59, 36, 23, Text, worldText: true);
            Ref(view, "prompt", prompt.gameObject);
        }

        private static void BuildColonist(RectTransform root, SerializedProperty view)
        {
            var panel = Anchored("Colonist", root, BottomRight, BottomRight, -49, 91, 365, 126);
            Bevel("Frame", panel, 0, 0, 365, 126, 17, Ink, Rim, 3);
            Bevel("PortraitFrame", panel, 0, 0, 134, 126, 18, new Color32(23, 41, 56, 255), Rim, 3);
            Box("PortraitTopAccent", panel, 18, 0, 101, 3, Mint);
            Box("PortraitSideAccent", panel, 0, 18, 3, 90, Mint);
            Box("PortraitBottomAccent", panel, 18, 123, 101, 3, Mint);
            Shape("PortraitTopCut", panel, 0, 0, 20, 20, Mint, (18, 0), (20, 2), (2, 20), (0, 18));
            Shape("PortraitBottomCut", panel, 0, 106, 20, 20, Mint, (2, 0), (20, 18), (18, 20), (0, 2));
            Ref(view, "portrait", Picture("Portrait", panel, 4, 3, 126, 120, Sprite("mara_vale")));
            Ref(view, "colonistName", Label("ColonistName", panel, "MARA VALE", 153, 12, 195, 33, 24, Text, true));
            Ref(view, "background", Label("Profession", panel, "Engineer", 153, 46, 190, 31, 22, Secondary));
            var traitIcon = Picture("TraitIcon", panel, 156, 81, 26, 27, Sprite("wrench"));
            traitIcon.color = Mint;
            Ref(view, "trait", Label("Trait", panel, "Repair +10%", 196, 78, 156, 34, 22, Text));
        }

        private static void BuildScene(GameObject prefab)
        {
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DeepInk;
            camera.cullingMask = 0;

            var ui = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.layer = 5;
            ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var canvas = (RectTransform)ui.transform;
            var background = Box("BackdropColor", canvas, 0, 0, 0, 0, DeepInk);
            Stretch(background.rectTransform);
            var backdrop = Picture("MedbayReference", canvas, 0, 0, 1920, 804, Sprite("medbay"));
            backdrop.rectTransform.pivot = new Vector2(.5f, 1);
            backdrop.rectTransform.anchorMin = backdrop.rectTransform.anchorMax = new Vector2(.5f, 1);
            backdrop.rectTransform.anchoredPosition = Vector2.zero;
            var testLight = Box("BrightLighting", canvas, 0, 0, 0, 0, new Color32(197, 210, 220, 168));
            Stretch(testLight.rectTransform);
            testLight.gameObject.SetActive(false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            Stretch((RectTransform)instance.transform);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var preview = new GameObject("HudPreview").AddComponent<ExplorationHudPreview>();
            var data = new SerializedObject(preview);
            data.FindProperty("view").objectReferenceValue = instance.GetComponent<ExplorationHudView>();
            data.FindProperty("brightLighting").objectReferenceValue = testLight.gameObject;
            data.FindProperty("wrench").objectReferenceValue = Sprite("wrench");
            data.FindProperty("portrait").objectReferenceValue = Sprite("mara_vale");
            var list = data.FindProperty("items");
            list.arraySize = Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("icon").objectReferenceValue = Sprite(Items[i].art);
                item.FindPropertyRelative("name").stringValue = Items[i].name;
                item.FindPropertyRelative("description").stringValue = Items[i].description;
                item.FindPropertyRelative("quantity").intValue = Items[i].quantity;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ImportPreviewSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Demo.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("_reference.png", StringComparison.Ordinal)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static Sprite Sprite(string art) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Demo + art + "_reference.png");

        private static RectTransform Place(string name, RectTransform parent, float x, float y, float w, float h) =>
            Anchored(name, parent, TopLeft, TopLeft, x, -y, w, h);

        private static RectTransform Anchored(string name, RectTransform parent, Vector2 anchor, Vector2 pivot,
            float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = rect.offsetMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMax = Vector2.zero;
        }

        private static Image Picture(string name, RectTransform parent, float x, float y, float w, float h, Sprite sprite)
        {
            var image = Box(name, parent, x, y, w, h, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        private static Image Box(string name, RectTransform parent, float x, float y, float w, float h, Color tint)
        {
            var graphic = Place(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            graphic.color = tint;
            graphic.raycastTarget = false;
            return graphic;
        }

        private static VitalsHudShape Shape(string name, RectTransform parent, float x, float y, float w, float h,
            Color tint, params (float x, float y)[] points)
        {
            var graphic = Place(name, parent, x, y, w, h).gameObject.AddComponent<VitalsHudShape>();
            graphic.color = tint;
            graphic.raycastTarget = false;
            var data = new SerializedObject(graphic);
            var list = data.FindProperty("points");
            list.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
                list.GetArrayElementAtIndex(i).vector2Value = new Vector2(points[i].x / w, points[i].y / h);
            data.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }

        internal static VitalsHudShape Bevel(string name, RectTransform parent, float x, float y, float w, float h,
            float cut, Color fill, Color rim, float stroke)
        {
            var outline = Shape(name, parent, x, y, w, h, rim,
                (cut, 0), (w - cut, 0), (w, cut), (w, h - cut), (w - cut, h),
                (cut, h), (0, h - cut), (0, cut));
            if (stroke > 0)
            {
                w -= 2 * stroke;
                h -= 2 * stroke;
                cut = Mathf.Max(0, cut - .6f * stroke);
                Shape("Face", outline.rectTransform, stroke, stroke, w, h, fill,
                    (cut, 0), (w - cut, 0), (w, cut), (w, h - cut), (w - cut, h),
                    (cut, h), (0, h - cut), (0, cut));
            }
            return outline;
        }

        private static void MapRoom(string name, RectTransform parent, float x, float y, float w, float h,
            float cut, Color fill, Color rim) => Bevel(name, parent, x, y, w, h, cut, fill, rim, 2);

        private static void Cross(RectTransform parent, float x, float y, float size, Color tint)
        {
            Box("CrossHorizontal", parent, x, y + size / 3, size, size / 3, tint);
            Box("CrossVertical", parent, x + size / 3, y, size / 3, size, tint);
        }

        private static void Diamond(RectTransform parent, float x, float y, float size, float stroke)
        {
            float mid = size / 2;
            Shape("DiamondRim", parent, x, y, size, size, Amber, (mid, 0), (size, mid), (mid, size), (0, mid));
            float inside = size - 2 * stroke;
            mid = inside / 2;
            Shape("DiamondFace", parent, x + stroke, y + stroke, inside, inside, Ink,
                (mid, 0), (inside, mid), (mid, inside), (0, mid));
            float dot = size * .27f;
            Shape("DiamondCenter", parent, x + (size - dot) / 2, y + (size - dot) / 2, dot, dot, Amber,
                (dot / 2, 0), (dot, dot / 2), (dot / 2, dot), (0, dot / 2));
        }

        private static RectTransform Marker(string name, RectTransform parent, Vector2 position, float size) =>
            Anchored(name, parent, new Vector2(position.x, 1 - position.y), Vector2.one * .5f, 0, 0, size, size);

        private static VitalsHudMeter Meter(string name, RectTransform parent, float x, float y, float w, float h,
            Color fill, Color track, float gap)
        {
            var meter = Place(name, parent, x, y, w, h).gameObject.AddComponent<VitalsHudMeter>();
            meter.color = fill;
            meter.raycastTarget = false;
            var data = new SerializedObject(meter);
            data.FindProperty("trackColor").colorValue = track;
            data.FindProperty("segmentGap").floatValue = gap;
            data.ApplyModifiedPropertiesWithoutUndo();
            return meter;
        }

        private static TextMeshProUGUI Label(string name, RectTransform parent, string value, float x, float y,
            float w, float h, float size, Color tint, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool worldText = false)
        {
            var text = Place(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = worldText ? worldLabel : plainText;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = tint;
            text.text = value;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = align;
            text.raycastTarget = false;
            return text;
        }

        private static void Ref(SerializedProperty owner, string name, UnityEngine.Object value) =>
            owner.FindPropertyRelative(name).objectReferenceValue = value;
    }
}
