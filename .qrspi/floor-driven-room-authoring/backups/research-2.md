# QRSPI Research: floor-driven-room-authoring

## Answers

### What existing room, floor, wall, and door authoring behavior is present, and which parts already separate authored layout from generated visuals?

`SolitudeStartBuilder` creates a fixed 10×9 scene from an integer array. It creates separate editable Tilemaps named Floor, North Wall and Rule Boundaries, creates separate BoxCollider2D objects, and instantiates an animated door. It saves a newly created additive scene. There is no floor-observer geometry regeneration in this builder. The boundary Tile asset stores one rectangle and selects its sprite at evaluation time; it is independent of floor occupancy except for its configured rectangle, and checks vertical neighbors for side runs.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:55`, `:66`, `:75`, `:87`, `:95`, `:126`; `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:11`, `:16`, `:30`.

```csharp
rule.interior = new RectInt(1, 1, 8, 6);
Tilemap floor = NewMap(gridGO.transform, "Floor", 0);
Tilemap north = NewMap(gridGO.transform, "North Wall", 1);
Tilemap perimeter = NewMap(gridGO.transform, "Rule Boundaries", 2);
```

`OpeningSliceBuilder` copies Wakeup, creates an Opening Ship Structure root, places floors/walls/caps in separate Tilemaps, and places collision separately. Its Fill/North/Cap methods directly paint fixed coordinates. It refuses to overwrite an existing review scene. Layout and visuals are separated by Tilemap and asset references, but the current builder owns both coordinate placement and art selection; there is no separately serialized room-layout model in these two builders.

Files: `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:28`, `:37`, `:41`, `:106`.

```csharp
floor=Map(grid.transform,"Floor",0);
wall=Map(grid.transform,"North Wall Faces",20);
cap=Map(grid.transform,"Boundary Caps",2000);
```

### Which existing assets can supply the Standard room's floor, full-height north-wall modules, caps, and four structural corners, what uniquely identifies each asset, and what are their actual sprite dimensions, slicing, pixels per unit, pivots, transparent padding, and import settings?

The identity of “Standard room” is unresolved. Not found within `Assets`: case-insensitive `standard` in `.unity`, `.prefab`, `.asset`, or asset filenames. Not found within `Assets/_Project` or `Tools/ArtSources`: a room-specific Standard reference in `.cs`, `.json`, or `.md` searched with `standard` (excluding prompt/audit files). All searches excluded every `.qrspi/` tree. No asset can therefore be stated to be the Standard room's selected asset.

Current candidate assets are identifiable without that label. The following Aseprite sprites live in `Assets/_Project/Art/Tiles/Walls/Sprites/`; their metadata has GUID at line 2, importer PPU at line 39, and the animated sprite's name/pivot/rectangle/spriteID at lines 130–142. These are imported sprite rectangles, not assumed source canvas sizes.

| Asset basename | GUID | SpriteID | Imported rectangle | Pivot | PPU |
| --- | --- | --- | --- | --- | --- |
| wall_panel_1 | bcf45d671504241719de863b917f01df | c02c79046fd3942918b7afc7fae33fc4 | 128×238 | (.5,0) | 100 |
| wall_panel_dark_1 | 3fa52fae770d240c4821223aa44d41ec | 71ffb96997f4e4a4cb208bc9edb7bb21 | 128×238 | (.5,0) | 100 |
| wall_panel_dark_2 | c66473ef0f32543afaa897e488db4796 | bc7e51fd877854854bba1568d50ca401 | 128×256 | (.5,0) | 100 |
| wall_panel_intercom | dd70dcbb3f12c44f49c07dcba0b7dc4f | 2e5a33024b59c47c88c5415e85bf0e92 | 128×238 | (.5,0) | 100 |
| wall_panel_light_1 | d6d3ce27087004f03a2fa44d5c798b32 | 9411448de0565493494f275d9bc92a1c | 128×238 | (.5,0) | 100 |
| wall_panel_window_1 | aae0b9dc3ec1d4210a463b6871a2d6bc | 7b33686a8e085401a94ad9c377966807 | 128×238 | (.5,0) | 100 |
| wall_panel_window_2 | 5e502be47821749ff8deb4cc2dcdee81 | 5bc818d6e10b84110ab6e341eba92d4f | 128×256 | (.5,0) | 100 |
| wall_left | 10bde307f086149f2ad93005ab4fcc88 | eb7a6508c931947379e9701177d73b41 | 64×128 | (.5,0) | 100 |
| wall_right | 1c7c6966c436f4c05975abdaefe37786 | 5066917893392423793fa313156f02ea | 64×128 | (.5,0) | 100 |
| wall_bottom | d6181928af00d402c9c1efb7a8f88711 | e9763c44729b34d8ca96e064a0c25fff | 128×64 | (.5,0) | 100 |
| wall_top | 4808239ae040745ff9a78a24fc88a8ac | 15fbb64426b3c4b3f8158ff7c71324bd | 128×64 | (.5,-1.328125) | 100 |
| wall_left_corner | 194014c10089a4b70871a9259043af9a | 004a5c9de35cc4a14b0d88e62df54724 | 128×128 | (.5,0) | 100 |
| wall_right_corner | 5e1f450556e1043b6bca51a25803473a | 0f5255425b91748c58c52fc9a837e5b9 | 128×128 | (.5,0) | 100 |
| wall_bottom_left_corner | 91baf1d3c817345b3bdd3c02e50fbd42 | efe5b39346b124da3ae85777b8130f6c | 128×64 | (.5,.3125) | 100 |
| wall_bottom_right_corner | c38f93b97734b45d89bb554a41d60006 | 93584c4d993a54236b207b5103dae33b | 128×64 | (.5,.3125) | 100 |

Files: each `<basename>.aseprite.meta:2`, `:39`, `:130` in the directory above. Example:

```yaml
# wall_panel_1.aseprite.meta:130
- name: wall_panel_1
  originalName:
  pivot: {x: 0.5, y: 0}
  alignment: 9
