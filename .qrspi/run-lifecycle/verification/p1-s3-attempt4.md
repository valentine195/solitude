# Run lifecycle — Phase 1 Step 3 — Attempt 4

Status: FAILED / not complete. HEAD fe68a638ace5b8dbcd4739e17413f03f9808ad71; no staging/commit. Single-step execution.

## Approved inputs and repair

Spec SHA-256: a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922.
Normalized Phase 1 SHA-256: 961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089.
Human approved replacing the two inaccessible PreciseMax references with public p.Settings.MaxHealth. That two-reference correction is applied; approved tolerance and runtime recovery rules are unchanged. Existing invalid-save finally cleanup retained.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s3 --output /private/tmp/solitude-run-lifecycle-p1-s3-attempt4` with required filesystem access.
Expected: full required gate passes. Actual: failed, exit 1, Recovery corrupted backup.

223 pure tests, 343 EditMode tests and all 122 PlayMode tests passed (688 total; no failed/skipped/inconclusive Unity tests). Authoring validation, verification build and shipping build passed. The 11 RunLifecycleResumeTests cases passed, including clean-session checkpoints, saved production-scene routing/New Save, invalid-scene preservation and rejection cleanup.

Player world-collect, world-restart-newsave, partial-temp and missing-primary processes returned success. After missing-primary exited, the runner's original-backup-byte comparison failed. Remaining candidate, interrupted-write/recovery, and four living-colonist process scenarios did not run. No manual validation or independent Review ran.

## Failure evidence

Baseline primary vs missing-primary backup differ only in survivor.hunger.current:
- Expected original backup: 99.99587188933509.
- Actual backup: 99.99387189012485.
All other parsed fields match. The snapshot-only smoke waits two frames before verification; live hunger advances in that window. With Step 3's new quit/dirty capture, the process writes those live values and rotates the normal backup. Recovery promotion itself preserves the backup; the failure follows ordinary post-startup save rotation in a process intended to check an unchanged snapshot. The recovery notice and snapshot assertions inside the process passed before the outer backup assertion failed.

Source: Verification/Player/SolitudeCompositionSmoke.cs Boot/Start/Verify; SaveGameService.cs Flush/LateUpdate/OnApplicationQuit; Application/Persistence/SaveRecovery.cs CompleteRecovery/Write; Tools/Verification/verify.py backup comparison.

## Proposed repair — awaiting approval

Extend the existing verification-only startup pause lease in SolitudeCompositionSmoke.Boot to include -verifySnapshotOnly, before any simulation Update. The current pause already covers living-snapshot process fixtures. Keep all primary/backup snapshot and notice assertions, every interruption checkpoint, and production save/simulation behavior unchanged. This one-condition test-fixture adjustment is within Step 3's approved smoke file scope and does not require a plan revision. Rerun the full gate. No repair applied yet.

## Criterion-linked evidence and limits

S1/S5/S11/S16: all planned PlayMode restore/checkpoint/scene-routing/New Save assertions passed. The additional process-level exact living snapshot/fallback/invalid-scene scenarios remain unrun, so Step 3 is incomplete.
S14: domain/Unity tests, authoring, both builds and early collection/restart checks passed. Backup preservation failed; remaining recovery checks unrun. No full regression pass claimed.

## Preservation

All seven feature source hashes and 371 original dirty baseline hashes match; HEAD unchanged and index empty. No scene/prefab edits or unrelated source changes. Earlier failures and baseline diagnosis retained.
Logs/XML/builds/save candidates/verdict: `/private/tmp/solitude-run-lifecycle-p1-s3-attempt4/`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `4eddb10ebf36f87074c48a238c2e81b67c78d2de6097b9ebe509c719ac3d18dc` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `cc5f161908ff2f656ab86c5ec9a61c7be4f84d48ad692a2263f969684bafb247` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `2032516df089a9539bcaebcfb08e685a4df8bf46e9a9c84d7a50fca160022b74` |
| `Tools/Verification/verify.py` | `128771b1bc386f1024bd74fc0e7250c1924bacc0022f76e429eb123b36cde552` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs` | `d79d4e9d782338909f22649e6963fd3d9164a2343eacd9b4bf9cad2ecb59ea04` |
