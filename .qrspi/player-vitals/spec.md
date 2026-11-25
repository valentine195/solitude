# QRSPI Spec: player-vitals

## Revision and Approval Status

Presentation revision, 2026-10-13: the human selected and authorized the two-line hungry notification. S13/S14 now specify amber `HUNGRY` with muted `Recovery paused` directly underneath, beside health. This revises presentation wording only; criterion IDs, recovery precedence, thresholds, gameplay rules, and Phase 2 scope remain unchanged. No criteria are added or retired. The initial behavioral contract was approved earlier in the workflow; this document is not verification evidence.

Option B, the dedicated selected-hotbar Use action, and the shared eligible gameplay clock are already approved. The defaults and edge policies below are explicit proposals to be adopted or revised at this Spec gate. They are not represented as earlier human decisions.

## Inputs

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`. Digests are SHA-256 of exact file bytes.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/player-vitals/request.md` | `973fd47d008b0038004d7c5b1b38c0e30eb2589d9f0f889ba36d675b53265b01` |
| `.qrspi/player-vitals/queries.md` | `bcc5d5935ed89d5752025bd6b4375d0f4d4f8ca47add9b472661d2dcbe1f9138` |
| `.qrspi/player-vitals/research.md` | `ae2eb3abdceba9f17fa94d05d4c9635203b8c56cff1d240edd420cb2d11978de` |
| `.qrspi/player-vitals/approach.md` | `f0caa809975d292bb10c358ced2328d71e0bac2a9f5891d4bc2e8a462e4eff7d` |

The selected approach's Decision preserves the human approval of Option B. Research reports no New Questions. Request has no unanswered Open Questions. The remaining behavioral choices identified by Shape are made concrete below for review.

## Scope and Existing Behavior

Currently, health and O2 are continuous clamped values with event-driven HUD fills. Awakening resets authored current values while retaining the existing Vital objects. No hunger, segmented recovery, consumable-use effects, or vital persistence were established by Research. Hotbar input selects a slot, and generic inventory removal already exists.

After this change, hunger limits delayed passive health recovery within configurable equal segments. Selected food and medical items can be consumed through Use, and the HUD explains their connection to recovery. Health remains continuous; segments constrain passive healing rather than damage or item healing. O2 retains its current behavior.

The approved separation of independent rules and runtime integration applies. Existing authoritative vitals retain their identity and compatible bindings. This specification defines observable contracts, not classes, component layouts, or implementation steps.

## Proposed Initial Tuning

All amounts below are vital units, not percentages, unless stated otherwise. These are initial values for functional verification; balancing and playtesting are deferred. They are authorable settings rather than an upgrade system.

| Setting | Proposed value |
| --- | --- |
| Health maximum and authored starting health | Preserve existing scene values; Wakeup uses 100 / 100 |
| O2 maximum and authored starting O2 | Preserve existing scene values; Wakeup uses 100 / 100 |
| Hunger maximum / authored starting hunger | 100 / 100 |
| Equal health segment count | 4 |
| Fed threshold | 20% of maximum hunger, inclusive |
| Hunger depletion | 0.1 units per eligible scaled second |
| Delay after damage | 5 eligible scaled seconds |
| Passive health recovery | 2 units per eligible scaled second |
| Ration restoration | 25 hunger units per item |
| Medical Kit restoration | 25 health units per item |
| Use bindings | Keyboard F and gamepad West button |

Food and medical definitions have their own positive fixed restoration amounts; later authored variants can differ without introducing a general effect framework. Health segment count supports other positive integer counts, including 3 and 2. Food availability and ship upgrade progression are not defined here.

## Behavioral Changes

### Hunger and the shared clock

Hunger is a bounded continuous value. It decreases during eligible gameplay, including while health is full, while recovery is delayed, and while hunger blocks healing. It stops at zero and never directly damages health, depletes O2, or requests death.

Hunger depletion, the damage delay, and passive recovery use the same scaled elapsed gameplay time. They advance only while the existing gameplay policy permits active gameplay. Unready, modal, primary-screen, paused, transitioning, and released/unbound states do not advance them. A screen that blocks gameplay without setting time scale to zero still stops these systems. Excluded time is never caught up when gameplay resumes.

At scale 0.5, ten real seconds of eligible gameplay represent five seconds for these systems. A ten-second pause contributes zero. The cooldown continues during eligible hungry gameplay even though healing is blocked; becoming fed does not introduce an additional delay when the original delay has elapsed.

### Damage and segmented passive recovery

