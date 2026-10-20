using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public static class RoomBoundaryExtractor
    {
        public static RoomGeometry Extract(IEnumerable<Vector2Int> input)
        {
            var cells = new HashSet<Vector2Int>(input ?? throw new ArgumentNullException(nameof(input)));
            var ordered = cells.OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
            if (ordered.Length == 0) return new RoomGeometry { Cells = ordered, Runs = Array.Empty<RoomBoundaryRun>(), ConvexCorners = Array.Empty<Vector2Int>() };
            var min = new Vector2Int(cells.Min(c => c.x), cells.Min(c => c.y));
            var max = new Vector2Int(cells.Max(c => c.x) + 1, cells.Max(c => c.y) + 1);
            var bounds = new RectInt(min, max - min);
            var runs = new List<RoomBoundaryRun>();
            foreach (RoomSide side in Enum.GetValues(typeof(RoomSide)))
            {
                var direction = side == RoomSide.North ? Vector2Int.up : side == RoomSide.South ? Vector2Int.down : side == RoomSide.East ? Vector2Int.right : Vector2Int.left;
                var horizontal = side == RoomSide.North || side == RoomSide.South;
                var edges = cells.Where(c => !cells.Contains(c + direction)).Select(c =>
                    side == RoomSide.North ? c + Vector2Int.up : side == RoomSide.East ? c + Vector2Int.right : c);
                foreach (var line in edges.GroupBy(c => horizontal ? c.y : c.x).OrderBy(g => g.Key))
                {
                    var positions = line.Select(c => horizontal ? c.x : c.y).OrderBy(n => n).ToArray();
                    for (var i = 0; i < positions.Length;)
                    {
                        var start = positions[i];
                        var end = start + 1;
                        while (++i < positions.Length && positions[i] == end) end++;
                        runs.Add(new RoomBoundaryRun(side, horizontal ? new Vector2Int(start, line.Key) : new Vector2Int(line.Key, start), end - start));
                    }
                }
            }
            var corners = new HashSet<Vector2Int>();
            foreach (var c in cells)
            {
                if (!cells.Contains(c + Vector2Int.left) && !cells.Contains(c + Vector2Int.down)) corners.Add(c);
                if (!cells.Contains(c + Vector2Int.right) && !cells.Contains(c + Vector2Int.down)) corners.Add(c + Vector2Int.right);
                if (!cells.Contains(c + Vector2Int.left) && !cells.Contains(c + Vector2Int.up)) corners.Add(c + Vector2Int.up);
                if (!cells.Contains(c + Vector2Int.right) && !cells.Contains(c + Vector2Int.up)) corners.Add(c + Vector2Int.one);
            }
            return new RoomGeometry { Cells = ordered, Bounds = bounds, IsRectangle = (long)bounds.width * bounds.height == cells.Count,
                Runs = runs.ToArray(), ConvexCorners = corners.OrderBy(c => c.y).ThenBy(c => c.x).ToArray() };
        }
    }
}
