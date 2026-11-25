# Run lifecycle — Phase 1 Step 3 — Diagnosis 1

Status: authorized test-isolation repair applied; baseline diagnosis complete; full Step 3 still blocked/uncommitted.
HEAD: `fe68a638ace5b8dbcd4739e17413f03f9808ad71`; no staged files.
Spec: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Current normalized Phase 1: `081c93eb075e2c68e3894a802c8c203eb75fa09b7dab9691c502d8b8bd64eea3`.
Criteria S1/S5/S11/S14/S16 remain incompletely qualified; this diagnosis supports S14 and does not replace the full gate.

## Authorized repair

Human approved cleaning up IncompatibleResumeStaysGatedAndDoesNotEraseTheSavedSnapshot and checking recovery precision against the pre-Step-3 baseline. The fixture now restores the original valid lifecycle snapshot in finally, after its unchanged rejection, gated-control, preserved-invalid-snapshot and unchanged-position assertions. No runtime code or precision assertions changed in this repair. No full gate rerun occurred yet.

## Baseline experiment

Workspace `/private/tmp/solitude-run-lifecycle-p1-s2` was verified before and after execution against pre-Step-3 HEAD, with 233 tracked code/assembly files matching committed bytes (plus the original unrelated dirty baseline). The target test, recovery rules and runtime vital controller are unchanged at that baseline. Separate disposable saves were used.

Command:
`/Users/jeremyvalentine/.unity/bin/unity test /private/tmp/solitude-run-lifecycle-p1-s2 --mode PlayMode --filter SOLITUDE.Tests.PlayerVitalsSliceTests.ActualWakeupClockDelaysThenRecoversToBoundaryWithoutChangingOxygen --output /private/tmp/run-lifecycle-p1-s3-baseline-precision.xml --timeout 300 -- -logFile /private/tmp/run-lifecycle-p1-s3-baseline-precision.log -solitudeSaveDirectory /private/tmp/solitude-run-lifecycle-p1-s3-baseline-precision-saves`

Expected diagnostic: establish whether unchanged baseline can fail its exact boundary assertion. Actual: 0 passed/1 failed, no skips/inconclusive; expected 50, actual 49.9999924f. CLI exit 8; failed XML retained. This reproduces the same class of discrepancy before Step 3 (previous full gate actual 49.9999962f). No claim that the baseline test passed.

## Source explanation

`Domain/Vitals/VitalsRecoveryRules.cs` defines BoundaryRelativeTolerance = 1e-7 (0.00001 HP at maximum 100). Ceiling returns the supplied current health when it is within this band, treating it as reached without extra healing. Frame-driven recovery can enter that band just before the exact value. The old PlayMode test at PlayerVitalsSliceTests.cs:63/64 demands exact 50 HP and exact .5 bar fill. Its exact assertion is stricter than the existing domain tolerance; this is a pre-existing timing-sensitive test mismatch, reproduced without Step 3 changes.

## Concrete proposal — awaiting approval

Add `Tests/PlayMode/PlayerVitalsSliceTests.cs` to Step 3's file list. Change only the two 50-health assertions in the timed recovery test to use existing maximum-health * BoundaryRelativeTolerance, and its normalized .5 bar-fill assertion to use BoundaryRelativeTolerance. Retain all timing, delayed/recovering/finished status, oxygen, hunger and later boundary checks. No runtime recovery rule change or test skip. Then rerun the full required gate.

Proposed full Phase 1 artifact: `verification/p1-s3-precision-plan-proposal.md`.
Proposed normalized Phase 1 SHA-256: `961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089`.
Normalize only Status marker tokens to [ ]; no other Phase 1 content changed. Spec, overview and other phase revisions remain unchanged. Approval would replace the current normalized Phase 1 revision above; the proposal has not replaced the active phase plan.

## Preservation

All 371 unrelated dirty files remain byte-identical. The sole new source edit since attempt 2 is the authorized fixture finally cleanup, within the current plan. Other Step 3 source hashes unchanged. No staging/commit, no normal player save access. Full gate builds/process tests remain unrun for this repaired snapshot.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `4eddb10ebf36f87074c48a238c2e81b67c78d2de6097b9ebe509c719ac3d18dc` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `cc5f161908ff2f656ab86c5ec9a61c7be4f84d48ad692a2263f969684bafb247` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `2032516df089a9539bcaebcfb08e685a4df8bf46e9a9c84d7a50fca160022b74` |
| `Tools/Verification/verify.py` | `128771b1bc386f1024bd74fc0e7250c1924bacc0022f76e429eb123b36cde552` |