The feature provides a deliberate damage operation. Positive finite damage reduces health, clamped to zero, and restarts the delay when it actually reduces health. Zero, negative, or nonfinite damage is rejected without mutation or cooldown changes. Initialization, awakening reset, maximum clamping, and general authored assignments are not damage events.

After the delay, a living player's health can recover only if hunger is at or above its threshold and health is below the top of its current segment. Passive recovery stops at the first segment boundary it reaches. An exact boundary is already that segment's top and does not qualify for recovery toward the next boundary. Zero health never passively recovers.

With maximum health 100 and four segments:

| Starting health | Passive recovery ceiling |
| --- | --- |
| 0 | 0 |
| 24 | 25 |
| 25 | 25 |
| 26 or 30 | 50 |
| 50 | 50 |
| 74 | 75 |
| 100 | 100 |

Damage from 50 to 40 can recover back to 50 after the delay. Medical healing from 30 to 55 makes 75 the new passive ceiling; the item can cross boundaries, while subsequent passive recovery is still bounded.

Time advancement respects changes inside an elapsed interval. It spends only the portion after the damage delay on healing, stops healing when hunger crosses below its threshold, and never carries unused healing time across a reached boundary. Subdividing an interval produces equivalent values and delay state within a documented small floating-point tolerance. Boundary tolerance must not turn a genuinely damaged value into permission to heal into a higher segment or allow accumulation beyond a boundary.

Example: health 30, hunger 100, and a newly restarted five-second delay followed by six eligible seconds produce health 32 and hunger 99.4. With the delay already complete, health 30 and hunger 20.1 followed by two eligible seconds produce health 32 and hunger 19.9: only the first second is fed time. Hunger exactly at 20 is fed, but continued depletion provides no positive healing interval below 20.

### Food and medical use

Use acts on the currently active hotbar slot, not an inventory pointer highlight or a remembered item. One fresh eligible press attempts to consume one item. Holding the control does not auto-repeat. Existing hotbar digit selection remains unchanged. Use shares gameplay input eligibility, close-activation-frame protection, and fresh-input-after-awakening behavior; a held button during a menu or awakening does not consume an item when gameplay resumes.

A Ration restores its fixed amount of hunger, clamped to maximum. A Medical Kit restores its fixed amount of health, clamped to maximum, regardless of hunger or the recovery delay. Medical healing does not count as damage or restart/erase an existing damage delay. Medical use does not modify hunger, and food use does not immediately heal health or erase the delay.

Proposed eligibility: the player must be available and have health above zero. Food requires hunger below maximum; medicine requires health below maximum. Use with no selection, an empty or unavailable slot, a non-consumable, invalid restoration settings, zero health, or an already-full target is rejected without consuming an item or changing vitals. Medical use at zero is not a revival feature.

Successful use consumes exactly one item from the selected stack and applies exactly one clamped effect as one coordinated result. Insufficient quantity, unavailable inventory, or failed consumption leaves both the inventory and vital unchanged. Notification/observer failures after a committed use must not make the input or caller replay consumption or apply the effect twice. Any reported outcome must distinguish a rejection from a committed use.

Examples: a Ration at hunger 90 restores to 100 and consumes one item; the same item at hunger 100 is retained. A Medical Kit at health 90 restores to 100; at health 100 it is retained. At health 30 and hunger 0, medicine restores health to 55. Removing the last item preserves the existing hotbar selection-clearing behavior.

### Lifecycle and configuration

Awakening keeps the existing health/O2 authored-reset behavior and Vital identity. Hunger resets to its authored starting value, full by default. Recovery timing is cleared; an awakening reset does not create a damage delay or retain a previous survivor's cooldown. If authored starting health lies inside a segment, it may recover on the next eligible advance when fed. Scene release stops advancement/use, and repeated binding must not multiply advancement or retain subscriptions to an old player.

Vitals are not added to the save schema in this feature. Recreating the player continues to use authored starting values, now including hunger. Consumed item quantities retain existing inventory persistence behavior. Save/reload continuation of vital values and cooldowns is deferred explicitly, even though item consumption persists.

Feature configuration must use finite positive health/hunger maxima, a positive integer segment count, a finite threshold between 0% and 100%, and finite nonnegative rates/delay. Restoration amounts must be finite and positive. Authored starting health/hunger are clamped into their valid ranges. Invalid configuration is surfaced as invalid, disables the affected new operations, and does not corrupt existing health/O2 or consume items; it is not silently turned into a different valid configuration. Zero rates/delay are valid authoring choices.

Supported changes to maximum health, maximum hunger, threshold, or segment count refresh the relevant HUD ratios/markers/status even when current values have not changed. Current values remain valid when maxima decrease. A maximum/configuration change is not damage and does not restart the delay. Increasing a maximum or changing segment count can change the passive ceiling according to the new configuration, without creating a ship upgrade mechanic.

