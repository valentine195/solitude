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

        public void BeginGesture(ShipRoomAuthoring room, Tool tool)
        {
            EndGesture();
            gesture = RoomAuthoringOperations.BeginEdit(room, "Room " + tool);
        }

        public void EndGesture()
        {
            var edit = gesture;
            gesture = null;
            edit?.Dispose();
        }

        private void OnEnable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += BeforeSceneSave;
            AssemblyReloadEvents.beforeAssemblyReload += EndGesture;
        }
        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= BeforeSceneSave;
            AssemblyReloadEvents.beforeAssemblyReload -= EndGesture;
            EndGesture();
        }
        private void BeforeSceneSave(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (gesture != null && gesture.Room != null && gesture.Room.gameObject.scene == scene) EndGesture();
        }

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (occupancyMarker != null && position.z == 0 && TryGetTarget(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, occupancyMarker, gesture);
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (position.z == 0 && TryGetTarget(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintCell(room, position, null, gesture);
        }

        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            if (BoxMode == BoxEditMode.Erase)
            {
                BoxErase(gridLayout, brushTarget, bounds);
                return;
            }
            if (occupancyMarker != null && TryGetTarget(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintBox(room, bounds, occupancyMarker, gesture);
        }

        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            if (TryGetTarget(gridLayout, brushTarget, out var room))
                RoomAuthoringOperations.PaintBox(room, bounds, null, gesture);
        }

        private bool TryGetTarget(GridLayout gridLayout, GameObject brushTarget, out ShipRoomAuthoring room)
        {
            room = gesture?.Room;
            if (room == null) return TryGetRoom(gridLayout, brushTarget, out room);
            return !gesture.IsDisposed && room.gameObject.activeInHierarchy && room.Occupancy != null &&
                room.Occupancy.gameObject == brushTarget && gridLayout == room.GetComponent<Grid>();
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
