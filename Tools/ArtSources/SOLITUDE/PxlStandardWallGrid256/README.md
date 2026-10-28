# SOLITUDE standard wall — supplied reference on a native 256×256 grid

The latest supplied 1254×1254 JPEG is retained read-only at `Reference/supplied_wall.jpg`. This version adapts the existing PXL wall vocabulary to that reference's layout, directly authoring geometry at 256×256 rather than shrinking/resampling the JPEG into production art.

Preserved features: pale upper and lower wall panels, straight inset vertical bands, small paired side-channel recesses, recessed teal upper channel and painted cover, continuous horizontal teal stripe, chamfered panel outlines and lower plinth joint. The small chamfers are panel-outline geometry, not separate corner brackets. No fasteners, vents or decorative details were added. Broad material planes are quiet off-white; the blue-gray shadow ramp is reserved for recesses and outlines.

A fixed 14-color palette eliminates JPEG color noise. The face remains #EEF1EF; coordinated steel/teal ramps match the new supplied reference. All source coordinates are integers, opacity is hard 0/255, and source art has no smoothing, glow or resampling. Separate layered PNGs compose back exactly to the flattened output.

## Deliverables

- `out/sprites/standard_wall_256.png`: native 256×256 production PNG, eight by eight 32px world cells.
- `wall.px.js`: editable PXL source.
- `Editable/standard_wall_256.aseprite`: five named editable layers.
- `out/sprites/standard_wall_256/*.png`: material, panel breaks, upper recess, outlines and stripe layers.
- `out/scenes/three_panels.png`: native 768×256 three-panel repetition.
- `out/scenes/grid_3x3.png`: broader inspection arrangement; vertical connection is not intended because top channel and lower plinth terminate the wall.
- `out/_preview`: PXL integer-scale previews.
- `validation.json`: machine-readable checks and reference hash.

Review material is outside Assets under `work/Reviews/PxlStandardWallGrid256`: `review_sheet.png`, clean 2×/4× nearest-neighbor enlargements, native/4× triple repeats and PXL ASCII inspection. Native, 4× and repeated arrangements were visually inspected.

Programmatic checks pass for native dimensions, 32px grid divisibility, palette-only colors, full opacity, hard layer alpha, horizontal edge equality, a continuous full-width stripe, exact repetitions, exact layer composition and exact rendering after reopening the saved Aseprite file.

Rebuild with `python3 Tools/ArtSources/SOLITUDE/PxlStandardWallGrid256/build.py` from the repository root. Requires installed PXL, Python/Pillow and the existing Aseprite CLI; override the executable with `ASEPRITE_BIN` if needed. Lossless RGBA conversion supports the layered Aseprite handoff. PXL remains the generation source of truth.

All earlier assets are preserved. Nothing was imported into Unity.
