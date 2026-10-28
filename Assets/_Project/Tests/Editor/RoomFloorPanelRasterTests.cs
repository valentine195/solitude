using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Editor.RoomAuthoring;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Tests.RoomAuthoring
{
    public sealed class RoomFloorPanelRasterTests
    {
        private static readonly Color32 Ink = new Color32(24, 48, 72, 128);
        private static RoomFloorPanelSettings Settings(int thickness = 1) => new RoomFloorPanelSettings
            { Enabled = true, SeamThickness = thickness, SeamPalette = new[] { (Color)Ink } };
        private static readonly RectInt[] Tee = { new RectInt(0, 0, 32, 64), new RectInt(32, 0, 32, 32), new RectInt(32, 32, 32, 32) };

        private static HashSet<Vector2Int> Eligible(IEnumerable<RectInt> regions)
        {
            var pixels = new HashSet<Vector2Int>();
            foreach (var r in regions)
                for (var y = r.y; y < r.y + r.height; y++) for (var x = r.x; x < r.x + r.width; x++) pixels.Add(new Vector2Int(x, y));
            return pixels;
        }

        // Independent whole-domain inverse oracle. It tests each eligible pixel against raw border inequalities;
        // no production seam union, raster/chunk helper or forward stroke loop supplies the expected coverage.
        private static Dictionary<Vector2Int, Color32> Oracle(IEnumerable<RectInt> panels, IEnumerable<RectInt> floor, RoomFloorPanelSettings settings)
        {
            var eligible = Eligible(floor); var lines = new List<(bool horizontal, int axis, int start, int end)>();
            foreach (var r in panels)
            {
                lines.Add((true, r.y, r.x, r.x + r.width)); lines.Add((true, r.y + r.height, r.x, r.x + r.width));
                lines.Add((false, r.x, r.y, r.y + r.height)); lines.Add((false, r.x + r.width, r.y, r.y + r.height));
            }
            var result = new Dictionary<Vector2Int, Color32>();
            foreach (var p in eligible)
            {
                var painted = lines.Any(line =>
                {
                    var along = line.horizontal ? p.x : p.y; var across = line.horizontal ? p.y : p.x;
                    if (settings.SeamThickness == 1)
                    {
                        if (along < line.start || along > line.end) return false;
                        return across == line.axis || across == line.axis - 1 &&
                            !eligible.Contains(line.horizontal ? new Vector2Int(along, line.axis) : new Vector2Int(line.axis, along));
                    }
                    if (settings.SeamThickness == 2)
                        return across >= line.axis - 1 && across <= line.axis && along >= line.start - 1 && along <= line.end;
                    return across >= line.axis - 1 && across <= line.axis + 1 && along >= line.start - 1 && along <= line.end + 1;
                });
                if (!painted) continue;
                var index = ((long)p.x + p.y) % settings.SeamPalette.Length; if (index < 0) index += settings.SeamPalette.Length;
                result[p] = (Color32)settings.SeamPalette[index];
            }
            if (settings.SeamThickness == 1)
            {
                // Inverse corner oracle: fill one eligible common neighbor only for diagonal samples
                // lacking an existing orthogonal connection. Tie order is lower y, then lower x.
                var direct = new HashSet<Vector2Int>(result.Keys); var bridges = new HashSet<Vector2Int>();
                foreach (var a in direct)
                    foreach (var step in new[] { new Vector2Int(1, 1), new Vector2Int(1, -1) })
                    {
                        var b = a + step; if (!direct.Contains(b)) continue;
                        var cross = new[] { new Vector2Int(a.x, b.y), new Vector2Int(b.x, a.y) };
                        if (cross.Any(direct.Contains)) continue;
                        var available = cross.Where(eligible.Contains).OrderBy(p => p.y).ThenBy(p => p.x).ToArray();
                        if (available.Length != 0) bridges.Add(available[0]);
                    }
                foreach (var p in bridges)
                {
                    var index = ((long)p.x + p.y) % settings.SeamPalette.Length; if (index < 0) index += settings.SeamPalette.Length;
                    result[p] = (Color32)settings.SeamPalette[index];
                }
            }
            return result;
        }

        private static Dictionary<Vector2Int, Color32> Assemble(RoomFloorPanelRaster raster, IEnumerable<RectInt> floor)
        {
            Assert.IsTrue(raster.Success, string.Join("\n", raster.Issues.Select(i => i.Reason)));
            var eligible = Eligible(floor); var result = new Dictionary<Vector2Int, Color32>(); var chunkPixels = new HashSet<Vector2Int>();
            foreach (var chunk in raster.Chunks)
            {
                Assert.AreEqual(chunk.Size * chunk.Size, chunk.Pixels.Count);
                for (var y = 0; y < chunk.Size; y++) for (var x = 0; x < chunk.Size; x++)
                {
                    var p = chunk.Origin + new Vector2Int(x, y); Assert.IsTrue(chunkPixels.Add(p), "Chunk images must not overlap.");
                    var color = chunk.Pixels[y * chunk.Size + x];
                    if (color.a == 0) { Assert.AreEqual(new Color32(0, 0, 0, 0), color); continue; }
                    if (!eligible.Contains(p)) Assert.Fail("Raster leaked outside eligible floor at " + p);
                    result.Add(p, color);
                }
            }
            return result;
        }

        private static void EqualPixels(Dictionary<Vector2Int, Color32> expected, Dictionary<Vector2Int, Color32> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var pair in expected)
            {
                Assert.IsTrue(actual.TryGetValue(pair.Key, out var color), "Missing seam at " + pair.Key); Assert.AreEqual(pair.Value, color, "Pixel " + pair.Key);
            }
        }

        [Test] public void LongEdgeAndMultipleSmallPanelsProduceOneCanonicalTeeNetwork()
        {
            var network = RoomFloorPanelSeams.Extract("tee", Tee); Assert.IsTrue(network.Success); Assert.AreEqual(6, network.Segments.Count);
            string Describe(RoomFloorPanelSeam s) => s.Start.x + "," + s.Start.y + ":" + s.End.x + "," + s.End.y;
            CollectionAssert.AreEqual(new[] { "0,0:64,0", "32,32:64,32", "0,64:64,64", "0,0:0,64", "32,0:32,64", "64,0:64,64" }, network.Segments.Select(Describe));
            Assert.AreEqual(352, network.Segments.Sum(s => s.End.x - s.Start.x + s.End.y - s.Start.y));
            CollectionAssert.AreEqual(network.Segments.Select(Describe), RoomFloorPanelSeams.Extract("tee", Tee.Reverse()).Segments.Select(Describe));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void TeeAndPerimeterUseExactDeclaredPixelsWithNoAlphaAccumulation(int thickness)
        {
            var settings = Settings(thickness); var network = RoomFloorPanelSeams.Extract("tee", Tee);
            var raster = RoomFloorPanelRasterizer.Rasterize("tee", network, settings);
            var actual = Assemble(raster, Tee); EqualPixels(Oracle(Tee, Tee, settings), actual);
            Assert.AreEqual(32, raster.PixelsPerUnit); Assert.IsTrue(actual.Values.All(c => c.a == 128));
            Assert.IsFalse(actual.ContainsKey(new Vector2Int(16, 16)), "Panel interiors remain transparent.");
            Assert.IsTrue(actual.ContainsKey(new Vector2Int(0, 16))); Assert.IsTrue(actual.ContainsKey(new Vector2Int(63, 16)));
            if (thickness == 1)
            {
                Assert.IsTrue(actual.ContainsKey(new Vector2Int(32, 16))); Assert.IsFalse(actual.ContainsKey(new Vector2Int(31, 16)));
            }
            else if (thickness == 2)
            {
                Assert.IsTrue(actual.ContainsKey(new Vector2Int(31, 16))); Assert.IsTrue(actual.ContainsKey(new Vector2Int(32, 16)));
                Assert.IsFalse(actual.ContainsKey(new Vector2Int(33, 16)), "Even thickness has negative-axis bias.");
            }
            else Assert.IsTrue(actual.ContainsKey(new Vector2Int(33, 16)));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void AssembledChunksMatchWholeDomainOracleAtSeveralOriginsAndSizes(int thickness)
        {
            var network = RoomFloorPanelSeams.Extract("tee", Tee); var settings = Settings(thickness);
            settings.SeamPalette = new[] { (Color)Ink, (Color)new Color32(70, 80, 90, 96) }; var expected = Oracle(Tee, Tee, settings);
            foreach (var size in new[] { 8, 16, 32, 64, 256 })
                foreach (var origin in new[] { Vector2Int.zero, new Vector2Int(3, -5), new Vector2Int(-17, 11) })
                {
                    var raster = RoomFloorPanelRasterizer.Rasterize("tee", network, settings, chunkSize: size, chunkOrigin: origin);
                    EqualPixels(expected, Assemble(raster, Tee)); Assert.LessOrEqual(raster.Chunks.Count, 128); Assert.LessOrEqual(raster.AllocatedPixels, 8388608);
                }
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void NegativeHoleReservationAndThresholdRegionsRemainTransparent(int thickness)
        {
            // Explicit isolated floor/exclusion fixture; no collider-derived equipment truth.
            var cells = RoomAuthoringTestFixtures.Cells(5, 4).Where(c => c != new Vector2Int(1, 1) &&
                c != new Vector2Int(3, 1) && c != new Vector2Int(3, 2) && !(c.y == 3 && (c.x == 1 || c.x == 2)))
                .Select(c => c + new Vector2Int(-3, -2)).ToArray();
            var floor = cells.Select(c => new RectInt(c.x * 32, c.y * 32, 32, 32)).ToArray();
            var settings = Settings(thickness); settings.Seed = 19;
            var partition = RoomFloorPanelPartitioner.Partition("negative", floor, settings); Assert.IsTrue(partition.Success);
            var before = string.Join(";", partition.Rectangles); var network = RoomFloorPanelSeams.Extract("negative", partition);
            var expected = Oracle(partition.Rectangles, floor, settings);
            foreach (var size in new[] { 32, 64, 256 })
                EqualPixels(expected, Assemble(RoomFloorPanelRasterizer.Rasterize("negative", network, floor, settings, size, new Vector2Int(3, -5)), floor));
            Assert.AreEqual(before, string.Join(";", partition.Rectangles));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void FinalThickCoverageClipsAgainstIndependentExclusionMask(int thickness)
        {
            var panels = new[] { new RectInt(0, 0, 64, 64) }; var mask = new[] { new RectInt(0, 0, 32, 64) };
            var settings = Settings(thickness); var network = RoomFloorPanelSeams.Extract("clip", panels);
            EqualPixels(Oracle(panels, mask, settings), Assemble(RoomFloorPanelRasterizer.Rasterize("clip", network, mask, settings), mask));
        }

        [Test] public void OnePixelConcaveCornerHasAnEligibleFourConnectedJoin()
        {
            var panels = new[] { new RectInt(0, 0, 32, 64), new RectInt(32, 0, 32, 32) };
            var settings = Settings(); var network = RoomFloorPanelSeams.Extract("concave", panels);
            var actual = Assemble(RoomFloorPanelRasterizer.Rasterize("concave", network, settings), panels);
            EqualPixels(Oracle(panels, panels, settings), actual); Assert.IsTrue(actual.ContainsKey(new Vector2Int(31, 31)));
            var remaining = new HashSet<Vector2Int>(actual.Keys); var pending = new Queue<Vector2Int>();
            var start = remaining.First(); pending.Enqueue(start); remaining.Remove(start);
            while (pending.Count != 0)
            {
                var p = pending.Dequeue();
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (remaining.Remove(p + step)) pending.Enqueue(p + step);
            }
            Assert.IsEmpty(remaining, "The seam network must connect through orthogonal pixels at this concave corner.");
        }

        [Test] public void AppearanceAndChunkCopiesDoNotMutatePartitionOrCanonicalSeams()
        {
            var settings = Settings(); var partition = RoomFloorPanelPartitioner.Partition("appearance", Tee, settings);
            var rectangles = string.Join(";", partition.Rectangles); var network = RoomFloorPanelSeams.Extract("appearance", partition);
            var segments = network.Segments.Select(s => s.Start + ":" + s.End).ToArray();
            var first = RoomFloorPanelRasterizer.Rasterize("appearance", network, settings); var copy = first.Chunks[0].CopyPixels();
            var original = first.Chunks[0].Pixels[0]; copy[0] = new Color32(255, 0, 255, 255); Assert.AreEqual(original, first.Chunks[0].Pixels[0]);
            settings.SeamThickness = 3; settings.SeamPalette = new[] { Color.red };
            var changed = RoomFloorPanelRasterizer.Rasterize("appearance", network, settings); Assert.IsTrue(changed.Success);
            Assert.AreEqual(rectangles, string.Join(";", partition.Rectangles)); CollectionAssert.AreEqual(segments, network.Segments.Select(s => s.Start + ":" + s.End));
            EqualPixels(Oracle(partition.Rectangles, partition.Rectangles, settings), Assemble(changed, partition.Rectangles));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void NativeReviewShapesMatchIndependentPixelsAcrossTextureBorders(int thickness)
        {
            // Native-pixel fixtures, independent of chunk boundaries. The final two masks
            // represent already excluded doorway/reservation regions, not missing base floor.
            var shapes = new[]
            {
                new[] { new RectInt(-32, 0, 576, 32) },
                new[] { new RectInt(-32, -32, 64, 96), new RectInt(32, -32, 64, 32) },
                new[] { new RectInt(-32, 0, 576, 32), new RectInt(224, 32, 64, 64) },
                new[] { new RectInt(-32, 0, 576, 32), new RectInt(224, -64, 64, 64), new RectInt(224, 32, 64, 64) },
                new[] { new RectInt(-32, 0, 288, 32), new RectInt(256, -32, 288, 96) },
                new[] { new RectInt(0, 0, 256, 8), new RectInt(256, 0, 8, 8) },
                new[] { new RectInt(0, -32, 96, 32), new RectInt(0, 32, 96, 32) },
                new[] { new RectInt(-32, 0, 32, 96), new RectInt(32, 0, 32, 96) }
            };
            foreach (var floor in shapes)
            {
                var settings = Settings(thickness); settings.Seed = 18;
                var partition = RoomFloorPanelPartitioner.Partition("native-review", floor, settings);
                Assert.IsTrue(partition.Success, string.Join("\n", partition.Issues.Select(i => i.Reason)));
                var network = RoomFloorPanelSeams.Extract("native-review", partition);
                var expected = Oracle(partition.Rectangles, floor, settings);
                foreach (var origin in new[] { Vector2Int.zero, new Vector2Int(-8, 8) })
                    EqualPixels(expected, Assemble(RoomFloorPanelRasterizer.Rasterize("native-review", network,
                        floor, settings, 256, origin), floor));
                if (floor.Sum(r => r.width * r.height) == 264 * 8)
                    Assert.IsTrue(partition.Rectangles.All(r => r.height == 8), "Eight-pixel remnants remain native-sized.");
            }
        }

        [TestCase("overlap")] [TestCase("coordinates")] [TestCase("area")]
        public void InvalidPartitionCannotProducePartialSeamsOrPixels(string invalid)
        {
            var rectangles = invalid == "overlap" ? new[] { new RectInt(0, 0, 32, 32), new RectInt(16, 0, 32, 32) } :
                invalid == "coordinates" ? new[] { new RectInt(-7, 0, 32, 32) } :
                new[] { new RectInt(0, 0, (RoomFloorPanelPartitioner.MaximumQuanta + 1) * 8, 8) };
            var network = RoomFloorPanelSeams.Extract("invalid", rectangles); Assert.IsFalse(network.Success); Assert.IsEmpty(network.Segments);
            var raster = RoomFloorPanelRasterizer.Rasterize("invalid", network, Settings());
            Assert.IsFalse(raster.Success); Assert.IsEmpty(raster.Chunks); Assert.AreEqual(0, raster.AllocatedPixels);
        }

        [TestCase("chunks")] [TestCase("pixels")] [TestCase("bounds")] [TestCase("thickness")] [TestCase("alpha")]
        public void ResourceAndConfigurationFailureReturnsNoPixelBuffers(string failure)
        {
            var rectangles = failure == "chunks" ? Enumerable.Range(0, 129).Select(x => new RectInt(x * 256, 0, 8, 8)).ToArray() :
                failure == "bounds" ? new[] { new RectInt(int.MaxValue - 15, 0, 8, 8) } : new[] { new RectInt(0, 0, 8, 8) };
            var network = RoomFloorPanelSeams.Extract("limits", rectangles); Assert.IsTrue(network.Success);
            var settings = Settings(); if (failure == "thickness") settings.SeamThickness = 4;
            if (failure == "alpha") settings.SeamPalette[0] = new Color(0, 0, 0, .0001f);
            var raster = RoomFloorPanelRasterizer.Rasterize("limits", network, settings, maximumPixels: failure == "pixels" ? 65535 : RoomFloorPanelRasterizer.MaximumPixels);
            Assert.IsFalse(raster.Success); Assert.IsEmpty(raster.Chunks); Assert.AreEqual(0, raster.AllocatedPixels);
            Assert.AreEqual("limits", raster.Issues.Single().RoomId); Assert.That(raster.Issues[0].Field, Does.StartWith("FloorPanels/Raster/"));
        }

        [Test] public void SparseFarApartRegionsDoNotAllocateTheirBoundingBoxAndDisabledOutputIsEmpty()
        {
            var rectangles = new[] { new RectInt(-1000000000, -1000000000, 8, 8), new RectInt(1000000000, 1000000000, 8, 8) };
            var settings = Settings(); var network = RoomFloorPanelSeams.Extract("sparse", rectangles);
            var raster = RoomFloorPanelRasterizer.Rasterize("sparse", network, settings);
            Assert.AreEqual(2, raster.Chunks.Count); Assert.AreEqual(131072, raster.AllocatedPixels);
            EqualPixels(Oracle(rectangles, rectangles, settings), Assemble(raster, rectangles));
            settings.Enabled = false; var disabled = RoomFloorPanelRasterizer.Rasterize("sparse", network, settings); Assert.IsTrue(disabled.Success); Assert.IsEmpty(disabled.Chunks);
            Assert.IsTrue(RoomFloorPanelSeams.Extract("empty", Array.Empty<RectInt>()).Success);
            Assert.IsEmpty(RoomFloorPanelRasterizer.Rasterize("empty", RoomFloorPanelSeams.Extract("empty", Array.Empty<RectInt>()), Settings()).Chunks);
        }
    }
}
