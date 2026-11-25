# QRSPI Research: floor-driven-room-authoring

## Answers

### What existing room, floor, wall, and door authoring behavior is present, and which parts already separate authored layout from generated visuals?

`SolitudeStartBuilder` validates a fixed 10×9 integer layout, updates reusable Tile assets and a boundary RuleTile, then creates a new additive scene with separate Floor, North Wall, and Rule Boundaries Tilemaps. Its interior is 8×6 cells; the north face occupies two rows. Collision boxes and the animated door are separate GameObjects. It saves to a fixed feature-local scene path and closes that new scene. It does not derive walls from painted floor occupancy. Its layout source is `SolitudeGeneratedLayout.Cells`, rather than the scene's current Tilemaps.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:29`, `:55`, `:66`, `:75`, `:87`, `:95`, `:126`.

```csharp
Tilemap floor = NewMap(gridGO.transform, "Floor", 0);
Tilemap north = NewMap(gridGO.transform, "North Wall", 1);
Tilemap perimeter = NewMap(gridGO.transform, "Rule Boundaries", 2);
// ...
if (x >= 1 && x <= 8 && y >= 1 && y <= 6) floor.SetTile(new Vector3Int(x, y), tiles[id]);
```

`OpeningSliceBuilder` makes Floor, North Wall Faces, and Boundary Caps maps at orders 0, 20, and 2000. It paints fixed coordinate ranges and creates collision separately. It refuses to overwrite its existing review scene, instructing editing instead. `RefineCurrent` changes the existing floor with `SwapTile`. `ScrubberEncounterBuilder.MakeRoom`, conversely, samples and clears every Tilemap in its target scene before filling fixed ranges; it destroys colliders under the named structure root. There is no authored occupancy/generated-output separation in that routine.

Files: `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:28`, `:39`, `:41`, `:98`, `:108`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:465`.

```csharp
if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)
    throw new InvalidOperationException("Review scene exists; edit it without overwriting.");
