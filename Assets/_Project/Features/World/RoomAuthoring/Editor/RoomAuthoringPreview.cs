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
                return editedRoom != null && GridPaintingState.gridBrush is ShipRoomBrush &&
                    GridPaintingState.scenePaintTarget == editedRoom.Occupancy?.gameObject ? editedRoom : null;
            }
        }

        public static void SetEditedRoom(ShipRoomAuthoring room) { if (editedRoom == room) return; editedRoom = room; Repaint(); }
        public static void ClearSelection() { editedRoom = null; issueRoom = null; selectedIssueId = null; SceneView.RepaintAll(); }
        private static void OnGenerated(ShipRoomAuthoring room) => Repaint();
        private static void OnPlayMode(PlayModeStateChange state) => ClearSelection();
        private static void Repaint() { PruneSelection(); SceneView.RepaintAll(); }
        private static void PruneSelection()
        {
            if (issueRoom == null || issueRoom != CurrentRoom)
            { issueRoom = null; selectedIssueId = null; return; }
            var result = RoomAuthoringValidator.Assess(issueRoom, out _);
            if (!result.Issues.Any(i => i.Id == selectedIssueId)) selectedIssueId = null;
        }

        public static RoomPreviewFeedback GetFeedback(ShipRoomAuthoring room)
        {
            if (!CanShow || room == null) return new RoomPreviewFeedback();
            var result = RoomAuthoringValidator.Assess(room, out var placement);
            var saved = room.GenerationResult;
            return new RoomPreviewFeedback
            {
                Assessment = result,
                IsEditing = RoomAuthoringOperations.IsEditing(room),
                PendingAddedCells = RoomAuthoringOperations.PendingCells(room, false),
                PendingRemovedCells = RoomAuthoringOperations.PendingCells(room, true),
                MissingFloorCells = placement != null && placement.Floor == null ? placement.Geometry.Cells : Array.Empty<Vector2Int>(),
                IsCurrent = saved != null && saved.Status != RoomGenerationStatus.Refused && result.Status != RoomGenerationStatus.Refused &&
                    !string.IsNullOrEmpty(saved.InputFingerprint) && saved.InputFingerprint == result.InputFingerprint &&
                    saved.OutputFingerprint == RoomAuthoringValidator.OutputFingerprint(room)
            };
        }

        public static string FieldLabel(RoomGenerationIssue issue) => issue.HasRegion ? issue.Field :
            issue.Kind == RoomIssueKind.Floor ? "style.floorTile" : issue.Kind == RoomIssueKind.Style ? "style" : "room ownership / coordinates";

        public static Bounds IssueBounds(ShipRoomAuthoring room, RoomGenerationIssue issue)
        {
            var a = room.Occupancy.CellToWorld(new Vector3Int(issue.Start.x, issue.Start.y, 0));
            var b = room.Occupancy.CellToWorld(new Vector3Int(issue.End.x, issue.End.y, 0));
            return new Bounds((a + b) * .5f, Vector3.Max(new Vector3(.75f, .75f, .1f), new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), .1f)));
        }

        public static bool SelectIssue(ShipRoomAuthoring room, string id, bool frame = true)
        {
            if (!CanShow || room == null || CurrentRoom != room) return false;
            var issue = RoomAuthoringValidator.Assess(room, out _).Issues.FirstOrDefault(i => i.Id == id);
            if (issue == null) return false;
            issueRoom = room; selectedIssueId = id;
            if (frame)
            {
                if (issue.HasRegion && SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(IssueBounds(room, issue), true);
                else if (!issue.HasRegion) EditorGUIUtility.PingObject(room.Style == null ? (UnityEngine.Object)room.gameObject : room.Style);
            }
            SceneView.RepaintAll();
            return true;
        }

        private static void Draw(SceneView view)
        {
            var room = CurrentRoom;
            if (room == null) return;
            PruneSelection();
            var feedback = GetFeedback(room);
            var result = feedback.Assessment;
            if (result == null || result.Status == RoomGenerationStatus.Refused) return;
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
                    var origin = room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
                    Handles.DrawSolidRectangleWithOutline(new[] { origin, origin + Vector3.right, origin + Vector3.right + Vector3.up, origin + Vector3.up },
                        new Color(1f, .3f, .15f, .12f), new Color(1f, .4f, .15f, .8f));
                }
                foreach (var issue in result.Issues.Where(i => i.HasRegion))
                {
                    var a = room.Occupancy.CellToWorld(new Vector3Int(issue.Start.x, issue.Start.y, 0));
                    var b = room.Occupancy.CellToWorld(new Vector3Int(issue.End.x, issue.End.y, 0));
                    var selected = issueRoom == room && selectedIssueId == issue.Id;
                    Handles.color = selected ? Color.yellow : Color.red;
                    if (a != b) Handles.DrawAAPolyLine(4, a, b);
                    var center = (a + b) * .5f;
                    Handles.DrawWireDisc(center, Vector3.forward, selected ? .22f : .14f);
                    Handles.Label(center + Vector3.up * .18f, "! " + issue.Field);
                    if (selected) Handles.DrawWireCube(IssueBounds(room, issue).center, IssueBounds(room, issue).size);
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
