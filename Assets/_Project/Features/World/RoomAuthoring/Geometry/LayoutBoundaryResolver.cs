using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum LayoutEdgeKind { Open, Exterior, Partition }

    public readonly struct LayoutEdgeAddress : IEquatable<LayoutEdgeAddress>
    {
        public readonly Vector2Int Start;
        public readonly bool Horizontal;
        public Vector2Int End => Horizontal ? new Vector2Int(checked(Start.x + 1), Start.y) : new Vector2Int(Start.x, checked(Start.y + 1));
        public LayoutEdgeAddress(Vector2Int start, bool horizontal) { Start = start; Horizontal = horizontal; }
        public static LayoutEdgeAddress At(Vector2Int cell, RoomSide side) => new LayoutEdgeAddress(
            side == RoomSide.North ? new Vector2Int(cell.x, checked(cell.y + 1)) :
            side == RoomSide.East ? new Vector2Int(checked(cell.x + 1), cell.y) : cell,
            side == RoomSide.North || side == RoomSide.South);
        public bool Equals(LayoutEdgeAddress other) => Start == other.Start && Horizontal == other.Horizontal;
        public override bool Equals(object other) => other is LayoutEdgeAddress address && Equals(address);
        public override int GetHashCode() => unchecked(Start.GetHashCode() * 397 ^ Horizontal.GetHashCode());
        public override string ToString() => (Horizontal ? "H:" : "V:") + Start.x + "," + Start.y;
    }

    public readonly struct LayoutBoundaryEdge
    {
        public readonly LayoutEdgeAddress Address;
        public readonly string NegativeOwner;
        public readonly string PositiveOwner;
        public LayoutEdgeKind Kind => NegativeOwner == null || PositiveOwner == null ? LayoutEdgeKind.Exterior :
            NegativeOwner == PositiveOwner ? LayoutEdgeKind.Open : LayoutEdgeKind.Partition;
        public string VisualOwner => Kind == LayoutEdgeKind.Partition
            ? (string.CompareOrdinal(NegativeOwner, PositiveOwner) <= 0 ? NegativeOwner : PositiveOwner) : NegativeOwner ?? PositiveOwner;
        public RoomSide Side => Address.Horizontal ? (NegativeOwner == null || Kind == LayoutEdgeKind.Partition ? RoomSide.South : RoomSide.North)
            : NegativeOwner == null ? RoomSide.West : RoomSide.East;
        public LayoutBoundaryEdge(LayoutEdgeAddress address, string negativeOwner, string positiveOwner)
        { Address = address; NegativeOwner = negativeOwner; PositiveOwner = positiveOwner; }
    }

    public readonly struct LayoutBoundarySpan
    {
        public readonly LayoutBoundaryEdge First;
        public readonly int Length;
        public readonly RoomEndpointKind StartKind;
        public readonly RoomEndpointKind EndKind;
        public Vector2Int End => First.Address.Start + (First.Address.Horizontal ? Vector2Int.right : Vector2Int.up) * Length;
        public LayoutBoundarySpan(LayoutBoundaryEdge first, int length, RoomEndpointKind startKind = RoomEndpointKind.Straight,
            RoomEndpointKind endKind = RoomEndpointKind.Straight)
        { First = first; Length = length; StartKind = startKind; EndKind = endKind; }
    }

    public sealed class LayoutJunction
    {
        public Vector2Int Position { get; }
        public RoomEndpointKind Kind { get; }
        public BoundaryDirections Directions { get; }
        public LayoutJunctionRole? Role { get; }
        public IReadOnlyList<string> Owners { get; }
        public IReadOnlyList<RoomJunction> ExteriorCorners { get; }
        internal LayoutJunction(Vector2Int position, RoomEndpointKind kind, BoundaryDirections directions,
            LayoutJunctionRole? role, IEnumerable<string> owners, RoomJunction[] exteriorCorners)
        {
            Position = position; Kind = kind; Directions = directions; Role = role;
            Owners = Array.AsReadOnly(owners.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray());
            ExteriorCorners = Array.AsReadOnly(exteriorCorners);
        }
    }

    public sealed class LayoutBoundaryGeometry
    {
        private readonly Dictionary<LayoutEdgeAddress, LayoutBoundaryEdge> edges;
        public IReadOnlyList<LayoutBoundaryEdge> Edges { get; }
        public IReadOnlyList<LayoutBoundarySpan> Spans { get; }
        public RoomGeometry Exterior { get; }
        public ShipLayoutTopology Topology { get; }
        public IReadOnlyList<LayoutJunction> Junctions { get; }
        private readonly Dictionary<Vector2Int, LayoutJunction> junctions;
        internal LayoutBoundaryGeometry(Dictionary<LayoutEdgeAddress, LayoutBoundaryEdge> resolved, LayoutBoundarySpan[] spans,
            RoomGeometry exterior, ShipLayoutTopology topology, Dictionary<Vector2Int, LayoutJunction> resolvedJunctions)
        {
            edges = resolved;
            Edges = Array.AsReadOnly(resolved.Values.OrderBy(e => e.Address.Start.y).ThenBy(e => e.Address.Start.x).ThenBy(e => e.Address.Horizontal).ToArray());
            Spans = Array.AsReadOnly(spans); Exterior = exterior; Topology = topology;
            junctions = resolvedJunctions;
            Junctions = Array.AsReadOnly(junctions.Values.OrderBy(j => j.Position.y).ThenBy(j => j.Position.x).ToArray());
        }
        public LayoutBoundaryEdge EdgeAt(Vector2Int cell, RoomSide side) => edges[LayoutEdgeAddress.At(cell, side)];
        public LayoutJunction JunctionAt(Vector2Int position) => junctions[position];
    }

    public static class LayoutBoundaryResolver
    {
        public static LayoutBoundaryGeometry Resolve(ShipLayoutTopology topology)
        {
            if (topology.Overlaps.Count != 0) throw new InvalidOperationException("Resolve occupancy ownership before architectural boundaries.");
            if (topology.Cells.Count != 0 && ((long)topology.Cells.Max(c => c.x) - topology.Cells.Min(c => c.x) + 1 > int.MaxValue ||
                (long)topology.Cells.Max(c => c.y) - topology.Cells.Min(c => c.y) + 1 > int.MaxValue))
                throw new OverflowException("Layout bounds exceed the supported integer grid.");
            var addresses = new HashSet<LayoutEdgeAddress>();
            foreach (var cell in topology.Cells)
                foreach (RoomSide side in Enum.GetValues(typeof(RoomSide))) addresses.Add(LayoutEdgeAddress.At(cell, side));
            var edges = new Dictionary<LayoutEdgeAddress, LayoutBoundaryEdge>();
            foreach (var address in addresses)
            {
                var positive = topology.OwnersAt(address.Start).SingleOrDefault();
                string negative = null;
                if (address.Horizontal ? address.Start.y != int.MinValue : address.Start.x != int.MinValue)
                    negative = topology.OwnersAt(address.Start - (address.Horizontal ? Vector2Int.up : Vector2Int.right)).SingleOrDefault();
                _ = address.End;
                edges.Add(address, new LayoutBoundaryEdge(address, negative, positive));
            }
            var spans = new List<LayoutBoundarySpan>();
            var boundaries = edges.Values.Where(e => e.Kind != LayoutEdgeKind.Open).ToArray();
            var exterior = RoomBoundaryExtractor.Extract(topology.Cells);
            var junctions = ResolveJunctions(boundaries, exterior);
            foreach (var line in boundaries.GroupBy(e => (e.Address.Horizontal,
                Axis: e.Address.Horizontal ? e.Address.Start.y : e.Address.Start.x, e.NegativeOwner, e.PositiveOwner))
                .OrderBy(g => g.Key.Horizontal).ThenBy(g => g.Key.Axis)
                .ThenBy(g => g.Key.NegativeOwner, StringComparer.Ordinal).ThenBy(g => g.Key.PositiveOwner, StringComparer.Ordinal))
            {
                var ordered = line.OrderBy(e => e.Address.Horizontal ? e.Address.Start.x : e.Address.Start.y).ToArray();
                for (var i = 0; i < ordered.Length;)
                {
                    var first = ordered[i]; var count = 1;
                    while (++i < ordered.Length && ordered[i].Address.Start ==
                        first.Address.Start + (first.Address.Horizontal ? Vector2Int.right : Vector2Int.up) * count &&
                        junctions[ordered[i].Address.Start].Kind == RoomEndpointKind.Straight) count++;
                    var end = first.Address.Start + (first.Address.Horizontal ? Vector2Int.right : Vector2Int.up) * count;
                    spans.Add(new LayoutBoundarySpan(first, count, junctions[first.Address.Start].Kind, junctions[end].Kind));
                }
            }
            return new LayoutBoundaryGeometry(edges, spans.ToArray(), exterior, topology, junctions);
        }

        private static Dictionary<Vector2Int, LayoutJunction> ResolveJunctions(LayoutBoundaryEdge[] boundaries, RoomGeometry exterior)
        {
            var incident = new Dictionary<Vector2Int, List<LayoutBoundaryEdge>>();
            foreach (var edge in boundaries)
                foreach (var point in new[] { edge.Address.Start, edge.Address.End })
                {
                    if (!incident.TryGetValue(point, out var list)) incident.Add(point, list = new List<LayoutBoundaryEdge>());
                    list.Add(edge);
                }
            var result = new Dictionary<Vector2Int, LayoutJunction>();
            foreach (var pair in incident)
            {
                var mask = BoundaryDirections.None;
                foreach (var edge in pair.Value)
                    mask |= edge.Address.Horizontal ? edge.Address.Start == pair.Key ? BoundaryDirections.East : BoundaryDirections.West
                        : edge.Address.Start == pair.Key ? BoundaryDirections.North : BoundaryDirections.South;
                RoomEndpointKind kind;
                LayoutJunctionRole? role = null;
                var corner = exterior.Junctions.Where(j => j.Position == pair.Key).ToArray();
                if (exterior.DiagonalContacts.Contains(pair.Key)) kind = RoomEndpointKind.Diagonal;
                else if (pair.Value.Count == 2 && corner.Length == 1)
                    kind = corner[0].Kind == RoomJoinKind.Convex ? RoomEndpointKind.Convex : RoomEndpointKind.Concave;
                else if (mask == (BoundaryDirections.North | BoundaryDirections.South) || mask == (BoundaryDirections.East | BoundaryDirections.West))
                    kind = RoomEndpointKind.Straight;
                else if (pair.Value.Count == 4) { kind = RoomEndpointKind.Cross; role = LayoutJunctionRole.Cross; }
                else if (pair.Value.Count == 3)
                {
                    kind = RoomEndpointKind.Tee;
                    role = (mask & BoundaryDirections.South) == 0 ? LayoutJunctionRole.TeeNorth :
                        (mask & BoundaryDirections.West) == 0 ? LayoutJunctionRole.TeeEast :
                        (mask & BoundaryDirections.North) == 0 ? LayoutJunctionRole.TeeSouth : LayoutJunctionRole.TeeWest;
                }
                else if (pair.Value.Count == 2)
                {
                    kind = RoomEndpointKind.Bend;
                    role = (mask & BoundaryDirections.North) != 0 ?
                        (mask & BoundaryDirections.East) != 0 ? LayoutJunctionRole.BendNortheast : LayoutJunctionRole.BendNorthwest :
                        (mask & BoundaryDirections.East) != 0 ? LayoutJunctionRole.BendSoutheast : LayoutJunctionRole.BendSouthwest;
                }
                else
                {
                    kind = RoomEndpointKind.End;
                    role = mask == BoundaryDirections.North ? LayoutJunctionRole.EndNorth : mask == BoundaryDirections.East ? LayoutJunctionRole.EndEast :
                        mask == BoundaryDirections.South ? LayoutJunctionRole.EndSouth : LayoutJunctionRole.EndWest;
                }
                result.Add(pair.Key, new LayoutJunction(pair.Key, kind, mask, role,
                    pair.Value.SelectMany(e => new[] { e.NegativeOwner, e.PositiveOwner }).Where(id => id != null), corner));
            }
            return result;
        }
    }
}