# :138
    x: 4
    y: 4
    width: 128
    height: 238
  spriteID: c02c79046fd3942918b7afc7fae33fc4
```

The source wall-panel canvases are 128×256 (Aseprite binary header bytes 8–11). Decoding the RGBA cel data establishes an alpha-union bounding box of x=0..127,y=18..255 for panel_1/dark_1/intercom/light_1/window_1, versus x=0..127,y=0..255 for dark_2/window_2. Thus the former have 18 transparent canvas rows above their visible content, and the importer trims to 238. This is source alpha inspection, not a Unity render measurement. Aseprite files can have cel extents outside their canvas: bottom/cap source canvases are 128×43 while metadata sprite rectangles are 128×64. For example bottom-left's source alpha union spans y=-1..62. Consequently source canvas height alone does not establish imported bounds or padding. No rendered alpha/baseline measurement of the imported sprites was performed.

Files: the panel and bottom/corner `.aseprite` binaries named above (header/cel chunks); corresponding `.aseprite.meta:136` records imported bounds. Import settings in these candidate metadata files include no mipmaps (`:15`), tight sprite mesh (`:36`), transparency (`:45`), Point filter (`:56`), animation import mode and mosaic padding 4 (`:91`), and one animatedSpriteImportData entry (`:129`); they are not 32-pixel grid slices.

An older, separate floor/wall-palette family uses PNG sheets: Floor_Tilemap_0 has a 128×128 rectangle and centered pivot, 128 PPU, Bilinear filtering, no mipmaps, and default texture compression value 1. Its unique sprite identity is sheet GUID plus local file ID. The analogous Wall_Panel_Tilemap_0 tile points to sheet GUID `e3f9c874ba8b545c5a9d3a33b2203b0b`, local file ID `968023469991117803`; its slice is 128×128 at 128 PPU. These are not the full-height Aseprite sprites in the table.

Files: `Assets/_Project/Art/Tiles/Floors/Floor_Tilemap.png.meta:2`, `:12`, `:40`, `:54`, `:80`, `:118`; `Assets/_Project/Art/Tiles/Walls/Wall_Panel_Tilemap.png.meta:2`, `:40`, `:54`, `:118`; `Assets/_Project/Art/Tiles/Walls/Tilemap/Wall_Panel_Tilemap_0.asset:15`.

```yaml
# Floor_Tilemap.png.meta:123
width: 128
height: 128
alignment: 0
pivot: {x: 0.5, y: 0.5}
```

The opening-room atlas is another family: its builder configures 32×32 multiple sprites at 32 PPU, centered pivots, Point filtering, uncompressed texture and no mipmaps; door slices are 64×64, bottom-center pivot, nine frames. Its assets therefore cannot be substituted for the candidate full-height wall sprites by name alone.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:23`, `:32`, `:160`.

