using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomEditor(typeof(ShipRoomAuthoring))]
    public sealed class ShipRoomAuthoringEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Painted occupancy is authoritative. Generated architecture is replaceable."));
            foreach (var name in new[] { "roomId", "style", "occupancy", "generatedOutput", "generationDiagnostic" })
            {
                var field = new PropertyField(serializedObject.FindProperty(name));
                field.SetEnabled(name == "style");
                root.Add(field);
            }
            var room = (ShipRoomAuthoring)target;
            var issue = RoomAuthoringCreation.GetOwnershipIssue(room);
            if (issue != null) root.Add(new HelpBox(issue, HelpBoxMessageType.Error));
            if (room.Style == null) root.Add(new HelpBox("Assign a prepared room style. Production artwork is not supplied by this tool.", HelpBoxMessageType.Info));
            root.Add(new Button(() => Selection.activeGameObject = room.Occupancy == null ? room.gameObject : room.Occupancy.gameObject)
                { text = "Select Authored Occupancy" });
            root.Add(new Button(() => RoomAuthoringPaletteSetup.EditRoom(room)) { text = "Edit Room in Tile Palette" });
            var validation = new HelpBox("", HelpBoxMessageType.Info);
            validation.SetEnabled(false);
            root.Add(new Button(() =>
            {
                validation.text = RoomAuthoringValidator.Validate(room) ?? "Room configuration is valid.";
                validation.SetEnabled(true);
            }) { text = "Validate Room" });
            root.Add(validation);
            root.Add(new Button(() => { RoomAuthoringOperations.Rebuild(room); serializedObject.Update(); }) { text = "Rebuild Room" });
            return root;
        }
    }
}
