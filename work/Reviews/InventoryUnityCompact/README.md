# Compact inventory review

Implemented in AtmosphericScrubberEncounter, native uGUI/TMP at1920×1080. Final authored panel1120×720;104px icon-first carried slots;192px inspector art area; horizontal44px commands. One persistent64px-cell hotbar remains outside the inventory panel. Exploration HUD/warning visuals restore their prior state on close.

Evidence:
- `journey.json`:14 focused checks pass, including real Tab input, persistent hotbar visibility/input gating, HUD suppression/restoration, moving/assigning/equipping, cancellation, close/reopen and simulated gamepad B.
- `application-tests.json`:15/15 focused existing ApplicationCompositionTests pass.
- `default.png`, `oxygen.png`, `full.png`, `action-focus.png`, `assign-destination.png`, `empty.png`, `exploration.png`: actual Unity offscreen scene/canvas renders. Fixtures populate sample stacks transiently through Restore; they do not alter starting loadouts or saved gameplay.
- `full-proof.json`, `focus-proof.json`: no TMP overflow in full grid; exactly one focused command fill; body leading26.1px. Mixed/oxygen/destination also reported no overflow.
- `item-art/`: generated source, deterministic strict-QC processing, alpha/chroma-spill cleanup. See Docs/UI/InventoryArt.md for shipping asset hashes.

Finish review: compact composition, clear square grid, complete silhouettes, distinct equipment underline and focus corners, correctly aligned input hints, no duplicate internal hotbar, no text clipping in reviewed states. Removed the slot Button color transition so focus is indicated solely by the authored immediate outline/corners without stale tinted cells. Final renders refreshed after that visual-only fix; prior behavior evidence remains applicable because event handlers, bindings and services were unchanged.

Limitations: connected Editor is headless; offscreen helper temporarily converts overlay canvases to camera rendering and restores their settings. These images do not prove a normal Game-window or physical-device session. Physical controller feel, mouse drag regression journeys, long/localized content and other desktop resolutions remain unverified. Use/Split/Drop remain target behaviors awaiting real commands/flows. No full gameplay suite or unrelated build was run.

Editor left in Edit mode. Unrelated SOLITUDE_Start changes preserved. No commits/staging performed.
