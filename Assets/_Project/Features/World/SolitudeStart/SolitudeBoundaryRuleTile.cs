using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.World.SolitudeStart
{
    /// <summary>Rectangle-boundary RuleTile. Interior bounds determine the direction;
    /// cardinal neighbors determine whether a perimeter cell still belongs to the run.</summary>
    [CreateAssetMenu(menuName = "SOLITUDE/Tiles/Room Boundary Rule")]
    public sealed class SolitudeBoundaryRuleTile : RuleTile
    {
        public RectInt interior = new RectInt(1, 1, 8, 6);
        public Sprite west, east, south, northWest, northEast, southWest, southEast;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData data)
        {
            int left = interior.xMin - 1, right = interior.xMax;
            int bottom = interior.yMin - 1, top = interior.yMax + 1;
            bool onLeft = position.x == left, onRight = position.x == right;
            bool onBottom = position.y == bottom, onTop = position.y == top;
            Sprite chosen = null;
            if (onLeft && onTop) chosen = northWest;
            else if (onRight && onTop) chosen = northEast;
            else if (onLeft && onBottom) chosen = southWest;
            else if (onRight && onBottom) chosen = southEast;
            else if (onLeft && position.y > bottom && position.y < top) chosen = west;
            else if (onRight && position.y > bottom && position.y < top) chosen = east;
            else if (onBottom && position.x > left && position.x < right) chosen = south;
            // Require the connected neighbor in the run. Changing occupancy refreshes
            // the owning rule tile and exposes accidental gaps during editing.
            if (chosen != null && !(onTop || onBottom))
            {
                var neighbor = tilemap.GetTile(position + Vector3Int.up);
                var below = tilemap.GetTile(position + Vector3Int.down);
                if (neighbor != this && below != this) chosen = null;
            }
            data.sprite = chosen;
            data.color = Color.white;
            data.transform = Matrix4x4.identity;
            data.gameObject = null;
            data.colliderType = Tile.ColliderType.None;
        }
    }
}
