# QRSPI Spec: floor-driven-room-authoring

**Status:** Revision 2 draft for human review; this document does not authorize implementation.
**Scope:** Floor-driven orthogonal room authoring, including cutouts, concave corners, holes, and useful partial previews in the Unity Editor. Prepared Standard artwork remains a separate visual-completion dependency.

## Inputs

Project root: `/Users/jeremyvalentine/SOLITUDE`. SHA-256 hashes identify exact file bytes.

| Input | Project-relative path | SHA-256 |
| --- | --- | --- |
| Settled request and approved clarifications | `.qrspi/floor-driven-room-authoring/request.md` | `ddfe3b49fcf0257ad159f0d9968c6fe6be6e6f26107581a28d914897445d50d5` |
| Approved research questions | `.qrspi/floor-driven-room-authoring/queries.md` | `4296e5b281952facdc740d2a7c2a365b4437329369cb78169358ce78546b291c` |
| Approved current-source research | `.qrspi/floor-driven-room-authoring/research.md` | `0fcb77321743205e1a95524a6737e797b4fbf7e7bbf5aeb4fd2463602d47e1c5` |
| Selected expanded Option A, including recorded human decision | `.qrspi/floor-driven-room-authoring/approach.md` | `0ed06af62ea3e51b3a828f58c400ffb901423815511393fd5b077aa9901c1dca` |

The human approved approach proposal `b3b7b3fa94a6f9e2f114190466f6a3cf3daa3a2f3f1966ccba9e38dfc8a6e316`; the selected input above adds that decision record. Existing room-specific brush transactions and the approved partial-preview UX remain selected. The latest clarifications supersede the original brief's deferred irregular shapes and earlier illustrative artwork sizes. Neither source filenames nor canvas dimensions establish final production compatibility.

## Revision summary

Prior specification: `backups/spec-1.md`, SHA-256 `a49ec32f7234b6cf4ceb54236df5144448a15a92fea8dcc07d0fdfca67adc8db`.

- Preserve IDs S1–S23. No IDs retired or reused.
- Refine S5, S7–S9, S11–S19, S21–S23 for irregular layouts, partial output, issue state, and revised visual evidence. S1–S4, S6, S10 and S20 retain their previous criterion text.
- Add S24–S29 for irregular boundaries, component/contact identity, partial-preview outcomes, selectable feedback, correction, and incomplete-state restoration.
- Replace blanket output clearing after local configuration/placement failure. Rectangle-only rendering is no longer the completed behavioral target.
- Existing completed implementation phases and their evidence remain historical for the prior specification. Do not erase progress or claim that those checks verify these changed requirements. The implementation plan requires revision before more product work.

## Behavioral delta

| Current behavior established by Research | Required behavior |
| --- | --- |
| Authored occupancy accepts arbitrary cells, but placement accepts only a filled rectangle and four bounding-box corners. | Renderable geometry follows actual exterior and interior boundaries, with oriented convex and concave joins; irregular shape alone is not an error. |
| A single placement/configuration error clears all owned generated output, including the floor. | Keep current floor coverage and independently valid architecture; remove obsolete/invalid output and identify unresolved locations. |
| A room has one diagnostic string and no structured issue locations or selectable error guides. | Distinguish complete, incomplete, empty and refused results; expose specific issues and editor-only guides that can be selected/focused. |
| Occupancy has no renderer, so missing generated floor can hide the authored layout. | Keep authored cells visible through editor-only guides when the floor asset cannot render them. |
| Existing tests expect invalid previews to become wholly empty. | Verification requires partial output, useful feedback, and restoration of complete/incomplete states as well as existing preservation guarantees. |

The existing creation, palette, explicit brush batching, ownership, inactive authoring-mask lifecycle, and scene persistence remain foundations. Existing production scenes and gameplay retain their behavior.

## Grid, assets and style contract

| Element | Native production convention |
| --- | --- |
| Logical cell | 32×32 pixels, 32 PPU, one Unity unit in X/Y |
| Floor | 32×32 pixels, one panel per occupied cell |
| North wall | 32×64 or 64×64 pixels, nominal 1×2 or 2×2-cell visual extent |
| Cap and corner canvas | 32×32 pixels; visible thickness and coverage are configured |

Use native proportions and Point filtering. No stretching, resampling, import rewrites, logical-grid changes, automatic replacement art, or unapproved rotation of asymmetric sprites. North-wall height extends northward outside authored floor occupancy. Canvas bounds, visible pixels, structural span, pivot, anchor, baseline and corner reservations are distinct configuration facts.

