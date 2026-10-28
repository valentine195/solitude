#20×20 sample room

Overall footprint20×20 cells at32px per cell:640×640 native pixels, including north-wall height and boundaries. The north wall is64px tall; its32px and64px widths occupy one and two columns respectively. Side/south walls show only an8px cap copied from the corrected wall family.

`sample_room_20x20.png` is the assembled preview. `Layers/` holds separate floor, north-wall and boundary-cap images. `room.json` supplies editable cell-aligned asset placements, portable copied assets in `Tiles/`, a conservative20×20 collision grid, and one spawn hook. There are306 walkable cells under the conservative whole-cell collision policy. This is a closed art sample with no exits or door pass.

Steel deck surrounds a10×10 light panel area and two grating service runs. No new artwork was generated. All imagery reuses the accepted source passes, with cap-only boundary pieces cropped/rotated from the wall top surface. No tall wall face was rotated.

Verified canvas dimensions, grid-aligned placements, floor coverage and exact reconstruction from exported placement data. Preview and grid-overlay review images live in `work/Reviews/SpaceShipSampleRoom20/`. This is an editable art sample, not a Unity scene; no game scenes or import settings changed.

Rebuild with `python3 assemble.py` using Pillow.
