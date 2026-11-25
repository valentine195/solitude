# Floor-Driven Room Authoring

**Status:** Selected architectural direction; implementation deferred while production artwork is prepared.  
**Requirement:** SOL-ENV-001  
**Last updated:** 2026-10-19

This document preserves the selected room-authoring approach independently of
disposable QRSPI artifacts. It is a design record, not an implementation report,
approved behavioral specification, or claim of visual correctness. No room
authoring implementation or production-art preparation has been completed by
this workflow.

## Purpose and designer workflow

A designer creates an explicitly identified room, assigns a room style, and
paints or box-fills its floor occupancy using a reusable Unity Tile Palette and
a room-specific brush. A completed edit derives visible flooring, north-facing
wall faces, thin west/east/south caps, and rectangular-room corners from that
occupancy. Erasing or resizing a room updates the derived architecture.

Painted occupancy is authoritative. Generated floors, walls, caps, and corners
never become inputs to subsequent generation. Designers do not paint each
architectural layer separately.

## Grid and art convention

| Element | Production pixels | Extent at 32 PPU |
| --- | --- | --- |
| Logical cell | 32×32 | 1×1 world units |
| Floor panel | 32×32 | 1×1 logical cells |
| Narrow north-wall module | 32×64 | 1×2 logical cells |
| Wide north-wall module | 64×64 | 2×2 logical cells |
| Cap canvas | 32×32 | 1×1 logical-cell canvas |

Use a unit-size logical Grid and prepared production assets at 32 PPU and native
scale. North-wall faces extend northward outside the playable floor footprint;
their visible height does not add rows to room occupancy.

Canvas size, imported sprite bounds, visible artwork bounds, and structural
coverage are separate measurements. A 32×32 cap canvas does not establish its
visible thickness, repeat distance, or connecting edge. Module alignment must
account for transparency, trimming, pivots, corner reservations, and explicit
pixel offsets.

Preserve original artwork and its GUIDs. Any production copies are prepared and
reviewed separately. There is no universal downsampling factor for the current
dimension targets. The room generator must not resize artwork, stretch sprites,
rotate asymmetric assets without approval, or change import settings to make a
room fit.

## Selected approach

**Option A: a room-specific Tile Palette brush with explicit generation
transactions.** This direction was selected after comparison with a stock-brush
change observer. Explicit brush operations provide a clear boundary for batch
generation and Undo, avoiding a dependency on unverified native Tilemap
notification timing.

Each room owns a stable identity, a reusable style reference, a serialized
occupancy Tilemap, and explicit references to its generated output. Adjacent
rooms remain distinct even when they use the same floor tile or style.

Use Tilemaps for authored occupancy and generated visible flooring. Use owned
SpriteRenderers for architectural modules where tall or padded sprites require
explicit placement. Suitable RuleTiles may select connected visual variants;
ordinary Tiles remain appropriate for deliberate variants. Neither mechanism
creates authoritative room geometry recursively.

The saved occupancy Tilemap remains editable. Setup utilities may create
components, reusable assets, palettes, and an initial scaffold, but must not
overwrite painted layouts as the normal authoring workflow.

## Responsibility boundaries

Names below describe responsibilities rather than frozen class or API names.

| Responsibility | Owns |
| --- | --- |
| Room authoring component | Room identity, style, authored occupancy, and generated-output ownership. |
| Room style definition | Floor and architectural asset references, structural spans, anchors, and alignment information. |
| Boundary extraction | Deterministic cell edges, continuous runs, and corner geometry, independent of rendering. |
| Module placement | Exact coverage of supported runs using configured native modules. |
| Scene writer | Application and removal of the current room's owned visual output. |
| Editor integration | Painting, box fill/erase, validation, explicit rebuild, Undo/Redo, and scene persistence. |

Geometry computation produces a deterministic placement description before the
scene writer applies it. This permits meaningful geometry tests without loading
or rendering a complete scene.

For Phase 1, retain one configured structural wall repeat width per style.
Styles can use the narrow or wide wall family, with exact coverage required.
The available pixel dimensions do not authorize arbitrary mixed-width packing
within a run. That policy remains future scope unless explicitly selected.

## Editing, regeneration, and ownership

Paint, erase, box fill, and box erase operate on the selected room's occupancy.
Box operations are batches. Inspector Rebuild uses the same deterministic
generation process. Repeated rebuilds with unchanged inputs produce equivalent
output without duplicate objects or cells.

