# SOLITUDE inventory prototype

Open `index.html` in a browser, or serve this directory locally. The current preview is at `http://127.0.0.1:8766/` while its local server is running. The revised presentation is a centered in-game overlay over an illustrative paused ship backdrop. Preview scenarios and Reset are tucked under Prototype controls at the lower right.

This is a visual design prototype with illustrative interactions, outside Unity's imported assets. It uses sample quantities and simulated operations. Nothing reads or writes game saves. Reloading or Reset restores the chosen preview scenario.

The Unity handoff is [InventorySpecification.md](../../../Docs/UI/InventorySpecification.md). The intended workflow is to define the visual prototype and specify behaviors in that document. Browser behavior code and tests are disposable demonstration support, not a production implementation to port or harden further. Confirmed requirements and open gameplay decisions are separated in the specification.

The distilled presentation keeps amber focus brackets, an item-in-hand cue, brief settling feedback, and item-specific receipts. Instructions and receipts share one reserved line beneath the grid. It is silent. Reduced-motion preferences replace movement with a static highlight while retaining text feedback. No operation waits for an animation to finish.

## Try it

- Select a carried item to inspect its details and actions.
- Use All / Tools / Consumables / Miscellaneous to filter by category. Unrelated occupied slots dim and cannot activate; slots never repack. Beginning a move or assignment restores All so every valid destination remains available.
- Equip / Assign to hotbar enters destination selection. Select a hotbar destination; occupied slots swap with the source. This is a prototype policy, not a new approved gameplay rule.
- Move enters pick-up/place mode. Select another slot to move, swap, or merge matching stacks up to their limit. Mouse dragging also uses this operation.
- Use simulates restoring O₂ by 20 percentage points. The quantity decrements only when the simulated effect succeeds. This value is demonstration data, not a balance change.
- Split stack selects a quantity and creates a new stack in the first free carried slot. The full-inventory preview demonstrates the unavailable state. Destination policy remains subject to gameplay implementation requirements.
- Drop selects a quantity, confirms, and creates an entry in World drops, available under Prototype controls. The Drop blocked preview demonstrates placement failure without item loss. No actual Unity pickup is created.
- Actions follow item capabilities. Non-stackable items omit Split; materials used at ship systems omit Use. All required inventory operations remain available for eligible items.
- Close returns to a neutral closed-inventory screen; Open inventory or I reopens it.

## Controls

Keyboard: arrow keys navigate by screen position, Enter/Space activates the focused button, Tab from an item jumps to its actions, Escape cancels the current subflow before closing. Native Tab / Shift+Tab remain available. X opens Drop for the selected item.

Standard browser gamepad: D-pad or left stick navigates, A activates, B backs out, Y focuses actions, X opens Drop, bumpers traverse slots. Left/right changes quantity when the quantity field is focused. Connect a controller and press a button to expose it to the browser. Physical hardware has not been verified.

## Visual materials

- Exo 2 from `Assets/_Project/UI/Fonts/Exo_2/Exo2-VariableFont_wght.ttf`; license copied to `assets/Exo2-LICENSE.txt`.
- Wrench from `Assets/_Project/Features/Items/Weapons/MaintenanceWrench/maintenance_wrench_placeholder.png`.
- Battery from `Assets/_Project/Art/SOLITUDE/Props/solitude_battery_normal_v01.png`.
- O₂ and fuse use simple authored SVG pictograms as prototype substitutes; these are not final game art.
- Ship backdrop from `work/Reviews/ExplorationAudits/SolitudeAudit/locker-play-v01.png`, an existing illustrative review screenshot rather than a current Unity capture. CSS darkens it and positions its old HUD beyond the viewport.
- Raster source paths are embedded into the copied PNGs. Original Unity assets are unchanged.

Existing visual authority: `Assets/_Project/UI/README.md`. The prototype explores the approved inventory layout without creating a replacement design system.

## 1080p readability refinement

The reference panel is 1400×870px at 1920×1080, with a complete 6×4 grid, adjacent inspector and nine-slot hotbar. Essential names, descriptions, category filters and actions use 18px Exo; supporting labels and key hints use 16px. Screen and item headings use 36px and 30px. Carried cells are 108px high, action rows 40px, and hotbar cells 80px. This is a visual prototype revision; no interaction policies changed.

Current captures and measured verification: `.impeccable/review/inventory-readability/`. The handoff specification records the design-space measurements and the remaining Unity scaling decision.
