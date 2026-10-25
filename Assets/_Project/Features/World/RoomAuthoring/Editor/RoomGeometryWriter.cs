using System.Linq;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SOLITUDE.Editor.RoomAuthoring
{
    public static class RoomGeometryWriter
    {
        public static string GetEmptyCleanupIssue(ShipLayoutAuthoring layout)
        {
            foreach (var room in layout.Spaces)
            {
                var output = room.GeneratedOutput;
                if (output == null) continue;
                if (output.GetComponents<Component>().Any(c => c == null ||
                    (c.GetType() != typeof(Transform) && c.GetType() != typeof(RoomGeneratedOutput))))
                    return "Output root for " + room.RoomId + " contains unrecognized components; move authored content outside generated output before cleanup.";
                // Compare output independently from authored input: erasure legitimately makes the input stale.
                if (output.transform.childCount != 0 && (string.IsNullOrEmpty(room.GenerationResult?.OutputFingerprint) ||
                    room.GenerationResult.OutputFingerprint != RoomAuthoringValidator.OutputFingerprint(room)))
                    return "Output for " + room.RoomId + " changed since its verified generation; inspect and preserve added/modified content before empty cleanup.";
            }
            return null;
        }

        public static void Clear(ShipRoomAuthoring room)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(room);
            if (issue != null) throw new System.InvalidOperationException("Cannot clear room output: " + issue);
            var root = room.GeneratedOutput.transform;
            for (var i = root.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }

        public static void Apply(ShipRoomAuthoring room, RoomPlacement placement, RoomFloorPanelOutput panels = null)
        {
            foreach (var sprite in placement.Sprites)
                if (sprite.OwnerId != null && sprite.OwnerId != room.RoomId)
                    throw new System.InvalidOperationException("Resolved architecture belongs to a different space; output was not changed.");
            Clear(room);
            if (placement.Geometry.Cells.Length == 0) return;
            if (placement.Floor != null)
            {
                var floor = new GameObject("Generated Floor", typeof(Tilemap), typeof(TilemapRenderer));
                floor.transform.SetParent(room.GeneratedOutput.transform, false);
                var map = floor.GetComponent<Tilemap>();
                map.tileAnchor = placement.FloorAnchor;
                foreach (var cell in placement.Geometry.Cells) map.SetTile(new Vector3Int(cell.x, cell.y, 0), placement.Floor);
                var renderer = floor.GetComponent<TilemapRenderer>();
                renderer.sortingOrder = placement.Rendering?.FloorOrder ?? 0;
                if (placement.Rendering != null)
                {
                    renderer.sortingLayerName = placement.Rendering.SortingLayerName;
                    if (placement.Rendering.FloorMaterial != null) renderer.sharedMaterial = placement.Rendering.FloorMaterial;
                }
                Undo.RegisterCreatedObjectUndo(floor, "Rebuild Room");
            }
            if (panels != null)
            {
                var offset = room.Layout == null ? Vector2Int.zero : RoomAuthoringValidator.LayoutOffset(room.Layout, room);
                foreach (var chunk in panels.Chunks)
                {
                    var go = new GameObject("Generated Floor Panels", typeof(SpriteRenderer));
                    go.transform.SetParent(room.GeneratedOutput.transform, false);
                    // Chunk coordinates are layout pixels; children use the owning space's local frame.
                    go.transform.localPosition = new Vector3(chunk.Origin.x / 32f - offset.x, chunk.Origin.y / 32f - offset.y, 0);
                    var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = chunk.Sprite;
                    renderer.sortingOrder = placement.Rendering?.DetailOrder ?? 1;
                    renderer.sortingLayerName = placement.Rendering?.SortingLayerName ?? "Default";
                    if (placement.Rendering?.FloorMaterial != null) renderer.sharedMaterial = placement.Rendering.FloorMaterial;
                    Undo.RegisterCreatedObjectUndo(go, "Rebuild Room");
                }
            }
            foreach (var item in placement.Sprites)
            {
                var go = new GameObject(item.Role, typeof(SpriteRenderer));
                go.transform.SetParent(room.GeneratedOutput.transform, false);
                go.transform.localPosition = item.Position;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = item.Sprite;
                renderer.sortingOrder = item.Rendering?.ArchitectureOrder ?? 1;
                if (item.Rendering != null)
                {
                    renderer.sortingLayerName = item.Rendering.SortingLayerName;
                    if (item.Rendering.ArchitectureMaterial != null) renderer.sharedMaterial = item.Rendering.ArchitectureMaterial;
                }
                Undo.RegisterCreatedObjectUndo(go, "Rebuild Room");
            }
        }
    }
}
