## Phase 3: Integrate Standard artwork

### Dependencies

- Depends on: 1, 2, 2a
- Why: Directly requires Phase 1's serialized style/placement/validation contracts and runtime-safe room output, Phase 2's palette, brush and batch operations, and Phase 2a's extended style/result contracts, irregular placements, selectable partial preview, restored issues and updated guide. Separately requires the user's prepared production floor, north-wall, cap, convex-corner and concave-corner art. It is not executable to visual completion while those assets are unavailable.

### Step 1: Establish Standard asset readiness without changing the art

- [ ] Status marker
- Criteria: S4, S8, S9, S13, S23, S24, S26, S28 — establish actual asset/configuration facts; this step alone does not pass visual seams.
- Files:
  - `work/Reviews/RoomAuthoring/AssetReadiness.md`
  - `work/Reviews/RoomAuthoring/AssetReadiness.json`
  - Read-only: the exact prepared floor/wall/cap/corner paths and their existing metadata, to be supplied when available; paths are intentionally not invented now.
  - QRSPI-owned query/research artifacts only through their isolated owning skills when refreshing factual evidence; never edited as implementation shortcuts.
- Changes: Inspect the prepared assets and record identities/GUID/local sprite IDs, file digests, canvas/imported/visible bounds, PPU, point filtering, pivots, native dimensions, structural north-wall repeat, cap repeat/thickness, oriented convex/concave role coverage, endpoint-type reservations, short-run/narrow-void constraints, and proposed alignment information. Check the 32×32 floor/cap and 32×64 or 64×64 wall convention. Select a compatible single-width Standard family from actual available assets; do not assume both widths must pack the same run. Apply the SOLITUDE art-pipeline review guidance. No resampling, replacement art, original import migration, or silent asset repair is authorized.
- Tests: S4/S13 — required used roles, including directional concave joins, and actual import properties either conform or yield a specific blocker with exact asset paths. Missing unused roles are distinguished from missing roles required by the demo layouts. S8 — the measured repeat/reservations admit exact coverage of the 8-cell demo boundary; zero-reservation two-cell coverage implies four modules, not the obsolete two-module example. S9/S23 — document which joins are still unverified and require Step 3's native rendering, including inner corners and tall north faces near holes. Missing art is Blocked/Not run, never Passed. The report identifies all inspected asset revisions.
- Recovery: If art is absent/incompatible or structural measurements contradict the spec, stop here, retain the report, and obtain a reviewed correction through the relevant owning stage. Do not modify the art or scale room roots. Refresh needed factual Research evidence under its isolation contract. A changed research artifact invalidates dependent spec/plan provenance: reassess and recover exact-revision approval before Step 2, preserving completed markers from Phases 1, 2 and 2a. Do not merely update stored hashes.
- Risk: High — the prior research does not verify the future production family, and nominal dimensions do not prove structural joins. This is an external readiness hold point, not permission to create a guessed Standard asset set.

### Step 2: Bind Standard and create an isolated painted demonstration

- [ ] Status marker
- Criteria: S1, S4, S5, S7, S8, S16, S20, S22, S23, S24, S25, S26, S27, S28, S29 — a real Standard room using the completed editing workflow.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Data/StandardRoomStyle.asset`
  - `Assets/_Project/Art/Tiles/Floors/RoomAuthoring/StandardFloor.asset`
  - `Assets/_Project/Features/World/RoomAuthoring/Scenes/RoomAuthoringDemo.unity`
  - `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringDemoSetup.cs`
  - `Assets/_Project/Tests/Editor/StandardRoomAuthoringTests.cs`
  - `Assets/_Project/Features/World/RoomAuthoring/README.md`
  - Corresponding new Unity metadata; existing production sprites and metadata remain unchanged.
- Changes: Bind verified asset references/offsets and oriented convex/concave joins/reservations in the Standard style, create its ordinary floor Tile, and create a new nonshipping demonstration scene with a unit Grid, review camera, and selected room authoring object. Author the 8×6 occupancy using the actual Box Fill workflow. Setup may create missing assets/initial scaffold, but must refuse to overwrite an existing painted demonstration and never create a second layout authority. Choose camera/render settings compatible with the project's 32 PPU pixel convention and scene-local draw order. Do not add the demo to shipping entries or copy/modify a production scene. Extend the guide with exact Standard asset/style selection. Keep an 8×6 baseline plus a measured, compatible irregular/cutout example and an intentionally incomplete narrow-void or missing-role example; neither invent production compatibility nor overwrite a painted layout to create them.
- Tests: S1/S4 — the style references verified assets and changing a compatible style reference does not change identity/occupancy. S5/S7/S8 — actual brush fill creates 48 native floors and the measured expected north-wall coverage without scaling or manual architectural edits. S16 — save/reopen retains style references, occupancy and generated counts; repeated setup preserves a manually adjusted demo layout. S20 — production scenes, build entries, originals and GUIDs match baseline. S22/S23/S24–S29 — guide identifies real Standard inputs and demonstrates complete versus incomplete irregular results, located issue selection, explicit correction and saved issue restoration; seam verification remains pending Step 3.
- Recovery: Remove/recreate only a new disposable demo during initial setup; once painted, preserve its authored occupancy and rebuild output rather than overwriting the scene. If new references are wrong, correct only new style data after verifying the asset identities; never regenerate existing art GUIDs.
- Risk: Medium — saved asset references, camera scale, and builder reruns can accidentally introduce transient materials, scene overwrites or wrong PPU. Use supported Unity authoring APIs and reopen verification.

### Step 3: Verify native-pixel joins and real-art runtime behavior

- [ ] Status marker
- Criteria: S4, S7, S8, S9, S10, S16, S19, S21, S23, S24, S26, S27, S28, S29 — qualify the prepared style visually and across runtime/persistence boundaries.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/Data/StandardRoomStyle.asset` — configuration/offset corrections only, within verified native coverage.
  - `Assets/_Project/Features/World/RoomAuthoring/Scenes/RoomAuthoringDemo.unity`
  - `Assets/_Project/Tests/Editor/StandardRoomAuthoringTests.cs`
  - `Assets/_Project/Tests/PlayMode/RoomAuthoringFloorCompatibilityTests.cs`
  - `work/Reviews/RoomAuthoring/VisualAlignment.md`
  - `work/Reviews/RoomAuthoring/Native/` — retained images only when captured; no empty speculative directory.
