# Quiet floor tiles

Six interchangeable, opaque 32x32 floor tiles, rendered in Blender from directly above. Native stride is 32px, matching SOLITUDE. 64px versions are exact 2x nearest-neighbor copies: use those only at the corresponding pixel density. All horizontal and vertical edge pairings match exactly. Floor tiles fill their frames and intentionally have no transparent gaps.

Order in the 3-column, 2-row atlas: plain, soft_patch, wear_a; wear_b, soft_lift, worn_patch. Favor plain (65% in the mixed preview). Palette extends the existing warm neutral-gray floor ramp with very close neighboring tones. Avoid rotating the full atlas during slicing; each cell is exactly 32x32 without gutters.

Improvement pass reduced rectangular tone-patch size and color contrast and softened wear. The result intentionally recedes; no tile frames, bolts, grates, bright highlights, or raised bevels. Original V1 retained separately. Each tile was checked in 3x3 repetition and in a mixed patch. Context review is a static composite with earlier wall/vent studies, not Unity validation.

Model/quiet_floor_tiles.blend opens on the floor scene. Earlier Blender scenes were retained to preserve existing work. Floor models use shallow pixel-coordinate XY relief with flat emission colors, not physical meter dimensions. PNG outputs and Blender source are separate. No Unity imports.
