using System.Linq;
using NUnit.Framework;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomGeometryTests
    {
        [Test] public void RectangleHasDeterministicPerimeterAndFourCorners()
        {
            var cells = RoomAuthoringTestFixtures.Cells();
            var first = RoomBoundaryExtractor.Extract(cells);
            var second = RoomBoundaryExtractor.Extract(cells.Reverse());
            Assert.IsTrue(first.IsRectangle);
            Assert.AreEqual(48, first.Cells.Length);
            Assert.AreEqual(4, first.Runs.Length);
            Assert.AreEqual(4, first.ConvexCorners.Length);
            CollectionAssert.AreEqual(new[] { 8, 8, 6, 6 }, first.Runs.Select(r => r.Length));
            CollectionAssert.AreEqual(first.Cells, second.Cells);
            CollectionAssert.AreEqual(first.Runs, second.Runs);
            CollectionAssert.AreEqual(first.ConvexCorners, second.ConvexCorners);
        }
        [Test] public void ExteriorErasureAndExpansionUpdateCellSetGeometry()
        {
            var erased = RoomBoundaryExtractor.Extract(RoomAuthoringTestFixtures.Cells().Where(c => c != Vector2Int.zero));
            Assert.AreEqual(47, erased.Cells.Length);
            Assert.IsFalse(erased.IsRectangle);
            Assert.AreEqual(28, erased.Runs.Sum(r => r.Length));
            var expanded = RoomBoundaryExtractor.Extract(RoomAuthoringTestFixtures.Cells(12, 6));
            Assert.AreEqual(72, expanded.Cells.Length);
            CollectionAssert.AreEqual(new[] { 12, 12, 6, 6 }, expanded.Runs.Select(r => r.Length));
        }
    }
}
