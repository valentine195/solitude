using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomRebuildTests
    {
        private static string[] Description(ShipRoomAuthoring room) => room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>(true)
            .Select(r => r.name + ":" + r.sprite.GetEntityId() + ":" + r.transform.localPosition).OrderBy(s => s).ToArray();

        [Test] public void DeletedOutputContainerRebuildsWithoutChangingAuthoredOrUnrelatedContent()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var other = f.Room(); other.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(other));
            var identity = room.RoomId;
            var cells = RoomAuthoringOperations.ReadCells(room);
            var expected = Description(room);
            var otherOutput = other.GeneratedOutput.GetComponentsInChildren<Transform>();
            var sentinel = new GameObject("Generated Architecture"); sentinel.transform.SetParent(room.transform, false);
            UnityEngine.Object.DestroyImmediate(room.GeneratedOutput.gameObject);
            Assert.IsNotNull(RoomAuthoringValidator.Validate(room));
            Assert.IsTrue(room.GeneratedOutput == null, "Validate must not recreate output.");
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            Assert.AreEqual(identity, room.RoomId);
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            CollectionAssert.AreEqual(expected, Description(room));
            CollectionAssert.AreEqual(otherOutput, other.GeneratedOutput.GetComponentsInChildren<Transform>());
            Assert.AreEqual(room.transform, sentinel.transform.parent);
            Assert.AreSame(room, room.GeneratedOutput.Owner);
            Assert.AreEqual(48, room.GeneratedOutput.GetComponentInChildren<Tilemap>().GetTilesBlock(new BoundsInt(0, 0, 0, 8, 6, 1)).Count(t => t != null));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.IsTrue(room.GeneratedOutput == null);
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            Undo.PerformRedo();
            Assert.AreSame(room, room.GeneratedOutput.Owner);
            CollectionAssert.AreEqual(expected, Description(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(expected, Description(room));
        }

        [TestCase("identity")]
        [TestCase("retained output")]
        [TestCase("transform")]
        public void MissingOutputRecoveryRefusesAmbiguousOrUnsupportedRooms(string fault)
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var retained = room.GeneratedOutput;
            var serialized = new SerializedObject(room);
            if (fault == "retained output")
            {
                serialized.FindProperty("generatedOutput").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(retained.gameObject);
                if (fault == "identity")
                {
                    var other = f.Room();
                    serialized.FindProperty("roomId").stringValue = other.RoomId;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                else room.transform.localScale = Vector3.one * 2;
            }
            var objects = room.GetComponentsInChildren<Transform>(true);
            var cells = RoomAuthoringOperations.ReadCells(room);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.IsTrue(room.GeneratedOutput == null);
            CollectionAssert.AreEqual(objects, room.GetComponentsInChildren<Transform>(true));
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            if (fault == "retained output") Assert.AreSame(room, retained.Owner);
        }
        [Test] public void RepeatedRebuildAndDeletedVisualsProduceSameNativeOutput()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var authored = RoomAuthoringOperations.ReadCells(room);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            var expected = Description(room);
            var floor = room.GeneratedOutput.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(48, floor.GetUsedTilesCount() == 1 ? floor.GetTilesBlock(floor.cellBounds).Count(t => t != null) : -1);
            foreach (var c in authored) Assert.AreSame(f.Style.FloorTile, floor.GetTile(new Vector3Int(c.x, c.y, 0)));
            Assert.IsNull(floor.GetTile(new Vector3Int(8, 0, 0)));
            Assert.AreEqual((Vector3)((Tile)f.Style.FloorTile).sprite.pivot / 32f, floor.tileAnchor);
            Assert.AreEqual(4, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "North"));
            foreach (var t in room.GeneratedOutput.GetComponentsInChildren<Transform>()) Assert.AreEqual(Vector3.one, t.localScale);
            UnityEngine.Object.DestroyImmediate(floor.gameObject);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(expected, Description(room));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(expected, Description(room));
            CollectionAssert.AreEqual(authored, RoomAuthoringOperations.ReadCells(room));
        }
    }
}
