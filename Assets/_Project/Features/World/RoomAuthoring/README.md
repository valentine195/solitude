# Ship layout and room authoring

Paint **Authored Occupancy** with **ShipRoomBrush**. Painted cells are authoritative;
generated floors and architecture are replaceable. A ShipLayout coordinates multiple
Room/Corridor spaces on one aligned grid and generates each shared partition once.
Connections open explicit gaps without merging space identities. Reusable temporary
art is sufficient; final production artwork is not an acceptance gate. Procedural
floor panel detailing is a later phase. Damage and decorative overlays are outside
the amended scope.

## Create or adopt

1. Choose **GameObject > SOLITUDE > Ship Layout Authoring** in an Edit Mode scene.
   Assign its Default Style. The layout Inspector lets you select the space, set
   Room/Corridor metadata, and choose **Edit Space in Tile Palette**.
2. Existing independent rooms remain usable. In a legacy room Inspector choose
   **Adopt into New Ship Layout** to transfer authority explicitly. Adoption keeps
   the room ID, painted map, style, world cells and output references. Its recorded
   ownership mapping is visible in the Inspectors; one Undo restores independence.
   Resolve any preflight refusal before adopting. Selection/reopen never migrates.
3. Keep room and layout roots unrotated, unit scale and integer-aligned in XY.
   Occupancy/output stay aligned direct children of their room. Do not share IDs,
   map references or output roots between owners.

## Paint and restore

1. Enter **Edit Space in Tile Palette** (or **Edit Room in Tile Palette** for a
   legacy room). Confirm ShipRoomBrush, RoomOccupancy palette and the intended
   Authored Occupancy target. Do not paint the generated floor.
2. Choose **Fill** under Box action and drag cells (0,0) through (7,5) for an
   8×6 region: 48 authored cells and 48 floor cells. Add (8,0) through (11,5) to
   expand it to 12×6, 72 cells.
3. Choose **Erase** under Box action and drag only the cells to remove. No Shift
   modifier is needed. Return to Fill to add cells; ordinary Paint/Erase tools
   remain independent of Box action.
4. A changed gesture generates once when it ends. During painting, pending cell
   additions/removals have editor guides while the last completed architecture
   remains visible. Unchanged/net-zero gestures do not regenerate. Escape cancels
   the unfinished gesture; target/brush changes, save/reload and entering Play
   Mode also cancel unfinished paint. Mouse leave/tool deactivation completes
   the accepted gesture. Box action resets to Fill when the Editor reloads.
5. Cmd+Z / Cmd+Shift+Z restores a completed gesture, including occupancy, output,
   layout/room results and issues. Focus the Scene view for native shortcuts.
   Empty erasure can be undone without manually rebuilding.
6. Save and reopen to retain the committed layout. Re-enter the palette editing
   command if saving deactivated its authoring mask. Reopen/selection does not
   rebuild automatically.

## Corridors, connections and recovery

Use **New Room Space** or **New Corridor Space** in the layout Inspector. Select
its type/style, then edit that space's occupancy through Tile Palette. Each space
keeps one ID and one authoritative painted map. Overlapping ownership is rejected.

For Corridor Paint/Erase, set a positive **Corridor width**. Drag the same corridor
through bends and existing branches. The exact full-stroke preview commits once
on release; cancellation leaves no partial edit. Even widths extend the extra
cell east/north from their anchor, so reversing a route keeps its footprint.
Box Fill/Erase remains available for deliberate rectangular edits.

In **Boundary connections**, enable **Select boundary in Scene**, click a shared
edge, inspect its canonical start/orientation, choose width and type, then
**Place connection**. Horizontal spans run east, vertical spans north. Distinct
spaces retain a partition until explicitly connected, including adjacent Corridor
spaces. All span edges must retain the same expected negative/positive owners.
Overlap, exterior spans and unresolved junction endpoints are rejected before
mutation. Escape exits boundary selection.

Open passage, Doorway and Airlock opening cut exact structural gaps. Doorway and
airlock frames are currently unconfigured and report local Frame artwork warnings;
the aperture stays clear. Runtime doors, locks and interactions are separate.

**Correct** loads an existing record for an explicit address/type/width update
while retaining its ID. **Apply connection correction** validates the new span;
**Remove** explicitly deletes it. Both support Undo/Redo. Several external invalid
records can be corrected one at a time; geometry stays stale until all structural
connection errors are resolved.

