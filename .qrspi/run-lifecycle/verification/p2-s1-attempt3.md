# Phase 2 Step 1 — attempt 3 (blocked)

## Approved scope and source
Criteria S9/S14, single-step mode. Approved attempt-2 fixture pause repair applied. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; normalized phase plan SHA-256 `5f41bb13ab5b601a31eb736429c24ddf1763faa256b5a785f35c5ba3d41e2cd8`. No substantive plan changes.
Source root `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`; base `62b17b02ae1a067bea59d8b623b2854cad6d8675`; [source manifest](p2-s1-attempt3-source.json), SHA-256 `93af1c0511e68a03002f250869b7f3db1862fd3f982acbb3b53d0138beb8103c`. No source staged or committed.

## Verification
Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s1-attempt3`.
Expected full gate pass. Actual **FAILED**, runner exit 1; PlayMode exit 8. Pure 219 passed; authoring passed; EditMode 339 passed; PlayMode 115 passed/1 failed/0 skipped. Builds and player scenarios unrun.

S9: scrubber fixture now passes grant, exact one-item installation, running/partial save, scene rebind, exact 1.25-second disk restoration, retry without consumption, completion and operational scene rebind. Failure at line 169, access.Interact(...).IsSuccess expected true, actual false. Final telemetry/task assertions unrun. Generic production repair and domain tests passed. S9 incomplete.
S14: existing executed suites passed; full qualification incomplete.

## Diagnosis and proposed repair (not applied)
InputPolicyCoordinator.AcquirePrimaryScreen explicitly refuses while PauseCoordinator.IsPaused. The fixture still holds its deterministic pause lease when attempting the final screen interaction. This is expected input policy behavior, not a persistence failure.
Propose fixture-only change: dispose and clear pauseLease immediately before the final access interaction (after operational restoration assertions). Keep all exact recovery and no-extra-consumption checks. Then rerun the full gate as attempt 4. No production code, assets, or plan scope changes. Awaiting human approval per qrspi-implement Error Handling; step/phase marked blocked.

[Results](p2-s1-attempt3/results.json), [PlayMode XML](p2-s1-attempt3/playmode.xml), [EditMode XML](p2-s1-attempt3/editmode.xml), [PlayMode log](p2-s1-attempt3/playmode.log). Raw output retained at `/private/tmp/solitude-run-lifecycle-p2-s1-attempt3`.
