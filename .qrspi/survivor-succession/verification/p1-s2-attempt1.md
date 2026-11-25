# Survivor Succession — Phase 1 Step 2 — Attempt 1

## Scope and revisions

Single-step continuation, Phase 1 Step 2. Base commit: `8790eac3fa8b7a32a0ba49e739e0d6fbb1071c4a`.
Spec SHA-256: `f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4`.
Approved normalized overview and all three phase digests, plus upstream provenance, match before execution and after verification.
Exact source hashes are in `p1-s2-attempt1-source.json` (1183 project source/assets/metadata files). No scoped source changed during verification.
The work remains unstaged and uncommitted. Step marker is `[!]`; Phase 1 remains incomplete. No helper phase-completion log was written.

## Changes under verification

Added opt-in succession composition, retained-avatar adapter and authored cryobay point. Wakeup authoring ran successfully through the connected Unity Editor API; existing endpoints and GUIDs were retained. Narrow context additions provide existing hotbar/selection, screen closure and feedback. Reset preserves Vital/readers/camera bindings, cancels gestures independently of modal state, suppresses held input until release and rebuilds proximity after relocation. Equipped deaths remain rejected without item or identity mutation. No ship-system logic or save schema changed.

## Command and actual results

```sh
DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-p1-s2-gate --output Logs/SurvivorSuccession/p1-s2-attempt1
```

Expected: complete successful gate, including native verification/shipping builds and player save/recovery scenarios.
Actual: exit 1; results.json status `Failed` after PlayMode exit 8. Pure: 106 passed/0 failed. Authoring validation: passed. EditMode: 204 passed/0 failed. PlayMode: 50 passed/1 failed (45 existing and five new tests passed).
Builds/player scenarios did not run. Manual runtime visual observation did not run. No complete qualification or step completion is claimed.
Reports: `Logs/SurvivorSuccession/p1-s2-attempt1/{results.json,editmode.xml,playmode.xml}`.

## Criterion-linked evidence

| Criteria | Expected | Actual |
| --- | --- | --- |
| S1/S2/S3 | Distinct B at authored point, retired A, reset vitals/selection, unchanged readers | EmptyDeathResetsRetainedAvatarVitalsSelectionReadersAndFeedback passed; A→B→C and stale A checks included. |
| S7 | Held movement neutral, fresh movement resumes, screens close, gestures cancel | HeldMovementAndInventoryAreNeutralUntilReleaseThenNewInputWorks; EquippedDeathIsRejectedAndCancelsGestureWithoutAnOpenModal; OpenJournalClosesWithoutRebindingUnconfiguredDemo passed. |
| S8 | One visible acknowledgment per successful death, no stale/rejected success | EmptyDeathResetsRetainedAvatarVitalsSelectionReadersAndFeedback and rejection cases passed. First-contact continuity remains unverified because its test setup failed. |
| S10 | Invalid point/nonempty equipment cannot retire current survivor or change ownership | InvalidPointRejectsDeathWithoutActivatingSuccessor and equipped-death rejection passed. Missing/wrong-scene/inactive authoring cases passed in EditMode. |
| S6/S11 | Existing powered door/first contact persists, one retained interactor and world scope | PoweredDoorFirstContactAndPickupRemainLiveAcrossDeath failed before its death assertions. Other reset/readers and configuration checks passed; continuity case remains unresolved. |
| S12 | Existing regressions and full gate pass | Existing pure/EditMode/45 PlayMode cases passed. Full gate failed before builds/player scenarios, so qualification is incomplete. |

## Failure and proposed next action

The continuity test loads the existing opening art reference scene through EditorSceneManager, then calls the production `StartNewSave()` API. That API reloads via SceneManager.LoadScene, but the reference scene is intentionally outside the build scene list. Unity emits an unexpected Error log at GameCompositionRoot.cs:169; the test fails at SurvivorSuccessionSliceTests.cs:132 before continuity assertions.

Proposed repair: remove the unnecessary StartNewSave call for the opening reference scene, whose OpeningReviewSession already supplies a fresh in-memory save store. Keep the reference scene outside the shipping build list. Rerun the complete gate as a new uniquely named attempt. No repair has been applied; interactive QRSPI failure handling requires the human decision first.
