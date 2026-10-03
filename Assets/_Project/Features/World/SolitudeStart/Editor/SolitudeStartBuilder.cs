using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Systems;
using SOLITUDE.Core.UI;

namespace SOLITUDE.World.SolitudeStart.Editor
{
    public static class SolitudeStartBuilder
    {
        private const string Root = "Assets/_Project/Features/World/SolitudeStart";
        private const string Art = Root + "/Art";
        private const string Generated = Root + "/Generated";
        private const string AtlasPath = Art + "/solitude-atlas-32.png";
        private const string DoorPath = Art + "/door-opening-spritesheet.png";

        [MenuItem("SOLITUDE/Build Starting Room")]
        public static void Build()
        {
            ValidateLayout();
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory(Root + "/Scenes");
            ConfigureTexture(AtlasPath, 32, 8, 4, false);
            ConfigureTexture(DoorPath, 64, 9, 1, true);
            var atlas = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);
            var doorSprites = AssetDatabase.LoadAllAssetsAtPath(DoorPath).OfType<Sprite>()
                .OrderBy(s => s.name).ToArray();
            if (doorSprites.Length != 9) throw new InvalidOperationException("Expected nine 64px door poses.");
            Tile[] tiles = new Tile[SolitudeGeneratedLayout.Names.Length];
            for (int i = 1; i < tiles.Length; i++)
            {
                if (!atlas.TryGetValue($"atlas_{i:00}", out Sprite sprite))
                    throw new InvalidOperationException($"Missing atlas sprite {i}.");
                string path = $"{Generated}/Tile_{SolitudeGeneratedLayout.Names[i]}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                tiles[i] = tile;
            }
            string rulePath = Generated + "/RoomBoundaryRule.asset";
            var rule = AssetDatabase.LoadAssetAtPath<SolitudeBoundaryRuleTile>(rulePath);
            if (rule == null) { rule = ScriptableObject.CreateInstance<SolitudeBoundaryRuleTile>(); AssetDatabase.CreateAsset(rule, rulePath); }
            rule.interior = new RectInt(1, 1, 8, 6);
            rule.west = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("west")].sprite;
            rule.east = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("east")].sprite;
            rule.south = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("south")].sprite;
            rule.northWest = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("northwest")].sprite;
            rule.northEast = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("northeast")].sprite;
            rule.southWest = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("southwest")].sprite;
            rule.southEast = tiles[SolitudeGeneratedLayout.Names.ToList().IndexOf("southeast")].sprite;
            EditorUtility.SetDirty(rule);
            AssetDatabase.SaveAssets();

            // Build additively so the currently open scene and unsaved edits survive.
            Scene previousScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var gridGO = new GameObject("SOLITUDE Room Grid", typeof(Grid));
            var grid = gridGO.GetComponent<Grid>(); grid.cellSize = Vector3.one;
            Tilemap floor = NewMap(gridGO.transform, "Floor", 0);
            Tilemap north = NewMap(gridGO.transform, "North Wall", 1);
            Tilemap perimeter = NewMap(gridGO.transform, "Rule Boundaries", 2);
            for (int sourceRow = 0; sourceRow < 9; sourceRow++)
            for (int x = 0; x < 10; x++)
            {
                int id = SolitudeGeneratedLayout.Cells[sourceRow, x];
                int y = 8 - sourceRow;
                if (x >= 4 && x <= 5 && y >= 7) continue; // One animated door, no closed tile behind it.
                if (x >= 1 && x <= 8 && y >= 1 && y <= 6) floor.SetTile(new Vector3Int(x, y), tiles[id]);
                else if (x >= 1 && x <= 8 && y >= 7) north.SetTile(new Vector3Int(x, y), tiles[id]);
                else perimeter.SetTile(new Vector3Int(x, y), rule);
            }
            perimeter.RefreshAllTiles();

            GameObject collision = new GameObject("Room Collision");
            AddBox(collision.transform, "West", new Vector2(0.875f, 4f), new Vector2(0.25f, 6f));
            AddBox(collision.transform, "East", new Vector2(9.125f, 4f), new Vector2(0.25f, 6f));
            AddBox(collision.transform, "South", new Vector2(5f, 0.875f), new Vector2(8.5f, 0.25f));
            AddBox(collision.transform, "North Left", new Vector2(2.5f, 7.65f), new Vector2(3f, 1.3f));
            AddBox(collision.transform, "North Right", new Vector2(7.5f, 7.65f), new Vector2(3f, 1.3f));
            AddBox(collision.transform, "Unbuilt North Exit Stop", new Vector2(5f, 9.15f), new Vector2(2f, 0.3f));

            GameObject door = new GameObject("SOLITUDE Sliding Door");
            door.transform.position = new Vector3(5, 7, -0.05f);
            SpriteRenderer sr = door.AddComponent<SpriteRenderer>(); sr.sprite = doorSprites[0]; sr.sortingOrder = 5;
            var controller = door.AddComponent<SolitudeSlidingDoor>();
            var blocker = door.AddComponent<BoxCollider2D>(); blocker.offset = new Vector2(0, 0.55f); blocker.size = new Vector2(1.35f, 1.1f);
            controller.Configure(sr, doorSprites, blocker);
            var proximity = new GameObject("Interaction Range"); proximity.transform.SetParent(door.transform, false);
            var sensor = proximity.AddComponent<BoxCollider2D>(); sensor.isTrigger = true;
            sensor.offset = new Vector2(0, -0.35f); sensor.size = new Vector2(2.2f, 2.1f);
            proximity.AddComponent<SOLITUDE.Core.Interaction.InteractableProximitySensor>();
            var completion = new GameObject("Door Passage Completion"); completion.transform.position = new Vector3(5, 7.35f, 0);
            var trigger = completion.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.1f, 0.35f);
            completion.AddComponent<SolitudeStartCompletion>().Configure(controller);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Player/PF_Player.prefab");
            if (playerPrefab == null) throw new InvalidOperationException("Existing PF_Player prefab missing.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab); player.transform.position = new Vector3(5, 2.5f, -0.1f);
            GameObject routerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Core/Input/SYS_InputRouter.prefab");
            if (routerPrefab == null) throw new InvalidOperationException("Existing input router prefab missing.");
            PrefabUtility.InstantiatePrefab(routerPrefab);
            GameObject systems = new GameObject("SOLITUDE Systems");
            var time = systems.AddComponent<TimeSystem>();
            var gm = systems.AddComponent<GameManager>();
            var gmObject = new SerializedObject(gm); gmObject.FindProperty("timeSystem").objectReferenceValue = time; gmObject.ApplyModifiedPropertiesWithoutUndo();
            GameObject cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.tag = "MainCamera"; cameraGO.transform.position = new Vector3(5, 4.5f, -10);
            var camera = cameraGO.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.25f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 43, 53, 255);
            var pixel = cameraGO.AddComponent<PixelPerfectCamera>(); pixel.assetsPPU = 32; pixel.refResolutionX = 320; pixel.refResolutionY = 288;
            var lightGO = new GameObject("Global 2D Light", typeof(Light2D)); lightGO.GetComponent<Light2D>().lightType = Light2D.LightType.Global;
            AddInteractionUI();
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/SOLITUDE_Start.unity");
            AssetDatabase.SaveAssets();
            if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("SOLITUDE starting room built: 48 floor cells, rule boundary, 64px door, player and systems.");
        }
        private static void ValidateLayout()
        {
            if (SolitudeGeneratedLayout.Cells.GetLength(0) != 9 ||
                SolitudeGeneratedLayout.Cells.GetLength(1) != 10)
                throw new InvalidOperationException("Room layout must be exactly 10 by 9 cells.");
            int floorCount = 0;
            for (int row = 0; row < 9; row++)
            for (int col = 0; col < 10; col++)
            {
                int id = SolitudeGeneratedLayout.Cells[row, col];
                if (id < 1 || id >= SolitudeGeneratedLayout.Names.Length)
                    throw new InvalidOperationException($"Invalid tile id {id} at ({col},{row}).");
                if (row >= 2 && row <= 7 && col >= 1 && col <= 8)
                {
                    floorCount++;
                    if (id < 1 || id > 7)
                        throw new InvalidOperationException($"Non-floor tile in interior at ({col},{row}).");
                }
                if ((col == 0 && row > 0 && row < 8 && id != 22) ||
                    (col == 9 && row > 0 && row < 8 && id != 23))
                    throw new InvalidOperationException("Side wall is not a straight vertical run.");
            }
            if (floorCount != 48) throw new InvalidOperationException("Floor must have six rows of eight cells.");
            for (int row = 0; row < 2; row++)
            for (int col = 4; col <= 5; col++)
                if (SolitudeGeneratedLayout.Cells[row, col] != 14 + row * 2 + col - 4)
                    throw new InvalidOperationException("The four door quadrants are not in a centered 2x2 footprint.");
        }
        private static void ConfigureTexture(string path, int size, int columns, int rows, bool bottomPivot)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            SpriteMetaData[] slices = new SpriteMetaData[bottomPivot ? columns * rows : SolitudeGeneratedLayout.Names.Length];
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int id = row * columns + col;
                if (id >= slices.Length) continue;
                slices[id] = new SpriteMetaData { name = (bottomPivot ? "door_" : "atlas_") + id.ToString("00"),
                    rect = new Rect(col * size, (rows - 1 - row) * size, size, size),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = bottomPivot ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f) };
            }
