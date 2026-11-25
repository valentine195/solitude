# SOLITUDE Unity Art Pipeline

This document defines how production-ready SOLITUDE art assets are imported, configured, integrated, and validated in Unity.

Use it for:

- sprite import,
- slicing,
- pivots,
- Tilemaps,
- prefabs,
- sorting,
- collision,
- animation integration,
- scene placement,
- gameplay-scale validation.

Assume source artwork already satisfies `pixel-pipeline.md`.

Do not use Unity settings or Transform scaling to compensate for invalid source art.

---

# 1. Inspect Existing Conventions First

Before adding or changing art integration, inspect the project for existing:

- Pixels Per Unit,
- sprite import settings,
- Tilemap structure,
- Tile Palettes,
- sorting layers,
- pivots,
- animation conventions,
- prefab patterns,
- asset directories.

Reuse compatible project conventions.

Do not create parallel art, Tilemap, prefab, or interaction systems without a clear reason.

---

# 2. World Scale

Default SOLITUDE scale:

`32 Pixels Per Unit`

Therefore:

- 32 px = 1 Unity unit
- 64 px = 2 Unity units
- 96 px = 3 Unity units

Use consistent pixel density across:

- tiles,
- characters,
- props,
- interactables,
- doors,
- world effects.

Do not vary PPU to make individual assets appear larger or smaller.

Fix the source dimensions instead.

---

# 3. Texture Import Settings

Pixel-art textures should normally use:

```text
Texture Type: Sprite (2D and UI)
Pixels Per Unit: 32
Filter Mode: Point
Compression: None
Generate Mip Maps: Off
```

Where appropriate:

```text
Alpha Is Transparency: On
```

Avoid:

- Bilinear filtering,
- Trilinear filtering,
- visible texture compression,
- mipmaps on ordinary 2D pixel sprites.

For tiles and grid-aligned sprites, prefer:

`Mesh Type: Full Rect`

unless a demonstrated need requires otherwise.

---

# 4. Sprite Mode and Slicing

Use `Single` for standalone sprites.

Use `Multiple` for:

- sprite sheets,
- animation sheets,
- tile atlases,
- coherent sprite families.

Slice deterministic sheets using explicit:

- cell width,
- cell height,
- origin,
- spacing,
- padding.

Do not rely on automatic slicing when grid dimensions are known.

Example player sheet:

```text
Cell Width: 32
Cell Height: 48
```

unless the source asset defines a documented alternative.

---

# 5. Pivots

Use consistent pivots within asset families.

## Characters

Default conceptual pivot:

`bottom center / floor contact`

Animation frames must preserve stable foot placement.

Do not correct frame jitter with per-frame pivot changes.

## Floor Props

Use a predictable footprint-based or bottom-center pivot.

Multi-tile assets should align consistently to the world grid.

## Wall Objects

Use the intended wall attachment point as the pivot.

Examples:

- terminal,
- sign,
- cabinet,
- service panel.

Do not solve inconsistent source alignment with arbitrary scene offsets.

---

# 6. Tilemaps and Tile Palettes

Follow the existing project Tilemap structure where possible.

Separate layers only when rendering or gameplay requires it.

Common concerns may include:

- floor,
- structural walls,
- decoration,
- collision,
- foreground/occlusion.

Avoid excessive Tilemap layers.

Create reusable Tile assets for stable concepts such as:

- floor bases,
- wall faces,
- wall caps,
- corners,
- thresholds,
- structural pieces,
- reusable variations.

Prefer palettes organized around shared ship construction, for example:

```text
Ship Base
Ship Structural
Ship Variations
Ship Damage
```

rather than room-specific palettes when those rooms share the same visual vocabulary.

---

# 7. Reusable States and Overlays

Preserve reusable source architecture where practical.

Examples:

- base wall + scuff overlay,
- base floor + stain,
- base door + status indicator,
- base terminal + powered/error state.

Avoid baking every state into room-specific assets or prefabs.

Visual state should correspond to actual gameplay state when applicable.

---

# 8. Interactive Assets

Before creating new components or prefabs, inspect existing gameplay systems.

Reuse established systems for:

- interaction,
- doors,
- containers,
- pickups,
- state changes,
- events.

New art should usually attach to existing behavior rather than recreate it.

Stateful objects may use prefabs when they require:

- interaction,
- animation,
- collision,
- multiple visual states,
- lighting,
- audio,
- gameplay logic.

