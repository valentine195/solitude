# Phase 2 Step 1 — attempt 1 (blocked)

## Approved scope and source
- Criteria: S9, S14. Single-step mode.
- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Normalized Phase 2 plan SHA-256: `5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8` (status tokens normalized).
- Source root: `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`.
- Base HEAD: `62b17b02ae1a067bea59d8b623b2854cad6d8675`.
- Uncommitted 11-file feature snapshot: [source manifest](p2-s1-attempt1-source.json), SHA-256 `0b2c28f6ecf56838f9af72d390fc38de0a41c79c42621fc04590f7df2f73b829`.
- No source changes staged or committed. Primary checkout product files were not edited by this step; unrelated dirty content excluded from verification.

## Verification
Command, from source root:
```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s1-attempt1
```
Expected: complete gate passes, including new production repair/scrubber scenarios, regression suites, authoring, both builds and player scenarios.
Actual: **FAILED**, exit 1; PlayMode command exit 8. Pure tests 219 passed; authoring passed; EditMode 339 passed; PlayMode 115 passed, 1 failed, 0 skipped. Builds/player scenarios did not run.

- S9: generic production repair test passed, including command-publication save deferral, exactly-one consumption, scene rebind, disk restore via root recreation, independent IDs and New Save clearing. New domain persistence tests passed. Production scrubber scenario failed at fixture item grant before installation/recovery assertions, so S9 is incomplete.
- S14: existing executed suites passed. Full gate is incomplete and cannot qualify the change.

Failure: `RunLifecycleShipPersistenceTests.ProductionScrubberRetainsPartialRecoveryAndRehydratesScreenAndTaskFeedback`, source line 134, expected successful Grant of two ReplacementFuse items but got false.

## Cause and proposed repair (awaiting human approval)
Wakeup uses GameRuntimeConfig and the production Database catalog. The replacement fuse exists only in the scrubber demo database. `ScrubberEncounterAuthoringTests.FuseCatalogAndSceneInstanceHaveDistinctStableIdentities` explicitly asserts exclusion from the production catalog. The runtime persistence code correctly rejects an unknown item. The new runtime fixture authored scrubber endpoints but did not supply the matching catalog.

Repair only the already-scoped new PlayMode test file: create an in-memory clone of the production database with the existing authored fuse definition added, temporarily bind this clone to the loaded runtime config before production startup, preserve it through root recreation, and restore the original reference and destroy the clone in teardown. Do not save or modify database/config assets, shipping scenes or production catalog policy. Rerun the complete gate as a new attempt. No plan/spec scope change is proposed.

The step and phase are marked `[!]`. Per qrspi-implement Error Handling, no repair has been applied; waiting for the human decision.

## Retained evidence
[Gate result](p2-s1-attempt1/results.json), [EditMode XML](p2-s1-attempt1/editmode.xml), [PlayMode XML](p2-s1-attempt1/playmode.xml), [pure log](p2-s1-attempt1/pure-tests.runner.log), [PlayMode log](p2-s1-attempt1/playmode.log). Remaining raw logs stay at `/private/tmp/solitude-run-lifecycle-p2-s1-attempt1`.
