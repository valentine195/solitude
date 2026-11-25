# Base floor and accent tiles — complete 32x32 cells

Four unframed cool steel base variants and six framed accent tiles. Each file in Tiles32 is a COMPLETE 32x32 tile, placed at a stride of 32 pixels. No quadrant assembly or 64px modules. The original reference-led V4 art remains unchanged.

Base tiles cover most of a room; start with 90-95 percent base and reserve accents for machinery, access points, drains, or doorways. The mixed preview is 94.375 percent base and 5.625 percent accents. Within the base tiles, favor the clean tile and use the three sparse wear variants occasionally. Base variants have exactly matching edges across all 16 horizontal and all 16 vertical ordered pairs.

The base comes from a new generated material sheet matching the approved V4 steel palette. Its clean variant is sampled from an unmarked source region. Source art is retained in Raw. Native exports are produced through controlled point sampling and four-color, non-dithered palette reduction. Accent tiles are whole approved 64px V4 designs reduced to 32px using exact 2x nearest-neighbor reduction, not split into quadrants. Some tiny accent detail is simplified or lost at this size; judge the native previews as the actual deliverable.

PNG/floor_atlas_32.png is 128x96, a four-column atlas with ten occupied cells. The last two cells in row three are empty. tile_manifest.json gives explicit tile names and atlas positions. Tiles32 contains only the ten occupied standalone tiles. Each is fully opaque. PNG files ending _8x are exact integer enlargements; each native pixel occupies an 8x8 block. Repeat previews show each tile in a 3x3 field. Native room and context images are also included.

Model/base_and_accent_floor_32.blend is a separate Blender preview document using packed native pixel textures with point sampling. It is not a geometric reconstruction of the accent details. Earlier Blender scenes are preserved. No Unity import or in-game validation has been performed.

Refinement: the first generated clean variant had a small repeating mark. Sampling a clear area of the same generated source removed that repeat from the dominant tile. The other variants retain sparse wear. Final tile dimensions, base edge pairings, opaque alpha and exact enlargement pixels were checked.
