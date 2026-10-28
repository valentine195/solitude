# SOLITUDE standard floor — native 256×256

The supplied 1254×1254 prototype is retained unmodified in `Reference/prototype_floor.png`. This is a native PXL interpretation of its broad center, clipped inner corners, shallow outer frame strips and corner junctions. The prototype was used as a visual guide, not fractionally resized into production pixels.

Main floor surface is exactly **#EEF1EF**, matching the standard wall. All seven output colors come from the existing standard wall's neutral ramp. No new palette colors, random noise, scratches, fasteners, decorative marks, gradients, smoothing or antialiasing were added. Depth is concentrated at the frame and inset panel outline. The central walking surface remains flat and quiet. Exposed upper/left frame edges are lighter; the inset's contact shadow and lower/right return distinguish the shallow recess without broad lighting bands.

## Files

- `out/sprites/floor_standard_256.png`: native 256×256 PNG.
- `floor.px.js`: editable procedural PXL source.
- `Editable/floor_standard_256.aseprite`: editable document with three named layers: surface, perimeter_frame, inset_panel.
- `out/sprites/floor_standard_256/*.png`: individual layers.
- `out/scenes/floor_repeat_3x3.png`: native 768×768 repeat test.
- `out/scenes/floor_horizontal_3.png`: native horizontal strip.
- `out/_preview`: PXL 4× previews.
- `validation.json`: machine-readable pixel, palette, seam and editable-export checks.

Review files are under `work/Reviews/PxlStandardFloor256`: `review_sheet.png`, clean 2×/4× nearest-neighbor views, `floor_repeat_3x3_native.png`, PXL ASCII inspection and the complete `floor_standard_256.zip` package.

## Validation

Native, 4× and 3×3 views were visually inspected. Automated checks pass:

- Exact 256×256 canvas: eight by eight 32px world cells.
- Fully opaque PNG; layer alpha restricted to 0/255.
- Central face and wall face match #EEF1EF exactly.
- Every output color belongs to the wall palette.
- Zero left/right edge mismatches and zero top/bottom edge mismatches.
- All nine cells of the repetition test equal the native tile exactly.
- Layer composition equals the flattened PNG.
- The saved Aseprite file is reopened and renders pixel-exactly to the PXL PNG.

The framed panel rhythm remains visible by design when repeated; matching edge pixels does not erase its intentional panel divisions. Final in-game readability and camera scale are not yet assessed. Nothing was imported into or changed in Unity, and all earlier assets remain untouched.

## Rebuild

From the repository root:

```sh
python3 Tools/ArtSources/SOLITUDE/PxlStandardFloor256/build.py
```

Uses installed PXL, Python/Pillow and the existing Aseprite CLI. Override the executable with `ASEPRITE_BIN` if necessary. Layer conversion to RGBA is lossless and supports the editable RGB export. Art is generated natively; review-only enlargements use integer nearest-neighbor. PXL remains the source of truth: rebuilding replaces generated exports and does not incorporate manual Aseprite edits.
