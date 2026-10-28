# Top-row north walls

Eight source-derived wall designs: four 32×64 modules (plain, window, service, switch), four 64×64 modules (control/alert, system, alert, door). Atlas: 384×64, no gutters. Use 32 PPU, Point filtering, no compression or mipmaps; bottom-center pivot.

Original attachment and 13 unscaled top-row crops are preserved in Source with crop coordinates. Built-in image editing adapts the source designs to an orthogonal north-wall face with visible cap. Exact prompt and raw output are retained. Repeated source window/plain designs are represented by reusable modules.

process.py applies integer 6:1 nearest-neighbor reduction, extends plain panel margins without stretching devices, and aligns the 8px cap / 52px face / 4px base. Source colors are retained. Small lettering and micro-details simplify at this resolution; this is an adaptation, not a lossless extraction. Native exports, atlas equivalence and full opacity are checked. Review images include native-scale tiles, integer zoom and 3×3 plain-wall repetition.

Outputs are art-source files only. No Unity assets or scenes were changed, and in-game validation remains outstanding.

## Top-surface correction
Run process.py then revise_cap.py to rebuild the latest revision. The latter replaces only the upper 8 rows with a generated overhead surface, perpendicular depth joints, rear boundary and front lip. Face and base pixels remain byte-identical. Original trim version is retained in PreviousTrimVersion. Built-in edit prompt: cap_prompt.txt. Updated comparison and floor context are in work/Reviews/TopRowNorthWalls.

## User-reference cap (current)
The light segmented cap is superseded. The current cap is extracted directly from the user's second attachment, stored as Source/user_cap_reference.png. Source rectangle (6,5,250,37) reduces exactly 4:1 to 61×8. Its original end shaping and front-edge highlight are preserved; only the flat middle is cropped/extended to fit 32/64 pixel widths. No re-generation or stylistic reinterpretation is used for this cap. All pixels below the top eight rows are unchanged. Run apply_reference_cap.py last to rebuild this current revision. Previous versions are preserved in PreviousTrimVersion and PreviousSegmentedVersion. Native dimensions and unchanged face/base pixels are verified; Unity validation remains outstanding.

## Seam and pixel cleanup (current)
Run cleanup.py last. The approved cap's flat center profile now repeats continuously, eliminating per-module end highlights. Joining frame edges and base junctions use shared source-derived pixels. The palette is reduced to 48 colors with dedicated neutral/red/amber/green/cyan/blue families, no dithering; palette-mode cleanup removes isolated colors only in low-contrast material interiors, excluding cap and joining boundaries. No smoothing resampling is used. Device layout and native dimensions are retained. All 64 ordered module pairs have equal outer boundary pixels; a pairwise contact sheet and before/after comparison are in the review folder. This proves boundary matching, not the absence of intentional panel seams. The pre-cleanup atlas is preserved in BeforeCleanup. No Unity scene changes or in-game verification.
