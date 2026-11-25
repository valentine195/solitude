# SOLITUDE UI Guide

Use this guide for all future SOLITUDE UI design, implementation, and review. It records the direction established by the SOLITUDE UI Mockup v0.3 and its UX specification. Preserve that direction; document unresolved behavior before implementing dependent features.

**Core principles:** Retro systems. Modern UX. World-first exploration. Flat, non-skeuomorphic surfaces. Strong typography, spacing, dividers, labels, and technical diagrams. Minimal permanent HUD.

**Color semantics:** Amber = action/focus (and explicitly labeled warnings); green = operational; red = fault/danger; cyan = AI analysis only. Use off-white for primary text and muted gray for secondary or unknown information. Always supplement status color with text or symbols.

Shared visual primitives belong in this `UI` folder. Keep feature-specific screens with their owning feature, interaction prompts in `Systems/Interaction/UI`, and shared inventory/transfer presentation in `Systems/Containers`, following the [project placement rules](../../../README.md). This guide does not require moving existing assets.

## 1. Screen intent

- **Exploration HUD:** Keep the ship and player readable, with only cell charge and location permanently visible. Show interaction opportunities and task changes temporarily.
- **Map:** Orient the player within a deck and communicate position, objective, route, doors, and selected ship system.
- **Inventory:** Present carried items in stable categories with names, technical codes, quantities, and an unmistakable selection.
- **Ship systems:** Explain subsystem connections, condition, telemetry, and available actions. The mockup illustrates an offline scrubber between nominal intake and distribution nodes.
- **AI diagnostic overlay:** Add spatial analysis to exploration: interrupted flow, suspected failure point, and suggested route. Distinguish AI interpretation from confirmed status.

## 2. Component inventory

| Component | Purpose / visual treatment | States | Appears / disappears |
| --- | --- | --- | --- |
| Text styles | Narrow technical typography; screen title, section/action label, body, small code/metadata/telemetry. Off-white primary, gray secondary. | Primary, secondary, unavailable | With associated content; essential labels remain readable at gameplay scale. |
| Panel / divider | Flat near-black backing; thin gray rules separate sections. No simulated hardware or decorative frames. | Standard, overlay | While the interface is open. |
| Status label | Dot/icon plus word: green nominal, amber warning, red fault/offline, gray unknown. | Nominal, warning, fault, unknown | Beside monitored nodes and in selected-unit details. |
| Key glyph | Filled amber keycap with dark character; reflects current binding and input device. | Available, pressed | With actionable contextual prompts only. |
| Contextual prompt | Short verb such as ACCESS; amber text on dark backing, near target. | Available, focused | While target is eligible and in range; hidden during primary interfaces. |
| Task notification | Amber vertical rule and heading, thin divider, concise task/location text. | Entering, visible, exiting | Temporarily after a task change. |
| Selection marker | Amber slot outline, tab underline, or filled action row with dark text. | Unselected, selected, focused, disabled | Within active interface. |
| Inventory slot | Equal-size cell: icon, code, name, consistent quantity corner. Empty cell uses muted plus. | Occupied, empty, selected, unavailable | Within category grids. |
| Schematic node | Compact labeled block/endpoint with status; preserve scrubber's clipped corners. | Nominal, warning, fault, unknown, selected | Within system diagram. |
| Route / connection | Thin directional system lines; dashed bypass; solid amber map route; dashed cyan AI suggestion. | Connected, interrupted, bypassed, suggested | While corresponding route or analysis exists. |
| Map marker / legend | Distinct player, objective, door shapes; objective uses amber ring. Match legend. | Current, selected; unknown if supported | On active deck map. |
| Charge / location | Segments plus percentage at upper left; deck/system/room breadcrumb at lower right. | Current, warning, unknown | During exploration. |
| AI annotation | Cyan bracket, dashed route, short analysis label, mode indicator. | Analyzing, annotated, unavailable | Only while AI analysis is active. |

## 3. Layout specification

**Shared:** The mockup banner, numbered captions, and explanatory notes are presentation material, not runtime UI. Lower panels represent separate interfaces. Establish hierarchy through spacing and thin rules; prioritize text contrast over background detail.

**Exploration:** Preserve approximately 90–95% of the view for the world. Charge occupies upper-left safe area; location lower right. Anchor prompts beside targets without covering player/equipment. Use a right-side notification lane; reposition or defer messages that obscure prompts.

