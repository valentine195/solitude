## Phase 2: Paint and edit rooms

### Dependencies

- Depends on: 1
- Why: Requires Phase 1's serialized room/style ownership, deterministic room-scoped Rebuild, nonmutating validation, safe cleanup, and verified authoring-mask lifecycle. This phase ends with the actual palette/brush workflow, batch editing, Undo/Redo, and recovery; it does not wait for production artwork.

### Step 1: Paint and erase occupancy with a reusable room palette

- [x] Status marker
- Criteria: S3, S5, S7, S18, S20, S21 — supported room-target painting and immediate completed-edit generation.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrush.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringPaletteSetup.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Editor/SOLITUDE.Editor.asmdef`
  - `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/Brushes/ShipRoomBrush.asset`
  - `Assets/_Project/Art/Tiles/Palettes/RoomAuthoring/RoomOccupancy.prefab`
  - `Assets/_Project/Art/Tiles/Palettes/RoomAuthoring/RoomOccupancy.asset`
  - `Assets/_Project/Tests/Editor/RoomBrushTests.cs`
  - Corresponding new Unity metadata.
- Changes: Supply a room-specific GridBrushBase integration, room-only valid target selection, an identifiable palette/occupancy marker, and standard brush preview behavior without a bespoke editor window. The occupancy marker is authoring data, not substitute room artwork. Paint/Erase modify only the selected authored map, then call the existing operation boundary for completed-edit output. Bind authoring visibility/selection to the qualified mask lifecycle. Reject generated and unrelated Tilemap targets. Setup may create missing palette/brush assets but must preserve existing palette layouts and painted scene cells on rerun. Add an explicit Unity.2D.Tilemap.Editor reference to the existing Editor and EditMode test assemblies where required by brush/editor API usage; the installed GridBrushEditorBase is defined in that Editor-only package assembly. Do not add that dependency to Runtime or change assembly ownership/package versions.
- Tests: S5 — exercise production brush callbacks and a manual Tile Palette selection/paint/erase smoke check in an isolated fixture, including a target using the qualified authoring lifecycle. Supported cells update ownership/floor output after completion; an intermediate unsupported shape gives its diagnostic. S3/S7 — generated floors do not become authoring inputs and accepted output exactly matches occupied cells. S18/S20 — no Play Mode requirement, runtime-mask regression still passes, invalid non-room/generated targets are unchanged, and repeated setup does not erase a manually adjusted palette.
- Recovery: Palette/brush setup is additive and idempotent. Revert only the new brush integration while retaining Phase 1's Inspector Rebuild and authored room data; do not reset existing shared palettes or scenes.
- Risk: Medium — valid-target filtering and mask visibility must work with the actual installed Tile Palette UI, not just direct method calls. Follow the Unity editor/UI guidance before implementing custom Inspector/brush UI.

### Step 2: Batch Box Fill and Box Erase as complete room edits

- [x] Status marker
- Criteria: S5, S10, S11, S12, S21 — rectangle editing and expansion/erasure produce one final generation result per box operation.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrush.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushBatchTests.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushTests.cs`
  - Corresponding new Unity metadata.
- Changes: Implement batched Box Fill/Erase through the installed brush operation boundary. Resolve/write the final room once per completed box edit, not once per changed cell; preserve deterministic result ordering and allow recovery from temporary unsupported painting states. Keep full-room regeneration scoped to the selected room. Do not make observer callback coalescing or a runtime loop a dependency.
- Tests: S5 — an 8×6 Box Fill authors 48 cells and emits one completed generation application; verify both callback integration and a Tile Palette smoke check. S10 — add four eastern columns to produce a 12×6/72-cell room, 12/12/6/6 perimeter, moved east corners, and no old east wall. S11 — erase a whole outer column to a valid or width-invalid rectangle according to fixture configuration, then box-erase to empty; verify exact diagnostic/cleanup semantics. S12 — repainting an unchanged region gives equivalent output without duplicate visuals. Use an operation boundary observation for batching, not a test duplicating the loop implementation.
- Recovery: Revert batching integration without changing room serialization. Retain the original occupancy from the failed operation's fixture and diagnose before retrying; do not compensate by reverting other rooms or silently truncating the box bounds.
- Risk: Medium — brush bounds and gesture completion can differ from individual cells. Test actual installed callback behavior before claiming once-per-operation batching.

