using System.Collections.Generic;
using UnityEngine;

namespace SOLITUDE.World.RoomAuthoring
{
    public enum RoomSide { North, South, East, West }

    public readonly struct RoomBoundaryRun
    {
        public readonly RoomSide Side;
        public readonly Vector2Int Start;
        public readonly int Length;
        public RoomBoundaryRun(RoomSide side, Vector2Int start, int length)
        { Side = side; Start = start; Length = length; }
    }

    public sealed class RoomGeometry
    {
        public Vector2Int[] Cells { get; internal set; }
        public RoomBoundaryRun[] Runs { get; internal set; }
        public Vector2Int[] ConvexCorners { get; internal set; }
        public RectInt Bounds { get; internal set; }
        public bool IsRectangle { get; internal set; }
    }
}
