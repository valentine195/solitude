using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomEditor(typeof(ShipLayoutAuthoring))]
    public sealed class ShipLayoutAuthoringEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Painted space occupancy is authoritative. This layout owns generated architecture."));
            foreach (var name in new[] { "layoutId", "defaultStyle", "spaces", "generatedOutput" })
            {
                var field = new PropertyField(serializedObject.FindProperty(name));
                field.SetEnabled(name == "defaultStyle");
                root.Add(field);
            }
            var layout = (ShipLayoutAuthoring)target;
            var feedback = new HelpBox("", HelpBoxMessageType.Info);
            var issues = new VisualElement();
            var focused = new Label();
            string previous = null;
            void Refresh()
            {
                if (layout == null) return;
                var preview = RoomAuthoringPreview.GetFeedback(layout);
                var result = preview.Assessment;
                if (result == null) { feedback.text = "Editor preview is hidden in Play Mode and builds."; issues.Clear(); return; }
                feedback.text = result.Status + (preview.IsCurrent ? " — current." : " — output not updated; Rebuild Layout.") +
                    (result.Issues.Length == 0 ? "" : "\n" + result.Summary);
                feedback.messageType = result.Status == RoomGenerationStatus.Refused ? HelpBoxMessageType.Error :
                    result.Issues.Length == 0 ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                var key = feedback.text + string.Join("|", result.Issues.Select(i => i.Id + ":" + i.Reason));
                if (key == previous) return;
                previous = key; issues.Clear(); focused.text = "";
                foreach (var issue in result.Issues)
                {
                    issues.Add(new HelpBox(RoomAuthoringPreview.IssueLabel(issue) + "\n" + issue.Reason,
                        result.Status == RoomGenerationStatus.Refused ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));
                    issues.Add(new Button(() =>
                    {
                        if (RoomAuthoringPreview.SelectIssue(layout, issue.Id)) focused.text = "Focused: " + RoomAuthoringPreview.IssueLabel(issue);
                    }) { text = (issue.HasRegion ? "Focus " : "Locate ") + RoomAuthoringPreview.FieldLabel(issue) });
                }
            }
            foreach (var room in layout.Spaces)
            {
                if (room == null) continue;
                var spaceControls = new Foldout { text = room.name + " — " + room.RoomId, value = layout.Spaces.Count == 1 };
                root.Add(spaceControls);
                if (!string.IsNullOrEmpty(room.GeneratedOutput == null ? null : room.GeneratedOutput.AdoptionRecord))
                    spaceControls.Add(new HelpBox(room.GeneratedOutput.AdoptionRecord, HelpBoxMessageType.Info));
                var data = new SerializedObject(room);
                foreach (var name in new[] { "spaceType", "authoringMetadata" })
                {
                    var field = new PropertyField(data.FindProperty(name));
                    field.Bind(data);
                    spaceControls.Add(field);
                }
                spaceControls.Add(new Button(() => Selection.activeGameObject = room.gameObject) { text = "Select Space" });
                spaceControls.Add(new Button(() => RoomAuthoringPaletteSetup.EditRoom(room)) { text = "Edit Space in Tile Palette" });
                if (room.SpaceType == LogicalSpaceType.Corridor)
                    spaceControls.Add(new HelpBox("Choose Paint or Erase in Tile Palette and set Corridor width. Drag a route through bends or existing branches. The preview shows the full stroke; release to apply it. Even widths extend east/north.", HelpBoxMessageType.Info));
            }
            root.Add(new Button(() => Selection.activeGameObject = RoomAuthoringCreation.CreateSpace(layout).gameObject) { text = "New Room Space" });
            root.Add(new Button(() => Selection.activeGameObject = RoomAuthoringCreation.CreateSpace(layout, LogicalSpaceType.Corridor).gameObject) { text = "New Corridor Space" });
            root.Add(feedback);
            root.Add(issues);
            root.Add(focused);
            root.Add(new Button(Refresh) { text = "Validate Layout" });
            root.Add(new Button(() => { RoomAuthoringOperations.Rebuild(layout); serializedObject.Update(); Refresh(); }) { text = "Rebuild Layout" });
            root.schedule.Execute(Refresh).Every(300);
            Refresh();
            return root;
        }
    }
}
