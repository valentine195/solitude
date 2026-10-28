# SpaceShip architectural extraction

First-pass source crops from `Assets/_Project/Art/Imported/SpaceShip/1.png` through `5.png`.

- `Floors/`: 151 floor sections, including repeated variants, grates, glass, growing channels, containment panels, and power conduits.
- `Walls/`: 47 wall sections, including windows, lit panels, controls, and sheet 4 construction fragments.
- `Doors/`: 13 door / doorway reference sections, retained separately for a future perspective pass.

These are intermediate source assets, not game-ready tiles. Original pixels, irregular dimensions, source borders, baked details, and existing perspective are preserved. Sheet 4 construction fragments can include the original white separator lines or black surround. No resampling, pixel-grid correction, perspective correction, alpha cleanup, or deduplication was performed. Crop boundaries follow apparent panel divisions; they do not claim a consistent source pixel grid. Some repeating designs extend across neighboring crops and need regrouping during alignment. Floors covered by furniture in sheet 3 and occupied lab panels in sheet 2 were excluded rather than reconstructed.

`manifest.json` records source hashes, native source crop rectangles (left, top, right-exclusive, bottom-exclusive), and output dimensions for every PNG. `extract.py` reproduces the extraction using Pillow and verifies every crop byte-for-byte against its decoded source rectangle. Originals and existing Unity metadata are untouched.

Review contact sheets are in `work/Reviews/SpaceShipExtraction/`. Preview thumbnails use nearest-neighbor; extracted PNGs remain at original source resolution. Unity import and gameplay validation are deferred until the requested alignment/perspective work is complete.
