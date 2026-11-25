# QRSPI Spec: floor-driven-room-authoring

**Status:** Draft for human review. No implementation is authorized by this draft.  
**Scope:** Phase 1 Standard rectangular-room authoring in the Unity Editor.  
**Revision:** Initial specification; introduces S1–S23. No criteria retired.

## Inputs

Project root: `/Users/jeremyvalentine/SOLITUDE`. Digests are SHA-256 of exact file bytes.

| Input | Project-relative path | SHA-256 |
| --- | --- | --- |
| Settled request and clarifications | `.qrspi/floor-driven-room-authoring/request.md` | `0fa4e38248951fc44c3015791a41bdc2bd08785973c58b451fdc2922ba779610` |
| Research questions | `.qrspi/floor-driven-room-authoring/queries.md` | `c2373ef7b1289c65410aefe6daf48f92fd0ab8ccf0302e6b194722f40fea7eb0` |
| Current-source research | `.qrspi/floor-driven-room-authoring/research.md` | `1cbcd510cc7e3ec0cf8c4cccae17a2886573ba596592f47a1d227f6d3e8e5744` |
| Selected Option A, including storage/asset status | `.qrspi/floor-driven-room-authoring/approach.md` | `cc57207f69bf9fd79538da645acafced98cad6134e710c4e3539d9322ee8e56f` |

The selected approach is a room-specific Tile Palette brush with explicit
generation transactions. The durable architectural record is
`Docs/Architecture/RoomAuthoring.md`; it does not replace this behavioral contract.

Latest request clarifications supersede conflicting dimensions and examples in
the original brief. Research describes existing assets and infrastructure, not
the forthcoming production-art family. Its old eight-cell/two-wall example is
superseded. Production assets are still being prepared; this specification does
not invent asset paths, pivots, cap thicknesses, corner offsets, or verified joins.

## Behavioral delta

| Before, as established by Research | Required new behavior |
| --- | --- |
| Room builders paint fixed scene coordinates. | A designer owns a serialized region and edits it through a room-specific Tile Palette workflow. |
| Flooring and boundaries are not derived from an explicit per-room occupancy model. | Stable room identity and authored occupancy determine all generated visuals. |
| Existing candidate assets have inconsistent density and unverified architectural joins. | A configured style must validate against the production convention; incompatible or absent assets produce diagnostics. |
| Existing brush Undo does not establish restoration of separate room output. | Completed supported edits restore occupancy and generated output together through Undo/Redo. |
| Existing fixed-layout tools do not supply room-scoped output ownership. | Rebuild changes only explicitly owned output and preserves authored scene content. |

Existing builders and production scenes retain their current behavior. This is
new editor authoring capability, not a gameplay-system replacement.

## Grid and asset contract

| Element | Production dimensions | Required convention |
| --- | --- | --- |
| Logical cell | 32×32 pixels | One world unit in X and Y. |
| Floor | 32×32 pixels | One visible panel per occupied cell. |
| North-wall module | 32×64 or 64×64 pixels | Nominal visual extent 1×2 or 2×2 cells. |
| Directional cap canvas | 32×32 pixels | One-cell canvas; visible thickness and structural repeat are configured. |

Production art uses 32 PPU, point filtering, and native proportions. The system
must not stretch, resample, rotate asymmetric art without approval, change the
logical grid, or rewrite import settings to resolve a placement discrepancy.
North-wall height extends northward from the floor boundary and never increases
authored floor occupancy. Structural span, sprite bounds, transparency, pivot,
baseline, and corner reservation are distinct configuration facts.

Each Phase 1 style has one configured structural north-wall repeat width. The
implementation supports styles using either admitted wall size, without requiring
mixed-width packing inside a run. Same-span variants may be configured; selection
is deterministic. A run must be exactly coverable after configured corner
reservations. Unsupported spans are errors, not permission to overlap or scale.

For an eight-cell run with no corner reservation and a verified two-cell repeat,
the expected result is four modules. A verified one-cell repeat requires eight.
These are contract examples, not claims about the pending Standard artwork.

