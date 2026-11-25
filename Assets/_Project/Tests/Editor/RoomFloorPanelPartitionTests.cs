using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelPartitionTests
    {
        private static RoomFloorPanelSettings Settings(int seed = 7, float density = .5f) =>
            new RoomFloorPanelSettings { Enabled = true, Seed = seed, Density = density };

        private static RectInt[] Regions(IEnumerable<Vector2Int> cells) => cells.Select(c => new RectInt(c.x * 32, c.y * 32, 32, 32)).ToArray();
        private static string Canonical(RoomFloorPanelPartition result) => string.Join(";", result.Rectangles.Select(r => r.x + "," + r.y + "," + r.width + "," + r.height));
        private static string Digest(string value) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", ""); }

        // Independent native-pixel oracle: no domain/partition helpers decide expected membership.
        private static void Coverage(RectInt[] expectedRegions, RoomFloorPanelPartition actual, RoomFloorPanelSettings settings)
        {
            Assert.IsTrue(actual.Success, string.Join("\n", actual.Issues.Select(i => i.Reason)));
            var expected = new HashSet<Vector2Int>();
            foreach (var r in expectedRegions)
                for (var y = r.y; y < r.y + r.height; y++) for (var x = r.x; x < r.x + r.width; x++) expected.Add(new Vector2Int(x, y));
            var visited = new HashSet<Vector2Int>();
            foreach (var r in actual.Rectangles)
            {
                Assert.Greater(r.width, 0); Assert.Greater(r.height, 0);
                Assert.LessOrEqual(r.width, settings.PreferredMaxPixels.x); Assert.LessOrEqual(r.height, settings.PreferredMaxPixels.y);
                Assert.AreEqual(0, r.x % 8); Assert.AreEqual(0, r.y % 8); Assert.AreEqual(0, r.width % 8); Assert.AreEqual(0, r.height % 8);
                for (var y = r.y; y < r.y + r.height; y++) for (var x = r.x; x < r.x + r.width; x++)
                {
                    var pixel = new Vector2Int(x, y);
                    if (!expected.Contains(pixel)) Assert.Fail("Out-of-domain pixel " + pixel + " in " + r);
                    if (!visited.Add(pixel)) Assert.Fail("Overlapping panel interiors at " + pixel);
                }
            }
            Assert.AreEqual(expected.Count, visited.Count, "Every eligible native pixel must be covered exactly once.");
            Assert.IsTrue(expected.SetEquals(visited));
        }

        private static Vector2Int[] Shape(string kind)
        {
            var cells = RoomAuthoringTestFixtures.Cells(8, 6).AsEnumerable();
            switch (kind)
            {
                case "L": cells = cells.Where(c => c.x < 3 || c.y < 2); break;
                case "U": cells = cells.Where(c => c.x < 2 || c.x >= 6 || c.y < 2); break;
                case "holes": cells = cells.Where(c => !(c.x >= 3 && c.x <= 4 && c.y >= 2 && c.y <= 3)); break;
                case "narrow": cells = cells.Where(c => c.x == 0); break;
                case "bend": cells = cells.Where(c => c.x == 0 || c.y == 0); break;
                case "T": cells = cells.Where(c => c.y >= 4 || c.x == 3); break;
                case "width": cells = cells.Where(c => c.x < 3 ? c.y < 2 : c.y < 4); break;
                case "reservation": cells = cells.Where(c => !(c.x >= 2 && c.x < 4 && c.y >= 1 && c.y < 3)); break;
                case "negative": cells = cells.Where(c => c.x < 3 || c.y < 2).Select(c => c + new Vector2Int(-11, -13)); break;
            }
            return cells.ToArray();
        }

        [TestCase("rectangle")] [TestCase("L")] [TestCase("U")] [TestCase("holes")] [TestCase("narrow")]
        [TestCase("bend")] [TestCase("T")] [TestCase("width")] [TestCase("negative")] [TestCase("reservation")]
        public void OrthogonalShapesHaveExactDisjointNativePixelCoverage(string shape)
        {
            var regions = Regions(Shape(shape)); var settings = Settings();
            var result = RoomFloorPanelPartitioner.Partition("space", regions, settings);
            Coverage(regions, result, settings);
            var again = RoomFloorPanelPartitioner.Partition("space", regions.Reverse(), settings);
            Assert.AreEqual(Canonical(result), Canonical(again));
            CollectionAssert.AreEqual(result.Advisories.Select(i => i.Id), again.Advisories.Select(i => i.Id));
        }

        [Test] public void ConstrainedEightPixelResidualsRemainCoveredAndLocated()
        {
            var regions = new[] { new RectInt(0, 0, 32, 40), new RectInt(32, 0, 8, 32) };
            var settings = Settings(); settings.PreferredMinPixels = Vector2Int.one * 64; settings.PreferredMaxPixels = Vector2Int.one * 96;
            var result = RoomFloorPanelPartitioner.Partition("residual-space", regions, settings);
            Coverage(regions, result, settings); Assert.IsTrue(result.Rectangles.Any(r => r.width == 8 || r.height == 8));
            Assert.IsTrue(result.Advisories.Any(i => i.HasRegion && i.RoomId == "residual-space" && i.Field.Contains("Residual/")));
            Assert.AreEqual(result.Advisories.Count, result.Advisories.Select(i => i.Id).Distinct().Count());
        }

        [Test] public void ExactCryobayFootprintProducesTenDistinctReproducibleSeededLayouts()
        {
            var cells = (from y in Enumerable.Range(-5, 9) from x in Enumerable.Range(-3, 7) select new Vector2Int(x, y))
                .Concat(from y in Enumerable.Range(-1, 5) from x in Enumerable.Range(-7, 4) select new Vector2Int(x, y)).ToArray();
            Assert.AreEqual(83, cells.Distinct().Count()); var regions = Regions(cells); var layouts = new HashSet<string>();
            for (var seed = 0; seed < 10; seed++)
            {
                var settings = Settings(seed); var output = RoomFloorPanelPartitioner.Partition("cryobay-fixture", regions, settings);
                Coverage(regions, output, settings); var geometry = Canonical(output); layouts.Add(geometry);
                var restored = JsonUtility.FromJson<RoomFloorPanelSettings>(JsonUtility.ToJson(settings));
                // An intervening unrelated evaluation cannot consume global random state or change this layout.
                RoomFloorPanelPartitioner.Partition("another-space", Regions(Shape("T")), Settings(seed + 101));
                var again = RoomFloorPanelPartitioner.Partition("cryobay-fixture", regions.OrderBy(r => r.x).ThenByDescending(r => r.y), restored);
                Assert.AreEqual(geometry, Canonical(again)); Assert.AreEqual(seed, again.Seed); Assert.AreEqual(1, again.Version);
                TestContext.WriteLine("Cryobay83 seed=" + seed + " panels=" + output.Rectangles.Count + " sha256=" + Digest(geometry) + " rectangles=" + geometry);
            }
            Assert.AreEqual(10, layouts.Count, "Every one of the ten recorded seeds must produce a distinct rectangle layout.");
        }

        private static int SharedLength(RoomFloorPanelPartition output)
        {
            var edges = new Dictionary<(int, int, bool), int>();
            void Edge(int x, int y, bool horizontal)
            {
                var key = (x, y, horizontal); edges.TryGetValue(key, out var n); edges[key] = n + 1;
            }
            foreach (var r in output.Rectangles)
            {
                for (var x = r.x; x < r.x + r.width; x++) { Edge(x, r.y, true); Edge(x, r.y + r.height, true); }
                for (var y = r.y; y < r.y + r.height; y++) { Edge(r.x, y, false); Edge(r.x + r.width, y, false); }
            }
            Assert.IsTrue(edges.Values.All(n => n == 1 || n == 2)); return edges.Count(pair => pair.Value == 2);
        }

        [Test] public void DensityChangesPanelCountsAndSharedLengthOnFeasibleFixture()
        {
            var regions = new[] { new RectInt(0, 0, 512, 512) }; var low = Settings(19, 0); var high = Settings(19, 1);
            var sparse = RoomFloorPanelPartitioner.Partition("density", regions, low); var dense = RoomFloorPanelPartitioner.Partition("density", regions, high);
            Coverage(regions, sparse, low); Coverage(regions, dense, high);
            var sparseLength = SharedLength(sparse); var denseLength = SharedLength(dense);
            TestContext.WriteLine("Density fixture512x512: low panels=" + sparse.Rectangles.Count + " sharedPixels=" + sparseLength +
                "; high panels=" + dense.Rectangles.Count + " sharedPixels=" + denseLength);
            Assert.Less(sparse.Rectangles.Count, dense.Rectangles.Count); Assert.Less(sparseLength, denseLength);
        }

        [TestCase(32)] [TestCase(64)]
        public void FixedOrConstrainedSizesReportDensitySaturation(int size)
        {
            var settings = Settings(); settings.PreferredMinPixels = settings.PreferredMaxPixels = Vector2Int.one * size;
            var regions = new[] { new RectInt(0, 0, 32, 32) }; var result = RoomFloorPanelPartitioner.Partition("fixed", regions, settings);
            Coverage(regions, result, settings); Assert.IsTrue(result.Advisories.Any(i => i.Field.EndsWith("DensitySaturated") && i.HasRegion));
        }

        [Test] public void SeamAppearanceAndSpaceLabelDoNotChangeRectanglesOrUnityRandomState()
        {
            var regions = Regions(Shape("L")); var settings = Settings(-981);
            var random = JsonUtility.ToJson(UnityEngine.Random.state); var result = RoomFloorPanelPartitioner.Partition("room", regions, settings);
            settings.SeamThickness = 3; settings.SeamPalette = new[] { Color.magenta, Color.gray };
            var appearance = RoomFloorPanelPartitioner.Partition("corridor", regions, settings);
            Assert.AreEqual(Canonical(result), Canonical(appearance)); Assert.AreEqual(random, JsonUtility.ToJson(UnityEngine.Random.state));
        }

        [Test] public void DomainAdapterUsesRealOwnerOffsetAndReservationExclusionsWithoutMutation()
        {
            using var f = new RoomAuthoringTestFixtures(32); var layout = RoomAuthoringCreation.CreateLayout(f.Scene, f.Style); var room = layout.Spaces[0];
            room.transform.localPosition = new Vector3(-9, -7, 0); ShipLayoutStyleTests.Fill(room, RoomAuthoringTestFixtures.Cells());
            var settings = Settings(81); room.SetFloorPanelAuthoring(settings, new[] { new RoomFloorReservation("equipment", room.RoomId, Vector2.one, new Vector2(2, 2)) });
            var serialized = EditorJsonUtility.ToJson(room); var dirty = f.Scene.isDirty; var group = Undo.GetCurrentGroup();
            var result = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room));
            var expected = RoomAuthoringTestFixtures.Cells().Where(c => !(c.x >= 1 && c.x <= 2 && c.y >= 1 && c.y <= 2)).Select(c => c + new Vector2Int(-9, -7));
            Coverage(Regions(expected), result, settings); Assert.AreEqual(serialized, EditorJsonUtility.ToJson(room)); Assert.AreEqual(dirty, f.Scene.isDirty); Assert.AreEqual(group, Undo.GetCurrentGroup());
            room.SetFloorPanelAuthoring(settings, new[] { new RoomFloorReservation("equipment", "wrong-owner", Vector2.one, Vector2.one) });
            var invalid = RoomFloorPanelPartitioner.Partition(RoomFloorPanelDomain.Build(room));
            Assert.IsFalse(invalid.Success); Assert.IsEmpty(invalid.Rectangles); Assert.That(invalid.Issues[0].Field, Does.Contain("Reservation/"));
        }

        [Test] public void EmptyDisabledAndOverlappingInputUnionHaveDefinedResults()
        {
            var settings = Settings(); Assert.IsEmpty(RoomFloorPanelPartitioner.Partition("empty", Array.Empty<RectInt>(), settings).Rectangles);
            settings.Enabled = false; var disabled = RoomFloorPanelPartitioner.Partition("disabled", new[] { new RectInt(0, 0, 32, 32) }, settings);
            Assert.IsTrue(disabled.Success); Assert.IsEmpty(disabled.Rectangles); settings.Enabled = true;
            var regions = new[] { new RectInt(0, 0, 32, 32), new RectInt(16, 0, 32, 32) };
            Coverage(regions, RoomFloorPanelPartitioner.Partition("union", regions.Reverse(), settings), settings);
        }

        [TestCase("version")] [TestCase("density")] [TestCase("range")] [TestCase("origin")] [TestCase("size")] [TestCase("overflow")]
        public void InvalidConfigurationOrCoordinatesYieldNoPartialPartition(string invalid)
        {
            var settings = Settings(); var region = new RectInt(0, 0, 32, 32);
            switch (invalid)
            {
                case "version": settings.Version = 2; break;
                case "density": settings.Density = float.NaN; break;
                case "range": settings.PreferredMinPixels = Vector2Int.zero; break;
                case "origin": region.x = -7; break;
                case "size": region.width = -8; break;
                case "overflow": region = new RectInt(int.MaxValue - 7, 0, 16, 8); break;
            }
            var result = RoomFloorPanelPartitioner.Partition("invalid", new[] { region }, settings);
            Assert.IsFalse(result.Success); Assert.IsEmpty(result.Rectangles); Assert.IsEmpty(result.Advisories);
            Assert.AreEqual("invalid", result.Issues.Single().RoomId);
        }

        [Test] public void AreaAndWorkLimitsWithholdAllOutputAndSparseCoordinatesAvoidBoundingBoxAllocation()
        {
            var settings = Settings(); var over = new[] { new RectInt(0, 0, (RoomFloorPanelPartitioner.MaximumQuanta + 1) * 8, 8) };
            var areaFailure = RoomFloorPanelPartitioner.Partition("large", over, settings);
            Assert.IsFalse(areaFailure.Success); Assert.IsEmpty(areaFailure.Rectangles); Assert.That(areaFailure.Issues[0].Field, Does.EndWith("Budget"));
            settings.PreferredMinPixels = settings.PreferredMaxPixels = Vector2Int.one * 8;
            var constrained = new[] { new RectInt(0, 0, 128, 128) };
            var workFailure = RoomFloorPanelPartitioner.Partition("work", constrained, settings, maximumWork: 600);
            Assert.IsFalse(workFailure.Success); Assert.IsEmpty(workFailure.Rectangles); Assert.IsTrue(workFailure.Issues[0].HasRegion);
            Assert.That(workFailure.Issues[0].Reason, Does.Contain("partial rectangles were withheld"));
            var success = RoomFloorPanelPartitioner.Partition("work", constrained, settings); Coverage(constrained, success, settings); Assert.AreEqual(256, success.Rectangles.Count);
            var sparse = new[] { new RectInt(-1000000000, -1000000000, 8, 8), new RectInt(1000000000, 1000000000, 8, 8) };
            Coverage(sparse, RoomFloorPanelPartitioner.Partition("sparse", sparse, Settings(), maximumQuanta: 2, maximumWork: 100), Settings());
        }
    }
}
