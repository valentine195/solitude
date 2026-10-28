# Corrected tall wall pass

This supersedes the earlier reversed64-wide×32-tall wall pass.

- `Tiles32x64/`:23 wall modules, **32 pixels wide ×64 pixels tall**.
- `Tiles64x64/`:24 wider modules, **64 pixels wide ×64 pixels tall**.
- `wall_atlas_32x64.png`:128×384,4 columns ×6 rows,32×64 cells; last cell transparent.
- `wall_atlas_64x64.png`:256×384,4 columns ×6 rows,64×64 cells.

No margins or gutters. Use32 PPU. Native assets are uniformly reduced4× with nearest-neighbor from newly restored original references. The short wall outputs were not stretched or rotated. Image edit prompts and raw results are retained. Partial source fragments are rebuilt as full modules, with reference-key gaps filled from a generated full panel. Native per-sprite palettes use64 colors, no dithering; shared structural pixels are reapplied after quantization.

All north-wall modules have an8px visible orthogonal top cap,52px tall vertical face and4px base. Shared cap/base profiles align across both widths. Side and south boundaries use cap-only pieces from the previous pass; do not rotate tall wall faces to form side walls. The previously produced cap-only pieces remain valid as32px grid boundary elements.

Every original47 wall references maps to one corrected output in `manifest.json`. Dimensions, opacity and atlas pixel equivalence were checked; contact sheets and a room context preview with32px floors are in `work/Reviews/SpaceShipWallTall/`. Small equipment details are simplified at native resolution. These are art-source deliverables; no Unity scene/import changes or in-game validation were performed. Source sheets and previous pass outputs are preserved.
