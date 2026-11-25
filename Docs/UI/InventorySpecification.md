# Inventory — visual and behavior specification

## Status and deliverables

This is a design handoff for the Unity inventory. It records confirmed scope, the current visual proposal, intended interaction rules, and open gameplay decisions.

The deliverables are a visual prototype and this behavior specification. Browser interactions illustrate states and transitions; their JavaScript, input bindings, test coverage, and simulated policies are not the Unity implementation contract. Further prototype work should prioritize visual decisions and representative states rather than production-quality browser behavior.

This document extends the inventory-specific details left unspecified in the [SOLITUDE UI Guide](../../Assets/_Project/UI/README.md). The shared guide remains authoritative for the game's overall identity, color semantics, and UI principles. The [product record](../../PRODUCT.md) supplies audience, platforms, and purpose.

## Unity implementation status

The first uGUI pass is now installed in **AtmosphericScrubberEncounter**. It implements the approved composition, item inspection, capability-derived category filters, existing transactional move/assignment rules, equipment selection and layered Back behavior. In-menu Use, Split and world Drop remain disabled pending their real transaction/flow work. See [InventoryImplementation.md](InventoryImplementation.md) for exact scope, authoring assets, validation and limitations. The remaining behavior contract below describes the target, not a claim that every action is implemented.

### Current selection-popover direction

The latest owner direction replaces the permanent inspector with a raised item popover and adds depth through actual contrast, overlap and hierarchy. The authored inventory body is718×680 at1920×1080, offset200px left of screen center so the combined grid/popover stays balanced. The6×4 physical grid retains104px cells;96px icon canvases make silhouettes larger. Empty slots are darker with recessed upper edges and quiet outlines; occupied and selected surfaces have distinct weights.

Selecting an occupied carried slot shows a400px-wide, content-sized opaque lighter popover overlapping the right edge of the inventory by18px and following the selected row, clamped to the gameplay viewport. The popover contains the item sprite (extending above its surface), name, actual stack information, authored description and E/R/M commands. Selecting a hotbar item positions the same panel above the persistent bar. A feathered geometry shadow offset10px provides depth without new bitmap art. Empty slots hide the popover. Move/Assign destination flows hide it while preserving context guidance and the selected source; Back restores inspection after cancellation. No hover requirement or sound was added. No changes to transactions, item rules or the persistent hotbar.

Current native evidence: `work/Reviews/InventoryUnityPopover/`. This composition supersedes the fixed inspector and horizontal command strip below.

### Current compact Unity composition

The latest owner direction removes the hotbar from the inventory panel because it persists at the bottom of the game. At1920×1080, the native panel is1120×720, centered36px above screen center. The6×4 carried grid uses104px square icon-first cells with6px gaps; quantities remain in cells and full names appear in the inspector. The inspector integrates a192px sprite area,32px item name, real stack quantity/limit, and authored description. Equip, Assign to hotbar and Move form one44px horizontal command strip. The action row now includes direct keyboard shortcuts: E Equip, R Assign to hotbar, M Move, displayed as adjacent keycaps resolved from the shared input asset. General Select/Navigate instructions are removed. Command shortcuts apply only to available actions in the visible inventory and cannot replace an active destination operation.

A single persistent nine-slot hotbar sits bottom-center24px from the bottom, using64px cells and6px gaps. It stays visible both during exploration and inventory, and supplies the inventory's actual assignment destinations. Its menu interaction is gated while inventory is closed; exploration selection retains the existing input policy. Equipment uses the green underline independently from amber inspection/focus. Exploration HUD and the scrubber's floating warning/status text are suppressed while inventory is open and restored on close. The ship world remains visible around the smaller panel under a52% dimmer.

Battery's broken icon reference is repaired using existing project art. Oxygen and fuse have distinct new32px pixel sprites, with provenance in [InventoryArt.md](InventoryArt.md). No gameplay effects or statistics were invented. Current native evidence: [InventoryUnityCompact](../../work/Reviews/InventoryUnityCompact/). This section supersedes earlier panel sizes, internal-hotbar layouts and vertical action rows below.

### Earlier Unity distillation

The later distill pass removes the Paused label, repeated Back hint, redundant inspector category subtitles, persistent unavailable notice and unimplemented action stubs. It preserves the game layout, actual pause policy, capacity, equipment, stack limits and relevant handling/result feedback. Back is consolidated in the header; empty-state guidance does not repeat it. Use/Split/Drop remain target behavior, not implemented functionality silently removed. See [InventoryImplementation.md](InventoryImplementation.md) for scope and verification.

### Earlier Unity visual amplification

