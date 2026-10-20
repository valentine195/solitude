using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.World.RoomAuthoring
{
    public readonly struct RoomSpritePlacement
    {
        public readonly string Role;
        public readonly Sprite Sprite;
        public readonly Vector3 Position;
        public RoomSpritePlacement(string role, Sprite sprite, Vector3 position)
        { Role = role; Sprite = sprite; Position = position; }
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
            result = new RoomPlacement { Geometry = geometry };
            issue = null;
            if (geometry.Cells.Length == 0) return true;
            if (style == null)
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Style, "style", "Assign a room style."));
                issue = result.Issues[0].Reason;
                return false;
            }
            var floorIssue = CheckFloor(style.FloorTile);
            if (floorIssue == null) result.Floor = (Tile)style.FloorTile;
            else result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Floor, "floorTile", floorIssue));
            if (!geometry.IsRectangle)
            {
                result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Geometry, "occupancy", "Only one filled rectangle is supported in this intermediate preview."));
                issue = string.Join("\n", result.Issues.ConvertAll(i => i.Reason));
                return false;
            }
            var bounds = geometry.Bounds;
            foreach (var run in geometry.Runs)
            {
                var modules = run.Side == RoomSide.North ? style.NorthWalls : new[] { run.Side == RoomSide.West ? style.WestCap : run.Side == RoomSide.East ? style.EastCap : style.SouthCap };
                var reservations = run.Side == RoomSide.North ? style.NorthReservationsPixels : run.Side == RoomSide.West ? style.WestReservationsPixels : run.Side == RoomSide.East ? style.EastReservationsPixels : style.SouthReservationsPixels;
                var repeat = modules == null || modules.Length == 0 ? 0 : modules[0]?.RepeatPixels ?? 0;
                var end = run.Start + (run.Side == RoomSide.North || run.Side == RoomSide.South ? Vector2Int.right : Vector2Int.up) * run.Length;
                string runIssue = null;
                if (modules == null || modules.Length == 0) runIssue = run.Side + " wall modules are missing.";
                else foreach (var module in modules)
                {
                    runIssue = CheckModule(module, run.Side.ToString(), run.Side == RoomSide.North);
                    if (runIssue != null) break;
                    if (module.RepeatPixels != repeat) { runIssue = run.Side + " requires sprites with one structural repeat width."; break; }
                }
                if (runIssue != null)
                {
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Asset, run.Side.ToString(), runIssue, true, run.Start, end));
                    continue;
                }
                var span = (long)run.Length * 32 - reservations.x - reservations.y;
                if (repeat <= 0 || reservations.x < 0 || reservations.y < 0 || span < 0 || span % repeat != 0)
                {
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Span, run.Side.ToString(),
                        run.Side + " span is not exactly coverable after corner reservations (check repeat/reservations).", true, run.Start, end));
                    continue;
                }
                var horizontal = run.Side == RoomSide.North || run.Side == RoomSide.South;
                for (var n = 0; n < span / repeat; n++)
                {
                    var origin = (Vector2)run.Start + (horizontal ? Vector2.right : Vector2.up) * ((reservations.x + n * repeat) / 32f);
                    Add(result, run.Side.ToString(), modules[n % modules.Length], origin);
                }
            }
            var corners = new[] { style.Southwest, style.Southeast, style.Northwest, style.Northeast };
            var origins = new[] { new Vector2(bounds.xMin, bounds.yMin), new Vector2(bounds.xMax, bounds.yMin), new Vector2(bounds.xMin, bounds.yMax), new Vector2(bounds.xMax, bounds.yMax) };
            var names = new[] { "Southwest", "Southeast", "Northwest", "Northeast" };
            for (var n = 0; n < 4; n++)
            {
                var cornerIssue = CheckModule(corners[n], names[n] + " corner", false);
                if (cornerIssue != null)
                {
                    var at = Vector2Int.RoundToInt(origins[n]);
                    result.Issues.Add(new RoomGenerationIssue(RoomIssueKind.Asset, names[n], cornerIssue, true, at, at));
                    continue;
                }
                Add(result, names[n], corners[n], origins[n]);
            }
            issue = result.Issues.Count == 0 ? null : string.Join("\n", result.Issues.ConvertAll(i => i.Reason));
            return issue == null;
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

        private static void Add(RoomPlacement result, string role, RoomStyleDefinition.Module module, Vector2 origin)
        {
            var position = origin + (module.Sprite.pivot - module.AnchorPixels + module.OffsetPixels) / 32f;
            result.Sprites.Add(new RoomSpritePlacement(role, module.Sprite, position));
        }
    }
}
