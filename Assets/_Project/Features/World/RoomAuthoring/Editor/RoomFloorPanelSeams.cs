using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public readonly struct RoomFloorPanelSeam
    {
        public readonly Vector2Int Start;
        public readonly Vector2Int End;
        public bool Horizontal => Start.y == End.y;
        internal RoomFloorPanelSeam(Vector2Int start, Vector2Int end) { Start = start; End = end; }
    }

    // Canonical closed segments describe shared and perimeter edges alike. They carry no rendering resources.
    public sealed class RoomFloorPanelSeams
    {
        public IReadOnlyList<RectInt> Rectangles { get; }
        public IReadOnlyList<RoomFloorPanelSeam> Segments { get; }
        public IReadOnlyList<RoomGenerationIssue> Issues { get; }
        public bool Success => Issues.Count == 0;

        private RoomFloorPanelSeams(IEnumerable<RectInt> rectangles, IEnumerable<RoomFloorPanelSeam> segments, IEnumerable<RoomGenerationIssue> issues)
        {
            Rectangles = Array.AsReadOnly(rectangles.ToArray()); Segments = Array.AsReadOnly(segments.ToArray()); Issues = Array.AsReadOnly(issues.ToArray());
        }

        internal static RoomGenerationIssue Issue(string owner, string field, string reason)
        {
            var issue = new RoomGenerationIssue(RoomIssueKind.Geometry, "FloorPanels/" + field, reason);
            issue.LocateInRoom(string.IsNullOrWhiteSpace(owner) ? "<missing-space>" : owner); return issue;
        }

        public static RoomFloorPanelSeams Extract(string owner, RoomFloorPanelPartition partition)
        {
            if (partition == null) throw new ArgumentNullException(nameof(partition));
            return partition.Success ? Extract(owner, partition.Rectangles) :
                new RoomFloorPanelSeams(Array.Empty<RectInt>(), Array.Empty<RoomFloorPanelSeam>(), partition.Issues);
        }

        public static RoomFloorPanelSeams Extract(string owner, IEnumerable<RectInt> rectangles)
        {
            RoomFloorPanelSeams Fail(string field, string reason) => new RoomFloorPanelSeams(Array.Empty<RectInt>(), Array.Empty<RoomFloorPanelSeam>(),
                new[] { Issue(owner, "Seams/" + field, reason) });
            if (string.IsNullOrWhiteSpace(owner) || rectangles == null) return Fail("Inputs", "Provide a space identity and partition rectangles.");
            var input = new List<RectInt>(); long area = 0;
            foreach (var r in rectangles)
            {
                if (r.width <= 0 || r.height <= 0 || r.x % 8 != 0 || r.y % 8 != 0 || r.width % 8 != 0 || r.height % 8 != 0 ||
                    (long)r.x + r.width > int.MaxValue || (long)r.y + r.height > int.MaxValue)
                    return Fail("Coordinates", "Panel rectangles require positive eight-pixel dimensions/aligned origins and representable native endpoints.");
                area += (long)(r.width / 8) * (r.height / 8);
                if (area > RoomFloorPanelPartitioner.MaximumQuanta) return Fail("Budget", "Panel area exceeds the bounded seam extraction budget.");
                input.Add(r);
            }
            var sorted = input.OrderBy(r => r.y).ThenBy(r => r.x).ThenBy(r => r.width).ThenBy(r => r.height).ToArray();
            var occupied = new HashSet<Vector2Int>();
            var lines = new Dictionary<(bool horizontal, int axis), List<(int start, int end)>>();
            void Edge(bool horizontal, int axis, int start, int end)
            {
                var key = (horizontal, axis);
                if (!lines.TryGetValue(key, out var spans)) lines.Add(key, spans = new List<(int, int)>());
                spans.Add((start, end));
            }
            foreach (var r in sorted)
            {
                for (var y = 0; y < r.height / 8; y++) for (var x = 0; x < r.width / 8; x++)
                    if (!occupied.Add(new Vector2Int(checked(r.x / 8 + x), checked(r.y / 8 + y))))
                        return Fail("Overlap", "Panel interiors overlap; no seam network was produced.");
                var right = checked(r.x + r.width); var top = checked(r.y + r.height);
                Edge(true, r.y, r.x, right); Edge(true, top, r.x, right);
                Edge(false, r.x, r.y, top); Edge(false, right, r.y, top);
            }
            var segments = new List<RoomFloorPanelSeam>();
            foreach (var line in lines.OrderBy(p => p.Key.horizontal ? 0 : 1).ThenBy(p => p.Key.axis))
            {
                var intervals = line.Value.OrderBy(p => p.start).ThenBy(p => p.end).ToArray();
                var start = intervals[0].start; var end = intervals[0].end;
                void Emit()
                {
                    segments.Add(new RoomFloorPanelSeam(line.Key.horizontal ? new Vector2Int(start, line.Key.axis) : new Vector2Int(line.Key.axis, start),
                        line.Key.horizontal ? new Vector2Int(end, line.Key.axis) : new Vector2Int(line.Key.axis, end)));
                }
                foreach (var interval in intervals.Skip(1))
                {
                    if (interval.start <= end) end = Math.Max(end, interval.end);
                    else { Emit(); start = interval.start; end = interval.end; }
                }
                Emit();
            }
            return new RoomFloorPanelSeams(sorted, segments, Array.Empty<RoomGenerationIssue>());
        }
    }
}
