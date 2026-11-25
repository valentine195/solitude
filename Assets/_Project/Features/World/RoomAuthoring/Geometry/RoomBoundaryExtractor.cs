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
            if (ordered.Length == 0) return new RoomGeometry { Cells = ordered, Runs = Array.Empty<RoomBoundaryRun>(), ConvexCorners = Array.Empty<Vector2Int>(), Junctions = Array.Empty<RoomJunction>(), DiagonalContacts = Array.Empty<Vector2Int>() };
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
            var vertices = new HashSet<Vector2Int>(cells.SelectMany(c => new[] { c, c + Vector2Int.right, c + Vector2Int.up, c + Vector2Int.one }));
            var joins = new List<RoomJunction>();
            var contacts = new List<Vector2Int>();
            foreach (var vertex in vertices.OrderBy(v => v.y).ThenBy(v => v.x))
            {
                var quadrants = new[] { vertex - Vector2Int.one, vertex - Vector2Int.up, vertex - Vector2Int.right, vertex };
                var occupied = quadrants.Select(cells.Contains).ToArray();
                var count = occupied.Count(v => v);
                var diagonal = count == 2 && occupied[0] == occupied[3] && occupied[1] == occupied[2];
                if (diagonal) contacts.Add(vertex);
                for (var n = 0; n < 4; n++)
                {
                    if ((count == 1 || diagonal) && occupied[n])
                        joins.Add(new RoomJunction(RoomJoinKind.Convex, (RoomCorner)(3 - n), vertex));
                    if (count == 3 && !occupied[n])
                        joins.Add(new RoomJunction(RoomJoinKind.Concave, (RoomCorner)n, vertex));
                }
            }
            var junctions = joins.OrderBy(j => j.Position.y).ThenBy(j => j.Position.x).ThenBy(j => j.Kind).ThenBy(j => j.Corner).ToArray();
            var locatedRuns = runs.Select(run => new RoomBoundaryRun(run.Side, run.Start, run.Length,
                junctions.Single(j => j.Position == run.Start && j.HasSide(run.Side)),
                junctions.Single(j => j.Position == run.End && j.HasSide(run.Side)))).ToArray();
            var remaining = new HashSet<Vector2Int>(cells);
            var components = 0;
            var queue = new Queue<Vector2Int>();
            while (remaining.Count > 0)
            {
                var seed = remaining.First(); remaining.Remove(seed); queue.Enqueue(seed); components++;
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var offset in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                        if (remaining.Remove(cell + offset)) queue.Enqueue(cell + offset);
                }
            }
            return new RoomGeometry { Cells = ordered, Bounds = bounds, IsRectangle = (long)bounds.width * bounds.height == cells.Count,
                Runs = locatedRuns, Junctions = junctions, DiagonalContacts = contacts.ToArray(), ComponentCount = components,
                ConvexCorners = junctions.Where(j => j.Kind == RoomJoinKind.Convex).Select(j => j.Position).ToArray() };
        }
    }
}
