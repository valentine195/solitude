# SOLITUDE service-room layout

A compact room assembled from the latest remade 32px floor accents, their four quiet base variants, the existing north-wall panel family, and the earlier ventilation unit. No new visual asset generation; the layout reuses accepted artwork. Everything remains outside Unity Assets.

## Layout

- Canvas: 512x352 pixels, 16x11 cells at 32px per cell.
- Clear interior: 14x8 cells. North wall shows a two-cell-high visible face. Side/south walls use thin cap strips cropped from the existing north cap; a full north face is not rotated onto side walls.
- North wall sequence: plain, vent, plain, rib, plain, service, plain. Each module spans two cells.
- Interior floor: 104 base cells, 8 accent cells (92.86 percent base, 7.14 percent accents). Two additional threshold cells form the south exit apron.
- Left service zone: four floor grille/perforated accents with the separate ventilation prop below. Right maintenance zone: maintenance and amber hatch accents. Central four-cell aisle is kept clear. Two-cell-wide south connection is open; no new door asset is fabricated.

## Files

PNG/service_room_native.png is the lossless assembled 512x352 preview. service_room_3x.png is an exact nearest-neighbor enlargement. floor, walls and props are also exported as separate transparent layers. service_room_grid_path_2x.png is a debug-only grid and spawn-to-exit path view.

room_layout.json is portable, editable layer and object data: individual floor cells, source paths, wall-module placements, one separate prop, conservative collision rectangles, walkable cells, spawn/exit hooks, zones and camera bounds. Coordinates start at the top left, with x right and y down. A hidden floor backing row under the north wall fills the wall art's transparent lower margin without exposing a gap.

Assets contains local copies of the actual source textures, so the package does not require paths back to previous studies. Native floor cells remain 32x32. North-wall modules retain their authored 64x64 dimensions and are placed on the same 32px grid. The ventilation source remains 64x64 with its original transparent margins.

Model/service_room_layout.blend is an editable Blender assembly of 128 floor planes, 25 wall pieces, and one separate prop, organized in collections with point-sampled packed textures. Original pixels are the production art; these are textured planes rather than modeled wall/fan geometry. Existing unrelated Blender scenes were retained.

## Checks and limits

All declared walkable cells are connected. A grid flood-fill reaches the south exit from the player spawn. The four-cell-wide central aisle is unobstructed. The fan footprint is conservatively blocked as a 2x2 cell area. Boundaries and the open doorway are explicit in data. The layout is an art/blockout package, not a Unity scene: runtime collision, player sorting, wall occlusion, interaction reach and in-game camera checks remain unvalidated. No Unity imports or project scene edits were performed.

The existing north wall art has a lower-detail, warmer appearance than the newly remade floor accents. This assembly intentionally preserves the requested source artwork rather than repainting it during layout work.
