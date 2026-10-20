using System.Linq;
using NUnit.Framework;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomPlacementTests
    {
        [TestCase(64, 4)] [TestCase(32, 8)]
        public void WallStructuralRepeatExactlyCoversEightCells(int width, int count)
        {
            using var f = new RoomAuthoringTestFixtures(width);
            Assert.IsTrue(RoomPlacementBuilder.TryBuild(RoomBoundaryExtractor.Extract(RoomAuthoringTestFixtures.Cells()), f.Style, out var result, out var issue), issue);
            var north = result.Sprites.Where(p => p.Role == "North").ToArray();
            Assert.AreEqual(count, north.Length);
            Assert.AreEqual(0, north[0].Position.x);
            Assert.AreEqual(6, north[0].Position.y);
            Assert.AreEqual(8f, north.Last().Position.x + north.Last().Sprite.rect.width / 32);
            Assert.AreEqual(48, result.Geometry.Cells.Length);
        }
        [Test] public void ReservationsOffsetsAndPivotAreExplicitAndImpossibleSpanFails()
        {
            using var f = new RoomAuthoringTestFixtures();
            var g = RoomBoundaryExtractor.Extract(RoomAuthoringTestFixtures.Cells());
            f.Style.NorthReservationsPixels = new Vector2Int(32, 32);
            f.Style.NorthWalls[0].AnchorPixels = new Vector2(8, 0);
            f.Style.NorthWalls[0].OffsetPixels = new Vector2(4, 0);
            Assert.IsTrue(RoomPlacementBuilder.TryBuild(g, f.Style, out var result, out var issue), issue);
            var north = result.Sprites.Where(p => p.Role == "North").ToArray();
            Assert.AreEqual(3, north.Length);
            Assert.AreEqual(0.875f, north[0].Position.x);
            f.Style.NorthReservationsPixels = new Vector2Int(32, 0);
            Assert.IsFalse(RoomPlacementBuilder.TryBuild(g, f.Style, out _, out issue));
            Assert.That(issue, Does.Contain("exactly coverable"));
        }
    }
}