## Authoring and generation contracts

### Room and style

A designer can create a room authoring object, assign a reusable style asset,
and identify its editable occupancy Tilemap. A room's stable identifier is
independent of its style, floor appearance, position, and generated output.
Distinct adjacent rooms may share a style without merging their authored regions.
Ambiguous identity or output ownership prevents destructive generation and is
reported visibly.

A style exposes a stable ID, display name, default floor, north-wall modules,
west/east/south caps, four rectangular corner roles, and their required alignment
and structural coverage configuration. A future door reference is optional and
does not make doors part of Phase 1. Asset substitution or alignment changes are
data changes, not changes to geometry rules. Missing required roles are invalid.

### Editing

Paint adds occupancy; Erase removes occupancy. Box Fill and Box Erase apply their
entire selected region as a completed operation. Every supported completed edit
updates the current room's generated result or its visible invalid-state result
in Edit Mode, without entering Play Mode or manually painting architecture.
Each box operation is one edit/generation batch, not a rebuild per cell.

Painted occupancy remains editable and is not overwritten by setup/build tools.
Generated layers are clearly distinguished from authoring. Changes to generated
visuals cannot change room occupancy and can be replaced by Rebuild. Direct
occupancy edits outside the supported brush workflow have explicit Validate and
Rebuild recovery; automatic synchronization for every third-party tool is not
promised.

### Geometry and rendering result

The geometry result describes boundaries between an occupied cell and cells not
owned by the same room, continuous directional runs, and corners. Adjacent cells
in the same room produce no internal boundary. Geometry is deterministic and
can be checked without rendering a scene. Its occupancy representation accepts
arbitrary cell sets even though Phase 1's complete architectural rendering is
restricted to a single supported rectangle.

Valid generation produces visible floor at every occupied cell, full-height
north-facing wall faces only along the north boundary, thin west/east/south caps,
and the four connecting corner roles. Padded sprites use their configured visual
alignment; corners must not duplicate north-wall faces. The saved result is
inspectable and grouped by explicit room ownership.

Rebuild uses authored occupancy and style configuration, removes obsolete owned
output, and leaves unrelated content intact. Identical inputs produce equivalent
assets, placements, and counts without duplicate cells or objects. Entire-room
regeneration is acceptable; incremental regeneration is not required.

### Validation and failure

Validate Room is nonmutating: it reports missing references, invalid import
conventions, unsupported geometry or spans, overlapping authored room regions,
and ambiguous ownership without changing occupancy, generated output, or scene
dirtiness. Generation returns success or actionable diagnostics identifying the
room and failing configuration/region. Validation never creates replacement art.

For an invalid style or unsupported shape with unambiguous ownership, a completed
edit or Rebuild preserves the authored cells and clears that room's generated
output, with a visible invalid-state diagnostic. Old walls or flooring must not
remain presented as the current successful result. Empty occupancy is a valid
empty result and removes owned output. Ownership ambiguity instead refuses
destructive writes and visibly marks the result invalid until resolved.

Once valid inputs are restored, Rebuild reconstructs complete output without
manual cleanup. Failures never delete another room's or an unrelated object's
content. A failed edit's occupancy/output changes remain part of its Undo/Redo
transaction.

### Persistence and existing runtime behavior

Undo restores occupancy and generated output before a completed edit; Redo
restores both after it. Successful edits mark their scene modified. Saving and
reopening preserves room identity, style, occupancy, and generated architecture.
Runtime loading or entering Play Mode does not regenerate the room.

The authoring occupancy representation must not introduce runtime walkable-floor
coverage or affect existing player awakening/landing queries. Generated
architectural visuals must not be treated as floor by those queries. Existing
floor detection on visible flooring, collision behavior, interactions, doors,
composition boundaries, and saved references remain unchanged.

## Data and integration changes

New serialized authoring data consists of room identity, style reference,
occupancy, and ownership of derived output. Reusable style data describes asset
roles and placement information independently of room occupancy. Generated data
is disposable and reconstructable; authored occupancy is durable.

