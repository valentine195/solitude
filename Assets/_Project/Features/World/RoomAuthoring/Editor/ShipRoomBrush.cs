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

        public void BeginGesture(ShipRoomAuthoring room, Tool tool)
        {
            EndGesture();
            cancelled = false;
            gesture = RoomAuthoringOperations.BeginEdit(room, "Room " + tool);
        }

        public void EndGesture()
        {
            var completed = gesture;
            gesture = null;
            completed?.Dispose();
        }

        public void CancelGesture()
        {
            cancelled = true;
            gesture?.Cancel();
            gesture = null;
        }

        private void OnDisable() => CancelGesture();

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (CanPaint && occupancyMarker != null && position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, occupancyMarker, gesture);
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (CanPaint && position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, null, gesture);
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
