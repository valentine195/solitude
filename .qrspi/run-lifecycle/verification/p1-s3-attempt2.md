# Run lifecycle — Phase 1 Step 3 — Attempt 2

Status: FAILED / not complete. No staging or commit. HEAD remains fe68a638ace5b8dbcd4739e17413f03f9808ad71. Single-step execution; later phases not started.

## Approved inputs

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Normalized Phase 1 SHA-256: `081c93eb075e2c68e3894a802c8c203eb75fa09b7dab9691c502d8b8bd64eea3`.
Criteria: S1/S5/S11/S14/S16. Human approved Step 3. Initial provenance and complete-plan approval checks passed before source changes.

## Changes under test

SaveGameService invokes a composition capture callback before dirty saves and explicit/pause/quit checkpoints, including a clean session; clean LateUpdate does not write every frame. Root captures active living scene/location/vitals/recovery; travel flushes before loading. Startup validates streamability and redirects to the saved production scene before player binding. Production New Save returns to Wakeup. A disposable second scene and three additional PlayMode tests cover checkpoint capture, saved-scene routing/New Save and unavailable-scene preservation. Four player process scenarios were added for clean-session orderly quit, exact resume, safe fallback and unavailable saved scene; none ran because PlayMode failed first.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s3 --output /private/tmp/solitude-run-lifecycle-p1-s3-attempt2` (with required filesystem access).
Expected: full gate passes. Actual: failed, PlayMode exit 8.

- Pure tests: 223 passed, 0 failed.
- Authoring validation passed.
- EditMode: 343 passed, 0 failed.
- PlayMode: 90 passed, 32 failed, 0 skipped/inconclusive.
- Builds and all player scenarios did not run. No manual gameplay validation or independent Review ran.
- Attempt 1 was blocked before tests by sandbox denial writing the existing Tools/Verification/obj .NET build cache. Reran unchanged source with required filesystem access; that attempt's verdict and log are retained separately.

## Failure findings

31 failures report setup Expected Ready / actual Failed. The existing IncompatibleResumeStaysGatedAndDoesNotEraseTheSavedSnapshot case itself passes but intentionally leaves survivor.scenePath = Assets/_Project/Scenes/AnotherProductionScene.unity in the shared disposable save. With Step 3's saved-scene validation, subsequent roots correctly reject this unavailable scene before tests can call New Save. Logs repeatedly report Saved production scene is unavailable. No production fallback to a fresh save is proposed.

The independent failure is PlayerVitalsSliceTests.ActualWakeupClockDelaysThenRecoversToBoundaryWithoutChangingOxygen, line 63: expected health 50, actual 49.9999962f. Its source and recovery rules are unchanged by Step 3. This run alone does not establish whether the small boundary miss is pre-existing timing/precision behavior or a regression. No baseline reproduction has run and no tolerance/runtime repair has been applied.

## Proposed repair and diagnosis — awaiting approval

In the incompatible-resume fixture, restore its original valid lifecycle snapshot in a finally cleanup after asserting the rejected snapshot remains intact and control remains gated. Keep rejection and save-preservation assertions. This is within Step 3's existing RunLifecycleResumeTests.cs scope. Then compare the unchanged health-recovery test against the pre-Step-3 committed source in a disposable workspace before selecting any precision-related repair. Do not weaken the boundary assertion or alter recovery rules without evidence and any required scope approval. Rerun the full gate after the authorized correction. No repair applied yet.

## Criterion-linked results and limits

S11: new CleanSessionCheckpointsCaptureLiveMovementVitalsAndRecovery passed for explicit Flush, application pause and application quit with no container dirt; blocked landing fallback test passed. Process-level resume/fallback remains unrun.
S1/S5: fresh binding/rebinding passed; startup routing test and production reload test were blocked in setup by the leaked invalid save.
S16: New Save return-to-cryobay/clear-progress assertions in the new startup test were blocked in setup; not qualified.
S14: domain/authoring/EditMode passed; full PlayMode failed and later checks were unrun, so regression qualification is incomplete.

## Snapshot and preservation

All six feature source hashes match the gate source snapshot; copied Assets files match the isolated project. Runner executes from the repository root and is not copied into the temporary project. All 371 pre-existing dirty paths remain byte-identical to the baseline, excluded from staging. Index is empty and HEAD unchanged. No imported scene/prefab files were edited in the main checkout.

Logs/XML/verdict: `/private/tmp/solitude-run-lifecycle-p1-s3-attempt2/`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `4eddb10ebf36f87074c48a238c2e81b67c78d2de6097b9ebe509c719ac3d18dc` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `cc5f161908ff2f656ab86c5ec9a61c7be4f84d48ad692a2263f969684bafb247` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `8056583bb096eedac632ca47598463e43080981c6eba38e101d2e5faea21fa5a` |
| `Tools/Verification/verify.py` | `128771b1bc386f1024bd74fc0e7250c1924bacc0022f76e429eb123b36cde552` |

## Failed cases

- `SOLITUDE.Tests.PlayerVitalsSliceTests.ActualWakeupClockDelaysThenRecoversToBoundaryWithoutChangingOxygen`: Expected: 50 /   But was:  49.9999962f
- `SOLITUDE.Tests.RunLifecycleResumeTests.OutsideAuthoredFloorUsesSameSceneSafePointWithoutChangingLifetime`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.RunLifecycleResumeTests.ProductionSceneReloadPreservesIdentityPreciseVitalsTimingPositionAndEquipment`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.RunLifecycleResumeTests.SceneArrivalKeepsColonistVitalsButUsesAuthoredArrivalPosition`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.RunLifecycleResumeTests.StartupSelectsSavedProductionSceneBeforeBindingAndNewSaveReturnsToCryobay`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.RunLifecycleResumeTests.UnavailableSavedSceneBlocksStartupPreservesBytesAndAllowsExplicitNewSave`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.RunLifecycleResumeTests.VitalRestorationPublishesCoherentlyAndRejectsReentrantOrIncompatibleChanges`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorRecoveryInteractionTests.FullRecoveryCapacityCanScrollAndTransferTheLastSlot`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorRecoveryInteractionTests.LeavingAndClosingCancelGesturesAndReturnInput`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorRecoveryInteractionTests.MissingPrefabConfigurationCreatesNoUsablePartialOwner`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorRecoveryInteractionTests.RecoveryCleanupPreservesOtherOwnersAndLockerLazyLoot`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorRecoveryInteractionTests.RecoveryUsesRealFocusAndTransferUIAndConservesPartialAndRepeatedTransfers`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.DeathClosesJournalAndInventoryAndSuppressesHeldMovementUntilRelease`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.PartialCachesEmptyDeathAndRejectedAttemptConserveEveryItem`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.RejectedPreparedHandoffReleasesOnlyItsEmptyOwnerAndCanTryAgain`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.ReleasedCacheDropsFocusSynchronouslyWithoutReleasingOtherOwners`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.RevisionChangeBetweenPreparationAndCommitRejectsTheWholeHandoff`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.TransferDragAndHeldPointerCannotActAfterCommittedFailureOrRetry`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionFailureTests.UncertainAcknowledgmentRetriesWithoutReactivationOrAnotherMessage`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionShipCompatibilityTests.AuthoredScrubberKeepsRunningAndReaderBindingsAcrossDeathButResetsOnReload`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionShipCompatibilityTests.RestorationTerminalDoorAndLaterRepairRemainLiveUntilSceneReload`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionShipCompatibilityTests.SamePositionDeathsRemainRecoverableThroughOrdinaryFocusAfterExhaustion`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionShipCompatibilityTests.WakeupPickupLockerAndEquippedSuccessionUseRetainedSceneAndUI`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.CommittedEquipmentSurvivesActivationFailureAndRetryDoesNotTransferAgain`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.EmptyDeathResetsRetainedAvatarVitalsSelectionReadersAndFeedback`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.EquippedDeathCommitsBothSourcesAndCancelsGestureWithoutAnOpenModal`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.HeldMovementAndInventoryAreNeutralUntilReleaseThenNewInputWorks`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.InvalidPointRejectsDeathWithoutActivatingSuccessor`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.MissingRecoveryRejectsWithoutLosingCarriedEquipment`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.OpenJournalClosesWithoutRebindingAuthoredDemo`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.PoweredDoorFirstContactAndPickupRemainLiveAcrossDeath`: SetUp :   Expected: Ready /   But was:  Failed
- `SOLITUDE.Tests.SurvivorSuccessionSliceTests.RepeatedEquippedDeathsKeepPartialCachesAndRecoverThroughInteraction`: SetUp :   Expected: Ready /   But was:  Failed