### HUD behavior

The current health/O2 HUD remains usable. Health gains equal segment boundary markers; hunger gains its own labeled continuous bar and a visible regeneration-threshold marker. Markers match the same effective configuration used by the rules. The initial four-segment health bar has internal boundaries at 25%, 50%, and 75%. A three-segment configuration has two evenly spaced internal boundaries. O2 has no new hunger/recovery decoration or behavior.

Recovery status is derived from the same observable recovery state as healing, including hunger and delay changes while health stays fixed. Proposed status precedence is:

1. Zero health or invalid recovery configuration: `Recovery unavailable`.
2. Health already at its passive ceiling, including full health: no recovery-status text.
3. Recovery needed but gameplay ineligible: `Recovery paused`.
4. Recovery needed during gameplay but below the hunger threshold: amber `HUNGRY`, with smaller muted `Recovery paused` directly underneath.
5. Recovery needed and fed, with damage delay remaining: `Recovery delayed`.
6. Recovery needed, fed, eligible, and delay complete: `Recovering`.

The hungry message appears beside health so the relationship is explicit. Hunger remains legible when recovery text is hidden. Status does not claim that reaching a segment boundary allows further passive healing. Labels/markers/text communicate the rules without relying on color alone and remain legible without overlap or clipping in the actual Wakeup HUD at supported project display sizes.

The selected food/medical item exposes a compact Use hint with item name and current binding. An ineligible target gives a concise reason such as `Hunger full` or `Health full`; no selection or a non-consumable has no consumable-use hint. No new inventory screen or notification system is required. Binding/re-enable immediately reconciles fills, markers, status, and hint without changing vitals or consuming an item.

## Observable API and Data Contracts

- **Damage:** accepts an amount, reports applied/rejected, updates health and damage delay only under the rules above. Does not initiate combat, death, or succession.
- **Advance:** accepts eligible scaled elapsed seconds, reports/reflects bounded hunger, health, remaining delay, and recovery status. Zero elapsed is a no-op; negative/nonfinite elapsed is rejected without mutation. Ordered damage/use/advance calls have their documented immediate effects and never retroactively heal elapsed time.
- **Use selected item:** reports committed or a concrete rejection, preserves the coordinated inventory/effect contract, and uses the current selection at attempt time.
- **Recovery presentation:** exposes effective segment/threshold settings and the current reason for recovery availability. Values/settings/lifecycle updates are observable without requiring an unrelated health change.
- **New data:** hunger current/maximum, recovery tuning/timing/status, and focused authored food/medical restoration metadata. Provide one Ration and one Medical Kit definition compatible with existing item identity, stacking, transfer, and persistence. Existing definitions/assets/GUIDs remain compatible.
- **Persistence:** no new vital save fields or schema migration. Consumable definitions can be acquired/transferred through existing inventory capabilities. Shipping food placement, starter grants, and availability balance are deferred; verification can supply items through existing fixture capabilities.

## Integration and Compatibility

The affected boundaries are player vitals and awakening, gameplay policy/time eligibility, the selected hotbar Use action, coordinated inventory consumption, item definitions, and the Wakeup HUD. Independent rules and runtime integration follow approved Option B and existing assembly direction. This statement does not prescribe new type names or movement of current files.

Preserve health/O2 object identity, current clamping and non-consumable interactions, existing movement/Interact/Pause/Inventory/Journal controls, hotbar selection and transfers, inventory/locker save recovery, and succession reset behavior. Add Use without repurposing Interact. O2 consumption, recovery, death behavior, and authored values are unchanged. Preserve existing tests, asset GUIDs, and assembly boundaries.

## Acceptance Criteria and Verification

These criteria define observable completion; the evidence descriptions identify suitable verification, not an implementation plan or claimed results.

