using System.Linq;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace SOLITUDE.Editor
{
    public static class PhaseFiveMigration
    {
        private static void Set(Object target, string field, Object value)
        { var obj = new SerializedObject(target); obj.FindProperty(field).objectReferenceValue = value; obj.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static GameObject UI(string name, Transform parent, params System.Type[] components)
        { var go = new GameObject(name, components); go.transform.SetParent(parent, false); return go; }
        [MenuItem("SOLITUDE/Migrations/Configure Wakeup Validation and Recovery")]
        public static void ConfigureWakeup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Wakeup.unity" || EditorApplication.isPlaying) throw new System.InvalidOperationException("Open Wakeup in Edit Mode.");
            foreach (var id in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SOLITUDE.SaveLoad.SaveableId>(true)))
            {
                id.AssignMissingSceneIdentity();
                if (PrefabUtility.IsPartOfPrefabInstance(id)) PrefabUtility.RecordPrefabInstancePropertyModifications(id);
                EditorUtility.SetDirty(id);
            }
            var scope = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneBindings>(true)).Single();
            var notice = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RecoveryNoticeView>(true)).SingleOrDefault();
            if (notice == null)
            {
                var canvasGO = UI("RecoveryNoticeCanvas", null, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
                var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
                var banner = UI("RecoveryNotice", canvas.transform, typeof(RectTransform), typeof(Image), typeof(RecoveryNoticeView));
                var rect = banner.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -18); rect.sizeDelta = new Vector2(760, 100);
                banner.GetComponent<Image>().color = new Color(.08f, .14f, .18f, .97f);
                var textGO = UI("Message", banner.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
                var textRect = textGO.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(18, 12); textRect.offsetMax = new Vector2(-120, -12);
                var text = textGO.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = 22; text.color = Color.white; text.raycastTarget = false;
                var buttonGO = UI("Dismiss", banner.transform, typeof(RectTransform), typeof(Image), typeof(Button));
                var buttonRect = buttonGO.GetComponent<RectTransform>(); buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1, .5f); buttonRect.anchoredPosition = new Vector2(-62, 0); buttonRect.sizeDelta = new Vector2(92, 44);
                buttonGO.GetComponent<Image>().color = new Color(.2f, .32f, .38f);
                var labelGO = UI("Label", buttonGO.transform, typeof(RectTransform), typeof(TextMeshProUGUI)); var labelRect = labelGO.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                var label = labelGO.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = "Dismiss"; label.fontSize = 18; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                notice = banner.GetComponent<RecoveryNoticeView>(); Set(notice, "root", banner); Set(notice, "message", text); Set(notice, "dismiss", buttonGO.GetComponent<Button>());
            }
            Set(scope, "recoveryNotice", notice); scope.Validate();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) }.Concat(EditorBuildSettings.scenes.Where(s => s.path != scene.path && !s.path.Contains("SOLITUDE_Expedition") && AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) != null)).ToArray();
            var manifest = AssetDatabase.LoadAssetAtPath<WakeupBuildManifest>(WakeupBuildManifest.AssetPath);
            if (manifest == null) { manifest = ScriptableObject.CreateInstance<WakeupBuildManifest>(); AssetDatabase.CreateAsset(manifest, WakeupBuildManifest.AssetPath); }
            manifest.scenes = new[] { scene.path }; manifest.runtimeConfig = AssetDatabase.LoadAssetAtPath<GameRuntimeConfig>("Assets/_Project/Core/Composition/GameRuntimeConfig.asset"); EditorUtility.SetDirty(manifest);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[PhaseFiveMigration] Wakeup manifest, recovery banner and build entry saved.");
        }
    }
}
