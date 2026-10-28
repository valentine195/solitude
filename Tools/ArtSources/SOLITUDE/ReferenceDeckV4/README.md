# Reference-led spaceship flooring V4

Uses the supplied SpaceShip sheets as visual inspiration: inset steel faces, graphite frames, controlled edge catches, recessed fasteners, grates, mesh, utility channels, and selective amber paint. The source sheets and earlier experiments remain unchanged.

## Pixel deliverables

- PNG/reference_deck_atlas_64.png is the native 256x256 atlas: 4 columns x 4 rows of 64x64 floor modules.
- Each module spans 2x2 of SOLITUDE's 32px world cells. Tiles32 contains the 64 exact quadrant slices; assemble each module's quadrants in their recorded order.
- PNG/reference_deck_atlas_8x.png is the exact 8x enlargement, 2048x2048. Each native pixel occupies an 8x8 square. Individual 8x previews are 512x512.
- Opaque floor surfaces, 30 visible colors, no alpha gaps, no smoothing or dither in final processing. Signal/material colors are reserved during quantization so amber, copper and blue survive.
- Raw/generated_floor_atlas.png is the generated 1254x1254 source image. It is pixel-inspired source art, not the native grid deliverable. It was point-sampled onto the fixed native atlas, then palette reduced without dithering. All subsequent slices and enlargements use integer coordinates/scales.

These are independently framed modules. Material frame widths and fastener placement still vary slightly among the generated designs; exact outer-edge color identity across every pairing is not claimed. They butt together without transparent gaps, but do not form a continuous unframed material. The raceway modules are framed service panels, not an automatically connecting pipe kit. Keep mesh/perforation tiles sparse because their high-frequency pattern can compete with actors.

## Module order

Reading left to right, top to bottom: steel_plate, steel_worn, interlocking_plate, maintenance_hatch; grate_horizontal, grate_vertical, perforated_plate, mesh_grate; grip_plate, blue_identifier, amber_service_hatch, plain_latched_plate; raceway_horizontal, raceway_vertical, door_threshold, plain_infill.

## Review and limitations

Each module has a 3x3 repetition preview. Slice reassembly and nearest-neighbor export checks passed. The room preview keeps mostly steel walking plates in the middle and darker service flooring at the perimeter. The static context image uses earlier wall and vent studies; those older assets now show less detail than the reference-led floor and may need a matching art pass later. No in-game validation has been performed, and nothing has been imported into Unity.

Model/reference_deck_v4.blend is a Blender texture-preview document with packed native floor textures, point sampling, and a directly-above orthographic camera. Floor details are generated pixel textures on planes, not a low-poly reconstruction of their individual bolts, frames and grates. Earlier scenes in the active Blender document were preserved.

The raw generation and initial palette processing were inspected; the improvement pass explicitly preserved amber/copper/blue accents that the initial global quantization had muted. Full metadata and quadrant assignments are in tile_manifest.json.
