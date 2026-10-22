# Shared layout authoring checkpoint

Phase 2c uses synthetic, one-pixel boundary artwork on native 32-pixel logical cells. These fixtures qualify topology, placement and editor operations; they do not qualify production wall perspective or seams. Explicit doors/openings and corridor-width tools remain Phase 2d work. Distinct logical spaces share partitions by default, including two Corridor spaces.

## Editing

Create a Ship Layout, then use **New Room Space** or **New Corridor Space**. Each member keeps its own painted occupancy Tilemap, stable ID, type, metadata and style. Choose **Edit Space in Tile Palette**, select Fill or Erase under Box action, and drag. One finished gesture rebuilds the layout; Undo/Redo restores authored occupancy and generated output together. Paint connected orthogonal cells for bends and branches within one Corridor space. Shared boundaries are resolved once at layout level.

Keep gameplay objects and decorations outside Generated Architecture. Editing one member can change a shared partition and its endpoint treatment, but must preserve other members' painted cells and authored choices. Generated architecture is disposable; authoring maps remain authoritative.

## Diagnostics

Select either the layout or an individual space. The Inspector reports the current assessment and whether generated output matches current inputs. **Validate Layout/Room** reads only; **Rebuild Layout/Room** updates generated architecture. Each issue identifies its space/owner, field and, where available, grid location. **Focus** frames the affected span or vertex and highlights its Scene marker. Layout coordinates are used for shared issues even when an offset member is selected. Focus and validation do not rebuild, dirty the scene or add Undo records.

Missing or incompatible local artwork retains useful floors and independent architecture; dependent pieces are withheld with located issues. Overlapping cell ownership refuses publication and retains stale output clearly marked as such. Missing floor artwork has editor-only cell guides. Correcting an issue removes its obsolete guide. Disconnected occupancy retains identity and cells but cannot be Complete; reconnect or erase the separated region.

## Verification

Actual Unity 6000.6.4f1 targeted Edit Mode run: **28 Passed, 0 Failed, 0 Skipped**. Covers shared erase/Undo/Redo and exact layout result restoration, complete/partial save-reopen, reversed member order and rebuild equivalence, read-only layout/member focus, offset coordinates, missing-floor guides, overlap refusal and stale/corrected results. Earlier failed attempts are retained in QRSPI evidence. A final layout-result Undo registration was needed after generated-object Undo writes to restore the matching Redo fingerprint.

Native walkthrough: **Passed** in the disposable `SolitudeJunctions2c3` Editor, Unity 6000.6.4f1. Native Box Erase removed Corridor A's adjacent six-cell strip (24→18 cells), leaving both neighboring rooms at 12 cells and exactly four B/C partition cells. Keyboard Undo/Redo restored exact layout/output snapshots with zero generation calls. Native Fill restored the strip, then painted a two-row L-shaped extension, yielding 36/12/12 cells and one generation per gesture. Shared partitions, the concave turn and the three-space junction were visually inspected with synthetic artwork.

With the TeeEast sprite withheld, all 60 floors remained visible and the Inspector showed a current Incomplete preview. Native Focus framed/highlighted the correct shared vertex at world (-6,-5); native Validate/Focus preserved exact snapshots, clean scene state, generation count and actual Undo/Redo records. Unity's numeric group counter advanced on mouse clicks, so recorded history was compared directly rather than treating that counter as an Undo entry. The earlier counter observation is retained in raw evidence.

The partial fixture reopened with exact state and no generation. Restoring its sprite cleared the issue/selection and returned Complete/current. Reopening the original complete fixture produced equivalent output; two explicit rebuilds from different entry points also matched. The gameplay sentinel remained at (20,30), authored decoration stayed under Room B, metadata remained intact and occupancy masks stayed hidden after editing.

Final complete RoomAuthoring Edit Mode regression: **209 Passed, 0 Failed, 0 Skipped**. This covers legacy and shared rebuild/Undo integration on the final source. The targeted 28-case run also passed. Earlier failed attempts and the temporary desktop-capture blocker are preserved in the evidence history. Play Mode execution for this editor-only step and production artwork qualification: **Not Run**. Full production acceptance remains in later phases. Detailed source snapshots and raw results are retained under `.qrspi/floor-driven-room-authoring/verification/phase-2c-step-4-*`.
