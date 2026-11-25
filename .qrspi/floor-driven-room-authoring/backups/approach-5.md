# QRSPI Approach: floor-driven-room-authoring

## Context

The selected tooling remains a **room-specific Tile Palette brush with explicit generation transactions**, per-room serialized occupancy and stable identity, generated floor Tilemaps, and owned architectural SpriteRenderers. The decision and rationale from the supplied `backups/approach-4.md` are preserved: explicit transactions control batching and Undo without depending on unresolved native Tilemap notification timing.

The latest human clarification changes geometry scope. Ordinary edits must follow painted boundaries, including cutouts, concave corners, and holes; rectangle-only architecture is insufficient (`request.md:1075–1081`). This reopens geometry design before production artwork integration. It does not reopen tooling selection or authorize art changes. Earlier deferral of all irregular shapes and concave corners is superseded for this scope. Shared boundaries between rooms, doors, runtime construction and collision changes remain future work.

The production baseline remains 32×32 logical cells and floor panels, unit Grid cells and 32 PPU, 32×64 or 64×64 walls, and 32×32 cap canvases. Structural coverage, corner reservations, and visible thickness remain explicit style information rather than deductions from canvas size (`request.md:1046–1061`). Original artwork is preserved. Production assets were reported as still in preparation (`request.md:1064–1072`); this revision supplies no readiness or visual-correctness claim. Optional `background.md` does not exist.

## Constraints and Facts

- **Observed disappearance mechanism:** current extraction already builds exposed cardinal runs from arbitrary occupied cells, but placement immediately rejects a nonrectangle. After validation failure, the editor clears the entire generated output, including the visible floor. That failure path does not erase the painted occupancy. Sources: `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomBoundaryExtractor.cs:12–46`; `Generation/RoomPlacementBuilder.cs:24–30`; `Editor/RoomAuthoringOperations.cs:126–146`; `Editor/RoomGeometryWriter.cs:10–28` within the same feature directory. This explains source behavior, without claiming reproduction of the exact reported gesture.
- **Current geometry limits:** runs know side, start and length; corners are untyped convex positions. Placement ignores extracted corner positions and places four corners at bounding-box extremes. Sources: `Assets/_Project/Features/World/RoomAuthoring/Geometry/RoomGeometry.cs:6–24`; `Generation/RoomPlacementBuilder.cs:32–58`.
- **Current style limits:** configuration contains per-side caps, four rectangular corner roles, per-side start/end reservations, explicit anchors/offsets and north modules sharing one structural repeat width. No concave roles appear in this source, and reservations do not distinguish endpoint junction types. Sources: `Assets/_Project/Features/World/RoomAuthoring/RoomStyleDefinition.cs:12–54`; `Generation/RoomPlacementBuilder.cs:35–43`.
- **Usable integration seams:** the writer consumes a placement description; edits record occupancy Undo before rebuilding. Identity/ownership and aligned Grid checks guard destructive output writes. Sources: `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs:18–38`; `Editor/RoomAuthoringOperations.cs:60–115`. These are compatible extension points, not evidence that changed behavior already works.
- **Coupled failures:** overlap, style configuration and placement currently feed one success/failure path. Configuration checks all four original corner roles and 32/64-pixel north repeats. Floor-preserving architectural failure behavior therefore affects validation and writing together. Source: `Assets/_Project/Features/World/RoomAuthoring/Editor/RoomAuthoringValidator.cs:17–29`, `:49–85`.
- **Research age and limits:** supplied Research predates the now-present RoomAuthoring implementation. Its absence findings for brushes, ownership and room tests are historical. Its asset dimensions and two-module example also precede the clarified production baseline (`research.md:19–97`, `:177–204`, `:224–280`). Direct source inspection above establishes current compatibility facts for Shape; it does not amend Research or establish a production family or rendered concave assembly.
- **Verification foundations:** current geometry tests cover a rectangle and exterior erasure at extraction level; rebuild tests cover output recovery, ownership and idempotence. Sources: `Assets/_Project/Tests/Editor/RoomGeometryTests.cs:10–32`; `RoomRebuildTests.cs:16–102`. Their existence is not a passing result. No tests or renders were run in Shape.
- **Preservation constraints:** runtime floor detection considers active same-scene noncolliding Tilemaps, so the occupancy mask must remain excluded. Source: `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:65–75`. Preserve feature ownership, assembly boundaries, GUIDs and room identities; reusable palettes/tiles remain under Art/Tiles (`research.md:99–116`).