```

### Which existing floor, north-wall, cap, and structural-corner assets can form the Standard room, and what are their actual sprite dimensions, slicing, pixels per unit, pivots, transparent padding, and import settings?

No named Standard room/profile was found by case-insensitive filename searches for `standard`, `north_wall`, `structural`, and `architecture` under `Assets` (excluding `.qrspi`). Thus identification of a particular Standard asset set is unresolved. The following existing architectural families are present and measurable.

Individual Aseprite floor sprites are under `Assets/_Project/Art/Tiles/Floors/Sprites/`. Examples `dark_grate_1`, `dark_grate_2`, and `glowing_grate_1` have 128×128 sprite rectangles, bottom-center pivots, and 32 PPU. `floor_1`, edges, panels, shiny floors, and most other grates have 128×128 rectangles, bottom-center pivots, and 100 PPU. These are not interchangeable native world widths under their current imports: 128/32 = 4 units versus 128/100 = 1.28 units.

Files: `Assets/_Project/Art/Tiles/Floors/Sprites/dark_grate_1.aseprite.meta:39`, `:130`, `:220`; `Assets/_Project/Art/Tiles/Floors/Sprites/floor_1.aseprite.meta:39`, `:130`.

```yaml
spritePixelsToUnits: 32
# animatedSpriteImportData
pivot: {x: 0.5, y: 0}
width: 128
height: 128
```

Individual wall metadata is under `Assets/_Project/Art/Tiles/Walls/Sprites/`. All wall sprites below are 100 PPU unless stated otherwise. Dimensions are imported sprite rectangles, distinct from source canvas sizes.

| Sprite names | Sprite rectangle | Pivot | Source canvas |
| --- | --- | --- | --- |
| wall_panel_1, wall_panel_dark_1, wall_panel_intercom, wall_panel_light_1, wall_panel_window_1 | 128×238 | (0.5, 0) | 128×256 |
| wall_panel_dark_2, wall_panel_window_2 | 128×256 | (0.5, 0) | 128×256 |
| wall_left, wall_right | 64×128 | (0.5, 0) | 64×128 |
| wall_left_corner, wall_right_corner | 128×128 | (0.5, 0) | 128×128 |
| wall_top_left, wall_top_right | 128×128 | (0.5, 0) | 128×128 |
| wall_top | 128×64 | (0.5, -1.328125) | 128×128 |
| wall_top_variant, wall_bottom | 128×64 | (0.5, 0) | 128×43 |
| wall_bottom_left_corner, wall_bottom_right_corner | 128×64 | (0.5, 0.3125) | 128×43 |
| wall_top_bottom, wall_right_left | 128×128 | (0.5, 0) | 128×128 |
| door_closed | 256×238, **32 PPU** | (0.5, 0) | 256×256 |

Files: each same-named `.aseprite.meta` in `Assets/_Project/Art/Tiles/Walls/Sprites/`, at `:39` (PPU), `:130` (name), `:132` (pivot), `:140` (rectangle), and `:184` (canvas; `door_closed` has additional layer records). Representative evidence:

```yaml
# wall_panel_1.aseprite.meta:132,140,184
pivot: {x: 0.5, y: 0}
width: 128
height: 238
canvasSize: {x: 128, y: 256}
```

The Aseprite importers use Point filtering, mipmaps off, transparency enabled, mesh type 1, mosaic padding 4, sprite padding 0, animated sprite import data, and no generated physics shape. `wall_panel_1` has no platform override entries. Floors such as `dark_grate_1` include platform entries, so settings must be checked per asset.

Files: `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:12`, `:34`, `:45`, `:56`, `:91`, `:129`, `:180`; `Assets/_Project/Art/Tiles/Floors/Sprites/dark_grate_1.aseprite.meta:180`.

Read-only binary inspection of Aseprite RGBA cel data found `wall_panel_1` and its 238-high variants have nontransparent cel bounds (0,18)–(128,256) on the 128×256 canvas; their 18 empty top rows are trimmed from the stored sprite rectangle. Dark/window variant 2 occupy full 256 height. Left/right and 128×128 corner cels reach their complete rectangular bounds. Several cap cels extend outside their source canvas: `wall_top`/`wall_bottom`/`wall_top_variant` have cel bounds (0,-21)–(128,43), and bottom corners (0,-1)–(128,63), matching stored 64-high import rectangles rather than their 43-high canvas. These are cel extents, not a live Unity import or a proof that every pixel inside is opaque. Binary source files have no line numbers; the corresponding rectangle/canvas metadata at the line references above corroborates the dimension distinction. The live imported Sprite.rect/textureRect and final alpha texture were not queried.

The PNG sheets `Floor_Tilemap.png`, `Wall_Panel_Tilemap.png`, and `Wall_Tilesheet.png` are a different family: multiple 128×128 centered slices at 128 PPU, Bilinear filtering (`filterMode: 1`), mipmaps off, and default platform compression 1. They already have ordinary Tile assets. They are not full-height 128×238 modules.

Files: `Assets/_Project/Art/Tiles/Floors/Floor_Tilemap.png.meta:12`, `:40`, `:54`, `:80`, `:118`; corresponding wall PNG `.meta` files at the same lines; `Assets/_Project/Art/Tiles/Floors/Tilemap/Floor_Tilemap_0.asset:15`.

### Where do the project's ownership, placement, naming, and assembly conventions place feature-owned room data, geometry logic, editor tooling, tests, demonstration content, and authoring documentation?

Root `README.md` is authoritative; `Assets/_Project/README.md` does not exist. Features own exclusive scripts, authoring data and prefabs; shared reusable runtime presentation/services live in Systems; shared tiles/palettes in Art/Tiles; Editor owns tools and validators; Tests owns test assemblies; nonshipping demos are under Scenes/Demos or feature-local Scenes. Review evidence belongs outside Assets. The README does not mandate a particular new room-authoring documentation filename; existing central editor documentation is `Assets/_Project/Editor/README.md:1`.

Files: `README.md:15`, `:39`, `:51`, `:54`, `:57`, `:65`, `:71`, `:95`.

```text
Keep exclusive content with its owner.
Use PascalCase without spaces for new first-party folders, scripts, scenes, and prefabs.
```

Domain and Application are Unity-independent. Runtime references both plus Unity packages; Editor references runtime/domain/application and Tilemap Extras and is Editor-only. EditMode tests reference Editor/Runtime/Domain/Application and are Editor-only TestAssemblies. Feature-local SolitudeStart editor scripts and Art/SOLITUDE editor scripts have `.asmref` files targeting the central Editor assembly. No existing room-specific independent geometry assembly was found in the inspected first-party room sources.

Files: `Assets/_Project/Domain/SOLITUDE.Domain.asmdef:1`; `Assets/_Project/Application/SOLITUDE.Application.asmdef:1`; `Assets/_Project/SOLITUDE.Runtime.asmdef:1`; `Assets/_Project/Editor/SOLITUDE.Editor.asmdef:1`; `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef:1`; `Assets/_Project/Features/World/SolitudeStart/Editor/SOLITUDE.Editor.asmref:1`.

### Which existing scene-composition, movement, interaction, door, and rendering dependencies touch the room geometry or Tilemaps that this tooling would create, and what serialized references or behavior must remain intact?

Movement uses Rigidbody2D.MovePosition; blocking is physics-based, rather than a Tilemap occupancy movement system. PlayerMovement requires BoxCollider2D and MovementBehavior. Builders explicitly create separate solid BoxCollider2D boundaries.

Files: `Assets/_Project/Features/Creatures/Movement.cs:10`, `:51`; `Assets/_Project/Features/Player/Movement/PlayerMovement.cs:5`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:87`.

