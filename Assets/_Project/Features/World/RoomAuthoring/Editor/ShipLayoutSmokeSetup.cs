using System;
using System.IO;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class ShipLayoutSmokeSetup
    {
        public const string FolderPrefix = "Assets/_Project/Tests/ShipLayoutSmoke";
        public const string MainSpaceName = "Walkthrough Space";

        [InitializeOnLoadMethod]
        private static void ProtectVerificationStartup()
        {
            if (Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"))
                SessionState.SetBool("SolitudeStartRebuild29", true);
        }

        [MenuItem("SOLITUDE/Room Authoring/Create Layout Walkthrough")]
        private static void CreateFromMenu() => Create(FolderPrefix + Guid.NewGuid().ToString("N"));

        public static string Create(string folder)
        {
            if (!Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification") ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Create the walkthrough in an isolated Edit Mode verification project.");
            if (folder == null || !folder.StartsWith(FolderPrefix, StringComparison.Ordinal) ||
                !Guid.TryParseExact(folder.Substring(FolderPrefix.Length), "N", out _))
                throw new ArgumentException("Use a fresh ShipLayoutSmoke folder with a unique GUID suffix.", nameof(folder));
            if (Directory.Exists(folder) || File.Exists(folder) || File.Exists(folder + ".meta") || AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Walkthrough destination already exists; no assets or scenes were changed.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isLoaded && string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save or close the untitled scene before creating an additive walkthrough; no assets or scenes were changed.");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(folder));
            Sprite Import(string name, int width, int height, Color color, bool grid = false)
            {
                var texture = new Texture2D(width, height);
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
                    texture.SetPixel(x, y, grid && (x == 0 || y == 0) ? color * .65f : color);
                texture.Apply();
                var path = folder + "/" + name + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var floorSprite = Import("Floor", 32, 32, new Color(.42f, .56f, .66f), true);
            var north = Import("NorthFace", 32, 64, new Color(.22f, .36f, .58f));
            var cap = Import("Cap", 32, 32, new Color(.25f, .65f, .5f));
            var convex = Import("Convex", 32, 32, new Color(.85f, .65f, .25f));
            var concave = Import("Concave", 32, 32, new Color(.7f, .4f, .8f));
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = floorSprite;
            AssetDatabase.CreateAsset(tile, folder + "/Floor.asset");
            RoomStyleDefinition.Module Module(Sprite sprite, int anchorX = 0, int anchorY = 0) => new RoomStyleDefinition.Module
                { Sprite = sprite, CanvasPixels = Vector2Int.RoundToInt(sprite.rect.size), RepeatPixels = 32,
                    AnchorPixels = new Vector2(anchorX, anchorY) };
            var style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
            style.DisplayName = "Synthetic walkthrough — not production art";
            style.FloorTile = tile; style.NorthWalls = new[] { Module(north) };
            // Boundary coordinates are cell edges; keep opaque caps outside authored floor.
            style.WestCap = Module(cap, 32); style.EastCap = Module(cap); style.SouthCap = Module(cap, 0, 32);
            style.Northwest = Module(convex, 32); style.Northeast = Module(convex);
            style.Southwest = Module(convex, 32, 32); style.Southeast = Module(convex, 0, 32);
            style.ConcaveNorthwest = Module(concave, 32); style.ConcaveNortheast = Module(concave);
            style.ConcaveSouthwest = Module(concave, 32, 32); style.ConcaveSoutheast = Module(concave, 0, 32);
            AssetDatabase.CreateAsset(style, folder + "/Style.asset"); AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var layout = RoomAuthoringCreation.CreateLayout(scene, style);
            layout.name = "Walkthrough Layout";
            var room = layout.Spaces[0]; room.name = MainSpaceName;
            RoomAuthoringOperations.Rebuild(layout);
            var legacy = RoomAuthoringCreation.Create(scene, style); legacy.name = "Legacy Sentinel Room";
            legacy.transform.position = new Vector3(-20, -20, 0);
            RoomAuthoringOperations.PaintBox(legacy, new BoundsInt(0, 0, 0, 2, 2, 1), tile);
            var neighbor = RoomAuthoringCreation.CreateLayout(scene, style); neighbor.name = "Other Layout Sentinel";
            neighbor.transform.position = new Vector3(20, -20, 0);
            RoomAuthoringOperations.PaintBox(neighbor.Spaces[0], new BoundsInt(0, 0, 0, 2, 2, 1), tile);
            var sentinel = new GameObject("Generated Architecture");
            SceneManager.MoveGameObjectToScene(sentinel, scene); sentinel.transform.position = new Vector3(24, 12, 0);
            var pathToScene = folder + "/UnifiedSingleSpace.unity";
            if (!EditorSceneManager.SaveScene(scene, pathToScene)) throw new InvalidOperationException("Could not save disposable walkthrough.");
            SceneManager.SetActiveScene(scene);
            if (!UnityEngine.Application.isBatchMode)
            {
                Selection.activeGameObject = layout.gameObject;
                var view = EditorWindow.GetWindow<SceneView>(); view.in2DMode = true;
                view.Frame(new Bounds(new Vector3(6, 3, 0), new Vector3(18, 12, 1)), true);
            }
            return pathToScene;
        }
    }
}
