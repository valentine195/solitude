# Connector cleanup — native 128×256

Focused cleanup of the latest supplied connector prototype. No wall infill, mounting redesign or Unity integration. The source reference remains intact in `Reference/prototype_connector.png`.

Native canvas: **128×256**. The narrow connector silhouette is centered with transparent margins, preserving its proportions instead of stretching it to fill the full width. Opaque bounds are x=48–79, y=10–245. Features retained: paired square end blocks and inset squares, long side rails, upper/lower dark service slots, clipped amber lamp well and narrow bright emitter.

The fixed palette (eight colors used) reuses the existing SOLITUDE graphite/steel and amber ramps. Integer geometry, regular edge highlights, recess shadows and hard alpha replace the prototype's irregular pixels and near-transparent fringing. No blurred glow, noise, smooth resampling, background panel or additional features were introduced. This is a PXL native interpretation rather than a downscaled bitmap.

## Outputs

- `out/sprites/connector_clean_128x256.png`: native transparent PNG.
- `connector.px.js`: standalone editable PXL source.
- `Editable/connector_clean_128x256.aseprite`: three named editable layers.
- `out/sprites/connector_clean_128x256/*.png`: separate spine, end_blocks and amber_emitter layers.
- `work/Reviews/PxlConnectorClean128/connector_preview_4x.png`: checkerboard review enlargement.
- `work/Reviews/PxlConnectorClean128/connector_transparent_4x.png`: transparent 4× nearest-neighbor view.
- `validation.json` and review `pixel_inspection.txt`: palette, dimensions, alpha, bounds, composition, Aseprite and PXL inspection evidence.

PXL preview and native/4× rendering were visually inspected. Checks pass for 128×256 size, centered bounds, only eight authored opaque colors, alpha restricted to 0/255, exact layer composition and pixel-exact rendering after reopening the saved Aseprite document.

Rebuild from repository root with `python3 Tools/ArtSources/SOLITUDE/PxlConnectorClean128/build.py`. Uses installed PXL, Python/Pillow and the existing Aseprite CLI (`ASEPRITE_BIN` can override its path). Native art is not resized; only review views use 4× nearest-neighbor. PXL remains the generation source of truth.

All previous versions and unrelated work remain untouched. Nothing was imported into or changed in Unity.
