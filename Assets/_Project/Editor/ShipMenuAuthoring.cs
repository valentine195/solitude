using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Player.UI;
using SOLITUDE.Tasks.UI;
using SOLITUDE.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SOLITUDE.Editor
{
    public static class ShipMenuAuthoring
    {
        private const string Prefab = "Assets/_Project/Systems/ShipMenu/ShipMenu.prefab";
        private const string Icons = "Assets/Space_Exploration_GUI_Kit/Picto_Icons/White/";
        private const string Regular = "Assets/_Project/UI/Fonts/Exo_2/Exo2-Regular SDF.asset";
        private const string Neutral = "Assets/_Project/Systems/Containers/Materials/InventoryText.mat";
        private static readonly Color Ink = new Color32(8, 19, 28, 255);
        private static readonly Color Surface = new Color32(12, 27, 37, 255);
        private static readonly Color Selected = new Color32(19, 49, 62, 255);
        private static readonly Color Rim = new Color32(51, 79, 96, 255);
        private static readonly Color Rule = new Color32(32, 57, 72, 255);
        private static readonly Color Mint = new Color32(84, 243, 173, 255);
        private static readonly Color Text = new Color32(237, 244, 248, 255);
        private static readonly Color Muted = new Color32(161, 186, 201, 255);
        private static readonly Color Objective = new Color32(255, 192, 65, 255);
        private static readonly Vector2 Reference = new(1920, 1080);
        private static TMP_FontAsset font;
        private static Material material;

        [MenuItem("SOLITUDE/Demos/Install Ship Menu In Scrubber Encounter")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring the ship menu.");
            var scene = SceneManager.GetSceneByPath(ScrubberEncounterBuilder.ScenePath);
            bool openHere = !scene.isLoaded;
            if (openHere) scene = EditorSceneManager.OpenScene(ScrubberEncounterBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save the Scrubber Encounter before installing the menu.");
                Configure(scene);
                SurvivorSuccessionAuthoring.SaveConfiguredScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (openHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("SOLITUDE/UI/Install Ship Menu In Active Scene")]
        public static void InstallInActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring the ship menu.");

            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.isDirty)
                throw new InvalidOperationException("Save the active scene before installing the ship menu.");
            var scope = Find<SceneBindings>(scene);
            if (scope == null || !scope.HasPlayer || Find<InventoryScreenView>(scene) == null)
                throw new InvalidOperationException("The active scene requires a composed player and Inventory screen.");

            if (Find<TaskJournalView>(scene) == null)
            {
                const string path = ScrubberEncounterBuilder.JournalFolder + "/TaskJournal.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("The Objectives view prefab is missing.");
                var journal = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<TaskJournalView>();
                Assign(scope, "journalView", journal);
                PrefabUtility.RecordPrefabInstancePropertyModifications(scope);
            }

            Configure(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the active scene.");
            AssetDatabase.SaveAssets();
        }

        internal static void Configure(Scene scene)
        {
            var scope = Find<SceneBindings>(scene);
            var journal = Find<TaskJournalView>(scene);
            var inventory = Find<InventoryScreenView>(scene);
            if (scope == null || journal == null || inventory == null) return;

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Regular);
            material = AssetDatabase.LoadAssetAtPath<Material>(Neutral);
            if (font == null || material == null) throw new InvalidOperationException("Ship menu fonts are missing.");

            if (Ref<Graphic>(journal, "titleMark") == null) StyleJournalPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (prefab == null) prefab = AuthorShell();

            var shell = Find<ShipMenuView>(scene);
            if (shell == null)
            {
                // The original journal instance contains only visual and layout overrides.
                // Its current authored layout now lives on the prefab.
                PrefabUtility.RevertPrefabInstance(journal.gameObject, InteractionMode.AutomatedAction);
                shell = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<ShipMenuView>();
            }

            StyleInventoryInstance(inventory);
            Assign(scope, "shipMenu", shell);
            PrefabUtility.RecordPrefabInstancePropertyModifications(scope);
        }

        private static T Find<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).SingleOrDefault();

        private static GameObject AuthorShell()
        {
            var root = new GameObject("ShipMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                ConfigureCanvas(root, 30100);
                var shell = root.AddComponent<ShipMenuView>();
                var frame = Place("Frame", root.transform, 0, 0, 1920, 1080);
                frame.anchorMin = frame.anchorMax = frame.pivot = Vector2.one * .5f;
                frame.anchoredPosition = Vector2.zero;

                Line("HeaderRule", frame, 52, 106, 1868, 106, Rule, 2);
                Box("WordmarkAccent", frame, 64, 42, 4, 48, Mint);
                var wordmark = Label("Wordmark", frame, "SOLITUDE", 92, 41, 213, 49, 32, Text, true);
                wordmark.characterSpacing = 8;

                BuildTabs(shell, frame);
                BuildBlankPage(shell, root.transform);

                FrameLines("ContentRim", frame, 52, 152, 1816, 802, 23, Rim);
                FrameLines("FooterRim", frame, 52, 954, 1816, 84, 20, Rule, false);
                var closeRect = Place("Close", frame, 92, 969, 270, 54);
                var close = closeRect.gameObject.AddComponent<Button>();
                close.targetGraphic = Paint(closeRect, Color.clear, true);
                close.transition = Selectable.Transition.None;
                Box("Keycap", closeRect, 0, 9, 67, 37, Muted);
                var key = Label("CloseKey", closeRect, "ESC", 0, 9, 67, 37, 25, Ink, true);
                key.alignment = TextAlignmentOptions.Center;
                key.enableAutoSizing = true;
                key.fontSizeMin = 15;
                key.fontSizeMax = 25;
                Assign(shell, "close", close);
                Assign(shell, "closeKey", key);
                Assign(shell, "closeLabel", Label("CloseLabel", closeRect, "Close", 84, 4, 184, 48, 27, Muted));

                SetColor(shell, "selectedFill", Mint);
                SetColor(shell, "focusFill", Selected);
                SetColor(shell, "selectedText", Ink);
                SetColor(shell, "idleText", Muted);
                root.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("SOLITUDE/UI/Rebuild Ship Menu Tabs")]
        public static void RebuildTabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before rebuilding the tabs.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Regular);
            material = AssetDatabase.LoadAssetAtPath<Material>(Neutral);
            if (font == null || material == null) throw new InvalidOperationException("Ship menu fonts are missing.");

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var shell = root.GetComponent<ShipMenuView>();
                var frame = (RectTransform)root.transform.Find("Frame");
                foreach (var child in new[] { frame.Find("Tabs"), root.transform.Find("BlankPage") })
                    if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
                BuildTabs(shell, frame);
                BuildBlankPage(shell, root.transform);
                SetColor(shell, "focusFill", Selected);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save the ship menu tabs.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildTabs(ShipMenuView shell, RectTransform frame)
        {
            const float width = 1548;
            var header = Place("Tabs", frame, 320, 33, width, 75);
            Polygon("RailEdge", header, 0, 0, width, 75, Rim,
                (42, 0), (1525, 0), (1548, 23), (1548, 75), (0, 75));
            Polygon("RailFace", header, 4, 3, width - 7, 69, Surface,
                (40, 0), (1519, 0), (1541, 21), (1541, 69), (0, 69));

            var specs = new (string Name, string Icon, float Width, float LabelWidth)[]
            {
                ("Inventory", "chest", 280, 162), ("Objectives", null, 310, 171),
                ("Map", "map", 204, 63), ("Crew", "person", 242, 84),
                ("Data", "page", 242, 73), ("Settings", "settings", 270, 142)
            };
            var bindings = new SerializedObject(shell);
            var tabs = bindings.FindProperty("tabs");
            tabs.arraySize = specs.Length;
            float x = 0;
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                bool first = i == 0, last = i == specs.Length - 1;
                const float slant = 43;
                float w = spec.Width;
                var fill = last
                    ? Polygon(spec.Name, header, x, 3, w, 69, Color.clear,
                        (slant, 0), (w - 24, 0), (w - 3, 21), (w - 3, 69), (0, 69))
                    : Polygon(spec.Name, header, x, 3, w + slant, 69, Color.clear,
                        (slant, 0), (w + slant, 0), (w, 69), (first ? 5 : 0, 69));
                fill.raycastTarget = true;
                var item = fill.rectTransform;
                var button = item.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.transition = Selectable.Transition.None;

                const float iconSize = 35, gap = 16;
                float groupCenter = (w + (last ? slant / 2 : slant)) / 2;
                float iconX = groupCenter - (iconSize + gap + spec.LabelWidth) / 2;
                var label = Label("Label", item, spec.Name.ToUpperInvariant(), iconX + iconSize + gap, 0,
                    spec.LabelWidth, 69, 27, Muted, true);
                Graphic icon = spec.Icon == null ? ObjectiveMark("Icon", item, iconX, 17, iconSize, Muted)
                    : Icon("Icon", item, spec.Icon, iconX, 17, iconSize, Muted);

                var tab = tabs.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("button").objectReferenceValue = button;
                tab.FindPropertyRelative("fill").objectReferenceValue = fill;
                tab.FindPropertyRelative("icon").objectReferenceValue = icon;
                tab.FindPropertyRelative("label").objectReferenceValue = label;
                x += w;
            }
            bindings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBlankPage(ShipMenuView shell, Transform root)
        {
            var page = Place("BlankPage", root, 0, 0, 0, 0);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = page.offsetMax = Vector2.zero;
            page.SetAsFirstSibling();
            Paint(page, Ink, true);
            var face = Polygon("BodySurface", page, 0, 0, 1816, 802, Surface,
                (23, 0), (1793, 0), (1816, 23), (1816, 779), (1793, 802), (23, 802), (0, 779), (0, 23));
            var body = face.rectTransform;
            body.anchorMin = body.anchorMax = body.pivot = Vector2.one * .5f;
            body.anchoredPosition = new Vector2(0, -13);
            page.gameObject.SetActive(false);
            Assign(shell, "blankPage", page.gameObject);
        }

        private static void StyleJournalPrefab()
        {
            string path = ScrubberEncounterBuilder.JournalFolder + "/TaskJournal.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureCanvas(root, 30020);
                var view = root.GetComponent<TaskJournalView>();
                var panel = (RectTransform)root.transform.Find("Panel");
                panel.GetComponent<Image>().color = Ink;
                foreach (string name in new[] { "Header", "Footer" }) panel.Find(name).gameObject.SetActive(false);
                var body = (RectTransform)panel.Find("Body");
                body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, .5f);
                body.sizeDelta = new Vector2(1816, 802);
                body.anchoredPosition = new Vector2(0, -13);
                Polygon("BodySurface", body, 0, 0, 1816, 802, Surface,
                    (23, 0), (1793, 0), (1816, 23), (1816, 779), (1793, 802), (23, 802), (0, 779), (0, 23))
                    .transform.SetAsFirstSibling();
                var tabs = (RectTransform)panel.Find("Tabs");
                tabs.SetParent(body, false);
                SetRect(tabs, 30, 30, 544, 52);

                var active = Ref<Button>(view, "activeTab");
                var completed = Ref<Button>(view, "completedTab");
                SetRect((RectTransform)active.transform, 0, 0, 214, 49);
                SetRect((RectTransform)completed.transform, 225, 0, 268, 49);
                SetRect(Ref<Image>(view, "activeUnderline").rectTransform, 0, 47, 214, 2);
                SetRect(Ref<Image>(view, "completedUnderline").rectTransform, 225, 47, 268, 2);
                Ref<Image>(view, "activeUnderline").color = Mint;
                Ref<Image>(view, "completedUnderline").color = Mint;

                var list = Ref<ScrollRect>(view, "listScroll");
                SetRect((RectTransform)list.transform, 30, 108, 544, 664);
                list.content.GetComponent<VerticalLayoutGroup>().spacing = 10;
                SetRect((RectTransform)body.Find("ColumnRule"), 605, 0, 2, 802);
                body.Find("ColumnRule").GetComponent<Image>().color = Rim;
                var detail = Ref<ScrollRect>(view, "detailScroll");
                SetRect((RectTransform)detail.transform, 667, 36, 1098, 724);
                var layout = detail.content.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 10;
                layout.padding = new RectOffset(0, 0, 3, 0);

                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                    StyleLabel(label, 25, Text);
                var heading = Ref<TMP_Text>(view, "title");
                StyleLabel(heading, 39, Text, true);
                heading.margin = new Vector4(54, 0, 0, 0);
                heading.GetComponent<LayoutElement>().minHeight = 54;
                var diamond = ObjectiveMark("ObjectiveMark", heading.transform, 0, 9, 36, Objective);
                Assign(view, "titleMark", diamond);

                var location = Ref<TMP_Text>(view, "location");
                StyleLabel(location, 25, Muted);
                var confirmed = Ref<TMP_Text>(view, "confirmed");
                StyleLabel(confirmed, 22, Muted, true);
                var stepLabel = Ref<TMP_Text>(view, "stepLabel");
                StyleLabel(stepLabel, 23, Muted, true);
                var step = Ref<TMP_Text>(view, "stepText");
                StyleLabel(step, 29, Text);
                var surface = Place("StepSurface", detail.content, 0, 0, 1098, 62);
                var highlight = Paint(surface, Selected);
                surface.gameObject.AddComponent<LayoutElement>().minHeight = 62;
                step.rectTransform.SetParent(surface, false);
                step.rectTransform.anchorMin = Vector2.zero;
                step.rectTransform.anchorMax = Vector2.one;
                step.rectTransform.offsetMin = new Vector2(66, 2);
                step.rectTransform.offsetMax = new Vector2(-14, -2);
                step.alignment = TextAlignmentOptions.MidlineLeft;
                var check = Place("CurrentStepMark", surface, 24, 18, 26, 26);
                FrameLines("Check", check, 0, 0, 26, 26, 0, Mint);
                Assign(view, "currentStepMark", check.gameObject);
                Assign(view, "currentStepSurface", surface.gameObject);
                Assign(view, "currentStepBackground", highlight);

                var rules = detail.content.Cast<Transform>().Where(x => x.name == "Divider").ToArray();
                foreach (var rule in rules) rule.GetComponent<Image>().color = Rule;
                var order = new[] { heading.transform, location.transform, rules[0], stepLabel.transform, surface,
                    Ref<TMP_Text>(view, "blocker").transform, rules[1], confirmed.transform,
                    Ref<TMP_Text>(view, "condition").transform, Ref<TMP_Text>(view, "outcome").transform,
                    Ref<TMP_Text>(view, "availability").transform };
                for (int i = 0; i < order.Length; i++) order[i].SetSiblingIndex(i);
                var empty = Ref<TMP_Text>(view, "empty");
                empty.alignment = TextAlignmentOptions.TopLeft;
                empty.color = Muted;

                StyleJournalRow();
                view.ValidateAuthoring();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void StyleJournalRow()
        {
            string path = ScrubberEncounterBuilder.JournalFolder + "/TaskJournalRow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var row = root.GetComponent<TaskJournalRowView>();
                var height = root.GetComponent<LayoutElement>();
                height.minHeight = height.preferredHeight = 69;
                root.GetComponent<VerticalLayoutGroup>().enabled = false;
                row.Button.image.color = new Color(Surface.r, Surface.g, Surface.b, 0);
                row.Button.transition = Selectable.Transition.None;
                var selection = Ref<Image>(row, "selection");
                selection.color = Selected;
                selection.rectTransform.anchorMin = Vector2.zero;
                selection.rectTransform.anchorMax = Vector2.one;
                selection.rectTransform.offsetMin = selection.rectTransform.offsetMax = Vector2.zero;
                selection.transform.SetAsFirstSibling();
                var accent = Box("TaskSelectionAccent", selection.transform, 0, 0, 5, 69, Mint);
                Assign(row, "selectionAccent", accent);
                var mark = ObjectiveMark("TaskMark", root.transform, 25, 20, 29, Muted);
                Assign(row, "taskMark", mark);
                var title = Ref<TMP_Text>(row, "title");
                StyleLabel(title, 27, Text);
                title.rectTransform.anchorMin = Vector2.zero;
                title.rectTransform.anchorMax = Vector2.one;
                title.rectTransform.offsetMin = new Vector2(79, 0);
                title.rectTransform.offsetMax = new Vector2(-16, 0);
                title.alignment = TextAlignmentOptions.MidlineLeft;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 20;
                title.fontSizeMax = 27;
                title.overflowMode = TextOverflowModes.Truncate;
                foreach (string key in new[] { "location", "outcome", "condition", "availability", "outcomeIcon" })
                    Ref<Component>(row, key).gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void StyleInventoryInstance(InventoryScreenView inventory)
        {
            var root = inventory.transform;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            PrefabUtility.RecordPrefabInstancePropertyModifications(scaler);
            var dimmer = root.Find("WorldDimmer").GetComponent<Image>();
            dimmer.color = Ink;
            if (dimmer.transform.Find("ShipMenuBody") == null)
            {
                var backing = Polygon("ShipMenuBody", dimmer.transform, 0, 0, 1816, 802, Surface,
                    (23, 0), (1793, 0), (1816, 23), (1816, 779), (1793, 802), (23, 802), (0, 779), (0, 23));
                var body = backing.rectTransform;
                body.anchorMin = body.anchorMax = body.pivot = Vector2.one * .5f;
                body.anchoredPosition = new Vector2(0, -13);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(dimmer);
            var panel = (RectTransform)root.Find("Panel");
            panel.anchoredPosition = new Vector2(0, 76);
            panel.localScale = new Vector3(1.12f, 1.12f, 1);
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
            if (panel.TryGetComponent<InventoryPanelGraphic>(out var oldSurface))
            {
                oldSurface.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldSurface);
            }
            var face = panel.GetComponent<Image>();
            face.color = Color.clear;
            PrefabUtility.RecordPrefabInstancePropertyModifications(face);
            foreach (string name in new[] { "HeaderFace", "HeaderDivider", "Title", "Back", "TitleUnderline", "Housing", "HousingShadow", "VitalsSurface",
                         "EquipmentDivider", "InspectorDivider" })
            {
                var element = panel.Find(name);
                if (element == null) continue;
                element.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(element.gameObject);
            }
        }

        private static void ConfigureCanvas(GameObject root, int order)
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        private static void StyleLabel(TMP_Text label, float size, Color color, bool bold = false)
        {
            label.font = font;
            label.fontSharedMaterial = material;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.raycastTarget = false;
            label.richText = false;
            if (label.overflowMode == TextOverflowModes.Ellipsis) label.overflowMode = TextOverflowModes.Truncate;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value,
            float x, float y, float width, float height, float size, Color color, bool bold = false)
        {
            var label = Place(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            StyleLabel(label, size, color, bold);
            label.text = value;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Image Icon(string name, Transform parent, string sprite, float x, float y, float size, Color color)
        {
            var icon = Box(name, parent, x, y, size, size, color);
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Icons + sprite + "-64.png");
            icon.preserveAspect = true;
            return icon;
        }

        private static ShipObjectiveMark ObjectiveMark(string name, Transform parent, float x, float y, float size, Color tint)
        {
            var mark = Place(name, parent, x, y, size, size).gameObject.AddComponent<ShipObjectiveMark>();
            mark.color = tint;
            mark.raycastTarget = false;
            return mark;
        }

        private static void FrameLines(string name, Transform parent, float x, float y, float w, float h,
            float cut, Color color, bool top = true)
        {
            var frame = Place(name, parent, x, y, w, h);
            float upperCut = top ? cut : 0;
            var points = new[] { new Vector2(upperCut, 0), new Vector2(w - upperCut, 0), new Vector2(w, upperCut),
                new Vector2(w, h - cut), new Vector2(w - cut, h), new Vector2(cut, h),
                new Vector2(0, h - cut), new Vector2(0, upperCut) };
            for (int i = top ? 0 : 2; i < (top ? 8 : 7); i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                Line("Edge" + i, frame, a.x, a.y, b.x, b.y, color, 3);
            }
        }

        private static void Line(string name, Transform parent, float x, float y, float endX, float endY, Color color, float weight)
        {
            var delta = new Vector2(endX - x, y - endY);
            if (delta.sqrMagnitude < .01f) return;
            var line = Box(name, parent, x, y, delta.magnitude, weight, color).rectTransform;
            line.pivot = new Vector2(0, .5f);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static RectTransform Place(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            SetRect(rect, x, y, w, h);
            return rect;
        }

        private static void SetRect(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Image Box(string name, Transform parent, float x, float y, float w, float h, Color color) =>
            Paint(Place(name, parent, x, y, w, h), color);

        private static Image Paint(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static VitalsHudShape Polygon(string name, Transform parent, float x, float y, float w, float h,
            Color color, params (float x, float y)[] points)
        {
            var graphic = Place(name, parent, x, y, w, h).gameObject.AddComponent<VitalsHudShape>();
            graphic.color = color;
            graphic.raycastTarget = false;
            var serialized = new SerializedObject(graphic);
            var nodes = serialized.FindProperty("points");
            nodes.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
                nodes.GetArrayElementAtIndex(i).vector2Value = new Vector2(points[i].x / w, points[i].y / h);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }

        private static T Ref<T>(UnityEngine.Object owner, string name) where T : UnityEngine.Object =>
            (T)new SerializedObject(owner).FindProperty(name).objectReferenceValue;

        private static void Assign(UnityEngine.Object owner, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(UnityEngine.Object owner, string name, Color value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(name).colorValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
