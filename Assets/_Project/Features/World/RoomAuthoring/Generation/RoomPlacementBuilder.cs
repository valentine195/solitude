using System;
using System.Collections.Generic;
using UnityEngine;

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
        public readonly List<RoomSpritePlacement> Sprites = new List<RoomSpritePlacement>();
    }

    public static class RoomPlacementBuilder
    {
        public static bool TryBuild(RoomGeometry geometry, RoomStyleDefinition style, out RoomPlacement result, out string issue)
        {
            result = new RoomPlacement { Geometry = geometry };
            issue = null;
            if (geometry.Cells.Length == 0) return true;
            if (!geometry.IsRectangle) { issue = "Only one filled rectangle is supported."; return false; }
            if (style == null || style.FloorTile == null) { issue = "Assign a room style with a floor tile."; return false; }
            if (style.NorthWalls == null || style.NorthWalls.Length == 0) { issue = "North wall modules are missing."; return false; }
            var bounds = geometry.Bounds;
            foreach (var run in geometry.Runs)
            {
                var modules = run.Side == RoomSide.North ? style.NorthWalls : new[] { run.Side == RoomSide.West ? style.WestCap : run.Side == RoomSide.East ? style.EastCap : style.SouthCap };
                var reservations = run.Side == RoomSide.North ? style.NorthReservationsPixels : run.Side == RoomSide.West ? style.WestReservationsPixels : run.Side == RoomSide.East ? style.EastReservationsPixels : style.SouthReservationsPixels;
                var repeat = modules[0]?.RepeatPixels ?? 0;
                if (repeat <= 0 || reservations.x < 0 || reservations.y < 0) { issue = run.Side + " has invalid repeat/reservations."; return false; }
                foreach (var module in modules)
                    if (module == null || module.Sprite == null || module.RepeatPixels != repeat)
                    { issue = run.Side + " requires sprites with one structural repeat width."; return false; }
                var span = (long)run.Length * 32 - reservations.x - reservations.y;
                if (span < 0 || span % repeat != 0) { issue = run.Side + " span is not exactly coverable after corner reservations."; return false; }
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
                if (corners[n] == null || corners[n].Sprite == null) { issue = names[n] + " corner sprite is missing."; return false; }
                Add(result, names[n], corners[n], origins[n]);
            }
            return true;
        }

        private static void Add(RoomPlacement result, string role, RoomStyleDefinition.Module module, Vector2 origin)
        {
            var position = origin + (module.Sprite.pivot - module.AnchorPixels + module.OffsetPixels) / 32f;
            result.Sprites.Add(new RoomSpritePlacement(role, module.Sprite, position));
        }
    }
}
