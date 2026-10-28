# Light neutral steel base floor

Palette-only lightening of the four quiet 32x32 base tiles, informed by the lighter, more neutral solid metal panels in the supplied SpaceShip inspiration sheets. The six remade 32px accent tiles retain their previous pixels and darker infrastructure values. Original studies remain intact.

Exact base palette mapping: #748194 -> #8B929C; #697688 -> #7E8692; #7D899A -> #959CA5; #8591A0 -> #9EA5AE. The principal surface rises by about 14 percent in weighted sRGB brightness, with a reduced blue-minus-red channel difference. This is a visual art adjustment, not a physical illumination change.

Source/generated_lightening_reference.png is the image-tool palette-edit reference. The native production tiles were edited with exact color replacement in Aseprite to preserve every existing pixel position and scuff shape; Source/light_steel_base.aseprite is that editable source. No new surface marks, tile outlines or panel joins were added in this lightening pass.

Tiles32 contains ten complete 32x32 tiles: four revised bases and six copied accents. PNG/floor_atlas_32.png is the four-column atlas; its final two cells are empty. The _8x files use exact nearest-neighbor scaling. All base edges match across all 16 ordered pairings in each axis. Palette-only pixel mapping was verified against every pixel of the previous bases. Accent source files were copied without modification.

Room contains the same service-room arrangement with the lighter base: portable layout data, copied local source assets, separate floor/walls/props layers, native and 3x previews, and a Blender preview. Grid connectivity and the clear central route were rechecked. PNG/before_left_after_right_2x.png places the previous room on the left and the lighter room on the right. The room geometry, accent positions, walls and props have not changed.

Room/Model/service_room_light_steel.blend is the separate editable Blender assembly with packed textures. The sprites remain pixel textures on planes. No Unity import, runtime lighting change, or in-game collision validation was performed.
