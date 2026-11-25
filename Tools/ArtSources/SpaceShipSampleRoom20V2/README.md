#20×20 room — connected wall revision

Supersedes the first sample composition. Overall footprint remains640×640 pixels,20×20 cells of32px, including the64px north wall.

Changes:
- Authored64×64 northwest/northeast transition modules connect a visible north cap to the vertical west/east cap.64×64 south corner modules close the perimeter.
- Side/south cap strips come from the same corner surface. North modules share corner-derived cap and base profiles; north faces are not rotated onto side walls.
- A quiet gray steel floor replaces the dense framed tiles, white center rectangle and grating stripes. Existing `RefinedSteelFloor32/Round3` clean/rubbed/joint sprites form low-contrast128px panel divisions on the32px grid.
- Fewer wall details: paired windows, plain plating, one system console. No alert display.

`sample_room_20x20.png` is the flattened preview. `Layers/` contains floors, north walls and boundary caps. `Tiles/` contains the actual reusable artwork. `room.json` records dimensions, source provenance, asset placements, collision grid and spawn hook. Boundary-strip pixel offsets are explicit so their8px visual thickness can be placed correctly within32px grid cells. Layers remain separate; no Unity scenes changed.

New corner artwork used built-in image editing with prompts retained in `corners_prompt.txt` and raw source in `corners_raw.png`. Native cleanup removes atlas dividers, reduces uniformly8× with nearest-neighbor, removes redundant interior strips to64×64, keys magenta, and copies shared structural profiles from generated source pixels. Existing floor and wall source assets are unchanged.

Checked full-room layout and four corner closeups; exported placements reconstruct the preview byte-for-byte. Review images live in `work/Reviews/SpaceShipSampleRoom20V2/`. This is an art assembly, with Unity/gameplay validation pending. Rebuild with `python3 assemble.py` (Pillow, NumPy).
