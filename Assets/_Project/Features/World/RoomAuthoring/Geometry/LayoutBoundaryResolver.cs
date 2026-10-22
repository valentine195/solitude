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
        public Vector2Int End => First.Address.Start + (First.Address.Horizontal ? Vector2Int.right : Vector2Int.up) * Length;
        public LayoutBoundarySpan(LayoutBoundaryEdge first, int length) { First = first; Length = length; }
    }

    public sealed class LayoutBoundaryGeometry
    {
        private readonly Dictionary<LayoutEdgeAddress, LayoutBoundaryEdge> edges;
        public IReadOnlyList<LayoutBoundaryEdge> Edges { get; }
        public IReadOnlyList<LayoutBoundarySpan> Spans { get; }
        public RoomGeometry Exterior { get; }
        public ShipLayoutTopology Topology { get; }
        internal LayoutBoundaryGeometry(Dictionary<LayoutEdgeAddress, LayoutBoundaryEdge> resolved, LayoutBoundarySpan[] spans,
            RoomGeometry exterior, ShipLayoutTopology topology)
        {
            edges = resolved;
            Edges = Array.AsReadOnly(resolved.Values.OrderBy(e => e.Address.Start.y).ThenBy(e => e.Address.Start.x).ThenBy(e => e.Address.Horizontal).ToArray());
            Spans = Array.AsReadOnly(spans); Exterior = exterior; Topology = topology;
        }
        public LayoutBoundaryEdge EdgeAt(Vector2Int cell, RoomSide side) => edges[LayoutEdgeAddress.At(cell, side)];
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
            var boundaries = edges.Values.Where(e => e.Kind != LayoutEdgeKind.Open);
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
                        first.Address.Start + (first.Address.Horizontal ? Vector2Int.right : Vector2Int.up) * count) count++;
                    spans.Add(new LayoutBoundarySpan(first, count));
                }
            }
            return new LayoutBoundaryGeometry(edges, spans.ToArray(), RoomBoundaryExtractor.Extract(topology.Cells), topology);
        }
    }
}
