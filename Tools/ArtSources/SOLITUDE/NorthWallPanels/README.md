# SOLITUDE north-wall panel study

Four 64x64 RGBA modules: plain, service, vent, rib. Place at a horizontal stride of 64 pixels (two 32px world cells). Each frame has two transparent rows above and below its 60px wall silhouette. Align identical frame origins; do not trim margins per tile. Horizontal continuation only; end caps and corners are outside this study.

PNG contains individual 256x256 source renders, palette-constrained 64x64 sprites, exact 8x enlargements, a combined render sheet, a combined pixel sheet, an eight-panel continuous strip, and a 3x3 repetition check. V1 retains the first pass.

Model/north_wall_panels.blend opens on the north-wall scene. Existing ventilation and preview scenes were preserved in the Blender document. North-wall modules are spaced 80 Blender units apart for source editing; the camera crops exactly 64 units per module. Units here are authored pixel coordinates, not Unity meters. Meshes are shallow XY relief representing the visible north-wall face in a top-down game. Orthographic camera is directly above; flat emission materials provide stable palette colors without baked room lighting.

Improvement pass: continued blue stripe across the rib, reduced bright panel joint highlights, added transparent top/bottom margins, then quantized nearest-neighbor exports to the nine-color project palette. All 16 ordered horizontal panel pairings have exactly matching outer pixel columns. Alpha is binary. The 3x3 preview checks repetition across three wall runs; these are not vertically seamless fill tiles.

Review: continuous cap and base give a stable silhouette; hatch, vent, and rib remain readable at native size. Warm ivory and graphite lead, with muted blue and one amber service indicator. Flat shading is clean but intentionally simple. Use plain panels most often to avoid obvious feature repetition. No Unity import or in-game visual validation was performed.