Each style retains one configured structural north-wall repeat width; same-span variants may be deterministic. Mixed-width composition is outside this revision. For an eight-cell north run with zero reservations, a verified one-cell repeat yields eight modules; a two-cell repeat yields four. Exact run coverage is required after the reservations applicable to its endpoint joins. A short or odd run that cannot fit a configured repeat is a specific unresolved span, not permission to stretch or overlap modules.

Styles describe default floor, north modules, directional caps, and oriented convex and concave joins with the coverage/alignment information they require. Existing IDs, fields, sprite references and rectangle configurations remain usable; absent new concave art must not invalidate a rectangle that never uses it. A missing or incompatible asset affects the placements that require it. Unused missing roles may be reported as style advisories but do not make an otherwise fully resolved room incomplete.

Production verification must establish native-pixel joins and compatible visual extents, including tall wall faces around narrow voids. A known incompatible overlap is an unresolved placement, not a successful preview. No universal minimum hole width or fit for arbitrary artwork is presumed. Synthetic configurations can verify geometry and editor behavior; they cannot establish production seams.

## Authoring and geometry contracts

### Identity and occupancy

Each room has a stable identity, style reference, serialized authored occupancy and explicitly owned derived output. Adjacent equal-style rooms never merge by appearance. Setup preserves painted cells. Editing or replacing generated visuals never changes occupancy.

Erasing a bridge may split one room's painted cells into disconnected components. All components retain that room's identity; generation covers each component's own boundaries. Diagonal-only contact does not create a passage or shared interior edge. The boundary branches remain distinct at a touching vertex; incompatible artwork there produces a local junction issue rather than a merged contour or a new room ID.

### Geometry and visible result

For every occupied cell, each cardinal side facing a cell not owned by that room is a boundary. Shared sides inside the same room are absent. Directional runs and oriented convex/concave joins describe the actual boundaries, including holes; results are deterministic and independent of sprites or enumeration order.

Cutouts, L/U shapes, notches and holes are admitted cell geometry. Valid geometry is distinct from the ability of a particular style to render every span or join. Full-height wall faces belong only on north-facing boundaries; west/east/south boundaries use thin caps. This orientation applies equally to exterior boundaries and holes. Joins must not duplicate wall faces.

Examples using cells x=0..7, y=0..5:

- Full rectangle: 48 occupied cells; north/south lengths 8, east/west lengths 6; four convex joins.
- Remove interior cell (6,4): 47 occupied cells; the outer boundary is unchanged and the hole adds four unit sides and four concave joins. With a compatible style, floors cover exactly those 47 cells and architecture follows both boundaries. If a particular hole join cannot render, only dependent placements are unresolved; the remaining floor stays visible.
- Remove corner cell (7,5): 47 occupied cells with a notch, five convex joins and one concave join. Geometry is admitted even if its short wall runs cannot fit a selected two-cell repeat.
- Empty occupancy: valid empty result, no owned generated floor/architecture or unresolved-layout guides.

### Editing and recovery

Paint, Erase and the explicit Fill/Erase box modes continue through the room palette/brush. Box Erase needs no Shift gesture; native Shift behavior may remain an optional route. Each completed box is one edit/generation batch. Completed supported edits produce the current complete or incomplete result in Edit Mode. A continuous paint stroke remains one Undo gesture.

Rebuild uses current authored cells and style, replacing only owned output. Identical inputs yield equivalent valid placements, issue identities/locations and output counts with no duplicates. Entire-room regeneration is acceptable. Obsolete boundaries and invalid visuals are removed; a previous successful wall must not remain looking current after a safe layout change invalidates it.

Corrections made through supported room-edit actions refresh at completion. Explicit Rebuild applies style/configuration corrections and occupancy edits made outside the supported brush. The guide and Inspector must make this recovery boundary clear; automatic observation of every stock/third-party tool or arbitrary asset edit is not promised.

## Partial preview and diagnostics

### Observable outcomes

- **Complete:** nonempty current occupancy is fully represented by valid floor and required architecture, with no unresolved required placement issues. This establishes configuration/placement completeness, not independently verified production pixel seams.
- **Incomplete preview:** safe generation has unresolved required floor, span, join or asset issues. Keep independently valid current placements and show the unresolved areas. A local error must not blank the whole room or architectural layer.
- **Empty:** no occupancy and no owned generated visuals; no style required to represent emptiness.
- **Refused:** room-wide ownership, transform/coordinate or overlapping-room ambiguity prevents safe generation. Do not perform unsafe destructive writes or choose a winning room. Clearly label any retained previous output as not updated; do not present it as a current successful preview. Refusal is distinct from an ordinary missing sprite.