#pragma warning disable 0618
            importer.spritesheet = slices;
#pragma warning restore 0618
            importer.SaveAndReimport();
        }
        private static Tilemap NewMap(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }
        private static void AddInteractionUI()
        {
            var canvasGO = new GameObject("SOLITUDE Interaction UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(320, 288);
            var promptView = new GameObject("Interaction Prompt", typeof(RectTransform), typeof(InteractionPromptView));
            promptView.transform.SetParent(canvasGO.transform, false);
            var promptLabel = AddLabel(promptView.transform, "Prompt Text", 24f, new Vector2(0, 28));
            SetReference(promptView.GetComponent<InteractionPromptView>(), "root", promptLabel.gameObject);
            SetReference(promptView.GetComponent<InteractionPromptView>(), "promptText", promptLabel);
            promptLabel.gameObject.SetActive(false);
            var feedbackView = new GameObject("Interaction Feedback", typeof(RectTransform), typeof(InteractionFeedbackView));
            feedbackView.transform.SetParent(canvasGO.transform, false);
            var feedbackLabel = AddLabel(feedbackView.transform, "Feedback Text", 18f, new Vector2(0, 54));
            SetReference(feedbackView.GetComponent<InteractionFeedbackView>(), "root", feedbackLabel.gameObject);
            SetReference(feedbackView.GetComponent<InteractionFeedbackView>(), "label", feedbackLabel);
            feedbackLabel.gameObject.SetActive(false);
            var controller = canvasGO.AddComponent<UIController>();
            SetReference(controller, "interactionPrompt", promptView.GetComponent<InteractionPromptView>());
            SetReference(controller, "feedbackView", feedbackView.GetComponent<InteractionFeedbackView>());
        }
        private static TextMeshProUGUI AddLabel(Transform parent, string name, float size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f); rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 40f);
            var label = go.GetComponent<TextMeshProUGUI>(); label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(235, 239, 225, 255);
            label.raycastTarget = false;
            return label;
        }
        private static void SetReference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void AddBox(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name, typeof(BoxCollider2D)); go.transform.SetParent(parent, false);
            go.transform.position = center; go.GetComponent<BoxCollider2D>().size = size;
        }
    }
}