```csharp
body.MovePosition(body.position + effective * moveSpeed * Time.fixedDeltaTime);
```

`SurvivorAvatarAdapter.HasFloorAt` enumerates **every active same-scene Tilemap without Collider2D** and accepts occupancy on any of them as authored floor. Safe/awakening points require same-scene authored floor. Collision-free landing also checks physics overlap against unrelated solid colliders. This includes noncolliding visual maps, regardless of map name.

Files: `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:58`, `:65`, `:75`.

```csharp
.Where(map => map.GetComponent<Collider2D>() == null).ToArray();
return floors.Length == 0 || floors.Any(map => map.HasTile(map.WorldToCell(position)));
```

SceneBindings serializes player inventory/hotbar/movement/interactor, UI endpoints, lockers, pickups, restoration junctions and feature composition references; validation rejects missing, duplicate, wrong-scene or unbound feature references. Its field declarations contain no room/Grid/Tilemap endpoint. Interaction ownership cannot coexist with standalone SceneInteractionBindings in a composed player scene.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:20`, `:67`, `:78`, `:104`.

SolitudeSlidingDoor owns serialized SpriteRenderer, nine-frame Sprite array, Collider2D blocker, frame duration and optional RestorationDoorGate; Awake enables the blocker, Update disables it after all nine frames. Builders create proximity trigger/sensor children separately. The older Door component also serializes its SpriteRenderer and blocker. Rendering depends on independent map orders and optional y-based sprite order (see rendering answer).

Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:10`, `:31`, `:38`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:95`; `Assets/_Project/Features/Interactables/Doors/Door.cs:10`.

### Which Unity Editor version and installed Tilemap, brush, and testing packages are available, and which custom-brush, tile-change notification, batched editing, Undo, and scene-persistence APIs do they provide?

Unity is 6000.6.4f1 (12bfff696524). Manifest versions: Tilemap 1.0.0, Tilemap Extras 9.0.1, Aseprite 6.0.0, Test Framework 1.8.0, URP 17.6.0. Current package source is available in project-local Library/PackageCache and was inspected specifically for these APIs.

Files: `ProjectSettings/ProjectVersion.txt:1`; `Packages/manifest.json:4`, `:8`, `:9`, `:18`, `:19`.

GridBrush extends GridBrushBase and overrides Paint, Erase, BoxFill, BoxErase and FloodFill. Paint/Erase delegate to bounds operations. Batched filling uses NativeArrays and `Tilemap.SetTiles(positionArray,tileArray,colorArray,transformArray,false)`; empty/null targets return. Custom brush discovery uses `TypeCache.GetTypesWithAttribute<CustomGridBrushAttribute>()`. GridBrushEditorBase exposes OnPaintSceneGUI and RegisterUndo; GridBrushEditor registers complete-object Undo for Tilemap and its GameObject and enumerates valid Tilemap targets.

Files: `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrush.cs:27`, `:137`, `:148`, `:159`, `:203`, `:218`, `:264`; `.../Editor/GridPaletteBrushes.cs:91`; `.../Editor/GridBrushEditorBase.cs:50`, `:102`; `.../Editor/GridBrushEditor.cs:662`, `:674`, `:987`.

```csharp
Undo.RegisterCompleteObjectUndo(new Object[] { tilemap, tilemap.gameObject }, undoMessage);
```

`SetTilesBlock` is used in the installed RandomBrush preview path. Scene persistence uses EditorSceneManager.MarkSceneDirty/SaveScene, and prefab persistence uses PrefabUtility.SaveAsPrefabAsset in first-party tools.

Files: `Library/PackageCache/com.unity.2d.tilemap.extras@9446f3c60919/Editor/Brushes/RandomBrush/RandomBrush.cs:504`; `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs:32`, `:55`.

Tile-change notification declarations/callback contracts remain unresolved from inspected source: searches for `tilemapTileChanged|tilemapPositionsChanged` in first-party C# and installed Tilemap package C#/Markdown found no declaration or usage. Engine module implementation is not in those package source directories. No event availability or callback ordering was established by this research; no compilation/runtime API probe was run.

### What logical Grid cell sizes, transforms, sprite-density conventions, pixel-perfect settings, and rendering-order rules currently govern interior floors, full-height walls, caps, and the player?

The shipping Wakeup scene Grid uses cell size (1,1,0), unit transform scale, and floor Tilemap anchor (0.5,0.5,0). Its camera records assetsPPU 32 and reference 640×360. The reusable Floors/Walls palette prefabs also have unit scale and (1,1,0) Grid cells. Do not confuse UI GridLayoutGroup cell-size overrides elsewhere in the scene with this world Grid.

Files: `Assets/_Project/Scenes/Wakeup.unity:958`, `:972`, `:986`, `:1936`, `:1938`, `:5050`, `:6114`; `Assets/_Project/Art/Tiles/Floors/Tilemap/Floors.prefab:30`, `:44`; `Assets/_Project/Art/Tiles/Walls/Tilemap/Walls.prefab:595`, `:609`.

```yaml
m_CellSize: {x: 1, y: 1, z: 0}
m_AssetsPPU: 32
m_RefResolutionX: 640
m_RefResolutionY: 360
```

SolitudeStartBuilder uses Vector3.one Grid cells, 32-pixel sprite slices at 32 PPU, maps 0/1/2 and door order 5; its camera reference is 320×288 at 32 PPU. OpeningSliceBuilder uses maps 0/20/2000; props and player use `1000 - RoundToInt(y*32)`. Opening camera uses PixelSnapping, Windowbox, 32 PPU, 640×360. Thus there is more than one existing ordering convention; no common renderer policy abstraction was found in the inspected room and opening sources.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:71`, `:97`, `:123`, `:166`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:39`, `:84`, `:112`; `Assets/_Project/Features/World/OpeningSlice/OpeningDepthSort.cs:9`.

The imported architectural families have mixed 32/100/128 PPU as detailed above. At native scale a 128-pixel module occupies four 1-unit cells only at 32 PPU. Current 100-PPU wall modules occupy 1.28 units; PNG tile-sheet slices at 128 PPU occupy one unit.

Files: `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:39`; `Assets/_Project/Art/Tiles/Floors/Sprites/dark_grate_1.aseprite.meta:39`; `Assets/_Project/Art/Tiles/Walls/Wall_Tilesheet.png.meta:54`.

### Can the existing Standard architectural assets cover an eight-cell north boundary with two native-size 128-pixel modules and connect all four rectangular corners, and which widths or alignments lack compatible assets?

Under the current unit Grid and current imported 100-PPU `wall_panel_1` settings, two modules span 2×128/100 = **2.56 units**, not eight cells. Two 128-PPU PNG sheet slices span two units. The only measured individual floor examples at 32 PPU span four units each. There is no identified Standard profile assigning a different import density or transform to north modules.

Files: `Assets/_Project/Scenes/Wakeup.unity:972`; `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:39`, `:140`; `Assets/_Project/Art/Tiles/Walls/Wall_Panel_Tilemap.png.meta:54`, `:123`.

Assets exist with all four corner names: wall_left_corner/right_corner (128×128) and wall_bottom_left_corner/right_corner (128×64), plus wall_top_left/right. Their pivots are inconsistent with `wall_top`'s negative y pivot. The cap RuleTile has neither default sprite nor tiling rules, so it does not establish connected corner behavior. No native full-height 32-, 64-, or 96-pixel-wide north-panel variant was found in filename/metadata enumeration of `Art/Tiles/Walls/Sprites`; all panel variants there are 128 pixels wide (door is 256). Exact compatible seam alignment for these source families is **unresolved**: no Standard assignment or scene using those modules as a complete eight-cell room was identified, and no live rendering/import check was performed. Presence of corner names is not evidence of seamless connection at a chosen baseline.

Files: the same-named wall `.aseprite.meta` files at `:132`, `:140`; `Assets/_Project/Art/Tiles/Walls/Rules/RT_Wall_Caps.asset:15`.

```yaml
m_DefaultSprite: {fileID: 0}
m_TilingRules: []
```

### What reusable Tile Palettes, Tiles, RuleTiles, custom brushes, room builders, and editor creation or validation utilities exist, and how do their current extension points handle room selection and authoring operations?

Palette prefabs exist at Art/Tiles/Floors/Tilemap/Floors.prefab, Art/Tiles/Walls/Tilemap/Walls.prefab, and Art/Tiles/Imported/DithArt/DithArtPalette.prefab. Floors/Walls contain Grid, Tilemap, and Palette Settings; ordinary Tile assets sit beside them. SolitudeStart also has persistent generated Tile assets. `RT_Wall_Caps` is an empty RuleTile. `SolitudeBoundaryRuleTile` is a custom RuleTile with a serialized RectInt interior and explicit directional sprites; it chooses sprite by absolute position relative to that rectangle, with a vertical-neighbor check for sides.

Files: `Assets/_Project/Art/Tiles/Floors/Tilemap/Floors.prefab:14`, `:60`, `:687`; `Assets/_Project/Art/Tiles/Walls/Tilemap/Walls.prefab:579`, `:623`; `Assets/_Project/Art/Tiles/Imported/DithArt/DithArtPalette.prefab:1`; `Assets/_Project/Art/Tiles/Floors/Tilemap/Floor_Tilemap_0.asset:13`; `Assets/_Project/Art/Tiles/Walls/Rules/RT_Wall_Caps.asset:18`; `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:11`.

```csharp
int left = interior.xMin - 1, right = interior.xMax;
int bottom = interior.yMin - 1, top = interior.yMax + 1;
```

Builders are menu commands with fixed source/output scene paths or the active scene constrained to a fixed path. They do not expose a selected-room registry or scene room selector. First-party custom brushes/brush editors were not found by `CustomGridBrush|GridBrushBase|GridBrushEditor` searches in `Assets/_Project/**/*.cs`, excluding `.qrspi`. Current general extension points are the installed brush target callbacks/validTargets described above. Composition, Wakeup, and metadata validators are existing menu/CI utilities, rather than room geometry validators.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:20`, `:26`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:21`, `:96`; `Assets/_Project/Editor/CompositionValidator.cs:20`; `Assets/_Project/Editor/WakeupValidation.cs:48`; `Assets/_Project/Editor/UnityMetaValidator.cs:26`.

### How do existing editor tools record batched changes, restore generated scene content through Undo and Redo, mark scenes dirty, and preserve prefab or scene serialization?

No first-party complete batch/Undo/Redo generation pattern was found. A search for `Undo\.|RegisterCompleteObjectUndo|PerformUndo|PerformRedo` over `Assets/_Project/**/*.cs` finds Undo.AddComponent in CompositionMigration but no generated-room Undo registration/restoration or Undo/Redo tests. Room builders use ordinary object creation and DestroyImmediate. Installed GridBrushEditor does register complete-object Undo for edited map/GameObject, but this does not show restoration of separate generated child hierarchies.

Files: `Assets/_Project/Editor/CompositionMigration.cs:36`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:35`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:487`; `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrushEditor.cs:987`.

Existing tools explicitly mark/save scenes and save assets. SurvivorSuccessionAuthoring rejects dirty target scenes, opens additively only when necessary, restores scene lifetime in finally, and uses PrefabUtility.SaveAsPrefabAsset. SolitudeStartBuilder updates existing Tile assets instead of replacing them and instantiates existing prefabs with PrefabUtility.InstantiatePrefab. OpeningSliceBuilder persists a material asset before saving its reference. SerializedObject edits commonly use ApplyModifiedPropertiesWithoutUndo.

Files: `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs:32`, `:40`, `:55`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:44`, `:109`, `:126`, `:233`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:63`.

```csharp
if (scene.isDirty) throw new InvalidOperationException("Save or resolve existing scene edits before authoring succession.");
EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
```

### What existing ownership mechanisms distinguish generated scene content from manually authored objects and protect unrelated objects during rebuilding or deletion?

Inspected room tools use fixed scene paths, root/object names, hierarchy membership and component searches as boundaries. SolitudeStartBuilder creates a fresh additive scene. OpeningSliceBuilder refuses to overwrite an existing review scene, copies Wakeup, then deletes explicitly named objects in the copied scene. Scrubber MakeRoom clears all Tilemaps in its supplied scene and all colliders below the named structure; it does not check an ownership marker or preserve manual map edits there. Survivor authoring checks a fixed allowed scene path and reuses SingleOrDefault components to avoid duplicates.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:66`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:31`, `:33`, `:35`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:467`, `:486`; `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs:59`, `:68`.

