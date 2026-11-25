# SOLITUDE standard wall — restrained refinement

A separate, non-destructive revision of `../PxlWall2Depth/wall_2.px.js`. Existing PXL functions and layered construction were edited; the panel was not replaced with a new design. All prior assets and runtime files remain untouched.

## Scoped changes

1. Main material plane is exactly **#EEF1EF**, with no diagonal or broad gray shading. All other output colors belong to the existing PXL palette. The requested off-white is the only palette addition.
2. Depth is confined to the existing upper recess, thin joining edges, lower return and fitting outlines. The entire `upper_recess` layer remains byte-identical to the previous pass.
3. Lower fittings use matched regular chamfered polygons, pale faces and narrow shadows rather than the old irregular dark shapes.
4. The teal stripe retains its position and continuous full-width face, with a restrained upper highlight and darker underside. Previous rail-intersection marks were removed from the stripe.
5. Full-height structural rails were reduced to two native pixels per outer edge: paired panels form a thin four-pixel join instead of repeated broad columns.

No vents, new fasteners, scratches, grime, lighting effects or decorative elements were added. Existing flush fasteners were retained. Canvas remains exactly 256×256 and fully opaque; no baked cap or extra margin was introduced.

## Outputs

- `out/sprites/standard_wall_refined.png`: native PNG.
- `Editable/standard_wall_refined.aseprite`: five named editable layers.
- `wall_2.px.js`: revised PXL source.
- `out/sprites/standard_wall_refined/*.png`: layer PNGs.
- `out/scenes/horizontal_repeat.png`: three horizontally adjacent copies, 768×256.
- `out/_preview`: PXL 4× grid previews.
- `validation.json`: pixel validation and provenance.

Review files in `work/Reviews/PxlStandardWallRefined`:

- `comparison_sheet.png`: original artwork, previous PXL and refined version, each with a three-panel repeat at **native 1:1**.
- `three_panels_native.png`: native 768×256 repeat strip.
- `standard_wall_refined_4x.png`: clean **1024×1024** nearest-neighbor enlargement, without inspection grid.
- `three_panels_4x.png`: **3072×1024** exact nearest-neighbor enlargement of the repeat strip.
- `pixel_inspection.txt`: PXL ASCII inspection.
- `standard_wall_refined.zip`: complete editable source, exports and review package.

## Verification

Native strip, native comparison sheet, PXL preview and clean 4× enlargement were visually inspected. Programmatic checks pass for dimensions, full opacity, hard overlay alpha, existing palette plus the explicit off-white target, flat off-white face regions, byte-identical upper-recess layer, zero horizontal edge mismatches, and a stripe that is constant across the complete width on every stripe row. Saved Aseprite output was reopened and rendered pixel-exactly to the PXL PNG.

Horizontal repetition is the relevant connection. Vertical stacking is not intended for this header/lower-return design. Actual Unity cap composition and gameplay camera appearance remain untested; nothing was imported into Unity.

## Rebuild

From the repository root:

```sh
python3 Tools/ArtSources/SOLITUDE/PxlStandardWallRefined/build.py
```

Uses installed PXL, Python/Pillow and the existing Aseprite CLI. `ASEPRITE_BIN` can override the executable. Layer conversion to RGBA is lossless; native art is never resized or filtered. Review enlargements use exactly 4× nearest-neighbor. PXL is the generation source of truth; rebuilding replaces generated Aseprite exports.
