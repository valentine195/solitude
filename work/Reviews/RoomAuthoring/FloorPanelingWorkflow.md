# Floor paneling native checkpoint

This is Phase 2f Step 6, under Spec 5 / Plan 14. Automated control-path checks do
not establish native authoring or visual qualification. Results below distinguish actual Editor execution from historical tests.

Use the isolated verification project. Preserve the original checkout and saved
native Medbay baseline. The earlier native fixture archive is
`.qrspi/floor-driven-room-authoring/verification/phase-2d-step-4-native-fixture.zip`.
It contains the natively painted 176-cell wing and three explicit connections.
Restoring those exact bytes is recovery, not scripted occupancy authoring.

Create a fresh temporary style with `ShipLayoutSmokeSetup.CreateFloorPanelStyle`
from the saved structural style. This creates flat 32×32 floor artwork and a new
style identity with sorting floor0/detail1/architecture2; it does not paint,
replace source assets or edit a scene. Bind the temporary style only in a saved
copy of the disposable wing. Preserve its baseline occupancy, connection IDs,
structural placements and unrelated sentinels before panel actions.

1. Select Treatment Room and expand Procedural floor panels. Enable a draft,
   keep version1, use seed17, preferred32–128px, density0.5, thickness1 and the
   default dark palette. Click Preview before Apply. Verify cyan proposed edges
   and excluded doorway cells; read-only snapshots must show no authoring,
   output, persistent revision, dirtiness or Undo mutation.
2. Apply. Inspect the transparent seam sprites above the uniform floor and below
   boundaries. Compare their saved RGBA pixels with the pure raster oracle;
   check Point,32PPU,no mipmaps,RGBA32 and native integer alignment.
3. Regenerate twice with seed unchanged: content IDs, references and counts must
   reuse. Click New Seed, then native Undo/Redo; exact authored seed, pixel hash
   and diagnostics must restore together. Finish at a known saved seed.
4. Add a stable-ID equipment reservation inside Treatment Room, position(3,4),
   size(2,2). Preview then Apply; confirm its four cells contain no ordinary
   seams while all base floors remain. Keep its ID through correction/Undo.
5. Enable the single bent/branched corridor, then the concave Supply Room using
   the same controls. Check both doorway threshold strips and the wide open
   passage, which must have no automatic threshold exclusion. Inspect corner,
   corridor bend, junction and chunk continuity with the actual Scene renderer.
6. Enter a deliberately invalid draft, such as thickness4: Apply must reject
   without output or Undo mutation. Correct to1 and Apply. If externally invalid
   data is separately exercised, retain its record and useful base geometry,
   report its field/location and correct explicitly.
7. Disable and Undo/Redo: remove only seam visuals; preserve seed/settings,
   reservations, base floors and architecture. Save, reopen and regenerate;
   compare exact authored/output hashes and sentinel snapshots.

Record actual project/Editor identity, source snapshot, scene/asset hashes,
seeds/settings, counts, canonical partition/pixel hashes, native screenshots,
read-only queries and per-check Passed/Failed/NotRun outcomes under verification/.
Do not create or repair occupancy through scripts to satisfy this procedure.

## Historical Phase 2f evidence

- Final Step6 repair source: Passed 480/480 RoomAuthoring EditMode tests, including
  three focused multi-chunk hash compatibility, unsaved texture tampering and
  event-driven Inspector lifecycle checks. Earlier Step6 factory 1/1 and Medbay
  3/3 checks are included in that final suite.
- Native Preview: observed 13 panels at seed17 and one excluded doorway cell.
  Serialized authoring/output/resources and clean-scene state were unchanged.
  Mouse/key events advanced Unity's Undo group counter; that counter alone does
  not establish unchanged history.
- Native Apply and two same-seed Regenerates: exact content references reused.
  Native New Seed advanced17→18; human Undo and Redo restored exact settings,
  sprite references, content hashes and diagnostics.
- Native equipment reservation: stable ID fe9fb9a299ac4011900a1a98725924aa,
  position(3,4),size(2,2). Treatment has four reservation cells plus its doorway
  excluded; 17 panels and 2,324 oracle-matching seam pixels at seed18.
- Native corridor and concave Supply controls: corridor seed0 has34 panels,
  two doorway cells excluded and4,847 oracle-matching pixels; Supply seed0 has10
  panels, one doorway cell excluded and1,422 oracle-matching pixels. The wide
  OpenPassage adds no threshold exclusion. All spaces total176 base-floor cells.
- Native invalid thickness4 Apply: visible located settings warning; persisted
  settings and all existing geometry/revisions remained exact. Corrected to1.
- Native Disable, Undo and Redo: Supply seams alone were removed/restored;
  exact settings, reservations, output hashes and diagnostics restored. Final
  Undo restored enabled Supply. All native base floors, structure, connections,
  unrelated spaces and sentinels remained unchanged.
- Native Save, New Empty Scene, Load Scene and Regenerate: reopened current,
  clean and byte-equivalent input/output snapshots, including reservation IDs,
  asset references and five retained immutable revisions.