**Map:** Full-screen. Deck title/section upper left; horizontal deck tabs above schematic. Give the schematic most space; system filters occupy a narrow right column; legend sits lower left. Preserve map proportions without stretching corridors.

**Inventory:** Header above Tools, Consumables, Miscellaneous category grids. Align equal-size slots and consistent category spacing. Preserve item order during navigation. Separate quantity from code/name. Item details, equipment slots, and item actions are unspecified; do not invent them.

**Ship systems:** Header title left, deck/room metadata right. Upper area: intake left, scrubber center, distribution right, bypass below. Lower area: selected-unit title, telemetry left, actions right. Keep amber focus distinct from red fault labels.

**AI overlay:** Retain world view. Analysis indicator upper right; flow/route legend lower left. Anchor brackets and labels to equipment and routes to world positions.

**Unresolved:** Inventory/system interface size, pause behavior, exact typography, spacing values, and color values are not defined by the mockup. Use established project visual assets where suitable; validate readability before locking values.

## 4. Interaction rules

- **Prompts:** Show one primary eligible target. Resolve competing targets by proximity/facing, with persistence to prevent flicker. Hide when out of range, unavailable, or a primary interface opens.
- **Focus:** Navigation changes focus; explicit activation performs actions. Retain selection on return. Empty slots have no action without a defined operation.
- **Open/close:** One primary map, inventory, or system interface at a time. Back/close returns to exploration without activating focus. Consume the closing input so it cannot trigger a world action.
- **Notifications:** Queue in one lane; never steal focus. Provisional dwell is four seconds, subject to readability testing. If a task record exists, task information remains retrievable there.
- **Faults:** Show confirmed status in diagram and details. Red OFFLINE remains red when its control has amber focus. Report restart success only after confirmed system response.
- **Actions:** Disabled actions cannot activate and explain why. Pending actions show progress and block duplicate activation.
- **AI:** Add cyan annotations without recoloring confirmed status. Closing analysis removes all annotations. Use “likely”/“suggested” for inference; refresh or remove stale findings.
- **Unresolved:** Bindings, AI activation, action consequences, notification history, and destructive-action confirmation require explicit decisions.

## 5. State model

Model independent dimensions so states coexist:

| Dimension | Minimum states | Rule |
| --- | --- | --- |
| Interaction | Normal, selected, disabled | Amber identifies selection/action; disabled cannot activate. |
| Condition | Nominal, warning, fault, unknown | Green, amber, red, gray; include text/symbol. |
| Analysis | Unannotated, AI-annotated | Cyan reserved for AI interpretation. |
| Action lifecycle | Idle, pending, succeeded, failed | Results follow actual outcomes. |
| Visibility | Hidden, visible | Hidden UI receives no input. |

A selected faulty unit retains red OFFLINE and gains a separate amber marker. Warning/charge thresholds and nominal downstream distribution during scrubber failure are unresolved; distinguish component health from current flow.

## 6. Implementation priorities

- **MVP:** Shared text/color/divider styles; charge/location; one prompt; one notification; one scrubber screen with status, telemetry, focus, restart feedback. Verify exploration → access → action → return.
- **Next pass:** Deck map with tabs/filters/markers/route; categorized inventory navigation; AI interrupted-flow annotation and suggested route.
- **Polish:** Responsive scaling, text readability, input glyphs, collision-aware labels, restrained transitions/audio, notification timing.

## 7. Exact handoff checklist

- [ ] Read this guide before UI work; apply project ownership rules.
- [ ] Exclude presentation banner/captions/notes.
- [ ] Preserve flat surfaces, technical typography, spacing, and dividers.
- [ ] Apply semantic colors with text/symbol equivalents.
- [ ] Keep charge/location as the only permanent exploration readouts.
- [ ] Show one eligible prompt; hide during primary interfaces.
- [ ] Follow specified hierarchy, alignment, and relative placement.
- [ ] Separate selection, condition, and AI annotation states.
- [ ] Support activation, disabled reasons, pending feedback, safe close.
- [ ] Synchronize diagram status and unit details.
- [ ] Label AI inference and clear annotations when analysis closes.
- [ ] Resolve documented ambiguities before dependent behavior.
