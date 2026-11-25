## Phase 2a: Follow irregular layouts with useful partial previews

### Dependencies

- Depends on: 1, 2
- Why: Directly consumes Phase 1's serialized room/style/output ownership, native placement and safe writer/lifecycle contracts, and Phase 2's working palette, batched brush, gesture Undo and recovery workflow. Existing completed phases are historical baselines; this phase revises their rectangle-only failure behavior. Prepared Standard artwork is not required for its synthetic geometry/editor verification.

### Step 1: Preserve valid output and report structured partial results

- [!] Status marker
- Criteria: S1–S8, S10–S18, S20, S21, S26, S28, S29 — establish partial results through the existing rectangle workflow before expanding geometry.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Generation/RoomGenerationResult.cs` — new result/status/located issue data, with no Editor dependency.
  - `Assets/_Project/Features/World/RoomAuthoring/Generation/RoomPlacementBuilder.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/ShipRoomAuthoring.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringValidator.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs` — minimal outcome/summary display; selectable guides follow in Step 3.
  - `Assets/_Project/Tests/Editor/RoomPartialPreviewTests.cs` — new.
  - `Assets/_Project/Tests/Editor/RoomAuthoringTestFixtures.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushTests.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushBatchTests.cs`
  - `Assets/_Project/Tests/Editor/RoomValidationTests.cs`
  - `Assets/_Project/Tests/Editor/RoomRebuildTests.cs`
  - `Assets/_Project/Tests/Editor/RoomOwnershipTests.cs`
  - `Assets/_Project/Tests/Editor/RoomUndoRedoTests.cs`
  - `Assets/_Project/Tests/Editor/RoomEditingWorkflowTests.cs`
  - New-file Unity metadata only; preserve existing GUIDs.
- Changes: Add deterministic Complete/Incomplete preview/Empty/Refused results, stable issue identities and actual region/field locations, and a compatible diagnostic summary. Assess floor and architectural dependencies independently; generate the current valid floor and independently resolved spans/joins instead of discarding everything after the first local error. Do not invent placements or fill an uncoverable run with an arbitrary prefix. Remove obsolete/invalid owned visuals and retain only current valid placements. Keep ownership, coordinate and overlap refusal ahead of unsafe writes; visibly identify retained output as not updated after refusal. Make Validate compute/report current assessment without serializing changes or claiming stale output is current Complete. Record completed-result state/issues with existing occupancy/output Undo transactions, and retain legacy scene/style references without destructive automatic migration. A legacy result lacking freshness data requires an honest assessment/Rebuild indication, not a fabricated current result. Updating the rectangle-only assertions is an intentional Spec revision, not removal of preservation checks.
- Tests: S7/S13/S26 — a valid rectangle with one missing required corner retains all floors and independent walls; missing floor permits valid architecture and located floor issue; missing style retains cells and records a room/field issue. S8 — a configured unfit run yields no arbitrary prefix and does not suppress independent runs. S12/S28 — repeated Rebuild yields equivalent placements/issues, correction clears only resolved issues, and unused missing roles do not invalidate unaffected output. S14 — ambiguous ownership/coordinates/overlap refuse before unsafe destruction and other-room/sentinel snapshots stay identical. S15/S29 — one missing-asset edit/correction Undo/Redo restores result, cells, output and issues coherently. S17 — Validate preserves complete serialized snapshot and a clean scene's dirty flag; external changes do not label old output current. Run the focused room Editor suite with its isolated-runner flag; update all affected legacy expectations within the listed tests. Existing nonrectangle placement rejection may remain until Step 2; document this intermediate limit without presenting the expanded phase as done.
- Recovery: Preserve failing synthetic input/output/result snapshots. Revert only this step's result/writer changes if necessary; keep occupancy, IDs, original art and unrelated scenes. Do not clear global Undo or strip pre-existing serialized data to obtain passing tests. This commit provides a usable partial rectangle preview and its result contract for later steps.
- Risk: High — early-return validation, output cleanup, serialization defaults and Undo must agree; partial output must not bypass room-wide safety guards.

### Step 2: Generate actual irregular boundaries and oriented joins

- [ ] Status marker
- Criteria: S1, S3, S4, S6–S16, S21, S24–S26, S28 — irregular geometry and placement through the real writer.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomGeometry.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomBoundaryExtractor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/RoomStyleDefinition.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Generation/RoomGenerationResult.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Generation/RoomPlacementBuilder.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringValidator.cs`
  - `Assets/_Project/Tests/Editor/RoomGeometryTests.cs`
  - `Assets/_Project/Tests/Editor/RoomPlacementTests.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushTests.cs`
  - `Assets/_Project/Tests/Editor/RoomValidationTests.cs`
  - `Assets/_Project/Tests/Editor/RoomUndoRedoTests.cs`
  - `Assets/_Project/Tests/Editor/RoomEditingWorkflowTests.cs`
  - `Assets/_Project/Tests/Editor/RoomAuthoringTestFixtures.cs`
  - `Assets/_Project/Tests/Editor/RoomPartialPreviewTests.cs`
  - `Assets/_Project/Tests/Editor/RoomIrregularLayoutTests.cs` — new, plus metadata.
