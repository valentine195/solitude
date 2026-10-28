# Quiet panel cleanup / 256×256

Native PXL cleanup of the supplied simple panel. Keeps only its quiet face, thin outline and four clipped corners. The main surface is the wall palette's #EEF1EF and the outline is its #A8B3B8. Broad soft shading, color noise and antialiasing were removed; no structural or decorative details were added.

The production canvas is 256×256. The outline is 1 native pixel thick, inset 2 pixels, with four matching 6px clipped corners. The surface is fully opaque. The 1254×1254 reference is copied unchanged into `Reference/prototype.png` and is never resampled into production art.

- `out/sprites/quiet_panel_256.png`: native PNG.
- `panel.px.js`: editable PXL source.
- `Editable/quiet_panel_256.aseprite`: two named editable layers.
- `out/sprites/quiet_panel_256/*.png`: surface and outline layer exports.
- `out/scenes/quiet_panel_repeat_3x3.png`: native repetition test.
- `validation.json`: dimensions, exact two-color palette, alpha, symmetry, border, repeat and reopened-Aseprite validation.
- `work/Reviews/PxlQuietPanel256`: 2×/4× nearest-neighbor views, native 3×3 repetition, ASCII inspection and full ZIP.

The native PNG, PXL preview, 4× view and repetition test were inspected. All automated checks pass. Saved Aseprite rendering matches the PNG pixel for pixel. Original and unrelated project work remain untouched; nothing was imported into Unity.

Rebuild from the repository root with `python3 Tools/ArtSources/SOLITUDE/PxlQuietPanel256/build.py`. Uses installed PXL, Python/Pillow and the existing Aseprite CLI. `ASEPRITE_BIN` overrides the CLI path. PXL remains the source of truth; manual Aseprite edits do not update it.
