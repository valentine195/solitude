using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomGridBrush(false, true, false, "Ship Room")]
    [BrushTools(typeof(PaintTool), typeof(EraseTool))]
    public sealed class ShipRoomBrush : GridBrushBase
    {
        [SerializeField] private TileBase occupancyMarker;
        public TileBase OccupancyMarker { get => occupancyMarker; set => occupancyMarker = value; }

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (occupancyMarker != null && position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, occupancyMarker);
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (position.z == 0 && TryGetRoom(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, null);
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
