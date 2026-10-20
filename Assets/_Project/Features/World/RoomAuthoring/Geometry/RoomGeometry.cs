using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum RoomSide { North, South, East, West }
    public enum RoomJoinKind { Convex, Concave }
    public enum RoomCorner { Southwest, Southeast, Northwest, Northeast }

    public readonly struct RoomJunction
    {
        public readonly RoomJoinKind Kind;
        public readonly RoomCorner Corner;
        public readonly Vector2Int Position;
        public RoomJunction(RoomJoinKind kind, RoomCorner corner, Vector2Int position)
        { Kind = kind; Corner = corner; Position = position; }
        public string Role => (Kind == RoomJoinKind.Concave ? "Concave" : "") + Corner;
        public bool HasSide(RoomSide side) => side == RoomSide.North
            ? Corner == RoomCorner.Northwest || Corner == RoomCorner.Northeast
            : side == RoomSide.South ? Corner == RoomCorner.Southwest || Corner == RoomCorner.Southeast
            : side == RoomSide.West ? Corner == RoomCorner.Northwest || Corner == RoomCorner.Southwest
            : Corner == RoomCorner.Northeast || Corner == RoomCorner.Southeast;
    }

    public readonly struct RoomBoundaryRun
    {
        public readonly RoomSide Side;
        public readonly Vector2Int Start;
        public readonly int Length;
        public readonly RoomJunction StartJoin;
        public readonly RoomJunction EndJoin;
        public Vector2Int End => Start + (Side == RoomSide.North || Side == RoomSide.South ? Vector2Int.right : Vector2Int.up) * Length;
        public RoomBoundaryRun(RoomSide side, Vector2Int start, int length, RoomJunction startJoin = default, RoomJunction endJoin = default)
        { Side = side; Start = start; Length = length; StartJoin = startJoin; EndJoin = endJoin; }
    }

    public sealed class RoomGeometry
    {
        public Vector2Int[] Cells { get; internal set; }
        public RoomBoundaryRun[] Runs { get; internal set; }
        public RoomJunction[] Junctions { get; internal set; }
        public Vector2Int[] ConvexCorners { get; internal set; }
        public Vector2Int[] DiagonalContacts { get; internal set; }
        public int ComponentCount { get; internal set; }
        public RectInt Bounds { get; internal set; }
        public bool IsRectangle { get; internal set; }
    }
}
