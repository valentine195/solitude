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

        [TestCase("rectangle", 28)]
        [TestCase("L", 12)]
        [TestCase("U", 16)]
        [TestCase("hole", 16)]
        [TestCase("negative", 28)]
        public void CanonicalBoundaryResolutionPreservesSingleOwnerGeometry(string shape, int perimeter)
        {
            var cells = shape == "L" ? new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.right * 2, Vector2Int.up, Vector2Int.up * 2 } :
                shape == "U" ? RoomAuthoringTestFixtures.Cells(3, 3).Where(c => c.y == 0 || c.x != 1).ToArray() :
                shape == "hole" ? RoomAuthoringTestFixtures.Cells(3, 3).Where(c => c != Vector2Int.one).ToArray() : RoomAuthoringTestFixtures.Cells();
            var offset = shape == "negative" ? new Vector2Int(-7, -4) : Vector2Int.zero;
            var topology = ShipLayoutTopology.Build(new[] { new ShipSpaceCells("room", cells, offset) });
            var resolved = LayoutBoundaryResolver.Resolve(topology);
            Assert.AreEqual(perimeter, resolved.Edges.Count(e => e.Kind == LayoutEdgeKind.Exterior));
            Assert.AreEqual(perimeter, resolved.Spans.Sum(s => s.Length));
            Assert.IsFalse(resolved.Edges.Any(e => e.Kind == LayoutEdgeKind.Partition));
            var old = RoomBoundaryExtractor.Extract(cells.Select(c => c + offset));
            CollectionAssert.AreEqual(old.Cells, resolved.Exterior.Cells);
            CollectionAssert.AreEqual(old.Runs, resolved.Exterior.Runs);
            CollectionAssert.AreEqual(old.Junctions, resolved.Exterior.Junctions);
        }
    }
}
