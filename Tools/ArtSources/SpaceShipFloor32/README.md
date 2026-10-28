# SpaceShip floors — 32px pass

151 floor sections exported as opaque 32×32 RGBA PNGs in `Tiles/`, retaining the extraction filenames. `floor_atlas_32.png` is 512×320, 16 columns × 10 rows, with 32×32 cells, zero margin and zero spacing. The final nine unused cells are transparent. Slice from the upper-left origin.

This pass uses built-in image generation to restore the reference sheet, followed by deterministic nearest-neighbor integer reduction, removal of low-information interior pixel strips to meet the exact cell size, and a shared 64-color palette without dithering. It changes source artwork and simplifies details; it is not a lossless crop or merely a resized original. Original extractions remain unchanged. `prompt.txt` contains the full image-edit instruction, `restored_raw.png` the returned image, and `manifest.json` maps every output back to its original crop and records the normalization operations.

All 151 exports were checked for exact dimensions and full opacity. All 151 were visually reviewed in 3×3 repeats at integer 2× zoom; previews live in `work/Reviews/SpaceShipFloor32/`. The atlas is aligned to an exact 32px grid and every output pixel is a single native pixel. Framed panels intentionally retain visible joints. This does not certify arbitrary neighbor combinations as seamless: containment corners, split vents, boundary panels and channels retain directional/layout constraints from the source, and some AI-derived texture irregularity remains. No seamless/autotile rules were authored.

These remain art-source deliverables outside Unity. No scenes, original assets, GUIDs, or Unity import settings changed; gameplay-scale approval is pending integration. In Unity use Point filtering, no compression or mipmaps, 32 PPU and 32×32 grid slicing when integrating.

Reproduce deterministic exports with `python3 process.py` (Pillow and NumPy).
