using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomRebuildTests
    {
        private static string[] Description(ShipRoomAuthoring room) => room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>(true)
            .Select(r => r.name + ":" + r.sprite.GetEntityId() + ":" + r.transform.localPosition).OrderBy(s => s).ToArray();
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
