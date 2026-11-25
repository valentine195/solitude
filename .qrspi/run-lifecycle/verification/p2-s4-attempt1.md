# Phase 2 Step 4 — attempt 1 (blocked)

## Scope and exact inputs
S8/S10/S13/S14, review F1/F2. Human-approved repair overview SHA-256 `dc6e3dec5d02179bc7c8fa565ab91e422de6afbaf5d6ad6d8e4c633eff4444cb`, normalized Phase 2 `bc733c6ee01970b3a5a493d42292baf0f4b886ae3d4f1953f0546fd43b2ef5a5`; spec `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922` unchanged. Provenance checked before verification.
Source `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`, base `0836bb852d55b9770deda335073ceaea15dc7d75`; [seven-file source manifest](p2-s4-attempt1-source.json), SHA-256 `ce62de00b46ce7fcc92edd3a7bb4ae895b262d7f75a5c9d7e0334ae7e39a42a3`. No repair changes staged or committed.

## Verification
Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s4-attempt1`.
Expected complete gate pass. Actual **FAILED**, runner exit 1/PlayMode exit 8. Pure 229 passed; authoring passed; EditMode 349 passed; PlayMode 99 passed/26 failed/0 skipped. Builds and standalone scenarios unrun.

F1/S8 tests passed: fresh session without source binding restores saved policy, prepares lost-source availability without early publication, preserves unloaded storage/completed objective, handles legacy metadata explicitly and checks mismatched authoring/deep copies. Production disk-codec policy test passed. F2/S10 initially inactive door hydration/activation regression passed. S13/S14 full qualification incomplete due fixture save leakage.

## Failure and proposed repair (not applied)
First failing test: `ScrubberEncounterLifecycleTests.ProductionSessionRejectsScrubberWithoutChangingInventoryOrSave`, startup rejects unknown critical component item `4716e84c715564ad2ac79c4be74037dd`. Subsequent recovery/succession test setup fails because the same disposable save remains incompatible with the normal catalog.

RunLifecycleShipPersistenceTests temporarily adds the fuse to an in-memory catalog. Cleanup currently calls StartNewSave while its sceneLoaded Author hook is still attached. That hook recreates the test-only critical source and its now-persisted item policy in the supposedly cleared save. Cleanup then restores the real production catalog, which intentionally excludes the fuse. Constructor policy validation now correctly detects the leaked metadata. This is fixture isolation, not a reason to weaken production validation.

Propose moving `SceneManager.sceneLoaded -= Author` before the teardown StartNewSave, so the fresh scene/save has no test-only critical source, then restoring the original catalog as before. Change only the already-scoped PlayMode fixture; rerun the complete gate as attempt 2. No plan/spec revision required. Await human decision under qrspi-implement Error Handling; step/phase blocked.

[Results](p2-s4-attempt1/results.json), [PlayMode XML](p2-s4-attempt1/playmode.xml), [EditMode XML](p2-s4-attempt1/editmode.xml), [PlayMode log](p2-s4-attempt1/playmode.log), [pure log](p2-s4-attempt1/pure-tests.runner.log). Full raw output: `/private/tmp/solitude-run-lifecycle-p2-s4-attempt1`.
