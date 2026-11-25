# Phase 1 Step 1 — verification attempt 2

Status: **Blocked: 4 passed, 1 failed.** No commit or completion marker.

Approved spec: `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`.
Approved normalized plan set: `839de1dd66a4b31b5640c91285eba9406214aeba0de3c9180eec1f74a17588fe`.
Source snapshot: `phase-1-step-1-source-attempt-2.json`.

The human approved the test-only preview-scene correction. NewPreviewScene and
ClosePreviewScene replaced additive scene setup/cleanup; production source was
unchanged. Tests reran in `/private/tmp/solitude-room-authoring-phase1` using the
same focused filter and isolated batch-mode command, with output filenames
`phase-1-step-1-tests-attempt-2.xml` and `.log`.

## Criterion evidence

- S1/S2: adjacent equal-style rooms retain distinct identities, occupancy and output — Passed.
- S2/S14 supporting ownership check: duplicate identities and foreign output references are diagnosed without repair — Passed.
- S3: changing style/deleting output leaves authored cells/identity intact — Passed.
- S4/S18 supporting creation check: native Grid and separate inactive EditorOnly occupancy mask — Passed.
- S18/S20 combined test: existing-object preservation assertions execute successfully, but the final Runtime assembly dependency assertion fails — Failed.
- S21: exact test XML and source snapshot retained. No step completion or full criterion conformance claimed.

Failure at RoomAuthoringCreationTests.cs:106: Expected False, But was True,
for the assertion that the **Editor-compiled** SOLITUDE.Runtime assembly has no
UnityEditor reference. This assertion is too broad for an Editor build: existing
ItemDefinition.cs:35-47, ItemDatabase.cs:99-120, and SaveableId.cs:32-45 contain
legitimate UNITY_EDITOR-guarded editor helpers. New RoomAuthoring runtime files
contain no UnityEditor code. Existing sources have not been changed.

## Proposed correction

Check the player compilation graph (CompilationPipeline.GetAssemblies with
AssembliesType.Player) for runtime assembly inclusion and absence of UnityEditor
references, rather than rejecting legal Editor-only code in the Editor build.
Retain the existing-object assertions and all other focused checks. This is a
test-only correction, not a gameplay or assembly-boundary scope change.

The same test-only fix/reverification requires a human decision under the current
interactive implementation skill. No source generation/placement work starts yet.
