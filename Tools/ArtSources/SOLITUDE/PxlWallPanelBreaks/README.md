# Standard wall — panel breaks, not architectural corner fittings

Non-destructive correction of `../PxlStandardWallRefined`. The attached reference clarifies that the inset vertical bands and lower line describe the edges of a separate cladding panel. They do not flare outward into corner brackets or structural columns.

The restored vertical bands sit at the original inset positions (x=20–23 and x=232–235). They extend straight up to the recessed architectural channel and straight down to a square horizontal break at y=240–243. The bands have a fixed shallow four-pixel profile rather than a wide framed rail. All previous upper corner brackets, tapered side connections, lower corner plates and irregular corner tips are removed. The four existing small flush fasteners remain; none were added.

The center of the upper recessed channel (x=32–223, y=0–21) remains pixel-identical to the previous pass. Its corner ornament is deliberately removed. The #EEF1EF face, original teal palette, stripe position and continuous horizontal stripe remain unchanged. Shadows use crisp native pixel bands with no antialiasing.

## Outputs

- `wall_2.px.js`: revised editable PXL source.
- `out/sprites/standard_wall_panel_breaks.png`: native 256×256 PNG.
- `Editable/standard_wall_panel_breaks.aseprite`: five named editable layers.
- `out/sprites/standard_wall_panel_breaks/*.png`: individual layers.
- `out/scenes/horizontal_repeat.png`: 768×256 three-panel strip.
- `work/Reviews/PxlWallPanelBreaks/comparison_sheet.png`: original, prior version and corrected panel breaks with native repeats.
- `work/Reviews/PxlWallPanelBreaks/standard_wall_panel_breaks_4x.png`: 1024×1024 nearest-neighbor enlargement.
- `work/Reviews/PxlWallPanelBreaks/three_panels_native.png` and `three_panels_4x.png`: repetition review.
- `work/Reviews/PxlWallPanelBreaks/standard_wall_panel_breaks.zip`: complete package.

## Validation

Native repeat and 4× enlargement were visually inspected. Checks pass for exact dimensions, opacity, hard alpha layers, unchanged palette, flat off-white faces, unchanged upper-channel core, and straight constant-profile vertical breaks. The upper-recess layer ends at row 21, proving no corner ornaments extend down into the face. Horizontal joining columns match and the stripe is constant across every row of its full width. The layered Aseprite file is saved, reopened and rendered back exactly to the PNG.

All previous sources and Unity assets remain unchanged. No Unity import was performed.

Rebuild from the repository root with `python3 Tools/ArtSources/SOLITUDE/PxlWallPanelBreaks/build.py`. Uses installed PXL, Python/Pillow and the existing Aseprite CLI; `ASEPRITE_BIN` can override its path. Native art is never resized. Review enlargement is exact 4× nearest-neighbor. Manual Aseprite edits are not transferred back to PXL.
