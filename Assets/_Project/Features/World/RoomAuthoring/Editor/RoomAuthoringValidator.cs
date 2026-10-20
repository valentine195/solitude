using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEditor;
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
            var result = Assess(room, out placement);
            issue = result.Issues.Length == 0 ? null : result.Summary;
            if (issue == null && room != null && room.GeneratedOutput == null)
                issue = "Room " + room.RoomId + ": Room output reference is missing. Rebuild Room to restore it.";
            return result.IsSuccessful && issue == null;
        }

        public static RoomGenerationResult Assess(ShipRoomAuthoring room, out RoomPlacement placement)
        {
            placement = null;
            var safety = RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true);
            if (safety == null) safety = GetOverlapIssue(room);
            if (safety != null)
            {
                var refusal = new RoomGenerationIssue(RoomIssueKind.Safety, "room", safety + " Retained output was not updated.");
                refusal.LocateInRoom(room == null ? "<missing>" : room.RoomId);
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = new[] { refusal } };
            }
            var geometry = RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room));
            RoomPlacementBuilder.TryBuild(geometry, room.Style, out placement, out _);
            foreach (var issue in placement.Issues) issue.LocateInRoom(room.RoomId);
            return new RoomGenerationResult
            {
                Status = geometry.Cells.Length == 0 ? RoomGenerationStatus.Empty : placement.Issues.Count == 0
                    ? RoomGenerationStatus.Complete : RoomGenerationStatus.IncompletePreview,
                Issues = placement.Issues.OrderBy(i => i.Id, System.StringComparer.Ordinal).ToArray(),
                InputFingerprint = InputFingerprint(room)
            };
        }

        public static bool IsCurrent(ShipRoomAuthoring room)
        {
            var saved = room == null ? null : room.GenerationResult;
            if (saved == null || saved.Status == RoomGenerationStatus.Refused || string.IsNullOrEmpty(saved.InputFingerprint) ||
                string.IsNullOrEmpty(saved.OutputFingerprint)) return false;
            var assessment = Assess(room, out _);
            return assessment.Status != RoomGenerationStatus.Refused && saved.InputFingerprint == assessment.InputFingerprint &&
                saved.OutputFingerprint == OutputFingerprint(room);
        }

        private static string Reference(UnityEngine.Object value)
        {
            if (value == null) return "null";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId) && !string.IsNullOrEmpty(guid)
                ? guid + ":" + localId : value.GetEntityId().ToString();
        }

        private static string Hash(StringBuilder value)
        {
            using var hash = SHA256.Create();
            return System.BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()))).Replace("-", "");
        }

        private static void SpriteFingerprint(StringBuilder text, Sprite sprite)
        {
            text.Append(Reference(sprite));
            if (sprite != null) text.Append(JsonUtility.ToJson(sprite.rect)).Append(JsonUtility.ToJson(sprite.pivot))
                .Append(sprite.pixelsPerUnit.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(sprite.texture.filterMode);
        }

        private static string InputFingerprint(ShipRoomAuthoring room)
        {
            var text = new StringBuilder(room.RoomId).Append(JsonUtility.ToJson(room.transform.position));
            foreach (var cell in RoomAuthoringOperations.ReadCells(room).OrderBy(c => c.y).ThenBy(c => c.x)) text.Append(JsonUtility.ToJson(cell));
            var style = room.Style;
            text.Append(Reference(style));
            if (style != null)
            {
                text.Append(EditorJsonUtility.ToJson(style));
                if (style.FloorTile is Tile tile) { text.Append(EditorJsonUtility.ToJson(tile)); SpriteFingerprint(text, tile.sprite); }
                var modules = (style.NorthWalls ?? System.Array.Empty<RoomStyleDefinition.Module>()).Concat(new[]
                    { style.WestCap, style.EastCap, style.SouthCap, style.Northwest, style.Northeast, style.Southwest, style.Southeast,
                        style.ConcaveNorthwest, style.ConcaveNortheast, style.ConcaveSouthwest, style.ConcaveSoutheast });
                foreach (var module in modules) if (module != null) SpriteFingerprint(text, module.Sprite);
            }
            return Hash(text);
        }

        public static string OutputFingerprint(ShipRoomAuthoring room)
        {
            if (room.GeneratedOutput == null) return "missing";
            var text = new StringBuilder();
            foreach (var child in room.GeneratedOutput.GetComponentsInChildren<Transform>(true))
            {
                text.Append(child.name).Append(child.gameObject.activeSelf).Append(JsonUtility.ToJson(child.localPosition))
                    .Append(JsonUtility.ToJson(child.localRotation)).Append(JsonUtility.ToJson(child.localScale));
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null) text.Append(Reference(renderer.sprite)).Append(renderer.sortingOrder).Append(renderer.sortingLayerID).Append(renderer.enabled);
                var map = child.GetComponent<Tilemap>();
                if (map != null)
                {
                    text.Append(JsonUtility.ToJson(map.tileAnchor));
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                        if (map.HasTile(cell)) text.Append(JsonUtility.ToJson(cell)).Append(Reference(map.GetTile(cell)));
                }
            }
            return Hash(text);
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

    }
}