Ownership references or markers authorize replacement and deletion; hierarchy
names alone do not. Rebuilding one room must preserve other rooms, unrelated
authored objects, and existing production scenes.

Resolve configuration and supported placements before scene writes. On failure,
preserve authored occupancy, report the specific problem, and avoid presenting
obsolete generated architecture as current. Empty occupancy yields empty owned
output. Unsupported irregular layouts or module coverage must produce explicit
diagnostics rather than distorted or silently incomplete architecture.

Treat a completed edit and its generated changes as an Undo transaction. Undo
and Redo must restore occupancy and output together; an unrecorded destructive
rebuild inside a callback is not a substitute. Generated data must survive scene
save/reload. Gesture grouping, restoration behavior, and dirty-state handling
require executable editor verification.

Other tools can bypass the supported brush path. Validate/Rebuild provides a
recovery route, and stale output needs a clear indication. No runtime update loop
or broad scene search should keep editor-generated geometry synchronized.

## Existing integration constraints

Research identified an existing floor-detection heuristic in
[`SurvivorAvatarAdapter`](../../Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs):
active same-scene Tilemaps without a Collider2D can count as floor. Disabling an
occupancy renderer alone does not exclude its Tilemap from that query. The
authoring mask needs an editor-only lifecycle that preserves serialization and
painting while excluding it from runtime floor detection. Its exact lifecycle
requires verification before implementation is accepted.

Architectural SpriteRenderers avoid adding wall/cap Tilemaps to that heuristic.
The generated visible floor has one floor tile per occupied logical cell.

Keep room tooling feature-owned. Preserve GameCompositionRoot, SceneBindings,
Domain/Application assembly boundaries, existing movement, interaction, doors,
collision, and serialized references. Do not introduce room-specific gameplay
branches merely to support authoring.

Likely owners are the RoomAuthoring feature under `Assets/_Project/Features/World`,
its Editor subfolder using the existing Editor assembly reference, reusable tiles
and palettes under `Assets/_Project/Art/Tiles`, and tests under the existing
Edit Mode test owner. An isolated demo is nonshipping. Placement follows the
[project README](../../README.md).

## Alternative considered

**Option B: stock brush plus an occupancy-change observer.** This supports more
direct Tilemap editing, but needs reliable notification timing, coalescing,
reentrancy protection, Undo ordering, reload behavior, and cancellation of queued
rebuilds. Existing research established internal brush buffering, not the full
public/native event-order contract. Those additional dependencies are unnecessary
for the selected Phase 1 approach. The occupancy/style separation leaves this
alternative open for later investigation.

## Initial scope and later extensions

The initial target is a Standard rectangular room, including an isolated 8×6
demonstration, reusable palette, Inspector validation/rebuild, persistence,
Undo/Redo, and a short authoring guide. The occupancy and geometry model should
remain reusable for later irregular rooms without implementing them prematurely.

Advanced geometry, shared boundaries, mixed-width wall packing, additional
themes, doors, decoration, floor transitions, generated collision, runtime
construction, and ship-system integration remain outside the initial scope.
Existing door gameplay is preserved for future integration rather than duplicated.

## Asset readiness and resumption

The user confirmed that the production assets are **still being prepared**.
Their absence does not prevent storing this selected approach. It does prevent
claiming a visually verified Standard room or assuming specific final assets.

When the art is ready, establish:

- Exact floor, wall, directional-cap, and four-corner asset identities and GUIDs.
- Actual imported dimensions, 32 PPU, filtering, trimming, pivots, and native scale.
- Structural wall coverage, repeat widths, cap thickness/repeat, corner
  reservations, and alignment offsets.
- A native-pixel 8×6 assembly showing compatible baselines and joins without
  unintended overlap. Module counts depend on verified structural spans and
  corner reservations, not just canvas widths.

Earlier research found candidate source families with different dimensions,
mixed 32/100/128 PPU, and unusual cap pivots. Those findings describe the then
current project; they do not verify the forthcoming production family. Refresh
the relevant evidence when that family is available.

Resume the established QRSPI workflow from this selected direction, refresh
asset research, and review the behavioral specification and implementation plan
before product changes. Runtime occupancy exclusion and editor Undo behavior also
need verification. No tests, renders, or finished room are claimed by this design
record.
