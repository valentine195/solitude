using System;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Rendering;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public sealed class RoomPreviewFeedback
    {
        public RoomGenerationResult Assessment;
        public Vector2Int[] MissingFloorCells = Array.Empty<Vector2Int>();
        public bool IsCurrent;
        public bool IsEditing;
        public Vector2Int[] PendingAddedCells = Array.Empty<Vector2Int>();
        public Vector2Int[] PendingRemovedCells = Array.Empty<Vector2Int>();
    }

    [InitializeOnLoad]
    public static class RoomAuthoringPreview
    {
        private static ShipRoomAuthoring editedRoom;
        private static ShipRoomAuthoring issueRoom;
        private static ShipLayoutAuthoring issueLayout;
        private static string selectedIssueId;
        public static string SelectedIssueId { get { PruneSelection(); return selectedIssueId; } }

        static RoomAuthoringPreview()
        {
            SceneView.duringSceneGui += Draw;
            Selection.selectionChanged += Repaint;
            Undo.undoRedoPerformed += Repaint;
            RoomAuthoringOperations.GenerationCompleted += OnGenerated;
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Detach;
        }

        public static bool CanShow => !EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer;
        public static ShipRoomAuthoring CurrentRoom
        {
            get
            {
                if (!CanShow) return null;
                var selected = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<ShipRoomAuthoring>(true);
                if (selected != null) return selected;
                if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<ShipLayoutAuthoring>(true) != null) return null;
                return editedRoom != null && GridPaintingState.gridBrush is ShipRoomBrush &&
                    GridPaintingState.scenePaintTarget == editedRoom.Occupancy?.gameObject ? editedRoom : null;
            }
        }

        public static ShipLayoutAuthoring CurrentLayout => !CanShow ? null : CurrentRoom != null ? CurrentRoom.Layout :
            Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<ShipLayoutAuthoring>(true);

        public static void SetEditedRoom(ShipRoomAuthoring room) { if (editedRoom == room) return; editedRoom = room; Repaint(); }
        public static void ClearSelection() { editedRoom = null; issueRoom = null; issueLayout = null; selectedIssueId = null; SceneView.RepaintAll(); }
        private static void OnGenerated(ShipRoomAuthoring room) => Repaint();
        private static void OnPlayMode(PlayModeStateChange state) => ClearSelection();
        private static void Repaint() { PruneSelection(); SceneView.RepaintAll(); }
        private static void PruneSelection()
        {
            RoomGenerationResult result;
            if (issueLayout != null && issueLayout == CurrentLayout)
                result = RoomAuthoringValidator.Assess(issueLayout, out _);
            else if (issueRoom != null && issueRoom == CurrentRoom)
                result = RoomAuthoringValidator.Assess(issueRoom, out _);
            else { issueRoom = null; issueLayout = null; selectedIssueId = null; return; }
            if (!result.Issues.Any(i => i.Id == selectedIssueId)) selectedIssueId = null;
        }

        public static RoomPreviewFeedback GetFeedback(ShipRoomAuthoring room)
        {
            if (!CanShow || room == null) return new RoomPreviewFeedback();
            var result = RoomAuthoringValidator.Assess(room, out var placement);
            var missing = placement != null && placement.Floor == null ? placement.Geometry.Cells : Array.Empty<Vector2Int>();
            if (room.Layout != null && room.Layout.Spaces.Count > 1)
            {
                RoomAuthoringValidator.AssessSpaces(room.Layout, out var placements);
                if (placements.TryGetValue(room, out var own) && own.Floor == null) missing = own.Geometry.Cells;
            }
            var editing = room.Layout == null ? RoomAuthoringOperations.IsEditing(room) : room.Layout.Spaces.Any(RoomAuthoringOperations.IsEditing);
            return new RoomPreviewFeedback
            {
                Assessment = result,
                IsEditing = editing,
                PendingAddedCells = RoomAuthoringOperations.PendingCells(room, false),
                PendingRemovedCells = RoomAuthoringOperations.PendingCells(room, true),
                MissingFloorCells = missing,
                IsCurrent = !editing && RoomAuthoringValidator.IsCurrent(room)
            };
        }

        public static RoomPreviewFeedback GetFeedback(ShipLayoutAuthoring layout)
        {
            if (!CanShow || layout == null) return new RoomPreviewFeedback();
            var result = RoomAuthoringValidator.AssessSpaces(layout, out var placements);
            return new RoomPreviewFeedback
            {
                Assessment = result,
                IsEditing = layout.Spaces.Any(RoomAuthoringOperations.IsEditing),
                IsCurrent = !layout.Spaces.Any(RoomAuthoringOperations.IsEditing) && RoomAuthoringValidator.IsCurrent(layout),
                MissingFloorCells = placements.Where(p => p.Value.Floor == null)
                    .SelectMany(p => p.Value.Geometry.Cells.Select(c => c + RoomAuthoringValidator.LayoutOffset(layout, p.Key))).ToArray()
            };
        }

        public static string FieldLabel(RoomGenerationIssue issue) => issue.HasRegion ? issue.Field :
            issue.Kind == RoomIssueKind.Floor ? "style.floorTile" : issue.Kind == RoomIssueKind.Style ? "style" : "room ownership / coordinates";

        public static void DrawCorridorPreview(ShipRoomAuthoring room, Vector2Int[] cells, string issue)
        {
            var color = Handles.color; var depth = Handles.zTest;
            try
            {
                Handles.zTest = CompareFunction.Always;
                foreach (var cell in cells)
                {
                    var origin = room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
                    Handles.DrawSolidRectangleWithOutline(new[] { origin, origin + Vector3.right, origin + Vector3.right + Vector3.up, origin + Vector3.up },
                        issue == null ? new Color(.1f, .8f, 1f, .2f) : new Color(1f, .2f, .1f, .2f), issue == null ? Color.cyan : Color.red);
                }
                if (issue != null && cells.Length != 0)
                    Handles.Label(room.Occupancy.CellToWorld(new Vector3Int(cells[0].x, cells[0].y, 0)), issue);
            }
            finally { Handles.color = color; Handles.zTest = depth; }
        }

        public static string IssueLabel(RoomGenerationIssue issue) => issue.RoomId + " — " + FieldLabel(issue) +
            (issue.HasRegion ? " at " + issue.Start + " → " + issue.End : "");

        private static Vector3 IssueWorld(ShipRoomAuthoring room, Vector2Int cell) =>
            room.Layout != null && room.Layout.Spaces.Count > 1 ? room.Layout.transform.position + (Vector3)(Vector2)cell :
            room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0));

        private static Vector3 IssueWorld(ShipLayoutAuthoring layout, Vector2Int cell) => layout.Spaces.Count == 1 ?
            IssueWorld(layout.Spaces[0], cell) : layout.transform.position + (Vector3)(Vector2)cell;

        private static Bounds RegionBounds(Vector3 a, Vector3 b) => new Bounds((a + b) * .5f,
            Vector3.Max(new Vector3(.75f, .75f, .1f), new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), .1f)));

        public static Bounds IssueBounds(ShipRoomAuthoring room, RoomGenerationIssue issue) =>
            RegionBounds(IssueWorld(room, issue.Start), IssueWorld(room, issue.End));

        public static Bounds IssueBounds(ShipLayoutAuthoring layout, RoomGenerationIssue issue) =>
            RegionBounds(IssueWorld(layout, issue.Start), IssueWorld(layout, issue.End));

        public static bool SelectIssue(ShipRoomAuthoring room, string id, bool frame = true)
        {
            if (!CanShow || room == null || CurrentRoom != room) return false;
            var issue = RoomAuthoringValidator.Assess(room, out _).Issues.FirstOrDefault(i => i.Id == id);
            if (issue == null) return false;
            issueRoom = room; issueLayout = room.Layout; selectedIssueId = id;
            Focus(issue, issue.HasRegion ? IssueBounds(room, issue) : default, room.Style == null ? (UnityEngine.Object)room.gameObject : room.Style, frame);
            return true;
        }

        public static bool SelectIssue(ShipLayoutAuthoring layout, string id, bool frame = true)
        {
            if (!CanShow || layout == null || CurrentLayout != layout) return false;
            var issue = RoomAuthoringValidator.Assess(layout, out _).Issues.FirstOrDefault(i => i.Id == id);
            if (issue == null) return false;
            issueRoom = null; issueLayout = layout; selectedIssueId = id;
            var owner = layout.Spaces.FirstOrDefault(r => r != null && r.RoomId == issue.RoomId);
            Focus(issue, issue.HasRegion ? IssueBounds(layout, issue) : default, owner == null ? (UnityEngine.Object)layout : owner.Style == null ? owner : owner.Style, frame);
            return true;
        }

        private static void Focus(RoomGenerationIssue issue, Bounds bounds, UnityEngine.Object target, bool frame)
        {
            if (frame)
            {
                if (issue.HasRegion && SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(bounds, true);
                else if (!issue.HasRegion) EditorGUIUtility.PingObject(target);
            }
            SceneView.RepaintAll();
        }

        private static void Draw(SceneView view)
        {
            var room = CurrentRoom;
            var layout = CurrentLayout;
            if (room == null && layout == null) return;
            PruneSelection();
            var feedback = room != null ? GetFeedback(room) : GetFeedback(layout);
            var result = feedback.Assessment;
            if (result == null) return;
            var color = Handles.color;
            var depth = Handles.zTest;
            try
            {
                Handles.zTest = CompareFunction.Always;
                foreach (var cell in feedback.PendingAddedCells.Concat(feedback.PendingRemovedCells))
                {
                    var origin = room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
                    var removed = feedback.PendingRemovedCells.Contains(cell);
                    Handles.DrawSolidRectangleWithOutline(new[] { origin, origin + Vector3.right, origin + Vector3.right + Vector3.up, origin + Vector3.up },
                        removed ? new Color(1f, .2f, .1f, .25f) : new Color(.1f, .8f, 1f, .25f), removed ? Color.red : Color.cyan);
                    Handles.Label(origin + Vector3.one * .25f, removed ? "− pending" : "+ pending");
                }
                foreach (var cell in feedback.MissingFloorCells)
                {
                    var origin = room != null ? room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0)) :
                        layout.transform.position + (Vector3)(Vector2)cell;
                    Handles.DrawSolidRectangleWithOutline(new[] { origin, origin + Vector3.right, origin + Vector3.right + Vector3.up, origin + Vector3.up },
                        new Color(1f, .3f, .15f, .12f), new Color(1f, .4f, .15f, .8f));
                }
                foreach (var issue in result.Issues.Where(i => i.HasRegion))
                {
                    var a = room != null ? IssueWorld(room, issue.Start) : IssueWorld(layout, issue.Start);
                    var b = room != null ? IssueWorld(room, issue.End) : IssueWorld(layout, issue.End);
                    var selected = selectedIssueId == issue.Id;
                    var bounds = room != null ? IssueBounds(room, issue) : IssueBounds(layout, issue);
                    Handles.color = selected ? Color.yellow : Color.red;
                    if (a != b) Handles.DrawAAPolyLine(4, a, b);
                    var center = (a + b) * .5f;
                    Handles.DrawWireDisc(center, Vector3.forward, selected ? .22f : .14f);
                    Handles.Label(center + Vector3.up * .18f, "! " + IssueLabel(issue));
                    if (selected) Handles.DrawWireCube(bounds.center, bounds.size);
                }
            }
            finally { Handles.color = color; Handles.zTest = depth; }
        }

        private static void Detach()
        {
            SceneView.duringSceneGui -= Draw;
            Selection.selectionChanged -= Repaint;
            Undo.undoRedoPerformed -= Repaint;
            RoomAuthoringOperations.GenerationCompleted -= OnGenerated;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            ClearSelection();
        }
    }
}
