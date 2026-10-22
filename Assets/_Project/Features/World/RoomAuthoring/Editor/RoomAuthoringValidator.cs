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
            if (room != null && room.Layout != null && room.Layout.Spaces.Count > 1 &&
                RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true) == null)
                return Assess(room.Layout, out placement);
            return AssessSpace(room, out placement);
        }

        private static RoomGenerationResult AssessSpace(ShipRoomAuthoring room, out RoomPlacement placement,
            bool includeLayoutMembers = true, bool buildBoundaries = true)
        {
            placement = null;
            var safety = RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true);
            if (safety == null) safety = GetOverlapIssue(room, includeLayoutMembers);
            if (safety != null)
            {
                var refusal = new RoomGenerationIssue(RoomIssueKind.Safety, "room", safety + " Retained output was not updated.");
                refusal.LocateInRoom(room == null ? "<missing>" : room.RoomId);
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = new[] { refusal } };
            }
            var geometry = RoomBoundaryExtractor.Extract(RoomAuthoringOperations.ReadCells(room));
            if (buildBoundaries) RoomPlacementBuilder.TryBuild(geometry, room.Style, out placement, out _);
            else placement = RoomPlacementBuilder.BuildFloor(geometry, room.Style);
            var topology = ShipLayoutTopology.Build(new[] { new ShipSpaceCells(room.RoomId, geometry.Cells, Vector2Int.zero) });
            AddDisconnectedIssues(topology, room.RoomId, placement.Issues);
            foreach (var issue in placement.Issues) issue.LocateInRoom(room.RoomId);
            return new RoomGenerationResult
            {
                Status = geometry.Cells.Length == 0 ? RoomGenerationStatus.Empty : placement.Issues.Count == 0
                    ? RoomGenerationStatus.Complete : RoomGenerationStatus.IncompletePreview,
                Issues = placement.Issues.OrderBy(i => i.Id, System.StringComparer.Ordinal).ToArray(),
                InputFingerprint = InputFingerprint(room)
            };
        }

        public static RoomGenerationResult Assess(ShipLayoutAuthoring layout, out RoomPlacement placement)
        {
            placement = null;
            var result = AssessSpaces(layout, out var placements);
            if (layout != null && layout.Spaces.Count == 1 && placements.TryGetValue(layout.Spaces[0], out var value)) placement = value;
            return result;
        }

        public static Vector2Int LayoutOffset(ShipLayoutAuthoring layout, ShipRoomAuthoring room) =>
            new Vector2Int(checked((int)((double)room.transform.position.x - layout.transform.position.x)),
                checked((int)((double)room.transform.position.y - layout.transform.position.y)));

        public static ShipLayoutTopology TopologyOf(ShipLayoutAuthoring layout) => ShipLayoutTopology.Build(
            layout.Spaces.Select(room => new ShipSpaceCells(room.RoomId, RoomAuthoringOperations.ReadCells(room), LayoutOffset(layout, room))));

        private static void AddDisconnectedIssues(ShipLayoutTopology topology, string roomId, List<RoomGenerationIssue> issues)
        {
            var components = topology.ComponentsOf(roomId);
            if (components.Count <= 1) return;
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                var start = new Vector2Int(component.Min(c => c.x), component.Min(c => c.y));
                var end = new Vector2Int(component.Max(c => c.x), component.Max(c => c.y));
                var issue = new RoomGenerationIssue(RoomIssueKind.Geometry, "Disconnected/" + i,
                    "Space has " + components.Count + " disconnected regions. Reconnect or erase the separated cells.", true, start, end);
                issue.LocateInRoom(roomId); issues.Add(issue);
            }
        }

        public static RoomGenerationResult AssessSpaces(ShipLayoutAuthoring layout,
            out Dictionary<ShipRoomAuthoring, RoomPlacement> placements)
        {
            placements = new Dictionary<ShipRoomAuthoring, RoomPlacement>();
            var safety = RoomAuthoringOperations.GetSafetyIssue(layout, allowMissingOutput: true);
            if (safety != null)
            {
                var refusal = new RoomGenerationIssue(RoomIssueKind.Safety, "layout", safety + " Retained output was not updated.");
                refusal.LocateInRoom(layout == null ? "<missing-layout>" : layout.LayoutId);
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = new[] { refusal } };
            }
            ShipLayoutTopology topology;
            try { topology = TopologyOf(layout); }
            catch (System.OverflowException)
            {
                var refusal = new RoomGenerationIssue(RoomIssueKind.Safety, "coordinates", "Layout cell coordinates exceed the supported integer grid.");
                refusal.LocateInRoom(layout.LayoutId);
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = new[] { refusal } };
            }
            if (topology.Overlaps.Count != 0)
            {
                var conflicts = topology.Overlaps.Select(cell =>
                {
                    var owners = string.Join(", ", topology.OwnersAt(cell));
                    var issue = new RoomGenerationIssue(RoomIssueKind.Safety, "overlap", "Cell has conflicting owners: " + owners,
                        true, cell, cell);
                    issue.LocateInRoom(owners); return issue;
                }).ToArray();
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = conflicts };
            }
            if (layout.Spaces.Count == 1)
            {
                var single = AssessSpace(layout.Spaces[0], out var placement);
                if (placement != null) placements.Add(layout.Spaces[0], placement);
                return single;
            }
            var issues = new List<RoomGenerationIssue>();
            var refused = false;
            foreach (var room in layout.Spaces.OrderBy(r => r.RoomId, System.StringComparer.Ordinal))
            {
                var result = AssessSpace(room, out var placement, false, false);
                refused |= result.Status == RoomGenerationStatus.Refused;
                var offset = LayoutOffset(layout, room);
                foreach (var issue in result.Issues)
                {
                    var copy = JsonUtility.FromJson<RoomGenerationIssue>(JsonUtility.ToJson(issue));
                    if (copy.HasRegion) { copy.Start += offset; copy.End += offset; copy.LocateInRoom(room.RoomId); }
                    issues.Add(copy);
                }
                if (placement != null) placements.Add(room, new RoomPlacement { Geometry = placement.Geometry, Floor = placement.Floor });
            }
            LayoutBoundaryGeometry boundaries;
            try { boundaries = LayoutBoundaryResolver.Resolve(topology); }
            catch (System.OverflowException)
            {
                var issue = new RoomGenerationIssue(RoomIssueKind.Safety, "coordinates", "Layout boundary coordinates exceed the supported integer grid.");
                issue.LocateInRoom(layout.LayoutId);
                return new RoomGenerationResult { Status = RoomGenerationStatus.Refused, Issues = new[] { issue } };
            }
            if (!refused)
            {
                var styled = RoomPlacementBuilder.BuildLayout(boundaries, layout.Spaces.ToDictionary(r => r.RoomId, r => r.Style, System.StringComparer.Ordinal));
                issues.AddRange(styled.Issues);
                var owners = layout.Spaces.ToDictionary(r => r.RoomId, System.StringComparer.Ordinal);
                foreach (var item in styled.Sprites)
                {
                    var room = owners[item.OwnerId];
                    var offset = LayoutOffset(layout, room);
                    placements[room].Sprites.Add(new RoomSpritePlacement(item.Role, item.Sprite,
                        item.Position - new Vector3(offset.x, offset.y, 0), item.OwnerId));
                }
            }
            return new RoomGenerationResult
            {
                Status = refused ? RoomGenerationStatus.Refused : topology.Cells.Count == 0 ? RoomGenerationStatus.Empty :
                    issues.Count == 0 ? RoomGenerationStatus.Complete : RoomGenerationStatus.IncompletePreview,
                Issues = issues.OrderBy(i => i.Id, System.StringComparer.Ordinal).ToArray(),
                InputFingerprint = LayoutInputFingerprint(layout)
            };
        }

        public static bool IsCurrent(ShipLayoutAuthoring layout)
        {
            if (layout == null || layout.Spaces.Count == 0 || layout.Spaces.Any(room => room == null)) return false;
            var saved = layout.GenerationResult;
            if (layout.Spaces.Count > 1)
            {
                if (saved == null || saved.Status == RoomGenerationStatus.Refused || string.IsNullOrEmpty(saved.InputFingerprint)) return false;
                var assessment = AssessSpaces(layout, out _);
                return assessment.Status != RoomGenerationStatus.Refused && saved.InputFingerprint == assessment.InputFingerprint &&
                    saved.OutputFingerprint == OutputFingerprint(layout);
            }
            var roomResult = layout.Spaces[0].GenerationResult;
            return saved != null && roomResult != null && saved.Status == roomResult.Status &&
                saved.InputFingerprint == roomResult.InputFingerprint && saved.OutputFingerprint == roomResult.OutputFingerprint &&
                IsCurrent(layout.Spaces[0]);
        }

        public static bool IsCurrent(ShipRoomAuthoring room)
        {
            if (room != null && room.Layout != null && room.Layout.Spaces.Count > 1) return IsCurrent(room.Layout);
            var saved = room == null ? null : room.GenerationResult;
            if (saved == null || saved.Status == RoomGenerationStatus.Refused || string.IsNullOrEmpty(saved.InputFingerprint) ||
                string.IsNullOrEmpty(saved.OutputFingerprint)) return false;
            var assessment = Assess(room, out _);
            return assessment.Status != RoomGenerationStatus.Refused && saved.InputFingerprint == assessment.InputFingerprint &&
                saved.OutputFingerprint == OutputFingerprint(room);
        }

        public static string GetAdoptionOutputIssue(ShipRoomAuthoring room)
        {
            var output = room.GeneratedOutput;
            if (output.GetComponents<Component>().Any(component => component == null ||
                (component.GetType() != typeof(Transform) && component.GetType() != typeof(RoomGeneratedOutput))))
                return "Generated output root contains unrecognized components; move authored content outside it before adoption.";
            foreach (Transform child in output.transform)
            {
                var floor = child.GetComponent<Tilemap>() != null;
                var allowed = floor ? new[] { typeof(Transform), typeof(Tilemap), typeof(TilemapRenderer) }
                    : new[] { typeof(Transform), typeof(SpriteRenderer) };
                var components = child.GetComponents<Component>();
                if (child.childCount != 0 || components.Length != allowed.Length ||
                    components.Any(component => component == null || !allowed.Contains(component.GetType())))
                    return "Generated output contains unrecognized content at " + child.name + "; move authored content outside it before adoption.";
            }
            // Existing children must match the last recorded build. Names alone do not establish disposal rights.
            if (output.transform.childCount != 0 && !IsCurrent(room))
                return "Generated output is modified or stale. Review its content before rebuilding and adopting.";
            return null;
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
            if (room.Layout != null) text.Append(room.Layout.LayoutId);
            foreach (var cell in RoomAuthoringOperations.ReadCells(room).OrderBy(c => c.y).ThenBy(c => c.x)) text.Append(JsonUtility.ToJson(cell));
            var style = room.Style;
            text.Append(Reference(style));
            if (style != null)
            {
                text.Append(EditorJsonUtility.ToJson(style));
                if (style.FloorTile is Tile tile) { text.Append(EditorJsonUtility.ToJson(tile)); SpriteFingerprint(text, tile.sprite); }
                var modules = (style.NorthWalls ?? System.Array.Empty<RoomStyleDefinition.Module>()).Concat(new[]
                    { style.WestCap, style.EastCap, style.SouthCap, style.Northwest, style.Northeast, style.Southwest, style.Southeast,
                        style.ConcaveNorthwest, style.ConcaveNortheast, style.ConcaveSouthwest, style.ConcaveSoutheast })
                    .Concat((style.JunctionModules ?? System.Array.Empty<RoomStyleDefinition.JunctionModule>())
                        .Where(binding => binding != null).Select(binding => binding.Artwork));
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
                if (renderer != null) text.Append(Reference(renderer.sprite)).Append(renderer.sortingOrder).Append(renderer.sortingLayerID)
                    .Append(renderer.enabled).Append(renderer.forceRenderingOff).Append(JsonUtility.ToJson(renderer.color));
                var map = child.GetComponent<Tilemap>();
                if (map != null)
                {
                    text.Append(JsonUtility.ToJson(map.tileAnchor)).Append(JsonUtility.ToJson(map.color));
                    var floorRenderer = child.GetComponent<TilemapRenderer>();
                    text.Append(floorRenderer == null ? "FloorRenderer:missing" : "FloorRenderer:present");
                    if (floorRenderer != null)
                        text.Append(floorRenderer.enabled).Append(floorRenderer.forceRenderingOff)
                            .Append(floorRenderer.sortingLayerID).Append(floorRenderer.sortingOrder);
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                        if (map.HasTile(cell))
                            text.Append(JsonUtility.ToJson(cell)).Append(Reference(map.GetTile(cell)))
                                .Append(JsonUtility.ToJson(map.GetColor(cell)))
                                .Append(JsonUtility.ToJson(map.GetTransformMatrix(cell)))
                                .Append((int)map.GetTileFlags(cell));
                }
            }
            return Hash(text);
        }

        private static string LayoutInputFingerprint(ShipLayoutAuthoring layout)
        {
            var text = new StringBuilder(layout.LayoutId).Append(JsonUtility.ToJson(layout.transform.position));
            foreach (var room in layout.Spaces.OrderBy(r => r.RoomId, System.StringComparer.Ordinal))
                text.Append(room.RoomId).Append(InputFingerprint(room));
            return Hash(text);
        }

        public static string OutputFingerprint(ShipLayoutAuthoring layout)
        {
            if (layout.Spaces.Count == 1) return OutputFingerprint(layout.Spaces[0]);
            var text = new StringBuilder(layout.LayoutId);
            foreach (var room in layout.Spaces.OrderBy(r => r.RoomId, System.StringComparer.Ordinal))
                text.Append(room.RoomId).Append(OutputFingerprint(room));
            return Hash(text);
        }

        private static string GetOverlapIssue(ShipRoomAuthoring room, bool includeLayoutMembers = true)
        {
            var positions = new HashSet<Vector3>();
            foreach (var c in RoomAuthoringOperations.ReadCells(room)) positions.Add(room.Occupancy.CellToWorld(new Vector3Int(c.x, c.y, 0)));
            foreach (var other in room.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ShipRoomAuthoring>(true)))
            {
                if (other == room || other.Occupancy == null) continue;
                if (!includeLayoutMembers && room.Layout != null && other.Layout == room.Layout) continue;
                var issue = RoomAuthoringOperations.GetSafetyIssue(other);
                if (issue != null) return "Cannot qualify room overlap: " + other.RoomId + ": " + issue;
                foreach (var c in RoomAuthoringOperations.ReadCells(other))
                    if (positions.Contains(other.Occupancy.CellToWorld(new Vector3Int(c.x, c.y, 0)))) return "Authored region overlaps room " + other.RoomId + ".";
            }
            return null;
        }

    }
}
