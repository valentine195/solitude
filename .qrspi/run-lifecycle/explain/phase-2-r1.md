# QRSPI Explain: run-lifecycle — phase-2-r1

## What this change does

Phase 2 makes repairs, scrubber recovery, door power, and opened shortcuts belong to the saved ship rather than a loaded scene. It also gives critical component sources a saved, objective-specific replacement policy, preparing the later death transition to restore a lost fuse without duplicating one retained in shared storage (S7–S10, S13, S14).

## Background

The existing `ContainerSaveSession` already captures containers and collected pickups into one save snapshot. Phase 1 added colonist, expedition, and ship records; this change connects the ship records to live components. A scene component now holds a temporary binding to a stable authored ID, while the session retains its facts after that component unloads.

Two boundaries matter. A container command commits inventory changes before notifying observers; an existing trusted callback can commit associated world facts at that same point. Separately, the existing save service captures complete session snapshots and avoids capturing during command publication. A command commit therefore establishes coherent in-memory facts; the existing save path supplies the subsequent durable write.

This explanation covers all four Phase 2 steps at HEAD `d07ace57916e75533e915e097224e0e041e05ba5`, from base `62b17b02ae1a067bea59d8b623b2854cad6d8675`, using exactly `git diff 62b17b02ae1a067bea59d8b623b2854cad6d8675 d07ace57916e75533e915e097224e0e041e05ba5 -- . ':!.qrspi'`. Product diff SHA-256: `30f1e059d59548b8d0e335e4b308c792e2dd83640c1482bfce7e62cc19ab237a`. The working tree was clean before reading and before publication, and the supplied spec and normalized overview/Phase 2 hashes matched at both checks. Only status tokens were normalized. No tests, builds, gameplay checks, or verification reports were run or consulted for this walkthrough.

## Walkthrough

**Repair facts commit alongside consumption.** `ContainerSaveSession` adds `RepairBinding` and `DoorBinding`, each rejecting missing or concurrently bound identities and keeping its saved record after release. A repair binding prepares a validated replacement ship snapshot before item removal. In `RestorableSystemState`, the command then uses the small commit callback:

```csharp
commands.Remove(slot.Address, 1, () => { persist?.Invoke(); IsRestored = true; });
```

By the time inventory observers run, both the item removal and installation fact have changed. Session capture remains blocked during publication. Installing also suppresses any critical source attached to that objective. Rebinding reads `installed` directly rather than attempting the consuming action again. Components without a production binding retain their existing scene-local state behavior.

**The scrubber saves simulation progress, not elapsed wall time.** `ScrubberEncounterState` uses the same repair binding for fuse installation, running state, and recovery seconds. Hydration applies these together before publishing updated feedback. Restart records running state; active simulation advances recovery toward five seconds. Partial progress updates the session record without dirtying an otherwise clean session every tick, so it joins the next accepted checkpoint. Completion marks the session dirty. Nothing adds closed-game time during restore.

`ScrubberComposition` remains responsible for equipment binding, followed by screens and task sources. `SceneBindings` separately binds generic repairs before restoration junctions and doors, and releases feature, door, and generic repair bindings before releasing inventory owners. This keeps existing composition ownership while making loaded objects views of saved facts.

**A door restores its destination immediately.** `SolitudeSlidingDoor` now owns saved `powered`, `unlocked`, and `open` facts. The power socket reads that owning door instead of maintaining a separate powered flag; its existing initial interaction checks battery possession without consuming the battery. A restored door needs no new battery check.

When opening begins, `persistence?.Store(hasPermanentPower, true, true)` records the accepted destination before animation finishes. During that animation the live blocker remains until completion; reloading restores the open destination immediately. `BindPersistence` applies the saved final frame and collider state. `Awake` calls the same presentation method, preserving facts already restored onto an initially inactive door rather than resetting its first frame. Repair gates observe restoration and record permanent power. Production binding validates door identities and same-scene references; the opening art-review session retains disposable door behavior.

**Critical loot has a bounded policy.** `CriticalComponentSource` associates one pickup, one repair objective, one item, and explicit shared-owner IDs. Authoring checks require one component per pickup, a same-scene repair target, and coverage of scene lockers. This slice rejects source or protected-storage repair prerequisites, keeping those resources accessible before the objective.

`CriticalComponentSourceState.Reconcile` asks whether the objective is installed and whether a usable component remains in the supplied snapshot. Snapshot capture includes live owners and unloaded saved owners. At legacy initialization it may count carried equipment; at a death boundary departing equipment must already be empty. The policy changes only its own availability and collection ID. Ordinary pickup and locker rules remain on the existing path.

Collection grants the item and suppresses the source through the same command callback. Capacity rejection changes neither fact. Rebinding an established source does not renew it just because the item is gone. An installed objective suppresses its source permanently.

**The policy itself survives reopening.** `CriticalSourceSaveRecord` now stores `itemId` and `sharedOwnerIds`, deep-clones the owner list, and validates its metadata. Session construction reconstructs policies from saved records before their source scene has loaded. Later authoring must agree with that saved mapping.

Older records with both metadata fields absent remain recognizable. Binding supplies their metadata without refreshing their existing availability. An unresolved unfinished source rejects death reconciliation instead of silently omitting it; a completed objective can remain suppressed without its missing policy.

`ReconcileCriticalSourcesForDeath` returns a cloned candidate and leaves the current session untouched. Phase 3 is responsible for committing that candidate with succession; this diff does not yet deliver automatic death or the playable production route.

**Tests describe the intended boundaries.** Domain additions exercise coherent repair observations, partial scrubber checkpoints, full-inventory collection, retained unloaded storage, migration, and reconciliation before source-scene binding. PlayMode additions create runtime fixtures in Wakeup, reload scenes, rebuild the composition root from disk, inspect scrubber feedback, and inspect door collision. A separate inactive-door case checks restoration before `Awake`. These are authored checks inspected here, not executed results; rebuilding the root in PlayMode is distinct from a standalone process restart.

## Where to focus

The first useful reading path is `RepairBinding.Prepare` → `RestorableSystemState.TryRestore` → the command commit callback: it explains how consumption and permanent installation become observable together, and where the later durable save fits.

The second is saved critical-source metadata → session reconstruction → candidate-only death reconciliation, alongside the inactive-door initialization path. These are the subtle restart and initialization-order cases in the updated snapshot. The authoring validators enforce explicit IDs and declared relationships; they do not demonstrate physical route accessibility or the later complete death journey.
