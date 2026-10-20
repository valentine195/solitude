using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.UIElements;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomEditor(typeof(ShipRoomBrush))]
    public sealed class ShipRoomBrushEditor : GridBrushEditorBase
    {
        public override string tooltip => "Paint room cells. Erase removes cells. Walls and floors follow the room style.";
        public override bool canChangeZPosition { get => false; set { } }
        public override GameObject[] validTargets => Resources.FindObjectsOfTypeAll<ShipRoomAuthoring>()
            .Where(room => room.gameObject.scene.IsValid() && room.gameObject.scene.isLoaded && room.gameObject.activeInHierarchy &&
                room.Occupancy != null && RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true) == null)
            .Select(room => room.Occupancy.gameObject).ToArray();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label(tooltip));
            var mode = new EnumField("Box action", ((ShipRoomBrush)target).BoxMode);
            mode.RegisterValueChangedCallback(evt => SetBoxMode((ShipRoomBrush.BoxEditMode)evt.newValue));
            root.Add(mode);
            return root;
        }

        public override void OnPaintInspectorGUI()
        {
            EditorGUILayout.HelpBox(tooltip, MessageType.Info);
            var brush = (ShipRoomBrush)target;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Box action");
            var mode = (ShipRoomBrush.BoxEditMode)GUILayout.Toolbar((int)brush.BoxMode, new[] { "Fill", "Erase" });
            EditorGUILayout.EndHorizontal();
            if (mode != brush.BoxMode) SetBoxMode(mode);
            if (GUILayout.Button("Edit Selected Room"))
            {
                var room = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<ShipRoomAuthoring>(true);
                if (room != null) RoomAuthoringPaletteSetup.EditRoom(room);
            }
        }

        private void SetBoxMode(ShipRoomBrush.BoxEditMode mode)
        {
            var brush = (ShipRoomBrush)target;
            brush.EndGesture();
            brush.BoxMode = mode;
            TilemapEditorTool.SetActiveEditorTool(typeof(BoxTool));
        }

        public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            if (!executing) ((ShipRoomBrush)target).EndGesture();
            RoomAuthoringPaletteSetup.ActivateTarget(brushTarget);
            base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);
        }

        public override void OnMouseEnter() => RoomAuthoringPaletteSetup.ActivateTarget(GridPaintingState.scenePaintTarget);
        public override void OnMouseLeave() => ((ShipRoomBrush)target).EndGesture();
        public override void OnToolDeactivated(GridBrushBase.Tool tool) => ((ShipRoomBrush)target).EndGesture();

        public override void RegisterUndo(GameObject brushTarget, GridBrushBase.Tool tool)
        {
            var grid = brushTarget == null ? null : brushTarget.GetComponentInParent<Grid>();
            if (ShipRoomBrush.TryGetRoom(grid, brushTarget, out var room))
                ((ShipRoomBrush)target).BeginGesture(room, tool);
        }
    }
}