### Where do the project's ownership, placement, naming, and assembly conventions place feature-owned room data, geometry logic, editor tooling, tests, demonstration content, and authoring documentation?

The repository README places exclusive feature scripts/data/prefabs with their feature; Unity-independent rules/state in Domain, Unity-independent workflows in Application, shared runtime services in Systems, reusable tiles/palettes in Art/Tiles, tools in Editor, tests in Tests, and demonstrations in Scenes/Demos or a feature-local scene directory. It allows reference scenes in Art/SOLITUDE/ReferenceScenes. It specifies no unique mandatory authoring-documentation directory; existing feature/editor READMEs are examples. Art source/intermediate files belong in Tools/ArtSources and retained review outputs in work/Reviews.

Files: `README.md:15`, `:39`, `:51`, `:63`.

```text
Keep exclusive content with its owner.
Use PascalCase without spaces for new first-party folders, scripts, scenes, and prefabs.
```

Existing feature-local editor scripts use SOLITUDE.Editor.asmref. SOLITUDE.Editor is Editor-only and references Runtime/Domain/Application, Tilemap Extras and URP; Runtime references Domain/Application and Tilemap Extras. Edit Mode tests reference Editor/Runtime/Domain/Application. Safe-move rules preserve GUIDs, metadata and assembly boundaries; first-party imported content belongs beneath Assets/_Project.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SOLITUDE.Editor.asmref:1`; `Assets/_Project/Editor/SOLITUDE.Editor.asmdef:2`; `Assets/_Project/SOLITUDE.Runtime.asmdef:2`; `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef:2`; `README.md:11`, `:71`.

### Which existing scene-composition, movement, interaction, door, and rendering dependencies touch room geometry or Tilemaps, and what serialized references or behavior must remain intact?

SceneBindings serializes the player movement/interactor/inventory endpoints, modal endpoints, lockers, pickups, restoration junctions and feature compositions. Validate rejects missing endpoints, duplicate or wrong-scene feature references and unbound feature compositions. It has no direct serialized room/Tilemap field in its authoring field list. Runtime movement is Rigidbody2D.MovePosition; collision is governed by colliders rather than floor Tile occupancy. InteractionQuery searches Physics2D colliders and retrieves IInteractable from each collider GameObject.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:20`, `:67`, `:104`; `Assets/_Project/Features/Creatures/Movement.cs:13`, `:51`; `Assets/_Project/Systems/Interaction/Core/InteractionQuery.cs:7`.

```csharp
body.MovePosition(body.position + effective * moveSpeed * Time.fixedDeltaTime);
var hits = Physics2D.OverlapCircleAll(position, range, mask.value);
```

SurvivorAvatarAdapter treats all active same-scene Tilemaps without a Collider2D component as authored floor candidates, then checks solid overlap for landing. This includes any noncolliding visual Tilemap, not just a map named Floor.

Files: `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:65`, `:75`.

```csharp
.Where(map => map.GetComponent<Collider2D>() == null).ToArray();
return floors.Length == 0 || floors.Any(map => map.HasTile(map.WorldToCell(position)));
```

