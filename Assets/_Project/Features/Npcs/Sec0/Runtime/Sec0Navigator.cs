using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.Npcs.Sec0
{
    // Target-driven local navigation. The caller supplies destinations and footprint
    // validation; patrol, pursuit and investigation use the same route lifecycle.
    internal sealed class Sec0Navigator
    {
        private const float Cell = .5f;
        private const int Extent = 16, MaxNodes = 900;
        private const float Arrival = .12f, Deviation = .3f, FailedRetry = .5f;
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        private readonly Func<Vector2, Vector2, bool> clear;
        private readonly List<Vector2> path = new();
        private readonly List<Entry> frontier = new();
        private readonly Dictionary<Vector2Int, Vector2Int> parents = new();
        private readonly Dictionary<Vector2Int, int> costs = new();
        private readonly HashSet<Vector2Int> closed = new();
        private Vector2 destination, plannedGoal, origin, segmentStart;
        private int cursor, sequence;
        private float retry;
        private bool hasDestination, dirty, direct, failed;
#if UNITY_EDITOR
        internal int SearchCount { get; private set; }
        internal int LastExpandedNodes { get; private set; }
#endif
        public Sec0Navigator(Func<Vector2, Vector2, bool> clear)
            => this.clear = clear ?? throw new ArgumentNullException(nameof(clear));

        // A caller tracking a moving target can allow small endpoint adjustments
        // before requesting a full search. Fixed destinations use the default zero.
        public void SetDestination(Vector2 next, float repathDistance = 0)
        {
            if (!Finite(next.x) || !Finite(next.y) || !Finite(repathDistance) || repathDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(next));
            if (hasDestination && destination == next) return;
            if (!hasDestination || (next - plannedGoal).sqrMagnitude > repathDistance * repathDistance)
            { dirty = true; failed = false; retry = 0; }
            destination = next; hasDestination = true;
            if (path.Count > 0) path[path.Count - 1] = next;
        }

        // For known topology changes or a movement guard rejecting a planned step.
        public void InvalidatePath()
        { path.Clear(); cursor = 0; dirty = true; direct = false; failed = false; retry = 0; }
        public void Reset()
        {
            InvalidatePath(); hasDestination = false;
            frontier.Clear(); parents.Clear(); costs.Clear(); closed.Clear();
        }

        // Compatibility for callers supplying a sampled target each update.
        public Vector2 Next(Vector2 position, Vector2 target, float elapsed)
        { SetDestination(target, .4f); return Next(position, elapsed); }

        public Vector2 Next(Vector2 position, float elapsed)
        {
            if (!hasDestination) return position;
            if (Finite(elapsed) && elapsed > 0) retry = Mathf.Max(0, retry - elapsed);
            if ((position - destination).sqrMagnitude < 1e-8f) return destination;
            if (failed && !dirty && retry > 0 && (position - origin).sqrMagnitude <= Deviation * Deviation)
                return position;
            if (dirty || failed) return Plan(position);
            if (direct)
            {
                if (clear(position, destination)) return destination;
                return Plan(position, directAlreadyBlocked: true);
            }
            while (cursor < path.Count && (position - path[cursor]).sqrMagnitude < Arrival * Arrival)
                segmentStart = path[cursor++];
            if (cursor == path.Count || DistanceToSegmentSquared(position, segmentStart, path[cursor]) > Deviation * Deviation)
                return Plan(position);
            if (clear(position, path[cursor])) return path[cursor];
            return Plan(position);
        }

        private Vector2 Plan(Vector2 position, bool directAlreadyBlocked = false)
        {
            path.Clear(); cursor = 0; dirty = false; failed = false; direct = false;
            origin = segmentStart = position; plannedGoal = destination;
            if (!directAlreadyBlocked && clear(position, destination))
            { direct = true; return destination; }
            Build();
            if (path.Count > 0) return path[0]; // The search validated the first edge.
            failed = true; retry = FailedRetry;
            return position;
        }

        private void Build()
        {
#if UNITY_EDITOR
            SearchCount++; LastExpandedNodes = 0;
#endif
            frontier.Clear(); parents.Clear(); costs.Clear(); closed.Clear(); sequence = 0;
            var start = Vector2Int.zero; parents.Add(start, start); costs.Add(start, 0);
            Push(new Entry(start, 0, Heuristic(start), sequence++));
            while (frontier.Count > 0 && closed.Count < MaxNodes)
            {
                var entry = Pop(); var current = entry.Cell;
                if (closed.Contains(current) || costs[current] != entry.Cost) continue;
                closed.Add(current);
#if UNITY_EDITOR
                LastExpandedNodes++;
#endif
                var world = World(current);
                if ((world - destination).sqrMagnitude < Cell * Cell && clear(world, destination))
                {
                    for (var found = current; found != start; found = parents[found]) path.Add(World(found));
                    path.Reverse();
                    if (path.Count == 0 || path[path.Count - 1] != destination) path.Add(destination);
                    return;
                }
                foreach (var offset in Neighbours)
                {
                    var next = current + offset;
                    if (Mathf.Abs(next.x) > Extent || Mathf.Abs(next.y) > Extent || closed.Contains(next)) continue;
                    int cost = entry.Cost + 1;
                    bool known = costs.TryGetValue(next, out int previous);
                    if ((known && cost >= previous) || (!known && parents.Count >= MaxNodes)) continue;
                    if (!clear(world, World(next))) continue;
                    costs[next] = cost; parents[next] = current;
                    Push(new Entry(next, cost, Heuristic(next), sequence++));
                }
            }
        }

        private Vector2 World(Vector2Int cell) => origin + (Vector2)cell * Cell;
        private float Heuristic(Vector2Int cell)
        {
            var delta = destination - World(cell);
            // The final connector can start anywhere within a one-cell radius.
            // Subtract that region's maximum Manhattan radius to stay admissible.
            return Mathf.Max(0, (Mathf.Abs(delta.x) + Mathf.Abs(delta.y)) / Cell - 1.414214f);
        }
        private readonly struct Entry
        {
            internal readonly Vector2Int Cell;
            internal readonly int Cost, Order;
            internal readonly float Estimate;
            internal Entry(Vector2Int cell, int cost, float estimate, int order)
            { Cell = cell; Cost = cost; Estimate = estimate; Order = order; }
        }
        private static bool Before(Entry a, Entry b)
        {
            float priorityA = a.Cost + a.Estimate, priorityB = b.Cost + b.Estimate;
            if (priorityA != priorityB) return priorityA < priorityB;
            if (a.Estimate != b.Estimate) return a.Estimate < b.Estimate;
            return a.Order < b.Order;
        }
        private void Push(Entry value)
        {
            int index = frontier.Count; frontier.Add(value);
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Before(value, frontier[parent])) break;
                frontier[index] = frontier[parent]; index = parent;
            }
            frontier[index] = value;
        }
        private Entry Pop()
        {
            var result = frontier[0]; int last = frontier.Count - 1;
            var value = frontier[last]; frontier.RemoveAt(last);
            if (last == 0) return result;
            int index = 0;
            while (index * 2 + 1 < frontier.Count)
            {
                int child = index * 2 + 1;
                if (child + 1 < frontier.Count && Before(frontier[child + 1], frontier[child])) child++;
                if (!Before(frontier[child], value)) break;
                frontier[index] = frontier[child]; index = child;
            }
            frontier[index] = value; return result;
        }
        private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            float fraction = segment.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return (point - start - segment * fraction).sqrMagnitude;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
