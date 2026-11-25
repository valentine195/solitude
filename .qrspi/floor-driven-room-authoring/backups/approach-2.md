# QRSPI Approach: floor-driven-room-authoring

## Context

Phase 1 introduces editor tooling: select a room style, paint an explicitly owned floor region through Unity's Tile Palette, and derive visible floors, north wall faces, thin west/east/south caps, and four rectangular-room corners in one deterministic process. Painted occupancy and stable room identity remain authoritative. Existing gameplay composition, doors, collision behavior, production scenes, and serialized references remain outside this feature's changes.

The latest human clarification in `request.md`, **Production dimensions and logical footprints — 2026-10-19**, establishes the shaping baseline:

| Element | Production dimensions at 32 PPU | Logical footprint |
| --- | --- | --- |
| Logical cell / Unity Grid cell | 32×32 pixels / 1×1 world units | 1×1 |
| Floor panel | 32×32 pixels | 1×1 cells |
| North-wall module | 64×128 pixels | 2×4 cells |
| Cap canvas | 64×64 pixels | 2×2-cell canvas; structural repeat and thickness unestablished |

Original artwork is preserved. Separate 50% production copies are the proposed art-preparation route from the discussed 128×256 walls, 128×128 caps, and 64×64 floors. The room generator consumes prepared production assets at native scale; rebuilding does not resize artwork. A four-cell north-wall height extends outside the floor footprint and does not consume four rows of occupancy.

This supersedes the earlier proposed two-by-two-cell floor policy and four-cell-wide wall modules in `backups/approach-1.md`. One floor panel now corresponds directly to one occupied cell. An eight-cell north boundary would use **four** two-cell-wide modules, rather than the original brief's two 128-pixel modules. This is dimensional arithmetic under the clarified convention, not proof that available sprites assemble correctly.

The prior approach's architecture recommendation remains sound. The new footprint convention removes its unresolved floor-policy choice, while conforming production art and rendered seam evidence remain necessary. Shape makes no art/import changes and supplies no implementation or visual-test result.

**Prior approach selection: None.** `backups/approach-1.md` records no human selection of Option A or Option B, so none is invented here. Its recommendation and rationale for explicit brush transactions are preserved below.

**Recorded human direction on dimensions:** the latest request clarification selects a 32×32 logical grid, one-cell production floors, two-by-four-cell production walls, preservation of originals, and consideration of separate half-size production copies. The user authorized revising Shape on that basis. This resolves the previous floor-policy decision; it is not evidence that production assets are ready or that this approach revision has been approved for implementation.

## Constraints and Facts

