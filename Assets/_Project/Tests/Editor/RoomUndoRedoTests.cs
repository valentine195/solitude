using System.Collections.Generic;
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
    public sealed class RoomUndoRedoTests
    {
        public static string[] Snapshot(ShipRoomAuthoring room)
        {
            var result = new List<string> { "diagnostic:" + room.GenerationDiagnostic };
            foreach (var c in RoomAuthoringOperations.ReadCells(room))
                result.Add("authored:" + c + ":" + room.Occupancy.GetTile(new Vector3Int(c.x, c.y, 0)).GetEntityId());
            foreach (var renderer in room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>())
                result.Add("sprite:" + renderer.name + ":" + renderer.transform.localPosition + ":" + renderer.sprite.GetEntityId());
            foreach (var map in room.GeneratedOutput.GetComponentsInChildren<Tilemap>())
                foreach (var p in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(p)) result.Add("floor:" + p + ":" + map.GetTile(p).GetEntityId());
            result.Add("objects:" + room.GeneratedOutput.GetComponentsInChildren<Transform>().Length);
            return result.OrderBy(s => s).ToArray();
        }

        [TestCase("Paint")]
        [TestCase("Paint stroke")]
        [TestCase("Erase invalid")]
        [TestCase("Box fill")]
        [TestCase("Box expansion")]
        [TestCase("Box erase")]
        public void NativePreActionBoundaryRestoresCompleteAuthoredAndGeneratedState(string operation)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = operation.StartsWith("Paint") ? f.Room(1, 1) : operation == "Box fill" ? f.Room(0, 0) : f.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var before = Snapshot(room);
            var brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = f.Style.FloorTile;
            var editor = (ShipRoomBrushEditor)UnityEditor.Editor.CreateEditor(brush);
            var tool = operation.StartsWith("Paint") ? GridBrushBase.Tool.Paint : operation == "Erase invalid" ? GridBrushBase.Tool.Erase : GridBrushBase.Tool.Box;
            try
            {
                editor.RegisterUndo(room.Occupancy.gameObject, tool);
                var grid = room.GetComponent<Grid>(); var target = room.Occupancy.gameObject;
                switch (operation)
                {
                    case "Paint": brush.Paint(grid, target, Vector3Int.right); break;
                    case "Paint stroke":
                        brush.Paint(grid, target, Vector3Int.right);
                        brush.Paint(grid, target, Vector3Int.right * 2);
                        break;
                    case "Erase invalid": brush.Erase(grid, target, new Vector3Int(7, 5, 0)); break;
                    case "Box fill": brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 8, 6, 1)); break;
                    case "Box expansion": brush.BoxFill(grid, target, new BoundsInt(8, 0, 0, 4, 6, 1)); break;
                    case "Box erase": brush.BoxErase(grid, target, new BoundsInt(0, 0, 0, 8, 6, 1)); break;
                }
                editor.OnToolDeactivated(tool);
                var after = Snapshot(room);
                CollectionAssert.AreNotEqual(before, after);
                if (operation == "Erase invalid")
                {
                    Assert.AreEqual(47, RoomAuthoringOperations.ReadCells(room).Length);
                    Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
                    Assert.That(room.GenerationDiagnostic, Does.Contain("rectangle"));
                }
                Undo.PerformUndo();
                CollectionAssert.AreEqual(before, Snapshot(room), operation + " Undo");
                Undo.PerformRedo();
                CollectionAssert.AreEqual(after, Snapshot(room), operation + " Redo");
                Assert.AreSame(room, room.GeneratedOutput.Owner);
                Assert.AreSame(room.transform, room.Occupancy.transform.parent);
            }
            finally { brush.EndGesture(); Object.DestroyImmediate(editor); Object.DestroyImmediate(brush); }
        }
    }
}
