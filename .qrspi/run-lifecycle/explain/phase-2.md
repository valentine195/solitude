# QRSPI Explain: run-lifecycle — phase-2

## What this change does

Phase 2 makes repairs, scrubber recovery, door power and opened shortcuts belong to the saved ship, so returning to a scene can present existing progress without spending another component. It also adds an objective-bound policy for deciding when a lost critical component may become obtainable again, preparing the boundary that phase 3 will use during death.

## Background

The existing production save already has one session containing inventory, locker records, collection IDs and the lifecycle snapshot. The ship schema contains repair, door and critical-source records; this change connects those records to loaded gameplay objects rather than introducing another save file or a general entity serializer.

There are two distinct lifetimes. A Unity component is the loaded view or simulation of an authored object. Its `ContainerSaveSession` binding refers to a stable `SaveableId`; disposing that binding releases the loaded connection while retaining the saved record. A production scene receives session bindings, while optional null bindings preserve disposable scene behavior. New Save invalidates the old repair/door bindings and clears retained critical-source mappings.

This walkthrough covers all three phase-2 steps, from `62b17b02ae1a067bea59d8b623b2854cad6d8675` to `0836bb852d55b9770deda335073ceaea15dc7d75`, excluding `.qrspi/`. The product diff SHA-256 is `2002f2a337fd38886f3a47536b25a5cb6f0bf86fd6194ca27e3e33e13df7055a`; HEAD, clean working tree, spec digest and normalized plan digests matched the supplied scope before and after reading. This is a source walkthrough, with no execution or verification-result claim.

## Walkthrough

### A repair commits its item cost and ship fact together

`ContainerSaveSession.BindRepair(id)` creates or retrieves the ship record and permits one live binding for that identity. `RestorableSystemState.BindPersistence` reads its installation flag; `ScrubberEncounterState` additionally reads running status and accumulated recovery seconds. Hydration assigns saved state directly. It does not call the consuming repair action.

The significant path is `RestorableSystemState.TryRestore`. After confirming the current player inventory and command service belong together, it prepares a validated replacement ship snapshot before removing one item:

```csharp
var persist = persistence?.Prepare(true, false, 0);
var result = commands.Remove(slot.Address, 1,
    () => { persist?.Invoke(); IsRestored = true; });
```

The existing container command commits slot changes, invokes this trusted world-state callback, then publishes container notifications. Thus an observer sees the installed flag alongside the reduced item quantity. Session capture is refused while the command is busy, and the existing save host defers flushing at that boundary. The resulting coherent snapshot goes through the existing durable save path; this callback itself is an in-memory commit, not a new disk writer. Repeat installation returns `AlreadyRestored` before another removal.

`RepairBinding.Prepare` also suppresses any critical-source record for the installed objective in the same ship snapshot. This connects permanent completion and component availability without relying solely on a later GameObject visibility update.

### Scrubber recovery restores the simulation, then its presentation

A scrubber uses that same repair binding for fuse installation. Restart separately records `installed=true`, `running=true` and the current recovery value. `Advance` adds active simulation time up to five seconds; operational status is derived from running plus that threshold. No wall-clock timestamp is used to credit closed-game time.

Partial recovery updates the session record but does not independently mark a clean session dirty every frame. It joins the next accepted checkpoint, while reaching the operational threshold marks the session dirty. Rebinding publishes the restored installation, running and recovery facts together, so existing screen and task readers can derive “replace fuse,” “restart,” “recovering” or “complete” from the restored state.

Ownership stays with existing composition. `SceneBindings` binds generic `RestorableSystem` instances before junction initialization and player input binding. `ScrubberComposition` passes the lifecycle session to each `ScrubberEquipment`, retaining ownership of scrubber initialization and release. Feature bindings, doors and generic repairs are released before inventory ownership is released. Without a production persistence session, these states continue to operate locally.

### A shortcut restores its destination without replaying opening

