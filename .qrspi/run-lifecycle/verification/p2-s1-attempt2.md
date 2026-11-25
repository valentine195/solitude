# Phase 2 Step 1 — attempt 2 (blocked)

## Scope and revision
Criteria S9, S14; single-step mode. Human approved the attempt-1 in-memory catalog repair. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; normalized Phase 2 plan SHA-256 `5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8`. No scope changes.
Source root `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`, base `62b17b02ae1a067bea59d8b623b2854cad6d8675`; [11-file source manifest](p2-s1-attempt2-source.json), SHA-256 `22296393de81b4042550caadd26e24f4f6c87702ccc5e5951e140b871f6814dd`. No source staged or committed.

## Verification
```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s1-attempt2
```
Expected complete gate pass. Actual **FAILED**, runner exit 1, PlayMode exit 8. Pure 219 passed; authoring passed; EditMode 339 passed; PlayMode 115 passed/1 failed/0 skipped. Both native builds and player scenarios remain unrun.

S9: fixture now successfully grants/installs fuse, saves installed/running/partial facts and restores 1.25 seconds through scene transition with one fuse remaining. Root recreation assertion at test line 160 failed: expected 1.25 seconds, actual 1.2519698413088918. Later operational/screen/task assertions unrun. Generic production repair scenario and new domain tests passed. S9 incomplete.
S14: executed existing regression tests passed; complete required gate incomplete, so no qualification.

## Diagnosis and proposed repair (not applied)
The test destroys its old composition root, which releases TimeSystem and sets Time.timeScale to 1. The new root is paused in sceneLoaded, during a frame whose deltaTime may already be positive. ScrubberEquipment.Update advances using this delta. The small increment is consistent with that loaded simulation tick; this result alone does not establish offline time advancement.

Propose a fixture-only change within RestartRoot: after old-root destruction and lease disposal, set Time.timeScale to zero and yield a frame before loading the new root, so scene creation starts with a zero simulation delta. Keep exact recovery assertions, runtime code and shipping assets unchanged. Rerun the complete gate as attempt 3. Await human approval under qrspi-implement Error Handling; step/phase marked blocked.

## Evidence
[Results](p2-s1-attempt2/results.json), [PlayMode XML](p2-s1-attempt2/playmode.xml), [EditMode XML](p2-s1-attempt2/editmode.xml), [PlayMode log](p2-s1-attempt2/playmode.log), [pure log](p2-s1-attempt2/pure-tests.runner.log). Full raw output retained at `/private/tmp/solitude-run-lifecycle-p2-s1-attempt2`.
