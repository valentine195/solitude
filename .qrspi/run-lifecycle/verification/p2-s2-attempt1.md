# Phase 2 Step 2 — attempt 1 (blocked)

## Approved inputs and snapshot
Criteria S10/S14, single-step mode; human authorized Step 2. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; normalized Phase 2 plan SHA-256 `5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8`. Input provenance matched before verification.
Source root `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`; base `700f4e21982404aad7df283e27fcf80bb32be5c6`; [eight-file source manifest](p2-s2-attempt1-source.json), SHA-256 `0e8a410240156527b4fa7f1375fda600fc44e63b3ebb59ac12ea4bc652b57bb4`. No Step 2 changes staged or committed.

## Verification
Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s2-attempt1`.
Expected complete gate pass. Actual **FAILED**, runner exit 1/PlayMode exit 8. Pure 219 passed; authoring passed; EditMode 339 passed; PlayMode 117 passed/3 failed/0 skipped. Both builds and player scenarios unrun.
S10 new tests passed: repair power/permission restored, shortcut open/collision restored after animation interruption/rebind/disk restore, socket power restores without battery checks, identity/record validation and binding invalidation pass. S14 incomplete because existing compatibility and fixture lifecycle tests fail.

## Failures and proposed repair (not applied)
1. `InteractionScopeLifecycleTests.InactiveFeatureFeedbackReceiversBindAndSceneReleaseStopsContactFragments`: reflects `<Powered>k__BackingField` on OpeningPowerSocket, which now derives Powered from its door. Propose changing the existing fixture to set its door's powered field instead, preserving all feedback/disposal assertions. This requires adding that test file to the Step 2 scope.
2. `SurvivorSuccessionSliceTests.PoweredDoorFirstContactAndPickupRemainLiveAcrossDeath`: existing OpeningReviewSession art-reference scene has a memory-only save host but no RestorationDemoSession marker, so new door identity validation classifies it as persistent. Propose excluding that explicit existing art-review marker from persistent door binding/validation in SceneBindings and SaveableIdValidator, keeping production validation strict. No scene edits or new marker type needed.
3. `RunLifecycleShipPersistenceTests.ProductionScrubberRetainsPartialRecoveryAndRehydratesScreenAndTaskFeedback`: teardown Author callback tries to acquire pause from a root with an uninitialized pause field. FindFirstObjectByType may select the duplicate scene bootstrap pending destruction. Propose selecting enabled GameCompositionRoot instances in this fixture's Root helper, avoiding the disabled duplicate. Confirm with rerun; current trace alone does not fully prove the root-selection timing.

[Proposed complete Phase 2 file](p2-s2-phase-proposal.md), normalized SHA-256 `0195a7488b9931af5f0c06d0bbfa3ffe1a05918118ed6883094434792db6ba1f`. It adds only `Tests/PlayMode/InteractionScopeLifecycleTests.cs` and explicit compatibility verification wording. Overview/spec/other phase revisions unchanged. Await human approval to adopt the proposal, apply these scoped repairs, and rerun the full gate. Step/phase marked blocked per qrspi-implement Error Handling and Changing the Plan.

[Results](p2-s2-attempt1/results.json), [PlayMode XML](p2-s2-attempt1/playmode.xml), [EditMode XML](p2-s2-attempt1/editmode.xml), [PlayMode log](p2-s2-attempt1/playmode.log). Full raw output retained at `/private/tmp/solitude-run-lifecycle-p2-s2-attempt1`.
