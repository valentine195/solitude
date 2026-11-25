using System;
using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Player;
using SOLITUDE.Player.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using SOLITUDE.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SOLITUDE.Editor
{
    public static class ExplorationVitalsHudAuthoring
    {
        private const string PrefabPath = "Assets/_Project/Features/Player/Vitals/Views/ExplorationVitalsHud.prefab";
        private const string FontPath = "Assets/_Project/UI/Fonts/Exo_2/static/Exo2-ExtraBold SDF.asset";
        private static readonly Color Text = new Color32(231, 235, 232, 255);
        private static readonly Color Health = new Color32(131, 216, 164, 255);
        private static readonly Color Oxygen = new Color32(168, 185, 242, 255);

        [MenuItem("SOLITUDE/Vitals/Install Scrubber Exploration HUD")]
        public static void InstallScrubber()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Install the vitals HUD in Edit mode.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScrubberEncounterBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScrubberEncounterBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("Save existing Scrubber scene edits before installing the HUD.");
                ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the Scrubber HUD.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        internal static void ConfigureScene(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = AuthorPrefab();

            var roots = scene.GetRootGameObjects();
            var controller = roots.SelectMany(root => root.GetComponentsInChildren<PlayerVitalsController>(true)).Single();
            var bindings = roots.SelectMany(root => root.GetComponentsInChildren<SceneBindings>(true)).Single();
            var canvas = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).Single(c => c.name == "HUDCanvas");
            var view = canvas.GetComponentInChildren<ExplorationVitalsHud>(true);
            if (view == null)
                view = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform)).GetComponent<ExplorationVitalsHud>();
            SetReference(view, "controller", controller);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);

            var old = canvas.transform.Find("Vitals");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
            }

            var composition = bindings.GetComponent<PlayerVitalsComposition>();
            if (composition == null || composition.Controller != controller)
                PlayerVitalsAuthoring.ConfigureComposition(bindings, controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static GameObject AuthorPrefab()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("The Exo 2 UI font is missing.");
            var root = Place("ExplorationVitalsHud", null, 24, 24, 364, 160);
            try
            {
                var view = root.gameObject.AddComponent<ExplorationVitalsHud>();
                SetReference(view, "display", Group(root, 0));

                var health = Place("Health", root, 0, 0, 364, 82);
                Shape(health, ShipboardUiColors.PanelInk, (0, 0), (342, 0), (364, 22), (364, 34), (340, 82), (0, 82));
                Shape(Place("TopRule", health, 0, 0, 342, 1), ShipboardUiColors.PanelRim, (0, 0), (342, 0), (342, 1), (0, 1));
                Shape(Place("ClippedCornerRule", health, 342, 0, 22, 22), ShipboardUiColors.PanelRim,
                    (0, 0), (1.4f, 0), (22, 20.6f), (22, 22));
                Paint(Place("HealthSymbolHorizontal", health, 16, 19, 20, 8), Health);
                Paint(Place("HealthSymbolVertical", health, 22, 13, 8, 20), Health);
                Label(Place("HealthLabel", health, 48, 2.5f, 160, 42), font, "HEALTH", 30, Text);
                SetReference(view, "health", Meter(Place("HealthMeter", health, 16, 45, 324, 22), Health,
                    new Color32(38, 63, 54, 255)));

                var oxygen = Place("Oxygen", root, 0, 82, 280, 44);
                Shape(oxygen, ShipboardUiColors.PanelInk, (0, 0), (280, 0), (280, 6), (261, 44), (0, 44));
                Label(Place("OxygenLabel", oxygen, 16, 3, 42, 36), font, "O₂", 25, Text);
                SetReference(view, "oxygen", Meter(Place("OxygenMeter", oxygen, 65, 12, 192, 16), Oxygen,
                    new Color32(48, 58, 81, 255)));

                var hunger = Place("HungerWarning", root, 0, 126, 185, 34);
                SetReference(view, "hunger", Group(hunger, 0));
                Shape(hunger, ShipboardUiColors.Amber, (0, 0), (185, 0), (168, 34), (0, 34));
                var warning = Place("WarningSymbol", hunger, 14, 8, 20, 18);
                Shape(Place("Triangle", warning, 0, 0, 20, 18), ShipboardUiColors.PanelInk, (0, 18), (10, 0), (20, 18));
                Shape(Place("TriangleInset", warning, 0, 0, 20, 18), ShipboardUiColors.Amber, (3.3f, 16), (10, 4), (16.7f, 16));
                Paint(Place("ExclamationStem", warning, 9, 7, 2, 5), ShipboardUiColors.PanelInk);
                Paint(Place("ExclamationDot", warning, 9, 13, 2, 2), ShipboardUiColors.PanelInk);
                Label(Place("HungerLabel", hunger, 43, -1.5f, 116, 36), font, "HUNGRY", 21, ShipboardUiColors.PanelInk);
                return PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
        }

        private static RectTransform Place(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static CanvasGroup Group(RectTransform rect, float alpha)
        {
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Shape(RectTransform rect, Color tint, params (float x, float y)[] outline)
        {
            var graphic = rect.gameObject.AddComponent<VitalsHudShape>();
            graphic.color = tint;
            graphic.raycastTarget = false;
            var serialized = new SerializedObject(graphic);
            var points = serialized.FindProperty("points");
            points.arraySize = outline.Length;
            for (int i = 0; i < outline.Length; i++)
                points.GetArrayElementAtIndex(i).vector2Value =
                    new Vector2(outline[i].x / rect.sizeDelta.x, outline[i].y / rect.sizeDelta.y);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VitalsHudMeter Meter(RectTransform rect, Color fill, Color track)
        {
            var graphic = rect.gameObject.AddComponent<VitalsHudMeter>();
            graphic.color = fill;
            graphic.raycastTarget = false;
            var serialized = new SerializedObject(graphic);
            serialized.FindProperty("trackColor").colorValue = track;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return graphic;
        }

        private static void Paint(RectTransform rect, Color tint)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = tint;
            image.raycastTarget = false;
        }

        private static void Label(RectTransform rect, TMP_FontAsset font, string value, float size, Color tint)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = tint;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
