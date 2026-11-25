# Ship System Restoration — Behavioral Specification

## Inputs

Project root: `/Users/jeremyvalentine/SOLITUDE`. Paths below are relative to that root; hashes are SHA-256 of exact file bytes.

| Input | SHA-256 |
|---|---|
| `.qrspi/ship-system-restoration/request.md` | `c5702ddd00b33fe951f763ee1087a47a02ef234f094354437d864a356e8207b8` |
| `.qrspi/ship-system-restoration/queries.md` | `15cb549d1444abdb39646930abacf3f4c01698df5bdd88e92d5dff594101f3d8` |
| `.qrspi/ship-system-restoration/research.md` | `73c7fceb71878834a1bda0eac8b01764d762979c70fff3c24ce6d12384b6db95` |

No approach artifact. Shape is skipped because the requested single-item, one-way restoration behavior can be specified directly from the existing interaction and inventory contracts. The human approved proceeding from Research. Research used the explicitly approved ambient-context isolation exception; it was source inspection, not runtime verification.

## Behavioral changes

Deliver reusable components and a separate demonstration scene, not a replacement of the production cryopod sequence. The demo provides the full discover → diagnose → acquire → install → restore → new-access loop using existing movement, interactions, inventory, and item acquisition.

Before: the opening socket checks possession, invokes a particular door directly, and keeps a local power flag. After: a restorable system has an independently readable **Offline** or **Restored** state. A junction requests restoration; consumers depend on that system's state, without depending on the junction or on one another. Different systems remain independent.

Each junction names one existing item definition and requires exactly one unit. Matching uses item identity, not display text. Installation takes one unit from player inventory and permanently installs it for the current demo run. Items in a locker or hotbar must first be transferred to player inventory using existing controls. Multiple ingredients, configurable quantities, extraction, and reversal are excluded.

Before installation, the junction's prompt or interaction feedback identifies the offline condition and required item. Without the item, interaction reports the requirement without mutation. With the item, interaction consumes exactly one unit and restores the system. Missing dependencies, invalid configuration, or rejected inventory removal cannot report success or change restoration state. Repeated or reentrant attempts cannot consume an additional item or repeat the state transition.

Consumers read current state when activated as well as react to changes. An unavailable system never grants access. Successful restoration makes the configured exit door usable; it does not automatically open it. The player subsequently opens the door and can pass through it. The terminal screen visibly changes from unpowered to powered, and terminal interaction displays a short configurable placeholder message. The message may be read again; no dialogue progression or one-time narrative state is required.

Diagnostics are included only as existing interaction prompts and feedback. No inspection screen, scanner, or separate diagnosis step is required. After restoration, junction feedback accurately reports restored state, including when focus has not changed.

## API and integration contracts

- Keep `IInteractable` and `InteractionResult` compatible with existing callers. Restoration and terminal interactions use these contracts.
- Add a restoration contract supporting current-state reads, a restore attempt with an explicit outcome, and notification of a successful state transition. Rejected and repeated attempts emit no new transition. State and inventory effects are committed before success is reported; observers cannot cause duplicate consumption.
- Consumers receive the system dependency independently of the junction and other consumers. They reconcile current state on activation; missed notifications and enable cycles cannot leave access or visuals stale. A consumer failure cannot undo committed restoration or prevent other valid consumers from observing it.
- Keep inventory identity, revision validation, ownership, failure reporting, and existing transfer semantics. Restoration must not mutate slot data outside the established inventory command boundary.
- Existing interaction-trigger notifications are not proof of restoration success: Research found they can accompany failed interactions. Current restoration state is authoritative.

Concrete API names, storage types, event transport, and scene wiring belong to Plan and implementation.

## Data and lifecycle

New authoring data identifies a system, the junction's required item, each consumer's system dependency, and minimal feedback/presentation content. Each system begins Offline. Its runtime state survives consumer disable/enable and junction disable/enable during the run; distinct systems do not share progress accidentally.

Disk persistence is deferred. No save-schema change is required. Each fresh demonstration run, including scene reload, resets both restoration progress and its disposable item/container/pickup state together. The demo must not read or overwrite the player's normal save. This prevents a consumed or previously collected battery from leaving a fresh offline system impossible to restore.

Future persistence would need stable system identity plus restored status, coordinated with existing inventory/pickup saves. Door usability and terminal power can be derived from restored status; persisting door position or narrative history is outside this slice. No generic persistence framework is introduced now.

## Compatibility and scope

Existing movement, inventory/hotbar transfer, containers, pickup rules, pause/input policy, save recovery, and scene ownership remain compatible. Production scenes and existing art/reference scenes are not migrated by this feature. Any shared interaction or door change must preserve unrelated consumers; only behavior needed by the demo is in scope. Existing unrelated door defects are not a separate cleanup project.

No power network simulation, quests, crafting, broad state-machine framework, ECS conversion, new art production, room-wide lighting pass, AI service, or finished narrative content.

## Acceptance criteria

- **S1 — Independent state:** Two configured systems start Offline. Restoring one leaves the other Offline. Current state remains correct after the junction or a consumer is disabled and re-enabled.
- **S2 — Diagnosis:** An offline junction communicates its required item. With no matching item in player inventory, an attempt reports the unmet requirement and changes neither inventory nor state. Hotbar-only and locker-only possession do not satisfy installation.
- **S3 — Installation:** With two matching units in player inventory, one successful installation leaves one unit, sets Restored, and exposes committed state to observers. Unrelated items remain unchanged. A matching unit obtained by existing pickup or container transfer works.
- **S4 — Rejection and repetition:** Invalid/unavailable dependencies or rejected removal cause no restoration and no item loss. Attempts after success, including a reentrant attempt during notifications, consume nothing further and emit no second transition.
- **S5 — Consumer synchronization:** A consumer enabled after restoration immediately reflects Restored without replaying installation. Repeated enable cycles create no duplicate reactions. Missing state does not grant access; one faulty consumer does not prevent another valid consumer from reflecting committed state.
- **S6 — Door access:** Before restoration, the demo exit remains impassable and interaction explains that power is unavailable. Restoration leaves it closed but usable. A subsequent door interaction opens it and permits passage.
- **S7 — Terminal and feedback:** Before restoration, the terminal is visibly unpowered and cannot display its powered content. Afterwards its screen visibly powers on and interaction displays configurable placeholder text. The junction reports restored state without requiring the player to leave and refocus it.
- **S8 — Demonstration reset and save isolation:** A fresh demo run offers an obtainable required item and an Offline system. After completing the loop and reloading the demo, that starting condition returns. Normal player save files and progress are unchanged.
- **S9 — Reuse and regression:** A second configured system/junction/consumer set requires no feature-specific source edits and remains independent. Existing inventory, pickup, transfer, input, and save/lifecycle checks relevant to changed shared code continue to pass.

## Verification expectations

Use focused Domain/Application checks for S1–S4 where behavior permits, and Unity integration checks for S5–S9. Verify visible feedback, terminal appearance, door collision, and the complete demo loop in PlayMode or a documented manual run. Include lifecycle/reset and normal-save-isolation checks. Plan must map each criterion to concrete checks and run the repository verification gate when affected shared code requires it. No test or runtime verification has run for this specification.

## Revision summary

Initial specification: S1–S9 added; none changed or retired. Main scope choices presented for approval are single-unit inventory-only installation, minimal prompt-based diagnosis, and demo-only reset semantics with disk persistence deferred.
