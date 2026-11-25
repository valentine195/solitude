# Inventory selection popover

Native Unity uGUI in AtmosphericScrubberEncounter,1920×1080.718×680 inventory body and unchanged6×4 physical grid.96px icon canvases make occupied items more prominent; deeper empty slots, inset edges and quieter borders differentiate empty/occupied/selected states. One400px-wide opaque lighter selection popover overlaps the inventory edge by18px, follows the selected row and clamps above the persistent hotbar. Height428px for tools,380px for other items. The item sprite extends above the surface. Feathered geometry shadow offset10px supplies elevation without bitmap generation, decorative effects, sound or continuous animation.

Commands and item details are authored children of the popover. E/R/M retain their existing shared-input bindings. Confirming an occupied slot enters the commands directly for controller/keyboard navigation. Empty slots hide the popover; Move/Assign hide it and show existing destination guidance; cancel restores it. Existing transactions, category mapping, input/pause policy and persistent hotbar remain.

Evidence:
- `journey.json`:10 focused keyboard/submit checks, including entry into commands, move placement, E equipment destination, cancel, R assign, equip, hidden instructions, closed-screen gating and reopen.
- `popover-states.json`:7 presentation checks covering last-row, empty, tool/non-tool sizing, hotbar selection, destination visibility and cancel restoration.
- `default.png`, `oxygen.png`, `full.png`, `last-row.png`, `hotbar.png`, `empty.png`, `move-destination.png`, `after-cancel.png`, `action-focus.png`: actual Unity offscreen scene/canvas captures. Review fixtures transiently restore stacks; no save/loadout change.
- `full-proof.json`, `focus-proof.json`: no TMP overflow at full capacity,26.1px body leading and exactly one command focus fill. Mixed/cancel states also report no overflow.

Finish review: all24 cells fit, item silhouettes carry more visual weight than empty outlines, popover elevation is visible against both inventory and world, no permanent inspector/command row, no duplicate hotbar, keycaps and text legible in reviewed states, lower-row/hotbar panels stay within viewport. The capture helper resolves popover position after applying the review render target, avoiding the connected headless Editor’s640×480 layout affecting1920×1080 captures.

Limitations: offscreen Unity render does not prove a normal Game-window or physical-controller session. No animated transition or performance claim; the shadow is a static mesh rebuilt on geometry changes. Prior service/input test evidence remains applicable to unchanged services. No broad gameplay test suite or unrelated build was run. Final confirm-focus handler verified after the visual captures; it does not change their geometry or styling. Editor left in Edit mode; unrelated SOLITUDE_Start changes preserved.
