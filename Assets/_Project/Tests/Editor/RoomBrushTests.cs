using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomBrushTests
    {
        private GridBrushBase previousBrush;
        private GameObject previousTarget;
        private ShipRoomBrush brush;

        [SetUp] public void SetUp()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"), "Brush fixtures require the isolated verification runner.");
            previousBrush = GridPaintingState.gridBrush;
            previousTarget = GridPaintingState.scenePaintTarget;
            RoomAuthoringPaletteSetup.EnsureAssets();
            brush = ScriptableObject.CreateInstance<ShipRoomBrush>();
            brush.OccupancyMarker = AssetDatabase.LoadAssetAtPath<Tile>(RoomAuthoringPaletteSetup.MarkerPath);
            GridPaintingState.gridBrush = brush;
        }

        [TearDown] public void TearDown()
        {
            RoomAuthoringPaletteSetup.EndEditing();
            GridPaintingState.gridBrush = previousBrush;
            GridPaintingState.scenePaintTarget = previousTarget;
            if (brush != null) Object.DestroyImmediate(brush);
        }

        [Test] public void ProductionPaintEraseCallbacksMaintainOwnedCellsAndFloor()
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = f.Room(1, 1);
            var identity = room.RoomId;
            var grid = room.GetComponent<Grid>();
            GridPaintingState.scenePaintTarget = room.Occupancy.gameObject;
            RoomAuthoringPaletteSetup.ActivateTarget(room.Occupancy.gameObject);
            Assert.IsTrue(room.Occupancy.gameObject.activeSelf);
            brush.Paint(grid, room.Occupancy.gameObject, Vector3Int.right);
            Assert.AreEqual(2, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.AreSame(brush.OccupancyMarker, room.Occupancy.GetTile(Vector3Int.right));
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            Assert.AreSame(f.Style.FloorTile, floor.GetTile(Vector3Int.right));
            Assert.AreNotSame(brush.OccupancyMarker, floor.GetTile(Vector3Int.right));
            brush.Erase(grid, room.Occupancy.gameObject, Vector3Int.right);
            Assert.AreEqual(1, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.IsNull(room.Occupancy.GetTile(Vector3Int.right));
            Assert.IsNull(room.GeneratedOutput.GetComponentInChildren<Tilemap>().GetTile(Vector3Int.right));
            brush.Paint(grid, room.Occupancy.gameObject, new Vector3Int(1, 1, 0));
            Assert.AreEqual(2, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            Assert.AreEqual(2, room.GenerationResult.Issues.Count(i => i.Kind == RoomIssueKind.Geometry && i.Field.StartsWith("Disconnected/")));
            Assert.IsTrue(room.GenerationResult.Issues.All(i => i.HasRegion && i.RoomId == identity));
            Assert.AreEqual(identity, room.RoomId);
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(room));
            brush.Paint(grid, room.Occupancy.gameObject, Vector3Int.right);
            Assert.AreEqual(RoomGenerationStatus.Complete, room.GenerationResult.Status);
            Assert.IsEmpty(room.GenerationDiagnostic);
            Assert.AreEqual(3, RoomAuthoringTestFixtures.FloorCount(room));
            RoomAuthoringPaletteSetup.EndEditing();
            Assert.IsFalse(room.Occupancy.gameObject.activeSelf);
        }

        [Test] public void NonRoomAndGeneratedTargetsAndNonzeroZAreRejected()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            var generated = floor.GetTilesBlock(floor.cellBounds);
            var cells = RoomAuthoringOperations.ReadCells(room);
            var invalid = new GameObject("Unrelated", typeof(Grid), typeof(Tilemap));
            try
            {
                brush.Paint(room.GetComponent<Grid>(), floor.gameObject, new Vector3Int(30, 30, 0));
                brush.Erase(room.GetComponent<Grid>(), floor.gameObject, Vector3Int.zero);
                brush.Paint(invalid.GetComponent<Grid>(), invalid, Vector3Int.zero);
                brush.Paint(room.GetComponent<Grid>(), room.Occupancy.gameObject, new Vector3Int(0, 0, 1));
                CollectionAssert.AreEqual(generated, floor.GetTilesBlock(floor.cellBounds));
                CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
                Assert.IsNull(invalid.GetComponent<Tilemap>().GetTile(Vector3Int.zero));
                var editor = (ShipRoomBrushEditor)UnityEditor.Editor.CreateEditor(brush);
                try
                {
                    CollectionAssert.Contains(editor.validTargets, room.Occupancy.gameObject);
                    CollectionAssert.DoesNotContain(editor.validTargets, floor.gameObject);
                    CollectionAssert.DoesNotContain(editor.validTargets, invalid);
                }
                finally { Object.DestroyImmediate(editor); }
            }
            finally { Object.DestroyImmediate(invalid); }
        }

        [TestCase(false, "member")][TestCase(true, "member")]
        [TestCase(false, "layout")][TestCase(true, "layout")]
        [TestCase(false, "legacy")][TestCase(true, "legacy")]
        public void DirectRoomPaintRejectsForeignCellsBeforeMutationOrUndo(bool box, string ownership)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var room = layout.Spaces[0];
            var other = ownership == "member" ? RoomAuthoringCreation.CreateSpace(layout) :
                ownership == "layout" ? RoomAuthoringCreation.CreateLayout(f.Scene, f.Style).Spaces[0] : f.Room(0, 0);
            other.transform.localPosition = Vector3.right * 4;
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(other, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            var before = RoomUndoRedoTests.Snapshot(room); var otherBefore = RoomUndoRedoTests.Snapshot(other);
            var input = EditorJsonUtility.ToJson(room); var otherInput = EditorJsonUtility.ToJson(other);
            var objects = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var revisions = RoomFloorPanelAssetStore.RetainedRevisionCount;
            var anchor = new GameObject("Unrelated Undo anchor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(anchor, f.Scene);
            Undo.IncrementCurrentGroup(); Undo.RecordObject(anchor.transform, "Move unrelated anchor");
            anchor.transform.position = Vector3.up; Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            string issue;
            var accepted = box ? RoomAuthoringOperations.PaintBox(room, new BoundsInt(2, 0, 0, 3, 2, 1), f.Style.FloorTile, out issue) :
                RoomAuthoringOperations.PaintCell(room, new Vector3Int(4, 0, 0), f.Style.FloorTile, out issue);
            Assert.IsFalse(accepted);
            Assert.That(issue, Does.Contain(room.RoomId).And.Contain(other.RoomId).And.Contain("(4, 0)"));
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            CollectionAssert.AreEqual(otherBefore, RoomUndoRedoTests.Snapshot(other));
            Assert.AreEqual(input, EditorJsonUtility.ToJson(room)); Assert.AreEqual(otherInput, EditorJsonUtility.ToJson(other));
            CollectionAssert.AreEqual(objects, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
            Assert.AreEqual(revisions, RoomFloorPanelAssetStore.RetainedRevisionCount); Assert.AreEqual(group, Undo.GetCurrentGroup());
            Undo.PerformUndo(); Assert.AreEqual(Vector3.zero, anchor.transform.position, "Refusal adds no partial paint Undo item.");
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
        }

        [Test] public void WrongRoomEditCannotBeCancelledByARejectedPaintCall()
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = f.Room(2, 2); var other = f.Room(2, 2); other.transform.position = Vector3.right * 4;
            using var edit = RoomAuthoringOperations.BeginEdit(room, "Valid room stroke");
            Assert.IsTrue(RoomAuthoringOperations.PaintCell(room, new Vector3Int(2, 0, 0), f.Style.FloorTile, edit));
            Assert.Throws<InvalidOperationException>(() => RoomAuthoringOperations.PaintCell(other, new Vector3Int(-4, 0, 0), f.Style.FloorTile, edit));
            Assert.IsTrue(RoomAuthoringOperations.IsEditing(room)); Assert.AreEqual(5, RoomAuthoringOperations.ReadCells(room).Length);
            edit.Cancel(); Assert.AreEqual(4, RoomAuthoringOperations.ReadCells(room).Length);
        }

        [Test] public void RepeatedSetupPreservesManuallyAdjustedPalette()
        {
            var bytes = File.ReadAllBytes(RoomAuthoringPaletteSetup.PalettePath);
            var beforeGuid = AssetDatabase.AssetPathToGUID(RoomAuthoringPaletteSetup.PalettePath);
            try
            {
                var contents = PrefabUtility.LoadPrefabContents(RoomAuthoringPaletteSetup.PalettePath);
                try
                {
                    contents.GetComponentInChildren<Tilemap>().SetTile(new Vector3Int(3, 2, 0), brush.OccupancyMarker);
                    PrefabUtility.SaveAsPrefabAsset(contents, RoomAuthoringPaletteSetup.PalettePath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                var adjusted = File.ReadAllBytes(RoomAuthoringPaletteSetup.PalettePath);
                RoomAuthoringPaletteSetup.EnsureAssets();
                CollectionAssert.AreEqual(adjusted, File.ReadAllBytes(RoomAuthoringPaletteSetup.PalettePath));
                Assert.AreEqual(beforeGuid, AssetDatabase.AssetPathToGUID(RoomAuthoringPaletteSetup.PalettePath));
                Assert.IsTrue(AssetDatabase.LoadAllAssetsAtPath(RoomAuthoringPaletteSetup.PalettePath).Any(asset => asset is GridPalette));
                var savedBrush = AssetDatabase.LoadAssetAtPath<ShipRoomBrush>(RoomAuthoringPaletteSetup.BrushPath);
                Assert.AreSame(brush.OccupancyMarker, savedBrush.OccupancyMarker);
            }
            finally
            {
                File.WriteAllBytes(RoomAuthoringPaletteSetup.PalettePath, bytes);
                AssetDatabase.ImportAsset(RoomAuthoringPaletteSetup.PalettePath, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
