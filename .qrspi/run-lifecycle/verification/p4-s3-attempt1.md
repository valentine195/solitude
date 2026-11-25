# Phase 4 Step 3 — full gate attempt 1 (FAILED)

Spec: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Approved plans: `p4-s3-approved-plans.json`. Source: `p4-s3-attempt1-source.json`, base `6e42cdc02d46bd298125ef9f2462207d3da468af`. All 13 source file hashes remained unchanged after verification. No Step 3 commits or staging.

The approved PixelPerfectCamera namespace repair compiled successfully (`/private/tmp/solitude-p4-s3-compile-attempt2.log`).

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-s3-attempt1`.
Expected: full required gate PASS, including S2/S8/S15 journeys and S4/S6/S9/S10 integration.
Actual: pure tests 252/252; authoring PASS; EditMode 376/376; PlayMode 131 passed, 23 failed. Gate stopped before builds and process scenarios. No screenshot evidence was produced. Required criteria remain unverified.

## Causes and proposed repair

1. One ship-persistence socket test uses a global Single<OpeningPowerSocket>, now ambiguous with production socket. Select its fixture socket by SocketDoorId, preserving all assertions.
2. Twenty legacy succession tests fail setup because PhaseFiveFixtureBuilder clones the new production-only expedition encounter into DisposableSuccession. Runtime correctly rejects it in a disposable session. Amend Step 3 to include the fixture builder: remove only Production Expedition Route from the generated disposable copy, refresh feature/pickup references, preserve production/resume scenes and existing legacy cache assertions. Do not weaken the production encounter contract.
3. New ordinary-input route reaches and opens the scrubber, but Enter does not install the fuse. Test helper manually calls InputSystem.Update before EventSystem processing; use queued frame-driven UI Submit as the existing passing ScrubberEncounterSliceTests does, and assert the intended button is interactable before submitting. This is a supported hypothesis, not yet a verified repair.
4. Lost-fuse test successfully dies and reacquires the fuse, but locker modal does not open at (2.2,3). Its authored sensor center is (2.5,4.5), radius1; player collider center is offset upward .56, and movement tolerance can stop just outside its edge. Approach at (2.5,3.1) within the sensor while remaining below the existing solid collider; assert ordinary focus before E. Reachability still needs verification.

Proposed Phase 4 plan: `p4-s3-fixture-repair-proposal/plan-phase-4.md`; proposed complete normalized manifest: `p4-s3-fixture-repair-proposal/proposed-approved-plans.json`.

On approval, adopt exact proposed plan, implement only these repairs, capture a new source snapshot, rerun the full gate, and commit only after PASS. Existing source remains untouched after the failure. Detailed failures: `p4-s3-attempt1-failures.json`; raw logs/XML: `p4-s3-attempt1/`.

QRSPI Implement requires: “In every mode, stop immediately and wait for the human when a step fails, a blocker appears, or a step can't be done as written.” Step marked [!].
