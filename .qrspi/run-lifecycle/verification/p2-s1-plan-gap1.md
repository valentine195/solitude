# Run lifecycle — Phase 2 Step 1 — Plan gap 1

Status: blocked before source implementation; S9/S14 not implemented or verified for this step. No source changes, staging or commit.

## Approved inputs

Spec SHA-256: a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922.
Current normalized Phase 2 SHA-256: a6e0136ffe47cf22f1974768bbf2d7b790e24a5547f58d7c3a804e7665138235.
Phase 1 PASS review: reviews/phase-1.md, SHA-256 3007bf24af613a8c52e5a65c96e304e25e81823d2b6b29d2ce0036b39de461c3.
User authorized Phase 2 Step 1 in single-step mode. Exact approved inputs/provenance rechecked unchanged.

## Safe execution scope

Main root: /Users/jeremyvalentine/SOLITUDE.
The helper start in main returned exit 1 with dirty-tree: working tree is not clean. The main checkout has 1906 unrelated dirty paths, including room/art work and original NPC work, and an empty index. No cleanup/staging was attempted. A byte-hash baseline is retained in p2-s1-main-baseline.json.
A managed isolated checkout of reviewed HEAD 62b17b02ae1a067bea59d8b623b2854cad6d8675 was prepared at /Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE. Exact standard QRSPI artifacts were copied without helper state. Helper start with that explicit project root succeeded, exit 0 / phaseId 2, on a clean product tree. Source implementation remains unstarted because of the gap below.

## Source evidence

- Core/Composition/SceneBindings.cs:140–143 owns inventory registration and initializes restoration junctions with commands/inventory; it has authoritative session and production/disposable lifetime information.
- Core/Composition/SceneBindings.cs:309–334 releases feature/junction bindings before inventory/container owners.
- Features/World/Restoration/RestorableSystem.cs:6–12 is a scene component exposing domain state, with no existing session binding.
- Features/World/Restoration/RestorationJunction.cs:17–18 accepts only commands and inventory. It does not supply a session to the generic system.
- Features/World/ScrubberEncounter/ScrubberComposition.cs:66–67 already owns scrubber equipment binding and receives SceneFeatureContext; that planned file can pass its existing production session without moving ownership.

Step 1 requires stable saved generic repairs and scrubber facts, but omits SceneBindings from its file scope. Hiding generic repair binding in scrubber composition would make unrelated capabilities depend on one another. Adding a second discovery/lifecycle owner would conflict with the explicit ownership/minimal-change constraints. The existing binding owner needs one narrow edit.

## Proposed adjustment — awaiting approval

Add Core/Composition/SceneBindings.cs to Phase 2 Step 1. Bind/release generic RestorableSystem persistence through that existing owner before interaction/control and before inventory ownership is released. Continue using ScrubberComposition for scrubber equipment, preserve disposable demo behavior, and keep the planned atomic consumption/state and persistence tests. No new lifecycle manager, scene rewrite or feature scope is proposed.

Full proposed phase artifact: verification/p2-s1-phase-proposal.md.
Proposed normalized Phase 2 SHA-256: 5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8.
Normalization: only status-marker tokens to [ ]; all other bytes significant. No spec, overview, other step, phase dependency, or other phase content changes.

## Validation and preservation

This is bounded source inspection, not behavioral verification. No gameplay tests/builds were run. Both checkouts remain on reviewed HEAD, main dirty baseline is byte-identical, and the isolated product working tree/index are clean. No code changes are pending.
