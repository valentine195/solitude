# Run lifecycle — Phase 1 Step 2 — Attempt 3

Status: FAILED / not complete. No staging or commit. HEAD remains `3576e16dc12ff7eded770d50b1783d9dd2ce1c6a`.

## Approved scope

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Normalized Phase 1 SHA-256: `f337f4ca68ddf627908698208185820f2b9f410fd6ecc323ebe63351802932ea`.
Criteria: S1/S5/S11/S14, single-step mode. Human approved the test-only correction placing the second obstruction at collider.bounds.center; the expected rejection and no-mutation assertions remain intact. Runtime code is unchanged from attempt 2.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s2 --output /private/tmp/solitude-run-lifecycle-p1-s2-attempt3`.
Expected: complete required gate passes. Actual: FAILED, exit 1, `Interrupted write changed committed snapshot incorrectly`.

- 223 pure tests passed, 0 failed.
- Authoring validation passed.
- 343 EditMode tests passed, 0 failed.
- 119 PlayMode tests passed, 0 failed, including all 8 new resume tests. No skipped/inconclusive tests.
- Verification and shipping builds passed.
- Collection/restart/New Save and 10 save-candidate/recovery scenarios passed.
- Interrupted-write before-temp, after-temp and before-commit scenarios and their restarts passed.
- Interrupted-write after-commit process reached its checkpoint, but the runner seed assertion failed. Its restart and the four interrupted-recovery pairs did not run.
- No manual gameplay validation or independent review ran. The full gate remains failed; this step and its criteria are not complete.

## Criterion-linked evidence and limits

S1: fresh production binding initializes once; retained binding does not awaken again (new PlayMode fixture passed).
S5: production unload/rebind preserves identity, precise vitals/recovery, position and equipment; arrival preserves lifetime and uses authored arrival (fixtures passed).
S11: exact saved location and invalid-floor/blocked-location fallback passed; blocked fallback and incompatible resume reject without mutation and remain gated. Process restart of the living snapshot remains Step 3, not claimed here.
S14: all existing domain/EditMode/PlayMode regressions passed and both builds completed, but the required interrupted-file gate failed as detailed above.

## Failure evidence

`SaveGameService.LateUpdate` automatically flushes a dirty session. Production resume now stores the restored lifecycle snapshot, marking it dirty. The fault injector is armed from process startup solely by `-solitudeCrashAt`, while `SolitudeCompositionSmoke.Start` waits two frames before its `-faultWrite` branch increments worldSeed and invokes its target write. The startup automatic flush reaches after-commit first and kills the process. Baseline and write-after-commit primary files are structurally identical, including worldSeed 12345; checkpoint.txt contains after-commit. This is consistent with an earlier automatic write consuming the injection; it does not establish a failed atomic commit.

Source locations: `Core/SaveLoad/SaveGameService.cs` (LateUpdate/Flush), `Core/SaveLoad/VerificationFaults.cs` (Hit), `Verification/Player/SolitudeCompositionSmoke.cs` (Start faultWrite branch), `Tools/Verification/verify.py:93` (seed assertion).

## Proposed repair — awaiting approval

Adjust verification-only fault injection so `-faultWrite` cases arm immediately before the intended explicit smoke write, after startup binding completes. Preserve startup fault injection for recovery cases and retain every existing assertion and checkpoint. This requires a Step 2 plan adjustment adding `Core/SaveLoad/VerificationFaults.cs` and `Verification/Player/SolitudeCompositionSmoke.cs` to the approved file scope, with the explicit intended-write arming requirement. Show the revised normalized Phase 1 digest before resuming. Rerun the full gate. No repair or plan change applied yet.

## Preservation and snapshot

Approved spec/upstream provenance and all normalized plan revisions rechecked unchanged. The 9 tested feature files match the isolated copied source. All pre-existing dirty files remain byte-identical to the baseline. Git index is empty; no commit. Earlier failed evidence is retained.

Logs/XML/builds/scenarios: `/private/tmp/solitude-run-lifecycle-p1-s2-attempt3/`.
Full machine verdict and source manifest are preserved beside this report.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `8c742be0e60ef683d99693459f0f8def42814af6e922a1326b9b3a15bc3cafd3` |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `ff2ed9a67cf9086b9c4773ac030dd285f3e2b00086a9a2b360584cebca095ec1` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `c03e9edd6067eeff3da708fc6ada96d82d9ce247b5b0a79116c00fd0e4b864ba` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs` | `fb82fcda907e04d88ec1dd3833018c502b3222dd4fe69a57952923d5bbb5cef4` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs` | `a6d50a4db12c9352c0418c6b9b17b4cc33959908d5e62bc09085de787334add1` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs` | `65672610bac847a325cc6bfb563068f34617e78203bb3324826a586398f95509` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `53489312727ca9978d9c61364d7068ab56c4cacf4d6ce429418f6ec9099f5d01` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs.meta` | `a0972c016bcdec679462df74e30f69ee577d1073348798266d1a7278f7ef4b30` |
| `Assets/_Project/Editor/CompositionValidator.cs` | `4a84c314ee438e30744697e5a2a293d1424a8516c0273a6784f71cc63dbafc97` |
