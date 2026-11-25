# Phase 2 Step 1 — shared notice lifecycle

## Scope

Criteria S9/S10/S11/S13/S15. Spec SHA-256 b2875cdba9c829988e5d3320633c838862e2de5e7debed4a6d7ba470ca987ff8. Approved normalized plans in phase2-entry-revisions.json; user “commit and continue” authorizes entry and transfer of outstanding visual qualification to Step 4. Provenance checked PASS before commit. Base 48c3503; commit c148917fc8771f96a2e466cde8562d9a69c5b9ac. Only the four new coordinator/test/metadata files changed in this step, recorded in phase-2-step-1-attempt1/source-sha256.json.

## Checks

S9: deterministic tests cover one added notice per discovery, only observed Active terminal completion/failure notices, unchanged/stage/availability silence, same-task unpresented coalescing preserving other-task FIFO, and four seconds of successfully rendered unpaused exploration. A one-second visible interval plus blocked intervals plus three visible seconds expires; primary screens, pause, transition and readiness suppress presentation without consuming dwell.

S10/S15: abandonment, owning scope removal and journal session disposal drop current/pending notices; coordinator disposal removes subscriptions and hides the lane.

S11: first discovered Completed/Failed gives Added with labeled latest outcome, never fabricated Active or a second terminal notice.

S13: display and diagnostic exceptions are isolated; source completion remains Applied, stored terminal state and other observers remain correct. Failed rendering does not consume dwell.

Late coordinator/view binding does not replay preexisting or expired notices; a pending current notice resumes remaining dwell after rebind. The Application coordinator uses existing InputPolicyCoordinator.Gameplay and injected Advance elapsed seconds; no Unity or TaskJournal clock dependency. No view/focus manipulation is implemented in this step.

Verification command: Unity-bundled dotnet run --project Tools/Verification/PureTests.csproj -p:NUnitPath=<project Library/PackageCache NUnit DLL>. Expected whole pure suite passes; actual 90 passed, 0 failed, including eight new notice cases. First preflight discovered the new fixture namespace was outside the runner's exact SOLITUDE.Tests selection (82 tests, not counted as new-test evidence); corrected namespace then reran 90. Logs preserved.

Unity commands: unity command recompile, then recompile_status --project-path /Users/jeremyvalentine/SOLITUDE --json. Actual completed, failed false, errors [], compilationFailed false after final source. git diff --cached --check PASS before commit. Required no-build scope satisfied for Step 1; full gate and real notice lane presentation are deliberately unrun until Step 2.

## Remaining work

Step 2 authors/wires the notice prefab and removes demo duplicate completion presentation. Steps 3–4 qualify authoring/lifecycle and final behavior/visuals. Outstanding Phase 1 visual checks remain required in Step 4. No fresh review or full-feature completion claim. Pinning removed; tracked task feature deferred.