These outcomes and issues are observable alongside the existing room identity and diagnostic summary. The data contract identifies the owning room, issue kind, affected cell/run/join or relevant style field, and a useful reason or corrective action. Equivalent inputs yield stable issue identities and ordering. A room-wide configuration issue uses an explicit room/field location rather than an invented boundary coordinate.

### Valid partial output

If the floor asset is admitted, generated floor exactly matches current occupancy even when architecture is incomplete. A missing/invalid floor removes incompatible floor output but retains authored-cell visibility through editor-only guides and allows independently valid architecture. A missing style yields visible authored-cell guides and an actionable missing-style issue, without substitute production art.

A span or join is valid only when its own assets, coverage and dependent endpoint constraints are satisfied. Report dependent failures at their actual affected locations; retain unrelated resolved spans/joins. A run requiring exact coverage is not partially filled with an arbitrary prefix or guessed filler when the whole run is uncoverable. Missing a join does not automatically invalidate unrelated architecture, and missing unused concave assets does not prevent rectangular generation.

### Feedback and selection

The room Inspector shows Incomplete preview and its issues rather than only a generic failure string. Unresolved boundary spans/joins have red editor-only guides and specific markers; the issue text/icon distinguishes them without relying solely on red. Missing-floor guides show all current occupied cells without making them runtime walkable tiles.

Selecting an Inspector issue focuses/highlights its affected region in the Scene view or identifies its room/style field for room-wide configuration issues. Selecting an issue or displaying guides does not edit occupancy/output, dirty the scene, trigger generation, or add an Undo step. Guides belong to the relevant selected/edited room and cannot become production sprites, colliders or runtime floor coverage. Last-valid-room ghosting is not a required feature or default preview mode.

### Validation, persistence and correction

Validate Room is a read-only assessment of current inputs. It may display current issues and guides, but cannot modify serialized occupancy, generated output, persisted generation result or scene dirtiness. If stored output differs from current inputs, validation must not label that stored output as a current Complete result. Rebuild remains the explicit action that synchronizes unsupported external changes.

Correcting a diagnosed problem and rebuilding removes obsolete issues/guides and restores the corresponding output without manual cleanup. Adding/removing an issue cannot change another room or unrelated object.

Undo/Redo restores occupancy, owned visuals and the associated completed-result status/issue state together. Reopening a scene preserves identity, style, cells and generated complete/partial output; associated editor diagnostics must be available consistently without destructive generation on reopen. Selection/highlight alone is transient and need not persist. Runtime loading and Play Mode do not regenerate rooms or run editor guides.

## Compatibility and integration

Current room/style identities, authored cells, existing asset GUIDs, serialized references and assembly ownership remain intact. Added join configuration and result/issue data must admit existing rectangle-only scenes/styles without converting or repopulating their authored cells. Existing diagnostic consumers retain a meaningful summary while structured issues become available to editor tools.

Partial output and issue feedback remain part of the room-owned authoring capability, with no new gameplay room API, global room manager, service locator, or gameplay persistence dependency. Editor UI/guides introduce no UnityEditor dependency into runtime assemblies. No room-specific branches are added to existing composition, interaction, door, collision or landing systems.

Keep the authoring mask excluded from runtime/save/build walkable-floor detection. Floor and non-floor landing queries continue to use actual generated floor coverage; holes must not become walkable through occupancy guides or architecture. This revision does not generate collision boundaries or make incomplete previews production-verified rooms.

## Acceptance criteria

S1–S23 retain their IDs. These are targets, not verification claims for this revision.

