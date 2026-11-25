# Atmospheric scrubber encounter

## Purpose and scope

One playable, repeatable encounter: discover elevated room CO₂, inspect a scrubber with a blown fuse, collect one replacement fuse in that room, install it, restart, and watch the room recover. This is a standalone nonshipping encounter; its entire state resets on scene reload and it does not modify production saves. Persistence is deferred deliberately to keep item and equipment state consistent within this first slice.

## Inputs

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`. Digests are SHA-256 of exact file bytes.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/atmospheric-scrubber-encounter/request.md` | `4f4948a9b2d560c8504a159eef32e1eeec19bfee713f327efbca3349ff3e63ba` |
| `.qrspi/atmospheric-scrubber-encounter/queries.md` | `838a71084320fa40b5802adc8a9660fba62a17200150c9d50e551dcd9a558f61` |
| `.qrspi/atmospheric-scrubber-encounter/research.md` | `95d0ee277b0d33a8dbd2ee60f75577cfae772fcbee3b5d457da87a405331866d` |
| `Assets/_Project/UI/README.md` (supplemental UI direction) | `a87fa092e17bc433dbb3958e6d9b6dc849a2f886e5cd7ce50f254c997a0d3305` |

No approach artifact; optional Shape skipped. Initial spec: S1–S10 are new; no changed or retired criteria. Research inspected source and tests; it did not execute tests. The UI guide is used here as design direction, outside isolated Research.

## Before → after

Current comparable restoration consumes one item and immediately marks a system restored, with terminal and door feedback. No atmospheric scrubber, CO₂ telemetry, or atmospheric task integration was found in the inspected sources. This encounter adds separate repair and restart actions, a local atmospheric recovery, and a retrievable encounter objective. Existing restoration, inventory, pickups, save behavior, doors, and the shipping entry remain compatible.

## Player actions and system transitions

The room starts with an amber, text-labeled CO₂ warning and an audible alert; the scrubber shows red OFFLINE. One reachable fuse pickup is in the room. Neither the warning nor recovery changes player health or oxygen vitals. Accessing the scrubber shows its confirmed blown-fuse fault, operating state, fuse condition, airflow, room CO₂, objective, and repair/restart controls.

| State | Telemetry / objective | Action and result |
| --- | --- | --- |
| Faulted | OFFLINE; fuse BLOWN; airflow STOPPED; CO₂ ELEVATED. Objective: replace fuse. | Install Fuse without it: disabled with “Requires 1 replacement fuse in inventory.” Restart: fails with “Restart failed: blown fuse. Replace fuse first.” Both preserve all state and items. |
| Faulted, carrying fuse | Same telemetry. | Install Fuse consumes exactly one replacement fuse from player inventory and enters Repaired Offline. Feedback: “Fuse replaced. Restart scrubber.” |
| Repaired Offline | OFFLINE; fuse INTACT; airflow STOPPED; CO₂ ELEVATED. Objective: restart scrubber. | Install Fuse disabled: “Fuse already installed.” Restart succeeds, entering Recovering. |
| Recovering | RUNNING; fuse INTACT; airflow ACTIVE; CO₂ RECOVERING. Objective: wait for room air to recover. | Controls cannot repeat repair/restart. Five seconds of active encounter time leads to Operational. |
| Operational | RUNNING; fuse INTACT; airflow ACTIVE; CO₂ NORMAL. Objective: complete. | Repair/restart disabled with explanations; inspection remains available. |

CO₂ telemetry is categorical for this version; it makes no numerical concentration or medical-safety claim. Component health and current airflow are distinct: adjacent intake/distribution connections are labeled by actual flow, not implied nominal airflow while the scrubber is stopped. There is no additional fault, random restart failure, resource drain, or new prerequisite after fuse replacement.

Restart success appears only when RUNNING is confirmed. Equipment indication becomes green with a RUNNING label, the warning changes to RECOVERING, and an operating sound starts. At Operational the room warning clears, alert sound stops, and one temporary task-completion notification appears. The objective remains readable in the scrubber interface after that notification expires. Recovery continues while the interface is open or closed; it does not require the player to remain in proximity.

## Interaction and presentation contract