The owner subsequently requested a bolder game UI. The native inventory now uses44px/32px bold screen/item headings, a136px item preview, outlined cells, distinct focus corners,44px action rows with4px separation and compact control keycaps. Essential operating text stays18px. The detailed native treatment and verification are recorded in [InventoryImplementation.md](InventoryImplementation.md). Earlier browser captures below remain reference history; they do not supersede this later authorized Unity refinement.

## Confirmed scope

- PC and Mac, supporting keyboard and controller.
- Default design and review resolution: **1920×1080, 16:9**. Additional checks target standard desktop game resolutions; phones and tablets are out of scope unless requested.
- Carried items only. Locker and loot transfer screens are excluded.
- Inspect, rearrange, equip tools, assign hotbar slots, use consumables, split stacks, and drop items.
- Dropping places items into the world; it does not destroy them.
- Preserve the existing inventory pause policy and gameplay systems.
- Functional, modern, generation-ship character without skeuomorphism.

## Visual reference

Prototype: [InventoryPrototype/index.html](../../work/Reviews/InventoryPrototype/index.html).

The following historical browser captures show the earlier readability refinement. Current native evidence and dimensions are in the compact Unity section above:

| State | Reference | What it communicates |
|---|---|---|
| Resting inventory | [Default](../../.impeccable/review/inventory-readability/default.jpg) | Grid, selected-item details/actions, hotbar, pause and capacity |
| Consumable selected | [O₂ item](../../.impeccable/review/inventory-readability/consumable.jpg) | Readable name, description, metadata and all five actions |
| Move destination | [Move](../../.impeccable/review/inventory-readability/destination.jpg) | Held identity and guidance within the reserved context lane |
| Full inventory | [Full](../../.impeccable/review/inventory-readability/full.jpg) | Two-line names, quantities, disabled action and reason |
| Quantity selection | [Split](../../.impeccable/review/inventory-readability/quantity.jpg) | Enlarged dialog text and quantity controls |
| Placement failure | [Blocked drop](../../.impeccable/review/inventory-readability/error.jpg) | Legible failure text and retained inventory |

These captures are visual evidence, not pixel-exact production acceptance targets or approval of demonstration gameplay rules. The ship screenshot is illustrative background context rather than a current Unity scene capture. Browser-only controls, sample labels, native number-input chrome, and diagnostic screens do not belong in the game.

### Reference scale at 1920×1080

Preserve Exo 2, the flat generation-ship palette, amber focus and the grid/inspector/hotbar arrangement. Increase legibility through a larger composition rather than enlarging text inside the previous narrow panel.

| Role | Reference size | Treatment |
|---|---|---|
| Screen title | 36px | Weight 550, line height 36px |
| Selected-item and dialog headings | 30px | Weight 500; selected-item line height 33px |
| Item names and categories | 18px | Names allow two lines at 20.7px line height |
| Descriptions | 18px | 26.1px line height |
| Actions and operation receipts | 18px | Action rows 40px high |
| Metadata, quantities, equipment and navigation hints | 16px | Metadata line height 22.4px; tabular quantities |
| Keyboard glyphs | 16px | Weight 500, 19.2px line height |
| Held-item identity and destination detail | 16px | Two lines within a 56px context lane |

At the reference resolution the overlay measures **1400×870px**, centered with approximately 260px side margins. It has 36px horizontal padding, an 896px grid region, a 40px workspace gap, and a 392px inspector including its divider and 32px inset (359px readable width). Carried cells are approximately **144×108px**, separated by 6px. Hotbar cells are 80px high. The details region reserves 286px; five action rows reserve 200px. Quantity/confirmation dialogs are 480px wide and grow with their contents.

These are design-space reference pixels for the later Unity recreation, not a required UI framework or direct CSS-to-Unity conversion. Recheck equivalent apparent size with the chosen Unity canvas scaling and final fonts. Minimum supported desktop resolution and scaling below 1080p remain open; phone/tablet layouts are not targets. Representative sample names, descriptions, full stacks, destination guidance and dialog errors fit at 1080p without page scrolling or text clipping. Longer authored or localized content still needs Unity validation; allow wrapping and preserve actions instead of shrinking essential text.

### Composition

- A centered, compact overlay retains visible ship context around it.
- Header: Inventory, paused state, occupied/total carried capacity, and Back with the current input glyph.
- Left: a stable item grid, with All / Tools / Consumables / Miscellaneous filters above it.
- Right: selected-item sprite, name, compact category/stack metadata, description, and relevant actions.
- Below the grid: one reserved context line for destination guidance or operation feedback.
- Bottom: a separate hotbar, an explicit equipped-item/slot label, and concise navigation/action hints.
- Filtering preserves physical slot positions rather than repacking items. Beginning a destination operation exposes its valid destinations.
- Capacity and equipped state should each have one clear readout. Avoid repeated action chevrons, capacity captions, equipment badges, and decorative frames.

