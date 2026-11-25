# Phase 1 repair 2 — attempt 1

PASS: all 32 focused Editor tests passed, 0 failed/skipped.

Spec SHA-256 `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`; normalized plan set `839de1dd66a4b31b5640c91285eba9406214aeba0de3c9180eec1f74a17588fe`. Repair addresses phase-1-r1 F1 within existing Phase 1 S2/S14 ownership-isolation scope. Human approved this fix, regression tests and another feature-scoped independent review; unrelated untracked work remains excluded from that next review's scope check only.

Safety now rejects any generated subtree containing a loaded room's referenced authored occupancy before Rebuild enters cleanup. The loaded-object lookup includes inactive/preview scenes, filters out assets/nonloaded scenes, and runs only during explicit editor operations. The deletion boundary rechecks safety so a direct writer Clear cannot bypass ownership refusal.

S2/S14: three regressions move another room's occupancy directly/nested beneath target output, including the owning room in a separate loaded preview scene. Validate reports the issue without deleting data; Rebuild returns false; direct writer Clear throws before deletion. Both rooms retain occupancy references, exact cells and generated object references. Moving occupancy back allows both rooms to Rebuild. Existing missing-container recovery/Undo, native geometry, persistence and reload checks continue to pass.

Command: `unity test /private/tmp/solitude-room-authoring-phase1 --mode EditMode --filter SOLITUDE.Tests.RoomAuthoring --output <verification>/phase-1-repair-2-editor-tests-attempt-1.xml --timeout 300 --format json -- -nographics -solitudeRoomAuthoringVerification -logFile <verification>/phase-1-repair-2-editor-tests-attempt-1.log`.

Expected/actual: 32 pass, no failures or skips. Exact log/XML, initial changed-source snapshot and full matching first-party C#/assembly snapshot retained under repair-2 attempt-1 names. Approval provenance and preexisting dirty-file hashes rechecked after verification; staged whitespace check passed. No runtime/gameplay source change. Dedicated floor/ship tests at 826b212 remain historical, not rerun for this editor-only guard. Native Standard joins/build/brush checks remain later phases. Independent re-review pending.

Commit: `bc0a036142c9c7195aeebf5c314e4c5a05497195`.
