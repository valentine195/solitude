# Room authoring

Paint the room's **Authored Occupancy** Tilemap with **ShipRoomBrush**. The room
owns a separate **Generated Architecture** hierarchy containing its floor and
walls. Paint is authoritative; generated architecture is replaceable.

## Create and paint

1. In Edit Mode, open a disposable scene and choose **GameObject > SOLITUDE >
   Room Authoring**. Select the new ShipRoom root.
2. Assign a prepared **Room Style** in the Inspector. Production Standard artwork
   is still pending verification; the palette marker does not provide room art.
3. Click **Edit Room in Tile Palette**. Confirm the **ShipRoomBrush** brush,
   **RoomOccupancy** palette, and this room's **Authored Occupancy** target.
4. Choose Paint to add cells or Erase to remove cells. The native grid outline
   previews the cell. Choose **Fill** under **Box action**, then drag the Box tool
   from cell (0,0) through (7,5) for an 8×6 room: 48 authored cells and 48 generated floor cells.
5. Drag another box from (8,0) through (11,5) to expand east to 12×6, 72 cells.
   Walls and corners move to the new boundary when the gesture completes.
6. Choose **Erase** under **Box action**, then drag a box to remove cells. This
   selects the Box tool and needs no modifier key. Return Box action to Fill to
   add rectangles again. Erase a single exterior cell to see the unsupported-shape diagnostic; authored cells persist while
   generated output clears. Erasing all cells leaves an empty room.
7. Use **Edit > Undo/Redo** (macOS Cmd+Z / Cmd+Shift+Z) to restore a complete
   gesture, including authored cells, floor, walls, and diagnostic. A paint stroke
   spanning several cells is one gesture. Samples are queued and applied together
   when the gesture ends; geometry is generated once from the final cells. Leaving
   the tool or Scene view also commits the gesture. Undo empty deletion to restore a room.
8. Press **W** to leave painting before selecting the room root for its Inspector.
   Focus the Scene view when using the gesture Undo/Redo shortcuts.
9. Save and reopen the scene; select the room and click **Edit Room in Tile
   Palette** again to continue. The authored layout is retained.

Do not paint the generated floor. The occupancy marker is authoring data, not a
floor-art variant selector. Palette preparation is additive and preserves existing
palette layouts. Box action is tool-session state and resets to Fill when the
Editor reloads. Cell Paint/Erase tools are independent of Box action. Keep room
roots active, unrotated, unit scale, and aligned to integer XY coordinates. Move the whole room root, not its occupancy/output children.

## Dimensions and limits

One logical cell is 32×32 pixels at 32 PPU and one Unity unit. Floors are 32×32;
wall canvases are 32×64 or 64×64; cap/corner canvases are 32×32. A style uses one
configured structural wall repeat width. A 64-pixel repeat with zero corner
reservations requires an even room width; imported pixels are never resized to
force a fit. Anchors and corner reservations belong to the prepared style.

This version generates one filled rectangle. During an L shape, hole, unsupported
width, or missing art configuration, it retains authored occupancy, clears owned
generated output when safe, and reports the error in **Generation Diagnostic**.
Never duplicate room IDs or reuse another room's occupancy/output references.
Ownership errors refuse destructive generation so unrelated content stays intact.

## Validate and recover

**Validate Room** reports configuration errors without editing cells, output, or
scene dirtiness. It does not synchronize generated output. After painting with a
different tool or changing a style, click **Rebuild Room** explicitly. Rebuild
uses current occupancy and reconciles only that room's owned generated children.
Unchanged walls retain their objects, and the floor Tilemap updates only changed
cells. Deleted or changed modules are recreated as needed. Native Undo/Redo
restores cells, visuals, and child order. Rebuild also restores a safely deleted output root.
Resolve ambiguous ownership/reference errors before rebuilding.

The tool needs no Play Mode loop. Its qualified authoring mask is inactive when
saved and during runtime; selecting it for room painting activates it only for
editing. The generated floor remains the runtime walkable Tilemap. Re-enter
**Edit Room in Tile Palette** after saving if painting needs reactivation.

## Verification status

Phases 1–2 use disposable numeric styles and temporary native-sized white sprites.
They establish geometry, ownership, editing, persistence, and Undo behavior.
Production **Standard** bindings, native-pixel seams, and the final Standard demo
are Phase 3 acceptance work and are not established by these fixtures.
