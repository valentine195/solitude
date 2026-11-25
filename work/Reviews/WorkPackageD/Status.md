# Work Package D — regression verification

Regression additions and verification are finished. **Integrated acceptance is
blocked: the saved workspace's PlayMode suite is not green.** No runtime fixes,
scene restoration, assertion removal, or exception suppression were performed.

## Changes

- `Assets/_Project/Tests/Editor/ValidationTests.cs`: six authored-scene cases
  validate composition, check every GameObject for missing MonoBehaviour scripts,
  and assert validation does not change scene dirtiness. Tests include composed
  Wakeup/demo/reference scenes and both standalone SolitudeStart scenes.
- `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs`: verifies
  actual task-source disable/enable cycles and destruction while the scene scope
  stays alive. Listener counts return to their prior value, publisher availability
  clears, further focus events cannot update the released record, and repeated
  scene release removes the record and disposes delegates/focus.

These are seven new cases in existing suites. Both existing metadata files are
retained. Runtime code, Domain/Application boundaries, scene references, input,
and disposable demo persistence are unchanged by D. Concurrent task-notice
changes and the user's saved scene were preserved; no staging or commits were
performed.

## Results

| Check | Result / evidence under `Logs/WorkPackageD` |
| --- | --- |
| Domain/Application pure suite | **90 passed, 0 failed**; `pure-tests.runner.log`. Two existing unused test-event warnings. |
| Final Unity EditMode | **184 passed, 0 failed**; `editmode-final.xml`. Includes all six new scene/reference cases. |
| Final Unity PlayMode | **37 passed, 7 failed**; `playmode-final.xml`. New task-source destruction test passed. |
| Unity compilation | Live recompile completed with zero errors/warnings; final status up to date. Isolated test imports/compilation succeeded. `recompile.json`, `recompile-final.json`. |
| Authoring, metadata, assembly/manifest validation | Passed through `WakeupValidation.ValidateForCI`, which invokes the runtime composition validator and metadata checks. `validation/validation.json` has no findings. |
| Six gameplay/reference scene composition and missing-script checks | Passed as the six new EditMode cases; source scene validation in the live editor was unavailable because it was in Play mode. The failed live attempt is retained in `six-scene-validation.json`. |
| Scoped whitespace check | Passed for both changed test files. |

The initial repository gate also ran before the new D cases: 178 EditMode passed;
PlayMode reported 37 passed / 6 failed. It stopped at PlayMode. The final run adds
one passing PlayMode case; the known container destruction exception also occurs
in another notice-reload test, producing seven failures. Reports are retained
separately; there is no claim that rerunning produced a clean gate.

## Blocking failures

1. `ScrubberEncounterLifecycleTests.ReloadResetsEveryStateTogetherAndReleasesOldEndpoints`:
   the saved scene reloads its fuse pickup inactive, failing the expected reset
   activity assertion.
2. `ScrubberEncounterSliceTests.FusePickupRejectsFullInventoryThenCollectsExactlyOnceThroughInput`:
   the inactive pickup cannot become the player's focused target.
3. `TaskJournalSliceTests.ActualDemoDiscoversRepairsPausesCompletesAndResets` and
   `TaskJournalNoticeTests.ActualFocusAndRepairProduceSingleNoticesWithoutFocusTheft`:
   repair/completion expectations fail in the saved inactive-pickup scene.
4. `ScrubberEncounterSliceTests.CompletionNotificationDefersExpiresAndNeverReplays`:
   the concurrent new notice path never shows the expected completion notification.
   This test grants/installs/restarts without discovering the journal task first;
   the new notice coordinator observes accepted journal changes. This is a
   contract mismatch to resolve with that feature, not evidence of interaction
   scope dispatch failing. The test and current feature behavior were retained.
5. `ScrubberEncounterLifecycleTests.ScrubberSessionRejectsProductionWithoutWritingEncounterItems`
   and `TaskJournalNoticeTests.FailedFixtureCoalescesAndSceneReleaseDropsPendingLane`:
   scene destruction logs a `MissingReferenceException` from
   `ContainerView.Build`, via a still-subscribed presenter during container
   unregistration. The same underlying destroyed-slot reload exception was
   reproduced with pre-B code during B verification. Its occurrence varies by
   teardown order; it was not hidden or treated as a passing test.

