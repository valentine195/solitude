# Phase 1 Step 2 — attempt 2

Outcome: BLOCKED on two existing validation prerequisites. No files staged or committed.

## Authorized inputs and snapshot

Human authorized correcting the failed resize test setup and resuming Step 2. The correction now asserts rejected shrink preserves the binding, removes food from the truncated slot, asserts successful shrink, and verifies binding invalidation.
Spec SHA-256: `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`.
Normalized overview: `1021dc1d21303d304a853b0ef108a8ec8c5f9e5d1a83dc773e64740e05b943a2`.
Normalized Phase 1: `542b77fd786b2650e2d30df6fcd26b98ed4fbab4c1bded0d0070a73baa3b2a26`.
All upstream provenance and plan revisions checked on resume; plan revisions rechecked after verification. All 31 scoped files match `p1-s2-attempt2-source.json` and their copied isolated-project versions. The unrelated SOLITUDE_Start scene remains preserved outside this feature's commit scope. Review archives remain untouched/ignored.

## Criterion-linked verification

| Criteria | Command/check | Expected | Actual |
| --- | --- | --- | --- |
| S9/S15; existing pure regressions | Unity-bundled dotnet run --project Tools/Verification/PureTests.csproj with existing NUnit DLL | All pure cases pass | **257 passed, 0 failed**; `p1-s2-attempt2-pure.log`. |
| S2/S7/S8/S24 | Live Unity run_tests --mode editor --filter ItemActionAuthoring --async_tests true and test_status | Authored wrench values/tags, action reuse, legacy compatibility and invalid config checks pass | **19 passed, 0 failed/skipped/inconclusive**; `p1-s2-attempt2-editor-result.json`. |
| S7/S30 | Live Unity run_script AuthorWrench.Run through AssetDatabase | Wrench asset registered with stable GUID ID and correct initial config | Completed: ID 104950f738ebc4af4a05ec473310a59a, 6 tags, 1 Primary, .5500000119-second float-derived duration. First authoring call saved assets but immediate validation preceded ItemDefinition OnValidate identity generation; completed authoring explicitly initialized the missing itemId from its asset GUID through SerializedObject, following the existing identity contract. Both results preserved. |
| S2/S7/S30 | Live Unity tags + ItemCatalogValidator.AuditAllAssets + metadata validation | All assets validate | **Failed**: existing ReplacementFusePickup references a valid item omitted from the pre-existing database. HEAD database had only two entries and omitted the same item before Step 2. No unrelated repair made. |
| S30 | Read-only live catalog diagnosis followed by UnityMetaValidator.ValidateForCI | Enumerate omitted pickup items; metadata passes | Only ReplacementFusePickup missing; global metadata passed. `p1-s2-attempt2-diagnosis.json`. |
| S29/S30 | python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output Logs/WeaponSystemP1S2Attempt2 | Full isolated gate including builds and player scenarios passes | **Failed in EditMode**, 495 passed / 7 failed / 0 skipped. Pure and authoring stages passed. No PlayMode/build/player verdict obtained. `p1-s2-attempt2-gate-result.json`, `p1-s2-attempt2-gate-editmode.xml`. |

All seven gate failures are existing RoomAuthoring tests whose entry assertions require command-line flag `-solitudeRoomAuthoringVerification`. The generic gate's tests() function does not supply that flag. Six assertions explain the explicit isolation requirement; ExternalOccupancyValidatePreservesCleanSceneAndRebuildRecoversCurrentCells has the same flag assertion without text. No room behavior assertions ran in those failed cases. All newly added Step 2 cases passed in this run. This does not establish a complete regression pass.

## Proposed resolutions for human decision

1. Register the existing ReplacementFuse definition in Database.asset, preserving all identities and fields. This corrects catalog membership only, without modifying fuse behavior or weakening validation.
2. Update Tools/Verification/verify.py to pass `-solitudeRoomAuthoringVerification` to the isolated EditMode invocation, then rerun the full gate with fresh unique evidence. Keep tests and assertions intact. This file is outside the currently approved Step 2 list, so scope approval is needed before editing it.

The step and phase are marked blocked per qrspi-implement Error Handling. No Step 3 work started. Source remains uncommitted pending the required audit and full regression verdict.
