using System.Linq;
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
            foreach (var name in new[] { "roomId", "occupancy", "generatedOutput", "generationDiagnostic" })
            {
                var field = new PropertyField(serializedObject.FindProperty(name));
                field.SetEnabled(name == "style");
                root.Add(field);
            }
            var room = (ShipRoomAuthoring)target;
            root.Add(RoomAuthoringStyleControls.ForSpace(room));
            var adoptionFeedback = new HelpBox("", HelpBoxMessageType.Info);
            var adopt = new Button(() =>
            {
                try { Selection.activeGameObject = RoomAuthoringCreation.AdoptIntoNewLayout(room).gameObject; }
                catch (System.InvalidOperationException exception) { adoptionFeedback.text = exception.Message; }
            }) { text = "Adopt into New Ship Layout" };
            root.Add(adopt);
            root.Add(adoptionFeedback);
            var issue = RoomAuthoringCreation.GetOwnershipIssue(room);
            if (issue != null) root.Add(new HelpBox(issue, HelpBoxMessageType.Error));
            if (room.Style == null) root.Add(new HelpBox("Assign a prepared room style. Production artwork is not supplied by this tool.", HelpBoxMessageType.Info));
            root.Add(new Button(() => Selection.activeGameObject = room.Occupancy == null ? room.gameObject : room.Occupancy.gameObject)
                { text = "Select Authored Occupancy" });
            root.Add(new Button(() => RoomAuthoringPaletteSetup.EditRoom(room)) { text = "Edit Room in Tile Palette" });
            root.Add(new HelpBox("Choose Fill or Erase under Box action in the room brush, then drag a rectangle. Undo/Redo restores each gesture. After edits with another tool, use Validate Room and Rebuild Room.", HelpBoxMessageType.Info));
            var outcome = new HelpBox("", HelpBoxMessageType.Info);
            var issues = new VisualElement();
            var focusedField = new Label();
            string previousFeedback = null;
            void RefreshFeedback()
            {
                if (room == null) return;
                var adoptionIssue = RoomAuthoringCreation.GetAdoptionIssue(room);
                adopt.SetEnabled(adoptionIssue == null);
                adoptionFeedback.text = room.Layout != null ? room.GeneratedOutput == null
                    ? "Layout output reference is missing." : string.IsNullOrEmpty(room.GeneratedOutput.AdoptionRecord)
                        ? "Layout space." : room.GeneratedOutput.AdoptionRecord
                    : adoptionIssue ?? "Adoption preserves this room and its painted map. One Undo restores independent ownership.";
                var feedback = RoomAuthoringPreview.GetFeedback(room);
                var result = feedback.Assessment;
                if (result == null) { outcome.text = "Editor preview is hidden in Play Mode and builds."; issues.Clear(); return; }
                var title = result.Status == RoomGenerationStatus.IncompletePreview ? "Incomplete preview" : result.Status.ToString();
                var summary = title + (feedback.IsCurrent ? " — current." : " — output not updated; Rebuild Room.");
                var key = summary + string.Join("|", result.Issues.Select(i => i.Id + ":" + i.Reason));
                outcome.text = summary;
                outcome.messageType = result.Status == RoomGenerationStatus.Refused ? HelpBoxMessageType.Error :
                    result.Issues.Length == 0 ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                if (key == previousFeedback) return;
                previousFeedback = key;
                focusedField.text = "";
                issues.Clear();
                foreach (var issue in result.Issues)
                {
                    var item = new VisualElement();
                    item.Add(new HelpBox(RoomAuthoringPreview.IssueLabel(issue) + "\n" + issue.Kind + ": " + issue.Reason, HelpBoxMessageType.Warning));
                    var field = RoomAuthoringPreview.FieldLabel(issue);
                    item.Add(new Button(() =>
                    {
                        if (RoomAuthoringPreview.SelectIssue(room, issue.Id))
                            focusedField.text = issue.HasRegion ? "Focused: " + field + " at " + issue.Start + " → " + issue.End : "Check field: " + field;
                    }) { text = (issue.HasRegion ? "Focus " : "Locate ") + field });
                    issues.Add(item);
                }
            }
            root.Add(outcome);
            root.Add(issues);
            root.Add(focusedField);
            root.Add(new Button(() => RefreshFeedback()) { text = "Validate Room" });
            root.Add(new Button(() => { RoomAuthoringOperations.Rebuild(room); serializedObject.Update(); RefreshFeedback(); }) { text = "Rebuild Room" });
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshFeedback());
            root.schedule.Execute(RefreshFeedback).Every(300);
            RefreshFeedback();
            return root;
        }
    }
}
