# SOLITUDE Pixel Pipeline

This document defines the technical production rules for SOLITUDE pixel art.

Use it when creating, resizing, slicing, assembling, cleaning, or validating:

- tiles,
- environment sprites,
- props,
- characters,
- animation frames,
- sprite sheets.

For visual style, palette, materials, and environment identity, use `art-direction.md`.

This document ends when the image asset is technically production-ready. Unity import and integration belong to `unity-pipeline.md`.

---

# 1. Native Scale

SOLITUDE uses a **32 px world grid**.

Standard environment tile:

`32 × 32 px`

Multi-tile assets should use integer multiples of that grid where practical:

- 2×1 → `64 × 32`
- 2×2 → `64 × 64`
- 3×2 → `96 × 64`

Player target:

`approximately 32 × 48 px`

Do not enlarge source dimensions merely to gain drawing resolution. Simplify the design if it does not read at gameplay scale.

---

# 2. Pixel Integrity

Production art must use true pixel-art edges.

Required:

- hard pixel boundaries,
- no antialiasing,
- no blurred transparency,
- no subpixel detail,
- consistent pixel density.

Only use **nearest-neighbor** resizing.

Only use **integer scaling** for production pixel art.

Never use smoothing resampling such as:

- Lanczos,
- bilinear,
- bicubic.

If correct output would require fractional scaling, regenerate or redraw the source instead.

---

# 3. Production Resolution

Prefer:

`generate near target size → clean → validate`

over:

`generate large illustration → shrink aggressively`

Concept art may be high resolution, but production assets must be intentionally designed for their actual game scale.

Important gameplay information must survive normal camera zoom.

---

# 4. Perspective

SOLITUDE uses a flat orthogonal top-down interior perspective.

Do not introduce:

- isometric projection,
- perspective convergence,
- 3D cutaway-box geometry,
- faux-3D room extrusion.

Objects must agree with the same room perspective.

## Room Boundaries

North wall:

- may show a visible vertical face,
- approximately two tiles high unless otherwise specified.

West, east, and south boundaries:

- primarily read as wall caps / upper boundaries,
- do not become full visible wall faces.

Use:

- square corners,
- flat reusable geometry,
- consistent wall thickness.

Avoid:

- bevels,
- chamfers,
- diagonal extrusion,
- perspective wedges.

---

# 5. Tile Construction

Environment kits should use reusable tile families.

A base architectural family may include:

- floor,
- north wall,
- wall cap,
- corners,
- transitions,
- thresholds,
- door interfaces,
- structural pieces.

Do not bake entire rooms into single images when reusable tiles can represent them.

Large unique objects may exist as separate sprites layered over the Tilemap.

---

# 6. Seam Validation

Any repeating tile must be tested in repetition.

Test at least a `3 × 3` arrangement.

Reject tiles with:

- visible seams,
- accidental stripes,
- lighting discontinuities,
- obvious repeating landmarks,
- mismatched edge pixels.

Technically matching edges are not enough; repetition should remain visually unobtrusive.

---

# 7. Detail Hierarchy

Prioritize:

1. silhouette,
2. major shape divisions,
3. value/color blocks,
4. functional state,
5. secondary detail.

Do not rely on micro-detail for essential meaning.

If a feature is only understandable while zoomed into the PNG, it should not carry gameplay information.

---

# 8. Transparency

Standalone sprites should normally use true alpha.

Reject:

- visible halos,
- chroma residue,
- softened transparency edges.

If chroma key is required:

- use a color outside the SOLITUDE palette,
- remove it deterministically,
- inspect resulting edge pixels.

Prefer native alpha when reliable.

---

# 9. Frames, Cropping, and Anchors

Do not crop away transparent margins required for stable animation or shared pivots.

Related frames should use consistent dimensions.

## Characters

Anchor movement around **feet / floor contact**.

Across frames:

- foot position remains stable,
- body scale remains stable,
- transparent frame bounds remain consistent.

## Props

Use a predictable anchor based on world placement:

- floor objects → footprint or bottom-center,
- wall objects → attachment point,
- pickups → established world-center/floor convention.

Do not repair inconsistent source alignment later with arbitrary per-instance offsets.

---

# 10. Sprite Sheets

Sprite sheets must use an explicit deterministic grid.

Support independent:

`cell_width`

and:

`cell_height`

Do not assume square cells.

Player sheets will commonly use approximately:

`32 × 48 px`

unless the asset requires a documented larger frame.

Automatic slicing should not replace known grid dimensions.

---

# 11. Animation Consistency

Animation frames must preserve:

- character/object proportions,
- palette,
- equipment shape,
- anchor,
- lighting treatment.

Use the minimum number of frames needed for readable motion.

Check for:

- foot sliding,
- frame jitter,
- scale drift,
- changing costume/equipment details,
- pixel shimmer.

Do not independently redesign each animation frame.

---

# 12. Lighting and Shadows

Keep reusable source art broadly compatible with different environment lighting states.

Avoid baking broad room lighting into reusable sprites.

Small contact shadows are acceptable if they are part of the established art style.

Avoid soft blurred shadows.

Lighting-state changes should usually be handled downstream rather than by repainting every base sprite.

---

# 13. Variations and Damage

Prefer:

`BASE + OPTIONAL VARIANT / OVERLAY`

Examples:

- clean wall + scuff overlay,
- base panel + electrical damage overlay,
- terminal base + state variant.

Do not bake wear, damage, or narrative corruption into every base asset.

---

# 14. Deterministic Processing

Allowed post-generation processing includes:

- cropping,
- alpha cleanup,
- chroma removal,
- nearest-neighbor integer scaling,
- grid slicing,
- sprite-sheet assembly,
- anchor alignment,
- palette checks,
- transparency checks,
- duplicate-frame detection.

Processing should make generated art technically usable, not stylistically redraw it.

Do not assume generated images exist at a hard-coded path. Use the actual path returned or materialized by the current environment.

---

# 15. Pixel QC

Inspect assets at:

- native size,
- integer zoom such as 2× / 4× / 8×,
- expected gameplay scale.

Reject or repair assets with:

- antialiased or blurred edges,
- fractional scaling artifacts,
- transparency fringe,
- inconsistent pixel density,
- incompatible perspective,
- unstable animation alignment,
- obvious tile seams,
- essential details unreadable at gameplay scale.

If resizing or cleanup unexpectedly introduces many new colors, investigate before accepting the result.

---

# 16. Completion

A source asset is ready for Unity when:

- dimensions fit the 32 px world grid or documented frame size,
- pixels remain crisp and unsmoothed,
- no fractional scaling was used,
- transparency is clean,
- perspective is correct,
- anchors are stable,
- repeating tiles repeat cleanly,
- animations do not drift,
- gameplay-critical details remain readable.

Do not hide source-art problems downstream.

Once these conditions are satisfied, continue with `unity-pipeline.md`.