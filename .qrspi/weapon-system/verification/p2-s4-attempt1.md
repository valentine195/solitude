# Phase 2 Step 4 — attempt 1

Outcome: BLOCKED by test-source compilation. No tests or full gate run; no files staged or committed. S8/S11/S15/S22/S29 remain unqualified for this step.

## Inputs and source

Spec SHA-256 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5. Approved normalized overview d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc; Phase 2 ae7b665ae5d3d33ece4484f592ecf0f75ed2c15508c37d35a8d2f18421bb99d2. Upstream and all plan revisions matched on resume. Baseline a06353860ae360044e38bf558cf13ea9a8157f05. Three-file draft snapshot in p2-s4-attempt1-source.json. Unrelated SOLITUDE_Start.unity edit preserved and excluded.

## Draft changes

Added an opt-in disposable Sec0Patrol scene loader to the existing fixture and nine integrated encounter scenarios in WeaponCombatIntegrationTests. They use the actual scene/root/input/selection/action/resolver/recipient path with an explicitly stepped gameplay clock. Draft scenarios cover four-hit defeat, range/arc/cover misses, actual SEC-0 pulse stun interruption/recovery, vital-zero interruption, same-definition replacement lockout, equipment owner loss, scene release, independent actors sharing assets and a temporary differently tagged/configured melee definition through the unchanged primary-input route. No product source, authored asset, room geometry, production grant or save-format changes.

## Failed verification

Commands: Unity CLI command recompile, followed by recompile_status, against /Users/jeremyvalentine/SOLITUDE, caller plugin / skill qrspi-implement.
Expected: compile the integration test source without errors.
Actual: transport success; nested status completed with failed=true and compilationFailed=true. Diagnostics:

- Assets/_Project/Tests/PlayMode/WeaponCombatIntegrationTests.cs(163,39): error CS0246: The type or namespace name 'HotbarSelectionState' could not be found (are you missing a using directive or an assembly reference?)
- Assets/_Project/Tests/PlayMode/WeaponCombatIntegrationTests.cs(193,39): same CS0246 diagnostic.

Cause: the new test file omitted using SOLITUDE.Application, which owns the existing HotbarSelectionState. Implementation stopped at the first failed check, with step and phase marked [!]. No behavior or regression pass claimed. Existing Step 3 results remain historical evidence for their committed revision.

## Proposed bounded repair

Add using SOLITUDE.Application to WeaponCombatIntegrationTests.cs, recompile, then run the complete isolated verification gate and retain a unique attempt-2 report. No assertion, runtime behavior, scope or plan-content change proposed. Await human decision under qrspi-implement error handling.
