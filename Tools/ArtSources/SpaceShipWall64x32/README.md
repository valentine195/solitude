# SpaceShip wall perspective and64×32 grid pass

47 source wall sections restored as complete **64px wide ×32px high** north-wall modules in `Tiles/`. Filenames retain source identities; narrow or partial original fragments were rebuilt as complete wall segments rather than stretched at runtime. Eight cap-only straight/corner boundary pieces are in `Boundaries/`. Door artwork was excluded.

## Projection and placement

SOLITUDE orthogonal top-down convention: north walls show a visible horizontal upper surface and a short south-facing vertical face. Each north-wall sprite uses the same8px graphite cap (rows0–7),22px face (rows8–29), and2px base rail (rows30–31). Caps and rails were copied from the generated master to align their profiles across the family. There are no isometric slopes or converging edges. East, west and south boundaries use cap-only sprites with transparent unused space; do not rotate a north-wall face to construct side walls.

All exports have64×32 canvases, binary alpha, integer native pixels, and a shared128-color palette without dithering. The atlas is512×224,8 columns ×7 rows, with zero margin or spacing. First47 slots are north walls, next8 are boundaries, last slot is transparent. `manifest.json` records exact slot positions and provenance.

North-wall sprites have a bottom-center anchor at(32,32). Boundary sprites share the same canvas but their geometry is positioned by their orientation: north strip at y0–7; south at y24–31; west at x0–7; east at x56–63. Corners combine these strips. Each module spans two32px world-grid columns and one row. The requested32px wall height overrides the usual taller north-wall convention for this family.

## Processing and review

Built-in image editing restored the source reference layout with top-down north-wall cap/face proportions. The returned1024×1536 sheet was reduced by exactly4× nearest-neighbor into native64×32 sprites. No fractional scaling or interior strip removal was used. Some fine face details and alert colors are simplified at this size. Perspective was redrawn; these are not lossless original crops.

Original extractions, source sheets and Unity metadata remain unchanged. `restored_raw.png` retains the generated image; `prompt.txt` stores the exact instruction; `source_order.json` maps all47 source sections; `process.py` reproduces final exports using Pillow.

`qc.json` records dimension/alpha/atlas consistency checks and exact horizontal cap/base edge matches. All47 north walls were visually reviewed in three-module horizontal repeats, and all55 sprites reviewed at4×. `work/Reviews/SpaceShipWall64x32/room_context_4x.png` shows north faces plus cap-only side/south boundaries over the previous floor pass. This is an illustrative placement check, not an in-game screenshot. Individual face trims, panel seams, wiring and details vary; this is not an autotile or universally seamless face-transition library.

Source deliverables stay outside Unity pending integration. No scenes or import settings changed, and gameplay-scale validation in Unity remains pending. When integrating, use32 PPU, Point filtering, compression off, mipmaps off and64×32 slicing. Do not import review previews as game assets.
