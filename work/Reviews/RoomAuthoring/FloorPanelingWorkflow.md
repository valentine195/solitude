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

## Current evidence

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
