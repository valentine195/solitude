using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomValidationTests
    {
        [Test] public void ExteriorErasureRetainsOwnedFloorAndRecoveryRebuilds()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            var other = f.Room(); other.transform.position = Vector3.right * 8;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(other));
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var retained = other.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            room.Occupancy.SetTile(Vector3Int.zero, null);
            Assert.AreEqual(47, RoomAuthoringOperations.ReadCells(room).Length);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.That(room.GenerationDiagnostic, Does.Contain("rectangle"));
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
            CollectionAssert.AreEqual(retained, other.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>());
            room.Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.IsEmpty(room.GenerationDiagnostic);
            room.Occupancy.ClearAllTiles();
            room.Style = null;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(0, room.GeneratedOutput.transform.childCount);
            CollectionAssert.AreEqual(retained, other.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>());
        }

        [TestCase("wall", "North sprite")]
        [TestCase("corner", "Northwest corner")]
        [TestCase("ppu", "32 PPU")]
        [TestCase("filter", "Point filtering")]
        [TestCase("span", "exactly coverable")]
        public void InvalidStyleRetainsIndependentPreviewPreservesCellsAndRecovers(string fault, string message)
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var cells = RoomAuthoringOperations.ReadCells(room);
            var wall = f.Style.NorthWalls[0].Sprite;
            var corner = f.Style.Northwest.Sprite;
            Sprite incompatible = null;
            if (fault == "wall") f.Style.NorthWalls[0].Sprite = null;
            if (fault == "corner") f.Style.Northwest.Sprite = null;
            if (fault == "ppu")
            {
                incompatible = Sprite.Create(wall.texture, wall.rect, Vector2.zero, 64);
                f.Style.NorthWalls[0].Sprite = incompatible;
            }
            if (fault == "filter") wall.texture.filterMode = FilterMode.Bilinear;
            if (fault == "span") f.Style.NorthReservationsPixels = new Vector2Int(32, 0);
            Assert.That(RoomAuthoringValidator.Validate(room), Does.Contain(message));
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.That(room.GenerationDiagnostic, Does.Contain(message));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(SOLITUDE.World.RoomAuthoring.RoomGenerationStatus.IncompletePreview, room.GenerationResult.Status);
            CollectionAssert.AreEqual(cells, RoomAuthoringOperations.ReadCells(room));
            f.Style.NorthWalls[0].Sprite = wall;
            f.Style.Northwest.Sprite = corner;
            wall.texture.filterMode = FilterMode.Point;
            f.Style.NorthReservationsPixels = Vector2Int.zero;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room), room.GenerationDiagnostic);
            Assert.AreEqual(48, room.GeneratedOutput.GetComponentInChildren<Tilemap>().GetTilesBlock(new BoundsInt(0, 0, 0, 8, 6, 1)).Count(t => t != null));
            if (incompatible != null) Object.DestroyImmediate(incompatible);
        }

        [Test] public void ValidationDoesNotChangeSerializedStateOutputOrDirtiness()
        {
            using var f = new RoomAuthoringTestFixtures();
            var room = f.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var before = EditorJsonUtility.ToJson(room);
            var cells = EditorJsonUtility.ToJson(room.Occupancy);
            var output = room.GeneratedOutput.GetComponentsInChildren<Transform>();
            var dirty = f.Scene.isDirty;
            Assert.IsNull(RoomAuthoringValidator.Validate(room));
            f.Style.Northwest.Sprite = null;
            Assert.IsNotNull(RoomAuthoringValidator.Validate(room));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(room));
            Assert.AreEqual(cells, EditorJsonUtility.ToJson(room.Occupancy));
            CollectionAssert.AreEqual(output, room.GeneratedOutput.GetComponentsInChildren<Transform>());
            Assert.AreEqual(dirty, f.Scene.isDirty);
        }
    }
}
