# Phase 2 Step 1 — attempt 4 (PASS)

## Scope and exact revisions
- Criteria S9, S14; single-step mode. Step 1 complete; Steps 2/3 not started.
- Commit: `700f4e21982404aad7df283e27fcf80bb32be5c6` in isolated managed worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`. Working tree clean. Primary checkout has not received this commit; unrelated user content preserved.
- Base: `62b17b02ae1a067bea59d8b623b2854cad6d8675`.
- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Normalized Phase 2 plan SHA-256: `5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8`.
- [All approved plan hashes](p2-s1-attempt4-approved-plans.json). Only status tokens normalized. Upstream provenance rechecked before execution and commit.
- [Tested 11-file source manifest](p2-s1-attempt4-source.json), SHA-256 `ffe1732bb9a946d57f9f168856ffe7a7a50930fb2806617a42155d41b7de8110`; every listed byte verified against committed source after the gate.

## Resulting behavior
Existing session owns stable-ID repair installation/running/recovery records. Repair consumption and installation commit before command notifications; publication-time snapshots/saves are deferred. Existing loaded repair/scrubber adapters restore these facts without consuming again. SceneBindings owns generic repair binding/release; ScrubberComposition retains scrubber ownership. New Save invalidates bindings. Disposable demos retain local state. Partial recovery joins saved checkpoints; no offline catch-up is introduced.

## Verification and criteria
Command from source root:
```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s1-attempt4
```
Expected: complete gate PASS, including production repair/scrubber lifecycle coverage and existing regressions.
Actual: **PASS**, exit 0. Pure Domain/Application 219 passed; Unity EditMode 339 passed; Unity PlayMode 116 passed; zero failures/skips. Authoring checks, verification native build, shipping native build, and all 32 standalone save/restart/recovery scenarios passed (macOS ARM64).

- **S9:** domain tests verify coherent removal/installation, observer snapshots, distinct/stale/foreign bindings, installation/partial/running/operational hydration and no reconsumption. Production-root tests verify one of two items consumed, publication-time save deferred, scene rebind, actual disk codec restoration via root recreation, exact 1.25-second partial recovery, operational rebind, NORMAL telemetry and completed task feedback. Explicit New Save clears repair state. Existing process scenarios qualify durable writes/recovery; they do not independently author a scrubber in the standalone player.
- **S14:** existing command/container, restoration, scrubber/demo, lifecycle and save-recovery suites pass. No second save host, death owner, reset framework, scene/prefab/catalog edits or new assemblies introduced.

## Approved fixture repairs and limits
Attempt 1 exposed a missing test-only fuse catalog; attempt 2 exposed a loaded-frame delta during fixture restart; attempt 3 exposed an intentionally paused interaction. Each stopped at its human gate. Human-approved fixture-only fixes use an in-memory catalog clone, a paused frame before root recreation, and release the deterministic pause before UI interaction. Exact recovery and item-count assertions retained. Earlier reports remain unchanged.
No manual gameplay qualification or complete death/next-expedition journey claimed: these belong to later phases. The gate runs the clean reviewed base plus this step, excluding primary checkout's unrelated uncommitted NPC/art content. Primary baseline comparison after verification: 0 changed baseline paths.

## Evidence and next gate
[Gate result](p2-s1-attempt4/results.json), [EditMode XML](p2-s1-attempt4/editmode.xml), [PlayMode XML](p2-s1-attempt4/playmode.xml), [pure test log](p2-s1-attempt4/pure-tests.runner.log), [PlayMode log](p2-s1-attempt4/playmode.log). Full raw logs/builds remain at `/private/tmp/solitude-run-lifecycle-p2-s1-attempt4`.

Paused after Step 1. Human may authorize Phase 2 Step 2 (permanent power/shortcuts), request an isolated checkpoint review, or stop. Phase 2 remains in progress. No integration into primary checkout, push, or publication performed.