Erasing a boundary never silently relocates/deletes its connection. A nonempty
layout with invalid connectivity retains previous geometry and marks it stale.
A wholly unoccupied layout clears only verified disposable output and retains
its connections as inactive, located Incomplete diagnostics. Removing those
records yields Empty. Unsafe coordinates/ownership or modified/unrecognized
output prevent cleanup. Undo restores cells, records, visuals and result state.

## Diagnostics and dimensions

A logical cell is 32×32 pixels at 32 PPU and one Unity unit. Floors cover one
logical cell. Prepared styles describe wall canvases, structural repeats, pivots,
anchors and corner reservations; imported artwork is not resized to force a fit.
Styles declare each imported canvas, native PPU, repetition width, structural
anchor and conservative visual footprint independently. For example, a compatible
128×256 canvas at 128 PPU can repeat every one logical cell; at 64 PPU the
same canvas covers two cells in width. The declaration and actual sprite must
agree. No importer or authoring operation rescales source artwork automatically.
Configured rendering reserves separate floor, detail and architecture orders.
The legacy unconfigured floor/architecture orders remain 0/1.

Orthogonal irregular shapes use their actual occupied boundaries. Valid floor
remains visible when a local art/span/join cannot resolve; only affected visuals
are withheld. Missing floor/style still leaves editor-only cell guides. Unsafe
ownership, overlaps or coordinates refuse destructive writes and clearly mark
retained output as not updated. Safe empty occupancy clears only verified disposable output; dangling connections remain inactive.

**Validate** is read-only. Select a reported issue and use its **Focus/Locate**
button to frame/highlight a region or identify its configuration field. Correct
its style/asset configuration, then **Rebuild Layout** (or Rebuild Room). Rebuild
uses the current authored cells and replaces verified owned architecture. Other
paint tools require this explicit rebuild; Validate alone does not synchronize
output. Supported style controls and bound-asset changes follow the refresh
workflow below. Resolve ambiguous references before replacing missing roots.

Authoring masks/guides are excluded from Play Mode/build floor queries. Generated
floor Tilemaps provide runtime walkability; runtime procedural generation is not
part of this tool.

## Disposable walkthrough and qualification

The **SOLITUDE > Room Authoring > Create Layout Walkthrough** command requires an
isolated Editor launched with `-solitudeRoomAuthoringVerification`. It creates a
new uniquely named Tests folder and additive scene, refuses existing destinations,
and preserves other loaded scenes. Save a disposable baseline scene first; setup
refuses an untitled scene and never saves it automatically. The editable main layout starts empty; two
prepainted ownership sentinels and a same-name gameplay sentinel test isolation.
Its colored sprites are synthetic workflow fixtures, not production artwork.
Their opaque caps and corners sit outside painted floor cells: south caps occupy
the row immediately below the floor, west caps the column immediately to its
left. North faces start at the top boundary and extend upward.

Review evidence lives outside Assets in
`work/Reviews/RoomAuthoring/UnifiedSingleSpace.md` and its JSON companion. Automated
checks and native visual checks are reported separately. Final production perspective and art quality require separate artwork review.
Temporary compatible assets qualify alignment and tool behavior. The structural
interconnected Medbay workflow is qualified separately with synthetic fixtures. Earlier Phase 2a
failed/inconclusive walkthrough evidence remains historical; it is not replaced
by this checkpoint.

The **Create Structural Medbay Walkthrough** command prepares three empty spaces
and synthetic thin boundary/junction art, with the same isolated-project,
fresh-destination and saved-baseline guards. Author its geometry through the
intended painting and boundary controls. The procedure and separately reported
automated/native evidence are in
`work/Reviews/RoomAuthoring/MedbayStructuralWorkflow.md`. Its structural native
checkpoint passed with 176 painted cells, three explicit connections, Undo/Redo,
and save/reopen/rebuild recovery. Final production artwork is not required;
the scaffold itself does not establish a painted Medbay.

## Replace artwork without repainting

The layout Inspector's **Layout style / fallback** selects its default family.
A space's **Space style (None = inherit)** can override that family or inherit it.
The adjacent label identifies the effective source. Changing these controls
rebuilds the affected layout in the same Undo action, retaining occupancy,
space/connection identities and unrelated gameplay content.

