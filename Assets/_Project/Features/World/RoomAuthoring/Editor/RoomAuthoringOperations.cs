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
        public static Vector2Int[] ReadCells(ShipRoomAuthoring room)
        {
            var cells = new List<Vector2Int>();
            foreach (var position in room.Occupancy.cellBounds.allPositionsWithin)
                if (room.Occupancy.HasTile(position)) cells.Add(new Vector2Int(position.x, position.y));
            return cells.ToArray();
        }

        public static string GetSafetyIssue(ShipRoomAuthoring room)
        {
            var issue = RoomAuthoringCreation.GetOwnershipIssue(room);
            if (issue != null) return issue;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "Rebuild is available in Edit Mode only.";
            if (room.transform.rotation != Quaternion.identity || room.transform.lossyScale != Vector3.one ||
                room.transform.position.z != 0 || room.transform.position.x != Mathf.Round(room.transform.position.x) || room.transform.position.y != Mathf.Round(room.transform.position.y))
                return "Room requires an unrotated, unit-scale, integer-aligned XY transform.";
            var grid = room.GetComponent<Grid>();
            if (grid == null || grid.cellSize != Vector3.one || grid.cellGap != Vector3.zero || grid.cellLayout != GridLayout.CellLayout.Rectangle || grid.cellSwizzle != GridLayout.CellSwizzle.XYZ)
                return "Room requires a unit rectangular XY Grid.";
            foreach (var transform in new[] { room.Occupancy.transform, room.GeneratedOutput.transform })
                if (transform.parent != room.transform || transform.localPosition != Vector3.zero || transform.localRotation != Quaternion.identity || transform.localScale != Vector3.one)
                    return "Occupancy/output must be aligned direct children of the room.";
            if (room.GeneratedOutput.GetComponentsInChildren<ShipRoomAuthoring>(true).Length != 0 ||
                room.GeneratedOutput.GetComponentsInChildren<RoomGeneratedOutput>(true).Any(o => o != room.GeneratedOutput))
                return "Generated hierarchy contains another ownership root; destructive writes refused.";
            foreach (var position in room.Occupancy.cellBounds.allPositionsWithin)
                if (position.z != 0 && room.Occupancy.HasTile(position)) return "Occupancy must be on the XY plane at z=0.";
            return null;
        }

        public static bool Rebuild(ShipRoomAuthoring room)
        {
            var issue = GetSafetyIssue(room);
            if (issue != null) { SetDiagnostic(room, issue); return false; }
            var geometry = RoomBoundaryExtractor.Extract(ReadCells(room));
            RoomPlacement placement = null;
            issue = geometry.Cells.Length == 0 ? null : GetConfigurationIssue(room.Style);
            var valid = issue == null && RoomPlacementBuilder.TryBuild(geometry, room.Style, out placement, out issue);
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rebuild Room");
            if (valid) RoomGeometryWriter.Apply(room, placement);
            else RoomGeometryWriter.Clear(room);
            SetDiagnostic(room, issue);
            EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            return valid;
        }

        public static string GetConfigurationIssue(RoomStyleDefinition style)
        {
            if (style == null) return "Assign a room style.";
            if (!(style.FloorTile is Tile tile) || tile.sprite == null) return "Floor requires a native sprite Tile.";
            var issue = CheckSprite(tile.sprite, new Vector2Int(32, 32), "Floor");
            if (issue != null) return issue;
            if (tile.transform != Matrix4x4.identity) return "Floor tile transform must be identity.";
            if (style.NorthWalls == null || style.NorthWalls.Length == 0) return "North wall modules are missing.";
            foreach (var wall in style.NorthWalls)
            {
                if (wall == null || (wall.CanvasPixels != new Vector2Int(32, 64) && wall.CanvasPixels != new Vector2Int(64, 64))) return "North walls require 32x64 or 64x64 canvases.";
                issue = CheckSprite(wall.Sprite, wall.CanvasPixels, "North wall");
                if (issue != null) return issue;
                if (wall.RepeatPixels != 32 && wall.RepeatPixels != 64) return "North structural repeat must be 32 or 64 pixels.";
                if (wall.AnchorPixels.y != wall.OffsetPixels.y) return "North wall bottom must align with its north boundary and extend northward.";
            }
            var caps = new[] { style.WestCap, style.EastCap, style.SouthCap, style.Northwest, style.Northeast, style.Southwest, style.Southeast };
            foreach (var cap in caps)
            {
                if (cap == null || cap.CanvasPixels != new Vector2Int(32, 32)) return "Caps and corners require 32x32 canvases.";
                issue = CheckSprite(cap.Sprite, cap.CanvasPixels, "Cap/corner");
                if (issue != null) return issue;
            }
            return null;
        }

        private static string CheckSprite(Sprite sprite, Vector2Int dimensions, string role)
        {
            if (sprite == null) return role + " sprite is missing.";
            if (sprite.rect.size != (Vector2)dimensions) return role + " sprite dimensions do not match its canvas.";
            if (!Mathf.Approximately(sprite.pixelsPerUnit, 32)) return role + " requires 32 PPU.";
            if (sprite.texture.filterMode != FilterMode.Point) return role + " requires Point filtering.";
            return null;
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
