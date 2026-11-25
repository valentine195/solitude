# Phase 1 Step 4 — attempt 3

PASS: 25 Editor + 1 focused PlayMode + 4 existing ship-compatibility tests; none failed/skipped. Commit `826b212`.

Spec SHA-256 `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`; normalized plan set `839de1dd66a4b31b5640c91285eba9406214aeba0de3c9180eec1f74a17588fe`.

## Reproducible verification

All commands target `/private/tmp/solitude-room-authoring-phase1` using Unity 6000.6.4f1. Main and isolated first-party C#, asmdef/asmref, package manifests and Editor version files match; digests retained in `phase-1-step-4-verified-project-source-attempt-3.json`. Room-only source snapshot before verification is `phase-1-step-4-source-attempt-3.json`. XML and logs are retained under unique names below.

- `unity test <isolated-project> --mode EditMode --filter SOLITUDE.Tests.RoomAuthoring --output <verification>/phase-1-step-4-editor-tests-attempt-3.xml --timeout 300 --format json -- -nographics -solitudeRoomAuthoringVerification -logFile <verification>/phase-1-step-4-editor-tests-attempt-3.log` — expected 25 pass; actual 25 pass.
- `unity test <isolated-project> --mode PlayMode --filter SOLITUDE.Tests.RoomAuthoring.RoomAuthoringFloorCompatibilityTests --output <verification>/phase-1-step-4-floor-tests-attempt-3.xml --timeout 300 --format json -- -nographics -logFile <verification>/phase-1-step-4-floor-tests-attempt-3.log` — expected 1 pass; actual 1 pass.
- `unity test <isolated-project> --mode PlayMode --filter SOLITUDE.Tests.SurvivorSuccessionShipCompatibilityTests --output <verification>/phase-1-step-4-ship-tests-attempt-3.xml --timeout 300 --format json -- -nographics -solitudeSaveDirectory <verification>/phase-1-step-4-ship-saves-attempt-3 -logFile <verification>/phase-1-step-4-ship-tests-attempt-3.log` — expected 4 pass; actual 4 pass. Save directory is disposable and separate from user saves.

## Criterion-linked outcomes

- S16: saved and reopened temporary scene retains room identity, persistent style GUID, exact authored cells, 48 floor cells and equal sorted sprite asset/placement/count descriptions. Rebuild after reopen is equivalent.
- S17/S20: validation preserves a clean saved scene; separately loaded saved baseline scene and sentinel position/reference/dirtiness are unchanged by Rebuild/save/reopen.
- S18: normal domain/scene reload and both reloads disabled enter/exit Play Mode successfully; mask inactive, identity and 48 authored cells preserved; 28 generated sprite identities remain unchanged across runtime frames (no regeneration). Restored occupancy can be selected and manually rebuilt. Build-scene mask stripping retains generated output. This is a processor path check, not a completed player build.
- S19: public production ValidateResumePoint/Awaken accept visible floor and reject mask-only/architecture-only locations; actual production CanLandAt is exercised via reflection with live Physics2D and retains solid-blocker/trigger semantics. Its fixture uses the generated hierarchy/component shape without invoking Editor code at runtime. Existing ship tests additionally pass actual awakening, inventory/hotbar succession and recovery, locker/pickup interactions, powered door traversal, restoration, scrubber continuity and scene reload.
- S20: gameplay adapters/composition/collision semantics unchanged. All preexisting tracked modified-file hashes and approved Spec/Plan inputs rechecked after tests. Source staged whitespace check passed. New metadata imported by isolated Unity was copied only where absent, preserving existing GUIDs.
- S21: exact commands, XML/logs, source digests and commit recorded. No skipped checks claimed as passing.

Human-approved persistence fixture correction uses EditorSceneManager with an isolated saved baseline scene and additive new test scene, then reopens through OpenPreviewScene. Explicit runner flag prevents this fixture from replacing scenes in an ordinary test run without isolated-runner intent. Disposable fixture scene/assets are removed by test cleanup.

## Limits

Prepared Standard artwork, native-pixel rendered seams (S9/S23), supplied palette/brush transactions (Phase 2), and actual player-build/full verification gate (Phase 3) remain pending. No new production scene, prefab, art/import, or shipping build entry was created. Interactive main Editor UI appearance was not visually checked; these runs use an isolated project. Earlier main live test was inconclusive and is not used as passing evidence.
