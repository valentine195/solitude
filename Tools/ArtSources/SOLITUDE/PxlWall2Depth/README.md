# Wall 2 — depth and top-left lighting revision

A separate revision of `../PxlWall2Cleanup`, preserving the prior pass. Native canvas remains 256×256; construction, stripe placement and repeat rhythm are retained. No Unity assets were changed.

Changes:

- Stronger, sharply bounded inner-channel occlusion, especially beneath the left frame lip.
- Brighter exposed upper-left surfaces and illuminated lower/right returns inside recesses.
- Darker upper slot interior under the header lip, with a narrow lit lower return.
- Raised blue band's bright upper edge and darker underside, followed by a short downward shadow.
- Corner faces no longer have mirrored lighting; geometry repeats but illumination follows the same upper-left source.
- Broad face shading now falls monotonically from upper-left toward lower-right.

Shadows use exact native pixel bands. No blur, antialiasing, soft alpha or resizing is applied to source art. Comparison enlargement uses 2x nearest-neighbor. Palette contains 23 colors.

Outputs:

- `out/sprites/wall_2_clean.png`: revised native PNG.
- `Editable/wall_2_clean.aseprite`: five named editable layers.
- `wall_2.px.js`: PXL authoring source.
- `out/sprites/wall_2_clean/*.png`: individual layers.
- `out/scenes/horizontal_repeat.png`: three adjacent native copies.
- `work/Reviews/PxlWall2Depth/comparison_sheet.png`: previous pass beside revised lighting and native repeats.
- `validation.json`: dimension, alpha, palette, composition and Aseprite round-trip checks.

Validation passes: 256×256, fully opaque, authored colors only, zero horizontal joining-column mismatches, exact layer recomposition, and pixel-exact rendering after reopening the saved Aseprite file. Lighting was visually reviewed in PXL previews and the before/after sheet. Vertical repetition is not intended for a wall with terminal header/lower rail geometry.

Rebuild from the repository root with `python3 Tools/ArtSources/SOLITUDE/PxlWall2Depth/build.py`. The script uses installed PXL, Python/Pillow and the existing Aseprite CLI (`ASEPRITE_BIN` can override its location). PXL is the generation source of truth; manual Aseprite edits do not transfer back to PXL.
