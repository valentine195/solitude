using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomAuthoringOperations
    {
        public static event System.Action<ShipRoomAuthoring> GenerationCompleted;

        public sealed class RoomEdit : System.IDisposable
        {
            private readonly int group;
            internal readonly ShipRoomAuthoring Room;
            internal bool IsDisposed { get; private set; }

            internal RoomEdit(ShipRoomAuthoring room, string name)
            {
                Room = room;
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(name);
            }

            public void Dispose()
            {
                if (IsDisposed) return;
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                IsDisposed = true;
            }
        }

        public static RoomEdit BeginEdit(ShipRoomAuthoring room, string name)
        {
            var issue = GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null) throw new System.InvalidOperationException(issue);
            return new RoomEdit(room, name);
        }

        private static void CheckEdit(ShipRoomAuthoring room, RoomEdit edit)
        {
            if (edit != null && (edit.IsDisposed || edit.Room != room))
                throw new System.InvalidOperationException("Room edit is closed or belongs to another room.");
        }

        public static Vector2Int[] ReadCells(ShipRoomAuthoring room)
        {
            var cells = new List<Vector2Int>();
            foreach (var position in room.Occupancy.cellBounds.allPositionsWithin)
                if (room.Occupancy.HasTile(position)) cells.Add(new Vector2Int(position.x, position.y));
            return cells.ToArray();
        }

        public static string GetSafetyIssue(ShipRoomAuthoring room, bool allowMissingOutput = false)
        {
            var issue = RoomAuthoringCreation.GetOwnershipIssue(room, allowMissingOutput);
            if (issue != null) return issue;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Rebuild is available in Edit Mode only.";
            if (room.transform.rotation != Quaternion.identity || room.transform.lossyScale != Vector3.one ||
                room.transform.position.z != 0 || room.transform.position.x != Mathf.Round(room.transform.position.x) || room.transform.position.y != Mathf.Round(room.transform.position.y))
                return "Room requires an unrotated, unit-scale, integer-aligned XY transform.";
            var grid = room.GetComponent<Grid>();
            if (grid == null || grid.cellSize != Vector3.one || grid.cellGap != Vector3.zero || grid.cellLayout != GridLayout.CellLayout.Rectangle || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ)
                return "Room requires a unit rectangular XY Grid.";
            foreach (var transform in room.GeneratedOutput == null ? new[] { room.Occupancy.transform } : new[] { room.Occupancy.transform, room.GeneratedOutput.transform })
                if (transform.parent != room.transform || transform.localPosition != Vector3.zero || transform.localRotation != Quaternion.identity || transform.localScale != Vector3.one)
                    return "Occupancy/output must be aligned direct children of the room.";
            if (room.GeneratedOutput != null && (room.GeneratedOutput.GetComponentsInChildren<ShipRoomAuthoring>(true).Length != 0 ||
                room.GeneratedOutput.GetComponentsInChildren<RoomGeneratedOutput>(true).Any(o => o != room.GeneratedOutput)))
                return "Generated hierarchy contains another ownership root; destructive writes refused.";
            if (room.GeneratedOutput != null)
                foreach (var other in Resources.FindObjectsOfTypeAll<ShipRoomAuthoring>())
                {
                    if (!other.gameObject.scene.IsValid() || !other.gameObject.scene.isLoaded || other.Occupancy == null) continue;
                    var authored = other.Occupancy.transform;
                    var output = room.GeneratedOutput.transform;
                    if (authored == output || authored.IsChildOf(output))
                        return "Generated hierarchy contains referenced authored occupancy from room " + other.RoomId + "; destructive writes refused.";
                }
            foreach (var position in room.Occupancy.cellBounds.allPositionsWithin)
                if (position.z != 0 && room.Occupancy.HasTile(position)) return "Occupancy must be on the XY plane at z=0.";
            return null;
        }

        public static bool PaintCell(ShipRoomAuthoring room, Vector3Int position, TileBase marker, RoomEdit edit = null)
        {
            var issue = GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null || position.z != 0) return false;
            CheckEdit(room, edit);
            using var ownedEdit = edit == null ? BeginEdit(room, marker == null ? "Erase Room Cell" : "Paint Room Cell") : null;
            // Generated-object Undo registrations end native recording between stroke samples.
            // Capture each authored mutation, then collapse the samples into the gesture group.
            Undo.RegisterCompleteObjectUndo(room.Occupancy, "Paint Room Cells");
            room.Occupancy.SetTile(position, marker);
            return RebuildCore(room);
        }

        public static bool PaintBox(ShipRoomAuthoring room, BoundsInt bounds, TileBase marker, RoomEdit edit = null)
        {
            if (GetSafetyIssue(room, allowMissingOutput: true) != null || bounds.z != 0 ||
                bounds.size.z != 1 || bounds.size.x <= 0 || bounds.size.y <= 0) return false;
            var tiles = new TileBase[checked(bounds.size.x * bounds.size.y)];
            if (marker != null)
                for (var i = 0; i < tiles.Length; i++) tiles[i] = marker;
            CheckEdit(room, edit);
            using var ownedEdit = edit == null ? BeginEdit(room, marker == null ? "Erase Room Box" : "Paint Room Box") : null;
            Undo.RegisterCompleteObjectUndo(room.Occupancy, "Paint Room Cells");
            room.Occupancy.SetTilesBlock(bounds, tiles);
            return RebuildCore(room);
        }

        public static bool Rebuild(ShipRoomAuthoring room)
        {
            var issue = GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null) { SetDiagnostic(room, issue); return false; }
            using var edit = new RoomEdit(room, "Rebuild Room");
            return RebuildCore(room);
        }

        private static bool RebuildCore(ShipRoomAuthoring room)
        {
            string issue;
            if (room.GeneratedOutput == null)
            {
                var generated = new GameObject("Generated Architecture", typeof(RoomGeneratedOutput));
                generated.transform.SetParent(room.transform, false);
                var output = generated.GetComponent<RoomGeneratedOutput>();
                output.Initialize(room);
                Undo.RegisterCreatedObjectUndo(generated, "Rebuild Room");
                var serialized = new SerializedObject(room);
                serialized.FindProperty("generatedOutput").objectReferenceValue = output;
                serialized.ApplyModifiedProperties();
            }
            var valid = RoomAuthoringValidator.TryValidate(room, out var placement, out issue);
            if (valid) RoomGeometryWriter.Apply(room, placement);
            else RoomGeometryWriter.Clear(room);
            SetDiagnostic(room, issue);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            GenerationCompleted?.Invoke(room);
            return valid;
        }

        private static void SetDiagnostic(ShipRoomAuthoring room, string issue)
        {
            if (room == null) return;
            Undo.RecordObject(room, "Room Diagnostic");
            room.SetGenerationDiagnostic(issue);
            EditorUtility.SetDirty(room);
        }
    }
}
