## Phase 1: Create and rebuild a room

### Dependencies

- Depends on: none
- Why: Uses the current project's Unity/Tilemap and test infrastructure, not output from another implementation phase. Its exit is a usable create/manual-Rebuild slice with room ownership, generated visuals in temporary fixtures, error isolation, persistence, and runtime-mask exclusion. The approved spec and selected approach remain prerequisites.

### Step 1: Create an explicitly owned room with a reusable style

- [x] Status marker
- Criteria: S1, S2, S3, S4, S18, S20, S21 — establish serialized room/style ownership and an editor creation entry point without modifying existing rooms.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/ShipRoomAuthoring.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/RoomStyleDefinition.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/RoomGeneratedOutput.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/SOLITUDE.Editor.asmref`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringCreation.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomAuthoringCreationTests.cs`
  - Corresponding new Unity metadata, including folders.
- Changes: Add room ID, style reference, dedicated serialized occupancy Tilemap, and explicit generated-output ownership. Supply reusable style data for floor, north modules, directional caps, corners, and structural alignment. Add a creation command and a minimal Inspector showing authoring versus output. Creation affects only a new room in an explicitly chosen editable scene. Keep feature geometry/data in the existing Runtime assembly and Editor code in the existing Editor assembly; do not add Domain/Application dependencies or speculative assemblies. Detect ambiguous identity/ownership rather than silently merging rooms. Establish unit Grid cells and a safe authoring-mask state, with lifecycle verification completed in Step 4.
- Tests: S1/S2 — create two empty adjacent room fixtures sharing a style and assert distinct IDs and ownership; changing style does not change their IDs/cells. S3/S4 — generated children and occupancy are distinct, Grid scale/cells are native, and visual dimensions remain style data. S18/S20 — compile against the existing assemblies without UnityEditor leakage; verify pre-existing sentinel objects/scene references are unchanged. Missing production sprites are expected configuration diagnostics, not fabricated art. Record results for S21.
- Recovery: This establishes only new feature types and explicit scene creation. Revert only this step's new files/owned fixture objects; never reset a dirty production scene. Preserve any user-authored room data if a later revision requires changing the schema.
- Risk: Medium — room identity, ownership, and serialization are persistent contracts. Keep unsupported hierarchy/transform states explicit rather than repairing unrelated objects automatically.

### Step 2: Rebuild an 8×6 rectangle through the Inspector

- [x] Status marker
- Criteria: S3, S4, S6, S7, S8, S12, S18, S21 — a manual end-to-end Rebuild from serialized cells, with deterministic geometry and visible owned output in test fixtures.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomGeometry.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomBoundaryExtractor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Generation/RoomPlacementBuilder.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomGeometryTests.cs`
  - `Assets/_Project/Tests/Editor/RoomPlacementTests.cs`
  - `Assets/_Project/Tests/Editor/RoomRebuildTests.cs`
  - `Assets/_Project/Tests/Editor/RoomAuthoringTestFixtures.cs`
  - Corresponding new Unity metadata.
- Changes: Compute cardinal edges, ordered runs, and convex corners from arbitrary integer cell sets without scene rendering. Resolve the supported rectangular output with one configured structural repeat width, explicit corner reservations and offsets, and deterministic variant selection. Add Inspector Rebuild and apply a complete placement description to a generated floor Tilemap and owned architectural SpriteRenderers. Preserve authored occupancy; delete/recreate only identified output. Build a temporary 8×6 fixture, not a committed placeholder Standard style. Include the safety checks needed to refuse unsupported states from the first Rebuild; Step 3 completes the diagnostic matrix.
- Tests: S6 — shuffled 48-cell inputs produce the same 8/8/6/6 perimeter, four convex corners, and no internal edges; cover an exterior-cell removal independently of rectangular rendering. S7 — temporary native floor fixtures yield 48 aligned floor cells. S8 — a zero-reservation eight-cell run yields four two-cell modules or eight one-cell modules; nondivisible spans fail without scaling. Include configured corner reservations rather than deriving them from alpha bounds. S3/S12 — delete output and rebuild twice; compare sorted asset/placement/count descriptions and unchanged occupancy. S4/S18 — unit scale, 32 PPU fixture metadata, northward faces and distinct floor footprint; no Play Mode requirement. Fixture success does not establish S9 or S23.
- Recovery: Rebuild descriptions and output are disposable; authored cells and style data are preserved. If placement cannot be resolved, stop writes and report it; do not commit a partially successful rendering. Resume from the last passing geometry or placement check without touching existing builders.
- Risk: High — world anchors, corner reservations, and sprite bounds can diverge. Use explicit fixture geometry; real Standard offsets remain unknown until Phase 3.

### Step 3: Validate failures and isolate destructive cleanup

- [x] Status marker
- Criteria: S2, S11, S13, S14, S17, S20, S21 — nonmutating validation and safe invalid/empty generation behavior.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringValidator.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomValidationTests.cs`
  - `Assets/_Project/Tests/Editor/RoomOwnershipTests.cs`
  - `Assets/_Project/Tests/Editor/RoomRebuildTests.cs`
  - Corresponding new Unity metadata.
