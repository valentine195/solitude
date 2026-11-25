# Inventory Unity implementation — first pass

## Weapon, Tool and quick use in Scrubber

The Scrubber demo uses the [equipment and quick-use spec](InventoryEquipmentQuickUseSpec.md). The Weapon and Tool rows show carried assignments and identify the item in hand. Inventory offers Equip or Unequip for eligible slots, Move for carried items, and Pin or Clear for eligible consumables. Press **Tab** to open Inventory. During exploration, **Q** or **left shoulder** switches between assigned slots. **F** or the **gamepad west button** uses the pinned item. Primary uses the item in hand. The HUD shows that item and shows the quick-use cue when a consumable is pinned.

New runs begin with the wrench carried and in hand. This demo catalog currently contains the wrench and no consumable with an authored Use effect. The focused journey supplies a temporary second tool and medical item. The demo uses its disposable save store. The equipped references and legacy migration are also covered at the save-session boundary.

`SOLITUDE > UI > Install Weapon, Tool and Quick Use` applies the authored layout to the Scrubber scene and enables its runtime configuration. The Scrubber builder reconnects the equipment HUD and vitals composition. The sections below record earlier UI iterations.

## Distilled inventory — current presentation

The owner requested removal of unnecessary clutter, explicitly including Paused. The native inventory now removes that label, consolidates keyboard/controller Back into the header (`ESC / B` for the existing default bindings), and keeps only Navigate and Select hints below. Gameplay still pauses through the existing modal policy.

Inspector category subtitles are omitted; category filters remain. Stackable items retain a concise `Stack quantity / limit` line. Non-stackable items omit it and bring the description closer to the title. Authored item descriptions, capacity, equipment state and slot quantities remain.

Unimplemented Use, Split and Drop controls are hidden, rather than presented as permanently disabled choices. Their implementation remains pending under the original behavior contract; no working action has been removed. The repeated unavailable-action notice and redundant empty-state Back explanation are gone. The context lane is reserved for handling instructions, receipts and failures; changing inspected item/category clears stale feedback.

Available Equip, Assign and Move actions, single-action focus, grid geometry and actual pause/input/container policies remain intact. The Editor authoring recipe preserves these cuts, including after a later amplification pass. `SOLITUDE > UI > Distill Inventory Presentation` applies the scoped prefab refinement without recreating its hierarchy.

Verified at1920×1080 using the existing offscreen render helper: default, full and focused views report no text overflow; exactly one action has amber fill; active-text checks confirm no Paused, persistent unavailable notice, placeholder action buttons or duplicate Back hint. Stack limits and destination guidance remain visible when relevant.10 focused journey checks passed after the simplification. A later presentation-only guard prevents description-offset accumulation during reinitialization; final compilation and authoring validation pass. Prior15 ApplicationCompositionTests are reused for unchanged underlying contracts. Normal Game-window/physical-controller checks remain unverified in this headless Editor.

Evidence: `work/Reviews/InventoryUnityDistilled/`. Review fixtures do not modify saves or starting loadouts. The scene is left out of play mode and not dirty. Shared HUD, authored item art, unrelated scenes and existing uncommitted work were preserved.

## Earlier bolder game presentation

The owner requested a bolder presentation directed toward video game inventories. This pass preserves the flat generation-ship identity, configured capacities, container services and existing behavior. It changes the native authored inventory rather than the browser prototype.

- 44px bold screen title, 32px bold item title, 18px bold slot/action labels, 16px supporting text. Descriptions retain 18px size with measured26.1px baseline spacing.
- Selected-item artwork has a136×136px display inside a359×144px flat preview field. The field hides when no authored icon is available.
- The stable24-slot grid and nine-slot hotbar retain their positions. Cells gain thin neutral boundaries. Carried artwork grows to56px and hotbar artwork to52px. Two-line names have a52px text region and remain18px.
- Item inspection has a thin amber border; actual slot navigation focus has thicker amber corners; equipped state keeps its separate green underline and explicit label.
- Only the focused action gets amber fill and light corner markers. The primary action rests on a dark surface with amber text, so Equip no longer merges visually with a focused Assign row.
- Category tabs gain stronger spacing, weight and selected treatment. Back and navigation hints use compact keycap groups. The existing paused-state label, capacity, metadata and action wording remain.
- Panel opacity is now fully opaque, preventing world labels from ghosting through its interior. The surrounding scene and HUD were not restyled.

`SOLITUDE > UI > Amplify Inventory Presentation` applies this scoped style to the existing authored prefabs without recreating them. Initial authoring also applies it to newly created inventory prefabs. Existing GUIDs and bindings are retained.

Evidence is in `work/Reviews/InventoryUnityBolder/`. Default and full-grid text report no TMP overflow. The focus proof reports exactly one amber-filled action: Assign to hotbar.10 journey checks passed after the focus styling changes; the final subsequent adjustments were slot-name fit, description leading, preview backing visibility/order and header positioning. Gameplay, input routing and transactions were unchanged. Prior15 ApplicationCompositionTests are reused for those unchanged contracts rather than rerunning unrelated gameplay tests.

