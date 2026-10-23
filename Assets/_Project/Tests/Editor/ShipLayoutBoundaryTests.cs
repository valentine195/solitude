using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class ShipLayoutBoundaryTests
    {
        private static ShipLayoutAuthoring Pair(RoomAuthoringTestFixtures fixture, bool horizontal = false)
        {
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style, LogicalSpaceType.Corridor);
            var other = RoomAuthoringCreation.CreateSpace(layout, LogicalSpaceType.Corridor);
            other.transform.localPosition = horizontal ? Vector3.up * 4 : Vector3.right * 2;
            foreach (var room in layout.Spaces)
                foreach (var cell in RoomAuthoringTestFixtures.Cells(2, 4))
                    room.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), fixture.Style.FloorTile);
            return layout;
        }

        private static string[] Sprites(ShipLayoutAuthoring layout) => layout.GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.name + ":" + JsonUtility.ToJson(r.transform.position) + ":" + r.sortingOrder)
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();

        [TestCase(false)]
        [TestCase(true)]
        public void BothOwnersQueryOneCanonicalPartitionAndSameOwnerEdgesStayOpen(bool horizontal)
        {
            var first = new ShipSpaceCells("z", RoomAuthoringTestFixtures.Cells(2, 4), new Vector2Int(-4, -6));
            var second = new ShipSpaceCells("a", RoomAuthoringTestFixtures.Cells(2, 4), new Vector2Int(-4, -6) +
                (horizontal ? Vector2Int.up * 4 : Vector2Int.right * 2));
            var geometry = LayoutBoundaryResolver.Resolve(ShipLayoutTopology.Build(new[] { first, second }));
            var shared = geometry.Edges.Where(e => e.Kind == LayoutEdgeKind.Partition).ToArray();
            Assert.AreEqual(horizontal ? 2 : 4, shared.Length);
            foreach (var edge in shared)
            {
                var negative = edge.Address.Start - (horizontal ? Vector2Int.up : Vector2Int.right);
                var a = geometry.EdgeAt(negative, horizontal ? RoomSide.North : RoomSide.East);
                var b = geometry.EdgeAt(edge.Address.Start, horizontal ? RoomSide.South : RoomSide.West);
                Assert.AreEqual(a.Address, b.Address);
                Assert.AreEqual("z", a.NegativeOwner); Assert.AreEqual("a", a.PositiveOwner);
                Assert.AreEqual("a", a.VisualOwner);
                Assert.AreEqual(LayoutEdgeKind.Partition, a.Kind);
            }
            Assert.AreEqual(LayoutEdgeKind.Open, geometry.EdgeAt(new Vector2Int(-4, -6), RoomSide.East).Kind);
            Assert.AreEqual(1, geometry.Spans.Count(s => s.First.Kind == LayoutEdgeKind.Partition));
            var reversed = LayoutBoundaryResolver.Resolve(ShipLayoutTopology.Build(new[] { second, first }));
            CollectionAssert.AreEqual(geometry.Edges, reversed.Edges);
            CollectionAssert.AreEqual(geometry.Spans, reversed.Spans);
            Assert.AreEqual(geometry.Edges.Count(e => e.Kind != LayoutEdgeKind.Open), geometry.Spans.Sum(s => s.Length));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedPartitionRendersOnceAcrossRebuildOrderMemberOrderAndEditOrigin(bool horizontal)
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = Pair(fixture, horizontal);
            layout.transform.position = new Vector3(-10, -8, 0);
            var a = layout.Spaces[0]; var b = layout.Spaces[1];
            RoomAuthoringOperations.Rebuild(layout);
            var role = horizontal ? "PartitionNorth" : "PartitionEast";
            var partitions = layout.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == role).ToArray();
            Assert.AreEqual(horizontal ? 2 : 4, partitions.Length);
            var owner = horizontal ? a : string.CompareOrdinal(a.RoomId, b.RoomId) <= 0 ? a : b;
            Assert.IsTrue(partitions.All(r => r.transform.parent == owner.GeneratedOutput.transform));
            var expected = horizontal ? new[] { new Vector3(-10, -4, 0), new Vector3(-9, -4, 0) } :
                Enumerable.Range(0, 4).Select(y => new Vector3(-8, -8 + y, 0)).ToArray();
            CollectionAssert.AreEquivalent(expected, partitions.Select(r => r.transform.position));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => horizontal
                ? r.name == "North" && Mathf.Approximately(r.transform.position.y, -4)
                : r.name == "East" && Mathf.Approximately(r.transform.position.x, -8)));
            Assert.AreEqual(8, RoomAuthoringTestFixtures.FloorCount(a));
            Assert.AreEqual(8, RoomAuthoringTestFixtures.FloorCount(b));
            var snapshot = Sprites(layout); var result = JsonUtility.ToJson(layout.GenerationResult);
            var data = new SerializedObject(layout); data.FindProperty("spaces").MoveArrayElement(0, 1);
            data.ApplyModifiedPropertiesWithoutUndo();
            RoomAuthoringOperations.Rebuild(a); CollectionAssert.AreEqual(snapshot, Sprites(layout));
            Assert.AreEqual(result, JsonUtility.ToJson(layout.GenerationResult));
            RoomAuthoringOperations.Rebuild(b); CollectionAssert.AreEqual(snapshot, Sprites(layout));
            Assert.AreEqual(result, JsonUtility.ToJson(layout.GenerationResult));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
            Assert.AreEqual(RoomGenerationStatus.IncompletePreview, layout.GenerationResult.Status);
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Field == "PartitionEndpoint" && i.HasRegion));
        }

        [Test] public void ErasingAnAdjacentStripRemovesObsoletePartitionWithoutChangingOtherSpace()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = Pair(fixture); var a = layout.Spaces[0]; var b = layout.Spaces[1];
            RoomAuthoringOperations.Rebuild(layout);
            var before = Sprites(layout); var bCells = RoomAuthoringOperations.ReadCells(b);
            RoomAuthoringOperations.PaintBox(a, new BoundsInt(1, 0, 0, 1, 4, 1), null);
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name.StartsWith("Partition", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(bCells, RoomAuthoringOperations.ReadCells(b));
            Assert.AreEqual(8, RoomAuthoringTestFixtures.FloorCount(b));
            Assert.AreEqual(4, RoomAuthoringTestFixtures.FloorCount(a));
            Undo.PerformUndo();
            CollectionAssert.AreEqual(before, Sprites(layout));
            Assert.AreEqual(8, RoomAuthoringTestFixtures.FloorCount(a));
            CollectionAssert.AreEqual(bCells, RoomAuthoringOperations.ReadCells(b));
        }

        [Test] public void MissingPartitionArtWithholdsOnlyDependentOutputAndRetainsFloors()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = Pair(fixture);
            RoomAuthoringOperations.Rebuild(layout);
            fixture.Style.EastCap.Sprite = null;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(16, layout.Spaces.Sum(RoomAuthoringTestFixtures.FloorCount));
            Assert.IsFalse(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "PartitionEast"));
            Assert.IsTrue(layout.GetComponentsInChildren<SpriteRenderer>().Any(r => r.name == "North"));
            Assert.IsTrue(layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Asset && i.HasRegion && i.Start.x == 2 && i.End.x == 2));
            Assert.IsTrue(RoomAuthoringValidator.IsCurrent(layout));
        }

        [Test] public void WriterRejectsForeignPlacementBeforeDeletingOutput()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = Pair(fixture); RoomAuthoringOperations.Rebuild(layout);
            var room = layout.Spaces[0]; var other = layout.Spaces[1];
            var before = room.GeneratedOutput.GetComponentsInChildren<Transform>(true);
            var placement = RoomPlacementBuilder.BuildFloor(RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room)), fixture.Style);
            placement.Sprites.Add(new RoomSpritePlacement("PartitionEast", fixture.Style.EastCap.Sprite, Vector3.zero, other.RoomId));
            Assert.Throws<InvalidOperationException>(() => RoomGeometryWriter.Apply(room, placement));
            CollectionAssert.AreEqual(before, room.GeneratedOutput.GetComponentsInChildren<Transform>(true));
        }

        [Test] public void OverlapAndUnrepresentableBoundaryCannotSilentlyResolve()
        {
            var overlap = ShipLayoutTopology.Build(new[] { new ShipSpaceCells("a", new[] { Vector2Int.zero }, Vector2Int.zero),
                new ShipSpaceCells("b", new[] { Vector2Int.zero }, Vector2Int.zero) });
            Assert.Throws<InvalidOperationException>(() => LayoutBoundaryResolver.Resolve(overlap));
            var overflow = ShipLayoutTopology.Build(new[] { new ShipSpaceCells("a", new[] { new Vector2Int(int.MaxValue, 0) }, Vector2Int.zero) });
            Assert.Throws<OverflowException>(() => LayoutBoundaryResolver.Resolve(overflow));
        }

        [Test] public void EmptySecondMemberRetainsEquivalentRectanglePlacements()
        {
            using var fixture = new RoomAuthoringTestFixtures(32);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            var a = layout.Spaces[0];
            foreach (var cell in RoomAuthoringTestFixtures.Cells()) a.Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), fixture.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            var expected = Sprites(layout);
            RoomAuthoringCreation.CreateSpace(layout);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(layout));
            CollectionAssert.AreEqual(expected, Sprites(layout));
            Assert.AreEqual(48, RoomAuthoringTestFixtures.FloorCount(a));
        }

        [TestCase(4, 2)]
        [TestCase(3, 0)]
        public void TwoCellNorthRepeatRequiresExactCoverageWithoutPrefixFiller(int width, int expectedPanels)
        {
            using var fixture = new RoomAuthoringTestFixtures(64);
            var layout = RoomAuthoringCreation.CreateLayout(fixture.Scene, fixture.Style);
            RoomAuthoringCreation.CreateSpace(layout);
            foreach (var cell in RoomAuthoringTestFixtures.Cells(width, 3))
                layout.Spaces[0].Occupancy.SetTile(new Vector3Int(cell.x, cell.y, 0), fixture.Style.FloorTile);
            Assert.AreEqual(width % 2 == 0, RoomAuthoringOperations.Rebuild(layout));
            Assert.AreEqual(expectedPanels, layout.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "North"));
            Assert.AreEqual(width * 3, RoomAuthoringTestFixtures.FloorCount(layout.Spaces[0]));
            Assert.AreEqual(width % 2 != 0, layout.GenerationResult.Issues.Any(i => i.Kind == RoomIssueKind.Span && i.Field == "North"));
        }
    }
}
