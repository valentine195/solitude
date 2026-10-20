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
            return root;
        }

        public override void OnPaintInspectorGUI()
        {
            EditorGUILayout.HelpBox(tooltip, MessageType.Info);
            if (GUILayout.Button("Edit Selected Room"))
            {
                var room = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<ShipRoomAuthoring>(true);
                if (room != null) RoomAuthoringPaletteSetup.EditRoom(room);
            }
        }

        public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            RoomAuthoringPaletteSetup.ActivateTarget(brushTarget);
            base.OnPaintSceneGUI(gridLayout, brushTarget, position, tool, executing);
        }

        public override void OnMouseEnter() => RoomAuthoringPaletteSetup.ActivateTarget(GridPaintingState.scenePaintTarget);
    }
}
