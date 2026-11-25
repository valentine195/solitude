# QRSPI Approach: floor-driven-room-authoring

## Context

The current scope follows painted room boundaries through cutouts, concave corners and holes before production-art integration; rectangle-only generation is insufficient (`request.md:1075–1081`). The technical choice below concerns that geometry expansion. It preserves the selected room-specific Tile Palette brush, explicit generation/Undo transactions, serialized per-room occupancy and identity, separate visible floor Tilemaps, and owned architectural SpriteRenderers.

Partial-preview UX is already approved: retain current layout visibility, render valid architecture, show unresolved boundaries with red guides and specific markers, offer an Incomplete preview Inspector status with selectable issues, update after correction, preserve Undo, and remove obsolete architecture. Visibility remains useful if the floor asset is missing. Last-valid-room ghosting is optional, not the default (`request.md:1084–1096`).

The baseline remains a 32×32 logical grid/floor panel at 32 PPU with unit Grid cells, 32×64 or 64×64 wall modules, and 32×32 cap canvases. Canvas size alone establishes neither structural repeat nor seam compatibility. Original artwork remains preserved; production assets are still being prepared (`request.md:1046–1072`). No background.md is present. This revision uses the refreshed Research rather than the historical absence findings identified in `backups/approach-6.md`.

## Constraints and Facts

- **Why an erase blanks the room:** extraction already finds exposed cardinal runs for arbitrary occupancy; placement rejects nonrectangles; failed rebuild clears all owned generated output, including the floor. Occupancy remains serialized. This source mechanism explains the reported symptom without claiming gesture reproduction (`research.md:63–69`).
- **Geometry and placement limits:** current geometry has runs, untyped convex vertices and rectangle bounds, without concave roles or loop topology. Placement uses four bounding-box corners and a single repeat width per side. Styles hold explicit anchors, offsets and per-side start/end reservations (`research.md:9–15,41–45,63–65`). Removing the rectangle check alone cannot assemble irregular boundaries correctly.
- **Editor integration:** the custom brush already supports cell operations and batched Box Fill/Erase; explicit transactions record occupancy and generated-output Undo. Rebuild and creation reject Play Mode transitions. Existing lifecycle and persistence mechanisms retain identity/cells separately from generated visuals (`research.md:27–29,47–57`).
- **Partial preview is new behavior:** current validation returns one failure string; writing is all-or-nothing. No structured issue list, unresolved-boundary guides, or guaranteed missing-floor layout visualization was found. The occupancy map has no TilemapRenderer (`research.md:59–69`).
- **Safety and ownership:** aligned unit Grid/transform restrictions and explicit output ownership protect authored content. Runtime succession treats active noncolliding Tilemaps as floors; editor occupancy visibility must retain its runtime/build exclusion. Preserve assembly boundaries, GUIDs, room identity, unrelated scenes and gameplay behavior (`research.md:17–25,37–39,55–57`).
- **Evidence limits:** current test fixtures and assertions support geometry/editor verification foundations, not passing results or production visual compatibility. No prepared production style mapping or native-pixel seam/concave render evidence was established (`research.md:13–15,43–45,71–77`).

## Candidate Approaches

### Option A: Exposed edges with local junction classification

Derive cardinal edges wherever occupied cells meet empty space, including holes. Classify vertices from neighboring occupied quadrants into oriented convex/concave joins and straight continuation. Form collinear runs retaining their actual endpoint types; resolve placements from these runs and joins instead of rectangle bounds. North-facing edges receive wall faces; other directions receive thin caps, including on hole boundaries. Geometry remains independent of sprites.

**Fit and complexity:** this directly extends the existing edge/run extractor and placement description. Local integer-grid rules avoid a second contour representation. Typed joins and endpoint-dependent reservations are the main additions. Holes need no separate polygon model. Full current-room regeneration remains suitable.

**Operational behavior and failure modes:** local placement issues can identify unresolved runs/joins while preserving valid current output and the approved partial-preview UX. Missing concave pieces, short runs after reservations, incompatible repeat widths, and full-height walls extending across narrow voids remain explicit placement/art limitations. A partial result must never imply a complete enclosure. Unsafe ownership/coordinate faults still prevent unsafe writes; they are distinct from a locally missing sprite.

**Verification and migration:** useful evidence includes L/U shapes, notches, holes, negative coordinates, empty occupancy, disconnected regions and diagonal contacts; exact edge/junction coverage; short-run placement; and edit/correction/Undo/Redo/save-reload behavior with missing floor or architecture assets. Preserve existing occupancy, room IDs and serialized style references while extending join configuration. Generated output is reconstructable; rollback need not convert or discard occupancy. Native-pixel production seams remain separately unverified.

### Option B: Explicit oriented perimeter loops

Derive exposed edges, then trace closed exterior and hole contours. Classify ordered turns and construct runs from them. Preserve the same selected brush, ownership and rendering architecture and the same approved partial-preview UX.

**Fit and complexity:** loop order makes predecessor/successor joins and whole-contour diagnostics explicit. It is useful if ordered contour traversal is itself needed. Current code has no loop representation, so this adds winding, deterministic start points, termination and edge-consumption machinery beyond Option A.

