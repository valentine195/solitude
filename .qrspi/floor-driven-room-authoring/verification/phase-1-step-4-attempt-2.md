# Phase 1 Step 4 — attempt 2

Blocked: 24/25 Editor checks pass; persistence fixture fails. No Step 4 commit or staging.
Spec `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`; normalized plan set `839de1dd66a4b31b5640c91285eba9406214aeba0de3c9180eec1f74a17588fe`.

Human-approved Unity type qualifications applied. Compilation succeeds. Command is prior attempt's isolated EditMode command with attempt-2 XML/log filenames and -solitudeRoomAuthoringVerification. Source snapshot matches isolated C# source.

S18: both NormalReloadKeepsAuthoredMaskAndGeneratedOutput and DisabledReloadKeepsAuthoredMaskAndGeneratedOutput pass, with inactive mask, 48 retained cells/stable ID, 28 unchanged sprite entity IDs across runtime frames, preserved editor selection target and successful post-exit rebuild. BuildMaskRemoval test passes, retaining generated output. These do not establish a built-player result.

S16/S20: SaveReopenPreservesReferencesCellsAndEquivalentRebuildWithoutDirtyingOtherScenes fails at RoomPersistenceTests.cs:72 before scene creation: SceneManager.CreateScene is runtime-only; Unity directs EditorSceneManager.NewScene in Edit Mode. Proposed test-only correction: create disposable scenes through EditorSceneManager, using an explicitly isolated test fixture with a saved baseline/sentinel scene so additive creation is valid even when the runner initially has an untitled scene. No production-scene writes or gameplay change. Clean saved-scene dirtiness/persistence assertions not executed.

All previous 21 focused tests pass on this snapshot. S19 dedicated PlayMode safe-point/landing test not run because this verification attempt failed. S21 retains NUnit XML, log and source SHA-256 snapshot. Unity exit 2, CLI TESTS_FAILED exit 8. Approval provenance and preexisting modified-file hashes verified. Await human decision under interactive Implement failure gate.
