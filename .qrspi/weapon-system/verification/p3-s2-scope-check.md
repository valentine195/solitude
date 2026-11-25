# Phase 3 Step 2 — inspection/scope gate

Status: blocked before implementation by plan sequencing. No product source changes, staging, test run or new commit. HEAD remains 69c1fc7e75f95508692636781c60490c197bb5b1. Initial inspection showed only the unrelated SOLITUDE_Start.unity edit; preserved.

Approved spec SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5, overview 8f785cf5bdde1bb321ef304b4cdfbe3519a8864b6e2219f1dff5bbda5b0fbf10 and Phase 3 14d4ab3db4be8fa3d802e755d1649118e4848f9a852471f7a0e96d3691e58011 still match after status normalization. Upstream provenance checked. S25/S26 activation and S29 required full-gate qualification are not yet implemented/verified.

## Concrete conflict

The approved Step 2 activates public version 4 and the starting-wrench grant while requiring the full gate. Baseline fixture changes that activation necessarily needs are assigned mainly to Step 3, or absent from Step 2's file list.

- Tests/PlayMode/WeaponSystemTestFixtures.cs:43 manually grants the wrench after a new production run; activation would create a duplicate while its existing tests assert one wrench in the selected slot.
- Verification/Player/SolitudeCompositionSmoke.cs:110–111 seeds inventory/hotbar supplies for process verification, and :137–138 asserts totals 3/2. The default wrench adds an item to the hotbar before these grants. The process hook is listed in Step 3, but its ordinary baseline expectations must change before Step 2's required full gate can qualify activation.
- Editor PickupPersistenceTests creates a default SaveGameData session in SetUp, then CollectionRoundTripPreservesInventoryAndFact collects into the player inventory and captures it. Once CurrentVersion becomes 4, such already-initialized fixtures must explicitly mark completion; a genuine pending snapshot with carried items must still reject. PerformanceBehavior/Optimization and other existing fixtures have the same setup pattern. These are fixture corrections to preserve existing behavior assertions, not reasons to weaken pending-save validation.

## Proposed bounded sequencing/scope correction

Include baseline fixture updates in Step 2 alongside activation, rather than waiting for Step 3. Restrict changes to explicit completion markers for already-played v4 fixtures, preserved genuine v1–v3 migration fixtures, removal of redundant manual starter grants in new production-run fixtures, and expected default item/selection/count updates caused by the new wrench. Preserve existing test assertions about collection, transfer, lifecycle, backup integrity and recovery; no deletions, skipped tests or relaxed corruption checks.

Add the affected existing fixtures to Step 2 scope as required: Tests/Editor/PickupPersistenceTests.cs, PerformanceBehaviorTests.cs, PerformanceOptimizationTests.cs, TaskJournalCompositionTests.cs, UnityInputCompositionTests.cs, RestorationJunctionTests.cs; Tests/Domain/RestorationTests.cs, ContainerBoundaryTests.cs; Tests/Application/ApplicationCompositionTests.cs, SaveRecoveryTests.cs; Tests/PlayMode/WeaponSystemTestFixtures.cs, ScrubberEncounterLifecycleTests.cs. Baseline expectation adjustments in the already-planned Step 3 lifecycle/succession fixtures and Verification/Player/SolitudeCompositionSmoke.cs, Editor/PhaseFiveFixtureBuilder.cs and Tools/Verification/verify.py may move forward only where required by activation. Existing Step 2 codec/candidate fixtures remain in scope as already approved.

Keep all new Step 3 restart/failure-injection scenarios and integration qualifications in Step 3; do not merge steps or expand gameplay. Starting loadout implementation, v4 activation, complete verification and one Step 2 commit remain the objective. No changes to approved plan prose/revision digests until the human approves this correction. Step and phase markers [!] record the scope gate only.

Concurrent workspace note: the final status additionally showed untracked .impeccable/ and PRODUCT.md. They were not created, read, staged or changed by this implementation step and remain preserved outside its scope.
