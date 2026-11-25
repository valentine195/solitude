---
name: solitude-art-pipeline
description: >
  Production art pipeline for SOLITUDE, a top-down 2D pixel-art game aboard a
  generation ship. Use when creating, modifying, processing, integrating, or
  validating tiles, environments, characters, props, interactables, animations,
  or visual-state variants. Coordinates SOLITUDE-specific art direction,
  pixel-production rules, lower-level generation skills, and Unity integration.
---

# SOLITUDE Art Pipeline

Use this skill for production art work in SOLITUDE.

Optimize for **consistency, gameplay readability, reuse, native pixel quality,
and compatibility with the existing Unity project**. An image is not a finished
asset until it works in game.

## Core Invariants

- World grid: **32×32 px**
- Player target: **~32×48 px**
- Native pixel art; no antialiasing
- Nearest-neighbor, integer scaling only
- Orthogonal top-down interiors; no isometric or fake-3D room geometry
- Prefer modular/reusable assets over baked scenes
- **Normal first, wrong second**
- Tone progresses gradually: **calm → unease → suspense → sparse horror**
- Red is reserved for meaningful emergency/story states

SOLITUDE rules override conflicting assumptions from dependency skills.

## Reference Loading

Load only what the task requires.

- `references/art-direction.md` — visual identity, palette, materials,
  architecture, lighting, props, and environment design.
- `references/pixel-pipeline.md` — dimensions, perspective, walls, scaling,
  transparency, sprite sheets, animation alignment, and pixel QC.
- `references/unity-pipeline.md` — import settings, pivots, Tilemaps, prefabs,
  scene integration, and gameplay-scale validation.

Read multiple references when the task spans those concerns.

# Workflow

## 1. Inspect First

Determine the asset's gameplay purpose, location, scale, required states, tiling
or animation needs, and interaction constraints.

Search existing project art before creating anything. Classify relevant assets as
`KEEP`, `MODIFY`, `REPLACE`, or `MISSING`.

Prefer extending approved artwork over creating parallel versions.

## 2. Define the Minimum Asset Family

Create the smallest reusable set that satisfies the gameplay need.

Prefer:

`BASE + STATE + OPTIONAL OVERLAY`

over unrelated baked variants.

Do not generate speculative asset libraries.

## 3. Route to the Smallest Appropriate Skill

### `$generate2dsprite`

Use for actual production artwork:

- characters and animations,
- props and pickups,
- interactables, doors, and terminals,
- wall/floor sprites,
- individual tiles and tile variations,
- environmental decoration,
- sprite sheets.

Use it for tile artwork even when that artwork will later be placed in a Tilemap.

### `$generate2dmap`

Use only when spatial composition is required:

- room/corridor layouts,
- Tilemap planning,
- reusable room chunks,
- layered map composition,
- arranging an existing asset vocabulary,
- map/collision/runtime layout metadata.

Do not invoke it for isolated assets or small tile families.

`$generate2dmap` consumes the asset vocabulary; it is not the default generator
of that vocabulary.

## 4. Generate for Game Use

Generate at production intent, specifying the relevant purpose, native scale,
perspective, visual family, required states, palette constraints, and
transparency requirements.

Do not substitute concept illustrations for production sprites.

Simplify details that do not survive gameplay scale.

## 5. Process Deterministically

Allowed cleanup includes:

- cropping and alpha cleanup,
- grid slicing and sheet assembly,
- nearest-neighbor integer resizing,
- anchor alignment,
- palette/transparency checks.

Never use smoothing resampling or fractional scaling for production pixel art.

Prefer native alpha; if chroma key is required, reject visible edge fringing.

## 6. Integrate Without Rebuilding Systems

When Unity integration is required, read `references/unity-pipeline.md`.

Follow existing project structure and gameplay architecture. Do not create
duplicate asset hierarchies, Tilemap systems, prefabs, or interaction systems.

Fix incorrect source art rather than compensating with arbitrary Transform scale.

## 7. Validate In Game

Validate meaningful new assets in Unity, in the target scene or ArtValidation
scene, beside the player and approved assets, at normal gameplay zoom.

Reject or revise work that fails:

- pixel integrity or grid alignment,
- SOLITUDE perspective,
- gameplay readability,
- consistency with existing ship construction,
- required state readability,
- appropriate narrative tone.

If it looks good alone but wrong in the game, it is wrong.

# Current Priority

Until explicitly changed, prioritize the opening vertical slice:

**Cryobay → service/corridor → locker/battery → powered door → first AI contact**

Finish this reusable visual vocabulary before broadly expanding into other ship
areas.

# Completion

An art task is complete when the existing library was checked, the minimum
reusable asset family exists, relevant references were followed, source art is
technically valid, required gameplay states exist, Unity integration is correct
when applicable, and the result has been reviewed successfully at gameplay scale.