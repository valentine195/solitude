# SOLITUDE MedBay walls — visual iteration 02

Four native **128×256** full-height wall faces, authored in the installed PXL pipeline. The prior iteration remains intact in `../PxlMedBayWalls`. Nothing was imported into Unity.

## Design

- **Access A:** offset upper maintenance hatch and a small lower inspection door.
- **Access B:** narrow upper inspection recess and paired lower service covers.
- **Digital bedside station:** sealed recessed terminal, a continuous diagnostic trace, grouped information bars, small green/amber status indicators, round oxygen connector, square keyed vacuum connector, and shuttered diagnostic/data connection. The port types differ by shape as well as subdued color. There is no bloom or neon halo.
- **Supply cabinet:** accessible lower twin doors with recessed grips and simple medical inventory marks.

Depth is carried by upper/left exposed lip highlights, darker inner upper/left recess occlusion, and lighter lower/right internal returns. This distinguishes the exterior lip from the recessed interior. Sparse molded channels add architectural detail without random surface speckle. Broad surfaces remain bright and quiet. Routine controls and supply grips moved toward the lower section so the native human reference can expose reach/scale issues.

The base wall face is byte-identical to V1. Added architectural shading is a separate layer. The original 12px left/right connection bands, full-height rails, continuous stripe edge profile, and horizontal panel joint at rows 153–155 are preserved byte for byte. There is no baked wall cap or empty upper margin. Separate caps still layer above the wall.

## Review artifacts

Under `work/Reviews/PxlMedBayWallsV2`:

- `comparison_sheet.png`: V1/V2 pairs at **1:1**, each beside a native **32×48** human reference; also a six-panel composition at 1:1.
- `contact_sheet.png`: all four V2 panels at exact 2× nearest-neighbor, plus the native composition below.
- `native_scale_review.png`: **808×256**, consisting of six native panels (768px) and the 32×48 reference in an adjacent 40px bay. No resampling.
- `inspect_*.txt`: PXL ASCII pixel inspection.
- `med_bay_walls_v2.zip`: source, individual PNGs, editable document and review sheets.

The six-panel sequence is **Access A, Access B, Utility, Access A, Supply, Access B**: four ordinary panels, two specialty fixtures. Each individual variant also has three-copy and 3×3 repeat previews.

The 32×48 figure is a newly authored PXL scale stand-in, not an altered player asset. The existing `Features/Player/Sprites/SOLITUDE-Caretaker.aseprite` is 64×96 and was not resized. Keep that distinction when reviewing physical proportions: this study uses the requested 32×48 target reference. A 256px wall is approximately 5.33 of those reference canvas heights. The native comparison makes that relationship visible; it does not establish final camera/scene scale.

## Editable source and outputs

- `med_bay_walls.px.js`: generation source, including shared architecture, recess/hatch functions, fixtures and scale figure.
- `out/sprites/wall_med_bay_{access_a,access_b,utility,supply}.png`: individual flattened walls.
- `out/sprites/overlay_med_bay_*.png`: transparent fixture overlays.
- `out/sprites/wall_med_bay_*/{wall_face,architectural_depth,fixtures}.png`: independently exported layers.
- `out/sprites/player_scale_reference.png`: 32×48 review-only scale figure.
- `out/med_bay_walls_v2.png` and `.json`: 512×256 four-cell sheet and metadata.
- `Editable/med_bay_walls_v2.aseprite`: four alternative frames/tags, each with three editable layers: WallFace, ArchitecturalDepth, Fixtures. These are alternatives, not an animation.
- `out/scenes`: native repeat, mixed-run and scale review images.
- `out/_preview`: PXL previews at integer enlargement.

PXL remains the generation source of truth; Aseprite changes do not automatically transfer back into it. Rebuilding replaces generated exports.

## Validation

`validation.json` records all checks. They pass:

- All four panels are 128×256 and fully opaque to every edge.
- Overlay alpha contains only 0 or 255; colors remain in the authored palette.
- Original wall base, entire 12px connection bands and panel joint are unchanged.
- All 16 ordered horizontal variant pairings have zero edge mismatches.
- Three-copy and 3×3 compositions contain exact copies of the individual panels.
- Six-panel run contains exactly four ordinary and two specialty panels.
- The human-reference PNG is exactly 32×48 and is pasted at native size, bottom aligned.
- Layer recomposition equals each flattened PNG.
- The saved Aseprite file is reopened and renders back pixel for pixel to the PXL PNGs.

PXL previews, native comparison sheets and ASCII inspection were reviewed. Horizontal tiling is the relevant join; vertical stacking is not intended for full-height walls. No Unity/cap-composition/camera check has been performed.

## Rebuild

From the repository root:

```sh
python3 Tools/ArtSources/SOLITUDE/PxlMedBayWallsV2/build.py
```

Uses the installed PXL skill, Node, Python/Pillow and the existing Aseprite CLI. Override its executable with `ASEPRITE_BIN` if needed. Layer PNGs are losslessly converted from indexed storage to RGBA for the editable RGB document. Artwork is generated natively; only review images use integer nearest-neighbor enlargement. All outputs stay outside `Assets` and runtime code.
