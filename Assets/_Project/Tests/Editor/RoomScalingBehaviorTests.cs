using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomScalingBehaviorTests
    {
        [Test] public void ResizeRetainsFloorAndUnmovedWallsAndUndoRestoresGeometryAndOrder()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            var west = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "West").ToArray();
            var before = RoomUndoRedoTests.Snapshot(room);
            var beforeOrder = room.GeneratedOutput.GetComponentsInChildren<Transform>().Select(t => t.name + ":" + t.localPosition).ToArray();
            Assert.IsTrue(RoomAuthoringOperations.PaintBox(room, new BoundsInt(8, 0, 0, 4, 6, 1), fixture.Style.FloorTile));
            var after = RoomUndoRedoTests.Snapshot(room);
            var afterOrder = room.GeneratedOutput.GetComponentsInChildren<Transform>().Select(t => t.name + ":" + t.localPosition).ToArray();
            Assert.AreSame(floor, room.GeneratedOutput.GetComponentInChildren<Tilemap>());
            CollectionAssert.AreEqual(west, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "West"));
            Assert.AreEqual(72, after.Count(entry => entry.StartsWith("floor:")));
            Undo.PerformUndo(); CollectionAssert.AreEqual(before, RoomUndoRedoTests.Snapshot(room));
            Assert.AreSame(floor, room.GeneratedOutput.GetComponentInChildren<Tilemap>());
            CollectionAssert.AreEqual(beforeOrder, room.GeneratedOutput.GetComponentsInChildren<Transform>().Select(t => t.name + ":" + t.localPosition));
            Undo.PerformRedo(); CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
            Assert.AreSame(floor, room.GeneratedOutput.GetComponentInChildren<Tilemap>());
            CollectionAssert.AreEqual(afterOrder, room.GeneratedOutput.GetComponentsInChildren<Transform>().Select(t => t.name + ":" + t.localPosition));
        }
        [Test] public void StyleChangeReplacesOnlyChangedModuleAndFloorReconcilesEditedCells()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room(); Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            var others = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name != "Northwest").ToArray();
            fixture.Style.Northwest.Sprite = fixture.Sprite(32, 32);
            floor.SetTile(Vector3Int.zero, null); floor.SetTile(new Vector3Int(30, 30, 0), fixture.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.AreSame(floor, room.GeneratedOutput.GetComponentInChildren<Tilemap>());
            CollectionAssert.AreEqual(others, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name != "Northwest"));
            Assert.AreSame(fixture.Style.FloorTile, floor.GetTile(Vector3Int.zero)); Assert.IsFalse(floor.HasTile(new Vector3Int(30, 30, 0)));
            Assert.AreSame(fixture.Style.Northwest.Sprite, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Northwest").sprite);
        }
        [TestCase("transform")] [TestCase("plane")]
        public void DistantRoomMustStillPassSafetyBeforeOverlapBoundsCanSkipCells(string fault)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room(); var distant = fixture.Room(); distant.transform.position = Vector3.right * 100;
            Assert.IsNull(RoomAuthoringValidator.Validate(room));
            if (fault == "transform") distant.transform.localScale = Vector3.one * 2;
            else distant.Occupancy.SetTile(new Vector3Int(0, 0, 1), fixture.Style.FloorTile);
            Assert.That(RoomAuthoringValidator.Validate(room), Does.Contain("Cannot qualify room overlap"));
        }
        [Test] public void SpatialChecksObserveExternalNegativeCellEditsUndoAndMovedRooms()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var room = fixture.Room(); var other = fixture.Room(); other.transform.position = Vector3.right * 20;
            Assert.IsNull(RoomAuthoringValidator.Validate(room));
            Undo.IncrementCurrentGroup(); Undo.RegisterCompleteObjectUndo(other.Occupancy, "External cell edit");
            other.Occupancy.SetTile(new Vector3Int(-20, 0, 0), fixture.Style.FloorTile);
            Assert.That(RoomAuthoringValidator.Validate(room), Does.Contain("overlaps"));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.IsNull(RoomAuthoringValidator.Validate(room));
            other.transform.position = Vector3.left * 7; Assert.That(RoomAuthoringValidator.Validate(room), Does.Contain("overlaps"));
            other.transform.position = Vector3.left * 8; Assert.IsNull(RoomAuthoringValidator.Validate(room));
        }
    }
}
