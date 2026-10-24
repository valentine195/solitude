using System;
using System.Collections.Generic;
using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEngine;

namespace SOLITUDE.Editor.RoomAuthoring
{
    // A read-only snapshot of eligible native-pixel area. No textures, renderers or second occupancy source.
    public sealed class RoomFloorPanelDomain
    {
        private readonly RoomFloorPanelSettings settings;
        private readonly HashSet<Vector2Int> eligible;
        public string SpaceId { get; }
        public Vector2Int Offset { get; }
        public RoomFloorPanelSettings Settings => settings.Copy();
        public IReadOnlyList<Vector2Int> EligibleCells { get; }
        public IReadOnlyList<Vector2Int> ExcludedCells { get; }
        public IReadOnlyList<RectInt> PixelRegions { get; }
        public IReadOnlyList<RoomGenerationIssue> Issues { get; }
        public bool CanGenerate => settings.Enabled && Issues.Count == 0 && eligible.Count != 0;

        private RoomFloorPanelDomain(string owner, Vector2Int offset, RoomFloorPanelSettings config,
            HashSet<Vector2Int> cells, HashSet<Vector2Int> excluded, List<RectInt> regions, List<RoomGenerationIssue> issues)
        {
            SpaceId = owner; Offset = offset; settings = config.Copy(); eligible = cells;
            EligibleCells = Array.AsReadOnly(cells.OrderBy(c => c.y).ThenBy(c => c.x).ToArray());
            ExcludedCells = Array.AsReadOnly(excluded.OrderBy(c => c.y).ThenBy(c => c.x).ToArray());
            PixelRegions = regions.AsReadOnly();
            Issues = Array.AsReadOnly(issues.OrderBy(i => i.Id, StringComparer.Ordinal).ThenBy(i => i.Reason, StringComparer.Ordinal).ToArray());
        }

        public bool ContainsPixel(Vector2Int pixel) => eligible.Contains(new Vector2Int(
            (int)Math.Floor((double)pixel.x / RoomFloorPanelSettings.PixelsPerCell),
            (int)Math.Floor((double)pixel.y / RoomFloorPanelSettings.PixelsPerCell)));

        internal static string InputKey(RoomFloorPanelSettings config, IEnumerable<RoomFloorReservation> records) =>
            JsonUtility.ToJson(config) + "|" + string.Join("|", records.Select(r => r == null ? "<null>" : JsonUtility.ToJson(r)).OrderBy(s => s, StringComparer.Ordinal));

        public static bool HasAuthoredInputs(ShipRoomAuthoring room) => room.FloorReservations.Count != 0 ||
            JsonUtility.ToJson(room.FloorPanels) != JsonUtility.ToJson(new RoomFloorPanelSettings());

        public static RoomFloorPanelDomain Build(ShipRoomAuthoring room) => Build(room, room.FloorPanels, room.FloorReservations);

