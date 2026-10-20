using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomAuthoringValidator
    {
        public static string Validate(ShipRoomAuthoring room)
        {
            TryValidate(room, out _, out var issue);
            return issue;
        }

        public static bool TryValidate(ShipRoomAuthoring room, out RoomPlacement placement, out string issue)
        {
            placement = null;
            issue = RoomAuthoringOperations.GetSafetyIssue(room);
            if (issue == null)
            {
                var geometry = RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room));
                issue = GetOverlapIssue(room);
                if (issue == null && geometry.Cells.Length > 0) issue = GetConfigurationIssue(room.Style);
                if (issue == null && !RoomPlacementBuilder.TryBuild(geometry, room.Style, out placement, out issue)) { }
            }
            if (issue != null) issue = "Room " + (room == null ? "<missing>" : room.RoomId) + ": " + issue;
            return issue == null;
        }

        private static string GetOverlapIssue(ShipRoomAuthoring room)
        {
            var positions = new HashSet<Vector3>();
            foreach (var c in RoomAuthoringOperations.ReadCells(room)) positions.Add(room.Occupancy.CellToWorld(new Vector3Int(c.x, c.y, 0)));
            foreach (var other in room.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)))
            {
                if (other == room || other.Occupancy == null) continue;
                var issue = RoomAuthoringOperations.GetSafetyIssue(other);
                if (issue != null) return "Cannot qualify room overlap: " + other.RoomId + ": " + issue;
                foreach (var c in RoomAuthoringOperations.ReadCells(other))
                    if (positions.Contains(other.Occupancy.CellToWorld(new Vector3Int(c.x, c.y, 0)))) return "Authored region overlaps room " + other.RoomId + ".";
            }
            return null;
        }

        private static bool Finite(Vector2 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);

        private static string GetConfigurationIssue(RoomStyleDefinition style)
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
                if (!Finite(wall.AnchorPixels) || !Finite(wall.OffsetPixels)) return "North wall alignment must be finite.";
                if (wall.RepeatPixels != 32 && wall.RepeatPixels != 64) return "North structural repeat must be 32 or 64 pixels.";
                if (wall.AnchorPixels.y != wall.OffsetPixels.y) return "North wall bottom must align with its north boundary and extend northward.";
            }
            var caps = new[] { style.WestCap, style.EastCap, style.SouthCap, style.Northwest, style.Northeast, style.Southwest, style.Southeast };
            var roles = new[] { "West cap", "East cap", "South cap", "Northwest corner", "Northeast corner", "Southwest corner", "Southeast corner" };
            for (var i = 0; i < caps.Length; i++)
            {
                var cap = caps[i];
                if (cap == null || cap.CanvasPixels != new Vector2Int(32, 32)) return roles[i] + " requires a 32x32 canvas.";
                issue = CheckSprite(cap.Sprite, cap.CanvasPixels, roles[i]);
                if (issue != null) return issue;
                if (!Finite(cap.AnchorPixels) || !Finite(cap.OffsetPixels)) return roles[i] + " alignment must be finite.";
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

    }
}