SolitudeSlidingDoor serializes renderer, frames, blocker, frameSeconds and accessGate. Awake enables its blocker; opening requires exactly nine frames for progression; completion disables the blocker. The builders wire a separate proximity trigger and either completion or power-socket references. Opening visuals sort by feet Y; caps use a fixed order above the player. These collider/door/component links are observable current dependencies.

Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:10`, `:31`, `:38`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:95`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:66`, `:73`; `Assets/_Project/Features/World/OpeningSlice/OpeningDepthSort.cs:9`.

### Which Unity Editor version and installed Tilemap, brush, and testing packages are available, and which custom-brush, batched editing, Undo, and scene-persistence APIs do they provide?

The project version is 6000.6.4f1 (12bfff696524). Manifest dependencies include Tilemap 1.0.0, Tilemap Extras 9.0.1, Test Framework 1.8.0, URP 17.6.0 and Aseprite 6.0.0. Source copies exist in Library/PackageCache, including Tilemap at `com.unity.2d.tilemap@f4c7bd71ac8a` and Extras at `com.unity.2d.tilemap.extras@9446f3c60919`.

Files: `ProjectSettings/ProjectVersion.txt:1`; `Packages/manifest.json:3` (dependencies).

```text
m_EditorVersion: 6000.6.4f1
```

Installed GridBrush overrides Paint, Erase, BoxFill and BoxErase. BoxFill batches positions, tiles, colors and transforms into `map.SetTiles(...)` using NativeArrays/TileArray. Its editor overrides RegisterUndo before brush actions and uses complete-object Undo for the Tilemap and its GameObject; valid targets are GameObjects containing Tilemap. GridBrushEditorBase exposes virtual RegisterUndo and validTargets. Extras' GameObjectBrush demonstrates `[CustomGridBrush]` and inheritance from GridBrushBase. Existing project builders use EditorSceneManager.NewScene/OpenScene/MarkSceneDirty/SaveScene, PrefabUtility.InstantiatePrefab and AssetDatabase.SaveAssets.

Files: `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrush.cs:137`, `:159`, `:182`, `:203`, `:218`; same package `Editor/GridBrushEditor.cs:658`, `:674`, `:987`; `Editor/GridBrushEditorBase.cs:98`; `Library/PackageCache/com.unity.2d.tilemap.extras@9446f3c60919/Editor/Brushes/GameObjectBrush/GameObjectBrush.cs:16`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:66`, `:109`, `:126`.

```csharp
Undo.RegisterCompleteObjectUndo(new Object[] { tilemap, tilemap.gameObject }, undoMessage);
map.SetTiles(positionArray, tileArray, colorArray, transformArray, false);
```

### Which Tilemap change notifications are available in the installed Editor, and what do accessible engine source or API inspection establish about notification timing and callback ordering relative to editing operations, Undo, and Redo?

Accessible installed package source references `Tilemap.HasSyncTileCallback()` and `tilemap.bufferSyncTile`; GridBrushEditorBase enables buffering on OnEditStart and disables it on OnEditEnd for child Tilemaps when a sync callback exists. PaintableGrid calls those hooks as the executing state changes. PaintableSceneViewGrid forwards the hooks to the active brush editor, subscribes to Undo.undoRedoPerformed, and refreshes tiles in that callback. Brush Undo registration is documented and invoked before the brush action.

Files: `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrushEditorBase.cs:107`; `Editor/PaintableGrid.cs:75`, `:429`; `Editor/PaintableSceneViewGrid.cs:39`, `:159`, `:170`, `:251`.

```csharp
internal void OnEditStart(GridLayout gridLayout, GameObject brushTarget)
{ SetBufferSyncTile(brushTarget, true); }
internal void OnEditEnd(GridLayout gridLayout, GameObject brushTarget)
{ SetBufferSyncTile(brushTarget, false); }
```

The public event signatures and native dispatch/flush ordering for tilemapTileChanged and tilemapPositionsChanged, and ordering relative to Undo/Redo callbacks, remain unresolved within the authorized project root. Searches `tilemapTileChanged|tilemapPositionsChanged|HasSyncTileCallback|bufferSyncTile` across Library/PackageCache C# found only the buffer references above; filename enumeration for Tilemap.cs/Tilemap.bindings/TilemapModule/GridBrushBase.cs within Library found build metadata, not engine implementation source. No external Editor installation/source roots were authorized, and no runtime event-order experiment was performed. The buffering code does not establish native notification timing or guarantee a single event per user gesture.

### What logical Grid cell sizes, transforms, sprite-density conventions, pixel-perfect settings, and rendering-order rules currently govern interior floors, full-height walls, caps, and the player?

Wakeup serializes unit XY Grid cells, zero gap, identity transform and a Floor Tilemap with identity local position/scale and anchor (.5,.5,0). Floors/Walls palette prefabs also use unit XY cells and identity scale. There is no single consistent art density: older PNG floor/wall sheets use 128 PPU; candidate full-height Aseprite walls use 100 PPU; SolitudeStart atlas/door and the opening-room camera use 32 PPU.

