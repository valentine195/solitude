using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomIrregularLayoutTests
    {
        private static string Edge(RoomSide side, Vector2Int start, int length) => side + ":" + start.x + "," + start.y + ":" + length;
        private static string Join(RoomJunction join) => join.Role + ":" + join.Position.x + "," + join.Position.y;
        private static Vector2Int[] Cells(string shape)
        {
            var all = RoomAuthoringTestFixtures.Cells();
            switch (shape)
            {
                case "rectangle": return all;
                case "notch": return all.Where(c => c != new Vector2Int(7, 5)).ToArray();
                case "hole": return all.Where(c => c != new Vector2Int(6, 4)).ToArray();
                case "L": return new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up };
                case "U": return RoomAuthoringTestFixtures.Cells(3, 3).Where(c => c.x != 1 || c.y == 0).ToArray();
                default: throw new ArgumentException(shape);
            }
        }

        [TestCase("rectangle")]
        [TestCase("notch")]
        [TestCase("hole")]
        [TestCase("L")]
        [TestCase("U")]
        public void GeometryMatchesExplicitRunAndJoinOracleRegardlessOfOrderOrTranslation(string shape)
        {
            var edges = new List<string>();
            var joins = new List<string>();
            if (shape == "rectangle" || shape == "hole")
            {
                edges.AddRange(new[] { "North:0,6:8", "South:0,0:8", "East:8,0:6", "West:0,0:6" });
                joins.AddRange(new[] { "Southwest:0,0", "Southeast:8,0", "Northwest:0,6", "Northeast:8,6" });
            }
            if (shape == "hole")
            {
                edges.AddRange(new[] { "North:6,4:1", "South:6,5:1", "East:6,4:1", "West:7,4:1" });
                joins.AddRange(new[] { "ConcaveNortheast:6,4", "ConcaveNorthwest:7,4", "ConcaveSoutheast:6,5", "ConcaveSouthwest:7,5" });
            }
            if (shape == "notch")
            {
                edges.AddRange(new[] { "North:0,6:7", "North:7,5:1", "South:0,0:8", "East:7,5:1", "East:8,0:5", "West:0,0:6" });
                joins.AddRange(new[] { "Southwest:0,0", "Southeast:8,0", "Northeast:8,5", "ConcaveNortheast:7,5", "Northeast:7,6", "Northwest:0,6" });
            }
            if (shape == "L")
            {
                edges.AddRange(new[] { "North:0,2:1", "North:1,1:1", "South:0,0:2", "East:1,1:1", "East:2,0:1", "West:0,0:2" });
                joins.AddRange(new[] { "Southwest:0,0", "Southeast:2,0", "Northeast:2,1", "ConcaveNortheast:1,1", "Northeast:1,2", "Northwest:0,2" });
            }
            if (shape == "U")
            {
                edges.AddRange(new[] { "North:0,3:1", "North:1,1:1", "North:2,3:1", "South:0,0:3", "West:0,0:3", "West:2,1:2", "East:1,1:2", "East:3,0:3" });
                joins.AddRange(new[] { "Southwest:0,0", "Southeast:3,0", "Northeast:3,3", "Northwest:2,3", "ConcaveNorthwest:2,1", "ConcaveNortheast:1,1", "Northeast:1,3", "Northwest:0,3" });
            }
            var cells = Cells(shape);
            foreach (var offset in new[] { Vector2Int.zero, new Vector2Int(-17, -9) })
            {
                var input = cells.Reverse().Concat(cells).Select(c => c + offset).ToArray();
                var geometry = RoomBoundaryExtractor.Extract(input);
                CollectionAssert.AreEquivalent(edges, geometry.Runs.Select(r => Edge(r.Side, r.Start - offset, r.Length)));
                CollectionAssert.AreEquivalent(joins, geometry.Junctions.Select(j => Join(new RoomJunction(j.Kind, j.Corner, j.Position - offset))));
                Assert.AreEqual(cells.Length, geometry.Cells.Length);
                foreach (var run in geometry.Runs)
                {
                    Assert.AreEqual(run.Start, run.StartJoin.Position);
                    Assert.AreEqual(run.End, run.EndJoin.Position);
                    Assert.IsTrue(run.StartJoin.HasSide(run.Side));
                    Assert.IsTrue(run.EndJoin.HasSide(run.Side));
                }
                var again = RoomBoundaryExtractor.Extract(input.Reverse());
                CollectionAssert.AreEqual(geometry.Runs, again.Runs);
                CollectionAssert.AreEqual(geometry.Junctions, again.Junctions);
            }
        }

        [TestCase("notch", 5, 1)]
        [TestCase("hole", 4, 4)]
        public void ProductionRebuildFollowsFortySevenCellsAndActualJoins(string shape, int convex, int concave)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = f.Room();
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var removed = shape == "notch" ? new Vector3Int(7, 5, 0) : new Vector3Int(6, 4, 0);
            Assert.IsTrue(RoomAuthoringOperations.PaintCell(room, removed, null), room.GenerationDiagnostic);
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
            var geometry = RoomBoundaryExtractor.Extract(Cells(shape));
            var sprites = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>();
            Assert.AreEqual(convex, geometry.ConvexCorners.Length);
            Assert.AreEqual(concave, sprites.Count(r => r.name.StartsWith("Concave")));
            foreach (var join in geometry.Junctions)
                Assert.AreEqual(1, sprites.Count(r => r.name == join.Role && r.transform.localPosition == (Vector3)(Vector2)join.Position));
            if (shape == "notch") Assert.IsFalse(sprites.Any(r => r.name == "Northeast" && r.transform.localPosition == new Vector3(8, 6, 0)));
            Assert.AreEqual(geometry.Runs.Sum(r => r.Length) + geometry.Junctions.Length, sprites.Length);
            Assert.IsTrue(sprites.All(r => r.transform.localScale == Vector3.one));
            Assert.IsEmpty(room.GenerationResult.Issues);
            var after = RoomUndoRedoTests.Snapshot(room);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            CollectionAssert.AreEqual(after, RoomUndoRedoTests.Snapshot(room));
        }

        [Test] public void HoleWithTwoCellNorthRepeatKeepsIndependentOuterArchitecture()
        {
            using var f = new RoomAuthoringTestFixtures(64);
            var room = f.Room(); room.Occupancy.SetTile(new Vector3Int(6, 4, 0), null);
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
            var north = room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "North").ToArray();
            Assert.AreEqual(4, north.Length);
            Assert.IsTrue(north.All(r => r.transform.localPosition.y == 6));
            var issue = room.GenerationResult.Issues.Single();
            Assert.AreEqual(RoomIssueKind.Span, issue.Kind);
            Assert.AreEqual(new Vector2Int(6, 4), issue.Start);
            Assert.AreEqual(new Vector2Int(7, 4), issue.End);
        }

        [TestCase("missing")]
        [TestCase("reservation")]
        [TestCase("dependent")]
        [TestCase("clearance")]
        public void LocalConcaveAndFitFaultsRetainFloorsAndIndependentBoundaries(string fault)
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = f.Room(); room.Occupancy.SetTile(new Vector3Int(6, 4, 0), null);
            if (fault == "missing" || fault == "dependent") f.Style.ConcaveNortheast.Sprite = null;
            if (fault == "reservation") f.Style.ConcaveNorthReservationsPixels = new Vector2Int(-1, 0);
            if (fault == "dependent") f.Style.ConcaveNorthReservationsPixels = new Vector2Int(32, 0);
            if (fault == "clearance") f.Style.NorthWallClearanceCells = 2;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(47, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(8, room.GeneratedOutput.GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "North" && r.transform.localPosition.y == 6));
            Assert.IsTrue(room.GenerationResult.Issues.All(i => i.HasRegion && i.Start.y == 4));
            if (fault == "missing") Assert.AreEqual(RoomIssueKind.Join, room.GenerationResult.Issues.Single().Kind);
            if (fault == "dependent") Assert.AreEqual(2, room.GenerationResult.Issues.Length);
            if (fault == "clearance") Assert.AreEqual(RoomIssueKind.VisualFit, room.GenerationResult.Issues.Single().Kind);
        }

        [Test] public void UnusedConcaveAssetsDoNotInvalidateLegacyRectangles()
        {
            using var f = new RoomAuthoringTestFixtures();
            f.Style.ConcaveSouthwest.Sprite = null; f.Style.ConcaveSoutheast.Sprite = null;
            f.Style.ConcaveNorthwest.Sprite = null; f.Style.ConcaveNortheast.Sprite = null;
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(f.Room()));
        }

        [Test] public void BridgeEraseAndDiagonalContactRetainIdentityAndSeparateBranches()
        {
            using var f = new RoomAuthoringTestFixtures(32);
            var room = f.Room(3, 1);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var id = room.RoomId;
            Assert.IsTrue(RoomAuthoringOperations.PaintCell(room, Vector3Int.right, null));
            Assert.AreEqual(id, room.RoomId);
            Assert.AreEqual(2, RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room)).ComponentCount);
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(room));
            room.Occupancy.ClearAllTiles();
            room.Occupancy.SetTile(Vector3Int.zero, f.Style.FloorTile);
            room.Occupancy.SetTile(new Vector3Int(1, 1, 0), f.Style.FloorTile);
            Assert.IsTrue(RoomAuthoringOperations.Rebuild(room));
            var geometry = RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room));
            Assert.AreEqual(2, geometry.ComponentCount);
            Assert.AreEqual(8, geometry.Junctions.Length);
            Assert.AreEqual(2, geometry.Junctions.Count(j => j.Position == Vector2Int.one));
            f.Style.SupportsDiagonalContact = false;
            Assert.IsFalse(RoomAuthoringOperations.Rebuild(room));
            Assert.AreEqual(2, RoomAuthoringTestFixtures.FloorCount(room));
            Assert.AreEqual(2, room.GenerationResult.Issues.Length);
            Assert.IsTrue(room.GenerationResult.Issues.All(i => i.Start == Vector2Int.one && i.Kind == RoomIssueKind.Join));
            Assert.AreEqual(id, room.RoomId);
        }
    }
}
