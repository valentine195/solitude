# Unified style and palette compatibility

Status: automated and required native temporary-art checks passed; checkpoint review pending.
This record qualifies reusable temporary art and authoring behavior. Final production
artwork, procedural panel detailing, damage and decorative overlays are not delivered
by Phase 2e.

## Source and environment

The implementation workspace is the isolated `codex/layout-adoption-repair` worktree.
Phase 2e Steps 1–3 end at `78f6236da9595e99e9f26466c35bd15478807fe9`.
Step 4's source maps, actual Unity reports and attempt history are retained under
`.qrspi/floor-driven-room-authoring/verification/phase-2e-step-4-*`.
Approved inputs are Spec 5 (`b9a81579cbe4931cf6b6e03c74610e867d7a754acd28fd09cbd8f158bc15e4c1`)
and normalized Plan 9 bundle (`a67eb7e23cdd0263b6669e40e9eb276080b1bc43404e8dfd0a5e4fdfa090a573`).
Unity is 6000.6.4f1; verification uses the owned disposable project
`/private/tmp/solitude-layout-2e1-20261023/SolitudeArt2e1`.

## Workflow

1. Import/save compatible Tile and Sprite assets. A style specifies architectural
   roles, native PPU/canvas, repeats, anchors, footprints and rendering settings.
   Set the layout default; choose explicit space overrides only where needed.
2. Expand Artwork bindings in the layout/space Inspector. Replace the bound floor
   Tile or structural Sprite. The effective shared style and all its loaded
   consumers update in one supported Undo transaction. Painting stays authoritative.
3. Use reusable palettes to organize assets. A bound Tile/Sprite can be edited and
   reused; palette cell positions do not encode architectural roles. Preparation
   returns an existing palette without overwriting its arrangement or GUID.
4. Explicitly qualify compatible transition styles. Missing-role layout fallback
   is reported by role/address and still must satisfy clearance. A missing or
   incompatible transition leaves useful floors/independent art and a located issue.
5. Source texture pixel imports coalesce loaded-consumer refresh. No midgesture
   publication or closed-scene rewrite occurs. Scene Undo restores supported
   bindings and generated output; it does not restore historical external file bytes.
6. Inspect blocked automatic refresh rather than deleting unrecognized generated
   content. Correct configuration, then Explicit Rebuild. Validate/selection/reopen
   themselves remain read-only.

## Required native qualification

Use the preserved user/native-painted Structural Medbay scene at
`Assets/_Project/Tests/ShipLayoutSmokeaad37fed1d4f46ae8719e2b9fbd783ad/MedbayStructural.unity`.
Its 176 cells, three connection IDs and sentinel content must be inspected before
and after edits. Prepare only disposable style/palette assets; do not reconstruct
its occupancy or manually position walls. Keep the original warm-clone scene intact.

Exercise style inheritance/override, compatible bound floor Tile and north Sprite
replacement, a 128×256 large-canvas family, pixel-only import, palette arrangement,
missing-transition feedback/focus/correction, native Undo/Redo and save/reopen.
Inspect actual renderer layer/order, anchors, aperture clearance and visible joins.
Record each native operation and source snapshot; API inspection alone does not
establish a native input or visual check.

## Automated evidence and limits

Graphics-enabled focused Edit Mode execution: **Passed, 13/13**, attempt 4.
The actual nonempty north-wall output changes to the selected imported Sprite;
Undo/Redo restores output, inheritance and bindings. A persistent palette retains
its GUID, moved Tile and architectural bindings after save/reload. The Medbay test
checks 176 cells, three connections and actual configured floor/wall sorting.

Attempts 1–3 remain failed evidence. Diagnostics established that unattached UI
controls, an ineffective inherited-style edit and unsaved Sprite references were
inappropriate test fixtures. Unity serialized both transient Sprite references as
`instanceID:0`, so the asset-edit no-change guard did not rebuild. The qualifying
fixture uses imported saved assets and an attached Inspector window. Transient
in-memory Sprite replacement is not claimed as qualified palette authoring.

Full Edit Mode suite: **Passed, 362/362**, attempt 5. Executable source remained
exact. The clone automatically reserialized EditorSettings from version 15 to 16;
that clone-only configuration difference is retained in the evidence. Documentation
edits after the source snapshot do not alter the tested executable files.
Native temporary-art workflow: **Passed**. The original painted 176-cell Medbay,
three spaces, three connection IDs and unrelated sentinel content remained intact.
Native Inspector pickers assigned the layout default, cleared a space override to
inherit, assigned a second family, and rebound the floor Tile and north Sprite.
The second family admitted a 128×256 canvas at 128 PPU with a declared 128-pixel
repeat and bottom anchor. Its thin 128×4 ink strip tests placement and compatibility;
it does not qualify a finished full-face production wall.

Native Edit-menu Undo and Redo exactly restored the preceding and replacement
output snapshots. An external floor PNG pixel edit followed by native Assets Refresh
changed the scene's Supply floor appearance without changing Tile bindings or
importer metadata. Native palette editing moved a Tile one cell right and saved it;
the palette GUID, architectural bindings and complete scene snapshot stayed identical.
Native Save Project, scene save and Open Scene restored an exactly equivalent,
clean, current snapshot. Read-only CLI snapshots inspected actual output after
these native gestures; fixture asset preparation and camera setup used editor APIs.

The deliberate incompatible transition produced located feedback while keeping
all floors. Native Focus framed/highlighted its location without any snapshot
change. Correcting inheritance removed the new transition issues. Two original
missing doorway-frame warnings remain expected, retaining exact unframed openings.
Displayed Scene views showed temporary boundary alignment, concave joins and open
apertures. Additional camera setup did not visibly enlarge the view; subsequent
computer-use window errors prevented an optional close-up capture. No enlarged
or pixel-perfect camera qualification is claimed here.

Detailed criterion-linked checks and native evidence are in
`verification/phase-2e-step-4-qualification-attempt-6.json`. The saved/reopened,
Undo/Redo, focus, palette and preservation comparisons were asserted against actual
primitive snapshots. Native operation records use `phase-2e-step-4-native-*`.
Play Mode: **Not Run for Step 4** (editor controls and temporary asset qualification;
prior runtime-floor regression evidence remains historical).
