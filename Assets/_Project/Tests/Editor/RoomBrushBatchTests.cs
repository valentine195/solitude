using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomBrushBatchTests
    {
        [TestCase(false)][TestCase(true)]
        public void LateRoomConflictCancelsWholeGestureAndNextStrokeStillSupportsUndoRedo(bool box)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style);
            var room = layout.Spaces[0]; var other = RoomAuthoringCreation.CreateSpace(layout);
            other.transform.localPosition = Vector3.right * 4;
            room.AuthoringMetadata = "Room stroke fixture metadata";
            other.AuthoringMetadata = "Protected neighbor fixture metadata";
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(other, new BoundsInt(0, 0, 0, 2, 2, 1), f.Style.FloorTile));
            var before = ShipLayoutDiagnosticTests.Snapshot(layout);
            var inputs = layout.Spaces.Select(EditorJsonUtility.ToJson).ToArray();
            var objects = layout.GetComponentsInChildren<Transform>(true);
            var revisions = RoomFloorPanelAssetStore.RetainedRevisionCount;
            var anchor = new GameObject("Unrelated Undo anchor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(anchor, f.Scene);
            Undo.IncrementCurrentGroup(); Undo.RecordObject(anchor.transform, "Move unrelated anchor");
            anchor.transform.position = Vector3.up; Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup();
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            var generations = 0; Action<ShipRoomAuthoring> generated = _ => generations++;
            RoomAuthoringOperations.GenerationCompleted += generated;
            try
            {
                var grid = room.GetComponent<Grid>(); var target = room.Occupancy.gameObject;
                brush.BeginGesture(room, box ? UnityEngine.GridBrushBase.Tool.Box : UnityEngine.GridBrushBase.Tool.Paint);
                if (box) brush.BoxFill(grid, target, new BoundsInt(2, 0, 0, 1, 2, 1));
                else brush.Paint(grid, target, new Vector3Int(2, 0, 0));
                Assert.Greater(RoomAuthoringOperations.ReadCells(room).Length, 4);
                if (box) brush.BoxFill(grid, target, new BoundsInt(3, 0, 0, 2, 2, 1));
                else brush.Paint(grid, target, new Vector3Int(4, 0, 0));
                brush.Paint(grid, target, new Vector3Int(3, 0, 0)); // Remaining samples in the cancelled gesture are ignored.
                brush.EndGesture();
                Assert.That(brush.StrokeIssue, Does.Contain(room.RoomId).And.Contain(other.RoomId).And.Contain("(4, 0)"));
                Assert.IsFalse(RoomAuthoringOperations.HasActiveEdit); Assert.AreEqual(0, generations);
                CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
                CollectionAssert.AreEqual(inputs, layout.Spaces.Select(EditorJsonUtility.ToJson));
                CollectionAssert.AreEqual(objects, layout.GetComponentsInChildren<Transform>(true));
                Assert.AreEqual(revisions, RoomFloorPanelAssetStore.RetainedRevisionCount);
                Undo.PerformUndo(); Assert.AreEqual(Vector3.zero, anchor.transform.position);
                CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
                brush.BeginGesture(room, UnityEngine.GridBrushBase.Tool.Box);
                brush.BoxFill(grid, target, new BoundsInt(2, 0, 0, 2, 2, 1)); brush.EndGesture();
                Assert.IsNull(brush.StrokeIssue); Assert.AreEqual(1, generations);
                var filled = ShipLayoutDiagnosticTests.Snapshot(layout);
                Undo.PerformUndo(); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
                Undo.PerformRedo(); CollectionAssert.AreEqual(filled, ShipLayoutDiagnosticTests.Snapshot(layout));
                brush.BeginGesture(room, UnityEngine.GridBrushBase.Tool.Box);
                brush.BoxErase(grid, target, new BoundsInt(2, 0, 0, 2, 2, 1)); brush.EndGesture();
                Assert.AreEqual(2, generations); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= generated; Object.DestroyImmediate(brush); }
        }

        [Test] public void ExplicitBoxEraseModeBatchesOnceAndPreservesOtherRoomsAndGeneratedTargets()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); var other = f.Room(); other.transform.position = Vector3.right * 20;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room)); Assert.IsTrue(RoomAuthoringOperations.Rebuild(other));
            var otherState = RoomUndoRedoTests.Snapshot(other);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>();
            brush.BoxMode = ShipRoomBrush.BoxEditMode.Erase;
            var applications = 0; Action<ShipRoomAuthoring> completed = changed => { Assert.AreSame(room, changed); applications++; };
            RoomAuthoringOperations.GenerationCompleted += completed;
            try
            {
                var grid = room.GetComponent<Grid>();
                brush.BoxFill(grid, room.GeneratedOutput.GetComponentInChildren<Tilemap>().gameObject, new BoundsInt(0, 0, 0, 8, 6, 1));
                Assert.AreEqual(0, applications);
                brush.BoxFill(grid, room.Occupancy.gameObject, new BoundsInt(4, 0, 0, 4, 6, 1));
                Assert.AreEqual(1, applications);
                Assert.AreEqual(24, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(24, RoomUndoRedoTests.Snapshot(room).Count(s => s.StartsWith("floor:")));
                Assert.IsEmpty(room.GenerationDiagnostic);
                brush.BoxMode = ShipRoomBrush.BoxEditMode.Fill; brush.OccupancyMarker = f.Style.FloorTile;
                brush.BoxFill(grid, room.Occupancy.gameObject, new BoundsInt(4, 0, 0, 4, 6, 1));
                Assert.AreEqual(2, applications);
                Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length);
                CollectionAssert.AreEqual(otherState, RoomUndoRedoTests.Snapshot(other));
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= completed; Object.DestroyImmediate(brush); }
        }

        private static string[] Output(ShipRoomAuthoring room) => room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.name + ":" + r.transform.localPosition + ":" + r.sprite.GetEntityId()).OrderBy(s => s).ToArray();

        [Test] public void BoxCallbacksGenerateOnceThenExpandAndRepeatWithoutDuplicates()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(0, 0);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            var applications = 0;
            Action<ShipRoomAuthoring> completed = changed => { Assert.AreSame(room, changed); applications++; };
            RoomAuthoringOperations.GenerationCompleted += completed;
            try
            {
                var grid = room.GetComponent<Grid>(); var target = room.Occupancy.gameObject;
                brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 8, 6, 1));
                Assert.AreEqual(1, applications);
                Assert.AreEqual(48, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(48, room.GeneratedOutput.GetComponentInChildren<Tilemap>().GetTilesBlock(new BoundsInt(0, 0, 0, 8, 6, 1)).Count(t => t != null));
                brush.BoxFill(grid, target, new BoundsInt(8, 0, 0, 4, 6, 1));
                Assert.AreEqual(2, applications);
                Assert.AreEqual(72, RoomAuthoringOperations.ReadCells(room).Length);
                var geometry = RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEquivalent(new[] { 12, 12, 6, 6 }, geometry.Runs.Select(r => r.Length));
                var sprites = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
                Assert.IsTrue(sprites.Where(r => r.name == "East" || r.name == "Northeast" || r.name == "Southeast").All(r => r.transform.localPosition.x == 12));
                Assert.IsFalse(sprites.Any(r => r.name == "East" && r.transform.localPosition.x == 8));
                var expected = Output(room);
                var retained = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
                brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 12, 6, 1));
                Assert.AreEqual(2, applications, "An identical fill is not a changed gesture.");
                CollectionAssert.AreEqual(retained, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                CollectionAssert.AreEqual(expected, Output(room));
                Assert.AreEqual(1, room.GeneratedOutput.GetComponentsInChildren<Tilemap>().Length);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= completed; Object.DestroyImmediate(brush); }
        }

        [TestCase(32, true)]
        [TestCase(64, false)]
        public void ErasingOuterColumnUsesFinalExtentAndEmptyEraseCleansOutput(int wallWidth, bool validAfterErase)
        {
            using var f = new RoomAuthoringTestFixtures(wallWidth);
            var room = f.Room(12, 6); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>();
            var applications = 0;
            Action<ShipRoomAuthoring> completed = changed => applications++;
            RoomAuthoringOperations.GenerationCompleted += completed;
            try
            {
                brush.BoxErase(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(11, 0, 0, 1, 6, 1));
                Assert.AreEqual(1, applications);
                Assert.AreEqual(66, RoomAuthoringOperations.ReadCells(room).Length);
                if (validAfterErase)
                {
                    Assert.IsEmpty(room.GenerationDiagnostic);
                    Assert.IsNotNull(room.GeneratedOutput.GetComponentInChildren<Tilemap>());
                }
                else
                {
                    Assert.That(room.GenerationDiagnostic, Does.Contain("not exactly coverable"));
                    Assert.AreEqual(66, RoomAuthoringTestFixtures.FloorCount(room));
                }
                brush.BoxErase(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(0, 0, 0, 12, 6, 1));
                Assert.AreEqual(2, applications);
                Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room));
                Assert.IsEmpty(room.GenerationDiagnostic);
                Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= completed; Object.DestroyImmediate(brush); }
        }

        [Test] public void BoxRejectsGeneratedTargetsAndNonXYBoundsWithoutGeneration()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            var before = RoomAuthoringOperations.ReadCells(room); var expected = Output(room);
            var applications = 0; Action<ShipRoomAuthoring> completed = changed => applications++;
            RoomAuthoringOperations.GenerationCompleted += completed;
            try
            {
                brush.BoxErase(room.GetComponent<Grid>(), room.GeneratedOutput.GetComponentInChildren<Tilemap>().gameObject, new BoundsInt(0, 0, 0, 8, 6, 1));
                brush.BoxFill(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(0, 0, 1, 8, 6, 1));
                brush.BoxFill(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(0, 0, 0, 8, 6, 2));
                Assert.AreEqual(0, applications);
                CollectionAssert.AreEqual(before, RoomAuthoringOperations.ReadCells(room));
                CollectionAssert.AreEqual(expected, Output(room));
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= completed; Object.DestroyImmediate(brush); }
        }
        [Test] public void PartialHoleBoxEraseAppliesGenerationOnceAndRestoresCompleteOnFill()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            var applications = 0;
            void Applied(SOLITUDE.World.RoomAuthoring.ShipRoomAuthoring changed) { if (changed == room) applications++; }
            RoomAuthoringOperations.GenerationCompleted += Applied;
            try
            {
                brush.BoxMode = ShipRoomBrush.BoxEditMode.Erase;
                brush.BoxFill(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(6, 4, 0, 1, 1, 1));
                Assert.AreEqual(1, applications);
                Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.AreEqual(SOLITUDE.World.RoomAuthoring.RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
                brush.BoxMode = ShipRoomBrush.BoxEditMode.Fill;
                brush.BoxFill(room.GetComponent<Grid>(), room.Occupancy.gameObject, new BoundsInt(6, 4, 0, 1, 1, 1));
                Assert.AreEqual(2, applications);
                Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.AreEqual(SOLITUDE.World.RoomAuthoring.RoomGenerationStatus.Complete, room.GenerationResult.Status);
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Applied;
                Object.DestroyImmediate(brush);
            }
        }
    }
}