The prototype's 24 carried slots and nine hotbar slots are examples based on current defaults, not confirmed permanent limits. Render actual configured capacities. Category mapping is still a gameplay-data decision.

### Visual language and remaining visual work

- Use the shared guide's flat dark surfaces, technical typography, spacing, thin dividers, and semantic colors.
- Amber indicates focus/action; off-white and muted text establish hierarchy. Preserve separate focus and equipped/condition indications, with text or symbols rather than color alone. Cyan remains reserved for AI analysis.
- Keep item sprites sharp. Existing wrench/battery art and Exo 2 provide context; the prototype's outline O₂/fuse pictograms are placeholders rather than final art direction.
- Display names and quantities where they can be read at gameplay scale. Show technical codes only when authored player-facing data exists; never expose internal IDs as codes.
- Preserve a clear selected-slot marker and a visibly distinct held-item/destination state.
- **Readability refinement:** use the reference scale above: 18px operating text, 16px supporting labels, and the larger 1400px panel at 1920×1080. Verify apparent size again in Unity; avoid returning to the previous 12px operating text.
- Motion should explain an operation: a brief placement/equip response and an unobtrusive result. Feedback never delays the operation. Exact durations remain tuning values, not requirements; no sound is currently specified.

## Behavior contract

These rules describe player-visible behavior independently of a browser or Unity UI framework. Gameplay rules supply eligibility and outcomes.

| Interaction | Intended behavior | Result / cancellation |
|---|---|---|
| Open inventory | Open one primary interface, apply the existing pause/input policy, and restore valid prior focus when possible. | If opening is unavailable, remain in the current state; do not partly open or change items. |
| Inspect an item | Navigation changes focus and updates details. It does not equip, consume, move, or drop. | Return from a subflow to the same item when it still exists; otherwise choose a predictable occupied neighbor. |
| Filter categories | Change the visible/eligible category treatment while preserving item order and slot identity. | Do not move items or change capacity. Show a clear empty-category state. |
| Rearrange | Support an explicit pick-up/place flow usable with keyboard and controller. Mouse dragging may be an equivalent shortcut. Identify the source and valid destinations. | Commit only an accepted placement. Cancel or interrupted handling leaves items unchanged. Occupied-slot and merge rules come from gameplay; see open decisions. |
| Equip a tool | Make the chosen tool active through the existing hotbar/equipment relationship. Keep active equipment distinct from inspection focus. | Show success only after the actual equipment state changes. Do not create a separate equipment system or new equipment slots. |
| Assign to hotbar | Let the player choose a hotbar destination and show its eligibility. Assignment is distinct from explicitly using an item. | Confirm the actual assignment; explain rejection without losing the source. Storage/replacement semantics remain open. |
| Use a consumable | Invoke only an eligible item's gameplay action. Prevent duplicate activation while a result is pending. | Decrement quantity only according to a successful gameplay result. An unavailable/failed use retains the item and explains why. |
| Split a stack | Choose an integer amount from 1 through one less than the source quantity. Non-stackable items have no Split action. | Create the split through the existing container rules. Explain insufficient capacity. Cancel leaves the original stack unchanged. Destination policy remains open. |
| Drop | Choose a valid quantity, explain that it will become a world pickup, and require explicit confirmation in the current design flow. | Successful pickup placement and inventory removal form one accepted outcome. If placement fails, retain the quantity and show an actionable error. Placement rules remain open. |
| Back / close | Cancel the active quantity/destination subflow first. A subsequent Back closes the inventory and returns to exploration. | Do not execute the focused action or forward the closing input into the world. |

### Information before committing

Selected-item details must support the decision rather than require trial and error. A proposed refinement is to preview a consumable's gameplay-derived effect before Use, including a capped or wasted portion when relevant. At full capacity/vitals, show the applicable unavailable reason.

The browser's O₂ +20-point effect, stack quantities, automatic swaps, and first-free split placement are demonstration policies. They do not establish game balance, item rules, or storage architecture.

### Input and focus rules