- Changes: Capture the actual 8×6 Standard scene at native pixel scale and integer zoom, including north-wall joins, west/east/south caps, all used convex and concave orientations, complete compatible cutouts/holes, and a specifically diagnosed incompatible narrow void. Record sprite/configuration/source revisions, camera settings, baseline offsets and observable seam outcomes. Recheck after expanding to 12×6 and reopening. Correct configuration only when evidence establishes the intended anchor; artwork defects or impossible coverage return to the art/spec owner. Confirm layer/depth behavior with the player where relevant. Do not edit generated geometry by hand to make the screenshot pass.
- Tests: S4/S7/S8/S9 — inspect actual floor alignment, no module distortion, correct north-only faces, thin cap perspective, all corner joins, native visible thickness despite padding, and no unintended gaps/overlap or duplicated faces in completed placements. Confirm north-facing perspective on interior hole boundaries, actual inner-corner reservations and tall-wall fit; known incompatible regions show located issues instead of complete-preview claims. S10 — expansion moves the east edge/corners and removes old geometry. S16/S29 — saved/reopened complete and partial output and associated issue state remain consistent. S19 — production awakening/landing APIs reject hole, guide-only, mask-only and architecture-only positions, accept visible floor, and preserve blocker behavior with actual Standard assets. S21/S23 — native renders are evidence distinct from compilation/fixture checks; document Fail/Not run if any required view or runtime test cannot be exercised.
- Recovery: Preserve the failing captures/configuration and stop when an asset cannot meet its declared structural span. Revert only local new style offsets to their last measured state. Never stretch/rotate/resample art, change the logical grid, or patch gameplay floor queries to pass visual/runtime checks.
- Risk: High — native seams, sprite padding and render depth cannot be qualified by array assertions alone. Visual acceptance remains pending until all required actual-art views are inspected.

### Step 4: Complete criterion-linked verification and the designer handoff

- [ ] Status marker
- Criteria: S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13, S14, S15, S16, S17, S18, S19, S20, S21, S22, S23, S24, S25, S26, S27, S28, S29 — consolidate evidence for every criterion; no new feature scope.
- Files:
  - `Assets/_Project/Features/World/RoomAuthoring/README.md`
  - Existing room-authoring Edit Mode and PlayMode tests created by Phases 1, 2, 2a and 3, updated only for a demonstrated verification gap.
  - `work/Reviews/RoomAuthoring/Verification.md`
  - `work/Reviews/RoomAuthoring/ImplementationReport.md`
  - Read-only regression gate: `Tools/Verification/README.md` and `Tools/Verification/verify.py`.
  - Gate-generated logs/results in its documented output location or an explicitly selected isolated review output, not production assets.
- Changes: Run the focused geometry/editor/runtime suite and the documented isolated full verification gate because the authoring lifecycle affects the existing floor/awakening path. Use isolated scenes/projects/disposable saves; never validate against normal player saves or rewrite dirty production scenes. Record exact source and asset/configuration snapshots, test names/results, visual evidence, preservation findings and remaining blockers for S1–S29. Finish the guide through a literal designer walkthrough. Provide the requested architecture/files/workflow/verification/constraints/limitations report and answer whether a designer can select Standard and box-fill 8×6 without manually editing generated architecture. Hand off to the separate final QRSPI Review gate; do not claim integration or final review has already occurred.
- Tests: S21 — focused suite failures are resolved or explicitly reported; the complete gate needs a successful final results.json, with no missing builds/results or unqualified partial skip-build pass. S9/S23 — Step 3's exact production-asset renders still match current source/style/asset revisions. S20 — compare the implementation baseline for unrelated files, production scene/build entries, existing GUIDs and assembly boundaries; new files have valid metadata and player builds have no editor/test dependencies. S22/S24–S29 — a designer can follow irregular editing, partial issue selection/correction, missing-floor visibility, explicit box modes, Undo/Redo and partial-state reopening in addition to rectangle creation. All other IDs link to concrete test or observed evidence already specified in earlier steps; unavailable checks stay Not run. Do not broaden/repeat successful tests without a new change, failure, or unresolved concern.
- Recovery: If verification fails, preserve outputs and return only the affected behavior to its owning step; recheck evidence freshness after corrections. If the environment cannot run the required gate or render, report the limitation and leave the corresponding criteria incomplete. Never clean unrelated changes or replace missing evidence with a compilation claim.
- Risk: High — mixed source revisions, partial gates, or stale renders can produce a false completion claim. Evidence must describe the exact final snapshot; final independent Review is still required after implementation.