```csharp
var point = All<SurvivorAwakeningPoint>(scene).SingleOrDefault();
if (point == null) { /* create */ }
```

A generated room ownership component/token or authoring-region ownership table was not found within first-party room sources searched with `Generated|owner|ownership|Room` in `Features/World/SolitudeStart`, `Art/SOLITUDE/Editor`, and the room portions of central Editor. Existing interaction/persistence ownership identifiers are gameplay ownership, not generated-geometry deletion guards.

### How do existing authoring and validation tools handle empty regions, invalid asset configuration, overlapping authoring regions, and unsupported layouts, and what mechanisms report errors without leaving misleading generated content?

The fixed SolitudeStart validator throws before scene creation on wrong array dimensions, invalid IDs, non-floor interior, bad side runs, floor count other than 48, and wrong centered door quadrants. Sprite/door validation throws for missing sprites or nine-frame mismatch; missing player/router prefab checks occur after scene construction has begun. There is no try/finally around the entire SolitudeStart Build, so this source does not establish cleanup of the new scene on late failure. In its RuleTile, cells outside the configured rectangle or disconnected side positions return null sprites and no collider.

Files: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:29`, `:38`, `:42`, `:109`, `:132`; `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:20`, `:30`, `:36`.

```csharp
if (floorCount != 48) throw new InvalidOperationException("Floor must have six rows of eight cells.");
data.sprite = chosen;
data.colliderType = Tile.ColliderType.None;
```

OpeningArtImport.WriteNormalized also rejects an empty source-art region before writing its normalized output, using alpha >=192 to establish content bounds (`Assets/_Project/Art/SOLITUDE/Editor/OpeningArtImport.cs:58`, `:63`). This is source-image validation, not painted-room region validation.

Opening builder checks Play mode, dirty active scene and existing output before modifying its copied scene. Scrubber MakeRoom clears a map before skipping an empty sample; it has no empty-region warning in that method. Overlapping authoring-region detection and arbitrary unsupported-layout validation were not found in these room sources using `overlap|region|empty|ValidateLayout` searches. Composition validation reports reference/ownership faults, not room topology. No transactional rollback for room generation failures was found in these inspected builders.

Files: `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:28`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:470`; `Assets/_Project/Core/Composition/SceneBindings.cs:67`.

