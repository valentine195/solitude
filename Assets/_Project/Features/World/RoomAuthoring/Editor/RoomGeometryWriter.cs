using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomGeometryWriter
    {
        public static void Clear(ShipRoomAuthoring room)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(room);
            if (issue != null) throw new System.InvalidOperationException("Cannot clear room output: " + issue);
            var root = room.GeneratedOutput.transform;
            for (var i = root.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }

        public static void Apply(ShipRoomAuthoring room, RoomPlacement placement)
        {
            Clear(room);
            if (placement.Geometry.Cells.Length == 0) return;
            var floor = new GameObject("Generated Floor", typeof(Tilemap), typeof(TilemapRenderer));
            floor.transform.SetParent(room.GeneratedOutput.transform, false);
            var map = floor.GetComponent<Tilemap>();
            map.tileAnchor = ((Tile)room.Style.FloorTile).sprite.pivot / 32f;
            foreach (var cell in placement.Geometry.Cells) map.SetTile(new Vector3Int(cell.x, cell.y, 0), room.Style.FloorTile);
            floor.GetComponent<TilemapRenderer>().sortingOrder = 0;
            Undo.RegisterCreatedObjectUndo(floor, "Rebuild Room");
            foreach (var item in placement.Sprites)
            {
                var go = new GameObject(item.Role, typeof(SpriteRenderer));
                go.transform.SetParent(room.GeneratedOutput.transform, false);
                go.transform.localPosition = item.Position;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = item.Sprite;
                renderer.sortingOrder = 1;
                Undo.RegisterCreatedObjectUndo(go, "Rebuild Room");
            }
        }
    }
}
