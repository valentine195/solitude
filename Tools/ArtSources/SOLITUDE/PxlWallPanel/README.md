# SOLITUDE PXL wall panel proof of concept

Built with the installed `pxl-pixel-art` 0.1.0 skill. This evaluates its procedural authoring/rendering workflow under Codex; it is not a benchmark of the Claude model. Nothing was imported into Unity.

## Reference and interpretation

Existing generic artwork: `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite`. The original file is read-only; its SHA-256 is in `validation.json`. `Reference/wall_panel_1.png` is a native export. `Reference/reference.px.js` losslessly encodes that raster solely for the PXL comparison scene. The generated base is separately authored using primitives and reusable functions in `wall_panel.px.js`; it does not stamp or recolor the reference.

The reconstruction preserves the 128×256 canvas, original alpha silhouette (including the 18px upper margin), two panel faces, side rails, fastener columns, cap and plinth. Highlights favor upper/left edges and recess shadows favor lower/right edges. It simplifies the reference's fine tonal variation: 11 opaque colors, all present in the original's 2,407-color opaque palette. This is a clean interpretation, not a pixel-exact replica. The cap/plinth and hardware proportions are close rather than traced. Reference-to-base opaque RGB mean absolute error is 12.93/255; that measures difference, not visual quality.

## Deliverables

- `out/sprites/wall_pristine.png`, `wall_lightly_worn.png`, `wall_heavily_damaged.png`: individual native PNGs.
- `out/sprites/overlay_lightly_worn.png`: independent transparent abrasion overlay.
- `out/sprites/overlay_heavy_damage.png`: independent impact/delamination overlay, additive to wear.
- `out/sprites/overlay_heavily_damaged.png`: combined wear + damage for convenient single-layer use.
- `Editable/wall_panel_variants.aseprite`: 128×256 document with 3 state frames/tags and 5 editable layers: Structure, UpperPanel, LowerPanel, WearOverlay, DamageOverlay. State frames are alternatives, not a playback animation.
- `out/wall_variants.png` + `.json`: native 384×256 sheet and cell metadata.
- `out/scenes/repeat_*.png`: three adjacent copies of each state.
- `out/scenes/grid_*.png`: additional 3×3 repetition checks.
- `out/scenes/mixed_repeat.png`: different states joined in one strip.
- `out/scenes/comparison_reference_pristine.png`: PXL-rendered reference comparison.
- `out/_preview/`: PXL integer-scale previews.
- `work/Reviews/PxlWallPanel/contact_sheet.png`: reference, variants, native repeats.

## Validation and seam scope

`validation.json` records programmatic dimension, palette, alpha, border, composition, hash and Aseprite round-trip checks. All pass. Wear changes 105 pixels (0.34% of opaque area), heavy damage changes 1,850 (6.07%).

The left/right joining columns match exactly on every row in every state. Damage also leaves a 12px band on both sides, the top cap and bottom plinth unchanged. Horizontal 3-copy strips show continuous cap lines and deliberate structural mullions, with no extra join gaps or lighting steps. Repeating the same heavy impact is visibly patterned; use its overlay selectively.

Vertical tiling is not an intended connection: a terminal cap and plinth plus the inherited transparent upper margin make stacked rows visibly separate. The 3×3 previews document this; they are not a claim of vertical seamlessness.

PXL's built-in preview, painting-step comparison/highlight, live pixel inspector and CLI ASCII inspection were exercised. At native (64,127), the inspector reports `#b6bbc1`, key `b`. Full ASCII evidence is retained in `work/Reviews/PxlWallPanel/inspect_*.txt`. There is no dedicated comparison CLI in this installed version; the PXL reference-comparison scene and numerical checks supply that comparison. The contact sheet was visually reviewed at native and integer magnification. Unity/gameplay-scale integration remains deliberately untested.

## Rebuild and edit

Run `python3 Tools/ArtSources/SOLITUDE/PxlWallPanel/build.py` from the repository root. Requirements are Node ≥22.18, Python + Pillow, and the locally installed Aseprite CLI. Override its path with `ASEPRITE_BIN` if needed. The entry point rebuilds PXL PNGs, converts layer inputs losslessly to RGBA, exports/reopens the editable document, checks pixel-exact round trips and regenerates QC/contact sheets.

The RGBA conversion is necessary for this handoff: PXL emits indexed PNGs and Aseprite's `Image{fromFile}` retains their mode. Directly copying those into RGB cels interpreted indices as pixel values; the explicit conversion fixes that without altering pixels.

For live preview:

```sh
node ~/.codex/skills/pxl-pixel-art/bin/pxl.js serve Tools/ArtSources/SOLITUDE/PxlWallPanel/wall_panel.px.js
```

Edit `panel()` or the shared palette to update all states. Edit `wear()` or `impact()` to change overlays independently. Keep PXL as source of truth during procedural iteration: edits made in the exported Aseprite file do not update PXL and rebuilding replaces that export. All source, exports and review material remain outside `Assets` and runtime assemblies.

## Assessment against the existing Aseprite process

| Criterion | Finding |
| --- | --- |
| Consistency | Strong result for modular families: shared geometry, lighting conventions, border locks and a fixed palette prevent accidental drift among states. Aseprite can also achieve this through disciplined shared layers; PXL makes those constraints executable and easy to check. |
| Quality | Cleaner pixel clusters and crisp edges, but flatter material shading and more mechanical damage shapes than the reference. No demonstrated overall aesthetic improvement. Aseprite remains preferable for nuanced hand polishing and unique organic marks. |
| Iteration speed | The three authored variants rebuild in roughly 0.17 seconds without previews on this machine. The live viewer reports about one second for its broader traced project. Shared parameter changes are inexpensive after setup; initial reference analysis and procedural encoding take real work. No controlled Aseprite authoring-time comparison was performed, so a speedup multiplier is unsupported. |
| Editability | Useful layered Aseprite handoff, but a one-way workflow. Maintaining both PXL and manually painted derivatives needs an explicit source-of-truth choice. |
| Tiling | Exact horizontal border checks are straightforward. Damage repetition still requires visual judgment and selective overlay placement. |

Recommendation: use PXL for repeatable panel construction, palette experiments, controlled wear families and automated pixel QC; retain Aseprite for final art direction and local finishing. This proof of concept supports a hybrid pipeline, not replacing the current process wholesale. It is a source-art feasibility pass, not production approval at gameplay scale.
