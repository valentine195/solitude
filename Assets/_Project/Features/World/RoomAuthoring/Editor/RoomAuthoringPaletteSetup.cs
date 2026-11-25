using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
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

        public static GameObject PrepareStylePalette(RoomStyleDefinition style, string folder)
        {
            if (style == null || string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/_Project/", StringComparison.Ordinal))
                throw new ArgumentException("Choose a style and a first-party palette folder.");
            var path = folder + "/ArchitecturePalette.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing; // Preserve a designer's palette arrangement and asset GUIDs.
            if (System.IO.Directory.Exists(folder) || System.IO.File.Exists(folder) || AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("Use a fresh palette folder; existing content is preserved.");
            EnsureFolder(folder);
            GridPaletteUtility.CreateNewPalette(folder, "ArchitecturePalette", GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var map = contents.GetComponentInChildren<Tilemap>();
                if (style.FloorTile != null) map.SetTile(Vector3Int.zero, style.FloorTile);
                var modules = (style.NorthWalls ?? Array.Empty<RoomStyleDefinition.Module>())
                    .Concat(new[] { style.WestCap, style.EastCap, style.SouthCap }).ToArray();
                for (var i = 0; i < modules.Length; i++)
                {
                    if (modules[i]?.Sprite == null) continue;
                    var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = modules[i].Sprite;
                    AssetDatabase.CreateAsset(tile, folder + "/Artwork" + i + ".asset");
                    map.SetTile(new Vector3Int(i * 4, 4, 0), tile);
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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

    public static class RoomAuthoringStyleControls
    {
        public static bool AssignSpaceStyle(ShipRoomAuthoring room, RoomStyleDefinition selected, out string issue) =>
            Assign(room, room, () => room.Style = selected, out issue);

        public static bool AssignLayoutStyle(ShipLayoutAuthoring layout, RoomStyleDefinition selected, out string issue) =>
            Assign(layout, layout.Spaces.FirstOrDefault(), () => layout.DefaultStyle = selected, out issue);

        private static bool Assign(Object owner, ShipRoomAuthoring room, Action change, out string issue)
        {
            issue = room == null ? "Create a space first." : room.Layout == null ? RoomAuthoringOperations.GetStyleReplacementIssue(room) :
                RoomAuthoringOperations.GetStyleReplacementIssue(room.Layout);
            if (issue != null) return false;
            if (room.Layout != null && RoomAuthoringAssetRefresh.HasPendingPanelDraft(room.Layout))
            {
                issue = "Layout " + room.Layout.LayoutId + ": Apply or cancel its floor panel draft before changing architectural style.";
                return false;
            }
            if (RoomAuthoringOperations.HasActiveEdit) { issue = "Finish or cancel the active paint stroke first."; return false; }
            using var edit = RoomAuthoringOperations.BeginEdit(room, "Change Architectural Style");
            var group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(owner, "Change Architectural Style");
                change(); EditorUtility.SetDirty(owner);
                RoomAuthoringOperations.Rebuild(room);
                return true;
            }
            catch { edit.CloseAfterUndo(); Undo.RevertAllDownToGroup(group); throw; }
        }

        public static VisualElement ForSpace(ShipRoomAuthoring room) => Create(room, room.Layout);
        public static VisualElement ForLayout(ShipLayoutAuthoring layout) => Create(null, layout);

        private static VisualElement Create(ShipRoomAuthoring room, ShipLayoutAuthoring layout)
        {
            var root = new VisualElement { name = "architecturalStyleControls" };
            var selected = new ObjectField(room == null ? "Layout style / fallback" : "Space style (None = inherit)")
                { objectType = typeof(RoomStyleDefinition), allowSceneObjects = false, name = "assignedStyle" };
            var source = new Label { name = "styleSource" };
            var feedback = new HelpBox("", HelpBoxMessageType.Info) { name = "artworkRefreshState" };
            var fallbacks = new Label { name = "fallbackRoles" };
            var bindings = new Foldout { text = "Artwork bindings", value = false, name = "artworkBindings" };
            root.Add(selected); root.Add(source); root.Add(feedback); root.Add(fallbacks); root.Add(bindings);
            root.Add(new HelpBox("Painted cells define the layout. Bound Tile/Sprite edits update its artwork. Rearranging this palette or replacing a palette entry does not reassign architectural roles.", HelpBoxMessageType.Info));
            string key = null;
            string editIssue = null;
            void Refresh()
            {
                if (room == null && layout == null || room != null && !room.gameObject.scene.IsValid()) return;
                var data = new SerializedObject(room == null ? (Object)layout : room); data.Update();
                var assigned = (RoomStyleDefinition)data.FindProperty(room == null ? "defaultStyle" : "style").objectReferenceValue;
                selected.SetValueWithoutNotify(assigned);
                var effective = room == null ? layout.DefaultStyle : room.Style;
                source.text = effective == null ? "No effective style. Choose one or set the layout default." :
                    (room == null ? "Layout default and compatible fallback: " : assigned == null ? "Inherited from layout: " : "Space override: ") + effective.DisplayName;
                var pending = layout != null && RoomAuthoringAssetRefresh.IsPending(layout);
                var blocked = layout == null ? null : RoomAuthoringAssetRefresh.GetIssue(layout);
                feedback.text = editIssue ?? blocked ?? (pending ? "Artwork update queued. Active painting finishes or cancels before refresh." :
                    layout == null ? "Adopt into a layout for automatic artwork refresh. Independent rooms use Rebuild Room after asset edits." :
                    "Bound artwork refresh is automatic for loaded layouts. External source files are not restored by scene Undo.");
                feedback.messageType = editIssue == null && blocked == null ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                var result = layout == null ? room.GenerationResult : layout.GenerationResult;
                var used = (result?.StyleResolutions ?? Array.Empty<RoomStyleResolution>()).Where(r => r.UsedFallback &&
                    (room == null || r.OwnerId == room.RoomId)).ToArray();
                fallbacks.text = used.Length == 0 ? "No fallback roles used by the last build." : "Fallback used: " +
                    string.Join("\n", used.Take(4).Select(r => r.Role + " at " + r.Start)) + (used.Length > 4 ? " (+" + (used.Length - 4) + " more)" : "");
                var next = effective == null ? "" : EditorJsonUtility.ToJson(effective);
                if (next == key) return;
                key = next; bindings.Clear();
                if (effective == null) return;
                var style = effective;
                bindings.Add(new Button(() => Selection.activeObject = style) { text = "Select Style Asset" });
                var floor = new ObjectField("Bound floor Tile") { objectType = typeof(TileBase), allowSceneObjects = false, value = style.FloorTile, name = "boundFloorTile" };
                floor.RegisterValueChangedCallback(e =>
                {
                    RoomAuthoringOperations.EditStyleBindings(style, value => value.FloorTile = (TileBase)e.newValue, out var issue);
                    editIssue = issue;
                    if (issue != null) { floor.SetValueWithoutNotify(style.FloorTile); }
                    key = null; Refresh();
                });
                bindings.Add(floor);
                void Artwork(string label, RoomStyleDefinition.Module module, string name)
                {
                    if (module == null) return;
                    var field = new ObjectField(label) { objectType = typeof(Sprite), allowSceneObjects = false, value = module.Sprite, name = name };
                    field.RegisterValueChangedCallback(e =>
                    {
                        RoomAuthoringOperations.EditStyleBindings(style, value => module.Sprite = (Sprite)e.newValue, out var issue);
                        editIssue = issue;
                        if (issue != null) { field.SetValueWithoutNotify(module.Sprite); }
                        key = null; Refresh();
                    });
                    bindings.Add(field);
                }
                for (var i = 0; i < (style.NorthWalls?.Length ?? 0); i++) Artwork("North wall " + (i + 1), style.NorthWalls[i], "northArtwork" + i);
                Artwork("West cap", style.WestCap, "westArtwork"); Artwork("East cap", style.EastCap, "eastArtwork"); Artwork("South cap", style.SouthCap, "southArtwork");
                bindings.Add(new HelpBox("Replacements use the style's declared canvas, PPU, anchor and repeat. Select the style asset to configure different footprints, corners, junctions or sorting.", HelpBoxMessageType.Info));
                var transitions = new Foldout { text = "Compatible transition styles", value = false, name = "compatibleTransitions" };
                foreach (var compatible in (style.CompatibleTransitions ?? Array.Empty<RoomStyleDefinition>()).Where(s => s != null).Distinct())
                {
                    var partner = compatible;
                    transitions.Add(new Button(() =>
                    {
                        RoomAuthoringOperations.EditStyleBindings(style, value => value.CompatibleTransitions = (value.CompatibleTransitions ?? Array.Empty<RoomStyleDefinition>()).Where(s => s != partner).ToArray(), out editIssue);
                        key = null; Refresh();
                    }) { text = "Remove compatibility: " + partner.DisplayName });
                }
                var add = new ObjectField("Add compatible style") { objectType = typeof(RoomStyleDefinition), allowSceneObjects = false, name = "addCompatibleStyle" };
                add.RegisterValueChangedCallback(e =>
                {
                    if (e.newValue is RoomStyleDefinition partner && partner != style)
                        RoomAuthoringOperations.EditStyleBindings(style, value => value.CompatibleTransitions = (value.CompatibleTransitions ?? Array.Empty<RoomStyleDefinition>()).Append(partner).Distinct().ToArray(), out editIssue);
                    key = null; Refresh();
                });
                transitions.Add(add); bindings.Add(transitions);
            }
            selected.RegisterValueChangedCallback(e =>
            {
                string issue;
                var success = room == null ? AssignLayoutStyle(layout, (RoomStyleDefinition)e.newValue, out issue) :
                    AssignSpaceStyle(room, (RoomStyleDefinition)e.newValue, out issue);
                editIssue = success ? null : issue;
                key = null; Refresh();
            });
            root.schedule.Execute(Refresh).Every(300); Refresh();
            return root;
        }
    }

}