- The current starting-room builder creates a fixed-coordinate scene with Floor, North Wall, and Rule Boundaries Tilemaps. Its boundary RuleTile stores one rectangle and depends on neighboring boundary tiles; neither is an occupancy-driven room engine. Sources: `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:55`, `:66`, `:75`; `Assets/_Project/Features/World/SolitudeStart/SolitudeBoundaryRuleTile.cs:11`, `:16`, `:30`.
- Unity 6000.6.4f1, Tilemap 1.0.0, Extras 9.0.1, and Test Framework 1.8.0 are installed. GridBrush exposes Paint, Erase, BoxFill, and BoxErase; BoxFill batches Tilemap writes. Its editor registers Undo before brush actions and exposes target selection. Sources: `ProjectSettings/ProjectVersion.txt:1`; `Packages/manifest.json:3`; `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrush.cs:137`, `:159`, `:182`, `:203`, `:218`; `Editor/GridBrushEditor.cs:658`, `:674`, `:987` in that package.
- Internal Tilemap sync buffering exists, but Research did not establish public notification signatures, native dispatch timing, or Undo/Redo ordering. Buffering alone does not prove one notification per completed gesture. Source: `research.md`, Tilemap notification answer; `Library/PackageCache/com.unity.2d.tilemap@f4c7bd71ac8a/Editor/GridBrushEditorBase.cs:107`.
- Current candidate full-height Aseprite wall imports are 128 pixels wide, 238 or 256 pixels high, and **100 PPU**. Earlier floor PNG slices are 128×128 at 128 PPU with Bilinear filtering. These findings do not identify a prepared 32 PPU production family. Sources: `research.md`, asset-identification answer; `Assets/_Project/Art/Tiles/Walls/Sprites/wall_panel_1.aseprite.meta:39`, `:130`; `Assets/_Project/Art/Tiles/Floors/Floor_Tilemap.png.meta:40`, `:54`, `:118`.
- Some source wall panels have transparent top rows that the importer trims. Candidate caps/corners have different pivots and source extents, including a negative Y pivot for `wall_top`. The existing evidence identifies neither a selected Standard family nor a rendered four-corner seam fixture. Source dimensions, imported bounds, and visible structural baselines must remain distinct. Sources: `research.md`, asset-identification and seam/baseline answers; `Assets/_Project/Art/Tiles/Walls/Sprites/wall_top.aseprite.meta:132`; `wall_bottom_left_corner.aseprite.meta:132` in that directory.
- Research's existing eight-cell/two-module compatibility question uses the earlier dimensions. Its metadata and density mismatch findings remain useful, but its proposed assembly count is superseded by the latest request. It does not establish the existence or correctness of half-size production copies.
- `SurvivorAvatarAdapter` searches active same-scene Tilemaps without Collider2D as floor candidates and queries `HasTile`. An invisible occupancy Tilemap can affect this behavior; wall/cap Tilemaps can also be mistaken for floor. Sources: `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:65`, `:75`.
- Existing builders provide no dedicated generated-room ownership guard or complete generated-content Undo workflow. Hierarchy names alone do not distinguish owned generated objects from unrelated authored content. Sources: `research.md`, ownership and Undo answers; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:35`; `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:66`.
- Feature-owned scripts/data belong with their feature, reusable tiles/palettes under Art/Tiles, nonshipping demos outside shipping entry lists, and screenshots/exports outside Assets. Feature-local Editor scripts use an assembly reference; the existing Edit Mode assembly references Editor, Runtime, Domain, and Application. Sources: `README.md:15`, `:39`, `:63`, `:71`; `Assets/_Project/Features/World/SolitudeStart/Editor/SOLITUDE.Editor.asmref:1`; `Assets/_Project/Tests/Editor/SOLITUDE.Tests.EditMode.asmdef:2`.

## Candidate Approaches

### Option A: Room-specific brush with explicit generation transactions

**Shape.** Each room has an explicit stable identity, a style reference, and a dedicated serialized occupancy Tilemap. A custom Tile Palette brush targets the selected room's occupancy map and passes completed edits to a deterministic room generator. Box Fill/Erase are batches; Inspector Rebuild uses the same generation path. Integer-cell boundary extraction and continuous runs remain separate from style-dependent placements and Unity scene writing.

Visible floors use a separate Tilemap with an ordinary Tile or appropriate RuleTile, one native 32×32 floor panel per occupied cell. Large wall modules, padded caps, and structural corners use explicitly owned SpriteRenderers. Style metadata describes their anchors, structural coverage, and alignment offsets independently of canvas dimensions. The clarified 64×128 wall naturally covers two logical columns at identity scale. No runtime generation loop is needed.

**Fit and compatibility.** This matches the requested GridBrush extensibility while making operation boundaries explicit without depending on unverified native notifications. Per-room occupancy preserves room identity when styles match or rooms touch. The geometry model can represent arbitrary occupancy and neighboring owners while Phase 1's visual resolver limits completion to supported rectangular geometry and native module coverage. Shared-wall resolution remains future scope. No new composition or Domain/Application branch is required.

The authoring mask needs an authoring-only lifecycle that excludes it from runtime floor detection; merely disabling its renderer is insufficient. Keeping its container inactive outside editing is a plausible local approach, but editor activation, saved state, and Play Mode behavior require verification. SpriteRenderer architecture avoids introducing additional false floor Tilemaps. The clarified one-panel-per-cell visible floor also avoids the sparse `HasTile` gaps of the previous two-cell floor proposal.

**Complexity and operational behavior.** Brush/editor code must coordinate target selection, complete edit batches, scene dirtiness, and Undo for occupancy plus owned generated content. This is a bounded amount of custom tooling, rather than an elaborate editor window. Undo should restore the recorded transaction as a unit, without relying on an unrecorded destructive rebuild during notification callbacks. Stroke grouping and generated-object restoration need executable editor evidence.

**Failure modes.** Other tools can bypass the supported brush workflow, leaving output out of date. Clear validation and explicit Rebuild provide recovery; generated layers must not become alternative authoring sources. Resolve placements before scene writes, retain authored occupancy on failure, and make obsolete owned architecture visibly invalid or remove it rather than presenting stale walls as current. Empty occupancy can yield empty output. Diagnose room overlap and unsupported geometry without implementing future shared boundaries. Deletion authority comes from room ownership references/markers, never object names.

**Testing and migration.** Pure boundary and placement descriptions permit focused deterministic tests. Editor fixtures exercise batch edits, idempotence, ownership, Undo/Redo, mask exclusion, and save/reload. A native-pixel room render remains necessary for cap seams and baselines. An isolated demo and reusable palette need no production-room conversion. Existing builder assets and GUIDs remain intact; generated output can be removed and rebuilt from its room mask.

### Option B: Stock brush with an occupancy-change observer

**Shape.** Keep the same per-room mask, identity, style configuration, geometry engine, and mixed visual writer. Designers use Unity's ordinary Tile Palette brush. An editor observer filters occupancy notifications, coalesces changes, and regenerates after editing operations and Undo/Redo.

**Strengths.** The stock brush remains familiar, and direct Tilemap edits can be observed without a room-specific brush integration for every tool. The data model and prepared-art convention are equally compatible with this option. A later switch from Option A could preserve room occupancy and styles.

**Costs and operational risks.** Research has not established the timing needed to distinguish completed batches from intermediate writes or order generation against Undo restoration. Coalescing introduces delayed previews, repeated rebuilds, queued work for destroyed/unloaded rooms, recursive notifications, and Undo-group risks. Scene/domain reload and multiple-room filtering add lifecycle complexity. Broad scene scanning or runtime synchronization would conflict with the requested editor-first approach.

**Failure modes, testing, and migration.** This option inherits Option A's asset, ownership, geometry, and visual-verification concerns. It additionally needs targeted Research and experiments on public event availability, batching, callback reentrancy, Undo/Redo, reloads, and cancellation. It offers broader stock-tool interoperability, but its synchronization surface is larger and currently rests on unresolved evidence. It still requires no production-scene migration.

## Recommendation

Recommend **Option A**, retaining the rationale from the prior approach: explicit brush transactions fit Phase 1's modest editor-first workflow and make ownership, batching, and Undo behavior easier to reason about and verify. Native Tilemap notification timing need not become a Phase 1 dependency. Option B remains a future alternative if stock-brush interoperability justifies the extra investigation and synchronization machinery.

Use the mixed writer: an editable occupancy Tilemap, a generated visible floor Tilemap, and owned SpriteRenderers for architecture. Geometry derives directly from the occupancy mask in one deterministic process. Connected RuleTiles are suitable where they select compatible tile visuals, but must not create a recursive floor-to-wall-to-cap authority chain. Reusing the current rectangle-boundary RuleTile as the core solver would preserve the wrong coupling.

Adopt the clarified production convention directly: 32-pixel cells, 32×32 floors, 64×128 north walls, and 64×64 cap canvases at 32 PPU. The earlier proposal to require even floor extents for two-by-two flooring is withdrawn; a floor now covers each occupied cell. Wall runs still need exact coverage by the configured native modules. Cap/corner coverage constraints cannot be derived from the canvas and remain evidence-dependent.

Preserve original artwork and prepare separate production copies through a distinct art-preparation scope. Half-size copies are the selected direction to evaluate, not evidence that naive downsampling preserves pixel details, seams, pivots, or asymmetric corner joins. The generator should neither scale transforms to disguise nonconforming imports nor modify artwork during rebuild. Visual completion of Standard remains blocked until the family is identified, prepared, and verified; this approach does not assume that current imports meet the clarified convention.

Likely ownership remains `Assets/_Project/Features/World/RoomAuthoring` for feature types/data and its guide, its Editor subfolder with the existing Editor assembly reference, `Assets/_Project/Art/Tiles` for reusable tiles/palettes, the existing Edit Mode owner for tests, and a nonshipping demo scene. Prepared reusable production sprites follow the repository's Art ownership rules; originals retain their current identity. These identify change owners, not implementation steps.

## Decision

None.

## Open Questions

1. **Production asset identity/readiness — return to Research with updated questions and source roots.** Which exact source floor, north-wall, cap, and corner assets comprise Standard, and which prepared production copies exist? Establish actual imported dimensions, PPU, filtering, trimming, pivots, and GUID/local sprite identities against the clarified 32×32 / 64×128 / 64×64 convention. The original research did not verify those copies. If preparation is still needed, its art/import scope must be resolved separately from Shape.
2. **Visual compatibility — return to Research after production assets are available.** Establish visible baselines, alignment offsets, cap repeat distances, corner reservations, and native-pixel evidence for an 8×6 assembly with four two-cell north-wall modules. Check the half-size artwork's detail and seams, including trimmed/padded sprites. A 64×64 cap canvas does not prove a two-cell repeat or a clean four-corner assembly.
3. **Notification behavior — deferred unless Option B is selected.** Establish the installed Editor's public notification signatures, timing, batching, and Undo/Redo ordering if the observer approach is chosen. This evidence gap does not block choosing Option A.

Brush gesture/Undo behavior, runtime exclusion of the occupancy mask, output ownership, persistence, and native seam correctness require implementation verification later. No tests or renders were run during this shaping revision; no acceptance criteria, behavioral spec, or implementation sequence is supplied here.