### Step 3: Restore complete edits through Undo and Redo

- [x] Status marker
- Criteria: S3, S15, S16, S21 — occupancy, owned hierarchy, and generated tiles restore together.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrush.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomUndoRedoTests.cs`
  - `Assets/_Project/Tests/Editor/RoomPersistenceTests.cs`
  - Corresponding new Unity metadata.
- Changes: Group each supported completed edit with all authored and generated mutations. Record Tilemap changes, object creation/destruction, reference changes and failure cleanup so Undo/Redo restores the recorded state rather than performing an unrecorded destructive rebuild. Keep validation read-only and preserve normal scene dirtiness. Integrate with the installed pre-action brush Undo behavior without duplicate registrations/groups. No asynchronous observer rebuild may race restoration.
- Tests: S15 — for Paint, Erase, Box Fill, and Box Erase, compare pre-edit and post-edit occupancy and sorted output snapshots through Undo/Redo. Include 8×6 fill, expansion, empty deletion, and an exterior-cell removal that clears invalid output. Undo must restore its former valid walls; Redo must restore 47 authored cells plus the invalid/empty generated state. S3 — transient generated edits never alter occupancy. S16 — save/reopen after a restored valid state with no duplicate objects or dangling references. Verify a full user gesture in the Editor, not only internal action methods.
- Recovery: If transaction boundaries fail, stop before further painting; preserve the scene snapshot and owned authoring cells. Revert only the transaction changes and resume from the last verified operation. Never clear the user's global Undo history to hide failures.
- Risk: High — brush pre-action Undo, hierarchy destruction, serialized references, and invalid-state cleanup must form one coherent user action.

### Step 4: Verify resizing, erasure, and explicit recovery end to end

- [x] Status marker
- Criteria: S5, S10, S11, S12, S14, S17, S18, S21, S22 — finish a usable editing workflow and draft its guide.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrush.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushBatchTests.cs`
  - `Assets/_Project/Tests/Editor/RoomUndoRedoTests.cs`
  - `Assets/_Project/Tests/Editor/RoomEditingWorkflowTests.cs`
  - `Assets/_Project/Tests/Editor/RoomOwnershipTests.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/README.md`
  - `work/Reviews/RoomAuthoring/EditorWorkflow.md`
  - Corresponding metadata for new Unity-imported files only.
- Changes: Provide an explicit Fill/Erase box mode in the existing room-brush controls. In Erase mode, a native Box Fill gesture invokes the same batched erase operation; Paint/Erase cell tools remain independent. Keep native Shift BoxErase when delivered, but do not require it for the documented box-erasing workflow. Complete visible diagnostics and Validate/Rebuild recovery for external occupancy edits, missing/deleted generated objects, unsupported extents, and empty rooms. Reuse the existing generation contract; do not add a third-party tool observer. Write the guide's exact creation/selection/paint/box/erase/Undo/recovery steps and clearly label its fixture-based verification. Reusable setup remains additive; preserve manually painted layout across reopening and tool re-selection.
- Tests: Verify the explicit Fill/Erase selector through the native Tile Palette and a complete box-erasure gesture with Undo/Redo; extend batch and transaction regressions for this route, including unchanged other rooms and rejected generated targets. S5/S10/S11 — follow the guide to create 8×6, expand to 12×6, erase to unsupported, undo/redo, and empty/recover in a disposable scene. S12 — explicit Rebuild twice yields the same result. S14 — a second equal-style room and unrelated same-name sentinels remain unchanged throughout. S17 — direct occupancy modification followed by Validate changes no data/dirtiness; Rebuild restores correct current output. S18 — editing needs no runtime loop and runtime-mask tests still pass. S22 — guide smoke walkthrough is reproducible without hand-painting architecture. Record S21 evidence and unresolved production-art checks.
- Recovery: Resume from saved test snapshots or the last valid authored mask; production scenes are never the test target. Keep the guide honest about pending Standard artwork rather than marking the full feature done after fixture success.
- Risk: Medium — explicit recovery must not silently promise automatic synchronization for arbitrary tools or mistake test fixtures for production acceptance.
