using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutArtCompatibilityTests
    {
        private string folder;
        [SetUp] public void SetUp()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            folder = RoomPersistenceTests.CreateAssets();
        }
        [TearDown] public void TearDown()
        {
            Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(folder);
        }
        private Sprite Import(string name, int width, int height, int ppu)
        {
            var texture = new Texture2D(width, height);
            texture.SetPixels(Enumerable.Repeat(Color.white, width * height).ToArray()); texture.Apply();
            var path = folder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private RoomStyleDefinition Configure()
        {
            var style = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset");
            ((Tile)style.FloorTile).sprite = Import("floor64", 64, 64, 64);
            EditorUtility.SetDirty(style.FloorTile);
            style.FloorCanvasPixels = new Vector2Int(64, 64); style.FloorPixelsPerUnit = 64;
            style.NorthWalls = new[] { new RoomStyleDefinition.Module {
                Sprite = Import("north128", 128, 256, 64), CanvasPixels = new Vector2Int(128, 256),
                PixelsPerUnit = 64, RepeatPixels = 128, Orientation = RoomStyleDefinition.ModuleOrientation.North } };
            var material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, folder + "/SpriteMaterial.mat");
            style.Rendering = new RoomStyleDefinition.RenderingConfiguration {
                SortingLayerName = "Wall_Panels", FloorOrder = -4, DetailOrder = -2, ArchitectureOrder = 3,
                FloorMaterial = material, ArchitectureMaterial = material };
            EditorUtility.SetDirty(style); AssetDatabase.SaveAssets(); return style;
        }
        private static void AssertOutput(ShipRoomAuthoring room)
        {
            var map = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(new Vector3(.5f, .5f, 0), map.tileAnchor);
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.IsFalse(map.HasTile(new Vector3Int(8, 0, 0)));
            Assert.AreEqual(Vector3.one, map.transform.localScale);
            Assert.AreEqual(Vector2.one, (Vector2)map.GetSprite(Vector3Int.zero).bounds.size);
            var floorRenderer = map.GetComponent<TilemapRenderer>();
            Assert.AreEqual(-4, floorRenderer.sortingOrder); Assert.AreEqual("Wall_Panels", floorRenderer.sortingLayerName);
            Assert.AreSame(room.Style.Rendering.FloorMaterial, floorRenderer.sharedMaterial);
            var walls = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.IsTrue(walls.All(r => r.sortingOrder == 3 && r.sortingLayerName == "Wall_Panels"));
            Assert.IsTrue(walls.All(r => r.sharedMaterial == room.Style.Rendering.ArchitectureMaterial));
            var north = walls.Where(r => r.name == "North").OrderBy(r => r.transform.localPosition.x).ToArray();
            Assert.AreEqual(4, north.Length);
            Assert.AreEqual(new Vector3(1, 8, 0), north[0].transform.localPosition);
            Assert.AreEqual(new Vector2(2, 4), (Vector2)north[0].sprite.bounds.size);
            Assert.AreEqual(8, north.Last().bounds.max.x, .00001f);
            Assert.AreEqual(6, north[0].bounds.min.y, .00001f);
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }
        [TestCase(false)] [TestCase(true)]
        public void ConfiguredPlacementReachesActualOutputAndSurvivesUndoReopen(bool multipleSpaces)
        {
            Configure(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = RoomPersistenceTests.CreateRoom(scene, folder);
            var layout = RoomAuthoringCreation.AdoptIntoNewLayout(room);
            if (multipleSpaces) RoomAuthoringCreation.CreateSpace(layout);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout)); AssertOutput(room);
            var id = room.RoomId; var cells = RoomAuthoringOperations.ReadCells(room);
            var snapshot = RoomUndoRedoTests.Snapshot(room);
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 2, 6, 1), room.Style.FloorTile));
            Undo.PerformUndo(); room = layout.Spaces.Single(r => r.RoomId == id); AssertOutput(room);
            CollectionAssert.AreEqual(snapshot, RoomUndoRedoTests.Snapshot(room));
            Undo.PerformRedo(); Assert.AreEqual(60, RoomAuthoringTestFixtures.FloorCount(room));
            Undo.PerformUndo(); AssertOutput(room);
            CollectionAssert.AreEquivalent(cells, RoomAuthoringOperations.ReadCells(room));
            var path = folder + "/ArtCompatibility.unity"; AssetDatabase.SaveAssets();
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single(r => r.RoomId == id);
            AssertOutput(room); CollectionAssert.AreEqual(snapshot, RoomUndoRedoTests.Snapshot(room));
            RoomAuthoringOperations.Rebuild(room); AssertOutput(room);
            CollectionAssert.AreEquivalent(cells, RoomAuthoringOperations.ReadCells(room));
        }
        [TestCase("ppu")] [TestCase("trim")] [TestCase("repeat")] [TestCase("anchor")]
        [TestCase("alignment")] [TestCase("orientation")] [TestCase("padding")]
        [TestCase("layer")] [TestCase("sort")]
        public void IncompatibleDeclarationsProduceActionableIssues(string fault)
        {
            Configure(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = RoomPersistenceTests.CreateRoom(scene, folder);
            AssertOutput(room);
            var style = room.Style; var module = style.NorthWalls[0];
            switch (fault)
            {
                case "ppu": module.PixelsPerUnit = 32; break;
                case "trim": module.CanvasPixels = new Vector2Int(128, 238); break;
                case "repeat": module.RepeatPixels = 33; break;
                case "anchor": module.AnchorPixels = new Vector2(float.NaN, 0); break;
                case "alignment": module.AnchorPixels = new Vector2(.1f, 0); break;
                case "orientation": module.Orientation = RoomStyleDefinition.ModuleOrientation.South; break;
                case "padding": module.HasFootprint = true; module.FootprintsPixels = new[] { new RectInt(120, 0, 16, 1) }; break;
                case "layer": style.Rendering.SortingLayerName = "MissingRoomAuthoringLayer"; break;
                case "sort": style.Rendering.DetailOrder = style.Rendering.ArchitectureOrder; break;
            }
            Assert.IsTrue(style.FloorTile != null, "Fixture floor marker must remain a valid asset after import.");
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length, "Fixture must have 48 cells before validation.");
            var assessment = RoomAuthoringValidator.Assess(room, out _);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, assessment.Status, "Pre-publication assessment: " + assessment.Summary);
            RoomAuthoringOperations.Rebuild(room);
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length, "Rebuild must preserve fixture cells.");
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.IsNotEmpty(room.GenerationResult.Issues);
            Assert.IsTrue(room.GenerationResult.Issues.All(i => !string.IsNullOrEmpty(i.Field) && !string.IsNullOrEmpty(i.Reason)));
            Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.IsEmpty(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "North"));
            if (fault != "layer" && fault != "sort") Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
        }
        [Test] public void UnconfiguredLegacyRenderingStaysCompatibleAcrossReopen()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = RoomPersistenceTests.CreateRoom(scene, folder); Assert.IsNull(room.Style.Rendering);
            var path = folder + "/Legacy.unity"; EditorSceneManager.SaveScene(scene, path);
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
            Assert.IsNull(room.Style.Rendering);
            Assert.AreEqual(0, room.GeneratedOutput.GetComponentInChildren<TilemapRenderer>().sortingOrder);
            Assert.IsTrue(room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().All(r => r.sortingOrder == 1));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
        }
    }
}
