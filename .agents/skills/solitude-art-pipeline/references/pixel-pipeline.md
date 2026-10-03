# SOLITUDE Pixel Pipeline

This document defines the technical pixel-art production rules for SOLITUDE.

Use it whenever creating, resizing, slicing, assembling, cleaning, or evaluating:

- tiles,
- sprites,
- props,
- characters,
- animation frames,
- sprite sheets,
- environment assets.

For visual style, palette, materials, and environment identity, use `art-direction.md`.

For Unity import settings and in-game integration, use `unity-pipeline.md`.

---

# 1. Native Scale

SOLITUDE uses a **32 px world grid**.

## Environment Tiles

Standard tile:

`32 × 32 px`

Environment geometry should align to this grid unless an asset intentionally spans multiple tiles.

Examples:

- 1×1 prop footprint: `32 × 32`
- 2×1 object: `64 × 32`
- 2×2 object: `64 × 64`
- 3×2 machine: `96 × 64`

Large assets should still be designed as integer multiples of the 32 px grid whenever practical.

---

## Player Character

Target character frame:

`approximately 32 × 48 px`

The player may use transparent space within the frame.

Do not arbitrarily enlarge the frame to gain more drawing resolution.

If the character cannot read clearly at this scale, simplify the design.

---

# 2. Pixel Integrity

Production art must use true pixel-art edges.

Required:

- hard pixel boundaries,
- no antialiasing,
- no subpixel positioning,
- no smoothing filters,
- no blurred transparency edges.

One source pixel must remain a discrete visual unit.

Do not treat generated artwork as production-ready until these conditions are verified.

---

# 3. Scaling

Only use **nearest-neighbor scaling**.

Scaling must use integer multiples whenever resizing pixel artwork.

Allowed examples:

- 16 → 32 px: `2×`
- 32 → 64 px: `2×`
- 32 → 96 px: `3×`

Avoid:

- 32 → 48 px
- 48 → 64 px
- arbitrary percentage scaling.

Never use:

- Lanczos,
- bicubic,
- bilinear,
- smoothing interpolation.

If an asset is generated at an unsuitable resolution, prefer regenerating it at a better source scale rather than repeatedly resampling it.

---

# 4. Production Resolution

Whenever possible, create assets at their intended native game resolution.

Preferred workflow:

`generate near target resolution → clean → validate`

rather than:

`generate very large illustration → shrink aggressively → repair`

Large generated references may be useful for concept development, but should not automatically become production assets.

Production assets must be redesigned for gameplay-scale readability.

---

# 5. Perspective

SOLITUDE uses a flat orthogonal top-down interior perspective.

It is not:

- isometric,
- perspective-projected,
- a 3D cutaway,
- faux-3D interior rendering.

Objects must visually agree with the room perspective.

Avoid:

- diagonal perspective convergence,
- visible side faces that imply a 3D camera,
- perspective scaling across a sprite,
- beveled room-box geometry.

---

# 6. Room Geometry

## North Wall

The north wall may show a visible vertical wall face.

Current target:

`approximately 2 tiles high`

Its exact visible construction may vary by environment family, but it must remain compatible with the 32 px grid.

---

## West / East / South Boundaries

These should primarily read as the **top/cap surface of the wall**.

They should not become full vertical wall faces.

They should visually represent the same wall construction continuing around the room perimeter.

Use:

- simple flat geometry,
- square corners,
- consistent wall thickness.

Avoid:

- extruded side walls,
- bevels,
- chamfers,
- angled corner geometry,
- cutaway-box appearance.

---

# 7. Tile Construction

Build environment kits from reusable tile families.

A base architectural family should generally consider:

- floor,
- north wall face,
- wall cap,
- inside corners,
- required transitions,
- door transitions,
- edge or threshold pieces.

Do not generate an entire room as one baked image when reusable tiles can represent it.

Large decorative elements may exist as separate sprites layered over the base tilemap.

---

# 8. Tile Seam Validation

Any tile intended to repeat must be tested in repetition.

Validate at minimum:

`3 × 3`

for floor or surface tiles.

Check for:

- obvious seams,
- accidental repeating stripes,
- lighting discontinuities,
- texture features that create a visible grid,
- mismatched edge pixels.

A tile is not considered seamless merely because its left and right edges technically connect.

The repetition should remain visually unobtrusive.

---

# 9. Visual Detail at Pixel Scale

Prioritize detail in this order:

1. silhouette,
2. major shape divisions,
3. major value/color blocks,
4. functional state,
5. secondary detail,
6. micro-detail.

Do not use individual pixels to simulate detail that disappears at gameplay scale.

If a feature cannot be understood without zooming into the PNG, it should not carry important gameplay information.

---

# 10. Transparency

Independent sprites should normally use true alpha transparency.

Transparent pixels should contain no visible halo or fringe.

Check especially around:

- character outlines,
- tools,
- cables,
- thin machinery,
- rounded props.

---

## Chroma-Key Fallback

If generation requires a temporary chroma background:

- use a color outside the SOLITUDE palette,
- use a uniform background,
- remove it deterministically,
- inspect edge pixels afterward.

Bright magenta is acceptable because it is not part of the normal SOLITUDE palette.

Reject outputs that retain visible chroma-colored fringe.

Prefer native alpha generation when reliable.

---

# 11. Cropping

Crop assets deterministically.

Do not crop away intentional transparent margins required for:

- animation alignment,
- shared pivots,
- consistent sprite-sheet cells,
- effects extending beyond the visible body.

For asset families, preserve consistent frame dimensions where consistency matters more than minimum bounding-box size.

---

# 12. Anchors and Pivots

Related sprites must use stable visual anchors.

## Characters

Primary anchor:

**feet / floor contact**

Across animation frames:

- feet should remain consistently aligned,
- body position should not jitter because of inconsistent cropping,
- transparent frame size should remain stable.

## Props

Anchor should reflect how the object occupies the world.

Examples:

- floor object → bottom-center or footprint-based anchor,
- wall object → grid-relative wall attachment point,
- pickup → consistent world-center or floor-contact convention.

Do not solve inconsistent source alignment later with arbitrary per-instance offsets.

---

# 13. Sprite Sheets

Sprite sheets must use a deterministic grid.

Each cell must have:

- known width,
- known height,
- consistent alignment.

Do not assume cells are square.

The pipeline must support explicit:

`cell_width`

and:

`cell_height`

For the player, a likely starting frame cell is:

`32 × 48 px`

unless animation content requires a documented larger cell.

---

# 14. Animation

Animation should preserve the same:

- proportions,
- palette,
- outline behavior,
- lighting direction,
- equipment placement,
- anchor.

Avoid generation workflows where each animation frame independently redesigns the character.

Generate animation families with consistency as a primary constraint.

---

## Frame Count

Use the minimum frame count needed to communicate motion clearly.

More frames are not automatically better.

At SOLITUDE's resolution, strong key poses are often more valuable than highly interpolated motion.

---

## Animation QC

Check animation for:

- foot sliding,
- scale drift,
- head/body size drift,
- changing costume details,
- changing equipment shape,
- unintended pixel shimmer,
- inconsistent shadows.

---

# 15. Shadows

Use shadows consistently.

Do not bake large directional environmental shadows into reusable base sprites unless required by the game's rendering approach.

Prefer sprites that remain compatible with multiple placements.

Small contact shadows may be appropriate when they are part of the established project style.

Avoid soft blurred shadows.

---

# 16. Lighting in Source Assets

Separate intrinsic object color from environmental lighting where practical.

A base locker should generally remain a locker.

Do not create separate fully repainted versions for:

- normal lighting,
- partial power,
- emergency lighting,

unless the lighting change requires unique baked artwork.

Prefer environment lighting or controlled overlays for broad lighting-state changes.

---

# 17. Damage and Variation

Do not bake wear and damage into every base tile.

Preferred model:

`base asset + optional variation or overlay`

Examples:

- clean wall,
- scuffed wall variation,
- damaged overlay,
- electrical scorch overlay.

This keeps the visual vocabulary reusable and supports gradual narrative deterioration.

---

# 18. Palette Discipline

The exact visual palette is defined by `art-direction.md`.

At the pixel-processing level:

- avoid introducing unnecessary intermediate colors,
- avoid interpolation-created colors,
- avoid accidental gradients,
- avoid edge colors created only by antialiasing.

If resizing or cleanup unexpectedly increases the color count, investigate before accepting the asset.

---

# 19. Generated Asset Cleanup

A generated asset may require cleanup before becoming production art.

Allowed deterministic operations include:

- cropping,
- nearest-neighbor resizing,
- palette cleanup,
- alpha cleanup,
- chroma removal,
- sprite-sheet slicing,
- sprite-sheet assembly,
- anchor alignment,
- duplicate-frame detection.

Do not use automated processing that materially redraws the asset unless explicitly intended.

Generation should produce the art.

Processing should make it technically usable.

---

# 20. Asset QC

Before accepting a pixel asset, inspect it at:

### Native size

Check actual pixels and edge quality.

### Integer zoom

Use integer zoom levels such as:

`2×`, `4×`, or `8×`

for inspection.

Do not evaluate pixel quality using smoothed image viewers.

### Gameplay scale

Always inspect the asset at approximately the size the player will actually see it.

An asset that looks excellent at 800% zoom but unreadable in-game has failed.

---

# 21. Automatic Failure Conditions

Reject or repair an asset if any of the following are present:

- antialiased edges,
- blurred pixels,
- fractional scaling artifacts,
- unintended transparency fringe,
- inconsistent pixel density,
- perspective incompatible with the environment,
- animation frame jitter,
- inconsistent frame dimensions,
- obvious tile seams,
- important features unreadable at gameplay scale,
- accidental color proliferation caused by processing.

Do not accept an asset merely because its overall concept is good.

---

# 22. Generated Image Paths

Do not assume image-generation output exists at a hard-coded global path.

Use the actual path returned or materialized by the current execution environment.

Do not depend on paths such as:

`$CODEX_HOME/generated_images`

unless the environment explicitly confirms that location.

Once an image becomes part of the project, move or copy it into the appropriate project-controlled asset location.

---

# 23. Source and Derived Assets

When practical, preserve:

- original generated source,
- cleaned production sprite,
- assembled sheet or atlas if applicable.

Do not repeatedly process an already processed asset if the original source is available.

Prefer:

`source → production`

over:

`source → processed → processed again → processed again`

Repeated transformations increase the risk of pixel degradation and inconsistencies.

---

# 24. Handoff to Unity

This document stops at production-ready image assets.

Once an asset is technically valid, follow `unity-pipeline.md` for:

- file placement,
- import settings,
- sprite slicing,
- Pixels Per Unit,
- pivots,
- Tilemap integration,
- prefab integration,
- gameplay validation.

Do not encode Unity-specific corrections into the source artwork unless the artwork itself is incorrect.

---

# 25. Completion Check

A pixel asset is production-ready when:

- dimensions are appropriate for the 32 px world grid,
- pixel edges are crisp,
- no antialiasing is present,
- no non-integer scaling was used,
- transparency is clean,
- perspective is correct,
- anchors are stable,
- repeating tiles repeat cleanly,
- animations do not jitter or drift,
- important details remain readable at gameplay scale,
- and the asset is ready for Unity import without corrective Transform scaling.

When uncertain, regenerate or simplify rather than hide technical problems downstream.