The screenshots use representative transient container fixtures and the existing offscreen Unity render helper; they do not change starting loadout or saves. A normal Game-window and physical-controller visual check remain unverified in the headless Editor. Missing/stale authored item artwork remains an asset issue; no invented item artwork or effects were added. Use/Split/Drop implementation status is unchanged.

## Scene and authored assets

Target: `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity`, explicitly selected by the owner. The shipping Wakeup scene has not been migrated.

- `Assets/_Project/Systems/Containers/Prefabs/InventoryScreen.prefab`: editable uGUI hierarchy; Screen Space Overlay canvas, 1920×1080 reference, 1120×720 centered panel, six-column carried grid, inspector, horizontal command strip, capacity and Back.
- `PersistentHotbar.prefab`: one bottom-centered hotbar shared by exploration and inventory, independent of the inventory overlay.
- `InventoryCarriedSlot.prefab` and `InventoryHotbarSlot.prefab` in that directory: authored dynamic slot templates, selection marker and independent equipment indication.
- `Assets/_Project/Systems/Containers/Materials/InventoryText.mat`: neutral scoped material for the existing Exo 2 Regular SDF asset. The shared font and its original tinted material are unchanged.
- `InventoryScreenView`: presentation, current-item inspection, filters, destination flow and event wiring.
- `InventoryScreenAuthoring`: one-time Editor authoring and scene migration. Authoring does not overwrite an existing screen prefab, so manual hierarchy changes survive. The scrubber builder retains an installed screen during later scene migrations.

No fixed screen is constructed in Awake. Runtime instantiates only authored slot templates. SceneBindings supplies the existing containers, command service, selection, input host and modal pause policy. Existing locker/transfer assets are unchanged; optional name/focus/filter presentation fields were added to shared slot/view components.

## Playable behavior

Enter play mode in AtmosphericScrubberEncounter and press **Tab** (the existing inventory binding), or the existing controller inventory binding. Arrows/D-pad navigate; Enter/A submits; Escape/B cancels destination selection first, then closes. Back in the header follows the same layered cancellation.

Inspection changes details without performing an item action. The actual configured carried capacity is used; the default 24 slots fit without scrolling at 1080p. Category filtering dims unrelated physical slots without repacking them. Category mapping currently derives from real capabilities: supported vital consumables → Consumables; authored item actions → Tools; remaining items → Miscellaneous. This is presentation mapping, not a new authored taxonomy. Existing O2 canisters have no supported vital-use specification and therefore do not acquire a fabricated effect/category through this UI.

Move and hotbar assignment use the existing revision-checked Transfer command: actual stack ownership changes, matching stacks merge to the authored limit, and different stacks follow existing swap/eligibility rules. Assign does not automatically equip. Equip from a carried item asks for a hotbar destination, transfers the stack and selects that slot only after a successful transfer. Equip from an existing hotbar slot selects it. Number-key behavior remains the existing exploration-only selection policy.

The old legacy hotbar is replaced in the requested scene by one persistent bottom-screen hotbar. It remains visible while the inventory opens/closes; its same live slots serve assignment and equipment selection. Menu pointer/navigation input is gated while inventory is closed, and inspection highlights clear while active equipment remains. There is no duplicate internal hotbar. HUDCanvas and the scrubber floating warning/status visuals are suppressed only while inventory is open, with previous visibility restored on close. The1120×720 panel reveals more of the real ship world with a52% dimmer. Carried cells are104px squares, icon-first; full names appear only in the inspector. Actions are44px horizontal commands.

Battery artwork now resolves to the existing project sprite. Oxygen/fuse have distinct32×32 point-filtered pixel sprites; see [InventoryArt.md](InventoryArt.md) for provenance. Unknown/missing art still avoids white quads; descriptive item information remains in the inspector. No item balance or capabilities changed.

## Deliberately incomplete actions

This is the first Unity UI implementation, not completion of every behavior in the design specification:

- **In-menu Use:** disabled. The current vital-use target requires unpaused gameplay. A scoped, transactional inventory-use eligibility path and effect preview are still needed; existing hotbar use during exploration remains unchanged.
- **Split stack:** disabled. A validated atomic split command, quantity UI and destination policy are still needed.
- **Drop into the world:** disabled. A quantity/confirmation flow and coordinated world-pickup/inventory transaction are still needed. Removing items without creating pickups is not an implementation of Drop.

These actions are not simulated and do not mutate items. Their unavailable controls remain hidden. Additional UI flow work should build on the existing Domain/Application contracts rather than copy browser demonstration policies.

## Verification

Evidence: `work/Reviews/InventoryUnity/`.