Expand **Artwork bindings** to replace the bound floor **Tile asset** or a north,
west, east or south **Sprite asset**. These controls edit the effective shared
style: other loaded layouts that use it also update. Import and save replacement
assets before binding them. Their dimensions, PPU and declared footprint must
match the style; use **Select Style Asset** to configure other module declarations,
corners, junctions, repetitions and rendering settings. A saved compatible large
canvas is supported; an incompatible replacement produces a located diagnostic.

Artwork palettes contain ordinary Tiles that reference reusable artwork. Editing
a bound floor Tile's Sprite or reimporting its referenced texture updates loaded
consumers. Structural palette Tiles preview the style's Sprite assets; to replace
a structural role, use its style binding. Moving palette entries or replacing an
unbound entry does not reassign floor/wall roles. Preparing an existing style
palette preserves its GUID and manual arrangement.

**Compatible transition styles** is an explicit qualification for existing
transition art. It does not invent a missing treatment. Shared north faces use
the visible side's family; cap/junction ownership and structural variants are
deterministic. An explicitly compatible layout default can supply a missing role
only after placement/clearance checks. **Fallback used** identifies the role and
address; missing or incompatible joins remain located issues while useful floors
and independent geometry remain visible.

Loaded layouts coalesce effective asset changes and defer updates during a paint
stroke. Closed scenes are not rewritten. Selection, Validate, Undo and reopening
do not regenerate. Modified/unrecognized generated output blocks automatic
replacement; inspect it, preserve authored content, then use **Rebuild Layout**.
Source-file pixel edits remain the actual current bytes and are not restored by
scene Undo. Supported binding edits restore references and generated output
through Undo/Redo together.

The temporary-art replacement procedure and separately reported automated/native
results are in `work/Reviews/RoomAuthoring/UnifiedStyleCompatibility.md`.

## Procedural floor panels

Expand **Procedural floor panels** in either the space Inspector or its layout's
space section. Settings are a draft until **Apply panel inputs**; one Apply is
one Undo action. Set Enabled, seed, generator version (currently 1), preferred
minimum/maximum pixel dimensions (multiples of eight), density 0–1, seam
thickness 1–3 and seam colors. Use a style whose detail sorting order lies
strictly between floor and architecture, for example 0 / 1 / 2.

**Preview** draws cyan proposed rectangular boundaries and yellow excluded cells
in Scene view. It does not replace existing visuals or publish assets. Clear
preview, Apply, Undo, save, play or reload cancels it. **Regenerate** keeps the
seed; **New Seed** advances it explicitly and is Undoable. **Disable** removes
owned seam visuals while retaining settings and reservation records, including
externally invalid records that still need correction. Changes to occupancy can
repartition that space; palette and thickness changes preserve panel rectangles.

Add equipment reservations as positive integral cell rectangles. Their IDs remain
stable while editing position/size; remove a record explicitly when it is no
longer needed. Doorway and airlock connections automatically exclude one cell of
depth on each side of their full opening width; open passages do not. No base
floor cell or collider is removed by these exclusions. Invalid input is reported
without silently moving or deleting authored data.

Inspector validation refreshes after object edits, Undo/Redo, asset changes and
generation events rather than polling the full layout while idle. Explicit
Validate remains a fresh assessment. Scene guides assess only repaint events.
Panel content is checked once per shared container within each output assessment;
no integrity result is cached across later edits.

Prepared sprites/textures live in immutable content revisions under this
feature's `GeneratedFloorPanels` directory. Same-content regeneration reuses
resources. Old revisions remain available to Undo and saved scenes; the Inspector
shows the managed store's retained count. Cleanup is explicit and conservative;
there is no automatic historical-resource deletion. Original artwork, painted
occupancy and unrelated gameplay objects remain separate.

The isolated **Create Empty Floor Panel Walkthrough** command prepares empty
spaces and temporary uniform floor art. It never paints an acceptance layout.
For the native paneling checkpoint, restore the archived natively painted Medbay
into a disposable project and preserve its baseline. The procedure and current
Passed/Failed/NotRun evidence are in
`work/Reviews/RoomAuthoring/FloorPanelingWorkflow.md`. Production artwork is not a
gate; standalone prepared-pixel player proof remains mandatory in Phase 4.