- **S1 — Reusable styles:** Two room objects can reference one style asset; changing its admitted visual asset configuration requires no geometry-code change and does not alter either room's occupancy or identity.
- **S2 — Explicit identity:** Two adjacent equal-style rooms retain distinct serialized identities and occupancy. Ambiguous identity/ownership is diagnosed and no destructive generation occurs until resolved.
- **S3 — Authoritative occupancy:** Deleting/replacing generated output does not change authored cells; Rebuild can reconstruct it. Editing generated visuals never adds/removes occupancy.
- **S4 — Grid and native art:** Accepted floor, wall, and cap configurations use the table's production dimensions and 32 PPU convention. An eight-cell floor width remains eight world units; wall height does not change occupancy. Generation never modifies artwork, imports, or scale to force a fit.
- **S5 — Create and paint:** In Edit Mode, a designer can create a room, select its style, and use the supplied palette/room brush for Paint, Erase, Box Fill and explicit Box Erase without Shift. An 8×6 fill authors exactly 48 cells; a box operation updates the current complete/partial result once as a batch.
- **S6 — Rectangle geometry:** Occupancy at x=0..7 and y=0..5 yields north/south edge lengths eight, east/west lengths six, exactly four convex corners, and no internal boundaries. Results are independent of sprite appearance and input enumeration order.
- **S7 — Floor coverage:** An admitted floor yields exactly one native 32×32 panel per occupied cell for rectangles and irregular/component layouts, with no outside coverage. The 8×6 rectangle has 48 panels; removing (6,4) leaves exactly 47 and no floor at that hole, even if architecture is incomplete.
- **S8 — North-wall coverage:** A verified eight-cell north run with zero reservations yields four aligned two-cell modules or eight one-cell modules, without stretching or incompatible overlap. An uncoverable run is specifically unresolved; independently coverable runs remain visible, with no arbitrary prefix/filler claimed as complete coverage.
- **S9 — Perspective and joins:** Prepared Standard art uses full-height faces only on north-facing boundaries and thin caps elsewhere, including hole boundaries. Required convex and concave joins connect at native-pixel resolution without unintended gaps, duplicated faces or incompatible overlap; padding/alignment do not change intended cap thickness. Narrow-void incompatibilities are explicitly unresolved rather than silently rendered as correct.
- **S10 — Expansion:** Extending the 8×6 rectangle east by four cells yields 72 authored/floor cells, north/south lengths twelve, east/west lengths six, a moved east boundary and repositioned corners, and no obsolete output from the former east edge.
- **S11 — Erasure and empty state:** Erasing (7,5) from the 8×6 room admits the resulting notch; erasing (6,4) admits a hole. Each leaves exactly the other 47 authored/floor cells with an admitted floor; style limitations affect only dependent architecture and produce partial-preview issues. Erasing all occupancy removes owned output/guides and yields Empty; another room is unchanged.
- **S12 — Deterministic rebuild:** Rebuilding unchanged inputs yields equivalent valid asset references, placements, counts, status and stable issue identities/locations/order, with no duplicate objects/cells and unchanged authored occupancy, including incomplete results.
- **S13 — Missing/incompatible assets:** A required missing or incompatible asset yields a specific located configuration/placement issue. Preserve occupancy, current valid floor/architecture and layout visibility; remove affected invalid/obsolete visuals only within safe ownership. Do not substitute unrelated sprites or modify art/imports. An unused missing concave role does not invalidate a rectangle. Rebuild after correction recovers affected output.
- **S14 — Ownership isolation:** Rebuild, partial failure, refusal or empty cleanup leaves other rooms, unrelated scene objects and non-room Tilemaps unchanged, including matching-name sentinels. Ambiguous ownership, invalid coordinate setup or overlapping rooms produce visible refusal before unsafe writes, without silently merging regions or choosing shared-wall ownership.
- **S15 — Undo/Redo:** Paint, Erase, Box Fill and Box Erase restore occupancy, owned visuals, result status and completed-result issues as single gestures. Cover 8×6 fill, irregular erase, partial asset failure/correction and empty deletion; no stale/duplicate geometry or mismatched issues remain.
- **S16 — Scene persistence:** Saving/reopening complete and incomplete isolated scenes preserves identity, style, cells, valid generated placements/counts and consistent associated diagnostics. Rebuild remains idempotent; no manual architecture repair or destructive automatic rebuild is needed on reopen.
- **S17 — Validation and recovery:** Validate reports current located issues without changing authored/generated/persisted-result data or scene dirtiness. Selection and guides are read-only. Rebuild recovers deleted generated visuals and current externally edited occupancy/configuration, clearing resolved issues; stale stored output is not labelled current Complete by validation.
- **S18 — Editor lifecycle:** Supported edits generate only the targeted room in Edit Mode. Guides, issue selection and missing-floor layout visualization remain editor-only and add no runtime synchronization/regeneration loop, floor candidates, colliders or unintended UnityEditor assembly dependency.
- **S19 — Runtime compatibility:** Play Mode/loading performs no regeneration and introduces no false floor from occupancy, guides or architecture. Landing at generated floor succeeds under existing rules; a real hole/non-floor position does not gain coverage. Existing blockers, triggers, doors, interactions, composition and references retain behavior without room-specific branches.
- **S20 — Preservation:** Existing production scenes, prefabs, art/imports, asset GUIDs, assembly boundaries, and unrelated working-tree changes are preserved. The demonstration does not replace a production scene or join shipping build entries.
- **S21 — Verification evidence:** Automated checks cover exact rectangle/irregular geometry, joins, coverage, determinism, partial/refused/empty outcomes, ownership, correction, Undo/Redo and persistence; runtime compatibility is checked where affected. Native editor evidence covers partial-preview guides/selection and gestures. Production native-pixel evidence separately establishes S9. Record criterion IDs and exact source/configuration; unavailable checks are Not run.
- **S22 — Demonstration and guide:** An isolated nonshipping Standard demo and guide show 8×6 creation, expansion, notch/hole editing, explicit box erase, incomplete-preview issue selection/correction, Validate/Rebuild, Undo/Redo and save/reopen without manual architecture edits. Synthetic fixtures establish editor behavior while Standard art is pending; they are labelled accordingly and do not satisfy production visual acceptance.
- **S23 — Asset readiness and honest completion:** Verify actual Standard floor/wall/cap/convex/concave assets, native geometry/density, repeats/anchors/reservations and rendered straight/inner/outer joins, including narrow-void limitations, before claiming the finished Standard room. Missing prepared art or visual evidence blocks production visual completion; compilation or substitute fixtures do not satisfy it.
- **S24 — Irregular boundary geometry:** For L/U shapes, notches, holes and negative-coordinate layouts, exposed sides equal occupied-to-empty cardinal adjacencies, with no internal edges. Oriented convex/concave joins and runs cover the actual boundaries deterministically. The specified interior-cell hole adds four unit sides/four concave joins; corner-cell notch yields five convex/one concave join. Neither is rejected merely for being nonrectangular.
- **S25 — Components and diagonal contact:** Removing a bridge retains the original identity/style for all remaining components and resolves each perimeter independently. Diagonal-only contact contributes no shared interior side or passage; touching branches stay separate, with an actionable local issue if art cannot form the junction.
- **S26 — Useful partial preview:** With a valid floor and one missing required corner or unfit wall run, every occupied floor remains visible and independently valid architecture remains. Only invalid/obsolete/dependent placements are withheld, with precisely located unresolved issues. Missing floor or style preserves editor-visible authored cells without substitute production art or runtime coverage. Results clearly distinguish Complete, Incomplete preview, Empty and Refused.
- **S27 — Selectable diagnostics:** Inspector issues and red Scene guides/markers identify room, kind, actual span/join/cell or style field, and corrective reason. Text/icon feedback supplements color. Selecting an issue focuses/highlights its location or field without mutation, generation, scene dirtiness or an Undo step; unrelated rooms' guides do not become the active room's issues.
- **S28 — Correction and dependency isolation:** Correcting an issue and invoking the supported update/Rebuild replaces its guides with valid output and removes that issue. An unavailable endpoint invalidates only placements whose constraints depend on it; unrelated spans/joins remain. Equivalent input produces equivalent issue identities, and repeated correction/rebuild does not accumulate output or stale guides.
- **S29 — Incomplete-state restoration:** Undo/Redo and save/reopen preserve consistent occupancy, valid partial output and associated issue/status state through complete→incomplete→corrected and nonempty→empty transitions. Transient issue selection need not persist. Reopening or displaying diagnostics does not cause a destructive generation or runtime/editor-floor leak.

