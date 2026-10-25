using System;
using System.Linq;
using System.Collections.Generic;
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
            root.Add(RoomFloorPanelControls.ForSpace(room));
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
            RoomAuthoringPreview.TrackInspectorFeedback(root, RefreshFeedback);
            RefreshFeedback();
            return root;
        }
    }
    public static class RoomFloorPanelControls
    {
        public static VisualElement ForSpace(ShipRoomAuthoring room)
        {
            var root = new Foldout { text = "Procedural floor panels", name = "floorPanelControls", value = room.FloorPanels.Enabled };
            root.Add(new HelpBox("Draft changes apply together as one Undo action. Footprint changes may repanel this space. Palette and thickness preserve the partition. Configure style sorting above floors and below structure (for example 0 / 1 / 2).", HelpBoxMessageType.Info));
            var inputs = new VisualElement(); var rows = new VisualElement();
            var feedback = new HelpBox("", HelpBoxMessageType.Info) { name = "panelFeedback" };
            var counts = new Label { name = "panelRetainedCounts" };
            var settings = room.FloorPanels.Copy();
            var records = room.FloorReservations.Select(r => r?.Copy()).ToList();
            var baseline = ""; var dirty = false;
            string CurrentKey() => RoomFloorPanelDomain.InputKey(room.FloorPanels, room.FloorReservations);
            void Changed() { dirty = true; RoomAuthoringPreview.ClearFloorPanelPreview(); }
            void DrawReservations()
            {
                rows.Clear();
                for (var n = 0; n < records.Count; n++)
                {
                    var index = n; var record = records[index]; var row = new VisualElement();
                    row.Add(new Label(record == null ? "Missing reservation record" : record.Id + " — owner " + record.SpaceId));
                    if (record != null)
                    {
                        var position = new Vector2Field("Cell position (integers)") { value = record.Position };
                        var size = new Vector2Field("Cell size (positive integers)") { value = record.Size };
                        position.RegisterValueChangedCallback(e => { var old = records[index]; records[index] = new RoomFloorReservation(old.Id, old.SpaceId, e.newValue, old.Size); Changed(); });
                        size.RegisterValueChangedCallback(e => { var old = records[index]; records[index] = new RoomFloorReservation(old.Id, old.SpaceId, old.Position, e.newValue); Changed(); });
                        row.Add(position); row.Add(size);
                        if (record.SpaceId != room.RoomId)
                            row.Add(new Button(() => { var old = records[index]; records[index] = new RoomFloorReservation(old.Id, room.RoomId, old.Position, old.Size); Changed(); DrawReservations(); }) { text = "Associate with this space" });
                    }
                    row.Add(new Button(() => { records.RemoveAt(index); Changed(); DrawReservations(); }) { text = "Remove reservation" });
                    rows.Add(row);
                }
            }
            void DrawSettings()
            {
                inputs.Clear();
                var enabled = new Toggle("Enabled") { name = "panelEnabled", value = settings.Enabled };
                enabled.RegisterValueChangedCallback(e => { settings.Enabled = e.newValue; Changed(); }); inputs.Add(enabled);
                var seed = new IntegerField("Seed") { name = "panelSeed", value = settings.Seed, isDelayed = true };
                seed.RegisterValueChangedCallback(e => { settings.Seed = e.newValue; Changed(); }); inputs.Add(seed);
                var version = new IntegerField("Generator version (supported: 1)") { value = settings.Version, isDelayed = true };
                version.RegisterValueChangedCallback(e => { settings.Version = e.newValue; Changed(); }); inputs.Add(version);
                var min = new Vector2IntField("Preferred minimum (pixels)") { value = settings.PreferredMinPixels };
                var max = new Vector2IntField("Preferred maximum (pixels)") { value = settings.PreferredMaxPixels };
                min.RegisterValueChangedCallback(e => { settings.PreferredMinPixels = e.newValue; Changed(); });
                max.RegisterValueChangedCallback(e => { settings.PreferredMaxPixels = e.newValue; Changed(); }); inputs.Add(min); inputs.Add(max);
                var density = new FloatField("Seam density (0–1)") { value = settings.Density, isDelayed = true };
                density.RegisterValueChangedCallback(e => { settings.Density = e.newValue; Changed(); }); inputs.Add(density);
                var thickness = new IntegerField("Seam thickness (1–3 pixels)") { value = settings.SeamThickness, isDelayed = true };
                thickness.RegisterValueChangedCallback(e => { settings.SeamThickness = e.newValue; Changed(); }); inputs.Add(thickness);
                var palette = new VisualElement();
                void DrawPalette()
                {
                    palette.Clear();
                    for (var i = 0; i < (settings.SeamPalette?.Length ?? 0); i++)
                    {
                        var index = i; var color = new ColorField("Seam color " + (i + 1)) { value = settings.SeamPalette[i] };
                        color.RegisterValueChangedCallback(e => { settings.SeamPalette[index] = e.newValue; Changed(); }); palette.Add(color);
                        palette.Add(new Button(() => { settings.SeamPalette = settings.SeamPalette.Where((c, n) => n != index).ToArray(); Changed(); DrawPalette(); }) { text = "Remove color" });
                    }
                }
                DrawPalette(); inputs.Add(palette);
                inputs.Add(new Button(() => { settings.SeamPalette = (settings.SeamPalette ?? Array.Empty<Color>()).Append(Color.gray).ToArray(); Changed(); DrawPalette(); }) { text = "Add seam color" });
            }
            void Reload()
            {
                settings = room.FloorPanels.Copy(); records = room.FloorReservations.Select(r => r?.Copy()).ToList();
                baseline = CurrentKey(); dirty = false; DrawSettings(); DrawReservations();
            }
            bool Ready()
            {
                if (!dirty) return true;
                feedback.text = "Apply or reload the draft before this action."; return false;
            }
            root.Add(inputs); root.Add(rows);
            root.Add(new Button(() => { records.Add(new RoomFloorReservation(Guid.NewGuid().ToString("N"), room.RoomId, Vector2.zero, Vector2.one)); Changed(); DrawReservations(); }) { text = "Add equipment reservation", name = "panelAddReservation" });
            root.Add(new Button(() =>
            {
                if (!RoomAuthoringPreview.ShowFloorPanelPreview(room, settings, records)) { feedback.text = "Finish the active stroke; preview is available in Edit Mode."; return; }
                var preview = RoomAuthoringPreview.GetFloorPanelPreview(room);
                feedback.text = preview.Partition.Rectangles.Count + " proposed panels; " + preview.Domain.ExcludedCells.Count + " excluded cells. " +
                    string.Join("\n", preview.Partition.Issues.Concat(preview.Partition.Advisories).Select(i => i.Field + ": " + i.Reason));
                feedback.messageType = preview.Partition.Success ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            }) { text = "Preview", name = "panelPreview" });
            root.Add(new Button(RoomAuthoringPreview.ClearFloorPanelPreview) { text = "Clear preview" });
            root.Add(new Button(() =>
            {
                if (baseline != CurrentKey()) { feedback.text = "Panel inputs changed elsewhere. Reload the draft before applying."; return; }
                if (!RoomAuthoringOperations.EditFloorPanelAuthoring(room, settings, records, out var issue)) { feedback.text = issue; feedback.messageType = HelpBoxMessageType.Warning; return; }
                Reload(); feedback.text = room.GenerationResult.IsSuccessful ? "Panel inputs applied." : room.GenerationDiagnostic;
            }) { text = "Apply panel inputs", name = "panelApply" });
            root.Add(new Button(() => { Reload(); RoomAuthoringPreview.ClearFloorPanelPreview(); feedback.text = "Draft reloaded."; }) { text = "Reload / cancel draft", name = "panelReload" });
            root.Add(new Button(() => { if (!Ready()) return; RoomAuthoringOperations.RegenerateFloorPanels(room, out var issue); feedback.text = issue ?? "Regenerated with the same seed."; }) { text = "Regenerate", name = "panelRegenerate" });
            root.Add(new Button(() => { if (!Ready()) return; RoomAuthoringOperations.NewFloorPanelSeed(room, out var issue); feedback.text = issue ?? "New seed applied."; if (issue == null) Reload(); }) { text = "New Seed", name = "panelNewSeed" });
            root.Add(new Button(() => { if (!Ready()) return; RoomAuthoringOperations.DisableFloorPanels(room, out var issue); feedback.text = issue ?? "Paneling disabled; settings retained."; if (issue == null) Reload(); }) { text = "Disable", name = "panelDisable" });
            root.Add(counts); root.Add(feedback);
            root.schedule.Execute(() =>
            {
                if (room == null) return;
                counts.text = "Retained panel revisions (managed store): " + RoomFloorPanelAssetStore.RetainedRevisionCount;
                if (!dirty && baseline != CurrentKey()) Reload();
            }).Every(300);
            Reload(); return root;
        }
    }

}
