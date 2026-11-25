# Phase 2 Step 4 — attempt 2

Outcome: FAIL at PlayMode verification. No source staged/committed; step and phase remain [!].

## Approval and inputs

Human approved the single missing namespace import and full verification rerun. The only source change from attempt 1 is `using SOLITUDE.Application;` in WeaponCombatIntegrationTests.cs. Fixture and metadata bytes unchanged. Spec SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5; normalized overview d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc; Phase 2 ae7b665ae5d3d33ece4484f592ecf0f75ed2c15508c37d35a8d2f18421bb99d2. Upstream/all plan revisions validated on resume. Baseline a06353860ae360044e38bf558cf13ea9a8157f05. Three-file snapshot p2-s4-attempt2-source.json matches working/isolated verification source; unrelated SOLITUDE_Start.unity remains excluded.

## Verification

Live Unity recompile_status completed without errors (failed=false, compilationFailed=false). Full command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output /Users/jeremyvalentine/SOLITUDE/Logs/WeaponSystemP2S4Attempt2.

Expected: integration tests, existing suites, both builds and recovery checks pass.
Actual: results.json Failed at playmode exit 8. Pure tests 269 passed/0 failed; Editor 516 passed/0 failed/0 skipped; PlayMode 223 total, 218 passed, 5 failed/0 skipped. Builds and player recovery checks not reached. All five failures are in the new integration fixture; existing 214 PlayMode tests passed. Retained XML/results/pure log alongside this report.

Four new cases passed: range/arc/cover miss followed by valid hit; actual robot pulse interrupts Windup, movement recovers and a new swing hits; same-definition replacement cleans presentation and preserves lockout; two actors sharing definitions retain independent execution/damage/cancellation. These provide partial evidence for S8/S11/S15 and S17–S24, not a qualified complete step or phase.

## Failure analysis

1. FourActualPrimarySwingsDefeatAuthoredRobotAndClearContactResources reached its final swing-count assertion (line 95), expected 4 but observed 9. Each of the four 40→30→20→10→0 health/lifecycle iterations passed before that point. LostEquipmentOwner (line 145), SceneRelease (line 154), and TemporaryMeleeDefinition (line 198) fail at swings.Single() because the collection contains prior tests' executions. NUnit reuses the fixture instance; Prepare never clears the readonly HashSet. Existing EquippedWrenchSwingTests explicitly clears its analogous collection in Prepare. This is shared mutable test-fixture bookkeeping, not mutable item/action asset state.

2. VitalZero test reaches line 130 after proving immediate active-action cancellation, restored presentation, denied fresh input and no additional damage. It then incorrectly asserts recipient.IsAvailable is false. Sec0DamageRecipient availability depends on its own living health/controller binding and policy, not the attacking player's health. SEC-0 remains alive at 30 health and may receive damage from another actor. Its controller's target-availability predicate gates outgoing behavior; those are different responsibilities. Preserve recipient availability; assert the player remains at zero health and cannot request an action instead. No product behavior fix is indicated by this failure.

Implementation stopped at the failed gate. No assertions removed or runtime code changed.

## Proposed bounded repair

Only WeaponCombatIntegrationTests.cs: clear swings in Prepare before subscribing; replace the incorrect target-unavailability assertion with explicit player-zero-health and live-target-availability assertions, retaining denied-action/no-further-damage checks. Rerun the full isolated gate with unique attempt-3 evidence. No changes to runtime code, item assets, enemy balance, input, test scope or approved plan content. Await human approval under qrspi-implement failure handling.