Files: `Assets/_Project/Scenes/Wakeup.unity:958`, `:972`, `:985`, `:5034`, `:5049`, `:6114`; `Assets/_Project/Art/Tiles/Floors/Tilemap/Floors.prefab:29`, `:44`; `Assets/_Project/Art/Tiles/Walls/Tilemap/Walls.prefab:595`, `:609`; `Assets/_Project/Art/Tiles/Floors/Floor_Tilemap.png.meta:54`; `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:39`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:166`.

```yaml
m_CellSize: {x: 1, y: 1, z: 0}
m_TileAnchor: {x: 0.5, y: 0.5, z: 0}
```

SolitudeStart sets floor/north/perimeter orders 0/1/2, door 5 and camera assetsPPU=32, reference resolution 320×288. OpeningSlice instead sets floor/north/caps orders 0/20/2000, player OpeningDepthSort uses `1000 - RoundToInt(y*32)`, and its camera uses assetsPPU=32, 640×360, PixelSnapping and Windowbox. These are distinct scene-specific configurations.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:72`, `:97`, `:123`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:39`, `:66`, `:84`; `Assets/_Project/Features/World/OpeningSlice/OpeningDepthSort.cs:9`.

### Can the existing Standard architectural assets cover an eight-cell north boundary with two native-size 128-pixel modules and connect all four rectangular corners, and what rendered seam and baseline evidence establishes compatible or unsupported alignments after current pivots, trimming, transparent padding, and density settings are applied?

Standard asset identity is unresolved as recorded above. For the actual full-height candidate Aseprite wall modules, current width 128 and PPU 100 produce width 1.28 world units each, or 2.56 units for two modules at identity scale. Eight unit Grid cells span 8 units; therefore two such modules do not cover that existing Grid span at current native scale. This is an arithmetic conclusion from metadata and Grid serialization, not a rendered seam test. Older 128 PPU PNG sprites span one world unit each and also do not span eight unit cells with two sprites.

Files: `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:39`, `:140`; `Assets/_Project/Art/Tiles/Walls/Wall_Panel_Tilemap.png.meta:54`, `:123`; `Assets/_Project/Scenes/Wakeup.unity:972`.

```yaml
# full-height candidate
spritePixelsToUnits: 100
# animated sprite rectangle
width: 128
height: 238
```

Metadata establishes different vertical origins: wall_panel_1 is 238 high and bottom-pivoted; north corner candidates are 128 high/bottom-pivoted; bottom corner candidates are 64 high with pivot Y=.3125 (20 pixels); wall_top is 64 high with pivot Y=-1.328125 (-85 pixels). Thus an equal transform origin is not proof of equal artwork baseline. No candidate corner GUIDs or panel_1 GUID were found in `.unity`, `.prefab`, or `.asset` references under Assets using the exact GUIDs in the asset table. No rendered seam/baseline fixture or compatible four-corner assembly was established for this candidate family. This question remains unresolved beyond the current density/span mismatch and metadata facts; source alpha bounds alone do not prove rendered connection.

Files: `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:132`; `wall_left_corner.aseprite.meta:132`; `wall_right_corner.aseprite.meta:132`; `wall_bottom_left_corner.aseprite.meta:132`; `wall_bottom_right_corner.aseprite.meta:132`; `wall_top.aseprite.meta:132` (same directory).

### What reusable Tile Palettes, Tiles, RuleTiles, custom brushes, room builders, and editor creation or validation utilities exist, and how do their current extension points handle room selection and authoring operations?

Floors.prefab, Walls.prefab, Wall_Panels.prefab and DithArtPalette.prefab exist as palette assets, alongside ordinary Tile assets under Art/Tiles. Walls.prefab contains a UnityEditor.GridPalette subasset with cellSizing=0. RT_Wall_Caps exists but has no default sprite and no tiling rules. SolitudeBoundaryRuleTile is implemented and referenced by the starting-room builder; it has one RectInt interior and seven sprite fields, no room-selection UI. Starting-room/OpeningSlice builders expose MenuItems and use fixed paths and layout coordinates. OpeningSlice refinements check the active scene path; starting-room Build creates a fixed scene. No first-party `GridBrush`/`GridBrushBase`/`CustomGridBrush` class was found by C# searches in Assets/_Project. Installed Extras supplies brush examples but these are package tools.

Files: `Assets/_Project/Art/Tiles/Floors/Tilemap/Floors.prefab:14`; `Assets/_Project/Art/Tiles/Walls/Tilemap/Walls.prefab:623`; `Assets/_Project/Art/Tiles/Walls/Rules/RT_Wall_Caps.asset:13`; `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:8`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:26`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:25`, `:93`; `Library/PackageCache/com.unity.2d.tilemap.extras@9446f3c60919/Editor/Brushes/GameObjectBrush/GameObjectBrush.cs:16`.

```yaml
m_Name: RT_Wall_Caps
m_DefaultSprite: {fileID: 0}
m_TilingRules: []
```

Project validators include CompositionValidator, WakeupValidation and UnityMetaValidator. UnityMetaValidator reports metadata pairing and GUID errors; ValidationTests exercises scene composition and missing-script validation. These are not room selection or floor-layout geometry validators.

Files: `Assets/_Project/Editor/UnityMetaValidator.cs:16`, `:26`, `:57`; `Assets/_Project/Tests/Editor/ValidationTests.cs:37`.

### How do existing editor tools record batched changes, restore generated scene content through Undo and Redo, mark scenes dirty, and preserve prefab or scene serialization?

The installed default brush records complete-object Undo for its target Tilemap and GameObject and uses batched SetTiles. Its scene-painting surface refreshes tiles after Undo/Redo; GameObject creation menu code registers created-object Undo. First-party room builders inspected do not record Undo for created geometry or tile batches. Project-wide `Undo.` C# searches under Assets/_Project found CompositionMigration using Undo.AddComponent but no room-generation undoRedoPerformed handler or generated geometry Undo restoration workflow.

Files: `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrushEditor.cs:987`; `Editor/GridBrush.cs:203`; `Editor/PaintableSceneViewGrid.cs:159`; `Editor/ObjectCreationMenu/GameObjectCreation.cs:199`; `Assets/_Project/Editor/CompositionMigration.cs:36`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:66`.

