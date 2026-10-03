---
name: solitude-art-pipeline
description: >
  Production art pipeline for SOLITUDE, a top-down 2D pixel-art game set aboard
  a generation ship. Use when creating, modifying, processing, integrating, or
  validating environment tiles, props, characters, doors, interactables,
  environmental storytelling assets, animations, or visual-state variants.
  This skill coordinates SOLITUDE-specific art direction, pixel-pipeline
  constraints, reusable asset design, lower-level generation skills, and Unity
  validation.
---

# SOLITUDE Art Pipeline

Use this skill for production art work in SOLITUDE.

The goal is not to maximize asset count or generate attractive standalone images.

Optimize for:

1. visual consistency,
2. gameplay readability,
3. reuse and modularity,
4. native pixel quality,
5. compatibility with the existing Unity project.

Generated artwork is not complete until it works inside the game.

---

## Core Invariants

These rules apply to all SOLITUDE production art unless explicitly overridden.

- World tile grid: **32×32 px**
- Player target: **approximately 32×48 px**
- Native pixel art only
- No antialiasing
- Nearest-neighbor and integer scaling only
- Orthogonal top-down interiors
- No isometric perspective or fake 3D room geometry
- Prefer reusable modular assets over baked scenes
- The ship should feel **normal and inhabited before it feels wrong**
- Visual progression is gradual: **calm → unease → suspense → sparse horror**
- Red is a rare emergency/story signal, not a default environmental color

Project-specific rules defined by this skill and its references override generic
styling assumptions from lower-level generation skills.

---

## Supporting References

Load only the references relevant to the current task.

### `references/art-direction.md`

Read for:

- visual design,
- palette and materials,
- environment identity,
- generation-ship architecture,
- tonal progression,
- environmental storytelling,
- human evidence,
- damage philosophy,
- normal / neglected / failing / critical states.

Read this before any task that makes visual design decisions.

### `references/pixel-pipeline.md`

Read for:

- sprite or tile dimensions,
- room perspective,
- wall construction,
- pixel density,
- transparency,
- scaling,
- sprite-sheet layout,
- anchors,
- animation frames,
- deterministic cleanup,
- pixel-art QC.

Read this before generating or processing any pixel asset.

### `references/unity-pipeline.md`

Read for:

- Unity import settings,
- PPU and filtering,
- sprite slicing,
- pivots,
- Tilemap integration,
- asset directories,
- prefab or scene integration,
- ArtValidation scene usage,
- gameplay-scale visual validation.

Read this whenever the task includes importing, placing, configuring, or
validating art inside Unity.

If a task spans multiple areas, read every applicable reference.

Do not load unrelated references merely because they exist.

---

# Workflow

Follow this workflow for every production-art task.

## 1. Inspect Before Creating

Understand the gameplay need first.

Determine:

- where the asset appears,
- what the player does with it,
- intended gameplay scale,
- required states,
- required dimensions,
- whether it tiles,
- whether it animates,
- whether collision or interaction behavior affects its design.

Search the existing project before generating new artwork.

Classify relevant assets as:

- `KEEP`
- `MODIFY`
- `REPLACE`
- `MISSING`

Prefer extending existing approved assets over creating parallel versions.

Do not replace working artwork without a reason.

---

## 2. Define the Minimum Asset Family

Determine the smallest reusable set required to solve the gameplay need.

Prefer:

`base asset + state variants + optional overlays`

over:

`many unrelated unique assets`

Example:

A powered door may require:

- frame,
- closed state,
- open state,
- unpowered state,
- indicator variation.

It does not require a large decorative door library unless gameplay needs one.

---

## 3. Choose the Appropriate Generation Tool

Use lower-level skills when available.

Use `$generate2dsprite` for:

- props,
- characters,
- pickups,
- interactables,
- individual environment sprites,
- animation frames,
- sprite sheets.

Use `$generate2dmap` for:

- tile families,
- room layouts,
- reusable environment kits,
- map composition,
- tile-based environment planning.

Apply SOLITUDE's references in addition to the dependency skill.

Do not inherit generic visual styles, palettes, dimensions, or genre assumptions
from dependency skills when they conflict with SOLITUDE.

---

## 4. Generate at Production Intent

Generate assets for their actual game use.

Prompts or generation instructions must communicate the relevant:

- asset purpose,
- native scale,
- viewing perspective,
- visual family,
- required states,
- palette constraints,
- transparency/background requirements.

Do not request large illustrative concept art when the desired result is a
production sprite or tile.

When a generated result contains extra decorative detail that does not survive at
gameplay scale, simplify it.

---

## 5. Process Deterministically

After generation, only use deterministic processing for production cleanup.

Allowed operations include:

- cropping,
- alpha cleanup,
- grid slicing,
- sprite-sheet assembly,
- nearest-neighbor integer resizing,
- anchor alignment,
- palette checks,
- transparency checks.

Do not use smoothing resampling for pixel assets.

Never use:

- Lanczos,
- bilinear,
- bicubic,
- fractional scaling.

If chroma-key transparency is required, validate the final alpha edge for color
fringing.

Prefer native transparency when reliable.

---

## 6. Integrate Into the Existing Project

When integration is part of the task, read `references/unity-pipeline.md`.

Follow existing project organization and conventions where reasonable.

Do not create:

- redundant art directories,
- duplicate Tile Palette systems,
- parallel prefab systems,
- arbitrary Transform scaling to compensate for bad source artwork.

Fix the source asset rather than hiding inconsistencies inside Unity.

Preserve existing gameplay systems unless art integration genuinely requires a
small supporting change.

---

## 7. Validate In Game

A PNG is not an accepted game asset.

Validate meaningful new assets:

- inside Unity,
- in the real target scene or ArtValidation scene,
- beside the player,
- beside approved existing assets,
- at normal gameplay camera scale.

Evaluate:

### Pixel Integrity

- crisp native pixels,
- no smoothing,
- correct dimensions,
- correct grid alignment.

### Perspective

- correct SOLITUDE room perspective,
- no accidental isometric or fake-3D geometry.

### Readability

- clear silhouette,
- understandable gameplay function,
- states distinguishable at normal zoom.

### Visual Consistency

Ask:

> Does this look like it was built aboard the same generation ship as the
> existing approved assets?

### Tone

Ask:

> Is this asset appropriate for where the player is in the game's progression?

Early-game assets should not accidentally communicate late-game horror.

If an asset looks impressive in isolation but wrong in the game, revise it.

---

# Asset Design Rules

Prefer composable assets.

Use:

`BASE + STATE + OVERLAY`

where appropriate.

Examples:

- clean wall + wear overlay,
- door base + powered/unpowered indicator,
- terminal base + active/error state,
- room base + localized damage.

Avoid baking dirt, damage, warning lights, or story-specific corruption into
every base asset.

Damage should communicate a plausible cause rather than function as random visual
noise.

---

# Current Production Priority

Until explicitly changed, prioritize the opening vertical slice:

**Cryobay → service/corridor area → locker/battery → powered door → first AI contact**

Prioritize assets required to make this sequence visually coherent before
expanding broadly into unrelated ship environments.

Prefer finishing one reusable visual vocabulary over partially generating many
biomes.

---

# Completion Gate

An art task is complete only when all applicable conditions are satisfied:

- existing assets were inspected first,
- the minimum required reusable asset family exists,
- the relevant reference files were followed,
- output obeys native pixel constraints,
- perspective and scale are correct,
- required gameplay states are represented,
- assets are placed in the correct project structure,
- Unity import settings are correct when applicable,
- the result has been reviewed at gameplay scale,
- the new work visually belongs to SOLITUDE.

If any required condition fails, revise the asset rather than marking the task
complete.