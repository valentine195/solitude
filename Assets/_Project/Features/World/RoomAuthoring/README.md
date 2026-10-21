# Ship layout and room authoring

Paint **Authored Occupancy** with **ShipRoomBrush**. Painted cells are authoritative;
generated floors and architecture are replaceable. This checkpoint supports one
logical space per ShipLayout. Shared multi-space partitions, connections and the
full Medbay workflow are later migration phases.

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

## Diagnostics and dimensions

A logical cell is 32×32 pixels at 32 PPU and one Unity unit. Floors cover one
logical cell. Prepared styles describe wall canvases, structural repeats, pivots,
anchors and corner reservations; imported artwork is not resized to force a fit.
The current checkpoint validates prepared 32-PPU modules; broader source-art
placement compatibility is addressed in the later artwork integration phase.

Orthogonal irregular shapes use their actual occupied boundaries. Valid floor
remains visible when a local art/span/join cannot resolve; only affected visuals
are withheld. Missing floor/style still leaves editor-only cell guides. Unsafe
ownership, overlaps or coordinates refuse destructive writes and clearly mark
retained output as not updated. Safe empty occupancy clears owned output.

**Validate** is read-only. Select a reported issue and use its **Focus/Locate**
button to frame/highlight a region or identify its configuration field. Correct
its style/asset configuration, then **Rebuild Layout** (or Rebuild Room). Rebuild
uses the current authored cells and replaces verified owned architecture. Other
paint tools/style changes require this explicit rebuild; Validate alone does not
synchronize output. Resolve ambiguous references before replacing missing roots.

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
checks and native visual checks are reported separately. Production perspective,
source-art alignment, seams and the complete interconnected Medbay wing require
later acceptance and are not established by synthetic fixtures. Earlier Phase 2a
failed/inconclusive walkthrough evidence remains historical; it is not replaced
by this checkpoint.
