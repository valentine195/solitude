# Inventory sprite provenance

Oxygen canister and replacement fuse are new project-owned inventory illustrations generated with the built-in image generation tool on2026-11-12. The existing `solitude_battery_normal_v01.png` supplied the visual reference; its artwork was preserved and its Battery item reference repaired. The existing maintenance wrench artwork is unchanged.

Generation brief: two upright practical generation-ship hardware silhouettes; stout oxygen cylinder with valve/handle and sage band; glass fuse cartridge with metal caps and amber conductor. Match the project battery’s ivory metal, slate outline, blue-gray shadows and restrained palette. Coarse hard-edged native32px pixel clusters on flat magenta, no labels, glow, scenery or external shadows.

Source sheet and deterministic processing record: `work/Reviews/InventoryUnityCompact/item-art/`. Generated source is retained there as `raw-sheet.png`. Processing uses the installed generate2dsprite processor: two equal square source cells,32×32 output, nearest-neighbor reciprocal-integer1/28 downscale, centered alignment, no smoothing, magenta removal and strict QC. Both sprites pass nonempty, edge containment and unclamped-anchor checks. Final deterministic chroma-spill correction replaced2 oxygen and11 fuse contaminated edge pixels with the existing dark slate outline color (31,39,53); counts are recorded in `despill.json`.

Imports: Sprite/Single,32PPU, Point, uncompressed, mipmaps off, alpha transparency. ItemDefinition icons reference the imported sprites; no gameplay properties were changed.

- `Assets/_Project/Art/SOLITUDE/Props/solitude_oxygen_canister_icon_v01.png` — SHA-256 `469ceff8d078ed9bc9ddb02ea78e544ca2845fae13bbfa4bdcb306ca65fb789b`
- `Assets/_Project/Art/SOLITUDE/Props/solitude_replacement_fuse_icon_v01.png` — SHA-256 `6563c8af210fed1a615a5ba9f3ba5931513437e13e3b7091acba66e69845915a`