- Actual Scene renderer inspected in the conversation: whole wing and enlarged
  Treatment/reservation/doorway and Supply concavity/corridor views. Uniform
  floor remains visible beneath transparent crisp seams; placeholder boundaries
  render above seams. No artificial chunk border or seam leak was observed.
  Read-only renderer inspection confirms Point/32PPU/RGBA32/one mip level and
  floor0/detail1/architecture2. These are temporary-art checks; production
  perspective/camera-motion qualification and final artwork are not claimed.
- Performance repair: output assessment302.74→28.70ms; room feedback
  1305.87→105.99ms on the same native fixture, identical fingerprints/content IDs.
  Event-driven Inspector refresh reduced measured idle CPU from roughly90% to
  8.3% with Supply's panel controls open. Measurements are local observations,
  not universal performance limits. Native desktop access became responsive.
- Native baseline, failed desktop/profiling attempts and successful read-only
  snapshots are retained under `.qrspi/floor-driven-room-authoring/verification/`.
  Final source snapshot/test results and qualification record identify the exact
  tested code. The final native scene/art/resources are preserved in
  `phase-2f-step-6-native-final-fixture-attempt-1.zip` with per-file hashes.
- PlayMode was not rerun for this editor-only repair; earlier Step4 results are
  historical. Standalone player loading remains Not Run and mandatory at the
  Phase4 shipping-build gate. Phase3 visual refinement and Phase4 final
  integration acceptance remain outstanding.

## Designer guide — current Phase 3 workflow

Start in Edit Mode. Create/adopt a ShipLayout, add Room/Corridor spaces, choose
compatible temporary styles, and paint each space through Edit Space in Tile
Palette. Confirm the selected target is Authored Occupancy. Use Box action Fill
or Erase without Shift; corridor strokes use their configured width and one
continuous corridor space through bends/branches. Adjacent distinct spaces need
explicit connections. Leave gameplay props outside Generated Architecture.

To enable Medbay Corridor paneling, select **Medbay Corridor**, expand
**Procedural floor panels**, tick **Enabled**, then **Preview** and **Apply panel
inputs**. The same section is available for the selected space in the layout
Inspector. Each space has independent settings; enabling Treatment does not
enable its corridor. Confirm the effective style has a uniform floor Tile and
sorting floor 0 / detail 1 / architecture 2 (or another strictly increasing set).

A useful starting point is version 1, seed 0, preferred widths/heights 32–128
pixels, density 0.5, thickness 1 and one dark seam color. Sizes are multiples of
eight pixels. Increase density for more/smaller panels, reduce it for fewer/larger
panels. Preferred minima can be constrained by narrow edges, thresholds and
reservations: eight-pixel remnants are valid and reported. Rectangular seams
provide asymmetric/staggered paneling; this prototype does not create chamfers,
grilles, damage, wear, decals or equipment docking artwork.

Preview shows proposed rectangles/exclusions without publishing resources. Apply
commits one Undoable edit. Regenerate retains the seed; New Seed explicitly
changes it. Palette and thickness preserve panel rectangles. Footprint,
exclusions, preferred sizes and density may repartition the edited space. Disable
removes seams only and retains seed/settings/reservations. Save prepared output
with the scene; use Undo/Redo for authored edits rather than painting generated
floor or seam objects.

Reserve equipment areas using positive whole-cell rectangles entirely inside the
space. Reservations and doorway/airlock strips remove detail only: floor remains
walkable and gameplay blockers must be authored separately. Doorway strips are
one cell deep on both sides of the full opening; Open passage adds no exclusion.
Invalid reservations/connections remain located records for explicit correction.

### Replace temporary art

Keep reusable Tile/Sprite assets and their metadata. For a bound floor Tile,
change its Sprite or reimport its source pixels to refresh loaded consumers. For
walls, caps and junctions, replace the style's Artwork binding and ensure the
sprite matches its canvas, PPU, repeat, anchor and footprint declaration. Editing
palette arrangement or dropping an unbound Tile into a palette does not reassign
a structural role. Preparing an existing palette preserves manual arrangement.
Closed scenes are not rewritten; reopen, validate and explicitly rebuild as needed.

Finish or cancel other pending panel drafts before applying/importing, and keep
one panel-editing Inspector per layout. RA-TD-001 remains a known, human-deferred
multi-Inspector refresh limitation. Source-file art edits are not scene Undo;
supported binding edits are. Modified/unrecognized generated output requires
inspection before explicit rebuilding, rather than automatic replacement.

### Diagnose and review

Validate is read-only. Read the Inspector reason/field/location and use Focus to
highlight it: Scene view shows compact markers and one concise focused label.
Correct configuration instead of stretching sprites or moving generated output.
Safe local art/panel failures retain useful floors and independent architecture;
unsafe ownership or invalid structural connections can retain stale output.

Compare Game output at native size and integer zoom, including camera movement,
concave edges, intersections, thresholds and texture borders. Seams should stay
below architecture, props and the player; exclusions should reveal uniform base
floor. Current evidence and precise test-camera limits are in
[VisualAlignment](VisualAlignment.md). No shipping camera was modified. The
standalone player proof and final performance/integration review remain Phase 4.
