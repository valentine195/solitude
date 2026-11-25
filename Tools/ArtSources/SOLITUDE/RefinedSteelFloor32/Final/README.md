# Refined steel floor32

Selected after three critique rounds. Main floor: light neutral steel. Service side areas: cooler muted steel. Small dark accents remain the strongest floor details.

- Tiles32: twenty complete32×32 PNGs.
- PNG/floor_atlas_32.png: 128×160 atlas, four columns, five rows; manifest gives cell order.
- PNG/floor_atlas_8x.png: exact 8× enlargement.
- Source/refined_steel_floor.aseprite: editable native-resolution atlas.
- Room: portable assets, layers, layout/collision metadata, native 512×352 preview and 3× preview.
- Model/service_room_refined_steel.blend: separate Blender scene with packed pixel textures; plane-based sprite layout, not remodeled3D floor geometry.

Use the plain four base variants freely within one palette. North/west/corner joint tiles are optional structural tiles for larger panel groups. Do not scatter these randomly. The example uses 3-cell side service bands and staggered 4×2-cell central plate groupings. Cell size remains 32px throughout.

All six existing accent tiles are unchanged. Main and service base families pass edge equality checks. Pixel previews use nearest neighbor. No Unity assets were imported or altered; runtime collision remains untested. See validation.json and critique_rounds.md for checks and limitations.
