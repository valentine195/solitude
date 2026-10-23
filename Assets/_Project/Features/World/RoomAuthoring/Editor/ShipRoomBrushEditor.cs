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
        public override string tooltip => "Paint room cells or a continuous corridor route. Erase removes cells. Walls and floors follow the space style.";
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
            var width = new IntegerField("Corridor width (cells)") { value = ((ShipRoomBrush)target).CorridorWidth };
            width.RegisterValueChangedCallback(evt =>
            {
                var value = Mathf.Max(1, evt.newValue); ((ShipRoomBrush)target).CorridorWidth = value;
                width.SetValueWithoutNotify(value); SceneView.RepaintAll();
            });
            root.Add(width);
            root.Add(new Label("Even widths place the extra cell east/north. Esc cancels a stroke."));
            return root;
        }

        public override void OnPaintInspectorGUI()
        {
            EditorGUILayout.HelpBox(tooltip + " Unresolved spans/joins have Scene markers; select the room to focus individual Inspector issues. Missing-floor cells remain visible as editor guides.", MessageType.Info);
            var brush = (ShipRoomBrush)target;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Box action");
            var mode = (ShipRoomBrush.BoxEditMode)GUILayout.Toolbar((int)brush.BoxMode, new[] { "Fill", "Erase" });
            EditorGUILayout.EndHorizontal();
            if (mode != brush.BoxMode) SetBoxMode(mode);
            var width = Mathf.Max(1, EditorGUILayout.IntField("Corridor width (cells)", brush.CorridorWidth));
            if (width != brush.CorridorWidth) { brush.CorridorWidth = width; SceneView.RepaintAll(); }
            EditorGUILayout.LabelField("Even widths extend east/north; Esc cancels.", EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(brush.StrokeIssue)) EditorGUILayout.HelpBox(brush.StrokeIssue, MessageType.Error);
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

        public override void OnSceneGUI(GridLayout gridLayout, GameObject brushTarget)
        {
            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                ((ShipRoomBrush)target).CancelGesture();
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }
            base.OnSceneGUI(gridLayout, brushTarget);
        }

        public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            if (!executing) ((ShipRoomBrush)target).EndGesture();
            RoomAuthoringPaletteSetup.ActivateTarget(brushTarget);
            RoomAuthoringPreview.SetEditedRoom(brushTarget == null ? null : brushTarget.GetComponentInParent<ShipRoomAuthoring>(true));
            if (ShipRoomBrush.TryGetRoom(gridLayout, brushTarget, out var room) && room.SpaceType == LogicalSpaceType.Corridor &&
                (tool == GridBrushBase.Tool.Paint || tool == GridBrushBase.Tool.Erase))
            {
                var brush = (ShipRoomBrush)target;
                var cells = brush.PreviewCorridor(room, position.min, tool, out var issue);
                RoomAuthoringPreview.DrawCorridorPreview(room, cells, issue);
                return;
            }
            base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);
        }

        public override void OnMouseEnter() => RoomAuthoringPaletteSetup.ActivateTarget(GridPaintingState.scenePaintTarget);
        public override void OnMouseLeave() => ((ShipRoomBrush)target).EndGesture();
        public override void OnToolDeactivated(GridBrushBase.Tool tool)
        {
            ((ShipRoomBrush)target).EndGesture();
            RoomAuthoringPreview.SetEditedRoom(null);
        }

        public override void RegisterUndo(GameObject brushTarget, GridBrushBase.Tool tool)
        {
            var grid = brushTarget == null ? null : brushTarget.GetComponentInParent<Grid>();
            if (ShipRoomBrush.TryGetRoom(grid, brushTarget, out var room))
                ((ShipRoomBrush)target).BeginGesture(room, tool);
        }
    }
}
