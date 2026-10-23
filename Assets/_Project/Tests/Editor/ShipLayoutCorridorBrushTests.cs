using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutCorridorBrushTests
    {
        private string folder;
        private ShipLayoutAuthoring layout;
        private ShipRoomAuthoring corridor;
        private ShipRoomBrush brush;
        private int generations;

        [SetUp] public void SetUp()
        {
            folder = ShipLayoutDiagnosticTests.CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            layout = RoomAuthoringCreation.CreateLayout(scene, AssetDatabase.LoadAssetAtPath<RoomStyleDefinition>(folder + "/Style.asset"), LogicalSpaceType.Corridor);
            corridor = layout.Spaces[0]; corridor.AuthoringMetadata = "Corridor brush fixture";
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = corridor.Style.FloorTile;
            generations = 0; RoomAuthoringOperations.GenerationCompleted += Generated;
        }

        private void Generated(ShipRoomAuthoring _) => generations++;

        [TearDown] public void TearDown()
        {
            RoomAuthoringOperations.GenerationCompleted -= Generated;
            if (brush != null) Object.DestroyImmediate(brush);
            RoomAuthoringOperations.CancelActiveEdit(); Selection.activeObject = null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (folder != null) AssetDatabase.DeleteAsset(folder);
        }

        private static Vector2Int[] Route(string shape) => shape switch
        {
            "straight" => new[] { new Vector2Int(0, 0), new Vector2Int(4, 0) },
            "bend" => new[] { new Vector2Int(0, 0), new Vector2Int(4, 0), new Vector2Int(4, 3) },
            "cross" => new[] { new Vector2Int(0, 2), new Vector2Int(4, 2), new Vector2Int(2, 2), new Vector2Int(2, 0), new Vector2Int(2, 4) },
            _ => new[] { new Vector2Int(0, 0), new Vector2Int(4, 3) }
        };

        private static HashSet<Vector2Int> Expected(string shape, int width)
        {
            var extents = new[] { (0, 0), (0, 1), (-1, 1), (-1, 2) }[width - 1];
            var result = new HashSet<Vector2Int>();
            void Rect(int x0, int y0, int x1, int y1)
            {
                for (var y = y0; y <= y1; y++) for (var x = x0; x <= x1; x++) result.Add(new Vector2Int(x, y));
            }
            var lo = extents.Item1; var hi = extents.Item2;
            if (shape == "cross") { Rect(lo, 2 + lo, 4 + hi, 2 + hi); Rect(2 + lo, lo, 2 + hi, 4 + hi); }
            else
            {
                Rect(lo, lo, 4 + hi, hi);
                if (shape != "straight") Rect(4 + lo, lo, 4 + hi, 3 + hi);
            }
            return result;
        }

        private void Sample(Vector2Int cell, bool erase = false)
        {
            var position = new Vector3Int(cell.x, cell.y, 0);
            if (erase) brush.Erase(corridor.GetComponent<Grid>(), corridor.Occupancy.gameObject, position);
            else brush.Paint(corridor.GetComponent<Grid>(), corridor.Occupancy.gameObject, position);
        }

        [TestCase(1, "straight")][TestCase(2, "straight")][TestCase(3, "straight")][TestCase(4, "straight")]
        [TestCase(1, "bend")][TestCase(2, "bend")][TestCase(3, "bend")][TestCase(4, "bend")]
        [TestCase(1, "cross")][TestCase(2, "cross")][TestCase(3, "cross")][TestCase(4, "cross")]
        [TestCase(1, "sparse")][TestCase(2, "sparse")][TestCase(3, "sparse")][TestCase(4, "sparse")]
        public void ExactOddEvenFootprintCommitsOnceAndReversesWithoutChangingIdentity(int width, string shape)
        {
            brush.CorridorWidth = width; var id = corridor.RoomId; var before = ShipLayoutDiagnosticTests.Snapshot(layout);
            var route = Route(shape); brush.BeginGesture(corridor, GridBrushBase.Tool.Paint);
            foreach (var sample in route) { Sample(sample); Assert.IsEmpty(RoomAuthoringOperations.ReadCells(corridor)); }
            var preview = brush.PreviewCorridor(corridor, (Vector3Int)route.Last(), GridBrushBase.Tool.Paint, out var issue);
            Assert.IsNull(issue); CollectionAssert.AreEquivalent(Expected(shape, width), preview);
            CollectionAssert.AreEquivalent(preview, brush.CorridorPreviewCells);
            Assert.IsFalse(RoomAuthoringPreview.GetFeedback(layout).IsCurrent); Assert.AreEqual(0, generations);
            brush.EndGesture(); Assert.AreEqual(1, generations); Assert.AreEqual(id, corridor.RoomId);
            CollectionAssert.AreEquivalent(preview, RoomAuthoringOperations.ReadCells(corridor));
            Assert.AreEqual(preview.Length, RoomAuthoringTestFixtures.FloorCount(corridor));
            Assert.AreEqual(RoomGenerationStatus.Complete, layout.GenerationResult.Status, layout.GenerationResult.Summary);
            var committed = ShipLayoutDiagnosticTests.Snapshot(layout);
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
            Undo.PerformRedo(); CollectionAssert.AreEqual(committed, ShipLayoutDiagnosticTests.Snapshot(layout)); Assert.AreEqual(1, generations);
            brush.BeginGesture(corridor, GridBrushBase.Tool.Paint);
            foreach (var sample in route.Reverse()) Sample(sample);
            CollectionAssert.AreEquivalent(preview, brush.CorridorPreviewCells);
            brush.EndGesture(); Assert.AreEqual(1, generations, "Repainting the same corridor is a net-zero gesture.");
            CollectionAssert.AreEqual(committed, ShipLayoutDiagnosticTests.Snapshot(layout)); Assert.AreEqual(id, corridor.RoomId);
        }

        [Test] public void LateOverlapRejectsTheWholeRouteAndPreservesBothOwnersAndOutput()
        {
            var other = RoomAuthoringCreation.CreateSpace(layout); other.transform.localPosition = Vector3.right * 4;
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(other, new BoundsInt(0, 0, 0, 2, 2, 1), other.Style.FloorTile));
            generations = 0; var before = ShipLayoutDiagnosticTests.Snapshot(layout); var objects = layout.GetComponentsInChildren<Transform>(true);
            brush.CorridorWidth = 2; brush.BeginGesture(corridor, GridBrushBase.Tool.Paint);
            Sample(Vector2Int.zero); Sample(Vector2Int.right * 5);
            var preview = brush.PreviewCorridor(corridor, new Vector3Int(5, 0, 0), GridBrushBase.Tool.Paint, out var issue);
            Assert.That(issue, Does.Contain(corridor.RoomId).And.Contain(other.RoomId).And.Contain("(4, 0)"));
            Assert.IsNotEmpty(preview); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
            Assert.IsFalse(RoomAuthoringPreview.GetFeedback(other).IsCurrent);
            brush.EndGesture(); Assert.That(brush.StrokeIssue, Does.Contain(other.RoomId)); Assert.AreEqual(0, generations);
            CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout)); CollectionAssert.AreEqual(objects, layout.GetComponentsInChildren<Transform>(true));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void CancelWidthChangeAndScenePreparationLeaveNoCommittedRoute()
        {
            var before = ShipLayoutDiagnosticTests.Snapshot(layout);
            foreach (var action in new[] { "cancel", "width", "save" })
            {
                brush.BeginGesture(corridor, GridBrushBase.Tool.Paint); Sample(Vector2Int.zero); Sample(new Vector2Int(3, 2));
                if (action == "cancel") brush.CancelGesture();
                else if (action == "width") brush.CorridorWidth++;
                else RoomAuthoringEditLifecycle.PrepareForRuntime(corridor.gameObject.scene);
                brush.EndGesture(); Assert.IsEmpty(brush.CorridorPreviewCells); Assert.AreEqual(0, generations);
                CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => brush.CorridorWidth = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => brush.CorridorWidth = -1);
        }

        [Test] public void BranchAndWidthEraseKeepOneCorridorAndOneGenerationPerGesture()
        {
            var id = corridor.RoomId; brush.CorridorWidth = 2;
            brush.BeginGesture(corridor, GridBrushBase.Tool.Paint); Sample(Vector2Int.zero); Sample(Vector2Int.right * 4); brush.EndGesture();
            brush.BeginGesture(corridor, GridBrushBase.Tool.Paint); Sample(new Vector2Int(2, 0)); Sample(new Vector2Int(2, 4)); brush.EndGesture();
            Assert.AreEqual(2, generations); Assert.AreEqual(id, corridor.RoomId); Assert.AreEqual(1, layout.Spaces.Count);
            var before = ShipLayoutDiagnosticTests.Snapshot(layout); var cells = RoomAuthoringOperations.ReadCells(corridor);
            brush.BeginGesture(corridor, GridBrushBase.Tool.Erase); Sample(new Vector2Int(2, 3), true); Sample(new Vector2Int(2, 4), true);
            var erased = brush.CorridorPreviewCells; brush.EndGesture(); Assert.AreEqual(3, generations);
            CollectionAssert.AreEquivalent(cells.Except(erased), RoomAuthoringOperations.ReadCells(corridor)); Assert.AreEqual(id, corridor.RoomId);
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, ShipLayoutDiagnosticTests.Snapshot(layout)); Assert.AreEqual(3, generations);
        }
    }
}