```csharp
private void UndoRedoPerformed() { RefreshAllTiles(); }
```

First-party authoring uses MarkSceneDirty + SaveScene and SaveAssets. SurvivorSuccessionAuthoring refuses dirty scenes, opens additively, validates and closes only scenes it opened. SaveConfiguredScene constrains permitted paths and resets camera rect before saving. OpeningSlice persists a material asset explicitly rather than leaving a transient material reference; serialized wiring is commonly changed with SerializedObject.ApplyModifiedPropertiesWithoutUndo. Existing prefab instantiation preserves prefab identity through PrefabUtility.

Files: `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs:40`, `:55`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:63`, `:90`, `:105`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:111`, `:118`.

### What existing ownership mechanisms distinguish generated scene content from manually authored objects and protect unrelated objects during rebuilding or deletion?

The inspected room builders use hierarchy names, fixed scene paths, and object/component lookup, rather than a dedicated generated-content marker with an owner ID. Starting-room Build creates an additive scene and restores the prior active scene. OpeningSlice refuses overwriting its review scene, copies Wakeup, and removes named roots from that copied scene. ScrubberEncounterBuilder refuses an existing target scene and deletes roots not in a hardcoded keep-name list in its copied target scene. These mechanisms limit certain operations by scene/path, but do not intrinsically distinguish a manually authored object from a generated object with the same name.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:66`, `:126`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:28`, `:35`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:270`.

```csharp
if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
    throw new InvalidOperationException("Encounter already exists; refusing to overwrite.");