- Changes: Extend exposed-edge geometry with oriented convex/concave junctions and actual run endpoint types; keep separate branches at diagonal contact. Replace bounding-box corner placement and rectangle-only acceptance with current boundaries, including holes and disconnected components under the same room identity. Add explicit directional concave asset/coverage/alignment and endpoint-reservation configuration while preserving original serialized rectangle fields/references. Only used roles and their dependent constraints affect room completeness. Retain one north repeat width per style, deterministic variant order, native scale and perspective on every boundary. Classify missing joins, short/uncoverable runs and declared incompatible visual-fit constraints as located partial issues; no generic nonrectangle error, clipping, guessed filler or silent rotation. Diagnose known tall-wall fit conflicts from explicit style/placement facts; fixture fit does not establish production pixel seams.
- Tests: S6/S24 — compare an explicit edge/junction oracle for rectangle, L/U, notch, hole and negative-coordinate inputs, including permuted/duplicate cell input. Full 8×6 has four convex corners; removing (7,5) gives five convex/one concave; removing (6,4) adds four hole sides/four concave joins. No same-room internal edge appears. S7/S11 — use production Rebuild on those 47-cell masks: floor coverage is exactly 47; compatible one-cell synthetic art resolves actual boundary positions and removes obsolete corners. S25 — bridge erasure preserves identity/components and diagonal contact keeps branches separate, with deterministic local issue when configured artwork cannot join. S8/S13/S26 — two-cell repeat with short run, missing concave role, invalid dependent reservation and declared narrow-void fit conflict retain independent floors/architecture and precisely scoped issues. Test all four concave orientations, existing rectangle compatibility, no scaling and no duplicate faces. Run focused Editor suite; numeric expectations prove geometry/placement only, not production art.
- Recovery: Preserve authored sets, role configuration and failing placement descriptions. Keep existing style fields/GUIDs; additive concave fields may remain unused by old rectangles. If a new geometry case exposes a policy outside the selected orthogonal/component contract, stop rather than add shared-room junctions or mixed-width solving. This commit exposes irregular Rebuild through the existing brush/writer/result path.
- Risk: High — corner orientation, endpoint dependencies, diagonal branches and north-face direction around holes can produce plausible but wrong output.

### Step 3: Show and select unresolved regions in the Inspector and Scene view

- [ ] Status marker
- Criteria: S5, S13, S17–S19, S21, S26–S28 — make partial state useful without modifying authored data through feedback.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomAuthoringEditor.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringPreview.cs` — new Editor-only scene guides, issue focus/highlight and transient assessment/selection.
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomPreviewFeedbackTests.cs` — new, plus metadata.
- Changes: Use the existing Inspector/Tile Palette with an explicit Incomplete preview status and selectable located issues. Draw red unresolved span/join guides and specific markers, with text/icon feedback in addition to color. Show all current authored cells with Editor-only guides when floor/style is missing; do not make the occupancy map a runtime floor candidate or create substitute production sprites. Issue selection frames/highlights its actual region or identifies the relevant room/style field. Limit guides/selection to the selected or edited room; dispose stale selection/caches on target change, reload or destruction. Validate's transient current assessment may update feedback without changing persisted generation state. Show stale/refused output honestly and retain the documented explicit Rebuild boundary after external asset/tool edits. Consult Unity UI routing, UI Toolkit and IMGUI guidance for the respective existing Inspector and Scene GUI surfaces; no bespoke editor window or broad UI framework.
- Tests: S27 — issue identity maps to expected world region/style field, selecting/framing it changes no cell, output, serialized status, dirty flag, Undo group or generation count. S17 — validate-only feedback on a clean saved fixture remains nonmutating. S26 — missing floor/style exposes every authored cell through guides and retains valid independent architecture. S18/S19 — guides have no runtime TilemapRenderer/Collider/floor contribution and disappear in Play/build contexts; target switch and room destruction do not retain another room's issues. S28 — correcting/rebuilding removes stale markers while unresolved others remain. Verify actual visible Inspector issue list, scene guide and click-to-focus once in a disposable Unity fixture; API-bound tests alone do not establish the native UX.
- Recovery: Remove transient guides/selection subscriptions and return to the Inspector summary without changing occupancy or generated architecture. Preserve a failed UI fixture snapshot before cleanup; never save/reload a dirty production scene for this check.
- Risk: Medium — read-only UI actions can accidentally dirty scenes or leak stale room references; scene feedback must remain editor-only and useful without relying on color alone.

### Step 4: Restore complete and incomplete edits across Undo, reopen and runtime boundaries

