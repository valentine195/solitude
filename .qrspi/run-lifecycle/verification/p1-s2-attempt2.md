# Run lifecycle — Phase 1 Step 2 — Attempt 2

Status: FAILED / not complete. No staging or commit. Step 1 remains complete at `3576e16dc12ff7eded770d50b1783d9dd2ce1c6a`.

## Approved scope

Spec: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Normalized Phase 1: `f337f4ca68ddf627908698208185820f2b9f410fd6ecc323ebe63351802932ea`.
Criteria S1/S5/S11/S14; single-step execution. Human approved retaining Initialize's original signature and moving persistence setup into an internal composition method. Only the two planned composition files changed for that repair; the task-journal compatibility test is unchanged and now passes.

## Verification

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s2 --output /private/tmp/solitude-run-lifecycle-p1-s2-attempt2`

Expected: complete gate passes. Actual: 223 pure tests passed/0 failed; authoring passed; 343 EditMode passed/0 failed; PlayMode 118 passed/1 failed, zero skipped/inconclusive. All 111 pre-existing PlayMode cases passed; 7 of the 8 new resume cases passed. Builds and process scenarios did not run; no manual gameplay validation ran. The full required gate remains failed and criteria are not claimed complete.

## Failure

- `SOLITUDE.Tests.RunLifecycleResumeTests.BlockedSavedLocationFallsBackAndBlockedSafePointRejectsWithoutMutation`: Expected: <System.IO.InvalidDataException>   But was:  null

The new test successfully verifies fallback from its first blocked saved landing, then moves a small second obstruction to the awakening Transform origin and expects failure. The authored PF_Player solid BoxCollider2D has offset `(-0.018435359, 0.72913504)` and size `(0.7643883, 1)`, so the Transform origin is not the collision footprint center. A tiny obstruction at that origin does not establish that the safe landing footprint is blocked. Source evidence: `Assets/_Project/Features/Player/PF_Player.prefab:310` and `:320`; failing assertion in `Tests/PlayMode/RunLifecycleResumeTests.cs:159`.

## Proposed repair — awaiting approval

Change only the new test fixture's second obstruction placement to the player's actual collider.bounds.center after fallback, then synchronize physics and preserve the expected InvalidDataException/no-mutation assertions. No runtime collision checks or scene/prefab authoring need changing based on this evidence. The test file is already within Step 2's approved scope. Rerun the full gate; do not skip the blocked-safe-point case. No repair applied yet.

## Preservation

All tested feature source hashes and all original dirty baseline hashes still match. No unrelated source changes, no staged files, no commit. Logs/XML/verdict: `/private/tmp/solitude-run-lifecycle-p1-s2-attempt2/`. Source manifest: `/private/tmp/run-lifecycle-p1-s2-attempt2-source.json`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `8c742be0e60ef683d99693459f0f8def42814af6e922a1326b9b3a15bc3cafd3` |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `ff2ed9a67cf9086b9c4773ac030dd285f3e2b00086a9a2b360584cebca095ec1` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `c03e9edd6067eeff3da708fc6ada96d82d9ce247b5b0a79116c00fd0e4b864ba` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs` | `fb82fcda907e04d88ec1dd3833018c502b3222dd4fe69a57952923d5bbb5cef4` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs` | `a6d50a4db12c9352c0418c6b9b17b4cc33959908d5e62bc09085de787334add1` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs` | `65672610bac847a325cc6bfb563068f34617e78203bb3324826a586398f95509` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `b7ce3efc5ddd3344d91393441a52f26a2a0f43371bbf7024973b1fc2220b77b6` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs.meta` | `a0972c016bcdec679462df74e30f69ee577d1073348798266d1a7278f7ef4b30` |
| `Assets/_Project/Editor/CompositionValidator.cs` | `4a84c314ee438e30744697e5a2a293d1424a8516c0273a6784f71cc63dbafc97` |