New editor operations are Create Room, the supported painting operations,
Validate Room, and Rebuild Room. Validation/generation diagnostics are observable
contracts; no existing gameplay API or persistence schema gains a room dependency.
Editor-only behavior must not introduce UnityEditor dependencies into runtime
assemblies. Existing Domain/Application boundaries and asset GUIDs are preserved.

## Acceptance criteria

All criteria are initial IDs. They describe required outcomes, not tests already run.

- **S1 — Reusable styles:** Two room objects can reference one style asset; changing its admitted visual asset configuration requires no geometry-code change and does not alter either room's occupancy or identity.
- **S2 — Explicit identity:** Two adjacent equal-style rooms retain distinct serialized identities and occupancy. Ambiguous identity/ownership is diagnosed and no destructive generation occurs until resolved.
- **S3 — Authoritative occupancy:** Deleting/replacing generated output does not change authored cells; Rebuild can reconstruct it. Editing generated visuals never adds/removes occupancy.
- **S4 — Grid and native art:** Accepted floor, wall, and cap configurations use the table's production dimensions and 32 PPU convention. An eight-cell floor width remains eight world units; wall height does not change occupancy. Generation never modifies artwork, imports, or scale to force a fit.
- **S5 — Create and paint:** In Edit Mode, a designer can create a room, select its style, and use the supplied palette/room brush for Paint, Erase, Box Fill, and Box Erase. An 8×6 fill authors exactly 48 cells; a box operation updates output once as a batch.
- **S6 — Rectangle geometry:** Occupancy at x=0..7 and y=0..5 yields north/south edge lengths eight, east/west lengths six, exactly four convex corners, and no internal boundaries. Results are independent of sprite appearance and input enumeration order.
- **S7 — Floor coverage:** With an admitted style, an 8×6 room produces 48 native 32×32 floor panels aligned to its occupied cells, with no floor outside occupancy and no sparse floor-detection holes.
- **S8 — North-wall coverage:** For a verified eight-cell north run with zero corner reservation, a two-cell configured structural repeat produces exactly four aligned wall modules; a one-cell repeat produces eight. Neither result stretches or overlaps modules. Uncoverable runs are diagnosed.
- **S9 — Perspective and joins:** With the prepared Standard family, north faces extend northward and west/east/south use thin caps. All four corners connect at native-pixel resolution without unintended gaps, duplicated faces, or incompatible overlap. Padding and configured pivots/offsets do not change visible cap thickness.
- **S10 — Expansion:** Extending the 8×6 rectangle east by four cells yields 72 authored/floor cells, north/south lengths twelve, east/west lengths six, a moved east boundary and repositioned corners, and no obsolete output from the former east edge.
- **S11 — Erasure and empty state:** Erasing an exterior cell updates the geometry for the remaining 47-cell set. If the new shape is unsupported, occupancy persists, owned output is cleared, and a visible diagnostic replaces a successful preview. Removing all occupancy yields no owned generated geometry; another room is unchanged.
- **S12 — Deterministic rebuild:** Rebuilding twice without changing inputs yields equivalent asset references, placements, and counts, with no duplicate objects/cells and unchanged authored occupancy.
- **S13 — Missing/incompatible assets:** Removing a required role or using an incompatible admitted-art convention produces a specific configuration error. Generation preserves occupancy, clears only unambiguously owned output, and never substitutes unrelated sprites or creates/changes artwork. Restoring valid configuration permits recovery by Rebuild.
- **S14 — Ownership isolation:** Rebuilding, failing generation, or clearing one room leaves other rooms, unrelated scene objects, and non-room Tilemaps unchanged, including unrelated objects with matching names. Overlapping regions are diagnosed rather than silently merged or given shared-wall resolution.
- **S15 — Undo/Redo:** Undo and Redo of Paint, Erase, Box Fill, and Box Erase restore both authored and generated states as completed edit transactions. Include an 8×6 Box Fill and an edit that becomes unsupported; no stale or duplicate geometry remains after restoration.
- **S16 — Scene persistence:** Saving and reopening the isolated demo preserves room identity, style references, occupancy, native placements, and output counts. Rebuild after reopen remains idempotent and does not require hand-editing generated architecture.
- **S17 — Validation and recovery:** Validate Room reports the specified error categories without changing authored/output state or scene dirtiness. Inspector Rebuild recovers output from valid occupancy/style after generated visuals were removed or a supported edit was made outside the room brush.
- **S18 — Editor lifecycle:** The supported workflow runs without Play Mode, rebuilds only the targeted room after completed operations, and introduces no runtime synchronization/regeneration loop or unintended UnityEditor dependency in runtime assemblies.
- **S19 — Runtime compatibility:** Entering Play Mode/loading the authored demo introduces no false floor coverage from occupancy or architecture. Existing awakening/landing results at floor and non-floor positions, collisions, doors, interaction, and composition references remain valid without room-specific gameplay branches.
- **S20 — Preservation:** Existing production scenes, prefabs, art/imports, asset GUIDs, assembly boundaries, and unrelated working-tree changes are preserved. The demonstration does not replace a production scene or join shipping build entries.
- **S21 — Verification evidence:** Automated checks cover geometry, exact module coverage, determinism, expansion/erasure, missing assets, ownership, editor Undo/Redo, and persistence; runtime compatibility is checked where affected. Native-pixel visual evidence separately establishes S9. Evidence names criterion IDs and the tested source/configuration revision; unavailable checks are Not run.
- **S22 — Demonstration and guide:** An isolated nonshipping 8×6 Standard room and a short guide show creation, style selection, painting/box editing, resizing, validation/rebuild, supported-width limits, Undo/Redo, and recovery, without manual edits to generated architecture.
- **S23 — Asset readiness and honest completion:** Actual Standard floor/wall/cap/corner identities, imported geometry/density, structural repeat/offsets, and rendered joins are verified before claiming the finished Standard room. Missing prepared art or absent visual evidence remains an explicit blocker to visual completion; compilation or substitute fixtures do not satisfy it.