        public static RoomFloorPanelDomain Build(ShipRoomAuthoring room, RoomFloorPanelSettings config,
            IReadOnlyList<RoomFloorReservation> reservations, bool retainInactiveRecords = true)
        {
            if (room == null || config == null || reservations == null) throw new ArgumentNullException();
            var issues = new List<RoomGenerationIssue>();
            var local = new HashSet<Vector2Int>(); var cells = new HashSet<Vector2Int>(); var excluded = new HashSet<Vector2Int>();
            var regions = new List<RectInt>(); var offset = Vector2Int.zero;
            void Issue(string field, string reason, bool inactive = false, RoomFloorReservation record = null)
            {
                var issue = new RoomGenerationIssue(RoomIssueKind.Geometry, "FloorPanels/" + field, reason) { Inactive = inactive };
                if (record != null && record.AddressIssue() == null)
                {
                    var x = (long)record.Position.x + offset.x; var y = (long)record.Position.y + offset.y;
                    var right = x + (int)record.Size.x; var top = y + (int)record.Size.y;
                    if (x >= int.MinValue && y >= int.MinValue && right <= int.MaxValue && top <= int.MaxValue)
                    {
                        issue.HasRegion = true; issue.Start = new Vector2Int((int)x, (int)y); issue.End = new Vector2Int((int)right, (int)top);
                    }
                }
                issue.LocateInRoom(room.RoomId); issues.Add(issue);
            }
            RoomFloorPanelDomain Result() => new RoomFloorPanelDomain(room.RoomId, offset, config, cells, excluded, regions, issues);
            var safety = room.Layout == null ? RoomAuthoringOperations.GetSafetyIssue(room, true) : RoomAuthoringOperations.GetSafetyIssue(room.Layout, true);
            if (safety != null) { Issue("Safety", safety); return Result(); }
            try
            {
                offset = room.Layout == null ? Vector2Int.zero : RoomAuthoringValidator.LayoutOffset(room.Layout, room);
                local.UnionWith(RoomAuthoringOperations.ReadCells(room));
                foreach (var cell in local) cells.Add(new Vector2Int(checked(cell.x + offset.x), checked(cell.y + offset.y)));
            }
            catch (OverflowException) { Issue("Coordinates", "Floor cells exceed the supported layout coordinate range."); return Result(); }
            var empty = cells.Count == 0;
            var settingsIssue = config.ValidationIssue();
            if (settingsIssue != null) Issue("Settings", settingsIssue, empty);
            if (empty && (config.Enabled || JsonUtility.ToJson(config) != JsonUtility.ToJson(new RoomFloorPanelSettings())))
                Issue("Inactive", "Inactive floor panel settings and seed retained: space has no occupied cells.", true);
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < reservations.Count; index++)
            {
                var record = reservations[index]; var field = "Reservation/" + (record?.Id ?? "<missing-" + index + ">");
                var reason = record == null ? "Reservation record is missing." : record.AddressIssue();
                if (record != null && !identities.Add(record.Id ?? "")) reason = "Reservation identity is duplicated.";
                if (reason == null && record.SpaceId != room.RoomId) reason = "Reservation belongs to space " + record.SpaceId + ", not " + room.RoomId + ".";
                if (empty && retainInactiveRecords)
                {
                    Issue(field, "Inactive reservation retained: space has no occupied cells." + (reason == null ? "" : " " + reason), true, record);
                    continue;
                }
                if (reason == null)
                {
                    var area = (long)(int)record.Size.x * (int)record.Size.y;
                    if (area > local.Count) reason = "Reservation must remain entirely inside its space's occupied cells.";
                    else
                    {
                        for (var y = 0; y < (int)record.Size.y && reason == null; y++)
                            for (var x = 0; x < (int)record.Size.x; x++)
                                if (!local.Contains(new Vector2Int((int)record.Position.x + x, (int)record.Position.y + y)))
                                { reason = "Reservation must remain entirely inside its space's occupied cells."; break; }
                    }
                }
                if (reason != null) { Issue(field, reason, false, record); continue; }
                for (var y = 0; y < (int)record.Size.y; y++)
                    for (var x = 0; x < (int)record.Size.x; x++)
                        excluded.Add(new Vector2Int(checked((int)record.Position.x + x + offset.x), checked((int)record.Position.y + y + offset.y)));
            }
            if (!empty && room.Layout != null)
            {
                try
                {
                    var topology = RoomAuthoringValidator.TopologyOf(room.Layout);
                    var connectionIssues = topology.Overlaps.Count == 0 ? LayoutBoundaryResolver.ConnectionIssues(topology, room.Layout.Connections) : Array.Empty<RoomGenerationIssue>();
                    if (topology.Overlaps.Count != 0 || connectionIssues.Length != 0)
                        Issue("Connections", "Resolve layout ownership and invalid connections before paneling.");
                    else
                        foreach (var connection in room.Layout.Connections.Where(c => c.Type != LayoutConnectionType.OpenPassage &&
                            (c.NegativeOwner == room.RoomId || c.PositiveOwner == room.RoomId)))
                            foreach (var edge in connection.Edges())
                            {
                                var cell = edge.Start;
                                if (connection.NegativeOwner == room.RoomId)
                                    cell = new Vector2Int(checked(cell.x - (connection.Horizontal ? 0 : 1)), checked(cell.y - (connection.Horizontal ? 1 : 0)));
                                excluded.Add(cell);
                            }
                }
                catch (OverflowException) { Issue("Connections", "Connection coordinates exceed the supported panel grid."); }
            }
            cells.ExceptWith(excluded);
            try
            {
                foreach (var cell in cells.OrderBy(c => c.y).ThenBy(c => c.x))
                {
                    var x = checked(cell.x * RoomFloorPanelSettings.PixelsPerCell); var y = checked(cell.y * RoomFloorPanelSettings.PixelsPerCell);
                    _ = checked(x + RoomFloorPanelSettings.PixelsPerCell); _ = checked(y + RoomFloorPanelSettings.PixelsPerCell);
                    regions.Add(new RectInt(x, y, RoomFloorPanelSettings.PixelsPerCell, RoomFloorPanelSettings.PixelsPerCell));
                }
            }
            catch (OverflowException) { regions.Clear(); Issue("Coordinates", "Eligible floor exceeds the supported signed native-pixel coordinate range."); }
            return Result();
        }
    }
}
