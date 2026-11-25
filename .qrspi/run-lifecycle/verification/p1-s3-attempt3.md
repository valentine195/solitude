# Run lifecycle — Phase 1 Step 3 — Attempt 3

Status: FAILED / not complete. No staging/commit. HEAD fe68a638ace5b8dbcd4739e17413f03f9808ad71. Single-step mode.

## Approved scope

Spec SHA-256: a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922.
Normalized Phase 1 SHA-256: 961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089.
Human approved the exact proposed plan revision adding PlayerVitalsSliceTests.cs and the established health/bar tolerance; it was applied and shown before implementation. Runtime recovery rules were not changed. The previously authorized invalid-save finally cleanup remains applied.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s3 --output /private/tmp/solitude-run-lifecycle-p1-s3-attempt3` (required filesystem access).
Expected: complete gate passes. Actual: FAILED at authoring, CLI exit 6; Unity scripts did not compile. Pure tests: 223 passed, 0 failed. Authoring validation could not execute; EditMode, PlayMode, builds and player scenarios did not run. No manual gameplay validation or independent Review ran.

## Failure and proposed repair

The two new health-boundary tolerance expressions use `p.Vitals.Health.PreciseMax`. Vital.PreciseMax is internal to Domain and is not accessible from SOLITUDE.Tests.PlayMode, producing CS1061 in PlayerVitalsSliceTests.cs lines 63 and 64. This was an implementer error, not a missing gameplay capability. The public PlayerVitalsController.Settings property exposes VitalsRecoverySettings.MaxHealth, the same configured maximum for this test.

Proposed repair, awaiting human approval: replace only the two inaccessible `p.Vitals.Health.PreciseMax` references with `p.Settings.MaxHealth`. Preserve the approved BoundaryRelativeTolerance, all other assertions, existing assembly visibility and runtime code. No plan scope/revision change is necessary. Rerun the full gate. No repair applied yet.

## Criterion status

S1/S5/S11/S14/S16 remain incompletely qualified for Step 3. The pure regression suite passed; no Unity behavior is qualified for this snapshot because the test assembly did not compile. The baseline precision diagnosis remains historical evidence of the pre-existing exact-assertion mismatch, not a current gate pass.

## Preservation

All seven tested feature file hashes match the snapshot, all 371 original dirty files remain byte-identical, HEAD unchanged and index empty. No unrelated work was staged or modified. Earlier attempts and baseline diagnosis remain retained. Logs/verdict: `/private/tmp/solitude-run-lifecycle-p1-s3-attempt3/`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `4eddb10ebf36f87074c48a238c2e81b67c78d2de6097b9ebe509c719ac3d18dc` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `cc5f161908ff2f656ab86c5ec9a61c7be4f84d48ad692a2263f969684bafb247` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `2032516df089a9539bcaebcfb08e685a4df8bf46e9a9c84d7a50fca160022b74` |
| `Tools/Verification/verify.py` | `128771b1bc386f1024bd74fc0e7250c1924bacc0022f76e429eb123b36cde552` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs` | `78204cfa8e4cba1a3bf35ad539b3601987f2bfa4c02d6c2e0e0795a49e776100` |