## Relationship to the original requirement

S1–S4 preserve reusable style, explicit room ownership, authored/generated
separation, and the latest grid convention. S5–S17 cover FR-003 through FR-009
and the original geometry/regeneration/editor tests T-001 through T-008. The
original T-002 example using two 128-pixel modules is replaced by S8's current
one- or two-cell structural repeats. S9 and S23 preserve visual correctness as a
hard requirement. S18–S22 cover lifecycle, compatibility, preservation, evidence,
and authoring documentation from the original nonfunctional requirements and
AC-01 through AC-15. No previous S criteria exist to retire or renumber.

## Out of scope

Phase 2+ irregular architectural rendering, concave corners, T-junctions,
shared-wall resolution, mixed-width wall packing, additional production themes,
doors/animations, decoration, material transitions, damage, atmosphere/power
integration, runtime room construction, runtime collision changes, incremental
regeneration, a bespoke level-editor window, and broad gameplay refactors are
excluded. A general cell-set data model or explicit diagnostics do not constitute
implementation of those future capabilities.

Art creation, resampling, import migration, and guessed Standard asset bindings
are excluded. The generator consumes separately prepared, verified art. This
draft neither authorizes changing old source artwork nor assumes prepared copies
already exist.

## Pending evidence and verification status

The behavioral target is defined independently of exact final asset filenames.
Production asset availability, native imported bounds, structural spans, cap
thickness/repeat, and corner baselines remain pending. Their values must be
established from the prepared assets rather than guessed in code. Refresh the
relevant Research evidence before approving asset-dependent implementation scope;
if the assets cannot satisfy this contract, return to Shape/Spec for a reviewed
correction.

Native Tilemap notification timing remains unresolved and is not a dependency of
the selected explicit-brush behavior. Editor lifecycle/Undo behavior and existing
runtime floor-query compatibility still require implementation verification.

**Tests, builds, scene reload experiments, and native-pixel renders: Not run.**
Only the document's IDs, input digests, and internal consistency were checked.
No acceptance criterion is claimed to have passed. The next human gate concerns
this exact specification; no implementation plan or product changes are included.