- [ ] Status marker
- Criteria: S2, S3, S5, S11–S21, S25, S26, S28, S29 — qualify the expanded editing workflow as coherent transactions.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/ShipRoomAuthoring.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringOperations.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringEditLifecycle.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrush.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/ShipRoomBrushEditor.cs`
  - `Assets/_Project/Tests/Editor/RoomUndoRedoTests.cs`
  - `Assets/_Project/Tests/Editor/RoomPersistenceTests.cs`
  - `Assets/_Project/Tests/Editor/RoomEditingWorkflowTests.cs`
  - `Assets/_Project/Tests/Editor/RoomBrushBatchTests.cs`
  - `Assets/_Project/Tests/Editor/RoomAuthoringLifecycleTests.cs`
  - `Assets/_Project/Tests/PlayMode/RoomAuthoringFloorCompatibilityTests.cs`
- Changes: Extend existing transaction/persistence/lifecycle handling only where expanded-state verification demonstrates a gap. Persist or consistently expose associated completed-result status/issues after reopening without destructive auto-generation; keep selected issue/highlight transient. Preserve old scenes/styles and inactive EditorOnly masks, including disabled reload settings. Continue exact one-application box batching and one gesture for a continuous stroke. No asynchronous observer or runtime regeneration repair.
- Tests: S15/S29 — snapshot occupancy, assets/placements/counts, status/issues across Paint/Erase/Box Fill/explicit Box Erase and complete→incomplete→corrected, split components, and nonempty→empty transitions. Undo/Redo must restore each full recorded state. S16 — save/reopen complete and partial fixtures with persistent synthetic assets; references, result/issues and output remain consistent, subsequent Rebuild idempotent, another scene/sentinel unchanged, no generation triggered by reopen/selection. S5 — observe one generation application per box. S18/S19 — isolated PlayMode floor test accepts an occupied floor cell, rejects hole/mask-only/architecture-only/guide positions, preserves blocker/trigger rules and excludes guides/mask in reload/build lifecycle. Run focused Editor and relevant PlayMode suites; do not run the full gameplay gate unless this step changes an existing gameplay consumer (a plan/spec reassessment would then be required).
- Recovery: Preserve failing before/after snapshots, saved fixture and isolated logs. Stop on transaction failure before more edits. Do not clear global Undo, rebuild during Undo callbacks, reset real saves, or patch landing APIs. Resume only the affected result/transaction/lifecycle behavior under the normal failure gate.
- Risk: High — new issues/status and partial hierarchy must restore with Tilemap occupancy, especially after native strokes and save/reopen.

### Step 5: Walk through the corrected designer UX and update durable guidance

- [ ] Status marker
- Criteria: S1–S8, S10–S22, S24–S29 — final synthetic end-to-end evidence for this phase; production S9/S23 remain pending.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/README.md`
  - `Assets/_Project/Tests/Editor/RoomEditingWorkflowTests.cs`
  - `Assets/_Project/Tests/Editor/RoomPreviewFeedbackTests.cs`
  - `work/Reviews/RoomAuthoring/EditorWorkflow.md`
  - `Docs/Architecture/RoomAuthoring.md` — feature-owned durable record only; preserve unrelated document content and do not stage dirty root README changes.
- Changes: Update the guide and durable selected design for irregular boundaries, partial output, exact explicit box-mode use, issue selection, correction/Rebuild and supported-width/narrow-void limits. Complete a native disposable-scene walkthrough using temporary native-sized art; retain only evidence outside Assets. Include an intentionally unresolved asset/span and missing-floor visualization, clearly distinguished from production art readiness. Clean only disposable scene/in-memory fixture assets and restore prior selection/framing/tool state without saving dirty production scenes. Finish a criterion-linked phase report; independent QRSPI Review remains a separate gate. The new durable-record scope explicitly permits updating this feature's existing untracked design document; inspect and preserve unrelated additions if present.
- Tests: S5/S11/S24/S26/S27 — native 8×6 fill, single interior-cell erase to 47 cells, Undo back to 48, four-column expansion to 72, valid irregular boundary update, deliberate partial issue, click-to-focus, correction/Rebuild and full box erase. Confirm only the requested cells change after each erase and no misleading whole-room disappearance; native Undo/Redo restores matching cells/output/status/issues. S16/S17/S29 — validate read-only and saved/reopened partial/complete fixtures behave as documented. S14/S20 — equal-style second room, same-name unowned sentinel, original unsaved scene, original art/GUIDs and unrelated dirty files match their baselines. S18/S19 — rely on Step 4's matching runtime evidence unless source changed. S21/S22 — guide reproduces the observed workflow; label tests/renders not run, native input inconclusive, art-dependent checks pending as appropriate. Re-run tests only for new source/test changes or stale evidence. Capture exact final source/configuration digests; do not substitute fixture success for S9/S23.
- Recovery: Preserve failed walkthrough evidence and authored fixture before cleanup. If native input is unavailable or inconclusive, leave this step incomplete and request only the specific human gesture/observation needed; never waive the native UX check silently. A guide correction cannot conceal an unverified implementation.
- Risk: Medium — native guide/selection/Undo evidence must describe the committed source, and temporary assets must not enter production scenes or shipping build entries.
