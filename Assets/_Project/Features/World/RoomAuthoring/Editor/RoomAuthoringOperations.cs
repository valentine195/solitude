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
        private static RoomEdit activeEdit;

        static RoomAuthoringOperations()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CancelActiveEdit;
            Undo.undoRedoPerformed += () => activeEdit?.CloseAfterUndo();
        }

        public static bool HasActiveEdit => activeEdit != null;
        public static bool IsEditing(ShipRoomAuthoring room) => activeEdit != null && activeEdit.Room == room;
        public static void CancelActiveEdit() => activeEdit?.Cancel();
        public static void CancelActiveEdit(UnityEngine.SceneManagement.Scene scene)
        {
            if (activeEdit?.Room != null && activeEdit.Room.gameObject.scene == scene) activeEdit.Cancel();
        }

        public static Vector2Int[] PendingCells(ShipRoomAuthoring room, bool removed)
        {
            if (!IsEditing(room) || activeEdit.originalTiles == null) return System.Array.Empty<Vector2Int>();
            var before = activeEdit.originalTiles.Keys.Select(p => new Vector2Int(p.x, p.y));
            var after = ReadCells(room);
            return (removed ? before.Except(after) : after.Except(before)).OrderBy(p => p.y).ThenBy(p => p.x).ToArray();
        }

        public sealed class RoomEdit : System.IDisposable
        {
            private readonly int group;
            internal readonly ShipRoomAuthoring Room;
            internal bool IsDisposed { get; private set; }
            internal Dictionary<Vector3Int, TileBase> originalTiles;

            internal RoomEdit(ShipRoomAuthoring room, string name)
            {
                Room = room;
                Undo.IncrementCurrentGroup();
                group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(name);
            }

            internal void PrepareMutation()
            {
                if (originalTiles != null) return;
                originalTiles = Tiles(Room);
                Undo.RegisterCompleteObjectUndo(Room.Occupancy, "Paint Room Cells");
            }

            private static Dictionary<Vector3Int, TileBase> Tiles(ShipRoomAuthoring room)
            {
                var tiles = new Dictionary<Vector3Int, TileBase>();
                foreach (var position in room.Occupancy.cellBounds.allPositionsWithin)
                {
                    var tile = room.Occupancy.GetTile(position);
                    if (tile != null) tiles.Add(position, tile);
                }
                return tiles;
            }

            public bool Commit()
            {
                if (IsDisposed) return false;
                if (originalTiles != null)
                {
                    var current = Tiles(Room);
                    if (current.Count == originalTiles.Count && originalTiles.All(pair =>
                        current.TryGetValue(pair.Key, out var tile) && tile == pair.Value))
                    { Cancel(); return true; }
                }
                CloseAfterUndo();
                try
                {
                    var result = originalTiles == null || RebuildCore(Room);
                    FinishGroup();
                    return result;
                }
                catch
                {
                    Undo.RevertAllDownToGroup(group);
                    throw;
                }
            }

            public void Cancel()
            {
                if (IsDisposed) return;
                CloseAfterUndo();
                if (originalTiles != null) Undo.RevertAllDownToGroup(group);
                else FinishGroup();
            }

            internal void CloseAfterUndo()
            {
                IsDisposed = true;
                if (activeEdit == this) activeEdit = null;
            }

            private void FinishGroup()
            {
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
            }

            public void Dispose() { if (!IsDisposed) Commit(); }
        }

        public static RoomEdit BeginEdit(ShipRoomAuthoring room, string name)
        {
            var issue = GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null) throw new System.InvalidOperationException(issue);
            activeEdit?.Cancel();
            return activeEdit = new RoomEdit(room, name);
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
            if (room.Layout != null)
            {
                var frame = room.Layout.transform;
                if (frame.rotation != Quaternion.identity || frame.lossyScale != Vector3.one || frame.position.z != 0 ||
                    frame.position.x != Mathf.Round(frame.position.x) || frame.position.y != Mathf.Round(frame.position.y))
                    return "Layout requires an unrotated, unit-scale, integer-aligned XY transform.";
            }
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
            => PaintCell(room, position, marker, out _, edit);

        public static bool PaintCell(ShipRoomAuthoring room, Vector3Int position, TileBase marker,
            out string issue, RoomEdit edit = null)
        {
            CheckEdit(room, edit);
            issue = position.z != 0 ? "Occupancy must be on the XY plane at z=0." :
                GetCellPaintIssue(room, new[] { new Vector2Int(position.x, position.y) }, marker);
            if (issue != null) { edit?.Cancel(); return false; }
            using var ownedEdit = edit == null ? BeginEdit(room, marker == null ? "Erase Room Cell" : "Paint Room Cell") : null;
            if (room.Occupancy.GetTile(position) == marker) return true;
            (edit ?? ownedEdit).PrepareMutation();
            room.Occupancy.SetTile(position, marker);
            return edit != null || ownedEdit.Commit();
        }

        public static bool PaintBox(ShipRoomAuthoring room, BoundsInt bounds, TileBase marker, RoomEdit edit = null)
            => PaintBox(room, bounds, marker, out _, edit);

        public static bool PaintBox(ShipRoomAuthoring room, BoundsInt bounds, TileBase marker,
            out string issue, RoomEdit edit = null)
        {
            CheckEdit(room, edit);
            issue = bounds.z != 0 || bounds.size.z != 1 || bounds.size.x <= 0 || bounds.size.y <= 0
                ? "Paint a nonempty box on the XY plane at z=0."
                : GetCellPaintIssue(room, BoxCells(bounds), marker);
            if (issue != null) { edit?.Cancel(); return false; }
            var tiles = new TileBase[checked(bounds.size.x * bounds.size.y)];
            if (marker != null)
                for (var i = 0; i < tiles.Length; i++) tiles[i] = marker;
            using var ownedEdit = edit == null ? BeginEdit(room, marker == null ? "Erase Room Box" : "Paint Room Box") : null;
            var changed = false;
            foreach (var position in bounds.allPositionsWithin)
                if (room.Occupancy.GetTile(position) != marker) { changed = true; break; }
            if (!changed) return true;
            (edit ?? ownedEdit).PrepareMutation();
            room.Occupancy.SetTilesBlock(bounds, tiles);
            return edit != null || ownedEdit.Commit();
        }

        private static IEnumerable<Vector2Int> BoxCells(BoundsInt bounds)
        {
            foreach (var position in bounds.allPositionsWithin)
                yield return new Vector2Int(position.x, position.y);
        }

        public static string GetCellPaintIssue(ShipRoomAuthoring room, IEnumerable<Vector2Int> cells, TileBase marker)
        {
            var safety = GetSafetyIssue(room, allowMissingOutput: true);
            if (safety != null || marker == null) return safety;
            var candidates = cells.Distinct().OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
            foreach (var other in Resources.FindObjectsOfTypeAll<ShipRoomAuthoring>()
                .Where(r => r != room && r.gameObject.scene == room.gameObject.scene && r.Occupancy != null)
                .OrderBy(r => r.RoomId, System.StringComparer.Ordinal))
            {
                safety = GetSafetyIssue(other, allowMissingOutput: true);
                if (safety != null) return "Cannot check space " + other.RoomId + ": " + safety;
                var occupied = new HashSet<Vector3>(ReadCells(other).Select(c => other.Occupancy.CellToWorld(new Vector3Int(c.x, c.y, 0))));
                foreach (var cell in candidates)
                    if (occupied.Contains(room.Occupancy.CellToWorld(new Vector3Int(cell.x, cell.y, 0))))
                        return "Cell " + cell + " in space " + room.RoomId + " overlaps space " + other.RoomId + ". Stroke rejected; neither space was changed.";
            }
            return null;
        }

        public static bool PaintCells(ShipRoomAuthoring room, IEnumerable<Vector2Int> cells, TileBase marker,
            out string issue, RoomEdit edit = null)
        {
            var positions = cells.Distinct().OrderBy(c => c.y).ThenBy(c => c.x).ToArray();
            issue = GetCellPaintIssue(room, positions, marker);
            if (issue != null) return false;
            CheckEdit(room, edit);
            using var ownedEdit = edit == null ? BeginEdit(room, "Paint Corridor Cells") : null;
            var changed = positions.Where(c => room.Occupancy.GetTile(new Vector3Int(c.x, c.y, 0)) != marker).ToArray();
            if (changed.Length == 0) return true;
            (edit ?? ownedEdit).PrepareMutation();
            room.Occupancy.SetTiles(changed.Select(c => new Vector3Int(c.x, c.y, 0)).ToArray(),
                Enumerable.Repeat(marker, changed.Length).ToArray());
            return edit != null || ownedEdit.Commit();
        }

        public static string GetSafetyIssue(ShipLayoutAuthoring layout, bool allowMissingOutput = false)
        {
            var issue = RoomAuthoringCreation.GetLayoutOwnershipIssue(layout, allowMissingOutput);
            if (issue != null) return issue;
            foreach (var room in layout.Spaces)
            {
                issue = GetSafetyIssue(room, allowMissingOutput);
                if (issue != null) return issue;
            }
            return null;
        }

        public static string GetConnectionIssue(ShipLayoutAuthoring layout, LayoutConnectionDefinition connection, string replacingIdentity = null)
        {
            var safety = GetSafetyIssue(layout, allowMissingOutput: true);
            if (safety != null) return safety;
            if (layout.Spaces.Any(IsEditing)) return "Finish or cancel the active paint stroke before placing a connection.";
            try
            {
                if (replacingIdentity != null && (connection == null || connection.Id != replacingIdentity ||
                    layout.Connections.Count(c => c != null && c.Id == replacingIdentity) != 1))
                    return "Correction must preserve one existing connection identity.";
                var proposed = layout.Connections.Where(c => replacingIdentity == null || c == null || c.Id != replacingIdentity).Concat(new[] { connection });
                var issues = LayoutBoundaryResolver.ConnectionIssues(RoomAuthoringValidator.TopologyOf(layout), proposed);
                // A correction can improve one external invalid record while unrelated records remain stale.
                // Any conflict involving the corrected span still rejects this operation atomically.
                if (replacingIdentity != null) issues = issues.Where(i => i.Field == "Connection/" + replacingIdentity).ToArray();
                return issues.Length == 0 ? null : string.Join("\n", issues.Select(i => i.Reason));
            }
            catch (System.Exception exception) when (exception is System.OverflowException || exception is System.InvalidOperationException)
            { return exception.Message; }
        }

        public static bool AddConnection(ShipLayoutAuthoring layout, LayoutConnectionDefinition connection, out string issue)
        {
            issue = GetConnectionIssue(layout, connection);
            if (issue != null) return false;
            return ChangeConnections(layout, layout.Connections.Concat(new[] { connection }).ToArray(), "Add Layout Connection");
        }

        public static bool UpdateConnection(ShipLayoutAuthoring layout, string identity, LayoutConnectionDefinition replacement, out string issue)
        {
            if (string.IsNullOrWhiteSpace(identity)) { issue = "Select an existing connection to correct."; return false; }
            issue = GetConnectionIssue(layout, replacement, identity);
            if (issue != null) return false;
            return ChangeConnections(layout, layout.Connections.Select(c => c != null && c.Id == identity ? replacement : c).ToArray(), "Correct Layout Connection");
        }

        public static bool RemoveConnection(ShipLayoutAuthoring layout, string identity, out string issue)
        {
            issue = GetSafetyIssue(layout, allowMissingOutput: true);
            if (issue != null) return false;
            if (layout.Spaces.Any(IsEditing)) { issue = "Finish or cancel the active paint stroke first."; return false; }
            if (string.IsNullOrWhiteSpace(identity) || layout.Connections.Count(c => c != null && c.Id == identity) != 1)
            { issue = "Select one uniquely identified authored connection to remove."; return false; }
            return ChangeConnections(layout, layout.Connections.Where(c => c == null || c.Id != identity).ToArray(), "Remove Layout Connection");
        }

        private static bool ChangeConnections(ShipLayoutAuthoring layout, LayoutConnectionDefinition[] definitions, string label)
        {
            // One authored action includes its derived walls and result state, even if artwork is incomplete.
            using var edit = new RoomEdit(null, label);
            var group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(layout, label);
                layout.SetConnections(definitions); EditorUtility.SetDirty(layout);
                RebuildLayoutCore(layout);
                return true;
            }
            catch
            {
                edit.CloseAfterUndo(); Undo.RevertAllDownToGroup(group); throw;
            }
        }

        public static bool RegenerateFloorPanels(ShipRoomAuthoring room, out string issue)
        {
            issue = room == null ? "Choose a space." : room.Layout == null ? GetStyleReplacementIssue(room) : GetStyleReplacementIssue(room.Layout);
            if (issue != null) return false;
            if (HasActiveEdit) { issue = "Finish or cancel the active paint stroke before regenerating panels."; return false; }
            var success = Rebuild(room);
            if (!success) issue = room.GenerationDiagnostic;
            return success;
        }

        public static bool NewFloorPanelSeed(ShipRoomAuthoring room, out string issue)
        {
            if (room == null) { issue = "Choose a space."; return false; }
            var settings = room.FloorPanels.Copy(); settings.Seed = unchecked(settings.Seed + 1);
            return SetFloorPanelSettings(room, settings, out issue);
        }

        public static bool DisableFloorPanels(ShipRoomAuthoring room, out string issue)
        {
            if (room == null) { issue = "Choose a space."; return false; }
            var settings = room.FloorPanels.Copy(); settings.Enabled = false;
            return SetFloorPanelSettings(room, settings, out issue);
        }

        public static bool SetFloorPanelSettings(ShipRoomAuthoring room, RoomFloorPanelSettings settings, out string issue) =>
            EditFloorPanelAuthoring(room, settings, room == null ? null : room.FloorReservations, out issue);

        public static bool UpsertFloorReservation(ShipRoomAuthoring room, RoomFloorReservation reservation, out string issue)
        {
            if (room == null || reservation == null) { issue = "Choose a space and reservation."; return false; }
            var records = room.FloorReservations.ToList();
            var index = records.FindIndex(r => r != null && r.Id == reservation.Id);
            if (index < 0) records.Add(reservation); else records[index] = reservation;
            return EditFloorPanelAuthoring(room, room.FloorPanels, records, out issue);
        }

        public static bool RemoveFloorReservation(ShipRoomAuthoring room, string identity, out string issue)
        {
            if (room == null || string.IsNullOrWhiteSpace(identity)) { issue = "Choose a space and reservation identity."; return false; }
            return EditFloorPanelAuthoring(room, room.FloorPanels, room.FloorReservations.Where(r => r == null || r.Id != identity), out issue);
        }

        public static bool EditFloorPanelAuthoring(ShipRoomAuthoring room, RoomFloorPanelSettings settings,
            IEnumerable<RoomFloorReservation> reservations, out string issue)
        {
            issue = room == null || settings == null || reservations == null ? "Choose a space, panel settings and reservation records." :
                room.Layout == null ? GetStyleReplacementIssue(room) : GetStyleReplacementIssue(room.Layout);
            if (issue != null) return false;
            if (HasActiveEdit) { issue = "Finish or cancel the active paint stroke before editing panel inputs."; return false; }
            var proposed = reservations.Select(r => r?.Copy()).ToArray();
            var currentKey = RoomFloorPanelDomain.InputKey(room.FloorPanels, room.FloorReservations);
            if (currentKey == RoomFloorPanelDomain.InputKey(settings, proposed)) return true;
            var restoredEnabled = settings.Copy(); restoredEnabled.Enabled = room.FloorPanels.Enabled;
            var disableOnly = room.FloorPanels.Enabled && !settings.Enabled &&
                currentKey == RoomFloorPanelDomain.InputKey(restoredEnabled, proposed);
            // Disable can retain externally malformed inputs verbatim; it never repairs or prunes authored records.
            if (!disableOnly)
            {
                var settingsIssue = settings.ValidationIssue();
                if (settingsIssue != null) { issue = "Space " + room.RoomId + ": FloorPanels/Settings: " + settingsIssue; return false; }
                var candidate = RoomFloorPanelDomain.Build(room, settings, proposed, retainInactiveRecords: false);
                var invalid = candidate.Issues.FirstOrDefault(i => !i.Inactive);
                if (invalid != null) { issue = "Space " + invalid.RoomId + ": " + invalid.Field + ": " + invalid.Reason; return false; }
            }
            using var edit = BeginEdit(room, "Edit Floor Panel Inputs");
            var group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(room, "Edit Floor Panel Inputs");
                room.SetFloorPanelAuthoring(settings, proposed); EditorUtility.SetDirty(room);
                RebuildCore(room);
                return true;
            }
            catch { edit.CloseAfterUndo(); Undo.RevertAllDownToGroup(group); throw; }
        }

        // Association establishes which root we own; the recorded build establishes which contents may be replaced.
        public static string GetStyleReplacementIssue(ShipRoomAuthoring room) => GetReplacementIssue(room, false);

        internal static string GetOutputReplacementIssue(ShipRoomAuthoring room) => GetReplacementIssue(room,
            room != null && !room.FloorPanels.Enabled && room.GeneratedOutput?.PreparedPanels == null &&
            !RoomAuthoringValidator.HasPreparedPanels(room));

        // Disabled legacy rendering supports resetting visual edits, but never adopts added objects
        // or components. Panel/style replacement retains the stricter recorded structural check.
        private static bool IsRecordedVisual(RoomGeneratedOutput.GeneratedChild child)
        {
            if (child.Components == null || child.Transform.childCount != 0) return false;
            return child.Transform.GetComponents<Component>().All(component => component != null &&
                child.Components.Contains(component) && (component.GetType() == typeof(Transform) ||
                component.GetType() == typeof(SpriteRenderer) || component.GetType() == typeof(Tilemap) ||
                component.GetType() == typeof(TilemapRenderer)));
        }

        private static string GetReplacementIssue(ShipRoomAuthoring room, bool allowGeneratedVisualChanges)
        {
            var issue = GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null) return issue;
            var output = room.GeneratedOutput;
            if (output == null) return null;
            // A surviving generated object must match its recorded scene structure. Imported Sprite
            // bounds may change independently; missing recorded children may be reconstructed.
            if (!string.IsNullOrEmpty(output.RootFingerprint))
            {
                var children = output.GeneratedChildren;
                var unchanged = output.RootFingerprint == RoomAuthoringValidator.GeneratedObjectFingerprint(output.transform, false) &&
                    children != null && children.All(child => child != null &&
                        (child.Transform == null || (child.Transform.parent == output.transform &&
                         (child.Fingerprint == RoomAuthoringValidator.GeneratedObjectFingerprint(child.Transform, true) ||
                         (allowGeneratedVisualChanges && IsRecordedVisual(child)))))) &&
                    output.transform.Cast<Transform>().All(child => children.Any(record => record.Transform == child));
                // Check the recorded container even when all its renderers were deleted. Never adopt
                // altered historical pixels merely because the visible scene no longer references them.
                if (unchanged && (output.PreparedPanels == null || RoomFloorPanelAssetStore.IsVerified(output.PreparedPanels))) return null;
                return "Space " + room.RoomId + ": generated output contains modified or unrecognized content, or modified prepared panel resources. Preserve that content before rebuilding; style change refused.";
            }
            var recorded = room.GenerationResult?.OutputFingerprint;
            if (!string.IsNullOrEmpty(recorded) && recorded == RoomAuthoringValidator.OutputFingerprint(room)) return null;
            if (string.IsNullOrEmpty(recorded) && output.transform.childCount == 0 &&
                output.GetComponents<Component>().All(component => component != null &&
                    (component.GetType() == typeof(Transform) || component.GetType() == typeof(RoomGeneratedOutput)))) return null;
            return "Space " + room.RoomId + " at " + room.name + "/" + output.name +
                ": generated output is modified or unverified. Move authored content outside this root and review the output before an explicit rebuild; style change refused.";
        }

        public static string GetStyleReplacementIssue(ShipLayoutAuthoring layout)
        {
            var issue = GetSafetyIssue(layout, allowMissingOutput: true);
            if (issue != null) return issue;
            foreach (var room in layout.Spaces)
            {
                issue = GetStyleReplacementIssue(room);
                if (issue != null) return "Layout " + layout.LayoutId + ": " + issue;
            }
            return null;
        }

        // Supported role/renderer binding edits include every loaded consumer's derived output in one Undo action.
        // External source-file imports use the asynchronous refresh path and do not promise historical file bytes.
        public static bool EditStyleBindings(RoomStyleDefinition style, System.Action<RoomStyleDefinition> change, out string issue)
        {
            issue = null;
            if (style == null || change == null) { issue = "Choose a style and binding edit."; return false; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || HasActiveEdit)
            { issue = "Finish or cancel painting and return to Edit Mode before changing style bindings."; return false; }
            var consumers = RoomAuthoringAssetRefresh.LoadedLayouts().Where(layout => RoomAuthoringValidator.StyleDependencies(layout).Contains(style)).ToArray();
            foreach (var layout in consumers)
            {
                if (RoomAuthoringAssetRefresh.HasPendingPanelDraft(layout))
                {
                    issue = "Layout " + layout.LayoutId + ": Apply or cancel its floor panel draft before changing shared artwork bindings.";
                    return false;
                }
                issue = GetStyleReplacementIssue(layout);
                if (issue != null) return false;
            }
            using var edit = new RoomEdit(null, "Edit Architectural Style Bindings");
            var group = Undo.GetCurrentGroup();
            try
            {
                var before = EditorJsonUtility.ToJson(style);
                Undo.RegisterCompleteObjectUndo(style, "Edit Architectural Style Bindings");
                change(style);
                if (before == EditorJsonUtility.ToJson(style)) return true;
                EditorUtility.SetDirty(style);
                foreach (var layout in consumers) RebuildLayoutCore(layout);
                return true;
            }
            catch { edit.CloseAfterUndo(); Undo.RevertAllDownToGroup(group); throw; }
        }

        public static bool Rebuild(ShipLayoutAuthoring layout)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (layout == null) return false;
            var room = layout.Spaces.Count == 1 ? layout.Spaces[0] : null;
            if (activeEdit != null && layout.Spaces.Contains(activeEdit.Room))
                return activeEdit.originalTiles != null ? activeEdit.Commit() : RebuildLayoutCore(layout);
            activeEdit?.Commit();
            using var edit = new RoomEdit(room, "Rebuild Layout");
            return RebuildLayoutCore(layout);
        }

        public static bool Rebuild(ShipRoomAuthoring room)
        {
            if (room == null || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (activeEdit != null && activeEdit.Room == room)
                return activeEdit.originalTiles != null ? activeEdit.Commit() : RebuildCore(room);
            activeEdit?.Commit();
            using var edit = new RoomEdit(room, "Rebuild Room");
            return RebuildCore(room);
        }

        private static bool RebuildCore(ShipRoomAuthoring room)
        {
            if (room.Layout != null && GetSafetyIssue(room, allowMissingOutput: true) == null)
                return RebuildLayoutCore(room.Layout);
            var result = RoomAuthoringValidator.Assess(room, out var placement);
            return ApplyRebuild(room, null, result, placement);
        }

        private static bool RebuildLayoutCore(ShipLayoutAuthoring layout)
        {
            var result = RoomAuthoringValidator.AssessSpaces(layout, out var placements);
            Undo.RegisterCompleteObjectUndo(layout, "Layout Generation Result");
            if (result.Status == RoomGenerationStatus.Refused)
            {
                layout.SetGenerationResult(result);
                EditorUtility.SetDirty(layout);
                return false;
            }
            if (layout.Spaces.Count == 1) return ApplyRebuild(layout.Spaces[0], layout, result, placements[layout.Spaces[0]]);
            // Prepare all persistent resources before publishing any scene references.
            var panels = layout.Spaces.ToDictionary(member => member, member => PreparePanels(member, placements[member], result));
            foreach (var member in layout.Spaces) EnsureOutput(member, layout);
            foreach (var member in layout.Spaces.OrderBy(r => r.RoomId, System.StringComparer.Ordinal))
                ApplyRebuild(member, layout, JsonUtility.FromJson<RoomGenerationResult>(JsonUtility.ToJson(result)),
                    placements[member], false, panels[member]);
            result.OutputFingerprint = RoomAuthoringValidator.OutputFingerprint(layout);
            Undo.RegisterCompleteObjectUndo(layout, "Layout Generation Result");
            layout.SetGenerationResult(result);
            EditorUtility.SetDirty(layout);
            GenerationCompleted?.Invoke(layout.Spaces[0]);
            return result.IsSuccessful;
        }

        private static bool ApplyRebuild(ShipRoomAuthoring room, ShipLayoutAuthoring layout,
            RoomGenerationResult result, RoomPlacement placement, bool recordLayout = true, RoomFloorPanelOutput preparedPanels = null)
        {
            Undo.RegisterCompleteObjectUndo(room, "Room Generation Result");
            if (result.Status == RoomGenerationStatus.Refused)
            {
                room.SetGenerationResult(result);
                EditorUtility.SetDirty(room);
                return false;
            }
            var panels = recordLayout ? PreparePanels(room, placement, result) : preparedPanels;
            EnsureOutput(room, layout);
            RoomGeometryWriter.Apply(room, placement, panels);
            result.OutputFingerprint = RoomAuthoringValidator.OutputFingerprint(room);
            Undo.RegisterCompleteObjectUndo(room, "Room Generation Result");
            room.SetGenerationResult(result);
            if (layout != null && recordLayout)
            {
                Undo.RegisterCompleteObjectUndo(layout, "Layout Generation Result");
                layout.SetGenerationResult(JsonUtility.FromJson<RoomGenerationResult>(JsonUtility.ToJson(result)));
                EditorUtility.SetDirty(layout);
            }
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            if (recordLayout) GenerationCompleted?.Invoke(room);
            return result.IsSuccessful;
        }

        private static RoomFloorPanelOutput PreparePanels(ShipRoomAuthoring room, RoomPlacement placement, RoomGenerationResult result)
        {
            var output = RoomFloorPanelAssetStore.Prepare(room, placement, out var issues, out var advisories);
            result.Advisories = result.Advisories.Concat(advisories).OrderBy(i => i.Id, System.StringComparer.Ordinal).ToArray();
            if (issues.Length != 0)
            {
                result.Issues = result.Issues.Concat(issues).OrderBy(i => i.Id, System.StringComparer.Ordinal).ToArray();
                result.Status = RoomGenerationStatus.IncompletePreview;
            }
            return output;
        }

        private static void EnsureOutput(ShipRoomAuthoring room, ShipLayoutAuthoring layout)
        {
            if (room.GeneratedOutput == null)
            {
                var generated = new GameObject("Generated Architecture", typeof(RoomGeneratedOutput));
                generated.transform.SetParent(room.transform, false);
                var output = generated.GetComponent<RoomGeneratedOutput>();
                output.Initialize(room, layout);
                // This root was created here and is empty; the prior result describes the deleted
                // root, so establish its new ownership proof before the writer checks disposal.
                output.RecordGeneratedChildren(RoomAuthoringValidator.GeneratedObjectFingerprint(output.transform, false),
                    System.Array.Empty<RoomGeneratedOutput.GeneratedChild>(), null);
                Undo.RegisterCreatedObjectUndo(generated, "Rebuild Room");
                var serialized = new SerializedObject(room);
                serialized.FindProperty("generatedOutput").objectReferenceValue = output;
                serialized.ApplyModifiedProperties();
                if (layout != null && layout.GeneratedOutput == null && layout.Spaces[0] == room)
                {
                    var layoutData = new SerializedObject(layout);
                    layoutData.FindProperty("generatedOutput").objectReferenceValue = output;
                    layoutData.ApplyModifiedProperties();
                }
            }
        }

    }
}