## Candidate Approaches

### Option A: Exposed edges and local junction classification

**Shape.** Retain occupancy as complete geometric input. Derive every cardinal edge adjacent to empty space, including edges around interior holes. Classify grid vertices from their occupied neighboring quadrants to distinguish oriented convex corners, oriented concave corners and straight continuation. Join compatible collinear edges into runs retaining actual endpoint junction types. Resolve placements from these edges and junctions rather than the bounding rectangle.

North-facing edges receive north wall faces wherever they occur, including inside cutouts and holes; other directions receive the established thin caps. Orientation follows occupied-to-empty direction, not an assumption that each perimeter is an exterior rectangle. Corner roles are relative to occupancy, and asymmetric sprites are not implicitly rotated. Endpoint reservations can differ for convex and concave joins.

**Fit and complexity.** This extends the existing cardinal-run extractor and placement writer while keeping geometry independent of sprites. Holes require no separate polygon representation. Local integer-grid decisions offer a small deterministic core; typed junctions, endpoint reservations and short-run resolution are the substantive additions. Merely removing the rectangle check is insufficient. Full current-room regeneration remains suitable.

Recommend preserving disconnected components under the existing room owner, with independent boundaries, rather than changing room identities after an erase. Diagonal-only contact should keep separate boundary branches instead of inventing a connected passage; art compatibility at that touching vertex may still need a diagnostic. These are proposed design choices, not observed requirements already implemented.

**Operational behavior and failures.** A missing concave piece, insufficient run length after reservations, or a two-cell-only wall repeat can prevent architecture from fitting a small edit. A two-cell-high north face can also extend across a narrow hole into occupied floor or other architecture. Correct extraction alone does not prevent these visual conflicts. Such failures should identify specific placement/art limitations rather than reject all nonrectangular geometry.

Recommend that safe, valid occupancy continue to produce its current visible floor even if architecture cannot complete. Remove obsolete architecture and show a clear diagnostic; conservatively withhold the room's architectural layer on a placement failure rather than present scattered segments as a finished enclosure. This keeps the edited floor visible without retaining stale walls. It is not a claim of architectural completion with incompatible art. Ownership or coordinate-system faults continue to block unsafe mutation. Empty occupancy naturally yields empty output.

**Verification and migration.** Useful evidence includes exact edges/junctions for L/U shapes, notches and holes, negative coordinates, disconnected components and diagonal contacts; short-run and reservation placement; and brush erase/Undo/Redo/save-reload checks proving only requested occupancy changes occur and floors follow them. Native-pixel concave joins and narrow voids need rendered evidence. Existing occupancy and identities need no conversion. Preserve original style fields and serialized references while adding concave configuration. Generated output remains disposable; rolling back the expanded renderer need not destroy occupancy.

### Option B: Trace explicit oriented perimeter loops

**Shape.** Derive exposed edges, then trace closed oriented contours, including exterior loops and hole loops. Classify turns and form runs from ordered segments. Keep the selected brush, ownership and mixed rendering architecture.

**Strengths.** Continuous perimeter traversal provides natural predecessor/successor relationships for joins and reservations. Exterior versus hole classification and whole-loop diagnostics are explicit. This may be worthwhile if ordered perimeter traversal becomes a direct authoring need.

**Costs and failure modes.** Current source has runs but no contour topology, so this adds more representation and traversal machinery than Option A. Winding, deterministic start points, multiple components, and branch pairing at diagonal contact need explicit treatment. Incorrect pairing can join separate contours or give a hole the wrong orientation and wall perspective. Loop tracing still does not resolve unavailable concave artwork, short runs or full-height wall overlap. Arbitrary-angle polygons and smoothing add scope without helping the painted orthogonal grid.