- A hidden or inactive inventory receives no inventory actions.
- Only the active interface/subflow receives its input. Quantity and confirmation dialogs own navigation and activation until closed; underlying item actions cannot fire through them.
- Navigation and explicit activation are separate. All required actions and rejection reasons must be discoverable with keyboard and controller.
- Back acts on one level at a time. A held Back/confirm/action input must not cascade into a newly entered state.
- When focus or an input device returns, an already-held control must not accidentally confirm or drop an item. Interrupted handling cancels or returns to a defined safe state without changing quantities.
- Closing, cancellation, and action completion restore valid focus. An empty slot can be a destination but has no independent item action.
- Input glyphs reflect the actual device and binding. The browser's keys and standard gamepad mapping are examples, not approved final bindings.
- **Open:** whether hotbar digits 1–9 select/equip, activate/use, or merely identify slots. Do not infer their behavior from the prototype.

### Feedback and state model

Keep independent dimensions independent:

| Dimension | Required distinctions |
|---|---|
| Visibility | Hidden / visible |
| Navigation | Unfocused / focused / unavailable, with an accessible reason |
| Item handling | Inspecting / selecting destination / selecting quantity / confirming |
| Equipment | Active equipment separate from inspection focus |
| Operation | Idle / pending / succeeded / failed |

Show source identity and eligible targets during handling. A confirmed operation may produce a brief receipt in the shared context line; it must not steal focus or cover controls. Important state remains visible after transient feedback fades. Errors remain understandable until dismissed or resolved. Show progress only for real pending work; do not introduce a delay to stage an effect.

## Open decisions for the Unity handoff

| Decision | Boundary until resolved |
|---|---|
| Unity text/canvas mapping and minimum desktop resolution | Reference typography and panel bounds are defined above at 1920×1080. Confirm equivalent apparent size in Unity and specify scaling for smaller desktop resolutions. |
| Item categories and authored codes | Use real item data; do not invent categories or visible IDs to fill the layout. |
| Hotbar storage and replacement | Confirm physical stacks versus references and occupied-destination behavior. Browser swapping is not authoritative. |
| Equip versus Assign | Confirm whether Equip performs an assignment when needed, how its destination is chosen, and how replacing the active slot behaves. |
| Hotbar bindings | Confirm selection versus immediate activation and its scope during exploration versus inventory. |
| Stack merging and split destinations | Use approved container rules; first-free placement in the demo is not a requirement. |
| Consumable outcomes | Read actual eligibility, effect, cap, and quantity cost from gameplay data. |
| World-drop placement and recovery | Confirm valid placement, pickup representation, and subsequent collection behavior. Preserve atomic failure handling. |
| Feedback timing and final icon assets | Tune in the Unity scene at normal gameplay scale. |

These decisions can be resolved independently. They should not block review of the current composition or be silently implemented in the browser to appear settled.

## Unity implementation acceptance checks

When implementation is requested, evaluate these in the actual Unity scene with the existing inventory services, input/modal policy, and authored UI prefabs:

- Exploration → inventory → each scoped action → return works with keyboard and controller at 1920×1080.
- Inspecting never performs an item action. Active equipment and inspection focus remain distinct.
- Hidden inventory, underlying screens, and controls behind dialogs cannot activate.
- Back, held inputs, focus/device changes, and interrupted handling do not leak actions or lose quantities.
- Empty/full inventory, unavailable actions, invalid destinations, pending operations, and failed placement remain readable and recoverable.
- Drop creates a real world pickup only on a successful operation; a failed placement retains the source quantity.
- Saved layouts remain authored and editable. Gameplay rules are reused rather than reconstructed from browser code.

The visual prototype does not prove these checks. They are acceptance criteria for the later Unity implementation.

Popover input detail: confirming an occupied slot enters its first applicable command, supporting controller/keyboard navigation without traversing empty cells. Height is428px for tools and380px for other items.

## SOLITUDE inventory surface refinement

The owner clarified that all item art is placeholder and excluded it from this pass. Current inventory presentation uses authored cut-corner graphite surfaces with restrained vertical lighting falloff, a separate shadow under the inventory body, a stronger spaced title, a short amber header accent, quiet slot corner marks, and amber selected-slot fill. Unselected category buttons and secondary commands have transparent backgrounds. The popover shares the same cut-corner geometry and a small amber selection tab; green remains on the persistent equipment indication. World dimming is66%. Existing item art, positions, capacities, popover sizing, commands and input flows are unchanged by this pass.

`InventoryPanelGraphic` is static UI geometry with authored colors and corner cuts. `InventoryScreenAuthoring.RefineHousing()` updates the saved prefabs. Current visual evidence: `work/Reviews/InventoryUnityHousing/`,1920×1080 offscreen Unity renders. Default/full/focused/empty/lower-row/hotbar/destination states checked, with no TMP overflow in the measured states. Prior interaction evidence applies to unchanged behavior. Ordinary Game-window appearance and physical controller feel remain unverified.
