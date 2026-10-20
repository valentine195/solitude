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
            root.Add(new HelpBox("Choose Fill or Erase under Box action in the room brush, then drag a rectangle. Undo/Redo restores each gesture. After edits with another tool, use Validate Room and Rebuild Room.", HelpBoxMessageType.Info));
            var outcome = new HelpBox("", HelpBoxMessageType.Info);
            void RefreshOutcome()
            {
                var saved = room.GenerationResult;
                outcome.text = saved == null ? "No recorded preview. Rebuild Room to create a current result." :
                    (saved.Status == RoomGenerationStatus.IncompletePreview ? "Incomplete preview" : saved.Status.ToString()) +
                    (RoomAuthoringValidator.IsCurrent(room) ? " — current." : " — output not updated; Rebuild Room.");
            }
            RefreshOutcome();
            root.Add(outcome);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshOutcome());
            var validation = new HelpBox("", HelpBoxMessageType.Info);
            validation.SetEnabled(false);
            root.Add(new Button(() =>
            {
                var assessment = RoomAuthoringValidator.Assess(room, out _);
                validation.text = (assessment.Issues.Length == 0 ? "Current inputs: " + assessment.Status + "." : assessment.Summary) +
                    (RoomAuthoringValidator.IsCurrent(room) ? " Preview is current." : " Stored output is not current; Rebuild Room.");
                RefreshOutcome();
                validation.SetEnabled(true);
            }) { text = "Validate Room" });
            root.Add(validation);
            root.Add(new Button(() => { RoomAuthoringOperations.Rebuild(room); serializedObject.Update(); RefreshOutcome(); }) { text = "Rebuild Room" });
            return root;
        }
    }
}
