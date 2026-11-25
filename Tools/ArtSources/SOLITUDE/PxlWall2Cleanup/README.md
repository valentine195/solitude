# Wall 2 — PXL cleanup

Reference: `Assets/AI_Sprites/Walls_2/Wall-2.png`, copied read-only into `Reference/wall_2_original.png`. The original asset was not changed or reimported.

This is a reference-guided native redraw in PXL, not a pixel-exact trace. It retains the actual source dimensions (256×256), broad upper/lower panel proportions, recessed upper rail, side channels, corner joints, blue band at rows 158–175, and lower structural return. Geometry and construction marks are regularized; light favors upper-left exposed surfaces. Inner occlusion and lighter return surfaces preserve the recess depth. Ten closely spaced material tones preserve broad face shading while removing random tonal speckle. The complete output uses 22 colors rather than the source's 5,984 RGBA values.

The reference has one isolated fully transparent pixel. The cleaned wall fills it and is fully opaque. No filtering, arbitrary resizing, dithering, random texture or antialiasing is used. Review enlargement is exact 2× or 3× nearest-neighbor.

## Outputs

- `out/sprites/wall_2_clean.png`: cleaned native 256×256 PNG.
- `wall_2.px.js`: editable generation source with separate material, structure, upper recess, band and hardware functions.
- `Editable/wall_2_clean.aseprite`: editable 256×256, five named layers.
- `out/sprites/wall_2_clean/*.png`: individually exported layers with hard alpha.
- `out/scenes/horizontal_repeat.png`: native three-copy strip.
- `out/scenes/repeat_grid.png`: additional 3×3 inspection image. Vertical stacking is not an intended join because header/lower rail geometry terminates the wall.
- `out/_preview`: PXL integer-scale previews.
- `validation.json`: dimensions, alpha, palette, joining edge, layer recomposition, source provenance hash and saved-Aseprite round-trip evidence.
- `work/Reviews/PxlWall2Cleanup/comparison_sheet.png`: before/after at 2× and three repeats at native 1:1.
- `work/Reviews/PxlWall2Cleanup/pixel_inspection.txt`: PXL ASCII inspection.

The preview and comparison sheet were visually inspected. Left/right joining columns match exactly on all 256 rows; the blue band and upper recess rhythm remain continuous in horizontal repetition. Aseprite was reopened after saving and its rendering matches the PXL PNG exactly.

## Rebuild

From the repository root:

```sh
python3 Tools/ArtSources/SOLITUDE/PxlWall2Cleanup/build.py
```

Requires installed PXL, Python/Pillow and the existing Aseprite CLI. Override the latter using `ASEPRITE_BIN`. Indexed PXL PNG layer storage is converted losslessly to RGBA before the editable RGB handoff. Manual edits in the Aseprite export do not transfer back into PXL; rebuilding replaces generated exports.

All generation source, PNG exports and review artifacts remain outside `Assets` and runtime code. No Unity changes were made.