## Out of scope

Shared-wall resolution between rooms, T/four-way architectural junctions between room owners, mixed-width packing, additional production themes, doors/animations, decoration, material transitions, damage, atmosphere/power integration, runtime room construction, runtime collision changes, incremental regeneration, automatic synchronization with arbitrary tools/assets, a bespoke level-editor window, and broad gameplay refactors remain excluded. Diagonal branch handling in S25 is not shared-wall or multiroom junction resolution.

Art creation, resampling, import migration, guessed production bindings, automatic sprite clipping/cutaways and last-valid-room ghosting are not required or authorized. Existing original artwork is preserved. This specification defines outcomes, not an implementation sequence or permission to bypass the next plan gate.

## Verification status and downstream work

This revision checked intent, selected approach, input digests, stable criterion IDs and internal contract consistency. No new tests, builds, scene reloads, editor gestures or renders were run to verify this expanded target. Prior implementation checks remain historical evidence for their recorded rectangle-only sources/configurations; no changed/new criterion is claimed passed here.

Prepared Standard art, oriented concave joins, short-run/narrow-hole fit and native-pixel evidence remain pending for production visual completion. Source facts are sufficient to define fixture-based geometry/editor work. Native Tilemap event ordering remains unverified and is not a dependency of the selected explicit-brush workflow.

On human approval of this exact specification, revise the implementation plan while preserving completed phase history, inserting the necessary irregular-geometry/partial-preview work before production artwork integration. Do not update old approval hashes merely to make prior plan checks pass.