### What Edit Mode test infrastructure and conventions support testing deterministic grid geometry, module placement, idempotent generation, room expansion and erasure, missing assets, and Undo and Redo?

Existing EditMode infrastructure is NUnit in SOLITUDE.Tests.EditMode, Editor-only, referencing central Editor and runtime/domain/application. Tests construct objects, load assets, call authoring commands, open target scenes additively, inspect components and SerializedObject references, assert unchanged GUIDs and instance identities, and close scenes in finally. Concrete idempotence coverage exists for repeated journal/succession authoring. These establish test conventions, not passing results in this research run.

Files: `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef:1`; `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs:18`; `Assets/_Project/Tests/Editor/SurvivorSuccessionAuthoringTests.cs:18`, `:32`.

```csharp
SurvivorSuccessionAuthoring.AuthorWakeup();
Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(SurvivorSuccessionAuthoring.ScenePath));
Assert.AreSame(player, All<PlayerInteractor>(scene).Single());
```

No tests for deterministic room grid derivation, full-height module placement, floor expansion/erasure or generated-room Undo/Redo were found in `Assets/_Project/Tests` using `Tilemap|RuleTile|GridBrush|PerformUndo|PerformRedo|Undo\.|module|geometry` C# searches, excluding `.qrspi`. Existing room-adjacent tests validate renderer ordering and physics paths, rather than tile authoring operations. No tests were executed.

