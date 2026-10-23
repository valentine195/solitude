using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomGridBrush(false, true, false, "Ship Room")]
    [BrushTools(typeof(PaintTool), typeof(EraseTool), typeof(BoxTool))]
    public sealed class ShipRoomBrush : GridBrushBase
    {
        public enum BoxEditMode { Fill, Erase }
        // Tool-session state: reopening the Editor starts safely in Fill mode.
        public BoxEditMode BoxMode { get; set; }
        [SerializeField] private TileBase occupancyMarker;
        public TileBase OccupancyMarker { get => occupancyMarker; set => occupancyMarker = value; }
        private RoomAuthoringOperations.RoomEdit gesture;
        private bool cancelled;
        private bool CanPaint => !cancelled && (gesture == null || !gesture.IsDisposed);
        private int corridorWidth = 1;
        private ShipRoomAuthoring corridorRoom;
        private TileBase corridorMarker;
        private Vector2Int? lastSample;
        private readonly HashSet<Vector2Int> corridorCells = new HashSet<Vector2Int>();
        public string StrokeIssue { get; private set; }
        public int CorridorWidth
        {
            get => corridorWidth;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Corridor width must be positive.");
                if (value == corridorWidth) return;
                CancelGesture(); cancelled = false; corridorWidth = value;
            }
        }
        public Vector2Int[] CorridorPreviewCells => gesture == null || gesture.IsDisposed ? Array.Empty<Vector2Int>() :
            corridorCells.OrderBy(c => c.y).ThenBy(c => c.x).ToArray();

        public void BeginGesture(ShipRoomAuthoring room, Tool tool)
        {
            EndGesture();
            cancelled = false;
            StrokeIssue = null;
            gesture = RoomAuthoringOperations.BeginEdit(room, "Room " + tool);
        }

        public void EndGesture()
        {
            var completed = gesture;
            gesture = null;
            try
            {
                if (completed != null && !completed.IsDisposed && corridorRoom != null && corridorCells.Count != 0)
                {
                    if (!RoomAuthoringOperations.PaintCells(corridorRoom, corridorCells, corridorMarker, out var issue, completed))
                    { StrokeIssue = issue; completed.Cancel(); return; }
                }
                completed?.Dispose();
            }
            finally { ClearCorridor(); }
        }

        public void CancelGesture()
        {
            cancelled = true;
            gesture?.Cancel();
            gesture = null;
            ClearCorridor();
        }

        private void ClearCorridor() { corridorRoom = null; corridorMarker = null; lastSample = null; corridorCells.Clear(); }

        // Even widths keep their extra cell east/north of the cursor, independent of travel direction.
        private static void AddSegment(HashSet<Vector2Int> cells, Vector2Int a, Vector2Int b, int width)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            _ = checked(width * width);
            if (a.x > b.x || (a.x == b.x && a.y > b.y)) (a, b) = (b, a);
            _ = checked(b.x - a.x); _ = checked(b.y - a.y);
            var bias = -(width - 1) / 2;
            void Stamp(int x, int y)
            {
                for (var dy = 0; dy < width; dy++) for (var dx = 0; dx < width; dx++)
                    cells.Add(new Vector2Int(checked(x + checked(bias + dx)), checked(y + checked(bias + dy))));
            }
            // A canonical horizontal-then-vertical segment makes sparse/diagonal samples reversible too.
            for (var x = a.x; x != b.x; x++) Stamp(x, a.y);
            var direction = Math.Sign((long)b.y - a.y);
            for (var y = a.y; y != b.y; y += direction) Stamp(b.x, y);
            Stamp(b.x, b.y);
        }

        public Vector2Int[] PreviewCorridor(ShipRoomAuthoring room, Vector3Int position, Tool tool, out string issue)
        {
            issue = null;
            if (room == null || room.SpaceType != LogicalSpaceType.Corridor || position.z != 0 ||
                (tool != Tool.Paint && tool != Tool.Erase)) return Array.Empty<Vector2Int>();
            var cells = new HashSet<Vector2Int>(CorridorPreviewCells);
            var sample = new Vector2Int(position.x, position.y);
            try { AddSegment(cells, corridorRoom == room && lastSample.HasValue ? lastSample.Value : sample, sample, corridorWidth); }
            catch (OverflowException) { issue = "Corridor footprint exceeds the supported integer grid."; return Array.Empty<Vector2Int>(); }
            var result = cells.OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
            issue = RoomAuthoringOperations.GetCellPaintIssue(room, result, tool == Tool.Erase ? null : occupancyMarker);
            return result;
        }

        private void PaintCorridor(ShipRoomAuthoring room, Vector3Int position, TileBase marker)
        {
            var standalone = gesture == null;
            if (standalone) BeginGesture(room, marker == null ? Tool.Erase : Tool.Paint);
            if (gesture.IsDisposed) return;
            if (corridorRoom != null && (corridorRoom != room || corridorMarker != marker))
            { CancelGesture(); StrokeIssue = "Painting target changed; stroke cancelled."; return; }
            corridorRoom = room; corridorMarker = marker;
            var sample = new Vector2Int(position.x, position.y);
            try { AddSegment(corridorCells, lastSample ?? sample, sample, corridorWidth); lastSample = sample; }
            catch (OverflowException) { CancelGesture(); StrokeIssue = "Corridor footprint exceeds the supported integer grid."; return; }
            if (standalone) EndGesture();
        }

        private void OnDisable() => CancelGesture();

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (CanPaint && occupancyMarker != null && position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
            {
                if (room.SpaceType == LogicalSpaceType.Corridor) PaintCorridor(room, position, occupancyMarker);
                else RoomAuthoringOperations.PaintCell(room, position, occupancyMarker, gesture);
            }
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (CanPaint && position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
            {
                if (room.SpaceType == LogicalSpaceType.Corridor) PaintCorridor(room, position, null);
                else RoomAuthoringOperations.PaintCell(room, position, null, gesture);
            }
        }

        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            if (BoxMode == BoxEditMode.Erase)
            {
                BoxErase(gridLayout, brushTarget, bounds);
                return;
            }
            if (CanPaint && occupancyMarker != null && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintBox(room, bounds, occupancyMarker, gesture);
        }

        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            if (CanPaint && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintBox(room, bounds, null, gesture);
        }

        public static bool TryGetRoom(GridLayout gridLayout, GameObject brushTarget, out ShipRoomAuthoring room)
        {
            room = brushTarget == null ? null : brushTarget.GetComponentInParent<ShipRoomAuthoring>(true);
            return room != null && room.gameObject.activeInHierarchy && room.Occupancy != null &&
                room.Occupancy.gameObject == brushTarget && gridLayout == room.GetComponent<Grid>() &&
                RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true) == null;
        }
    }
}
