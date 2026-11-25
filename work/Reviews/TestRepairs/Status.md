# Test repair follow-up

## Repairs

- Restored the authored demo fuse and UICanvas to active. Both edits were saved through Unity Editor APIs in the isolated verification project. Each migration was checked for an exact single active-state change before copying back; camera viewport and all other serialized values were preserved.
- Strengthened the demo authoring test to require the pickup and interaction UI to be available on direct entry/reset.
- SceneBindings now unbinds a retained controller’s managed presenter even when Unity considers its native object destroyed. Never-active controllers receive no OnDestroy callback; leaving that presenter subscribed caused owner teardown to build a destroyed view. The new PlayMode regression failed before this repair and passed afterwards; it verifies presenter removal, unavailable reader, and idempotent release.
- Updated the deferred completion-notice test to discover the scrubber through accepted access interaction before completing it, matching the current journal notice contract. Deferral, one notice, expiry, selected control, and no replay assertions remain.
- Added diagnostic feedback assertion output without relaxing pickup behavior assertions.

## Boundaries and preservation

No persistence implementation, Domain/Application source or assemblies changed. Disposable-demo save isolation remains tested with its sentinel file. Existing concurrent task-notice implementation and other working-tree changes were preserved. No staging or commits. Existing metadata for all five repaired assets is byte-identical to HEAD.

## Evidence

`Logs/TestRepairs/never-active-before.xml` reproduces the destroyed-view failure; `never-active-after.xml` passes with the cleanup repair. `full-gate` records the intermediate 44/45 PlayMode run that exposed the inactive UI canvas. `pickup-hide.log` shows input and interaction delivery reached the feedback service but its inactive parent kept the view inactive.

## Validation

- Pure tests: 90 passed, 0 failed.
- EditMode: 184 passed, 0 failed, 0 skipped.
- PlayMode: 45 passed, 0 failed, 0 skipped.
- Authoring validation: passed.
- macOS ARM64 verification and shipping builds: passed.
- All 28 native player scenarios: passed, including deliberate interruption/restart checkpoints, corrupted/missing saves, rejected incompatible data, and production collection/reset.
- Complete repository gate: **Passed**, `Logs/TestRepairs/final-gate/results.json`.

The full gate ran with `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-work-package-d --output /Users/jeremyvalentine/SOLITUDE/Logs/TestRepairs/final-gate` on Unity 6000.6.4f1. Intentional crash scenarios use expected nonzero exits and then validate committed snapshots and recovery on restart.

All first-party source files match the tested isolated snapshot except the demo camera viewport recalculated by PixelPerfectCamera when the existing authoring migration test saves the disposable copy. Only viewport y/height differ; the source camera remains unchanged. Evidence: `source-snapshot-differences.json` and `test-authored-scene-difference.txt`.

Manual visual/audio review has not been performed. The user's open Editor and save files were not used for the gate.