Static decoration does not automatically require its own prefab.

---

# 9. Animation Integration

Animation assets must preserve the alignment defined in `pixel-pipeline.md`.

When creating clips:

- verify frame order,
- preserve anchor alignment,
- use consistent timing within the animation family,
- inspect looping behavior,
- avoid Transform scaling.

Check for:

- foot sliding,
- visible frame jitter,
- scale drift,
- incorrect transitions.

---

# 10. Sorting and Occlusion

Use existing Sorting Layers and ordering conventions.

Verify that:

- the player passes correctly in front of or behind props,
- wall-mounted objects render correctly,
- foreground elements occlude intentionally,
- pickups remain readable.

Do not solve sorting issues with arbitrary extreme order values if the underlying sorting model is incorrect.

---

# 11. Collision

Collision should represent gameplay space, not perfectly trace sprite transparency.

Prefer simple shapes:

- boxes,
- capsules,
- Tilemap collision,
- simple polygons when needed.

Do not automatically generate complex colliders from sprite outlines unless gameplay genuinely requires them.

---

# 12. Lighting

Use the project's existing 2D lighting approach.

Broad environmental states such as:

- normal operation,
- partial power,
- emergency,

should usually be handled through scene lighting or controlled overlays rather than repainted copies of every sprite.

Avoid rendering or post-processing choices that blur native pixel edges.

---

# 13. Transform and Grid Integrity

Production sprites should normally use:

```text
Scale:
X = 1
Y = 1
Z = 1
```

If unusual Transform scale is required to make the sprite look correct, investigate:

- source dimensions,
- PPU,
- pivot,
- camera settings,
- parent scale.

Tile-based architecture must align exactly to the world grid.

Watch for:

- fractional positions,
- incorrect pivots,
- inconsistent cell dimensions,
- unintended parent scaling.

---

# 14. Art Validation Scene

Maintain a lightweight `ArtValidation` scene if the project does not already have an equivalent.

It should contain representative:

- floor tiles,
- north wall,
- side/south boundaries,
- corners,
- door,
- player,
- terminal,
- container,
- common prop,
- relevant lighting states.

Use it as a stable test fixture, not a showcase level.

---

# 15. Validation Workflow

Validate meaningful new asset families in this order:

1. import/configuration,
2. `ArtValidation`,
3. one representative real room,
4. broader usage.

Inspect assets through the normal gameplay camera.

Check:

## Import

- PPU correct,
- Point filtering,
- compression off,
- mipmaps off,
- alpha correct.

## Geometry

- dimensions correct,
- pivot correct,
- scale = 1,
- grid alignment correct.

## Rendering

- pixels remain crisp,
- sorting is correct,
- lighting behaves correctly,
- no unexpected color changes.

## Gameplay

- collision works,
- interaction works,
- state transitions work,
- animation aligns correctly.

A sprite that looks correct only in the Inspector has not been validated.

---

# 16. Replacing Prototype Art

When replacing temporary artwork:

1. preserve the working gameplay object,
2. replace or update its visual reference,
3. verify behavior,
4. verify placement,
5. remove obsolete art only after confirming it is unused.

Do not rebuild functioning gameplay prefabs solely to install new graphics.

Preserve existing `.meta` files and GUIDs when replacing established assets in place where appropriate.

---

# 17. Naming and Repository Hygiene

Follow existing project naming conventions.

If none exist, use descriptive stable names such as:

```text
wall_north_ivory_base
door_service_closed
prop_battery_portable
terminal_ai_wall
overlay_wall_scuff_01
```

Avoid names based on generation order or revision history.

Do not place temporary generation outputs, chroma intermediates, duplicate exports, or unused concept sheets in production runtime directories.

---

# 18. Integration Scope

Art integration should avoid unrelated refactoring.

Small supporting changes are acceptable when required, such as:

- adding a visual state,
- assigning a SpriteRenderer,
- adding an Animation Clip,
- creating a Tile asset,
- wiring an existing state to new visuals.

Do not redesign unrelated gameplay systems during an art task.

---

# 19. Completion

Unity integration is complete when the asset:

- imports with correct pixel settings,
- uses the expected world scale,
- requires no corrective Transform scaling,
- aligns to the intended grid or anchor,
- renders in the correct order,
- preserves existing gameplay behavior,
- supports required visual states,
- and has been reviewed successfully through the gameplay camera.

If integration requires hiding a source-art problem, return to `pixel-pipeline.md` and fix the asset instead.