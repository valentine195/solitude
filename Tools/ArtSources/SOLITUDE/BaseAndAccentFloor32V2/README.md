# Remade 32x32 floor accents

This update replaces the six previous reduced-detail accent tiles with fresh designs explicitly planned for 32x32 readability. It preserves all four quiet base files byte-for-byte. Earlier versions remain intact outside Unity Assets.

## Deliverables

Tiles32 contains ten COMPLETE 32x32 PNG tiles. Each occupies one SOLITUDE world cell. These are not 64px modules or quadrant slices. Use a placement stride of 32 pixels.

The six redesigned accents are steel_plate, maintenance_hatch, grate_horizontal, perforated_plate, amber_service_hatch, and door_threshold. Their feature budgets are deliberately small: four broad grille bars; nine large perforations; two broad flush hatch latches; three threshold strips; simple metal edge bands; robust amber corner marks. The four unchanged base variants remain base_clean, base_scuff_a, base_rubbed, base_scuff_b.

PNG/redesigned_accents_native.png is a 64x96 atlas, two columns and three rows. PNG/redesigned_accents_8x.png is the exact 8x preview, 512x768. PNG/floor_atlas_32.png includes bases and accents in a four-column, three-row atlas; its final two cells are empty. Explicit indices appear in tile_manifest.json.

PNG/old_top_new_bottom_8x.png compares the old whole-tile reductions in the TOP row with the new designs in the BOTTOM row. PNG/floor_mix_native.png and context_native.png show the unchanged room placement with 94.375 percent base and 5.625 percent accents. Every tile also has a 3x3 repetition check and an exact 8x enlargement.

## Source and verification

Raw/redesigned_accent_source.png is a new generated source, guided by the approved reference style and explicit 32px feature limits. It is not the previous 64px accent atlas. Its presentation grid drifted, so each complete frame was cropped explicitly before controlled nearest-neighbor sampling onto a 32x32 canvas. Crop coordinates are retained in the manifest. Final processing uses ten fixed accent palette colors and no dithering or smoothing. Raw source resolution is higher than native resolution; the native exports are the production pixel-grid deliverables.

Each exported tile is exactly 32x32 and fully opaque. Every 8x8 block of every enlarged tile was compared against its native source pixel. Base-file hashes match the prior version. Static repetition and sparse room previews were inspected. This does not claim hand-placed pixel provenance or identical edge colors across all accent pairings. Some generated corner contours remain asymmetric, but major forms were simplified specifically to survive at 32px.

Model/base_and_accent_floor_32_v2.blend contains packed native textures on preview planes with point sampling, not separate geometric grille bars or fasteners. Earlier scenes in the live Blender document were retained. No Unity import, scene modification, or in-game validation was performed.
