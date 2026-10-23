using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.World.RoomAuthoring
{
    public readonly struct RoomSpritePlacement
    {
        public readonly string Role;
        public readonly Sprite Sprite;
        public readonly Vector3 Position;
        public readonly string OwnerId;
        public readonly RoomStyleDefinition.RenderingConfiguration Rendering;
        public RoomSpritePlacement(string role, Sprite sprite, Vector3 position, string ownerId = null, RoomStyleDefinition.RenderingConfiguration rendering = null)
        { Role = role; Sprite = sprite; Position = position; OwnerId = ownerId; Rendering = rendering; }
    }

    public sealed class RoomPlacement
    {
        public RoomGeometry Geometry;
        public Tile Floor;
        public Vector3 FloorAnchor;
        public RoomStyleDefinition.RenderingConfiguration Rendering;
        public readonly List<RoomGenerationIssue> Issues = new List<RoomGenerationIssue>();
        public readonly List<RoomStyleResolution> StyleResolutions = new List<RoomStyleResolution>();
        public readonly List<RoomSpritePlacement> Sprites = new List<RoomSpritePlacement>();
    }

    public static class RoomPlacementBuilder
    {
        public static bool TryBuild(RoomGeometry geometry, RoomStyleDefinition style, out RoomPlacement result, out string issue)
        {
            result = BuildFloor(geometry, style);
            issue = result.Issues.Count == 0 ? null : result.Issues[0].Reason;
            if (geometry.Cells.Length == 0 || style == null) return issue == null;
            var unresolvedJoins = new HashSet<RoomJunction>();
            foreach (var join in geometry.Junctions)
            {
                var joinIssue = CheckModule(JoinModule(style, join), join.Role + " corner", false, RoomStyleDefinition.ModuleOrientation.Corner) ?? CheckRendering(style.Rendering);
                if (joinIssue == null) joinIssue = CheckFootprint(JoinModule(style, join), join.Position, geometry.Cells, style.FloorEdgeInsetPixels);
                if (!style.SupportsDiagonalContact && geometry.DiagonalContacts.Contains(join.Position))
                    joinIssue = "Configured artwork cannot resolve separate diagonal branches at this junction.";
                if (joinIssue == null) continue;
                unresolvedJoins.Add(join);
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Join, join.Role, joinIssue, true, join.Position, join.Position));
            }
            foreach (var run in geometry.Runs) AddRun(result, run, style, unresolvedJoins);
            foreach (var join in geometry.Junctions)
                if (!unresolvedJoins.Contains(join)) Add(result, join.Role, JoinModule(style, join), join.Position, rendering: style.Rendering);
            issue = result.Issues.Count == 0 ? null : string.Join("\n", result.Issues.ConvertAll(i => i.Reason));
            return issue == null;
        }

        public static RoomPlacement BuildFloor(RoomGeometry geometry, RoomStyleDefinition style)
        {
            var result = new RoomPlacement { Geometry = geometry };
            if (geometry.Cells.Length == 0) return result;
            if (style == null)
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Style, "style", "Assign a room style."));
            else
            {
                var issue = CheckFloor(style);
                if (issue == null)
                {
                    result.Floor = (Tile)style.FloorTile;
                    result.FloorAnchor = result.Floor.sprite.pivot / style.FloorPixelsPerUnit;
                    result.Rendering = style.Rendering;
                }
                else result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Floor, "floorTile", issue));
            }
            return result;
        }

        public static RoomPlacement BuildLayout(LayoutBoundaryGeometry geometry, IReadOnlyDictionary<string, RoomStyleDefinition> styles,
            RoomStyleDefinition fallback = null)
        {
            var result = new RoomPlacement { Geometry = geometry.Exterior };
            var unresolved = new HashSet<RoomJunction>();
            var bindings = new Dictionary<Vector2Int, RoomStyleDefinition.JunctionModule>();
            var missingEndpoints = new HashSet<Vector2Int>();
            var inset = styles.Values.Where(s => s != null).Select(s => s.FloorEdgeInsetPixels).DefaultIfEmpty(0).Min();
            foreach (var join in geometry.Exterior.Junctions)
            {
                var kind = geometry.JunctionAt(join.Position).Kind;
                if (kind != RoomEndpointKind.Convex && kind != RoomEndpointKind.Concave && kind != RoomEndpointKind.Diagonal) continue;
                var owner = JoinOwner(geometry, join);
                var required = IncidentOwners(geometry, join.Position).Select(id => styles[id]).ToArray();
                // At a diagonal contact, each convex branch remains its own exterior treatment.
                if (join.Kind == RoomJoinKind.Convex) required = new[] { styles[owner] };
                var style = SelectStyle(styles[owner], required, fallback, candidate =>
                {
                    var problem = CheckModule(JoinModule(candidate, join), join.Role, false, RoomStyleDefinition.ModuleOrientation.Corner) ?? CheckRendering(candidate.Rendering);
                    if (problem == null) problem = CheckFootprint(JoinModule(candidate, join), join.Position, result.Geometry.Cells, inset, geometry.Edges);
                    if (!candidate.SupportsDiagonalContact && geometry.Exterior.DiagonalContacts.Contains(join.Position))
                        problem = "Configured artwork cannot resolve separate diagonal branches at this junction.";
                    return problem;
                }, out var issue, out var usedFallback);
                var module = style == null ? null : JoinModule(style, join);
                if (issue != null)
                {
                    unresolved.Add(join);
                    var located = new RoomGenerationIssue(RoomIssueKind.Join, join.Role, issue, true, join.Position, join.Position);
                    located.LocateInRoom(owner); result.Issues.Add(located);
                }
                else
                {
                    Add(result, join.Role, module, join.Position, owner, style.Rendering);
                    RecordStyle(result, owner, join.Role, join.Position, join.Position, style, usedFallback);
                }
            }
            foreach (var junction in geometry.Junctions.Where(j => j.Role.HasValue))
            {
                var eligible = IncidentOwners(geometry, junction.Position);
                var owner = eligible[0];
                var style = SelectStyle(styles[owner], eligible.Select(id => styles[id]), fallback, candidate =>
                {
                    var configured = JunctionBinding(candidate, junction.Role.Value);
                    if (configured == null) return "Configure exactly one artwork binding for " + junction.Role.Value + ".";
                    var problem = CheckModule(configured.Artwork, junction.Role.Value.ToString(), false, RoomStyleDefinition.ModuleOrientation.Junction) ?? CheckRendering(candidate.Rendering);
                    if (problem == null && !configured.Artwork.HasFootprint) problem = "Declare the junction artwork's visual footprint.";
                    if (problem == null) problem = CheckFootprint(configured.Artwork, junction.Position, result.Geometry.Cells, inset, geometry.Edges);
                    return problem ?? CheckJunctionCoverage(configured, junction.Directions);
                }, out var issue, out var usedFallback);
                var binding = style == null ? null : JunctionBinding(style, junction.Role.Value);
                // Even unresolved declared reservations must suppress dependent spans, never leave a gap labelled complete.
                if (binding != null) bindings.Add(junction.Position, binding);
                if (issue != null)
                {
                    missingEndpoints.Add(junction.Position);
                    var located = new RoomGenerationIssue(RoomIssueKind.Join, "PartitionEndpoint", junction.Role.Value + ": " + issue,
                        true, junction.Position, junction.Position);
                    located.LocateInRoom(owner); result.Issues.Add(located);
                }
                else
                {
                    Add(result, junction.Role.Value.ToString(), binding.Artwork, junction.Position, owner, style.Rendering);
                    RecordStyle(result, owner, junction.Role.Value.ToString(), junction.Position, junction.Position, style, usedFallback);
                }
            }
            // A straight change of visible style has no extra structural vertex sprite, but still needs explicit seam compatibility.
            foreach (var junction in geometry.Junctions.Where(j => j.Kind == RoomEndpointKind.Straight))
            {
                var eligible = IncidentOwners(geometry, junction.Position);
                var owner = eligible[0];
                var issue = CheckCompatibility(styles[owner], eligible.Select(id => styles[id]));
                if (issue == null) continue;
                var located = new RoomGenerationIssue(RoomIssueKind.Join, "StyleTransition", issue, true, junction.Position, junction.Position);
                located.LocateInRoom(owner); result.Issues.Add(located);
            }
            foreach (var span in geometry.Spans)
            {
                var edge = span.First;
                var owner = VisibleOwner(edge);
                var side = VisibleSide(edge);
                var style = SelectStyle(styles[owner], new[] { styles[owner] }, fallback,
                    candidate => CheckRunModules(candidate, side), out var selectionIssue, out var usedFallback);
                var before = result.Issues.Count;
                if (selectionIssue != null)
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Asset, side.ToString(), selectionIssue,
                        true, edge.Address.Start, span.End));
                else
                {
                    var starts = geometry.Exterior.Junctions.Where(j => j.Position == edge.Address.Start && j.HasSide(side) &&
                        (span.StartKind == RoomEndpointKind.Convex || span.StartKind == RoomEndpointKind.Concave || span.StartKind == RoomEndpointKind.Diagonal)).ToArray();
                    var ends = geometry.Exterior.Junctions.Where(j => j.Position == span.End && j.HasSide(side) &&
                        (span.EndKind == RoomEndpointKind.Convex || span.EndKind == RoomEndpointKind.Concave || span.EndKind == RoomEndpointKind.Diagonal)).ToArray();
                    var run = new RoomBoundaryRun(side, edge.Address.Start, span.Length,
                        starts.FirstOrDefault(), ends.FirstOrDefault(), starts.Length != 0, ends.Length != 0, span.StartKind, span.EndKind);
                    var defaults = RunReservations(style, run, edge.Kind == LayoutEdgeKind.Partition);
                    var reservations = new Vector2Int(bindings.TryGetValue(run.Start, out var startBinding) ?
                        startBinding.Reservation(edge.Address.Horizontal ? BoundaryDirections.East : BoundaryDirections.North) : defaults.x,
                        bindings.TryGetValue(run.End, out var endBinding) ?
                        endBinding.Reservation(edge.Address.Horizontal ? BoundaryDirections.West : BoundaryDirections.South) : defaults.y);
                    AddRun(result, run, style, unresolved, owner, edge.Kind == LayoutEdgeKind.Partition,
                        reservations, missingEndpoints.Contains(run.Start), missingEndpoints.Contains(run.End), inset, geometry.Edges);
                    if (result.Issues.Count == before)
                        RecordStyle(result, owner, edge.Kind == LayoutEdgeKind.Partition ? "Partition" + side : side.ToString(), run.Start, run.End, style, usedFallback);
                }
                foreach (var issue in result.Issues.Skip(before)) issue.LocateInRoom(owner);
            }
            return result;
        }

        private static string JoinOwner(LayoutBoundaryGeometry geometry, RoomJunction join)
        {
            if (join.Kind == RoomJoinKind.Convex)
            {
                var cell = join.Position - (join.Corner == RoomCorner.Northeast ? Vector2Int.one :
                    join.Corner == RoomCorner.Northwest ? Vector2Int.up : join.Corner == RoomCorner.Southeast ? Vector2Int.right : Vector2Int.zero);
                return geometry.Topology.OwnersAt(cell).Single();
            }
            return geometry.Edges.Where(e => e.Kind == LayoutEdgeKind.Exterior &&
                (e.Address.Start == join.Position || e.Address.End == join.Position)).Select(VisibleOwner)
                .OrderBy(id => id, StringComparer.Ordinal).First();
        }

        // Topology stays style-free. Horizontal partitions expose the south space's north face;
        // two-sided caps use the stable-ID tie already present on the canonical edge.
        private static string VisibleOwner(LayoutBoundaryEdge edge) => edge.Kind == LayoutEdgeKind.Partition && edge.Address.Horizontal
            ? edge.NegativeOwner : edge.VisualOwner;
        private static RoomSide VisibleSide(LayoutBoundaryEdge edge) => edge.Kind == LayoutEdgeKind.Partition && edge.Address.Horizontal
            ? RoomSide.North : edge.Side;

        private static string[] IncidentOwners(LayoutBoundaryGeometry geometry, Vector2Int point) => geometry.Edges
            .Where(e => (e.Kind == LayoutEdgeKind.Exterior || e.Kind == LayoutEdgeKind.Partition) &&
                (e.Address.Start == point || e.Address.End == point))
            .Select(VisibleOwner).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();

        private static RoomStyleDefinition.JunctionModule JunctionBinding(RoomStyleDefinition style, LayoutJunctionRole role)
        {
            var bindings = (style.JunctionModules ?? Array.Empty<RoomStyleDefinition.JunctionModule>()).Where(b => b != null && b.Role == role).ToArray();
            return bindings.Length == 1 ? bindings[0] : null;
        }

        private static string CheckCompatibility(RoomStyleDefinition candidate, IEnumerable<RoomStyleDefinition> required)
        {
            if (candidate == null) return "Assign a style for this structural role.";
            foreach (var other in required.Distinct())
                if (other == null || other != candidate && !(candidate.CompatibleTransitions ?? Array.Empty<RoomStyleDefinition>()).Contains(other))
                    return "Style " + candidate.StyleId + " needs an explicitly compatible transition to " +
                        (other == null ? "the missing incident style" : other.StyleId) + ". Qualify compatible artwork in Compatible Transitions.";
            return null;
        }

        private static RoomStyleDefinition SelectStyle(RoomStyleDefinition preferred, IEnumerable<RoomStyleDefinition> required,
            RoomStyleDefinition fallback, Func<RoomStyleDefinition, string> check, out string issue, out bool usedFallback)
        {
            var incident = required.ToArray();
            issue = CheckCompatibility(preferred, incident);
            if (issue == null) issue = check(preferred);
            usedFallback = false;
            if (issue == null || fallback == null || fallback == preferred) return preferred;
            var fallbackIssue = CheckCompatibility(fallback, incident.Concat(new[] { preferred }));
            if (fallbackIssue == null) fallbackIssue = check(fallback);
            if (fallbackIssue != null) { issue += " Layout fallback unavailable: " + fallbackIssue; return preferred; }
            usedFallback = true; issue = null;
            return fallback;
        }

        private static void RecordStyle(RoomPlacement result, string owner, string role, Vector2Int start, Vector2Int end,
            RoomStyleDefinition style, bool fallback) => result.StyleResolutions.Add(new RoomStyleResolution
            { OwnerId = owner, Role = role, Start = start, End = end, StyleId = style.StyleId, UsedFallback = fallback });

        private static RoomStyleDefinition.Module[] RunModules(RoomStyleDefinition style, RoomSide side) => side == RoomSide.North ? style.NorthWalls :
            new[] { side == RoomSide.West ? style.WestCap : side == RoomSide.East ? style.EastCap : style.SouthCap };

        private static string CheckRunModules(RoomStyleDefinition style, RoomSide side)
        {
            var modules = RunModules(style, side);
            if (modules == null || modules.Length == 0) return side + " wall modules are missing.";
            var repeat = modules[0] == null ? 0 : LogicalRepeat(modules[0]);
            foreach (var module in modules)
            {
                var issue = CheckModule(module, side.ToString(), side == RoomSide.North,
                    (RoomStyleDefinition.ModuleOrientation)Enum.Parse(typeof(RoomStyleDefinition.ModuleOrientation), side.ToString())) ?? CheckRendering(style.Rendering);
                if (issue != null) return issue;
                if (LogicalRepeat(module) != repeat) return side + " requires sprites with one structural repeat width.";
            }
            return null;
        }

        private static int VariantIndex(Vector2 origin, string role, RoomStyleDefinition style, int count)
        {
            // FNV-1a with explicit integer bytes/UTF-16 code units: no process-randomized string hash or mutable run index.
            unchecked
            {
                uint hash = 2166136261;
                void Integer(long value) { for (var i = 0; i < 8; i++) { hash = (hash ^ (byte)value) * 16777619; value >>= 8; } }
                void Text(string value) { foreach (var c in value ?? "") Integer(c); Integer(-1); }
                Integer((long)Math.Round((double)origin.x * 32)); Integer((long)Math.Round((double)origin.y * 32));
                Text(role); Text(style.StyleId); Integer(style.StructuralSeed);
                return (int)(hash % (uint)count);
            }
        }

        private static Vector2Int RunReservations(RoomStyleDefinition style, RoomBoundaryRun run, bool partition)
        {
            var convex = Reservations(style, run.Side, false);
            var concave = Reservations(style, run.Side, true);
            return new Vector2Int(!partition && run.HasStartJoin ? (run.StartJoin.Kind == RoomJoinKind.Concave ? concave.x : convex.x) : 0,
                !partition && run.HasEndJoin ? (run.EndJoin.Kind == RoomJoinKind.Concave ? concave.y : convex.y) : 0);
        }

        private static void AddRun(RoomPlacement result, RoomBoundaryRun run, RoomStyleDefinition style,
            HashSet<RoomJunction> unresolvedJoins, string ownerId = null, bool partition = false,
            Vector2Int? endpointReservations = null, bool unresolvedStart = false, bool unresolvedEnd = false, int? floorInset = null, IReadOnlyList<LayoutBoundaryEdge> apertures = null)
        {
            var modules = RunModules(style, run.Side);
            var reservations = endpointReservations ?? RunReservations(style, run, partition);
            var repeat = modules == null || modules.Length == 0 || modules[0] == null ? 0 : LogicalRepeat(modules[0]);
            var end = run.End;
            var runIssue = CheckRunModules(style, run.Side);
            if (runIssue != null)
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Asset, run.Side.ToString(), runIssue, true, run.Start, end));
                return;
            }
            if ((reservations.x > 0 && (unresolvedStart || run.HasStartJoin && unresolvedJoins.Contains(run.StartJoin))) ||
                (reservations.y > 0 && (unresolvedEnd || run.HasEndJoin && unresolvedJoins.Contains(run.EndJoin))))
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Span, run.Side.ToString(),
                    run.Side + " span requires its unresolved endpoint join to supply reserved coverage.", true, run.Start, end));
                return;
            }
            if (run.Side == RoomSide.North && (style.NorthWallClearanceCells < 0 ||
                result.Geometry.Cells.Any(c => c.x >= run.Start.x && c.x < end.x && c.y >= run.Start.y && (long)c.y - run.Start.y < style.NorthWallClearanceCells)))
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.VisualFit, "North", "North wall's declared clearance conflicts with authored floor or has an invalid clearance setting.", true, run.Start, end));
                return;
            }
            var span = (long)run.Length * 32 - reservations.x - reservations.y;
            if (repeat <= 0 || reservations.x < 0 || reservations.y < 0 || span < 0 || span % repeat != 0)
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Span, run.Side.ToString(),
                    run.Side + " span is not exactly coverable after corner reservations (check repeat/reservations).", true, run.Start, end));
                return;
            }
            var horizontal = run.Side == RoomSide.North || run.Side == RoomSide.South;
            for (var n = 0; n < span / repeat; n++)
            {
                var origin = (Vector2)run.Start + (horizontal ? Vector2.right : Vector2.up) * ((reservations.x + n * repeat) / 32f);
                var role = partition ? "Partition" + run.Side : run.Side.ToString();
                var module = modules[VariantIndex(origin, role, style, modules.Length)];
                var fit = CheckFootprint(module, origin, result.Geometry.Cells, floorInset ?? style.FloorEdgeInsetPixels, apertures);
                if (fit != null)
                {
                    var location = Vector2Int.FloorToInt(origin);
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.VisualFit, run.Side.ToString(), fit, true, location, location));
                    continue;
                }
                Add(result, role, module, origin, ownerId, style.Rendering);
            }
        }

        private static string CheckFootprint(RoomStyleDefinition.Module module, Vector2 origin, Vector2Int[] cells, int inset, IReadOnlyList<LayoutBoundaryEdge> apertures = null)
        {
            // Check actual declared ink, or conservatively the whole canvas when no footprint is supplied.
            foreach (var rect in module.HasFootprint ? module.FootprintsPixels ?? Array.Empty<RectInt>() :
                new[] { new RectInt(Vector2Int.zero, module.CanvasPixels) })
            {
                var min = origin + ((Vector2)rect.position - module.AnchorPixels + module.OffsetPixels) / module.PixelsPerUnit;
                var max = min + (Vector2)rect.size / module.PixelsPerUnit;
                foreach (var aperture in apertures ?? Array.Empty<LayoutBoundaryEdge>())
                {
                    if (aperture.Kind != LayoutEdgeKind.Opening) continue;
                    var a = aperture.Address.Start; var b = aperture.Address.End;
                    var enters = aperture.Address.Horizontal ? min.x < b.x - .000001f && max.x > a.x + .000001f && min.y <= a.y && max.y >= a.y :
                        min.y < b.y - .000001f && max.y > a.y + .000001f && min.x <= a.x && max.x >= a.x;
                    if (enters) return "Artwork footprint intrudes into connection " + aperture.ConnectionId + " at " + aperture.Address + ". Use compatible artwork or alignment; the aperture remains clear.";
                }
            }
            if (!module.HasFootprint) return null;
            if (inset < 0 || inset > 15) return "Floor edge inset must be between 0 and 15 pixels, preserving a walkable cell core.";
            if (module.FootprintsPixels == null || module.FootprintsPixels.Length == 0) return "Declare at least one visual footprint rectangle.";
            foreach (var rect in module.FootprintsPixels)
            {
                if (rect.width <= 0 || rect.height <= 0 || rect.x < 0 || rect.y < 0 ||
                    (long)rect.x + rect.width > module.CanvasPixels.x || (long)rect.y + rect.height > module.CanvasPixels.y)
                    return "Visual footprint rectangles must be positive and inside the sprite canvas.";
                var min = origin + ((Vector2)rect.position - module.AnchorPixels + module.OffsetPixels) / module.PixelsPerUnit;
                var max = min + (Vector2)rect.size / module.PixelsPerUnit;
                var band = inset / 32f;
                foreach (var cell in cells)
                    if (min.x < cell.x + 1 - band - 0.000001f && max.x > cell.x + band + 0.000001f &&
                        min.y < cell.y + 1 - band - 0.000001f && max.y > cell.y + band + 0.000001f)
                        return "Artwork footprint enters a walkable floor core at " + cell.x + "," + cell.y + ". Adjust compatible artwork or alignment.";
            }
            return null;
        }

        private static string CheckJunctionCoverage(RoomStyleDefinition.JunctionModule binding, BoundaryDirections mask)
        {
            foreach (var direction in new[] { BoundaryDirections.North, BoundaryDirections.East, BoundaryDirections.South, BoundaryDirections.West })
            {
                var required = binding.Reservation(direction);
                if (required < 0 || ((mask & direction) == 0 && required != 0))
                    return "Junction reservations must be nonnegative and belong to incident boundary directions.";
                if (required == 0) continue;
                var horizontal = direction == BoundaryDirections.East || direction == BoundaryDirections.West;
                var positive = direction == BoundaryDirections.East || direction == BoundaryDirections.North;
                var intervals = new List<Vector2>();
                foreach (var rect in binding.Artwork.FootprintsPixels)
                {
                    var min = ((Vector2)rect.position - binding.Artwork.AnchorPixels + binding.Artwork.OffsetPixels) * (32f / binding.Artwork.PixelsPerUnit);
                    var max = min + (Vector2)rect.size * (32f / binding.Artwork.PixelsPerUnit);
                    var crossMin = horizontal ? min.y : min.x; var crossMax = horizontal ? max.y : max.x;
                    if (crossMin > 0 || crossMax < 0) continue;
                    var a = horizontal ? min.x : min.y; var b = horizontal ? max.x : max.y;
                    intervals.Add(positive ? new Vector2(a, b) : new Vector2(-b, -a));
                }
                var covered = 0f;
                foreach (var interval in intervals.OrderBy(i => i.x))
                {
                    if (interval.y <= covered) continue;
                    if (interval.x > covered + 0.000001f) break;
                    covered = interval.y;
                }
                if (covered + 0.000001f < required) return "Junction footprint does not supply its reserved " + direction + " coverage.";
            }
            return null;
        }

        private static RoomStyleDefinition.Module JoinModule(RoomStyleDefinition style, RoomJunction join)
        {
            if (join.Kind == RoomJoinKind.Concave)
                return join.Corner == RoomCorner.Southwest ? style.ConcaveSouthwest : join.Corner == RoomCorner.Southeast ? style.ConcaveSoutheast
                    : join.Corner == RoomCorner.Northwest ? style.ConcaveNorthwest : style.ConcaveNortheast;
            return join.Corner == RoomCorner.Southwest ? style.Southwest : join.Corner == RoomCorner.Southeast ? style.Southeast
                : join.Corner == RoomCorner.Northwest ? style.Northwest : style.Northeast;
        }

        private static Vector2Int Reservations(RoomStyleDefinition style, RoomSide side, bool concave)
        {
            if (concave) return side == RoomSide.North ? style.ConcaveNorthReservationsPixels : side == RoomSide.South ? style.ConcaveSouthReservationsPixels
                : side == RoomSide.West ? style.ConcaveWestReservationsPixels : style.ConcaveEastReservationsPixels;
            return side == RoomSide.North ? style.NorthReservationsPixels : side == RoomSide.South ? style.SouthReservationsPixels
                : side == RoomSide.West ? style.WestReservationsPixels : style.EastReservationsPixels;
        }

        private static bool Finite(Vector2 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y);

        private static int LogicalRepeat(RoomStyleDefinition.Module module) => module.PixelsPerUnit <= 0 ? 0 :
            (int)((long)module.RepeatPixels * 32 / module.PixelsPerUnit);

        private static string CheckFloor(RoomStyleDefinition style)
        {
            if (!(style.FloorTile is Tile tile) || tile.sprite == null) return "Floor requires a native sprite Tile.";
            var issue = CheckSprite(tile.sprite, style.FloorCanvasPixels, style.FloorPixelsPerUnit, "Floor");
            if (issue != null) return issue;
            if (style.FloorCanvasPixels != new Vector2Int(style.FloorPixelsPerUnit, style.FloorPixelsPerUnit))
                return "Floor canvas and declared PPU must cover exactly one logical cell.";
            return CheckRendering(style.Rendering) ?? (tile.transform != Matrix4x4.identity ? "Floor tile transform must be identity." : null);
        }

        private static string CheckModule(RoomStyleDefinition.Module module, string role, bool north,
            RoomStyleDefinition.ModuleOrientation orientation)
        {
            if (module == null) return role + " module is missing.";
            var issue = CheckSprite(module.Sprite, module.CanvasPixels, module.PixelsPerUnit, role);
            if (issue != null) return issue;
            if (!Enum.IsDefined(typeof(RoomStyleDefinition.ModuleOrientation), module.Orientation) ||
                module.Orientation != RoomStyleDefinition.ModuleOrientation.Any && module.Orientation != orientation)
                return role + " module orientation is incompatible with its role.";
            if (!Finite(module.AnchorPixels) || !Finite(module.OffsetPixels)) return role + " alignment must be finite.";
            var alignment = (module.Sprite.pivot - module.AnchorPixels + module.OffsetPixels) * (64f / module.PixelsPerUnit);
            if (!Mathf.Approximately(alignment.x, Mathf.Round(alignment.x)) || !Mathf.Approximately(alignment.y, Mathf.Round(alignment.y)))
                return role + " alignment must lie on the logical half-pixel grid.";
            if (module.RepeatPixels <= 0 || (long)module.RepeatPixels * 32 / module.PixelsPerUnit > int.MaxValue || (long)module.RepeatPixels * 32 % module.PixelsPerUnit != 0)
                return role + " repeat must be a positive integral logical-pixel distance.";
            var axis = orientation == RoomStyleDefinition.ModuleOrientation.West || orientation == RoomStyleDefinition.ModuleOrientation.East
                ? module.CanvasPixels.y : module.CanvasPixels.x;
            if (module.RepeatPixels > axis) return role + " repeat exceeds its oriented canvas.";
            if (north && module.AnchorPixels.y != module.OffsetPixels.y) return "North wall bottom must align with its north boundary and extend northward.";
            return null;
        }

        private static string CheckSprite(Sprite sprite, Vector2Int dimensions, int ppu, string role)
        {
            if (sprite == null) return role + " sprite is missing.";
            if (dimensions.x <= 0 || dimensions.y <= 0 || sprite.rect.size != (Vector2)dimensions)
                return role + " sprite dimensions do not match its declared canvas (including any trim/padding).";
            if (ppu <= 0 || !Mathf.Approximately(sprite.pixelsPerUnit, ppu)) return role + " requires " + ppu + " PPU matching its declared PPU.";
            if (sprite.texture.filterMode != FilterMode.Point) return role + " requires Point filtering.";
            return null;
        }

        private static string CheckRendering(RoomStyleDefinition.RenderingConfiguration rendering)
        {
            if (rendering == null) return null;
            if (!SortingLayer.layers.Any(layer => layer.name == rendering.SortingLayerName)) return "Configure an existing sorting layer.";
            if (rendering.FloorOrder < short.MinValue || rendering.ArchitectureOrder > short.MaxValue ||
                rendering.FloorOrder >= rendering.DetailOrder || rendering.DetailOrder >= rendering.ArchitectureOrder)
                return "Sorting must reserve distinct floor < detail < architecture orders within signed 16-bit limits.";
            foreach (var material in new[] { rendering.FloorMaterial, rendering.ArchitectureMaterial })
                if (material != null && (material.shader == null || !material.HasProperty("_MainTex")))
                    return "Configured renderer material must support sprite textures (_MainTex).";
            return null;
        }

        private static void Add(RoomPlacement result, string role, RoomStyleDefinition.Module module, Vector2 origin, string ownerId = null, RoomStyleDefinition.RenderingConfiguration rendering = null)
        {
            var position = origin + (module.Sprite.pivot - module.AnchorPixels + module.OffsetPixels) / module.PixelsPerUnit;
            result.Sprites.Add(new RoomSpritePlacement(role, module.Sprite, position, ownerId, rendering));
        }
    }
}
