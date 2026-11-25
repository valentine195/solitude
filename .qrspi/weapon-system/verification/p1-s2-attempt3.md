# Phase 1 Step 2 — attempt 3

Outcome: BLOCKED. No staging or commit.

Human approved the proposed fuse catalog registration and room-test runner flag. Plan updated before execution, with normalized overview SHA-256 `cced6f0825d297a5f742954287d6960a9076c66e492664d577584a866091eca9` and Phase 1 SHA-256 `f73e17735e912653514abdd0da39adf84df74f0a99e71fdf4ae050ffdc84b1ab`. Spec remains `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`. All previous source hashes/provenance/normalized plan hashes verified before work. Tested source matches 32-file `p1-s2-attempt3-source.json` and copied isolated project; rechecked after test failure. Existing unrelated scene modification remains untouched.

## Criterion-linked results

S7/S30: Registered existing fuse using AssetDatabase/SerializedObject; identity retained. ItemCatalogValidator.AuditAllAssets and UnityMetaValidator passed. Result: `p1-s2-attempt3-catalog.json`.

S29/S30: Full `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output Logs/WeaponSystemP1S2Attempt3` failed in EditMode. Pure suite passed (257 cases), authoring passed; Editor **500 passed, 2 failed, 0 skipped**. Seven prior room flag failures are fixed; all Step 2 item-action cases passed. PlayMode/build/player stages did not run. Result: `p1-s2-attempt3-gate-result.json`, XML `p1-s2-attempt3-gate-editmode.xml`.

Failing tests:
- ScrubberEncounterAuthoringTests.FuseCatalogAndSceneInstanceHaveDistinctStableIdentities, line 145.
- ScrubberTaskSourceTests.FeatureKeepsMissingFuseCatalogTaskEligibilityFailClosed, line 133.

Both explicitly assert that ReplacementFuse is absent from the production Database.asset. The demo has its own ScrubberItemDatabase.asset containing the fuse. This is intentional demo/production separation, not missing production content. The implementer incorrectly classified the broad audit finding as a broken production catalog in attempt 2; adding the fuse changed established behavior and caused these failures. The global AuditAllAssets helper assumes every pickup belongs to the one production catalog, which is incompatible with this existing demo boundary. That broad helper is not the production validation gate; its ValidateForCI delegates to WakeupValidation.

## Proposed recovery requiring human decision

Remove only the implementer's new fuse entry from production Database.asset; preserve fuse GUID, demo catalog and tests. Retain the approved room-verification runner flag. Validate new item capabilities directly and use the existing WakeupValidation production gate, while recording the broad all-pickup audit limitation rather than weakening it or changing demo behavior. Rerun full isolated verification. Update the approved phase-plan exception accordingly before resuming. No Step 3 work started.
