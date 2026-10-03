# SOLITUDE Unity Art Pipeline

This document defines how production-ready SOLITUDE art assets are imported, configured, organized, integrated, and validated in Unity.

Use it whenever:

- importing new sprites or tiles,
- configuring texture import settings,
- slicing sprite sheets,
- creating Tile assets or Tile Palettes,
- configuring pivots,
- placing art into scenes,
- updating art-related prefabs,
- or validating newly generated assets in game.

For visual design decisions, use `art-direction.md`.

For pixel-art generation and image processing, use `pixel-pipeline.md`.

The source artwork should already be technically correct before reaching this stage.

Do not use Unity Transform scaling, filtering, or import configuration to hide problems in the source asset.

---

# 1. Inspect Existing Project Conventions First

Before changing import settings, directories, palettes, prefabs, or scenes, inspect the existing project.

Determine:

- current Pixels Per Unit,
- existing sprite import settings,
- existing Tilemap structure,
- Tile Palette locations,
- sorting layers,
- sprite pivots,
- asset directories,
- prefab conventions,
- scene organization,
- animation-controller conventions.

Prefer existing project conventions when they are compatible with the requirements in this document.

Do not create parallel systems unnecessarily.

If an existing convention conflicts with SOLITUDE's required pixel-art behavior, fix the inconsistency deliberately rather than silently introducing a second convention.

---

# 2. Source Asset Location

Production artwork should live under the project's established art hierarchy.

If no suitable structure already exists, prefer approximately:

```text
Assets/
└── Art/
    └── SOLITUDE/
        ├── Characters/
        ├── Tiles/
        ├── Props/
        ├── Interactables/
        ├── Doors/
        ├── Terminals/
        ├── Effects/
        └── Reference/
```

Do not create a new hierarchy if equivalent project folders already exist.

Keep raw generation sources separate from production assets when practical.

Only production-ready images should be imported into runtime asset directories.

---

# 3. Pixels Per Unit

Default SOLITUDE world scale:

`32 Pixels Per Unit`

Therefore:

- 32 px = 1 Unity world unit,
- 64 px = 2 Unity world units,
- 96 px = 3 Unity world units.

Use the same pixel density for:

- environment tiles,
- characters,
- props,
- interactables,
- doors,
- world effects.

Do not change PPU on individual assets simply to make them appear larger or smaller.

Correct the source dimensions instead.

---

# 4. Texture Import Settings

Pixel-art textures should normally use:

```text
Texture Type: Sprite (2D and UI)
Filter Mode: Point
Compression: None
Generate Mip Maps: Off
Pixels Per Unit: 32
```

Where applicable:

```text
Alpha Is Transparency: On
```

Do not use:

- Bilinear filtering,
- Trilinear filtering,
- compressed texture formats that visibly alter pixel colors,
- mip maps for ordinary 2D pixel sprites.

---

# 5. Sprite Mode

Use:

`Single`

for standalone sprites.

Use:

`Multiple`

for:

- sprite sheets,
- animation sheets,
- tile atlases,
- intentionally packed sprite families.

Do not combine unrelated art into large sheets solely for organizational convenience.

Sheets should represent a coherent production family.

---

# 6. Mesh Type

For tiles and sprites where exact rectangular bounds matter, prefer:

`Full Rect`

This is especially appropriate for:

- tiles,
- wall sections,
- floor pieces,
- grid-aligned props.

Use tighter meshes only when there is a demonstrated rendering or performance reason.

Do not introduce irregular meshes that make grid placement harder to reason about.

---

# 7. Sprite Slicing

Sprite sheets should be sliced deterministically.

Use explicit:

- cell width,
- cell height,
- grid origin,
- padding,
- spacing.

Do not rely on automatic slicing when it can produce inconsistent frame boundaries.

For example, a 32×48 character sheet should use explicit:

```text
Cell Width: 32
Cell Height: 48
```

unless the asset specification explicitly defines a different frame size.

---

# 8. Pivot Conventions

Pivots must be consistent within asset families.

## Characters

Default conceptual pivot:

`bottom center / floor contact`

The visual foot position should remain stable between animation frames.

Do not compensate for frame jitter using different pivots per animation frame.

Fix the sprite alignment instead.

---

## Floor Props

Prefer a pivot that corresponds predictably to the object's footprint.

Usually:

`bottom center`

or an established grid-footprint convention.

Objects occupying multiple tiles must align consistently to the world grid.

---

## Wall-Mounted Assets

Use a pivot corresponding to the intended attachment point.

Examples:

- wall terminal,
- sign,
- panel,
- cabinet.

Placement should remain predictable when used on different wall segments.

---

# 9. Tilemap Structure

Use the existing project Tilemap architecture where available.