Files: `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs:162`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:124`.

### What existing checks or tooling can verify scene save/reload persistence, native-pixel visual alignment, absence of generated duplicates, and preservation of unrelated scenes, assets, and serialized references?

Existing authoring tests call saved authoring operations, reopen scenes, assert GUID continuity, singleton counts, preserved component instances and serialized endpoint references. They give precedents for persistence and duplicate/reference checks, but do not currently cover a generated room save/reload cycle. Survivor tests also inspect authored camera viewport state. Metadata validation checks missing/orphan metadata, asset/meta tracking pairs and duplicate GUIDs; composition validation checks scene endpoints and ownership. These do not prove preservation of every unrelated asset or scene byte.

Files: `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs:20`, `:33`, `:40`, `:55`; `Assets/_Project/Tests/Editor/SurvivorSuccessionAuthoringTests.cs:32`, `:50`; `Assets/_Project/Editor/UnityMetaValidator.cs:64`; `Assets/_Project/Editor/README.md:18`; `Assets/_Project/Editor/CompositionValidator.cs:25`.

The verification runner copies Assets/Packages/ProjectSettings to a dedicated temporary project and supports Unity EditMode/PlayMode/authoring validation and player builds. Its documentation explicitly says tileset work is separate from that gate. No native-pixel seam/alignment automated check was found in inspected room/editor tests or verification documentation with `pixel|native|Tilemap` searches. Existing point-filter/PPU/camera settings are configuration evidence only; no screenshot, rendered alignment verification, scene reload experiment, tests or build was run here.

Files: `Tools/Verification/README.md:10`, `:14`, `:17`, `:30`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:84`.

```text
The runner copies Assets, Packages, and ProjectSettings into a dedicated temporary project.
Tileset work is separate from this gate.
```

## New Questions

- Which exact existing asset paths identify the Standard room's floor, full-height north modules, caps and four structural corners?
- What current Unity engine source or authorized live API inspection can establish Tilemap change notification availability and callback/Undo ordering in Unity 6000.6.4f1?
- What rendered seam/baseline evidence establishes compatibility of the individual architectural cap and corner sprites after their current pivots, trimming and density settings are applied?