Door records distinguish `powered`, `unlocked` and `open`. `RestorationDoorGate` observes its repair and records permanent power when restoration becomes effective. `OpeningPowerSocket.Powered` now reads the door's permanent power instead of maintaining a separate socket flag. Its initial interaction still checks for possession of a battery; it does not consume one. A restored powered socket reports success without requiring that battery again.

`SolitudeSlidingDoor.BeginOpening` accepts the persistent destination before starting the animation:

```csharp
persistence?.Store(hasPermanentPower, true, true);
powered = hasPermanentPower;
unlocked = true;
opening = true;
```

During ordinary play the blocker remains until the opening animation completes. If a saved scene is rebound during that animation, the accepted destination is restored immediately: `BindPersistence` selects the open frame and sets `blocker.enabled = !open`. It restores traversal state directly rather than replaying a battery check, item transaction or opening animation. A repaired but never-opened door may retain power while remaining closed.

`SceneBindings` checks production doors for missing or duplicate IDs and checks that repair gates and socket targets refer to the same scene. The editor duplicate-ID validator adds corresponding door/socket authoring checks. The existing `OpeningReviewSession` explicitly excludes its doors from production persistence, keeping the art-review setup disposable.

### Critical components use an authored recovery policy

`CriticalComponentSource` annotates a normal `WorldPickup` with its repair objective, single component item and explicit shared-owner IDs. It requires a same-scene repair or scrubber objective and accounts for every authored locker in that scene. `CriticalComponentSourceState` excludes inventory/hotbar from shared storage and rejects prerequisite lists: this slice supports unconditional access to protected sources and storage. `RunLifecycleValidator` checks source/objective uniqueness and pickup identity collisions in open scenes.

Binding a source registers the mapping with the save session. The live registration lease can be released during scene teardown, while the session retains the mapping for later reconciliation. A missing saved policy record is the legacy-initialization boundary: the source is reconciled once using both carried equipment and shared storage. Other collection IDs remain intact.

The decision is small and objective-specific:

```csharp
record.available = !IsCompleted(snapshot)
    && !HasRetainedComponent(snapshot, includeEquipment);
```

An installed objective suppresses its source. Otherwise, a surviving usable component suppresses a replacement; with neither completion nor retained component, the source becomes available and only that source's collection ID is removed. Snapshot capture overlays live container contents onto retained saved owner records, allowing reconciliation to consider unloaded lockers too. Encountered retained components in undeclared shared storage cause an error rather than an inferred replacement policy.

Collection still uses the existing inventory grant. Only a successful grant commits `available=false` and the collection ID, before observers run. Full inventory leaves the source available. `WorldPickup` refreshes visibility from session facts and subscribes to objective changes, so installing an objective can hide its source immediately. Losing a component or revisiting a scene does not itself replenish a source whose policy record already exists.

The death-facing operation is deliberately preparatory. `ReconcileCriticalSourcesForDeath` clones and validates a supplied post-loss snapshot, rejects remaining inventory/hotbar contents, reconciles the session's known authored mappings, and returns the candidate without publishing it. Phase 3 must supply and commit that candidate as part of succession. Automatic death, expedition renewal and the authored playable production route are later phases; this diff adds no shipping scene changes. The new PlayMode source constructs runtime fixtures in Wakeup to exercise these production bindings, while the Domain test source describes command publication, unloaded storage and migration scenarios.

## Where to focus

The repair publication boundary is the first useful reading point: follow `RepairBinding.Prepare` through `ContainerCommandService.Commit`, then the save host's busy guard. This explains how item consumption, permanent installation and critical-source suppression become one observable snapshot, and distinguishes that boundary from durable writing.

The other concentrated area is critical-source lifetime and authoring coverage. Follow which mappings have been registered in the session, which saved owners they declare, and where the later death transaction will call and commit reconciliation. Pair that with the door's accepted-open destination: saved facts intentionally outlive both loaded scene objects and unfinished visual animation.