Do not create a new Tilemap hierarchy for every asset family.

A typical SOLITUDE room may logically separate:

- floor,
- structural boundaries,
- walls,
- decoration,
- collision,
- foreground/occlusion.

The exact implementation should follow the existing project if it already solves these concerns.

Avoid excessive Tilemap layers without a clear rendering or gameplay purpose.

---

# 10. Tile Assets

When new reusable environment sprites are introduced, create or update the appropriate Unity Tile assets.

Tiles should correspond to stable reusable concepts such as:

- floor base,
- floor variation,
- north wall,
- wall cap,
- corners,
- thresholds,
- structural pieces.

Do not create a unique Unity Tile asset for every visually identical room placement.

---

# 11. Tile Palettes

Prefer a small number of coherent Tile Palettes organized around actual level-building needs.

For example:

```text
Ship Base
Ship Structural
Ship Variations
Ship Damage
```

rather than:

```text
Cryobay Tiles
Corridor Tiles
Room 01 Tiles
Room 02 Tiles
Room 03 Tiles
```

when those rooms share the same construction language.

The goal is to reinforce reusable ship architecture.

---

# 12. Variations and Overlays

Where art was designed as:

`base + overlay`

preserve that architecture in Unity where practical.

Examples:

- base wall + scuff overlay,
- base floor + stain,
- base machine + damage state,
- base door + status indicator.

Do not bake all possible visual states into separate room-specific prefabs unless required by gameplay.

---

# 13. Interactive Objects

Interactive artwork should integrate with existing gameplay systems.

Before creating a new art-specific prefab or component, inspect whether the project already has:

- interaction interfaces,
- state components,
- container logic,
- door logic,
- item pickups,
- event systems.

Reuse existing gameplay architecture.

Art integration should not result in duplicate interaction systems.

Visual states should respond to existing gameplay state whenever possible.

---

# 14. Door Integration

Doors should reuse the project's existing door behavior.

Visual states may include:

- closed,
- open,
- unpowered,
- locked,
- damaged,

but those visual states should be connected to actual door state rather than duplicated scene objects manually enabled by level design.

Do not rewrite working door behavior solely because new art was introduced.

---

# 15. Animation Integration

Animations should preserve the sprite anchor established in `pixel-pipeline.md`.

When creating Animation Clips:

- maintain consistent sample rate within an animation family,
- avoid Transform scaling to compensate for sprite inconsistency,
- verify frame order,
- verify foot alignment,
- inspect looping transitions.

Do not assume the generated sprite-sheet frame sequence is correct without checking it.

---

# 16. Sorting

Follow existing project Sorting Layers and sorting conventions.

New artwork should slot into the established system rather than adding arbitrary sorting layers.

Verify:

- player passes correctly behind/in front of appropriate props,
- wall-mounted objects render correctly,
- foreground pieces occlude when intended,
- dropped items remain readable.

Do not solve sorting issues by assigning extreme arbitrary order values.

---

# 17. Colliders

Visual asset generation and collision geometry are separate concerns.

Do not derive complicated physics shapes automatically from sprite transparency unless that behavior is actually useful.

Prefer simple gameplay-driven collision:

- boxes,
- capsules,
- tile collision,
- simple polygons where required.

Collision should represent where the player can move, not perfectly trace artwork.

---

# 18. Lighting

Use the project's existing 2D lighting solution.

Do not bake environmental room lighting into reusable sprites unless the asset specifically requires it.

Prefer lighting systems for broad environmental state changes such as:

- operational,
- partial power,
- emergency.

Source sprites should normally remain reusable across those states.

Avoid using post-processing or filtering that softens native pixel edges.

---

# 19. Prefabs

Create prefabs when the asset represents a reusable world object with:

- interaction,
- animation,
- multiple visual states,
- collision,
- lighting,
- audio,
- or gameplay behavior.

Examples:

- door,
- cryopod,
- locker,
- terminal,
- battery pickup.

Simple static decoration does not automatically need its own prefab.

Avoid excessive prefab creation for trivial one-off sprites.

---

# 20. Art Validation Scene

Maintain a lightweight scene dedicated to art validation if the project does not already have an equivalent.

Suggested name:

`ArtValidation`

This scene should provide a stable place to inspect art without depending on a full gameplay level.

Include representative:

- floor tiles,
- north wall,
- east/west/south boundaries,
- corners,
- door,
- player,
- locker/container,
- terminal,
- common prop,
- lighting states.

Keep this scene simple.

It is a test fixture, not a showcase level.

---

# 21. Validation Environment

New assets should be inspected under conditions representative of actual play.

Validate against:

- approved existing assets,
- normal camera zoom,
- intended lighting,
- player scale.

Do not judge compatibility only against an empty Unity Scene view.

---

