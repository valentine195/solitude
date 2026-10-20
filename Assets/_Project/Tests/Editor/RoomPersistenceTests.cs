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
    public sealed class RoomPersistenceTests
    {
        public static string CreateAssets()
        {
            var folder = "Assets/_Project/Tests/RoomAuthoringFixture" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", Path.GetFileName(folder));
            Sprite Import(string name, int width, int height)
            {
                var texture = new Texture2D(width, height);
                texture.SetPixels(Enumerable.Repeat(Color.white, width * height).ToArray()); texture.Apply();
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
            var square = Import("square", 32, 32);
            var wall = Import("wall", 64, 64);
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = square;
            AssetDatabase.CreateAsset(tile, folder + "/Floor.asset");
            RoomStyleDefinition.Module Module(Sprite sprite, int repeat) => new RoomStyleDefinition.Module
            { Sprite = sprite, CanvasPixels = Vector2Int.RoundToInt(sprite.rect.size), RepeatPixels = repeat };
            var style = ScriptableObject.CreateInstance<RoomStyleDefinition>();
            style.FloorTile = tile;
            style.NorthWalls = new[] { Module(wall, 64) };
            style.WestCap = Module(square, 32); style.EastCap = Module(square, 32); style.SouthCap = Module(square, 32);
            style.Northwest = Module(square, 32); style.Northeast = Module(square, 32);
            style.Southwest = Module(square, 32); style.Southeast = Module(square, 32);
            AssetDatabase.CreateAsset(style, folder + "/Style.asset"); AssetDatabase.SaveAssets();
            return folder;
        }

        public static ShipRoomAuthoring CreateRoom(Scene scene, string folder)
        {
            var style = AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset");
            var room = RoomAuthoringCreation.Create(scene, style);
            foreach (var cell in RoomAuthoringTestFixtures.Cells()) room.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            return room;
        }

        public static string[] Placements(ShipRoomAuthoring room) => room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.name + ":" + AssetDatabase.GetAssetPath(r.sprite) + ":" + r.transform.localPosition).OrderBy(s => s).ToArray();

        [Test] public void SaveReopenPreservesReferencesCellsAndEquivalentRebuildWithoutDirtyingOtherScenes()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"),
                "Persistence tests require an isolated project and explicit verification flag.");
            var folder = CreateAssets();
            var previous = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sentinel = new GameObject("Existing Scene Sentinel");
            sentinel.transform.position = new Vector3(7, 11, 0);
            Assert.IsTrue(EditorSceneManager.SaveScene(previous, folder + "/Baseline.unity"));
            var previousDirty = previous.isDirty;
            var scene = default(Scene);
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var room = CreateRoom(scene, folder);
                var id = room.RoomId;
                var styleGuid = AssetDatabase.AssetPathToGUID(folder + "/Style.asset");
                var cells = RoomAuthoringOperations.ReadCells(room);
                var placements = Placements(room);
                var brush = ScriptableObject.CreateInstance<ShipRoomBrush>();
                try
                {
                    brush.Erase(room.GetComponent<Grid>(), room.Occupancy.gameObject, new Vector3Int(7, 5, 0));
                    Assert.AreEqual(47, RoomAuthoringOperations.ReadCells(room).Length);
                    Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
                    Undo.PerformUndo();
                    CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                    CollectionAssert.AreEqual(placements, Placements(room));
                }
                finally { Object.DestroyImmediate(brush); }
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, folder + "/Room.unity"));
                Assert.IsFalse(scene.isDirty);
                Assert.IsNull(RoomAuthoringValidator.Validate(room));
                Assert.IsFalse(scene.isDirty, "Validation must preserve a clean saved scene.");
                SceneManager.SetActiveScene(previous);
                Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
                scene = EditorSceneManager.OpenPreviewScene(folder + "/Room.unity");
                room = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                Assert.AreEqual(id, room.RoomId);
                Assert.AreEqual(styleGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(room.Style)));
                CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEqual(placements, Placements(room));
                Assert.AreEqual(48, room.GeneratedOutput.GetComponentInChildren<Tilemap>().GetTilesBlock(new BoundsInt(0, 0, 0, 8, 6, 1)).Count(t => t != null));
                Assert.IsFalse(room.Occupancy.gameObject.activeSelf);
                Assert.AreEqual("EditorOnly", room.Occupancy.tag);
                Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
                CollectionAssert.AreEqual(placements, Placements(room));
                Assert.AreEqual(previousDirty, previous.isDirty);
                Assert.AreEqual(new Vector3(7, 11, 0), sentinel.transform.position);
                Assert.AreEqual(previous, sentinel.scene);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (scene.IsValid() && scene.isLoaded)
                {
                    if (EditorSceneManager.IsPreviewScene(scene)) EditorSceneManager.ClosePreviewScene(scene);
                    else EditorSceneManager.CloseScene(scene, true);
                }
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }
        [TestCase("complete")]
        [TestCase("hole")]
        [TestCase("missing floor")]
        public void SavedResultReopensWithoutGenerationAndRebuildIsEquivalent(string state)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            var folder = CreateAssets();
            var previous = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var neighbor = CreateRoom(previous, folder); neighbor.transform.position = Vector3.right * 20;
            var sentinel = new GameObject("Generated Architecture"); sentinel.transform.position = new Vector3(9, 11, 0);
            EditorSceneManager.SaveScene(previous, folder + "/Neighbor.unity");
            var neighborState = RoomUndoRedoTests.Snapshot(neighbor);
            var neighborObjects = neighbor.GetComponentsInChildren<Transform>(true);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var applications = 0;
            void Generated(ShipRoomAuthoring _) => applications++;
            try
            {
                var room = CreateRoom(scene, folder);
                if (state == "hole") room.Occupancy.SetTile(new Vector3Int(6, 4, 0), null);
                if (state == "missing floor")
                {
                    room.Style.FloorTile = null;
                    EditorUtility.SetDirty(room.Style); AssetDatabase.SaveAssets();
                }
                Assert.AreEqual(state == "complete", RoomAuthoringOperations.Rebuild(room));
                Assert.AreEqual(state == "complete" ? RoomGenerationStatus.Complete : RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
                var expected = RoomUndoRedoTests.Snapshot(room);
                var identity = room.RoomId;
                var styleGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(room.Style));
                var path = folder + "/SavedResult.unity";
                RoomAuthoringOperations.GenerationCompleted += Generated;
                Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
                SceneManager.SetActiveScene(previous);
                Assert.IsTrue(EditorSceneManager.CloseScene(scene, true));
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                room = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ShipRoomAuthoring>()).Single();
                Selection.activeGameObject = room.gameObject;
                Assert.AreEqual(0, applications, "Reopen and selection must not generate.");
                Assert.AreEqual(identity, room.RoomId);
                Assert.AreEqual(styleGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(room.Style)));
                CollectionAssert.AreEqual(expected, RoomUndoRedoTests.Snapshot(room));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(room));
                Assert.AreEqual(state == "missing floor" ? 0 : state == "hole" ? 47 : 48, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.IsFalse(room.Occupancy.gameObject.activeSelf);
                Assert.IsNull(RoomAuthoringPreview.SelectedIssueId);
                Assert.IsFalse(scene.isDirty);
                Assert.AreEqual(state == "complete", RoomAuthoringOperations.Rebuild(room));
                Assert.AreEqual(1, applications);
                CollectionAssert.AreEqual(expected, RoomUndoRedoTests.Snapshot(room));
                CollectionAssert.AreEqual(neighborState, RoomUndoRedoTests.Snapshot(neighbor));
                CollectionAssert.AreEqual(neighborObjects, neighbor.GetComponentsInChildren<Transform>(true));
                Assert.IsFalse(previous.isDirty);
                Assert.AreEqual(new Vector3(9, 11, 0), sentinel.transform.position);
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated;
                Selection.activeObject = null;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
