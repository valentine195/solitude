using System.Collections.Generic;
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

        // Compare live output rather than a serialized hash: manual edits and Undo remain visible.
        private static bool Matches(ShipRoomAuthoring room, RoomPlacement placement)
        {
            var root = room.GeneratedOutput.transform;
            if (placement.Geometry.Cells.Length == 0) return root.childCount == 0;
            if (root.childCount != placement.Sprites.Count + 1) return false;
            var floor = root.GetChild(0);
            var map = floor.GetComponent<Tilemap>();
            var floorRenderer = floor.GetComponent<TilemapRenderer>();
            if (map == null || floorRenderer == null || floor.name != "Generated Floor" ||
                floor.localPosition != Vector3.zero || floor.localRotation != Quaternion.identity || floor.localScale != Vector3.one ||
                floorRenderer.sortingOrder != 0 || map.tileAnchor != (Vector3)(((Tile)room.Style.FloorTile).sprite.pivot / 32f)) return false;
            int count = 0;
            foreach (var position in map.cellBounds.allPositionsWithin) if (map.HasTile(position)) count++;
            if (count != placement.Geometry.Cells.Length) return false;
            foreach (var cell in placement.Geometry.Cells)
                if (map.GetTile(new Vector3Int(cell.x, cell.y, 0)) != room.Style.FloorTile) return false;
            for (int i = 0; i < placement.Sprites.Count; i++)
            {
                var item = placement.Sprites[i]; var child = root.GetChild(i + 1);
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null || child.name != item.Role || child.localPosition != item.Position ||
                    child.localRotation != Quaternion.identity || child.localScale != Vector3.one ||
                    renderer.sprite != item.Sprite || renderer.sortingOrder != 1) return false;
            }
            return true;
        }

        public static void Apply(ShipRoomAuthoring room, RoomPlacement placement)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(room);
            if (issue != null) throw new System.InvalidOperationException("Cannot write room output: " + issue);
            if (Matches(room, placement)) return;
            if (placement.Geometry.Cells.Length == 0) { Clear(room); return; }
            var root = room.GeneratedOutput.transform;
            // Record only changed Tilemap data and child order. New/deleted objects
            // retain their explicit lifecycle registrations; unchanged walls need no copy.
            var previous = new List<Transform>(root.childCount);
            var retained = new HashSet<Transform>();
            var ordered = new List<Transform>(placement.Sprites.Count + 1);
            var sprites = new Dictionary<(string role, Sprite sprite, Vector3 position), Queue<Transform>>();
            Tilemap map = null;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i); previous.Add(child);
                if (child.localRotation != Quaternion.identity || child.localScale != Vector3.one) continue;
                if (map == null && child.name == "Generated Floor" && child.localPosition == Vector3.zero &&
                    child.TryGetComponent<Tilemap>(out var candidate) && child.TryGetComponent<TilemapRenderer>(out var floorRenderer) &&
                    floorRenderer.sortingOrder == 0)
                { map = candidate; retained.Add(child); continue; }
                if (!child.TryGetComponent<SpriteRenderer>(out var renderer) || renderer.sortingOrder != 1) continue;
                var key = (child.name, renderer.sprite, child.localPosition);
                if (!sprites.TryGetValue(key, out var matches)) sprites.Add(key, matches = new Queue<Transform>());
                matches.Enqueue(child);
            }
            if (map == null)
            {
                var floor = new GameObject("Generated Floor", typeof(Tilemap), typeof(TilemapRenderer));
                floor.transform.SetParent(root, false); map = floor.GetComponent<Tilemap>();
                Undo.RegisterCreatedObjectUndo(floor, "Rebuild Room");
            }
            ordered.Add(map.transform);
            UpdateFloor(map, room, placement.Geometry.Cells);
            foreach (var item in placement.Sprites)
            {
                var key = (item.Role, item.Sprite, item.Position);
                Transform child;
                if (sprites.TryGetValue(key, out var matches) && matches.Count > 0)
                { child = matches.Dequeue(); retained.Add(child); }
                else
                {
                    var go = new GameObject(item.Role, typeof(SpriteRenderer));
                    child = go.transform; child.SetParent(root, false); child.localPosition = item.Position;
                    var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = item.Sprite; renderer.sortingOrder = 1;
                    Undo.RegisterCreatedObjectUndo(go, "Rebuild Room");
                }
                ordered.Add(child);
            }
            foreach (var child in previous)
                if (!retained.Contains(child)) Undo.DestroyObjectImmediate(child.gameObject);
            Undo.RegisterChildrenOrderUndo(root, "Rebuild Room Order");
            for (int i = 0; i < ordered.Count; i++) ordered[i].SetSiblingIndex(i);
        }

        private static void UpdateFloor(Tilemap map, ShipRoomAuthoring room, Vector2Int[] cells)
        {
            var desired = new HashSet<Vector3Int>();
            var positions = new List<Vector3Int>(); var tiles = new List<TileBase>();
            foreach (var cell in cells)
            {
                var position = new Vector3Int(cell.x, cell.y, 0); desired.Add(position);
                if (map.GetTile(position) == room.Style.FloorTile) continue;
                positions.Add(position); tiles.Add(room.Style.FloorTile);
            }
            foreach (var position in map.cellBounds.allPositionsWithin)
                if (!desired.Contains(position) && map.HasTile(position)) { positions.Add(position); tiles.Add(null); }
            var anchor = (Vector3)(((Tile)room.Style.FloorTile).sprite.pivot / 32f);
            if (positions.Count == 0 && map.tileAnchor == anchor) return;
            Undo.RegisterCompleteObjectUndo(map, "Rebuild Room Floor");
            map.tileAnchor = anchor;
            if (positions.Count > 0) map.SetTiles(positions.ToArray(), tiles.ToArray());
        }
    }
}
