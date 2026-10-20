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
                brush.BoxFill(grid, target, new BoundsInt(0, 0, 0, 12, 6, 1));
                Assert.AreEqual(3, applications);
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
                    Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
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
    }
}