foreach (var root in scene.GetRootGameObjects())
    if (!keep.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
```

No generated-room ownership component or owner-ID deletion guard was found within the two room builders, SolitudeStart runtime sources, or project C# searches for `generatedRoot|Generated|Undo.`; absence is bounded to those searches and inspected owners. SceneBindings owns feature composition references and validates same-scene endpoints, but does not mark generated geometry.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:104`.

### How do existing authoring and validation tools handle empty regions, invalid asset configuration, overlapping authoring regions, and unsupported layouts, and what mechanisms report errors without leaving misleading generated content?

Starting-room ValidateLayout runs before asset creation and rejects anything other than 10×9, invalid tile IDs, non-floor interior IDs, nonstraight side runs, incorrect floor count, and incorrect centered 2×2 door quadrants. It does not accept an empty or arbitrarily shaped layout. Missing atlas sprites/door frame counts cause exceptions before new scene generation; missing player/input prefabs are checked later, after geometry construction. This sequence is not an atomic rollback implementation.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:29`, `:38`, `:42`, `:109`, `:132`.

```csharp
if (floorCount != 48) throw new InvalidOperationException("Floor must have six rows of eight cells.");
```

SolitudeBoundaryRuleTile returns null sprite outside its configured boundary or for isolated side-run cells, with no collider. OpeningSlice.Tile throws for missing Tile assets; OpeningSlice Build refuses Play mode, dirty current scenes and existing output. The installed default GridBrush returns for null target, target without Tilemap, or no populated brush cells. No overlapping-room-region validation or arbitrary-layout diagnostics were found within these room builders and boundary-rule sources; these tools have no serialized multiple-room region model to validate.

Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:20`, `:30`, `:36`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:28`, `:106`; `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrush.cs:159`.

### What Edit Mode test infrastructure and conventions support testing deterministic grid geometry, module placement, idempotent generation, room expansion and erasure, missing assets, and Undo and Redo?

SOLITUDE.Tests.EditMode is Editor-only, opts into TestAssemblies, and references the project Editor/Runtime/Domain/Application assemblies. Tests use NUnit [Test]/[TestCase], preview or additive scenes, try/finally cleanup, SerializedObject configuration and explicit assertions. ValidationTests checks deterministic validation and unchanged dirtiness. ScrubberEncounterAuthoringTests calls an authoring tool twice, compares its scene GUID, opens saved scenes and asserts exact serialized feature endpoints and singleton counts.

Files: `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef:2`; `Assets/_Project/Tests/Editor/ValidationTests.cs:14`, `:26`, `:37`; `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs:20`, `:33`, `:45`.

```csharp
SOLITUDE.Editor.ScrubberEncounterBuilder.AddTaskJournal();
SOLITUDE.Editor.ScrubberEncounterBuilder.AddTaskJournal();
Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));
```

No dedicated grid-geometry, native wall-module placement, floor expansion/erasure or room-generation Undo/Redo tests were found within Assets/_Project/Tests/Editor by `Tilemap|Undo|Redo|SaveScene|OpenScene` searches and test filename enumeration for Room/Tile/Brush. Existing coverage is infrastructure/pattern evidence, not evidence these unrepresented behaviors pass. No tests were run during this research.

### What existing checks or tooling can verify scene save/reload persistence, native-pixel visual alignment, absence of generated duplicates, and preservation of unrelated scenes, assets, and serialized references?

ValidationTests opens authored scenes additively, validates composition, asserts zero missing MonoBehaviour scripts, checks dirtiness is unchanged, closes only opened scenes and restores the previous active scene. This checks saved scene authoring references, not a mutate-save-reload geometry round trip. Scrubber authoring tests assert serialized references/singletons after tool calls; UnityMetaValidator checks metadata pairs and unique GUIDs. The README's verification runner copies Assets/Packages/ProjectSettings into an isolated project and runs Edit Mode/Play Mode/authoring/build checks; it explicitly states tileset work is separate from that gate.

Files: `Assets/_Project/Tests/Editor/ValidationTests.cs:37`; `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs:20`; `Assets/_Project/Editor/UnityMetaValidator.cs:16`, `:57`; `Tools/Verification/README.md:10`, `:14`, `:17`.

```csharp
Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
Assert.AreEqual(dirty, scene.isDirty, "Validation must not rewrite authoring.");
```

OpeningSlice camera settings establish a pixel-snapped rendering configuration and OpeningDepthSort establishes depth order, but neither is a seam-measurement assertion. No native-pixel seam/baseline verification fixture for the candidate architectural family, generated-room duplicate checker, or geometry save/reload test was found within the inspected room builders and Assets/_Project/Tests/Editor searches described above. No visual render or verification gate was run in this pass; compatibility and complete unrelated-content preservation remain unverified.

Files: `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:84`; `Assets/_Project/Features/World/OpeningSlice/OpeningDepthSort.cs:9`.

## New Questions
