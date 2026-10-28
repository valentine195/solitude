# SOLITUDE — refined walls and room depth pass

Selected update: cooler aged off-white walls, graphite framing, muted blue rail, fitted panel edges, five compatible wall variants, and restrained native-pixel contact shading.

## Files

- Room/PNG/service_room_native.png: authoritative 512×352 native composite.
- Room/PNG/service_room_3x.png: exact nearest-neighbor 3× preview.
- PNG/depth_detail_before_after_4x.png: previous mockup on left; depth pass on right.
- PNG/before_left_after_right_2x.png: full room comparison.
- PNG/north_wall_atlas.png: 320×64, modules in plain/service/vent/rib/wide order.
- PNG/north_wall_atlas_8x.png: exact 8× atlas enlargement.
- Tiles32: twenty 32×32 cells; each complete 64×64 wall module reconstructs from its four named cells without resizing. Wall proportions are preserved.
- Room/Assets: portable final floor, wall and prop assets.
- Room/PNG/contact_shadows_layer.png: separate hard-edged wall-to-floor shading.
- Source/vent_depth.aseprite: editable fan sprite with separate shadow layer.
- Source/wall_atlas.aseprite: editable native-resolution wall atlas.
- Model/service_room_depth.blend: packed room textures, separate contact geometry, and wall source scenes.
- Model/depth_wall_modules.blend: revised modeled wall reliefs.

## Depth treatment

Wall panels have narrow lower/right shade bands, thin upper/left highlights and a contact strip under the blue rail. The wall-to-floor shadow reaches only a few pixels. The raised fan casts a 2px-right/3px-down hard shadow and has a clearer alloy rim and darker cavity. Floor grates remain recessed; they cast no raised-object shadows.

The five wall variants are split plain, wide plain, service hatch, vent and structural rib. The two plain forms alternate with occasional functional modules. The amber indicator retains its service meaning. Base flooring and accent PNG files are unchanged; no broad lighting or shadow is baked into reusable floor tiles.

## Validation and limits

All 25 ordered wall edge pairs match. All 20 cell exports reconstruct their modules exactly. Room, wall and shadow alpha use only 0/255. 3×/8× previews use exact integer nearest-neighbor expansion. Shadow pixels lie on existing floor. Room geometry, walkability, object positions and collision rectangles are preserved. Unity was not imported into or modified; runtime collision was not tested.

The generated depth study is reference-only. Production wall art comes from the revised Blender geometry at native 64px, followed by palette normalization. Prop finishing uses Aseprite on the existing native sprite. Review at native scale confirms improved separation without adding floor clutter. The room remains a sparse layout study, not a fully furnished environment.