The saved source scene was not replaced with original authoring to obtain a
passing verdict. B's earlier 40-pass result against original authoring is recorded
in the B report and is not substituted for this current integrated result.

## Required coverage

| Requirement | Executed evidence / remaining limit |
| --- | --- |
| 1. Repeat binding/release and partial failure | `ScrubberTaskSourceTests.FeatureRebindActivationAndRepeatedReleaseLeaveNoDuplicateEndpoints`, `FeaturePartialEquipmentFailureUnwindsAndCanBindAgain`, `ScrubberEncounterLifecycleTests.LaterFeatureFailureUnwindsAllBoundFeaturesAndSceneLeases`: passed. |
| 2. Load/unload, stale leases/publishers/views | Production transition, additive unload, destroyed-view/input, and interaction disposal tests passed. Demo reset/re-entry acceptance remains blocked by the saved scene and container teardown failures. |
| 3. Journal → feature → container Back | `BackRoutesJournalThenFeatureThenContainerAndActivationKeepsSingleBindings`: passed. |
| 4. Inactive equipment/UI and late enable | Feature rebind/activation and interaction inactive-bind/late-enable tests passed, including initially inactive feedback roots and receivers. |
| 5. Save-session isolation and reload/reset | Production collection/reload/new-save and production rejection of the demo passed. Demo reset and the reverse rejection test are blocked as listed above. |
| 6. Independent event sources | `InteractionScopeTests.IndependentEventScopesNeverShareFocusOrTriggerDispatch` and `TwoSceneScopesBindIndependentPlayersAndPromptViews`: passed. |
| 7. Once-per-event listeners after cycles | UI/task-source rebinding tests, actual UI destruction, and the new task-source destruction test passed. |
| 8. Invalid feature endpoint validation | All missing/null/duplicate/wrong-scene/incomplete feature authoring cases passed, plus the six new authored-scene cases. |
| 9. Inventory/task journal regressions | Pure rules/transactions/journal tests and EditMode suites passed. Integrated demo journal/notice failures remain explicitly listed. |

## Reproduction and scope

Started with `python3 Tools/Verification/verify.py --workspace
/private/tmp/solitude-work-package-d --output
/Users/jeremyvalentine/SOLITUDE/Logs/WorkPackageD`. The dedicated project owns its
metadata-only Git index; it never points at the source repository's index.
The runner copies Assets/Packages/ProjectSettings, runs the same pure sources,
validates authoring, prepares fixtures through Unity Editor APIs, and uses
disposable save directories.

After adding the D cases, copied only the two test files into the isolated
project, then ran full `unity test` suites with `--filter SOLITUDE.Tests`,
`--mode EditMode` / `PlayMode`, `--timeout 600`, explicit XML/log outputs,
`-nographics`, and a disposable `-solitudeSaveDirectory` for PlayMode.
Final machine-readable results: `consolidation-results.json`; original gate
failure: `results.json`; baseline dirty state: `start-status.txt`.

Final first-party source comparison matches the tested snapshot except the
demo camera viewport values rewritten by the existing editor migration test
in the disposable copy. Source scene values remain untouched; this difference
is recorded in `final-source-snapshot-differences.json`. No unrelated source
or serialized values were restored.

Standalone builds, native player persistence/fault scenarios, and manual
visual/audio review were **not run**: the complete repository gate stops on
PlayMode failure. These are not claimed as passed. Resolving the saved demo
authoring, container teardown error, and concurrent notice contract is required
before final integrated acceptance and a clean full gate.

The next ship system can add a feature-owned composition and scene reference
without adding its specific equipment/screens/tasks/cleanup to `SceneBindings`.
The generic feature list and typed context already support this; the reported
test blockers do not require a service locator or new assembly layout.

## Test repair follow-up

The failures recorded above are resolved by the subsequent authorized test repair. The complete gate now passes: 90 pure tests, 184 EditMode tests, 45 PlayMode tests, both macOS ARM64 builds, and all 28 native persistence/recovery scenarios. The follow-up restored the demo pickup/UI canvas active authoring, hardened never-active controller presenter cleanup, and corrected notice test discovery setup. See `work/Reviews/TestRepairs/Status.md` and `Logs/TestRepairs/final-gate/results.json`. The original findings remain above as historical evidence. Manual visual/audio review remains outstanding.