# 22. Gameplay-Scale Review

For each meaningful new asset family, enter Play Mode or otherwise view the scene through the normal game camera.

Check:

- apparent scale,
- silhouette,
- contrast,
- state readability,
- collision alignment,
- sorting,
- animation,
- pixel integrity.

A sprite can look correct in the Inspector while being visually wrong during gameplay.

The gameplay camera is the final authority.

---

# 23. Pixel-Perfect Rendering

Where the project uses Unity's Pixel Perfect Camera or equivalent configuration, maintain compatibility with it.

Do not alter global pixel-perfect settings simply to accommodate one incorrectly sized asset.

If a sprite exhibits:

- shimmering,
- inconsistent pixel size,
- blurred movement,
- uneven scaling,

investigate:

- source resolution,
- PPU,
- camera settings,
- transform position,
- parent scaling.

Do not immediately add filtering.

---

# 24. Transform Scale

Production sprites should normally use:

```text
Scale:
X = 1
Y = 1
Z = 1
```

Do not use Transform scaling to resize artwork into the correct apparent size.

If an asset requires:

```text
0.73
1.42
2.15
```

to look correct, the source asset or import configuration is probably wrong.

Fix the underlying issue.

---

# 25. Grid Alignment

Tile-based architecture must align exactly to the project's world grid.

Watch for:

- fractional positions,
- incorrectly sized sprites,
- pivots that shift tile placement,
- transform scaling,
- inconsistent cell dimensions.

Large multi-tile assets should still align predictably to grid boundaries or documented anchor points.

---

# 26. Existing Scenes

Do not broadly replace level artwork immediately after generating a new asset family.

First validate it in:

1. `ArtValidation`,
2. one representative real room,
3. then expand its usage.

This limits the impact of a bad generation batch.

---

# 27. Generated Asset Review

When replacing temporary or prototype artwork:

1. preserve the working gameplay object,
2. replace or update its Sprite/Tile reference,
3. validate behavior,
4. validate visual placement,
5. remove obsolete artwork only when no longer referenced.

Do not rebuild functioning gameplay prefabs just to install new graphics.

---

# 28. Asset Naming

Follow established project naming conventions first.

If none exist, prefer stable descriptive names such as:

```text
floor_ship_ivory_01
wall_north_ivory_base
wall_cap_graphite
door_service_closed
door_service_open
door_service_unpowered
prop_battery_portable
prop_locker_single
terminal_ai_wall
overlay_wall_scuff_01
```

Avoid generation-oriented names:

```text
image_3
new_tile
test_final
door_v7_final2
```

File names should describe what the asset is, not how it was created.

---

# 29. Meta Files

Treat Unity `.meta` files as part of the asset.

Do not delete or regenerate `.meta` files for established assets unnecessarily.

Preserving GUIDs prevents broken references in:

- scenes,
- prefabs,
- animations,
- Tile assets,
- ScriptableObjects.

When replacing artwork in place, preserve the existing asset identity where appropriate.

---

# 30. Repository Hygiene

Do not commit:

- temporary generation outputs,
- duplicate intermediate PNGs,
- large unused concept sheets,
- debug exports,
- temporary chroma-key images,

into production runtime directories.

Keep useful source/reference material in an intentional reference/source location.

Delete disposable intermediates once they are no longer needed.

---

# 31. Integration Scope

Art tasks should avoid unrelated gameplay refactors.

Small supporting changes are acceptable when necessary for proper asset integration.

Examples:

- adding a visual state to an existing door,
- exposing a SpriteRenderer reference,
- adding an animation clip,
- creating a Tile asset.

Do not use an art task as an excuse to redesign unrelated systems.

---

# 32. Validation Checklist

Before considering Unity integration complete, verify all applicable items.

### Import

- PPU is correct.
- Filter Mode is Point.
- Compression is disabled.
- Mip Maps are disabled.
- Alpha behaves correctly.

### Geometry

- sprite dimensions are correct,
- pivot is correct,
- Transform scale is 1,
- grid alignment is correct.

### Visuals

- no smoothing,
- no unexpected color changes,
- correct sorting,
- correct lighting behavior.

### Gameplay

- collision still works,
- interaction still works,
- state transitions still work,
- animations align correctly.

### Context

- inspected beside existing approved assets,
- inspected beside the player,
- inspected through the gameplay camera.

---

# 33. Completion Rule

Unity integration is complete only when the asset:

- imports with correct pixel settings,
- uses the correct world scale,
- requires no corrective Transform scaling,
- occupies the intended grid position,
- renders in the correct order,
- preserves existing gameplay behavior,
- works in required visual states,
- and has been visually reviewed through the normal gameplay camera.

Do not mark an art asset complete because it appears correctly in the Project or Inspector window.

It must work in the game.