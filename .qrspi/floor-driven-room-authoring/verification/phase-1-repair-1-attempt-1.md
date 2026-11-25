# Phase 1 repair 1 — attempt 1

PASS: all 29 focused Editor tests pass (0 failed/skipped). Commit recorded separately in helper decision/history.

Approved spec: `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`; normalized plan set `839de1dd66a4b31b5640c91285eba9406214aeba0de3c9180eec1f74a17588fe`. Repair is within existing S3/S17 behavior and Phase 1 scope, addressing review phase-1 F2. Human approval also restricts the next review's untracked-file check to feature paths (F1); unrelated files preserved.

Rebuild preflights identity, occupancy ownership and native transforms while admitting a genuinely missing output container. Conflicting retained ownership markers prevent replacement. An unambiguous replacement is created and serialized inside the same Undo group as generated content. Validate remains nonmutating and continues diagnosing a missing container.

S3/S17: regression deletes entire container, confirms Validate does not recreate it, then Rebuild restores identical sorted sprite assets/positions/counts plus 48 floor cells, unchanged identity and occupancy. S14/S2: neighbor output and same-name sentinel remain intact; duplicate identity, retained ownership root with missing reference, and unsupported scale refuse recovery without hierarchy or cell mutation. S15 supporting safety (brush remains Phase 2): Undo removes replacement root/restores null reference, Redo restores owned root and equivalent output; repeat Rebuild is idempotent.

Command: `unity test /private/tmp/solitude-room-authoring-phase1 --mode EditMode --filter SOLITUDE.Tests.RoomAuthoring --output <verification>/phase-1-repair-1-editor-tests-attempt-1.xml --timeout 300 --format json -- -nographics -solitudeRoomAuthoringVerification -logFile <verification>/phase-1-repair-1-editor-tests-attempt-1.log`.

Expected 29 pass; actual 29 pass. XML/log and source snapshots retained. All first-party C# and assembly definition bytes match the isolated project's verified source snapshot. Approved input provenance and preexisting dirty-file hashes rechecked after verification. Source whitespace check passes. No Runtime or gameplay code changed in this repair; previous dedicated runtime/ship results are historical at 826b212, not represented as rerun. Native production art and player build remain later-phase checks. Independent re-review pending.

Repair commit: `e3b7870be2a9fee2ad111687704fafd5e8c58045`.
