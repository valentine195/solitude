# Phase 1 Step 5 — attempt 1

Status: BLOCKED; source remains uncommitted. Last verified commit c88a7ab09b337c36803ad174565143d5ee9af62e (Step 4).
Spec SHA-256: 50fa6addebd57b43f7617b1a266a6ba13f17e6b1f7e05c55b73df48fe0af35a9
Approved amended phase normalized SHA-256: 64225dbf598968e3d75c39ded978e64f62699ed48fcf0f144d03977883e40096
Criteria: S1, S5–S8, S10–S14, S16–S17.

## Changes and authoring

Authored TaskJournal/TaskJournalRow prefabs through Unity Editor API, with serialized controls, independent scroll regions, literal TMP text, actual input labels, separate condition/selection styling. SceneBindings initializes/releases optional view and degrades failed authoring without blocking gameplay. Scrubber demo and its owning/incremental builder now preserve explicit source identity and journal registration. Existing GUIDs preserved during authoring. Source snapshot below includes only feature-owned Assets changes; unrelated sprite audits excluded.

## Verification

Unity compilation completed, failed=false, errors=[]. Initial prefab eval timed out; output existed and idempotent retry returned success. No manual prefab/scene YAML editing.

Focused authoring command: `unity test /private/tmp/solitude-task-journal-p1-s4 --mode EditMode --filter SOLITUDE.Tests.ScrubberEncounterAuthoringTests --output /private/tmp/task-journal-p1-s5-authoring-attempt2.xml --timeout 300 --json -- -nographics -logFile /private/tmp/task-journal-p1-s5-authoring-attempt2.log -solitudeSaveDirectory /private/tmp/task-journal-p1-s5-authoring-saves2`
Actual 2/2 passed. Repeated AddTaskJournal, prefab references, scroll regions, literal text, one EventSystem, source identity and stable scene GUID checked (S12/S13/S16/S17). Earlier authoring attempt also 2/2 passed.

Earlier actual-demo journey and unavailable/failed fixture: `/private/tmp/task-journal-p1-s5-journey-attempt1.xml` passed 2/2. Sandbox license startup was prevented; rerun with service access completed. This earlier candidate lacked the subsequently added footer assertion and final layout refinements, so its result does not qualify the final candidate fully.

Final focused command: `unity test /private/tmp/solitude-task-journal-p1-s4 --mode PlayMode --filter SOLITUDE.Tests.TaskJournalSliceTests --output /private/tmp/task-journal-p1-s5-journey-attempt2.xml --timeout 300 --json -- -nographics -logFile /private/tmp/task-journal-p1-s5-journey-attempt2.log -solitudeSaveDirectory /private/tmp/task-journal-p1-s5-journey-saves2`
Actual 1 passed, 1 failed. `ActualDemoDiscoversPinsRepairsPausesCompletesAndResets` failed at line 66: expected ESC, actual ESCAPE. Fixture hard-codes the abbreviated display name; headless Unity uses the full display name. Assertions before this check covered readiness, hidden initial state, empty J open/close, actual focus discovery and J pause. Subsequent journey assertions are unrun for this candidate, not passes. `UnavailableSourceAndFailedFixtureStayLiteralWhileViewTeardownReleasesPause` passed source detach/retained pin, failed literal text, view-disable pause release, reattach, and scope cleanup (S1/S10/S13/S14/S15).

S17 live visual inspection: Game View captures initially stale while Unity backgrounded; activating Unity produced current composited UI. Checked selected+pinned+fault, unpinned, empty at actual Game View sizes 1920x1080, 1280x720, 1024x768 (GameView targetRenderSize reflected actual requested viewport). Files `/private/tmp/task-journal-p1-s5-{empty,pinned,unpinned}-final-{1920x1080,1280x720,1024x768}.png`. Text/keycaps/control bounds readable, red fault separate from amber marker, responsive row bounds. Initial opacity/empty-label/row sizing findings refined through Editor APIs, not counted as final passes until captured again. Footer live shows ESC; headless actual label ESCAPE requires fixture correction and checking longer glyph fit.

Live actual demo normal input path also collected the fuse through E, accessed equipment, invoked its authored InstallFuse/Restart controls, let natural recovery finish, and opened the Completed tab (outcome COMPLETED, count 1). Completed captures exist `/private/tmp/task-journal-p1-s5-completed-final-{1920x1080,1280x720,1024x768}.png` but were not visually inspected before the failed required test stopped work. Failed/unavailable visual screenshots not completed. Full Step 5 gate NOT RUN. Step 5 NOT COMMITTED; Phase 1 remains 4/5 complete. Phase 2 not started; autoloop remains stopped.

## Proposed repair

Compare the footer against UnityInputHost.BindingLabel for the authored Cancel action instead of a hard-coded short name. Verify both ESC/ESCAPE and ensure longer actual binding text fits the keycap. Re-run affected focused tests, finish required visual checks and run full gate before committing. No source repair performed after the required final-candidate failure.

## Source snapshot

```json
{
  "Assets/_Project/Core/Composition/SceneBindings.cs": "3ebf94b734a003a5b742f57efd5db54a729e993e32148e5b238f3f49a3c318bb",
  "Assets/_Project/Editor/ScrubberEncounterBuilder.cs": "2dceae7e8b75a4f786d891e0c7652be1320d16e0649b98485c1c5b549da7390d",
  "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity": "64d16ee11edef81101cef9bfe20dd85e4ffd2483868ae53c603fdf0410aabe3a",
  "Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs": "1547b0e0b7c74c3d7603d68a12403581f28d5de6d13091802922bec2c4880977",
  "Assets/_Project/Systems/Tasks.meta": "809ae489b091a6f9fa8e2994613ee5bf5290f557ef7a2802af010d8e2bb78b3c",
  "Assets/_Project/Systems/Tasks/UI.meta": "e7932bea1843042c19b60322565dfe40e9e8118dfaaf7774d492e66f6e6cf015",
  "Assets/_Project/Systems/Tasks/UI/TaskJournal.prefab": "e8a1b2827d6608ec7117a579ca99d805d05d8312d6e4e7ea395a802e72edc897",
  "Assets/_Project/Systems/Tasks/UI/TaskJournal.prefab.meta": "06b44d4afe93f29e78a47056dfcd10707d7e23468ca6be398c37e097cf928b61",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRow.prefab": "ef9525938abb493f4d90f39266b9540059bb6e2e8d43a2826df7d41536744b94",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRow.prefab.meta": "d94bda8cea91570c208148305b251b54a258b52f38f297dafed6b4f6c3756fc9",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRowView.cs": "a103334028e4a8d83b440bde6a6009c1aafff729ffeae2d520d9208fbfc30752",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalRowView.cs.meta": "7037cdbd396d02356b028919e20a8d07c8d1700106eb0fe4546ad129fb5e6b09",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalView.cs": "e8ff357a285cd73938efb59bbf368f9a8a25580bb829c79dde255ae783cd70d1",
  "Assets/_Project/Systems/Tasks/UI/TaskJournalView.cs.meta": "b1013d15be1d7bc2397b056290d2308b35081643044cac0beb478f9346972ee5",
  "Assets/_Project/Tests/PlayMode/TaskJournalSliceTests.cs": "43f76bfc5a761e834818cac2f5247d269c709ef822e825dcb3300b943d60dcae",
  "Assets/_Project/Tests/PlayMode/TaskJournalSliceTests.cs.meta": "899f7e2744f0e3e632a8ed84cd158bbd8a50f373c18257550d39f85077ddc2ea"
}
```
