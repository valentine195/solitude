using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

        [MenuItem("SOLITUDE/Room Authoring/Create Structural Medbay Walkthrough")]
        private static void CreateMedbayFromMenu() => CreateMedbay(FolderPrefix + Guid.NewGuid().ToString("N"));

        [MenuItem("SOLITUDE/Room Authoring/Create Empty Floor Panel Walkthrough")]
        private static void CreatePanelingFromMenu()
        {
            var structuralFolder = FolderPrefix + Guid.NewGuid().ToString("N");
            var path = CreateMedbay(structuralFolder);
            var scene = SceneManager.GetSceneByPath(path);
            var layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>())
                .Single(l => l.name == "Structural Medbay Layout");
            var style = CreateFloorPanelStyle(layout.DefaultStyle, FolderPrefix + Guid.NewGuid().ToString("N"));
            foreach (var room in layout.Spaces) room.Style = style;
            layout.DefaultStyle = style; RoomAuthoringOperations.Rebuild(layout);
            EditorSceneManager.SaveScene(scene);
        }

        // Creates art only. No occupancy, connections, panel settings or existing scene data are modified.
        public static RoomStyleDefinition CreateFloorPanelStyle(RoomStyleDefinition source, string folder)
        {
            if (!Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification") || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepare temporary panel art only in an isolated Edit Mode verification project.");
            if (source == null || !EditorUtility.IsPersistent(source) || folder == null || !folder.StartsWith(FolderPrefix, StringComparison.Ordinal) ||
                !Guid.TryParseExact(folder.Substring(FolderPrefix.Length), "N", out _))
                throw new ArgumentException("Choose a structural style and a fresh GUID-suffixed walkthrough folder.");
            if (Directory.Exists(folder) || File.Exists(folder) || File.Exists(folder + ".meta") || AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Temporary art destination already exists; no assets were changed.");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(folder));
            var texture = new Texture2D(32, 32); texture.SetPixels(Enumerable.Repeat(new Color(.42f, .56f, .66f), 1024).ToArray()); texture.Apply();
            var path = folder + "/FlatFloor.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            AssetDatabase.CreateAsset(tile, folder + "/FlatFloor.asset");
            var style = Object.Instantiate(source); style.name = "Temporary floor panel style";
            var data = new SerializedObject(style); data.FindProperty("styleId").stringValue = Guid.NewGuid().ToString("N"); data.ApplyModifiedPropertiesWithoutUndo();
            style.DisplayName = "Flat floor / panel walkthrough — temporary art"; style.FloorTile = tile;
            style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            AssetDatabase.CreateAsset(style, folder + "/PanelStyle.asset"); AssetDatabase.SaveAssets(); return style;
        }

        public const string CryobaySource = "Assets/_Project/Scenes/Wakeup.unity";
        public const string CryobaySourceDigest = "69961c0bd0d984539e7c3ef9184b5aa93b74e4f459f81502721675620fda2455";
        private static string Hash(byte[] bytes)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

        // Read serialized cells only: never open, modify or instantiate the production scene.
        public static Vector2Int[] ReadCryobayFloor()
        {
            var bytes = File.ReadAllBytes(CryobaySource);
            if (Hash(bytes) != CryobaySourceDigest) throw new InvalidOperationException("Wakeup source digest changed; reassess the approved Cryobay fixture.");
            var blocks = Regex.Split(Encoding.UTF8.GetString(bytes), @"(?m)^--- ");
            var floor = blocks.Single(b => b.StartsWith("!u!1 ", StringComparison.Ordinal) && Regex.IsMatch(b, @"(?m)^  m_Name: Floor\r?$"));
            var identity = Regex.Match(floor, @"^!u!1 &(\d+)").Groups[1].Value;
            var map = blocks.Single(b => b.StartsWith("!u!1839735485 ", StringComparison.Ordinal) &&
                b.Contains("m_GameObject: {fileID: " + identity + "}"));
            var tiles = map.Split(new[] { "  m_Tiles:" }, StringSplitOptions.None)[1].Split(new[] { "  m_AnimatedTiles:" }, StringSplitOptions.None)[0];
            var cells = Regex.Matches(tiles, @"(?m)^  - first: \{x: (-?\d+), y: (-?\d+), z: 0\}")
                .Cast<Match>().Select(m => new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToArray();
            if (cells.Length != 83 || cells.Distinct().Count() != 83) throw new InvalidOperationException("Expected exactly 83 unique Floor cells.");
            return cells.OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
        }

        public static string CreateCryobay(string folder)
        {
            var cells = ReadCryobayFloor(); // Digest failure precedes any scaffold mutation.
            var initial = CreateMedbay(folder);
            var scene = SceneManager.GetSceneByPath(initial);
            var layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>())
                .Single(l => l.name == "Structural Medbay Layout");
            layout.name = "Cryobay source-copy fixture";
            layout.DefaultStyle.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            EditorUtility.SetDirty(layout.DefaultStyle); AssetDatabase.SaveAssets();
            var room = layout.Spaces[0]; room.name = "Cryobay83"; room.SpaceType = LogicalSpaceType.Room;
            room.AuthoringMetadata = "S61 isolated Floor-only copy. No inferred equipment reservations; empty scaffold members are not authored rooms.";
            var path = folder + "/Floor.png"; var texture = new Texture2D(32, 32);
            try { texture.SetPixels(Enumerable.Repeat(new Color(.42f, .56f, .66f), 1024).ToArray()); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var positions = cells.Select(c => new Vector3Int(c.x, c.y, 0)).ToArray();
            room.Occupancy.SetTiles(positions, Enumerable.Repeat(layout.DefaultStyle.FloorTile, positions.Length).ToArray());
            if (!RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true, Seed = 0 }, out var issue))
                throw new InvalidOperationException(issue);
            var result = folder + "/Cryobay83.unity";
            if (!EditorSceneManager.SaveScene(scene, result)) throw new InvalidOperationException("Could not save Cryobay fixture.");
            return result;
        }

        [Serializable] public sealed class CryobaySeedRecord
        {
            public int seed, panels, seamPixels;
            public RoomFloorPanelSettings settings;
            public RectInt[] rectangles;
            public string rectangleSha256, pixelSha256, image, transparentImage;
        }
        [Serializable] public sealed class CryobayGallery
        {
            public string source = CryobaySource, sourceSha256 = CryobaySourceDigest;
            public string fixtureScene, spaceId;
            public string note = "Isolated S61 source-copy fixture, temporary flat art; prepared pixel export, not native camera-motion qualification. No inferred equipment reservations.";
            public Vector2Int[] cells;
            public CryobaySeedRecord[] seeds;
        }

        public static CryobayGallery ExportCryobayGallery(ShipRoomAuthoring room, string destination)
        {
            if (!Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification") || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Export in isolated Edit Mode verification only.");
            var expected = ReadCryobayFloor();
            if (room == null || !new HashSet<Vector2Int>(RoomAuthoringOperations.ReadCells(room)).SetEquals(expected) ||
                room.FloorReservations.Count != 0 || room.Layout == null || room.Layout.Connections.Count != 0 ||
                RoomAuthoringValidator.LayoutOffset(room.Layout, room) != Vector2Int.zero)
                throw new InvalidOperationException("Use the exact unreserved zero-offset Cryobay fixture.");
            var directory = Path.GetFullPath(destination); var assets = Path.GetFullPath(UnityEngine.Application.dataPath);
            if (directory == assets || directory.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                Directory.Exists(directory) || File.Exists(directory)) throw new InvalidOperationException("Choose a fresh gallery directory outside Assets.");
            if (RoomAuthoringAssetRefresh.HasPendingPanelDraft(room.Layout) || RoomAuthoringOperations.HasActiveEdit)
                throw new InvalidOperationException("Finish or cancel authoring before exporting the gallery.");
            Directory.CreateDirectory(directory);
            var result = new CryobayGallery { fixtureScene = room.gameObject.scene.path, spaceId = room.RoomId, cells = expected };
            var records = new List<CryobaySeedRecord>();
            for (var seed = 0; seed < 10; seed++)
            {
                var settings = new RoomFloorPanelSettings { Enabled = true, Seed = seed };
                if (!RoomAuthoringOperations.SetFloorPanelSettings(room, settings, out var issue)) throw new InvalidOperationException(issue);
                var partition = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room));
                if (!partition.Success) throw new InvalidOperationException("Cryobay partition failed.");
                var canonical = string.Join(";", partition.Rectangles.Select(b => b.x + "," + b.y + "," + b.width + "," + b.height));
                var renderer = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Generated Floor Panels");
                if (renderer == null) throw new InvalidOperationException("Cryobay prepared panels are missing: " + room.GenerationDiagnostic);
                var prepared = AssetDatabase.LoadAssetAtPath<RoomFloorPanelOutput>(AssetDatabase.GetAssetPath(renderer.sprite));
                if (!RoomFloorPanelAssetStore.IsVerified(prepared)) throw new InvalidOperationException("Unverified prepared Cryobay output.");
                const int width = 352, height = 288; var pixels = new Color32[width * height];
                foreach (var chunk in prepared.Chunks)
                {
                    var buffer = chunk.Texture.GetPixels32();
                    for (var y = 0; y < chunk.Texture.height; y++) for (var x = 0; x < chunk.Texture.width; x++)
                    {
                        var c = buffer[y * chunk.Texture.width + x]; if (c.a == 0) continue;
                        var px = chunk.Origin.x + x + 224; var py = chunk.Origin.y + y + 160;
                        if (px < 0 || px >= width || py < 0 || py >= height) throw new InvalidOperationException("Prepared pixel outside fixture bounds.");
                        var index = py * width + px;
                        if (pixels[index].a != 0) throw new InvalidOperationException("Overlapping prepared chunks.");
                        pixels[index] = c;
                    }
                }
                var rgba = pixels.SelectMany(c => new[] { c.r, c.g, c.b, c.a }).ToArray();
                var record = new CryobaySeedRecord { seed = seed, panels = partition.Rectangles.Count, settings = settings,
                    rectangles = partition.Rectangles.ToArray(), rectangleSha256 = Hash(Encoding.UTF8.GetBytes(canonical)), pixelSha256 = Hash(rgba),
                    seamPixels = pixels.Count(c => c.a != 0), image = "Seed-" + seed + ".png", transparentImage = "Seed-" + seed + "-seams.png" };
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    image.SetPixels32(pixels); image.Apply(); File.WriteAllBytes(Path.Combine(directory, record.transparentImage), image.EncodeToPNG());
                    var floor = new Color32[width * height];
                    foreach (var cell in expected)
                        for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
                        { var index = (cell.y * 32 + y + 160) * width + cell.x * 32 + x + 224; floor[index] = pixels[index].a == 0 ? (Color32)new Color(.42f, .56f, .66f) : pixels[index]; }
                    image.SetPixels32(floor); image.Apply(); File.WriteAllBytes(Path.Combine(directory, record.image), image.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(image); }
                records.Add(record);
            }
            result.seeds = records.ToArray();
            File.WriteAllText(Path.Combine(directory, "CryobayPaneling.json"), JsonUtility.ToJson(result, true));
            if (!EditorSceneManager.SaveScene(room.gameObject.scene)) throw new InvalidOperationException("Could not save gallery fixture final seed.");
            return result;
        }

        public static string CreateMedbay(string folder)
        {
            // Create enforces fresh destination, isolated runner and preservation of existing scenes.
            var initial = Create(folder);
            var scene = SceneManager.GetSceneByPath(initial);
            var layout = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipLayoutAuthoring>())
                .Single(l => l.name == "Walkthrough Layout");
            Sprite Ink(string name, int height, RectInt ink, Color color)
            {
                var texture = new Texture2D(32, height);
                texture.SetPixels(Enumerable.Repeat(Color.clear, 32 * height).ToArray());
                for (var y = ink.y; y < ink.yMax; y++) for (var x = ink.x; x < ink.xMax; x++) texture.SetPixel(x, y, color);
                texture.Apply(); var path = folder + "/" + name + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            var horizontal = Ink("MedbayHorizontal", 32, new RectInt(0, 0, 32, 1), Color.green);
            var vertical = Ink("MedbayVertical", 32, new RectInt(0, 0, 1, 32), Color.green);
            var north = Ink("MedbayNorth", 64, new RectInt(0, 0, 32, 1), Color.cyan);
            var dot = Ink("MedbayVertex", 32, new RectInt(0, 0, 1, 1), Color.yellow);
            RoomStyleDefinition.Module Module(Sprite sprite, RectInt ink, Vector2 anchor) => new RoomStyleDefinition.Module
            { Sprite = sprite, CanvasPixels = Vector2Int.RoundToInt(sprite.rect.size), RepeatPixels = 32,
                AnchorPixels = anchor, HasFootprint = true, FootprintsPixels = new[] { ink } };
            var style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
            style.DisplayName = "Medbay structural placeholder — production art not qualified";
            style.FloorTile = layout.DefaultStyle.FloorTile; style.FloorEdgeInsetPixels = 1;
            var line = new RectInt(0, 0, 32, 1); var point = new RectInt(0, 0, 1, 1);
            style.NorthWalls = new[] { Module(north, line, Vector2.zero) };
            style.WestCap = Module(vertical, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            style.EastCap = Module(vertical, new RectInt(0, 0, 1, 32), new Vector2(.5f, 0));
            style.SouthCap = Module(horizontal, line, new Vector2(0, .5f));
            style.Northwest = Module(dot, point, new Vector2(1, 0)); style.Northeast = Module(dot, point, Vector2.zero);
            style.Southwest = Module(dot, point, Vector2.one); style.Southeast = Module(dot, point, new Vector2(0, 1));
            style.ConcaveNorthwest = Module(dot, point, new Vector2(1, 0)); style.ConcaveNortheast = Module(dot, point, Vector2.zero);
            style.ConcaveSouthwest = Module(dot, point, Vector2.one); style.ConcaveSoutheast = Module(dot, point, new Vector2(0, 1));
            style.JunctionModules = Enum.GetValues(typeof(LayoutJunctionRole)).Cast<LayoutJunctionRole>().Select(role =>
                new RoomStyleDefinition.JunctionModule { Role = role, Artwork = Module(dot, point, new Vector2(.5f, .5f)) }).ToArray();
            AssetDatabase.CreateAsset(style, folder + "/MedbayStyle.asset");
            layout.name = "Structural Medbay Layout"; layout.DefaultStyle = style;
            var corridor = layout.Spaces[0]; corridor.name = "Medbay Corridor"; corridor.SpaceType = LogicalSpaceType.Corridor; corridor.Style = style;
            corridor.AuthoringMetadata = "Paint the L bend and T branch in this single corridor map.";
            var treatment = RoomAuthoringCreation.CreateSpace(layout); treatment.name = "Treatment Room";
            var supply = RoomAuthoringCreation.CreateSpace(layout); supply.name = "Supply Room";
            RoomAuthoringOperations.Rebuild(layout); AssetDatabase.SaveAssets();
            var pathToScene = folder + "/MedbayStructural.unity";
            if (!EditorSceneManager.SaveScene(scene, pathToScene)) throw new InvalidOperationException("Could not save Medbay scaffold.");
            if (!UnityEngine.Application.isBatchMode)
            {
                Selection.activeGameObject = layout.gameObject;
                var view = EditorWindow.GetWindow<SceneView>(); view.in2DMode = true;
                view.Frame(new Bounds(new Vector3(9, 5, 0), new Vector3(25, 18, 1)), true);
            }
            return pathToScene;
        }

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
