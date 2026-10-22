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
        public RoomSpritePlacement(string role, Sprite sprite, Vector3 position, string ownerId = null)
        { Role = role; Sprite = sprite; Position = position; OwnerId = ownerId; }
    }

    public sealed class RoomPlacement
    {
        public RoomGeometry Geometry;
        public Tile Floor;
        public readonly List<RoomGenerationIssue> Issues = new List<RoomGenerationIssue>();
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
                var joinIssue = CheckModule(JoinModule(style, join), join.Role + " corner", false);
                if (joinIssue == null) joinIssue = CheckFootprint(JoinModule(style, join), join.Position, geometry.Cells, style.FloorEdgeInsetPixels);
                if (!style.SupportsDiagonalContact && geometry.DiagonalContacts.Contains(join.Position))
                    joinIssue = "Configured artwork cannot resolve separate diagonal branches at this junction.";
                if (joinIssue == null) continue;
                unresolvedJoins.Add(join);
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Join, join.Role, joinIssue, true, join.Position, join.Position));
            }
            foreach (var run in geometry.Runs) AddRun(result, run, style, unresolvedJoins);
            foreach (var join in geometry.Junctions)
                if (!unresolvedJoins.Contains(join)) Add(result, join.Role, JoinModule(style, join), join.Position);
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
                var issue = CheckFloor(style.FloorTile);
                if (issue == null) result.Floor = (Tile)style.FloorTile;
                else result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Floor, "floorTile", issue));
            }
            return result;
        }

        public static RoomPlacement BuildLayout(LayoutBoundaryGeometry geometry, IReadOnlyDictionary<string, RoomStyleDefinition> styles)
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
                var style = styles[owner];
                var module = style == null ? null : JoinModule(style, join);
                var issue = style == null ? "Assign a style for this corner." : CheckModule(module, join.Role, false);
                if (issue == null) issue = CheckFootprint(module, join.Position, result.Geometry.Cells, inset);
                if (style != null && !style.SupportsDiagonalContact && geometry.Exterior.DiagonalContacts.Contains(join.Position))
                    issue = "Configured artwork cannot resolve separate diagonal branches at this junction.";
                if (issue != null)
                {
                    unresolved.Add(join);
                    var located = new RoomGenerationIssue(RoomIssueKind.Join, join.Role, issue, true, join.Position, join.Position);
                    located.LocateInRoom(owner); result.Issues.Add(located);
                }
                else Add(result, join.Role, module, join.Position, owner);
            }
            foreach (var junction in geometry.Junctions.Where(j => j.Role.HasValue))
            {
                var owner = junction.Owners[0];
                var style = styles[owner];
                var configured = style == null ? Array.Empty<RoomStyleDefinition.JunctionModule>() :
                    (style.JunctionModules ?? Array.Empty<RoomStyleDefinition.JunctionModule>()).Where(b => b != null && b.Role == junction.Role.Value).ToArray();
                RoomStyleDefinition.JunctionModule binding = configured.Length == 1 ? configured[0] : null;
                if (binding != null) bindings.Add(junction.Position, binding);
                var issue = binding == null ? "Configure exactly one artwork binding for " + junction.Role.Value + "." :
                    CheckModule(binding.Artwork, junction.Role.Value.ToString(), false);
                if (issue == null && !binding.Artwork.HasFootprint) issue = "Declare the junction artwork's visual footprint.";
                if (issue == null) issue = CheckFootprint(binding.Artwork, junction.Position, result.Geometry.Cells, inset);
                if (issue == null) issue = CheckJunctionCoverage(binding, junction.Directions);
                if (issue != null)
                {
                    missingEndpoints.Add(junction.Position);
                    var located = new RoomGenerationIssue(RoomIssueKind.Join, "PartitionEndpoint", junction.Role.Value + ": " + issue,
                        true, junction.Position, junction.Position);
                    located.LocateInRoom(owner); result.Issues.Add(located);
                }
                else Add(result, junction.Role.Value.ToString(), binding.Artwork, junction.Position, owner);
            }
            foreach (var span in geometry.Spans)
            {
                var edge = span.First;
                var style = styles[edge.VisualOwner];
                var before = result.Issues.Count;
                if (style == null)
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Style, "style", "Assign a style for this boundary.",
                        true, edge.Address.Start, span.End));
                else
                {
                    var starts = geometry.Exterior.Junctions.Where(j => j.Position == edge.Address.Start && j.HasSide(edge.Side) &&
                        (span.StartKind == RoomEndpointKind.Convex || span.StartKind == RoomEndpointKind.Concave || span.StartKind == RoomEndpointKind.Diagonal)).ToArray();
                    var ends = geometry.Exterior.Junctions.Where(j => j.Position == span.End && j.HasSide(edge.Side) &&
                        (span.EndKind == RoomEndpointKind.Convex || span.EndKind == RoomEndpointKind.Concave || span.EndKind == RoomEndpointKind.Diagonal)).ToArray();
                    var run = new RoomBoundaryRun(edge.Side, edge.Address.Start, span.Length,
                        starts.FirstOrDefault(), ends.FirstOrDefault(), starts.Length != 0, ends.Length != 0, span.StartKind, span.EndKind);
                    var defaults = RunReservations(style, run, edge.Kind == LayoutEdgeKind.Partition);
                    var reservations = new Vector2Int(bindings.TryGetValue(run.Start, out var startBinding) ?
                        startBinding.Reservation(edge.Address.Horizontal ? BoundaryDirections.East : BoundaryDirections.North) : defaults.x,
                        bindings.TryGetValue(run.End, out var endBinding) ?
                        endBinding.Reservation(edge.Address.Horizontal ? BoundaryDirections.West : BoundaryDirections.South) : defaults.y);
                    AddRun(result, run, style, unresolved, edge.VisualOwner, edge.Kind == LayoutEdgeKind.Partition,
                        reservations, missingEndpoints.Contains(run.Start), missingEndpoints.Contains(run.End), inset);
                }
                foreach (var issue in result.Issues.Skip(before)) issue.LocateInRoom(edge.VisualOwner);
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
                (e.Address.Start == join.Position || e.Address.End == join.Position)).Select(e => e.VisualOwner)
                .OrderBy(id => id, StringComparer.Ordinal).First();
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
            Vector2Int? endpointReservations = null, bool unresolvedStart = false, bool unresolvedEnd = false, int? floorInset = null)
        {
            var modules = run.Side == RoomSide.North ? style.NorthWalls : new[] { run.Side == RoomSide.West ? style.WestCap : style.EastCap };
            if (run.Side == RoomSide.South) modules = new[] { style.SouthCap };
            var reservations = endpointReservations ?? RunReservations(style, run, partition);
            var repeat = modules == null || modules.Length == 0 ? 0 : modules[0]?.RepeatPixels ?? 0;
            var end = run.End;
            string runIssue = null;
            if (modules == null || modules.Length == 0) runIssue = run.Side + " wall modules are missing.";
            else foreach (var module in modules)
            {
                runIssue = CheckModule(module, run.Side.ToString(), run.Side == RoomSide.North);
                if (runIssue != null) break;
                if (module.RepeatPixels != repeat) { runIssue = run.Side + " requires sprites with one structural repeat width."; break; }
            }
            if (runIssue == null && partition && repeat != 32) runIssue = "Placeholder partitions require a one-cell cap repeat.";
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
                var fit = CheckFootprint(modules[n % modules.Length], origin, result.Geometry.Cells, floorInset ?? style.FloorEdgeInsetPixels);
                if (fit != null)
                {
                    var location = Vector2Int.FloorToInt(origin);
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.VisualFit, run.Side.ToString(), fit, true, location, location));
                    continue;
                }
                Add(result, partition ? "Partition" + run.Side : run.Side.ToString(), modules[n % modules.Length], origin, ownerId);
            }
        }

        private static string CheckFootprint(RoomStyleDefinition.Module module, Vector2 origin, Vector2Int[] cells, int inset)
        {
            if (!module.HasFootprint) return null;
            if (inset < 0 || inset > 15) return "Floor edge inset must be between 0 and 15 pixels, preserving a walkable cell core.";
            if (module.FootprintsPixels == null || module.FootprintsPixels.Length == 0) return "Declare at least one visual footprint rectangle.";
            foreach (var rect in module.FootprintsPixels)
            {
                if (rect.width <= 0 || rect.height <= 0 || rect.x < 0 || rect.y < 0 ||
                    (long)rect.x + rect.width > module.CanvasPixels.x || (long)rect.y + rect.height > module.CanvasPixels.y)
                    return "Visual footprint rectangles must be positive and inside the sprite canvas.";
                var min = origin + ((Vector2)rect.position - module.AnchorPixels + module.OffsetPixels) / 32f;
                var max = min + (Vector2)rect.size / 32f;
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
                    var min = (Vector2)rect.position - binding.Artwork.AnchorPixels + binding.Artwork.OffsetPixels;
                    var max = min + (Vector2)rect.size;
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

        private static string CheckFloor(TileBase floor)
        {
            if (!(floor is Tile tile) || tile.sprite == null) return "Floor requires a native sprite Tile.";
            var issue = CheckSprite(tile.sprite, new Vector2Int(32, 32), "Floor");
            return issue ?? (tile.transform != Matrix4x4.identity ? "Floor tile transform must be identity." : null);
        }

        private static string CheckModule(RoomStyleDefinition.Module module, string role, bool north)
        {
            if (module == null) return role + " module is missing.";
            if (north ? module.CanvasPixels != new Vector2Int(32, 64) && module.CanvasPixels != new Vector2Int(64, 64)
                : module.CanvasPixels != new Vector2Int(32, 32))
                return role + (north ? " requires 32x64 or 64x64 canvases." : " requires a 32x32 canvas.");
            var issue = CheckSprite(module.Sprite, module.CanvasPixels, role);
            if (issue != null) return issue;
            if (!Finite(module.AnchorPixels) || !Finite(module.OffsetPixels)) return role + " alignment must be finite.";
            if (north && module.RepeatPixels != 32 && module.RepeatPixels != 64) return "North structural repeat must be 32 or 64 pixels.";
            if (north && module.AnchorPixels.y != module.OffsetPixels.y) return "North wall bottom must align with its north boundary and extend northward.";
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

        private static void Add(RoomPlacement result, string role, RoomStyleDefinition.Module module, Vector2 origin, string ownerId = null)
        {
            var position = origin + (module.Sprite.pivot - module.AnchorPixels + module.OffsetPixels) / 32f;
            result.Sprites.Add(new RoomSpritePlacement(role, module.Sprite, position, ownerId));
        }
    }
}
