# Phase 3 Step 1 — composition scope gap (no source changes)

## Snapshot and provenance
HEAD `d07ace57916e75533e915e097224e0e041e05ba5`; managed worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`, clean product working tree. Phase 2 re-review `phase-2-r1.md` source hashes remain current. Spec `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; current approved normalized Phase 3 `0d390b4ec2b8e462d97ccfb5b170d3b18f8ea744b547dd49f73b82f167a4c4c7`. Upstream and approved plan checks passed using retained `p2-s4-attempt2-approved-plans.json` (the old temporary manifest is no longer present). Helper Phase 3 start succeeded. No implementation or tests have begun.

## Evidence and required change
Step 1 moves production transition authority to the persistent root/session and must recover a committed receipt before control. However, its Files list omits the current scene owner:
- `Features/Player/Succession/SurvivorSuccessionComposition.cs:69–70` rejects every saved pendingDeath receipt during Initialize.
- The same file at line 87 unconditionally constructs a scene-owned SurvivorSuccessionCoordinator, and Release clears it. Its RequestDeath delegates to that coordinator; demo preparation creates recovery ownership.
- `Core/Composition/GameCompositionRoot.cs` calls scene feature initialization during BindScene; current code has no production coordinator-binding hook before that initialization.

Editing only the original Step 1 files would leave the existing production path owning a separate coordinator or rejecting the durable receipt. This is part of Step 1's promised authority/lifetime behavior, not the lethal-health wiring deferred to Step 2.

## Proposed revision
[Complete Phase 3 proposal](p3-s1-phase-proposal.md), normalized SHA-256 `666492730a4a50dcecee990115837b0beabf4f19f40316034eeb947aaf758a90`. It adds only:
- `Features/Player/Succession/SurvivorSuccessionComposition.cs` to Step 1 for binding the persistent authority and participating in receipt recovery. Step 2 still owns automatic health/death hooks.
- `Tests/PlayMode/RunLifecycleShipPersistenceTests.cs` for production-root binding/durable-transition regression coverage using explicit transition requests.

The existing root can supply the production coordinator hook to its authored composition before scene initialization; no new composition owner, SceneFeatureContext field, assembly, scene asset, recovery container or generalized lifecycle framework is proposed. App/Domain/session transaction changes remain in the already-approved Step 1 files. Spec, overview, other phases and Step 2/3 behavior unchanged.

Awaiting human approval to adopt this exact proposal and execute Step 1. Existing Step 1/Phase 3 marked blocked under qrspi-implement Changing the Plan; no source edits made. After approval, run the full verification gate and pause at the single-step boundary.
