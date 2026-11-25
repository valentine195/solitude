# QRSPI Spec: run-lifecycle

## Status and intent

Initial specification, awaiting human approval. No implementation is authorized by this document's existence. This is the behavioral delta for the first playable cryobay → maintenance → atmospheric scrubber loop, using selected Option A: persistent session ownership, retained-avatar succession, and selective expedition refresh.

Later clarifications override the original brief: new colonists start with **empty inventory and hotbar**; there is **no expedition completion state**; supplies are limited to bandages/rations; progress survives application restart; reopening resumes the living colonist at the **saved location**. The concrete supply budget proposed for approval is **one bandage and one ration per expedition across the slice**.

## Inputs

All paths are relative to `/Users/jeremyvalentine/SOLITUDE`. SHA-256 values identify exact file bytes.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/run-lifecycle/request.md` | `383da9b2532aaeed90e34a119fcc63b76c7f73ba530aa6e7282fa45ae8f4820b` |
| `.qrspi/run-lifecycle/queries.md` | `f7dad4b189d44811f28a64bff6ac84d367ee9813876a91684bc5258b39ccc6fb` |
| `.qrspi/run-lifecycle/research.md` | `ff73bede328b807e408c677bb719019b4f32d16b793f62bc521c696fa7a2a9d3` |
| `.qrspi/run-lifecycle/approach.md` | `241783ca0605f360192f72e86186087709b11724ffdad45e446346080d1e3541` |

Query and Research were independently approved at their recorded revisions. The approach decision records the human's selection of A and saved-location resume. Subsequent clarification does not change the source questions or Research findings; Shape has been reconciled with that clarification. This specification's concrete scope and behavior still require their own approval.

## Current behavior and required changes

Research establishes that explicit succession already resets the retained avatar and moves equipment into unsaved recovery ownership, but lethal health does not invoke succession. Scene initialization creates a new colonist even on a revisit. Production saves retain container contents and collected pickup IDs, while colonist identity/vitals/location, repairs, doors, and NPC state are not saved. Disposable demos intentionally reset on reload.

After this change, the production session owns lifecycle facts beyond any scene. Lethal health initiates death automatically. Loading a scene presents existing state; it never implicitly creates a colonist or expedition. Death retires the current colonist, loses their carried equipment, and refreshes only explicitly designated expedition content. Repair and shortcut facts survive both death and process restart. Existing transaction, interaction, inventory, vitals, and save-recovery behavior remains reusable.

Evidence anchors: Research answers on lethal vitals, scene transitions, save mechanisms, and ship compatibility; `Features/Player/Succession/SurvivorSuccessionComposition.cs`, `Application/Survivors/SurvivorSuccessionCoordinator.cs`, `Domain/ContainerSaveSession.cs`, `Domain/SaveGameData.cs`, and `Core/Composition/GameCompositionRoot.cs` under `Assets/_Project`.

## State ownership and lifetime

The authoritative production session owns the following logical state. Scene components own presentation and transient activity, and bind to the session's facts. These are behavioral ownership boundaries, not a requirement for new generic managers or registries.

| State | Death/new expedition | Scene revisit and reopening |
| --- | --- | --- |
| Completed repairs, installed critical components, permanent power/unlocks, discoveries | Retained | Retained |
| Ordinary shared locker contents and permanent pickup collection | Retained | Retained |
| Current expedition identity and designated encounter defeat/supply collection facts | Replaced/refreshed once | Retained |
| Current colonist identity, vitals, vital recovery progress, inventory/hotbar | Departing colonist retired; fresh identity, authored vitals, empty carried owners | Same living colonist and values restored |
| Current scene and player position | Cryobay awakening location | Saved scene/location on reopening; valid arrival position on ordinary travel |
| AI pursuit targets, movement velocity, open screens, active gestures, feedback effects | Cleared | Rebound safely; not saved as general entity snapshots |

Inventory and hotbar may retain their existing command-owner IDs and reader contracts, but their contents must belong to the current colonist. A shared locker is ship-owned, even when its contents were deposited by a previous colonist. There is no persistent corpse owner in production. Identity and records must remain stable across unload/reload; duplicate or missing required identities fail authoring validation instead of silently sharing or inventing world facts.

## Character death and expedition transitions

A new save creates the initial living colonist and first expedition in the cryobay, with authored healthy vitals and no carried items. Health reaching zero or below initiates succession without a debug action. Oxygen/hunger behavior continues to use existing vitals rules; no parallel death threshold is introduced.

Death stops gameplay control, closes active player interactions/screens, clears held input, retires the departing identity, loses all inventory/hotbar items, and awakens exactly one different colonist in the cryobay. Authored vital values and recovery timing are reset; hotbar selection and velocity are cleared. No recovery cache is published in production. Shared lockers are not cleared.

One accepted death advances expedition identity once. Loaded hostile encounters return to their authored initial state, designated supply sources receive the new expedition allowance, and unloaded content presents that same allowance when later bound. Completed ship changes remain effective. Death outside the cryobay scene must still awaken in the production cryobay without making scene loading a second lifecycle authority.

Requests from a retired colonist, duplicate lethal notifications, repeated bindings, or retries cannot create another successor or another supply allowance. Death and scene travel cannot commit concurrently. A zero-health colonist cannot regain playable control merely because transition preparation or saving failed. Failed transition work is recoverable through the existing error/recovery surfaces; it must not require another lethal notification to finish.

Death and world refresh are distinct operations: death replaces a character; a refresh changes expedition content. Only death requests a new expedition in this slice. There is no completion trigger, success screen, return-to-base completion rule, or public refresh action players can use to farm supplies. New Save retains its separate meaning of resetting the whole saved world.

## Enemies and minimal playable scope

The production slice must contain a reachable hostile encounter whose defeat/disable state is observable and can be changed through normal gameplay. Research did not find a defeat-capable enemy, so this scope includes the minimum missing encounter behavior rather than claiming reset bookkeeping delivers a playable defeat scenario.

For this slice, one authored hostile actor may be **disabled through the existing interaction system**; a weapon/health/combat expansion is not required. Disabling it stops hostile activity and records its stable encounter identity as defeated for the current expedition. A new expedition restores it; travel, reload, and reopening do not. Live AI may return to its authored patrol state when rebound; defeated actors cannot reappear or attack. No partial enemy-health or general AI serialization is required for the interaction-based encounter.

The minimum production route connects the cryobay, a maintenance area containing the encounter/supplies/component access, and the scrubber with a permanent shortcut. These may be contiguous areas in one production scene; this is not a mandate to build three scenes. Existing content may be reused, with only the authoring needed to exercise the lifecycle scenarios. Disposable demos must not be silently repurposed as production save sessions.

Permanent reclaimed-area clearing is deferred. Ship repairs do not implicitly reclaim territory or suppress hostile refresh. The defeat fact remains separate from permanent ship facts so later reclamation can have its own rules without changing this slice's behavior.

## Supplies, permanent loot, and critical components

The slice exposes one bandage and one ration in designated world sources per expedition, including the first. They use existing medical/food consumable contracts. These are world supplies, never awakening grants. Failed collection because inventory is full does not consume the allowance. Collecting, consuming, revisiting, reopening, or retrying a transition does not generate more supplies in that expedition. Remaining old world allowance is replaced rather than stacked at refresh. Items already stored in shared lockers remain legitimate saved inventory; the per-expedition world budget is not a global item-count cap.

All other pickups/containers preserve their existing saved behavior unless explicitly authored as critical sources. Ordinary lockers do not reroll at death. Unique items and permanent discoveries never become renewable merely because expedition identity changed.

Each required critical component has an authored source associated with the still-unfinished objective that needs it. On death, carried components are lost with other equipment. If an unfinished objective has no remaining component in an accessible, persistent shared owner, its authored source becomes obtainable for the successor despite a prior collected-pickup fact. If a usable component remains in a reachable locker, do not create a replacement. Once installed/consumed for permanent progress, that objective's source never replenishes. Presence checking includes all relevant persistent owners, not just loaded scene objects, and protected shared storage cannot be placed behind the lock it is needed to open.

This is a bounded policy for authored slice objectives, not a universal item-spawning service. It does not allow repeated collection within an expedition. Dropping/destroying the last critical component through unsupported paths must be prevented or leave the next-death reacquisition guarantee intact. Corpse retrieval and salvaging ordinary equipment remain deferred.

Repair consumption and its permanent result must appear in the same committed snapshot. Example: installing the fuse removes one fuse and records installation; restarting advances scrubber state; after the existing recovery period it is operational. Installed, running, recovery progress, and operational presentation survive death/reload. Unloaded or closed-game time does not advance simulation. A retry cannot consume a second fuse. Permanent power permission and an opened/unlocked shortcut restore before movement or interaction is enabled; restored doors must have correct collision immediately, without replaying an unlocking transaction.

## Persistence, resume, and recovery

Extend the existing save contract to retain ship facts, current expedition facts, current colonist state, saved scene/location, and the information needed to recognize/recover a committed death transition. No second save file or independent persistence framework owns these facts. Save validation, capture/cloning, durable writes, backup recovery, and authoring validation must include the new data.

An orderly quit/pause captures the latest accepted state, including player location/vitals. Reopening restores the same living identity, equipment, vitals/recovery progress, location, expedition, defeated encounters, loot facts, repairs and shortcuts. It grants no offline recovery and no refreshed supplies/enemies. In the exceptional case that saved coordinates are invalid for the authored scene, use a validated safe point in that same scene without replacing the colonist or expedition; report incompatible/missing required content through existing recovery behavior rather than erasing progress.

Gameplay resumes only after restored state is applied. Accepted repair, pickup and death snapshots must be internally coherent. A committed death can recover only forward to its successor and new expedition. Failure/retry cannot mix the old colonist's gear with the successor, replay a grant, lose a stored component, or leave a consumed fuse paired with an unrepaired scrubber. A durable-write failure keeps the affected transition gated and recoverable; it must not acknowledge success with an unsaved partial transition.

An abrupt process termination may resume the last complete durable snapshot, using the existing primary/backup recovery semantics. This is not a promise of continuous crash-proof position journaling. A snapshot containing lethal health must complete death before enabling gameplay. No previously acknowledged durable death revives its predecessor.

Version-2 saves preserve known equipment, locker records, seed and collection facts during migration. Missing lifecycle facts initialize one living colonist/expedition in the cryobay; the old schema has no location/vitals to resume. Unknown historical repairs cannot be inferred. For unfinished critical objectives, migration must reconcile collected sources whose components are absent so older saves do not inherit a softlock. Unsupported/corrupt data follows existing recovery rules and is not silently treated as New Save. Downgrading to older executable/save schemas is not supported by this feature.

## Integration and compatibility contracts

- Existing lethal-vitals events feed the current succession entry contract with the expected current identity; success/busy/stale/failure behavior remains explicit and retry-safe. Production equipment disposition changes from recovery handoff to permanent loss without duplicating succession coordination.
- Scene binding/restoration obtains existing current state. Fresh awakening, saved resume, and scene arrival are distinguishable; vitals are not reset on every bind. Existing retained-avatar readers remain valid during same-scene succession.
- Container commands remain the authority for mutations and capacity/transaction rules. Repair, critical-source reconciliation, world collection and saved facts must agree at command boundaries.
- Repair/scrubber and door presentation accept restored facts without consuming items, generating loot, or replaying callbacks that perform gameplay mutations.
- Existing save recovery remains the single durable save path. Existing demo recovery-cache behavior and disposable resets remain isolated and covered by their tests; production uses this specification's equipment-loss policy.
- Domain/Application code remains independent of Unity scene objects. Existing ownership, asset GUIDs, assembly boundaries, inventory interaction contracts, and unrelated NPC/art work are preserved.

These contracts describe required observable results; concrete class names, method signatures, serialization layout, scene construction tasks and work sequencing belong in the implementation plan.

## Acceptance criteria and validation

All IDs are new in this initial revision; none are retired. The listed checks are required future evidence, not executed results.

| ID | Testable outcome | Required validation |
| --- | --- | --- |
| S1 | New Save awakens one colonist/expedition in cryobay with authored fresh vitals and empty inventory/hotbar. | Application/EditMode initialization and production PlayMode. |
| S2 | Lethal damage automatically retires A and awakens B in cryobay, fresh vitals/empty gear/selection, no production cache; shared locker contents survive. | PlayMode through actual vitals mutation; manual ordinary gameplay damage and death. |
| S3 | Duplicate/stale notifications, preparation failure and retries yield one successor and one expedition advance; dead control stays gated until recovered. | Application transition/fault tests and PlayMode held-input/open-screen cases. |
| S4 | The authored hostile actor can be disabled through normal gameplay, stays defeated in this expedition, and returns once after death. | PlayMode encounter tests and manual encounter journey. |
| S5 | Scene unload/reload and reopening preserve defeated/collected facts, colonist identity/vitals/equipment, and expedition identity without refreshing content. | Production-session reload tests and standalone restart smoke. |
| S6 | One bandage and one ration are available per expedition; full-capacity failure, repeated binding and transition retry cannot duplicate grants; stored supplies survive. | Domain/Application allowance tests and PlayMode collection/consumption cases. |
| S7 | Ordinary lockers, unique pickups and permanent discoveries retain collection/content facts across death, reload and restart. | Container/pickup persistence regressions and production PlayMode/restart. |
| S8 | A carried critical fuse lost at death becomes obtainable again for its unfinished objective; a usable saved locker fuse prevents duplication; completed objectives never regenerate their component. | Application source-reconciliation tests, unloaded-owner tests, PlayMode death-before-install journey. |
| S9 | Component removal and permanent installation commit together; death/reload/restart preserve scrubber installation, running/recovery/operational state without another consumption or offline advancement. | Domain/Application repair/save tests, PlayMode scrubber, standalone restart. |
| S10 | Permanent power and shortcut unlock/open state survive death/reload/restart; restored collision and interaction permit traversal immediately. | PlayMode and manual traversals before/after death and reopen. |
| S11 | Orderly reopening resumes the same living colonist at saved scene/location with saved vitals/equipment and no offline recovery; invalid-location fallback does not refresh the expedition. | Standalone process restart and PlayMode invalid-location case. |
| S12 | Faults before/after transition commit or durable write recover a complete snapshot; an acknowledged death never restores A, repeats supplies or loses permanent repair facts. | Application persistence faults and isolated built-player interruption cases. |
| S13 | Version-2 migration retains known container/collection facts and reconciles missing critical components; corrupt/unsupported saves use existing recovery rather than silent reset. | Codec/session/recovery fixtures and isolated standalone save scenarios. |
| S14 | Existing command/readers, inventory/interactions, succession demos, disposable resets and save recovery remain reusable without redundant lifecycle authority. | Existing relevant Domain/Application, EditMode and PlayMode regressions; scoped code review. |
| S15 | A playable production route connects cryobay, maintenance encounter/supplies, scrubber and permanent shortcut; the complete A→death→B journey is possible without debug lifecycle calls. | Authoring validation and manual production journey. |
| S16 | Ordinary scene travel and quit/reopen do not count as expedition completion; only death refreshes this slice, while New Save still clears all progress. | Application/PlayMode transition-cause and new-save tests. |

Manual journey: start empty, collect supplies/component, disable the encounter, unlock the shortcut and repair/restart the scrubber; revisit/reload and confirm no encounter/loot reset; take lethal damage and confirm a fresh colonist, refreshed designated encounter/supplies, retained repair/shortcut/locker state; repeat with the critical component still carried at death; quit/reopen while alive and verify the saved location and unchanged expedition. Check actual HUD, interactions, blockers and enemy activity rather than only saved values.

The plan must map its work and evidence to these IDs. Source changes affecting Wakeup, inventory, hotbar, lockers or save recovery require the complete `Tools/Verification/verify.py` gate in an isolated workspace with disposable saves, plus the feature-specific checks above. `results.json` must pass; a skipped-build/partial rerun is not full qualification. Current gate qualification is macOS ARM64/Wakeup; other targets are not claimed. No tests, builds, fault runs, or manual gameplay checks have been executed for this specification.

## Explicitly deferred

Corpse retrieval/persistent recovery equipment; reclamation/territory control; dynamic repopulation; procedural generation; generalized reset/entity-snapshot frameworks; expedition completion/extraction and its UI/rewards; character selection/rosters; weapon or general enemy-combat expansion; saving live AI animation/path state; offline simulation; broader ship content/art redesign; cloud saves, platform expansion, and backward executable downgrade support.

## Revision summary

Initial behavioral contract: S1–S16 allocated. Concrete choices proposed for this approval include an interaction-disabled hostile encounter, a minimum connected production route, and one bandage plus one ration per expedition. No plan files or implementation changes are part of this revision.