- Changes: Validate required style roles, dimensions/density/filtering, structural coverage, supported rectangles, target-scene room overlaps, and ambiguous identities/output references. Add Validate Room and visible generation diagnostics. Validation alone does not mutate objects or dirtiness. Invalid style/shape clears only unambiguous owned output and retains occupancy; empty occupancy produces empty output. Ambiguous ownership refuses destructive writes and visibly invalidates the preview. Valid Rebuild recovers after the issue is fixed. Bound room overlap inspection to the relevant scene/authoring operation rather than runtime or repeated whole-project searches.
- Tests: S11 — remove an exterior cell to 47 cells and assert updated geometry plus invalid rectangular output with no stale owned visuals; clear all cells and assert no output. S13 — missing wall/corner, invalid PPU/filtering, and impossible spans yield specific diagnostics with occupancy intact, no sprite substitution, and successful recovery after correction. S2/S14 — duplicate IDs, foreign output references, overlapping rooms, and unrelated same-name sentinels never authorize deletion of foreign content. S17 — Validate leaves serialized state and scene dirtiness unchanged. S20 — no original art/import or production scene writes.
- Recovery: Failure-state operations must remain restricted to verified ownership. If ownership is uncertain, stop rather than clearing content. Revert only this step's logic after retaining authored masks and diagnostic fixtures; leave unrelated scenes and objects unchanged.
- Risk: High — destructive cleanup and ownership ambiguity. Test hostile same-name and foreign-reference cases, not just a fresh single-room scene.

### Step 4: Persist a rebuilt room and exclude authoring data from runtime floor queries

- [x] Status marker
- Criteria: S16, S18, S19, S20, S21 — complete the manual-Rebuild slice across scene reload and Play Mode/build lifecycle.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/ShipRoomAuthoring.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringEditLifecycle.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringCreation.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Tests/Editor/RoomPersistenceTests.cs`
  - `Assets/_Project/Tests/Editor/RoomAuthoringLifecycleTests.cs`
  - `Assets/_Project/Tests/PlayMode/RoomAuthoringFloorCompatibilityTests.cs`
  - Existing `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs` as read-only regression coverage unless a narrowly scoped test extension is necessary.
  - Corresponding new Unity metadata.
- Changes: Qualify a feature-local occupancy lifecycle that preserves the saved Tilemap and editor painting target while preventing its active noncolliding tiles from participating in runtime floor queries. Merely hiding the renderer is insufficient. Verify scene save/open, entering/exiting Play Mode, reload configuration, and editor target lifetime. Do not alter SurvivorAvatarAdapter, SceneBindings, production collision, or floor-query semantics to solve this. Mark only actual room writes dirty, and persist all asset references rather than temporary scene-only references. Keep editor control out of player assemblies. If the installed brush/scene APIs cannot support the selected lifecycle, stop for reassessment before Phase 2.
- Tests: S16 — save/reopen a temporary rebuilt scene with persisted fixture assets; assert stable room/style GUID references, exact cells/placements, and equivalent Rebuild. S18 — Edit Mode manual Rebuild works and runtime never regenerates. S19 — exercise the production awakening/landing validation path in an isolated fixture: visible-floor positions remain valid; mask-only and architecture-only positions do not become valid floor; same-scene solid blockers remain effective. Include entry to Play Mode with reload behavior supported by the project. S20 — preserve other loaded scenes/dirtiness and existing GUIDs. These tests qualify lifecycle behavior, not real-art seams.
- Recovery: Remove only disposable fixture scenes/assets and restore the prior active/loaded scene state. If lifecycle checks fail, leave the authoring room explicitly invalid for runtime use and stop rather than installing a gameplay workaround.
- Risk: High — inactive/authoring-only targets, serialization, and runtime startup ordering interact. This phase is not complete until the mask exclusion has an executable check through the real floor-query behavior.