| ID | Acceptance criterion | Expected evidence |
| --- | --- | --- |
| S1 | Hunger initializes to authored bounded values, depletes at its configured rate only during eligible scaled gameplay, clamps at zero, and never directly changes health/O2 or requests death. | Independent rule checks plus Unity gameplay-gating integration. |
| S2 | Four equal default segments and configurable counts produce the documented ceilings; health remains continuous; exact boundaries and zero do not passively heal toward another segment. | Boundary examples including counts 4, 3, and 2, below/on/above boundaries, full and zero. |
| S3 | Applied damage restarts the configured delay; invalid/no-op damage does not; resets and maximum/configuration changes are not damage. | Ordered damage, repeated damage, and non-damage lifecycle/configuration checks. |
| S4 | Passive recovery requires fed status and elapsed delay, stops at its segment ceiling, and resumes after food without erasing or inventing a delay. | Fed/hungry/equality and food-during/after-delay cases. |
| S5 | Long advances respect delay expiry and hunger-threshold crossing, and match subdivided advances within a documented tolerance without accumulated boundary overshoot. | The six-second and two-second examples above, repeated small steps, and large-interval comparisons. |
| S6 | One eligible fresh Use press consumes exactly one selected Ration/Medical Kit and applies its clamped fixed effect; medicine works while hungry and crosses segments. | Inventory/effect integration, partially filled target, and health 30/hunger 0 medical example. |
| S7 | Full targets, zero health, missing/empty/unsupported selection, invalid effect settings, unavailable inventory, and failed removal consume nothing and change no vital. | Rejection outcomes and inventory/vital snapshots before and after. |
| S8 | Committed use cannot be replayed or double-applied due to holding input, duplicate routing, or post-commit observer failure; consuming the last selected item preserves selection clearing. | Fresh-press/hold tests, committed-failure boundary checks, and last-stack removal. |
| S9 | Use and passive clocks stop in every ineligible gameplay state, including a screen with nonzero time scale; excluded time/input is not replayed on resume or awakening. | Input/policy integration for modal, primary screen, pause, transition, unready, and released states; scale 0.5 case. |
| S10 | Awakening retains health/O2 Vital objects and authored resets, resets hunger to its authored value, clears prior recovery timing, and does not introduce duplicated advancement after rebinding. | Existing HUD/succession regressions plus hunger/timing/lifecycle integration. |
| S11 | Valid authored/configuration changes update bounded values and effective ceilings; invalid/negative/nonfinite settings or elapsed inputs have the stated rejection/disabled behavior without corrupting vitals or consuming items. | Independent validation and runtime authoring/configuration checks. |
| S12 | Health segment and hunger threshold markers match configuration, and fills/markers refresh on supported maximum/settings changes even if current stays unchanged. | Configuration/maximum-only presentation checks plus actual-scene screenshots. |
| S13 | The health recovery status follows the stated precedence and updates on hunger, delay, policy, configuration, and lifecycle changes without needing a health-value change. | State-to-text checks and focused Unity transitions with constant health. |
| S14 | Wakeup presents labeled health/O2/hunger, readable markers/recovery status and selected-consumable Use hints; immediate/re-enabled binding changes no gameplay values and has no clipping/overlap at supported display sizes. | Existing readiness regression plus actual-scene visual evidence for fed/delayed/hungry/boundary/full states and Use eligibility. |
| S15 | F/gamepad West performs Use without changing existing gameplay bindings or non-consumable interactions; existing hotbar selection/transfer behavior is preserved. | Production input-asset and interaction regression checks. |
| S16 | Ration/Medical Kit identities support existing grant/pickup authoring, stack/transfer/removal, and inventory persistence; no vital save schema is introduced, and recreation uses authored vital initialization. | Definition authoring and inventory/save compatibility checks. |
| S17 | Existing Wakeup composition, inventory/hotbar/locker contents, save recovery, HUD readiness, and succession compatibility continue to qualify. | Relevant existing regressions and complete `Tools/Verification/verify.py` gate with successful `results.json`. |

S17 requires the complete isolated macOS ARM64/Wakeup gate documented in `Tools/Verification/README.md`; a missing/failed result or `--skip-build` partial rerun does not qualify source changes. Reports/screenshots/disposable saves belong outside Assets. Additional focused rules, input, lifecycle, and actual-scene UI evidence are required for the new behavior; the existing gate alone does not prove every criterion. Verification must identify the source revision and criterion IDs.

## Out of Scope

Ship medical upgrades, food production or distribution balancing, starter-item grants, final economy tuning/playtesting, combat encounters, health-driven death wiring or revival, O2 depletion/refill effects, a generalized consumable framework, new inventory screens, gamepad hotbar navigation, rebinding-settings UI, new production art, and vitals/cooldown save persistence are deferred.

## Open Questions and Evidence Limits

No unresolved upstream questions block this draft. Proposed tuning, Use bindings, no-benefit consumption rejection, zero-health use rejection, awakening hunger/timing policy, status precedence, and the compact Use hint are submitted for approval in this revision. Revise them here if needed before Plan; approval of Option B alone did not approve them.

No product code has changed for this Spec. No tests, player builds, or visual checks have run. Layout/readability, runtime correctness, balancing, and platform qualification are not established by the existence of this document.
