# Phase 2 Step 3 — attempt 1 (blocked)

## Approved scope and snapshot
Criteria S7, S8, S13, S14; single-step mode. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; normalized Phase 2 plan `0195a7488b9931af5f0c06d0bbfa3ffe1a05918118ed6883094434792db6ba1f`. All upstream provenance/plan revisions checked before verification.
Source root `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`; base `c18e6b29f6cd4b77b543bdef4419e71fe7d5200b`; [12-file source/meta manifest](p2-s3-attempt1-source.json), SHA-256 `022d04b504461ddb65fc3a059795fe95e6de9454a0852695f05b06c454ebd3f0`. No Step 3 source staged or committed.

## Verification
Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s3-attempt1`.
Expected complete gate PASS. Actual **FAILED**, runner exit 1/PlayMode exit 8. Pure 225 passed; authoring passed; EditMode 345 passed; PlayMode 109 passed/14 failed/0 skipped. Both builds and standalone player scenarios unrun.

New domain cases passed: S8 prepared loss reconciliation without early publication, saved/unloaded retained component suppression, live-container precedence, completed-objective suppression, full-inventory rejection and coherent collection; S13 legacy collected source reconciliation without clearing ordinary IDs and without duplicating usable carried components; S7 ordinary collection facts retained. PlayMode installation/disk suppression and critical authoring checks passed. S14/full gate incomplete due teardown regression.

## Failure and proposed repair (not applied)
All 14 PlayMode failures report WorldPickup MissingReferenceException from `WorldPickup.ReleaseBinding`, source line 63: new GetComponent<CriticalComponentSource>() queries a pickup whose Unity object was already destroyed. SceneBindings teardown can release its retained C# reference after that destruction. Previously ReleaseBinding only touched managed registration fields and remained safe.

Propose a narrow WorldPickup repair: cache the optional critical-source component during Initialize, release it using Unity's live-object null check, and clear the cached field. Keep managed registration disposal idempotent and usable after the native object dies. Do not call GetComponent in ReleaseBinding. Apply within current approved file scope and rerun the full gate as attempt 2. No plan/spec changes. Await human approval under qrspi-implement Error Handling; step/phase blocked.

The new source policy deliberately requires source and protected shared storage to have no repair prerequisites for this slice; it rejects declared lock dependencies rather than guessing geometry. Mappings include all authored scene lockers and explicit IDs for unloaded owners; undeclared storage holding a critical component rejects reconciliation before mutation. Death preparation returns an isolated post-loss snapshot; phase 3 must commit it with succession. No death implementation claimed here.

[Results](p2-s3-attempt1/results.json), [PlayMode XML](p2-s3-attempt1/playmode.xml), [EditMode XML](p2-s3-attempt1/editmode.xml), [PlayMode log](p2-s3-attempt1/playmode.log), [pure log](p2-s3-attempt1/pure-tests.runner.log). Raw output retained at `/private/tmp/solitude-run-lifecycle-p2-s3-attempt1`.
