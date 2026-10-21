using System;
using SOLITUDE.World.RoomAuthoring;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SOLITUDE.Editor.RoomAuthoring
{
    [InitializeOnLoad]
    public static class RoomAuthoringPaletteSetup
    {
        public const string PaletteFolder = "Assets/_Project/Art/Tiles/Palettes/RoomAuthoring";
        public const string PalettePath = PaletteFolder + "/RoomOccupancy.prefab";
        public const string MarkerPath = PaletteFolder + "/RoomOccupancy.asset";
        public const string BrushPath = "Assets/_Project/Features/World/RoomAuthoring/Editor/Brushes/ShipRoomBrush.asset";
        private static GameObject editingMask;

        static RoomAuthoringPaletteSetup()
        {
            GridPaintingState.scenePaintTargetChanged += ActivateTarget;
            GridPaintingState.brushChanged += brush => EndEditing();
            AssemblyReloadEvents.beforeAssemblyReload += EndEditing;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingEditMode) EndEditing(); };
        }

        [MenuItem("SOLITUDE/Room Authoring/Prepare Palette")]
        public static void EnsureAssets()
        {
            EnsureFolder(PaletteFolder);
            EnsureFolder(System.IO.Path.GetDirectoryName(BrushPath).Replace('\\', '/'));
            var marker = AssetDatabase.LoadAssetAtPath<Tile>(MarkerPath);
            if (marker == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(MarkerPath) != null) throw new InvalidOperationException("Existing occupancy asset is not a Tile; preserve and resolve it before setup.");
                marker = ScriptableObject.CreateInstance<Tile>(); marker.name = "RoomOccupancy";
                AssetDatabase.CreateAsset(marker, MarkerPath);
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "OccupancyMarker", filterMode = FilterMode.Point };
                for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
                    texture.SetPixel(x, y, x < 2 || y < 2 || x > 29 || y > 29 ? Color.white : new Color(.1f, .65f, .9f, .55f));
                texture.Apply();
                var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
                sprite.name = "OccupancyMarker";
                AssetDatabase.AddObjectToAsset(texture, marker); AssetDatabase.AddObjectToAsset(sprite, marker);
                marker.sprite = sprite; EditorUtility.SetDirty(marker);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(PalettePath) != null) throw new InvalidOperationException("Existing palette asset is incompatible; setup will not replace it.");
                GridPaletteUtility.CreateNewPalette(PaletteFolder, "RoomOccupancy", GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Automatic, new Vector3(1, 1, 0), GridLayout.CellSwizzle.XYZ);
                var contents = PrefabUtility.LoadPrefabContents(PalettePath);
                try
                {
                    contents.GetComponentInChildren<Tilemap>().SetTile(Vector3Int.zero, marker);
                    PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            var brush = AssetDatabase.LoadAssetAtPath<ShipRoomBrush>(BrushPath);
            if (brush == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(BrushPath) != null) throw new InvalidOperationException("Existing brush asset is incompatible; setup will not replace it.");
                brush = ScriptableObject.CreateInstance<ShipRoomBrush>(); brush.OccupancyMarker = marker;
                AssetDatabase.CreateAsset(brush, BrushPath);
            }
            AssetDatabase.SaveAssets();
        }

        public static void EditRoom(ShipRoomAuthoring room)
        {
            var issue = RoomAuthoringOperations.GetSafetyIssue(room, allowMissingOutput: true);
            if (issue != null || !room.gameObject.activeInHierarchy) throw new InvalidOperationException(issue ?? "Activate the room before painting.");
            EnsureAssets();
            GridPaintingState.gridBrush = AssetDatabase.LoadAssetAtPath<ShipRoomBrush>(BrushPath);
            GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            GridPaintingState.scenePaintTarget = room.Occupancy.gameObject;
            ActivateTarget(room.Occupancy.gameObject);
            Selection.activeGameObject = room.Occupancy.gameObject;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
        }

        public static void ActivateTarget(GameObject target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !(GridPaintingState.gridBrush is ShipRoomBrush)) return;
            var grid = target == null ? null : target.GetComponentInParent<Grid>();
            if (!ShipRoomBrush.TryGetRoom(grid, target, out _)) { EndEditing(); return; }
            if (editingMask != null && editingMask != target) EndEditing();
            editingMask = target;
            if (!editingMask.activeSelf) editingMask.SetActive(true);
        }

        public static void EndEditing()
        {
            RoomAuthoringOperations.CancelActiveEdit();
            if (editingMask != null) editingMask.SetActive(false);
            editingMask = null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
