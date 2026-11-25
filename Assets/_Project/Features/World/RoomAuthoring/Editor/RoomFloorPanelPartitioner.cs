using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public sealed class RoomFloorPanelPartition
    {
        public int Seed { get; }
        public int Version { get; }
        public IReadOnlyList<RectInt> Rectangles { get; }
        public IReadOnlyList<RoomGenerationIssue> Issues { get; }
        public IReadOnlyList<RoomGenerationIssue> Advisories { get; }
        public bool Success => Issues.Count == 0;

        internal RoomFloorPanelPartition(RoomFloorPanelSettings settings, IEnumerable<RectInt> rectangles,
            IEnumerable<RoomGenerationIssue> issues, IEnumerable<RoomGenerationIssue> advisories)
        {
            Seed = settings?.Seed ?? 0; Version = settings?.Version ?? 0;
            Rectangles = Array.AsReadOnly(rectangles.OrderBy(r => r.y).ThenBy(r => r.x).ThenBy(r => r.width).ThenBy(r => r.height).ToArray());
            Issues = Array.AsReadOnly(issues.ToArray()); Advisories = Array.AsReadOnly(advisories.ToArray());
        }
    }

    // Pure value geometry. Limits bound sparse input expansion and packing work, not a timing guarantee.
    // Version 1 fixes traversal, hash, target selection and tie rules; changing them requires a new version.
    public static class RoomFloorPanelPartitioner
    {
        public const int MaximumQuanta = 131072;
        public const long MaximumWork = 16777216;
        private const int Q = RoomFloorPanelSettings.Quantum;

        private sealed class WorkLimit : Exception { }

        public static RoomFloorPanelPartition Partition(RoomFloorPanelDomain domain)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));
            if (domain.Issues.Count != 0)
                return new RoomFloorPanelPartition(domain.Settings, Array.Empty<RectInt>(), domain.Issues, Array.Empty<RoomGenerationIssue>());
            return Partition(domain.SpaceId, domain.PixelRegions, domain.Settings);
        }

        // Regions describe the eligible-area union, not panels. The domain adapter supplies one region per floor cell.
        // This value-only entry point also permits independent geometry checks without Unity scenes or resources.
        public static RoomFloorPanelPartition Partition(string owner, IEnumerable<RectInt> regions, RoomFloorPanelSettings settings,
            int maximumQuanta = MaximumQuanta, long maximumWork = MaximumWork)
        {
            RoomGenerationIssue Diagnostic(string field, string reason, RectInt? pixels = null)
            {
                var issue = new RoomGenerationIssue(RoomIssueKind.Geometry, "FloorPanels/Partition/" + field, reason);
                if (pixels.HasValue)
                {
                    var r = pixels.Value; issue.HasRegion = true;
                    issue.Start = new Vector2Int((int)Math.Floor((double)r.x / 32), (int)Math.Floor((double)r.y / 32));
                    issue.End = new Vector2Int((int)Math.Ceiling(((double)r.x + r.width) / 32), (int)Math.Ceiling(((double)r.y + r.height) / 32));
                }
                issue.LocateInRoom(string.IsNullOrWhiteSpace(owner) ? "<missing-space>" : owner); return issue;
            }
            RoomFloorPanelPartition Fail(string field, string reason, RectInt? pixels = null) => new RoomFloorPanelPartition(settings,
                Array.Empty<RectInt>(), new[] { Diagnostic(field, reason, pixels) }, Array.Empty<RoomGenerationIssue>());
            if (string.IsNullOrWhiteSpace(owner)) return Fail("Owner", "Space identity is required.");
            if (settings == null || regions == null) return Fail("Inputs", "Provide panel settings and eligible pixel regions.");
            settings = settings.Copy();
            var configurationIssue = settings.ValidationIssue();
            if (configurationIssue != null) return Fail("Settings", configurationIssue);
            if (maximumQuanta <= 0 || maximumQuanta > MaximumQuanta || maximumWork <= 0 || maximumWork > MaximumWork)
                return Fail("Budget", "Packing budgets must be positive and cannot exceed " + MaximumQuanta + " quanta / " + MaximumWork + " work units.");
            if (!settings.Enabled) return new RoomFloorPanelPartition(settings, Array.Empty<RectInt>(), Array.Empty<RoomGenerationIssue>(), Array.Empty<RoomGenerationIssue>());
            var input = new List<RectInt>(); long area = 0;
            foreach (var region in regions)
            {
                if (region.width <= 0 || region.height <= 0 || region.x % Q != 0 || region.y % Q != 0 || region.width % Q != 0 || region.height % Q != 0 ||
                    (long)region.x + region.width > int.MaxValue || (long)region.y + region.height > int.MaxValue)
                    return Fail("Coordinates", "Eligible regions must have positive eight-pixel dimensions/aligned origins and representable native-pixel endpoints.");
                area += (long)(region.width / Q) * (region.height / Q);
                if (area > maximumQuanta || input.Count >= maximumQuanta)
                    return Fail("Budget", "Eligible input exceeds the " + maximumQuanta + "-quantum preflight budget; no partition was produced.");
                input.Add(region);
            }
            if (area * 2 + 1 > maximumWork) return Fail("Budget", "Work budget is below the minimum expansion/removal cost; no partition was produced.");
            if (input.Count == 0) return new RoomFloorPanelPartition(settings, Array.Empty<RectInt>(), Array.Empty<RoomGenerationIssue>(), Array.Empty<RoomGenerationIssue>());
            var remaining = new HashSet<Vector2Int>(); var rectangles = new List<RectInt>(); var advisories = new List<RoomGenerationIssue>();
            long work = 0; var current = new RectInt(input[0].x, input[0].y, Q, Q);
            void Charge() { if (++work > maximumWork) throw new WorkLimit(); }
            try
            {
                foreach (var region in input.OrderBy(r => r.y).ThenBy(r => r.x).ThenBy(r => r.width).ThenBy(r => r.height))
                    for (var y = 0; y < region.height / Q; y++) for (var x = 0; x < region.width / Q; x++)
                    { Charge(); remaining.Add(new Vector2Int(checked(region.x / Q + x), checked(region.y / Q + y))); }
                var ordered = remaining.OrderBy(p => p.y).ThenBy(p => p.x).ToArray(); var cursor = 0; var sizeChoice = false;
                var widthLimit = Math.Min(settings.PreferredMaxPixels.x / Q, (int)((long)ordered.Max(p => p.x) - ordered.Min(p => p.x) + 1));
                var heightLimit = Math.Min(settings.PreferredMaxPixels.y / Q, (int)((long)ordered.Max(p => p.y) - ordered.Min(p => p.y) + 1));
                var minWidth = settings.PreferredMinPixels.x / Q; var minHeight = settings.PreferredMinPixels.y / Q;
                while (remaining.Count != 0)
                {
                    while (true) { Charge(); if (remaining.Contains(ordered[cursor])) break; cursor++; }
                    var anchor = ordered[cursor]; current = new RectInt(checked(anchor.x * Q), checked(anchor.y * Q), Q, Q);
                    var targetWidth = Target(settings, anchor, Math.Min(minWidth, widthLimit), widthLimit, 0x9e3779b9);
                    var targetHeight = Target(settings, anchor, Math.Min(minHeight, heightLimit), heightLimit, 0x85ebca6b);
                    var spanWidth = widthLimit; var preferredOptions = 0; var anyOptions = 0;
                    var best = Vector2Int.zero; var bestAny = Vector2Int.zero;
                    long bestScore = long.MaxValue, anyScore = long.MaxValue; uint bestTie = uint.MaxValue, anyTie = uint.MaxValue;
                    void Candidate(int width, int height, ref Vector2Int choice, ref long score, ref uint tie)
                    {
                        var dx = (long)width - targetWidth; var dy = (long)height - targetHeight; var distance = dx * dx + dy * dy;
                        var hash = Hash(settings.Seed, settings.Version, anchor.x, anchor.y, width, height);
                        if (distance < score || distance == score && hash < tie) { choice = new Vector2Int(width, height); score = distance; tie = hash; }
                    }
                    for (var height = 1; height <= heightLimit; height++)
                    {
                        Charge(); var width = 0;
                        while (width < spanWidth)
                        {
                            Charge(); if (!remaining.Contains(new Vector2Int(checked(anchor.x + width), checked(anchor.y + height - 1)))) break;
                            width++;
                        }
                        spanWidth = width; if (spanWidth == 0) break;
                        anyOptions = Math.Min(2, anyOptions + Math.Min(2, spanWidth));
                        Candidate(Math.Min(spanWidth, Math.Max(1, targetWidth)), height, ref bestAny, ref anyScore, ref anyTie);
                        if (height < minHeight || spanWidth < minWidth) continue;
                        preferredOptions = Math.Min(2, preferredOptions + Math.Min(2, spanWidth - minWidth + 1));
                        Candidate(Math.Min(spanWidth, Math.Max(minWidth, targetWidth)), height, ref best, ref bestScore, ref bestTie);
                    }
                    var preferred = best != Vector2Int.zero; var selected = preferred ? best : bestAny;
                    sizeChoice |= (preferred ? preferredOptions : anyOptions) > 1;
                    var panel = new RectInt(current.x, current.y, checked(selected.x * Q), checked(selected.y * Q));
                    for (var y = 0; y < selected.y; y++) for (var x = 0; x < selected.x; x++)
                    { Charge(); remaining.Remove(new Vector2Int(checked(anchor.x + x), checked(anchor.y + y))); }
                    rectangles.Add(panel);
                    if (!preferred)
                        advisories.Add(Diagnostic("Residual/" + panel.x + "," + panel.y + "/" + panel.width + "x" + panel.height,
                            "Constrained residual uses " + panel.width + "x" + panel.height + " native pixels below the preferred range; exact coverage is retained.", panel));
                }
                if (!sizeChoice || widthLimit <= minWidth && heightLimit <= minHeight) advisories.Add(Diagnostic("DensitySaturated", "Domain/size constraints fix panel dimensions at every packing anchor; density is saturated.", rectangles[0]));
                return new RoomFloorPanelPartition(settings, rectangles, Array.Empty<RoomGenerationIssue>(), advisories);
            }
            catch (WorkLimit) { return Fail("Budget", "Packing exceeded " + maximumWork + " work units; all partial rectangles were withheld.", current); }
            catch (OverflowException) { return Fail("Coordinates", "Panel coordinates exceed the supported native-pixel range; all partial rectangles were withheld.", current); }
        }

        private static int Target(RoomFloorPanelSettings settings, Vector2Int anchor, int minimum, int maximum, uint salt)
        {
            var noise = Hash(settings.Seed, settings.Version, anchor.x, anchor.y, unchecked((int)salt), 0) / (double)uint.MaxValue;
            var proportion = Math.Max(0, Math.Min(1, 1 - settings.Density + (noise - .5) * .3));
            return minimum + (int)Math.Round((maximum - minimum) * proportion, MidpointRounding.AwayFromZero);
        }

        private static uint Hash(int seed, int version, int x, int y, int width, int height)
        {
            unchecked
            {
                uint hash = 2166136261;
                void Add(int value) { for (var i = 0; i < 4; i++) { hash ^= (byte)((uint)value >> (i * 8)); hash *= 16777619; } }
                Add(seed); Add(version); Add(x); Add(y); Add(width); Add(height); return hash;
            }
        }
    }
}
