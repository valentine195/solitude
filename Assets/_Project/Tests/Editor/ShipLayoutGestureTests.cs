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
    public sealed class ShipLayoutGestureTests
    {
        [TestCase("commit")] [TestCase("cancel")] [TestCase("no-change")] [TestCase("net-zero")]
        public void PanelResourcesPublishOncePerCompletedGestureAndNeverForCancelledOrUnchangedWork(string mode)
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Contains("-solitudeRoomAuthoringVerification"));
            using var fixture = new RoomAuthoringTestFixtures(32); ShipLayoutStyleTests.Configure(fixture);
            fixture.Style.Rendering = new RoomStyleDefinition.RenderingConfiguration();
            var name = "PanelGestureFixture" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/_Project/Tests", name); var folder = "Assets/_Project/Tests/" + name;
            RoomFloorPanelAssetStore.VerificationRoot = folder + "/Panels";
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style); var room = layout.Spaces[0];
            ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells()); Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            Assert.IsTrue(RoomAuthoringOperations.SetFloorPanelSettings(room, new RoomFloorPanelSettings { Enabled = true, Seed = 19 }, out var issue), issue);
            var before = RoomUndoRedoTests.Snapshot(room); var count = RoomFloorPanelAssetStore.RetainedRevisionCount;
            var calls = 0; void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                using (var edit = RoomAuthoringOperations.BeginEdit(room, "Panel gesture"))
                {
                    if (mode == "no-change") RoomAuthoringOperations.PaintCell(room, Vector3Int.zero, fixture.Style.FloorTile, edit);
                    else
                    {
                        RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 1, 6, 1), fixture.Style.FloorTile, edit);
                        RoomAuthoringOperations.PaintBox(room, new BoundsInt(9, 0, 0, 1, 6, 1), fixture.Style.FloorTile, edit);
                        if (mode == "net-zero") RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 2, 6, 1), null, edit);
                    }
                    Assert.AreEqual(0, calls); Assert.AreEqual(count, RoomFloorPanelAssetStore.RetainedRevisionCount);
                    if (mode == "cancel") edit.Cancel();
                }
                Assert.AreEqual(mode == "commit" ? 1 : 0, calls);
                Assert.AreEqual(count + (mode == "commit" ? 1 : 0), RoomFloorPanelAssetStore.RetainedRevisionCount);
                if (mode == "commit")
                {
                    Assert.AreEqual(60, RoomAuthoringTestFixtures.FloorCount(room)); Undo.PerformUndo();
                    CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                }
                else
                {
                    CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                    Undo.PerformUndo(); Assert.IsFalse(room.FloorPanels.Enabled, "Undo reaches the preceding committed panel enable.");
                }
            }
            finally
            {
                RoomAuthoringOperations.GenerationCompleted -= Generated;
                RoomAuthoringOperations.CancelActiveEdit(); Undo.ClearAll();
                Object.DestroyImmediate(layout.gameObject); AssetDatabase.DeleteAsset(folder); RoomFloorPanelAssetStore.VerificationRoot = null;
            }
        }

        [TestCase("stroke")]
        [TestCase("box")]
        [TestCase("erase")]
        public void NativeGestureBoundaryAppliesArchitectureExactlyOnce(string operation)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), fixture.Style.FloorTile));
            var before = RoomUndoRedoTests.Snapshot(room);
            var beforeLayout = JsonUtility.ToJson(layout.GenerationResult);
            var objects = layout.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = fixture.Style.FloorTile;
            var editor = (ShipRoomBrushEditor)UnityEditor.Editor.CreateEditor(brush);
            var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                editor.RegisterUndo(room.Occupancy.gameObject, operation == "stroke" ? GridBrushBase.Tool.Paint : GridBrushBase.Tool.Box);
                var grid = room.GetComponent<Grid>();
                if (operation == "stroke")
                    for (var y = 0; y < 6; y++) for (var x = 8; x < 12; x++)
                        brush.Paint(grid, room.Occupancy.gameObject, new Vector3Int(x, y, 0));
                else if (operation == "box") brush.BoxFill(grid, room.Occupancy.gameObject, new BoundsInt(8, 0, 0, 4, 6, 1));
                else
                {
                    brush.BoxMode = ShipRoomBrush.BoxEditMode.Erase;
                    brush.BoxFill(grid, room.Occupancy.gameObject, new BoundsInt(6, 4, 0, 2, 2, 1));
                }
                Assert.AreEqual(0, calls);
                Assert.AreEqual(operation == "erase" ? 44 : 72, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
                CollectionAssert.AreEqual(objects, layout.GeneratedOutput.GetComponentsInChildren<Transform>(true));
                var feedback = RoomAuthoringPreview.GetFeedback(room);
                Assert.IsTrue(feedback.IsEditing);
                Assert.IsFalse(feedback.IsCurrent);
                Assert.AreEqual(operation == "erase" ? 4 : 24,
                    operation == "erase" ? feedback.PendingRemovedCells.Length : feedback.PendingAddedCells.Length);
                editor.OnToolDeactivated(GridBrushBase.Tool.Box);
                Assert.AreEqual(1, calls);
                Assert.AreEqual(operation == "erase" ? 44 : 72, RoomAuthoringTestFixtures.FloorCount(room));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
                var after = RoomUndoRedoTests.Snapshot(room);
                var afterLayout = JsonUtility.ToJson(layout.GenerationResult);
                brush.EndGesture();
                Assert.AreEqual(1, calls, "Repeated end notification cannot apply twice.");
                Undo.PerformUndo();
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(beforeLayout, JsonUtility.ToJson(layout.GenerationResult));
                Undo.PerformRedo();
                CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(afterLayout, JsonUtility.ToJson(layout.GenerationResult));
                Assert.AreEqual(1, calls, "Undo/Redo restores recorded output without generating.");
            }
            finally
            {
                brush.CancelGesture();
                RoomAuthoringOperations.GenerationCompleted -= Generated;
                Object.DestroyImmediate(editor); Object.DestroyImmediate(brush);
            }
        }

        [TestCase("cancel")]
        [TestCase("no-change")]
        [TestCase("net-zero")]
        public void CancelledOrUnchangedGestureHasNoGenerationOrCommittedUndoEntry(string operation)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            RoomAuthoringOperations.Rebuild(layout);
            Assert.IsTrue(RoomAuthoringOperations.PaintCell(room, Vector3Int.zero, fixture.Style.FloorTile));
            var before = RoomUndoRedoTests.Snapshot(room);
            var result = JsonUtility.ToJson(layout.GenerationResult);
            var calls = 0;
            void Generated(ShipRoomAuthoring _) => calls++;
            RoomAuthoringOperations.GenerationCompleted += Generated;
            try
            {
                using (var edit = RoomAuthoringOperations.BeginEdit(room, "Test Gesture"))
                {
                    if (operation == "no-change") RoomAuthoringOperations.PaintCell(room, Vector3Int.zero, fixture.Style.FloorTile, edit);
                    else
                    {
                        RoomAuthoringOperations.PaintCell(room, Vector3Int.right, fixture.Style.FloorTile, edit);
                        if (operation == "cancel") edit.Cancel();
                        else RoomAuthoringOperations.PaintCell(room, Vector3Int.right, null, edit);
                    }
                }
                Assert.AreEqual(0, calls);
                Assert.IsFalse(RoomAuthoringOperations.IsEditing(room));
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(result, JsonUtility.ToJson(layout.GenerationResult));
                Undo.PerformUndo();
                Assert.IsEmpty(RoomAuthoringOperations.ReadCells(room), "Undo reaches the preceding committed paint.");
                Assert.AreEqual(RoomGenerationStatus.Empty, layout.GenerationResult.Status);
                Undo.PerformRedo();
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            }
            finally { RoomAuthoringOperations.GenerationCompleted -= Generated; }
        }

        [TestCase("notch")]
        [TestCase("hole")]
        [TestCase("empty")]
        [TestCase("missing-floor")]
        public void LayoutGestureRestoresCellsPartialOutputAndIssuesTogether(string operation)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            if (operation == "missing-floor") fixture.Style.FloorTile = null;
            var marker = ScriptableObject.CreateInstance<Tile>();
            try
            {
                RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), marker);
                var before = RoomUndoRedoTests.Snapshot(room);
                var beforeLayout = JsonUtility.ToJson(layout.GenerationResult);
                using (var edit = RoomAuthoringOperations.BeginEdit(room, "Erase Gesture"))
                {
                    if (operation == "empty") RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), null, edit);
                    else RoomAuthoringOperations.PaintCell(room, new Vector3Int(operation == "notch" ? 7 : 6, operation == "notch" ? 5 : 4, 0), null, edit);
                }
                Assert.AreEqual(operation == "empty" ? 0 : 47, RoomAuthoringOperations.ReadCells(room).Length);
                Assert.AreEqual(operation == "empty" || operation == "missing-floor" ? 0 : 47, RoomAuthoringTestFixtures.FloorCount(room));
                var after = RoomUndoRedoTests.Snapshot(room);
                var afterLayout = JsonUtility.ToJson(layout.GenerationResult);
                Undo.PerformUndo();
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(beforeLayout, JsonUtility.ToJson(layout.GenerationResult));
                Undo.PerformRedo();
                CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
                Assert.AreEqual(afterLayout, JsonUtility.ToJson(layout.GenerationResult));
                Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            }
            finally { Object.DestroyImmediate(marker); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StyleScopeRebuildRestoresStyleAndLayoutResultsAsOneUndoAction(bool layoutEntryPoint)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(0, 0, 0, 8, 6, 1), fixture.Style.FloorTile));
            var before = RoomUndoRedoTests.Snapshot(room);
            var beforeLayout = JsonUtility.ToJson(layout.GenerationResult);
            using (RoomAuthoringOperations.BeginEdit(room, "Remove Corner"))
            {
                Undo.RegisterCompleteObjectUndo(fixture.Style, "Remove Corner");
                fixture.Style.Northwest.Sprite = null;
                Assert.IsFalse(layoutEntryPoint ? RoomAuthoringOperations.Rebuild(layout) : RoomAuthoringOperations.Rebuild(room));
            }
            var after = RoomUndoRedoTests.Snapshot(room);
            var afterLayout = JsonUtility.ToJson(layout.GenerationResult);
            Undo.PerformUndo();
            CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Assert.AreEqual(beforeLayout, JsonUtility.ToJson(layout.GenerationResult));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Undo.PerformRedo();
            CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
            Assert.AreEqual(afterLayout, JsonUtility.ToJson(layout.GenerationResult));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void EndingPaletteEditingCancelsPendingCellsAndBlocksLateBrushSamples()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var room = layout.Spaces[0];
            RoomAuthoringOperations.Rebuild(layout);
            var before = RoomUndoRedoTests.Snapshot(room);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = fixture.Style.FloorTile;
            try
            {
                brush.BeginGesture(room, GridBrushBase.Tool.Paint);
                brush.Paint(room.GetComponent<Grid>(), room.Occupancy.gameObject, Vector3Int.zero);
                RoomAuthoringPaletteSetup.EndEditing();
                brush.Paint(room.GetComponent<Grid>(), room.Occupancy.gameObject, Vector3Int.right);
                brush.EndGesture();
                CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
                Assert.IsFalse(RoomAuthoringOperations.IsEditing(room));
            }
            finally { Object.DestroyImmediate(brush); }
        }
    }
}