- Scripts compiled with no reported compilation errors.
- SceneBindings and screen authoring validation pass; one EventSystem; scene saved and not dirty after verification.
- 15 existing ApplicationCompositionTests passed: modal pause/gating, cancellation, transfer/revisions, selection and lifecycle boundaries. Their source was unchanged by the later typography/icon-only refinements.
- 10 focused journey checks passed on the final runtime scripts: real inventory key opens/pauses, inspection, moving an actual starting hotbar wrench into carried storage, capability-derived filtering, assignment without implicit selection, equip, Back cancellation, close/resume, reopen without duplicate slots, simulated gamepad Back.
- Additional simulated controller checks passed through the shared input asset and UI input module: D-pad navigation, A enters assignment, B cancels while retaining the screen.
- Empty, wrench, equipped and full inventory rendered at 1920×1080. Full-stack scenario has 24 real authored item stacks with quantity99 and reports no TMP overflow. Full-state population was transient review setup using the existing Restore command; it is not a starting-loadout or save change.
- The Editor connection is headless. Frame advancement was confirmed. Input verification temporarily enabled unfocused input processing and explicitly dispatched Dynamic input updates; settings and simulated devices were restored. Screen capture has no Game-window render target, so the Editor review helper temporarily rendered the actual scene/canvases through the scene camera, restoring canvas modes/sorting/camera target afterward. Images are offscreen Unity renders, not proof of a native Game-window or physical-controller check.
- Console evidence contains integration-command/capture failures, including older Pipeline command aliases; no application runtime exception was observed in the reviewed entries. No plugin upgrade was performed.

Unverified: physical controller feel, ordinary Game-window composition, smaller desktop resolutions, localization/long authored text, capacities beyond current defaults, mouse drag regression journeys, and later Use/Split/Drop flows. No full gameplay suite or unrelated build was run. The initial dotnet-test invocation only restored the custom executable project and did not execute tests; the 15-test result comes from the focused Unity test runner instead.

Editor is left in Edit mode in the requested scene. Existing unrelated SOLITUDE_Start scene changes were preserved. Unity serializer whitespace remains in serialized scene output; scoped C# diff checks pass.

## Compact pass verification

Latest evidence is in `work/Reviews/InventoryUnityCompact/`; prior folders are historical. The current focused application suite passes15/15. Native journey checks cover the persistent hotbar, HUD suppression/restoration, actual inventory key, move/assign/equip, cancel/close/reopen and simulated controller Back. Final renders cover empty, mixed-item, action-focus, full capacity and handling states at1920×1080. Headless offscreen-render and physical-device limitations above still apply.

## Action keyboard keycaps

The inventory command strip now shows **E Equip**, **R Assign to hotbar**, and **M Move**, using compact32×28px keycaps. Generic Select/Navigate instructions are removed. These commands are authored in the shared SOLITUDE input asset and dispatched through UnityInputHost to the same button actions; keycap text resolves from the actual keyboard bindings. They require a visible inventory, an applicable action and no active destination operation. Back keeps its existing cancellation layers. Command widths184/260/184px accommodate the longest label without shrinking operating text.

Latest focused evidence: `work/Reviews/InventoryUnityActionKeys/`. Prior compact composition evidence remains applicable to unchanged layout regions and gameplay services.

## Selection popover / depth pass

Latest owner-approved direction replaces the fixed inspector and bottom command row with an authored400px-wide, content-sized item popover. The main inventory is718×680 with the existing6×4 grid. Item icons are96px canvases inside104px cells; deeper empty slots and brighter occupied/selected surfaces improve figure-ground contrast. The popover sits above the grid edge with a lighter opaque face, top surface highlight, feathered offset shadow and a sprite extending above its surface. Its item details and E/R/M action keycaps are serialized children, preserving existing button callbacks.

InventoryScreenView updates visibility and position from real selection. The popover is hidden on empty slots and during destination handling, follows the selected carried row and clamps to the viewport; hotbar selections place it above the persistent bar. It remains available while navigating into its commands. `InventorySoftShadow` renders a static feathered mesh without a bitmap or per-frame rebuild. No particles, sounds, invented stats or new transaction behavior.

Authoring: `InventoryScreenAuthoring.SelectionPopover()` migrates the saved prefab while preserving existing object references. Runtime still creates only dynamic slot instances from authored templates. Current evidence: `work/Reviews/InventoryUnityPopover/`.

Popover input detail: confirming an occupied slot enters its first applicable command, supporting controller/keyboard navigation without traversing empty cells. Height is428px for tools and380px for other items.

## SOLITUDE inventory surface refinement

The owner clarified that all item art is placeholder and excluded it from this pass. Current inventory presentation uses authored cut-corner graphite surfaces with restrained vertical lighting falloff, a separate shadow under the inventory body, a stronger spaced title, a short amber header accent, quiet slot corner marks, and amber selected-slot fill. Unselected category buttons and secondary commands have transparent backgrounds. The popover shares the same cut-corner geometry and a small amber selection tab; green remains on the persistent equipment indication. World dimming is66%. Existing item art, positions, capacities, popover sizing, commands and input flows are unchanged by this pass.

`InventoryPanelGraphic` is static UI geometry with authored colors and corner cuts. `InventoryScreenAuthoring.RefineHousing()` updates the saved prefabs. Current visual evidence: `work/Reviews/InventoryUnityHousing/`,1920×1080 offscreen Unity renders. Default/full/focused/empty/lower-row/hotbar/destination states checked, with no TMP overflow in the measured states. Prior interaction evidence applies to unchanged behavior. Ordinary Game-window appearance and physical controller feel remain unverified.