**Operational behavior and failure modes:** multiple components and diagonal contacts require explicit branch pairing. Incorrect pairing can merge contours or invert hole-facing perspective. Loop tracing does not solve missing concave artwork, short runs, or wall-face overlap; partial-preview issues are still necessary.

**Verification and migration:** needs the same placement/editor/render evidence as Option A plus complete edge consumption, loop closure and orientation checks. Occupancy needs no conversion, but generated geometry descriptions change more substantially. Ordered contours can also be derived later from Option A's edges if needed.

## Recommendation

Recommend **Option A**, which extends the current orthogonal edge/run model with less additional topology. This is a recommendation awaiting human selection. Keep the selected explicit-brush tooling and approved partial-preview UX for either option.

Recommend retaining disconnected painted components under their existing room identity and treating diagonal-only contact as separate boundary branches, without inventing a passage. A touching vertex may require an unresolved-junction diagnostic where artwork cannot fit. These component/contact policies are proposed choices for approval, not established behavior.

Keep one configured north structural repeat width per style in this revision. A compatible one-cell repeat family gives broader placement coverage for single-cell edits; a two-cell-only family can leave short or odd runs unresolved. The nominal availability of 32×64 and 64×64 canvases does not establish that they compose. Mixed-width solving, clipping/cutaway treatments, art generation and automatic scaling are outside the recommendation.

The preview must separate authored layout, valid current placements and unresolved locations. If the floor asset is unusable, editor-only occupancy guides can retain layout visibility without substituting production artwork. Specific configuration/placement issues should remove affected obsolete architecture while allowing independently valid placements. Ownership/coordinate faults require safe refusal rather than destructive cleanup. These design directions preserve the approved UX without prescribing an implementation contract or sequence.

Fixture-based geometry and editor behavior can be defined with the supplied facts once the geometry/component policy is selected. Production artwork is not a prerequisite for that definition; it remains a prerequisite for declaring production visual completion. Prepared art must later establish actual repeats, anchors, oriented convex/concave joins, reservations and fit around narrow holes. Neither option promises arbitrary artwork fits every painted layout.

## Historical Decisions

**Tooling selected, preserved:** the supplied prior approach records selection of the room-specific brush with explicit batched generation/Undo, separate visible floor Tilemap and owned SpriteRenderers after “Continue” on revision `b9da291ab226ea884fa2495a4521f2667d3ba192a9c39a743c5003375a8de2fe`. This avoids depending on unverified native Tilemap notification timing (`backups/approach-6.md`, Preserved Tooling Decision).

**Dimensions selected, preserved:** the latest dimensions supersede the earlier production-size proposals. The generator consumes prepared native-scale assets; it does not edit originals or resize during rebuilding (`request.md:1046–1061`).

**Preview UX approved, preserved:** “Yes, approved” on 2026-10-20 selects the partial-preview behavior described in Context. It supersedes withholding the entire architectural layer after a local placement failure. It does not approve this geometry recommendation or an unseen implementation plan (`request.md:1084–1096`; `backups/approach-6.md`, Approved Preview UX).

**Scope correction preserved:** “Yes, address it” expands boundary support to cutouts, concave corners and holes; it does not select an unseen technical approach. Shared room boundaries, doors and runtime collision changes remain outside this revision. `Docs/Architecture/RoomAuthoring.md` is the previously recorded durable design location; this shaping role changes only approach.md (`request.md:1064–1081`; `backups/approach-6.md`, Preserved Tooling Decision).

## Decision

Selected **Option A: exposed edges with local junction classification**. On 2026-10-20 the human approved proposal revision `b3b7b3fa94a6f9e2f114190466f6a3cf3daa3a2f3f1966ccba9e38dfc8a6e316` and authorized the revised Spec. This includes retaining disconnected painted components under the same room identity and treating diagonal-only contact as separate boundaries, not a passage. Existing tooling, grid/art dimensions and approved partial-preview UX remain selected. Production visual completion still depends on prepared artwork and native-pixel evidence; no new implementation plan is approved by this selection.

## Open Questions

1. **Human choice before downstream definition:** select Option A or Option B and the proposed disconnected-component/diagonal-contact policy. Tooling, grid/art targets and partial-preview UX remain selected.
2. **Pending production-art validation:** when prepared assets exist, establish imported identities/settings, structural spans, anchors, oriented joins and endpoint reservations; inspect native-pixel seams and overlap in cutouts, concave corners, holes and narrow voids. This limits production visual completion, not fixture-based geometry/editor definition. No universal minimum renderable void or arbitrary-art fit is claimed.
3. **Optional dependency unknown, not a blocker:** native Tilemap notification signatures/timing relative to edits and Undo/Redo remain unresolved (`research.md:31–35`). The selected explicit-transaction brush does not depend on them. Revisit only if stock-brush automatic observation is separately selected.

No blocking Research gap for the recommended fixture-based definition was identified. No tests or renders were run in Shape, and no product, spec, plan, durable-record or artwork changes are included.
