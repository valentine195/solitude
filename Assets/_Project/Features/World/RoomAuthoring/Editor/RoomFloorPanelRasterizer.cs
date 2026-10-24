using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public sealed class RoomFloorPanelPixelChunk
    {
        private readonly Color32[] pixels;
        public Vector2Int Origin { get; }
        public int Size { get; }
        public IReadOnlyList<Color32> Pixels { get; }
        internal RoomFloorPanelPixelChunk(Vector2Int origin, int size, Color32[] buffer)
        { Origin = origin; Size = size; pixels = buffer; Pixels = Array.AsReadOnly(buffer); }
        public Color32[] CopyPixels() => (Color32[])pixels.Clone();
    }

    public sealed class RoomFloorPanelRaster
    {
        public IReadOnlyList<RoomFloorPanelPixelChunk> Chunks { get; }
        public IReadOnlyList<RoomGenerationIssue> Issues { get; }
        public bool Success => Issues.Count == 0;
        public long AllocatedPixels => Chunks.Sum(c => (long)c.Size * c.Size);
        public int PixelsPerUnit => RoomFloorPanelSettings.PixelsPerCell;
        internal RoomFloorPanelRaster(IEnumerable<RoomFloorPanelPixelChunk> chunks, IEnumerable<RoomGenerationIssue> issues)
        { Chunks = Array.AsReadOnly(chunks.ToArray()); Issues = Array.AsReadOnly(issues.ToArray()); }
    }

    public static class RoomFloorPanelRasterizer
    {
        public const int DefaultChunkSize = 256;
        public const int MaximumChunks = 128;
        public const long MaximumPixels = 8388608;

        public static RoomFloorPanelRaster Rasterize(string owner, RoomFloorPanelSeams seams, RoomFloorPanelSettings settings,
            int chunkSize = DefaultChunkSize, Vector2Int chunkOrigin = default, int maximumChunks = MaximumChunks, long maximumPixels = MaximumPixels) =>
            Rasterize(owner, seams, seams?.Rectangles, settings, chunkSize, chunkOrigin, maximumChunks, maximumPixels);

        public static RoomFloorPanelRaster Rasterize(string owner, RoomFloorPanelSeams seams, IEnumerable<RectInt> eligibleRegions,
            RoomFloorPanelSettings settings, int chunkSize = DefaultChunkSize, Vector2Int chunkOrigin = default,
            int maximumChunks = MaximumChunks, long maximumPixels = MaximumPixels)
        {
            RoomFloorPanelRaster Fail(string field, string reason) => new RoomFloorPanelRaster(Array.Empty<RoomFloorPanelPixelChunk>(),
                new[] { RoomFloorPanelSeams.Issue(owner, "Raster/" + field, reason) });
            RoomFloorPanelRaster Empty() => new RoomFloorPanelRaster(Array.Empty<RoomFloorPanelPixelChunk>(), Array.Empty<RoomGenerationIssue>());
            if (string.IsNullOrWhiteSpace(owner) || seams == null || settings == null || eligibleRegions == null)
                return Fail("Inputs", "Provide a space identity, seam network, eligible area and raster settings.");
            if (!seams.Success) return new RoomFloorPanelRaster(Array.Empty<RoomFloorPanelPixelChunk>(), seams.Issues);
            settings = settings.Copy(); var invalid = settings.ValidationIssue();
            if (invalid != null) return Fail("Settings", invalid);
            if (chunkSize < 8 || chunkSize > 1024 || chunkSize % 8 != 0 || maximumChunks <= 0 || maximumChunks > MaximumChunks ||
                maximumPixels <= 0 || maximumPixels > MaximumPixels)
                return Fail("Budget", "Use eight-pixel-multiple chunks from 8–1024 pixels, at most 128 chunks and at most 8,388,608 allocated pixels.");
            if (!settings.Enabled || seams.Segments.Count == 0) return Empty();
            var palette = settings.SeamPalette.Select(c => (Color32)c).ToArray();
            if (palette.Any(c => c.a == 0)) return Fail("Palette", "Seam alpha must remain positive after RGBA32 quantization.");
            var regions = new List<RectInt>(); long area = 0;
            foreach (var r in eligibleRegions)
            {
                if (r.width <= 0 || r.height <= 0 || r.x % 8 != 0 || r.y % 8 != 0 || r.width % 8 != 0 || r.height % 8 != 0 ||
                    (long)r.x + r.width > int.MaxValue || (long)r.y + r.height > int.MaxValue)
                    return Fail("Coordinates", "Eligible regions require positive eight-pixel dimensions/aligned origins and representable native endpoints.");
                area += (long)(r.width / 8) * (r.height / 8);
                if (area > RoomFloorPanelPartitioner.MaximumQuanta) return Fail("Budget", "Eligible area exceeds the bounded raster mask budget.");
                regions.Add(r);
            }
            // Count spatial chunks from the sparse region list. Never allocate a bounding-box image.
            var origins = new HashSet<Vector2Int>();
            foreach (var r in regions)
            {
                var firstX = Floor((long)r.x - chunkOrigin.x, chunkSize); var lastX = Floor((long)r.x + r.width - 1 - chunkOrigin.x, chunkSize);
                var firstY = Floor((long)r.y - chunkOrigin.y, chunkSize); var lastY = Floor((long)r.y + r.height - 1 - chunkOrigin.y, chunkSize);
                for (var y = firstY; y <= lastY; y++) for (var x = firstX; x <= lastX; x++)
                {
                    var px = (long)chunkOrigin.x + x * chunkSize; var py = (long)chunkOrigin.y + y * chunkSize;
                    if (px < int.MinValue || py < int.MinValue || px + chunkSize > int.MaxValue || py + chunkSize > int.MaxValue)
                        return Fail("Coordinates", "Spatial chunk bounds exceed the supported signed native-pixel range.");
                    origins.Add(new Vector2Int((int)px, (int)py));
                    if (origins.Count > maximumChunks || (long)origins.Count * chunkSize * chunkSize > maximumPixels)
                        return Fail("Budget", "Sparse eligible area exceeds the chunk/pixel allocation budget; no pixel buffers were allocated.");
                }
            }
            if (origins.Count == 0) return Empty();
            try
            {
                var mask = new HashSet<Vector2Int>();
                foreach (var r in regions)
                    for (var y = 0; y < r.height / 8; y++) for (var x = 0; x < r.width / 8; x++)
                        mask.Add(new Vector2Int(checked(r.x / 8 + x), checked(r.y / 8 + y)));
                bool Eligible(long x, long y) => x >= int.MinValue && x <= int.MaxValue && y >= int.MinValue && y <= int.MaxValue &&
                    mask.Contains(new Vector2Int((int)Floor(x, 8), (int)Floor(y, 8)));
                // All count/bounds checks precede the first pixel-buffer allocation.
                var buffers = origins.ToDictionary(p => p, p => new Color32[checked(chunkSize * chunkSize)]);
                void Paint(long x, long y)
                {
                    if (!Eligible(x, y)) return;
                    var origin = new Vector2Int(checked((int)((long)chunkOrigin.x + Floor(x - chunkOrigin.x, chunkSize) * chunkSize)),
                        checked((int)((long)chunkOrigin.y + Floor(y - chunkOrigin.y, chunkSize) * chunkSize)));
                    var index = checked((int)(y - origin.y) * chunkSize + (int)(x - origin.x));
                    // Global diagonal palette phase is independent of segment visitation, junction count and chunk origin.
                    var color = (int)((x + y) % palette.Length); if (color < 0) color += palette.Length;
                    buffers[origin][index] = palette[color]; // Assignment is a union, never alpha blending.
                }
                void Thin(RoomFloorPanelSeam seam)
                {
                    var previous = false; long previousX = 0, previousY = 0;
                    var first = seam.Horizontal ? seam.Start.x : seam.Start.y;
                    var last = seam.Horizontal ? seam.End.x : seam.End.y;
                    for (long along = first; along <= last; along++)
                    {
                        long x = seam.Horizontal ? along : seam.Start.x, y = seam.Horizontal ? seam.Start.y : along;
                        if (!Eligible(x, y)) { if (seam.Horizontal) y--; else x--; }
                        if (!Eligible(x, y)) { previous = false; continue; }
                        // An inward bias switch may otherwise leave only diagonal contact at a concave corner.
                        // Union one eligible orthogonal bridge, preferring lower native y when both fit.
                        if (previous && Math.Abs(x - previousX) == 1 && Math.Abs(y - previousY) == 1)
                        {
                            var firstX = y < previousY ? previousX : x; var firstY = Math.Min(y, previousY);
                            var otherX = y < previousY ? x : previousX; var otherY = Math.Max(y, previousY);
                            if (Eligible(firstX, firstY)) Paint(firstX, firstY); else Paint(otherX, otherY);
                        }
                        Paint(x, y); previous = true; previousX = x; previousY = y;
                    }
                }
                // Closed segments have square pixel caps. Bias: 1=[0], 2=[-1,0], 3=[-1,0,1].
                // At 1px a missing positive-side perimeter sample falls inward to the negative adjacent pixel.
                // Wider ribbons keep the fixed layout bias and are clipped to eligible floor, including exclusions.
                var low = settings.SeamThickness == 1 ? 0 : 1; var high = settings.SeamThickness == 3 ? 1 : 0;
                foreach (var seam in seams.Segments)
                {
                    if (settings.SeamThickness == 1)
                    { Thin(seam); }
                    else if (seam.Horizontal)
                        for (long y = (long)seam.Start.y - low; y <= (long)seam.Start.y + high; y++)
                            for (long x = (long)seam.Start.x - low; x <= (long)seam.End.x + high; x++) Paint(x, y);
                    else
                        for (long x = (long)seam.Start.x - low; x <= (long)seam.Start.x + high; x++)
                            for (long y = (long)seam.Start.y - low; y <= (long)seam.End.y + high; y++) Paint(x, y);
                }
                return new RoomFloorPanelRaster(buffers.OrderBy(p => p.Key.y).ThenBy(p => p.Key.x)
                    .Select(p => new RoomFloorPanelPixelChunk(p.Key, chunkSize, p.Value)), Array.Empty<RoomGenerationIssue>());
            }
            catch (OutOfMemoryException) { return Fail("Allocation", "Pixel allocation failed; all temporary buffers were withheld."); }
            catch (OverflowException) { return Fail("Coordinates", "Raster coordinates overflowed; all temporary buffers were withheld."); }
        }

        private static long Floor(long value, int divisor)
        { var quotient = value / divisor; return value % divisor < 0 ? quotient - 1 : quotient; }
    }
}
