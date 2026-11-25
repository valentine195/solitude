# Unified wall family and revised20×20 sample

Replaces the mixed wall assemblies from prior room samples with one generated construction master. Plain, window, vent and console variants use the same background wall pixels, palette, panel divisions, cap and base. Detail changes are cropped overlays. Corners originate in the same generated continuous frame as the straight walls; there are no diagonal silhouette cuts or separate decorative corner posts.

`Tiles/` contains the64×64 north modules,32×64 plain module,64×64 corner pieces and cap strips. All north faces use8px cap,52px face and4px base. Side/south strips are8px thick. A single48-color master palette is used before cropping. The master is reduced uniformly4× nearest-neighbor, then two flat interior rows are repeated to reach the required face height. No fractional resizing, smoothing or arbitrary transform scales are used. Each variant reuses the master structural profiles.

`sample_room_20x20.png` is the640×640 room. The floor is byte-identical to the V2 floor; only walls changed. `Layers/` contains separate floor/north-wall/boundary layers. `room.json` contains portable source references, copied tiles, placements and the existing conservative collision grid. `qc.json` records dimensional and pixel-consistency checks. Generated master and exact built-in image-edit prompt remain in `master_raw.png` and `master_prompt.txt`. Original source sheets and previous revisions remain untouched.

Checks:32×64 and64×64 north dimensions; identical cap/base and outer face pixels across variants; exact corner-to-side-strip exit pixel matches; unchanged floor; no opaque magenta pixels; portable placement reconstruction matches the preview. Review images in `work/Reviews/SpaceShipWallUnified/` show the joined wall family, four corner closeups and the complete room. Visual checks are source-art review only; this revision has not been integrated or validated in Unity.

Rebuild with `python3 assemble.py` using Pillow.
