using System;
using System.Linq;
using UnityEngine;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [CustomEditor(typeof(ShipLayoutAuthoring))]
    public sealed class ShipLayoutAuthoringEditor : UnityEditor.Editor
    {
        private Vector2Int connectionStart;
        private bool connectionHorizontal = true;
        private int connectionWidth = 1;
        private LayoutConnectionType connectionType;
        private bool selectingBoundary;
        private string editingConnectionId;
        private Toggle boundarySelection;
        private Action refreshConnection;
        private Vector2IntField startField;
        private Toggle horizontalField;
        private string draftId = Guid.NewGuid().ToString("N");

        private LayoutConnectionDefinition Candidate(ShipLayoutAuthoring layout)
        {
            string negative = null, positive = null;
            if (RoomAuthoringOperations.GetSafetyIssue(layout, true) == null)
            {
                var topology = RoomAuthoringValidator.TopologyOf(layout);
                positive = topology.OwnersAt(connectionStart).Count == 1 ? topology.OwnersAt(connectionStart)[0] : null;
                if (connectionHorizontal ? connectionStart.y != int.MinValue : connectionStart.x != int.MinValue)
                {
                    var owners = topology.OwnersAt(connectionStart - (connectionHorizontal ? Vector2Int.up : Vector2Int.right));
                    negative = owners.Count == 1 ? owners[0] : null;
                }
            }
            return new LayoutConnectionDefinition(editingConnectionId ?? draftId, negative, positive, connectionStart, connectionHorizontal, connectionWidth, connectionType);
        }

        private void OnSceneGUI()
        {
            if (!selectingBoundary || !RoomAuthoringPreview.CanShow) return;
            var layout = (ShipLayoutAuthoring)target;
            var current = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            if (new Plane(Vector3.forward, layout.transform.position).Raycast(ray, out var distance))
            {
                var p = ray.GetPoint(distance) - layout.transform.position;
                if (current.type == EventType.MouseDown && current.button == 0 && !current.alt)
                {
                    connectionHorizontal = Mathf.Abs(p.y - Mathf.Round(p.y)) <= Mathf.Abs(p.x - Mathf.Round(p.x));
                    connectionStart = connectionHorizontal ? new Vector2Int(Mathf.FloorToInt(p.x), Mathf.RoundToInt(p.y)) :
                        new Vector2Int(Mathf.RoundToInt(p.x), Mathf.FloorToInt(p.y));
                    startField.SetValueWithoutNotify(connectionStart); horizontalField.SetValueWithoutNotify(connectionHorizontal);
                    refreshConnection?.Invoke(); current.Use();
                }
            }
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            { selectingBoundary = false; boundarySelection?.SetValueWithoutNotify(false); SceneView.RepaintAll(); current.Use(); }
            RoomAuthoringPreview.DrawConnectionPreview(layout, Candidate(layout), editingConnectionId);
        }

        private VisualElement ConnectionControls(ShipLayoutAuthoring layout)
        {
            var controls = new Foldout { text = "Boundary connections", value = true };
            controls.Add(new HelpBox("Select a shared boundary in Scene view, then choose width and type. Width runs east for horizontal boundaries and north for vertical boundaries. Escape exits selection. Doorway and airlock treatments currently use an unframed structural gap.", HelpBoxMessageType.Info));
            var picking = boundarySelection = new Toggle("Select boundary in Scene");
            picking.RegisterValueChangedCallback(e => { selectingBoundary = e.newValue; SceneView.RepaintAll(); }); controls.Add(picking);
            startField = new Vector2IntField("Boundary start") { value = connectionStart };
            horizontalField = new Toggle("Horizontal (eastward)") { value = connectionHorizontal };
            var widthField = new IntegerField("Opening width (cells)") { value = connectionWidth };
            var typeField = new EnumField("Connection type", connectionType);
            var feedback = new HelpBox("", HelpBoxMessageType.Info);
            var list = new VisualElement();
            var add = new Button(() =>
            {
                string issue;
                var changed = editingConnectionId == null ? RoomAuthoringOperations.AddConnection(layout, Candidate(layout), out issue) :
                    RoomAuthoringOperations.UpdateConnection(layout, editingConnectionId, Candidate(layout), out issue);
                if (changed) { editingConnectionId = null; draftId = Guid.NewGuid().ToString("N"); }
                feedback.text = issue ?? "Connection added. Select another boundary to place another connection.";
                refreshConnection?.Invoke();
            }) { text = "Place connection" };
            string previous = null;
            refreshConnection = () =>
            {
                if (layout == null) return;
                var candidate = Candidate(layout);
                var issue = RoomAuthoringOperations.GetConnectionIssue(layout, candidate, editingConnectionId);
                add.text = editingConnectionId == null ? "Place connection" : "Apply connection correction";
                feedback.text = issue ?? "Valid shared span: " + candidate.NegativeOwner + " → " + candidate.PositiveOwner;
                feedback.messageType = issue == null ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                add.SetEnabled(issue == null);
                var key = string.Join("|", layout.Connections.Select(c => c == null ? "<missing>" : JsonUtility.ToJson(c)));
                if (previous == key) return;
                previous = key; list.Clear();
                foreach (var definition in layout.Connections.Where(c => c != null))
                {
                    var record = definition;
                    list.Add(new Label(record.Type + " — " + record.Start + " / " + record.Width + " cells — " + record.Id));
                    list.Add(new Button(() =>
                    {
                        editingConnectionId = record.Id;
                        connectionStart = Vector2Int.RoundToInt(record.Start); connectionHorizontal = record.Horizontal;
                        connectionWidth = record.Width; connectionType = record.Type;
                        startField.SetValueWithoutNotify(connectionStart); horizontalField.SetValueWithoutNotify(connectionHorizontal);
                        widthField.SetValueWithoutNotify(connectionWidth); typeField.SetValueWithoutNotify(connectionType);
                        refreshConnection?.Invoke(); SceneView.RepaintAll();
                    }) { text = "Correct " + record.Type });
                    list.Add(new Button(() =>
                    {
                        RoomAuthoringOperations.RemoveConnection(layout, record.Id, out var removalIssue);
                        if (removalIssue != null) feedback.text = removalIssue;
                        else if (editingConnectionId == record.Id) editingConnectionId = null;
                        refreshConnection?.Invoke();
                    }) { text = "Remove " + record.Type });
                }
            };
            startField.RegisterValueChangedCallback(e => { connectionStart = e.newValue; refreshConnection(); SceneView.RepaintAll(); });
            horizontalField.RegisterValueChangedCallback(e => { connectionHorizontal = e.newValue; refreshConnection(); SceneView.RepaintAll(); });
            widthField.RegisterValueChangedCallback(e => { connectionWidth = e.newValue; refreshConnection(); SceneView.RepaintAll(); });
            typeField.RegisterValueChangedCallback(e => { connectionType = (LayoutConnectionType)e.newValue; refreshConnection(); });
            controls.Add(startField); controls.Add(horizontalField); controls.Add(widthField); controls.Add(typeField);
            controls.Add(feedback); controls.Add(add);
            controls.Add(new Button(() => { editingConnectionId = null; refreshConnection(); SceneView.RepaintAll(); }) { text = "Cancel connection correction" });
            controls.Add(list);
            controls.schedule.Execute(() => refreshConnection?.Invoke()).Every(300);
            refreshConnection(); return controls;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new Label("Painted space occupancy is authoritative. This layout owns generated architecture."));
            foreach (var name in new[] { "layoutId", "spaces", "generatedOutput" })
            {
                var field = new PropertyField(serializedObject.FindProperty(name));
                field.SetEnabled(name == "defaultStyle");
                root.Add(field);
            }
            var layout = (ShipLayoutAuthoring)target;
            root.Add(RoomAuthoringStyleControls.ForLayout(layout));
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
                spaceControls.Add(RoomAuthoringStyleControls.ForSpace(room));
                spaceControls.Add(RoomFloorPanelControls.ForSpace(room));
                spaceControls.Add(new Button(() => Selection.activeGameObject = room.gameObject) { text = "Select Space" });
                spaceControls.Add(new Button(() => RoomAuthoringPaletteSetup.EditRoom(room)) { text = "Edit Space in Tile Palette" });
                if (room.SpaceType == LogicalSpaceType.Corridor)
                    spaceControls.Add(new HelpBox("Choose Paint or Erase in Tile Palette and set Corridor width. Drag a route through bends or existing branches. The preview shows the full stroke; release to apply it. Even widths extend east/north.", HelpBoxMessageType.Info));
            }
            root.Add(new Button(() => Selection.activeGameObject = RoomAuthoringCreation.CreateSpace(layout).gameObject) { text = "New Room Space" });
            root.Add(new Button(() => Selection.activeGameObject = RoomAuthoringCreation.CreateSpace(layout, LogicalSpaceType.Corridor).gameObject) { text = "New Corridor Space" });
            root.Add(ConnectionControls(layout));
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