**Verification and migration.** This requires loop termination, orientation and complete edge-consumption evidence in addition to Option A's placement/editor/render checks. Occupancy still needs no conversion, but generated descriptions change more substantially. A contour view can be derived later from the same exposed edges if its ordering becomes useful.

## Recommendation

Recommend **Option A for expanded geometry**, preserving the already selected explicit-brush tooling. Exposed cardinal edges and typed local junctions describe edited orthogonal regions, including holes, and extend current source directly. Explicit contour loops can remain a later derived representation.

Include cutouts, concave corners and holes before production artwork integration. Preserve arbitrary occupancy, room identity, native art scale, chosen dimensions, deterministic rebuilding and grouped Undo. Keep safe floor output independent of architectural completeness so an unsupported architectural join cannot make all remaining floor disappear. Invalid architecture should be removed with a precise diagnostic, not left stale or represented as complete.

Retain one configured north structural repeat width per style for this revision rather than silently adding mixed-width composition. For unconstrained single-cell editing, recommend a prepared one-cell repeat family with compatible joins. A two-cell-only family necessarily has narrower placement coverage. Nominal 32×64 and 64×64 modules do not prove compatible structural spans or authorize combining them. If Standard requires both widths to cover short or odd runs, that is a separate human composition choice, supported by art evidence. Concave roles and endpoint-dependent reservations need explicit configuration; no substitutions or automatic artwork generation.

Full-height north faces near narrow holes require actual art and sorting evidence. Treat their fit as an explicit visual constraint, not an unstated exception to geometry support or permission to clip/scale sprites. Synthetic fixtures can establish geometry/editor behavior while production art is unavailable; they cannot establish final seams. Fully rendered completion remains dependent on compatible prepared artwork.

The prior stock-brush observer alternative remains deferred for its original reason: notification timing and Undo ordering are unresolved. The new geometry scope provides no reason to reopen tooling selection.

## Preserved Tooling Decision

**Tooling: selected, preserved.** Option A in `backups/approach-4.md` was selected when the human directed “Continue” after reviewing revision `b9da291ab226ea884fa2495a4521f2667d3ba192a9c39a743c5003375a8de2fe`. It selects the room-specific brush, explicit batched generation/Undo, separate visible floor Tilemap, and owned architectural SpriteRenderers, avoiding an unverified notification dependency.

**Expanded geometry approach: None.** Option A above is recommended and awaits human selection of this revision, including the proposed handling of disconnected/diagonal contact and floor-preserving architectural failure. “Yes, address it” authorizes scope reassessment rather than approval of this unseen design. Grid/art dimensions remain selected; asset readiness and rendered joins remain unverified. The prior durable record is `Docs/Architecture/RoomAuthoring.md`; this role changes only this approach artifact, not that record or downstream documents.

## Decision

None.

## Open Questions

1. **Research evidence gap — current inventory.** Refresh historical Research against present RoomAuthoring geometry, placement, validation, lifecycle and tests before downstream definition relies on its absence findings. Shape's source checks establish compatibility rather than a comprehensive lifecycle audit or executed verification.
2. **Research evidence gap — production family.** Identify actual prepared Standard assets and imported identities, extents, PPU, filtering, trimming, pivots, anchors and baselines against the latest dimensions. Preparation was reported incomplete; no uniform downsampling factor or import conformity is assumed.
3. **Research evidence gap — concave/small-void fit.** Establish available oriented concave joins, endpoint reservations, structural repeats and minimum coverable runs. Obtain rendered seam/overlap evidence for notches, holes, narrow voids opposite two-cell-high north faces, and diagonal-contact junctions. Diagnose particular art limitations rather than infer universal renderability from boundary support.
4. **Human design choice.** Select Option A's local edge/junction approach or Option B's explicit loops, including the recommended floor-preserving failure presentation and component/contact policies. Mixed-width composition or art-specific clipping/cutaway treatment requires a separate scope choice if actual production constraints demand it; neither is presumed here.

No implementation, acceptance criteria, implementation sequence, tests, or render results are supplied by this shaping revision.