Use existing world focus and pickup interaction conventions. A full inventory leaves the fuse pickup present and shows the existing capacity feedback. Only the player's inventory satisfies the repair requirement; a fuse in the hotbar or another container does not. Removing or transferring the fuse before activation makes installation unavailable without consuming anything.

Access opens one primary system interface, hiding world prompts and blocking world movement/interactions while it is open. Encounter time continues. Back/Close returns to exploration and consumes the closing input. Opening inventory requires closing the system interface first. Reopening preserves equipment selection and reads current encounter state; inspection alone never mutates it.

Apply the UI guide's flat panels, technical typography, header/location, upper schematic, and lower telemetry/actions layout. Amber focus stays separate from red OFFLINE; green means operational, and all status colors include words or symbols. Cyan is unused. Disabled actions remain readable and explain their prerequisite; focus never activates an action. Successful installation and restart commit before feedback; repeated inputs cannot duplicate consumption or transition notifications. Unexpected unavailability/rejection reports failure without partial state or item changes.

## Data, interfaces, and integration boundaries

New encounter data comprises equipment state, fuse pickup/possession, categorical room condition, recovery progress, and objective stage. These describe one coherent encounter and reset together. No production save schema changes are included.

The behavioral action contracts are inspect (read current state), install (consume one valid inventory item and commit repair together), and restart (reject while faulted; otherwise confirm running and start recovery exactly once). No public API signatures are prescribed. Integrations affect world interaction/focus, inventory item use, equipment presentation, room warning/audio, and temporary task feedback. They preserve existing command atomicity, pickup identity/capacity behavior, modal input ownership, independent equipment behavior, and assembly ownership. Encounter content stays under its first-party owner; review outputs stay outside Assets.

## Acceptance and verification

| ID | Observable acceptance criterion | Verification |
| --- | --- | --- |
| S1 | Fresh encounter contains one room, one blown-fuse scrubber, one reachable replacement fuse, elevated CO₂ warning and audible alert; no atmospheric damage/vital change occurs. | PlayMode setup and manual world/audio check. |
| S2 | Access shows matching fault, operating state, fuse, airflow, CO₂ and objective; focus and confirmed condition stay visually distinct with text equivalents. | PlayMode state/presentation checks and manual readability at gameplay scale. |
| S3 | Pickup grants exactly one fuse once; capacity refusal grants none and leaves pickup available. | Existing pickup Domain/Editor coverage plus encounter PlayMode collection check. |
| S4 | Missing, wrong, or noninventory item cannot install; explanatory feedback appears and all state/items are preserved. Faulted restart fails with the blown-fuse reason. | Domain action checks and PlayMode disabled-reason/restart feedback checks. |
| S5 | Installation consumes one inventory fuse once and enters Repaired Offline; room CO₂/airflow remain unchanged until restart. Repeated/reentrant activation cannot consume more. | Domain atomicity/repetition checks and PlayMode transition check. |
| S6 | Restart after repair confirms RUNNING, starts active airflow/operating sound and RECOVERING feedback once; five seconds later CO₂ is NORMAL and objective completes once. | Domain timing/transition checks, PlayMode sequence, and manual audio check. |
| S7 | Close/reopen or leaving proximity preserves current state and does not interrupt recovery; duplicate controls cannot repeat repair or restart. | PlayMode lifecycle/input checks. |
| S8 | Primary interface hides prompts and blocks world input; focus does not activate; close input cannot trigger an underlying world interaction. | PlayMode modal/input checks and manual navigation. |
| S9 | Completion clears warning/alert, shows a temporary notification, and remains inspectable afterward. Diagram, details and world indicators agree in every state. | PlayMode state/notification checks and manual feedback review. |
| S10 | Scene reload resets fuse, inventory, equipment, room and objective together; existing production saves and existing restoration/pickup behavior are preserved. | Demo lifecycle and production/repair regression checks; applicable complete project verification gate during implementation. |

Checks listed here are required evidence for later implementation, not reported passes. Existing tests supply conventions and regression coverage; new encounter behavior needs its own meaningful checks.

## Out of scope

Additional rooms/faults/items/outcomes; production encounter persistence; ship-wide gas simulation or numerical CO₂ thresholds; damage; new map, AI overlay, inventory redesign, or standalone task screen; new permanent HUD; bypass controls; door-unlock rewards; new shipping entry; broader UI migration.
