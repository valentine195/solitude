# Shared layout authoring checkpoint

Phase 2c uses synthetic, one-pixel boundary artwork on native 32-pixel logical cells. These fixtures qualify topology, placement and editor operations; they do not qualify production wall perspective or seams. Explicit doors/openings and corridor-width tools remain Phase 2d work. Distinct logical spaces share partitions by default, including two Corridor spaces.

## Editing

Create a Ship Layout, then use **New Room Space** or **New Corridor Space**. Each member keeps its own painted occupancy Tilemap, stable ID, type, metadata and style. Choose **Edit Space in Tile Palette**, select Fill or Erase under Box action, and drag. One finished gesture rebuilds the layout; Undo/Redo restores authored occupancy and generated output together. Paint connected orthogonal cells for bends and branches within one Corridor space. Shared boundaries are resolved once at layout level.

Keep gameplay objects and decorations outside Generated Architecture. Editing one member can change a shared partition and its endpoint treatment, but must preserve other members' painted cells and authored choices. Generated architecture is disposable; authoring maps remain authoritative.

## Diagnostics

Select either the layout or an individual space. The Inspector reports the current assessment and whether generated output matches current inputs. **Validate Layout/Room** reads only; **Rebuild Layout/Room** updates generated architecture. Each issue identifies its space/owner, field and, where available, grid location. **Focus** frames the affected span or vertex and highlights its Scene marker. Layout coordinates are used for shared issues even when an offset member is selected. Focus and validation do not rebuild, dirty the scene or add Undo records.

Missing or incompatible local artwork retains useful floors and independent architecture; dependent pieces are withheld with located issues. Overlapping cell ownership refuses publication and retains stale output clearly marked as such. Missing floor artwork has editor-only cell guides. Correcting an issue removes its obsolete guide. Disconnected occupancy retains identity and cells but cannot be Complete; reconnect or erase the separated region.

### Generated rendering-state audit

Generated output currentness compares the following writer-owned 2D configuration. A changed value marks the retained output stale; Validate and selection only report that state. Explicit Rebuild recreates the disposable configuration from authored cells/styles.

| Output | Compared state |
| --- | --- |
| Hierarchy | Names, active state, local transforms, GameObject layers and component type/order signatures, including missing components. |
| Both renderer types | Enabled/forced-off state, sorting layer/order, rendering-layer mask, priority, dynamic occlusion, local/world culling bounds, material assignment array and property-block presence. The writer never installs a property block, so any block is stale. Material access uses sharedMaterials and never creates material instances. |
| Architectural sprites | Sprite reference, tint, both flip axes, draw mode/size, tiling mode/threshold, mask interaction and sprite sort point. |
| Floor Tilemap | Enabled state, anchor/tint, orientation/matrix, animation rate, occupied positions/Tile references and per-cell color/matrix/flags. |
| Floor renderer | Render mode, tile sort order, mask interaction, chunk culling mode/bounds and chunk size/count/age. |

The comparison uses public rendering values, without serializing Inspector selection state, engine cache/identity fields or editor visibility controls. Camera/global lighting/render-pipeline changes, 3D baking/ray-tracing settings and shared asset-content edits are outside this generated-component audit; asset admission and production-art qualification remain later work. Generated content is not an artistic override store. Keep durable authored content outside its disposable roots.

The diagnostic regression matrix changes each listed rendering category independently, including asymmetric partition flipX/flipY. It checks stale member/layout feedback, read-only Inspector/validation behavior, original renderer/Tilemap state and culling-bound restoration, exact output-fingerprint recovery, all 48 floor cells, authored-neighbor preservation and a gameplay sentinel. Existing save/reopen, focus and rebuild-order tests guard against unstable fingerprints. Execution results for this repair are recorded separately in the QRSPI verification artifacts; this description does not claim those checks have run.

Bounds and integer-vector components are encoded explicitly: Unity's JsonUtility emits `{}` for those structs when passed directly. Regression cases move an interior occupied cell without changing count or outer bounds, independently on the authoritative map and generated floor; authored changes must become stale and generated changes must be recoverable. An editor-frame test checks unchanged output through queued updates and member/layout selection, so engine updates must not create false stale feedback.

## Verification

Actual Unity 6000.6.4f1 targeted Edit Mode run: **28 Passed, 0 Failed, 0 Skipped**. Covers shared erase/Undo/Redo and exact layout result restoration, complete/partial save-reopen, reversed member order and rebuild equivalence, read-only layout/member focus, offset coordinates, missing-floor guides, overlap refusal and stale/corrected results. Earlier failed attempts are retained in QRSPI evidence. A final layout-result Undo registration was needed after generated-object Undo writes to restore the matching Redo fingerprint.

Native walkthrough: **Passed** in the disposable `SolitudeJunctions2c3` Editor, Unity 6000.6.4f1. Native Box Erase removed Corridor A's adjacent six-cell strip (24→18 cells), leaving both neighboring rooms at 12 cells and exactly four B/C partition cells. Keyboard Undo/Redo restored exact layout/output snapshots with zero generation calls. Native Fill restored the strip, then painted a two-row L-shaped extension, yielding 36/12/12 cells and one generation per gesture. Shared partitions, the concave turn and the three-space junction were visually inspected with synthetic artwork.

With the TeeEast sprite withheld, all 60 floors remained visible and the Inspector showed a current Incomplete preview. Native Focus framed/highlighted the correct shared vertex at world (-6,-5); native Validate/Focus preserved exact snapshots, clean scene state, generation count and actual Undo/Redo records. Unity's numeric group counter advanced on mouse clicks, so recorded history was compared directly rather than treating that counter as an Undo entry. The earlier counter observation is retained in raw evidence.

The partial fixture reopened with exact state and no generation. Restoring its sprite cleared the issue/selection and returned Complete/current. Reopening the original complete fixture produced equivalent output; two explicit rebuilds from different entry points also matched. The gameplay sentinel remained at (20,30), authored decoration stayed under Room B, metadata remained intact and occupancy masks stayed hidden after editing.

Final complete RoomAuthoring Edit Mode regression: **209 Passed, 0 Failed, 0 Skipped**. This covers legacy and shared rebuild/Undo integration on the final source. The targeted 28-case run also passed. Earlier failed attempts and the temporary desktop-capture blocker are preserved in the evidence history. Play Mode execution for this editor-only step and production artwork qualification: **Not Run**. Full production acceptance remains in later phases. Detailed source snapshots and raw results are retained under `.qrspi/floor-driven-room-authoring/verification/phase-2c-step-4-*`.
