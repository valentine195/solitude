# Medbay temporary-art and floor-paneling workflow

Status: Phase 3 Step 2 qualification passed with the separately accepted RA-TD-001 limitation. Pixel-perfect motion and final qualification remain later steps. The checkpoint commit and exact source evidence are recorded under verification. This report distinguishes completed native observations from synthetic tests and remaining acceptance work. It is not a completed phase verdict.

## Scope and source

Spec revision 5: `b9a81579cbe4931cf6b6e03c74610e867d7a754acd28fd09cbd8f158bc15e4c1`.
Approved Plan revision 27 normalized bundle: `9d866594dd8fcd96124658010d3f64869de6c27b759a0b737cd947a50e5b6782`. Plan26 label-only evidence remains historical support for its recorded source; the complete current suite requalified it.
Implementation base: `3f26ddf97eec8641438ef437d8446972e8471047`, with the scoped Medbay integration test, approved Scene-label correction, atomic Room Paint/Box ownership correction, and saved demo/dependencies. Verification source maps identify the exact tested bytes; this report does not attribute those changes to the base commit alone.

The native Editor is Unity 6000.6.4f1, explicitly targeting the disposable project `/Users/jeremyvalentine/.codex/verification/layout-2e-repair-20261024/SOLITUDE`. The production checkout and Wakeup scene were not targeted. The preserved native fixture was verified against its archive before updating its authoring implementation. A 203-file recovery archive was retained before that update. The original native scene is `Assets/_Project/Tests/ShipLayoutSmokeaad37fed1d4f46ae8719e2b9fbd783ad/PanelingWalkthrough.unity`. The saved feature demo is now `Assets/_Project/Features/World/RoomAuthoring/Scenes/MedbayAuthoringDemo.unity`; it contains the preserved 176-cell authored wing, the two unrelated sentinels, and the unrelated same-name root.

Temporary structural modules and continuous floor artwork are intentional. Production art, damage, decorative overlays, and handcrafted floor-panel variants are not acceptance dependencies. Doorway frames remain unconfigured; the two exact architectural gaps are retained, producing two expected Frame warnings and a current IncompletePreview result.

## Paint and connect

Select a logical space and use **Edit Room in Tile Palette**, the **ShipRoomBrush**, and **Authored Occupancy**. Paint the logical footprint; generated floor, structural sprites and seam renderers are disposable output. Use Box action Fill/Erase explicitly. Do not paint Generated Floor or move generated walls to repair a layout.

| Space | Authored region | Cells | Panel seed / seam pixels |
|---|---|---:|---|
| Medbay Corridor | Width 3 horizontal route with one L bend and a T branch | 102 | 0 / 1 |
| Treatment Room | Cells (0,3) through (7,8) | 48 | 18 / 2 |
| Supply Room | Cells (11,3) through (15,8), excluding (14,7) through (15,8) | 26 | 0 / 1 |

The main corridor remains one logical space across its bend and branch. Stable space IDs and exact cell lists are recorded in the native snapshots. Treatment uses the compatible second temporary style; Corridor and Supply use the original flat-floor style. The native object picker assigned Treatment's style; native Undo/Redo preserved the exact topology and other spaces.

| Connection | Boundary | Width |
|---|---|---:|
| Treatment doorway | Horizontal (2,3) | 1 |
| Treatment open passage | Horizontal (5,3) | 2 |
| Supply doorway | Horizontal (12,3) | 1 |

Recorded topology: 176 cells, zero overlapping ownership, 385 unique canonical edges, 25 partition edges, four opening edges, and 91 closed edges represented by total structural span length 91. Exterior classification contains six convex and two concave corners. Each space has one connected component. These are exact structural measurements, not a claim of production-art seam qualification.

## Enable paneling and reserve equipment

Expand **Procedural floor panels** on a space. Edit draft settings, Preview, then Apply. Seeds/settings and explicit equipment reservations are authored data; generated seam textures are prepared editor output. An invalid seam thickness of 4 was refused without publishing the draft. Preview of valid thickness 2 left persisted inputs, output and retained resources unchanged; Apply and native Undo/Redo preserved coherent states.

Treatment's equipment reservation is an explicitly authored test rectangle at (3,4), size 2×2. It is not inferred from a production prop or collider. Together with its doorway threshold, Treatment excludes five cells from ordinary paneling. Corridor excludes its two doorway threshold cells; Supply excludes one. Base floors remain present beneath these exclusions.

Independent raster-oracle comparisons against actual prepared native texture buffers found zero mismatched pixels:

| Space | Rectangular panels | Excluded cells | Texture chunks | Opaque seam pixels |
|---|---:|---:|---:|---:|
| Corridor | 34 | 2 | 5 | 4847 |
| Treatment | 17 | 5 | 2 | 3420 |
| Supply | 10 | 1 | 2 | 1422 |

Actual prepared sprites use Point filtering, 32 PPU, no mip chain beyond the base level, and detail sorting order 1 between floor 0 and structure 2. Static native camera renders were captured. Pixel-perfect camera movement remains a separate Phase 3 Step 3 check.

## Editing, refresh and diagnostics observed

- Native Fill expanded Supply from 26 to exactly 30 cells by adding only its 2×2 notch. Native Undo/Redo restored exact authored and generated states. Native Box Erase restored the exact concave shape; its Undo/Redo also matched the recorded states.
- Erasing Supply cell (12,3) retained the connection record but invalidated its boundary. Publication was Refused/current=false, with all prior floors, structure and seam buffers retained and a located stale-connection message. Native Focus zoomed/highlighted the doorway without changing authored inputs, generated output or scene dirtiness. Native Undo/Redo and final Undo restored the valid wing.
- Erasing all of Supply alone produced the same expected nonempty-layout connection refusal. Its native Undo restored the exact 26-cell room. **Correction to earlier walkthrough wording:** inactive connection records are the exception for a wholly unoccupied layout; one empty space in an otherwise occupied layout does not qualify.
- Erasing all three spaces cleared every generated floor, structural sprite and seam, while retaining all three connections, panel settings/seeds, Treatment reservation and unrelated sentinels. All seven diagnostics became inactive. Native Undo restored the exact 176-cell wing; Redo reproduced the exact empty state; final Undo restored all authored/generated state and a native save left the scene clean. The history contained four erase gestures and five selection records; intermediate partial restorations were retained as evidence, not reported as completion.
- Two native Rebuild Room clicks produced equivalent cells, placements, seam buffers, connection records and sentinels; retained resource revisions remained seven. Native save followed by reopening the saved scene preserved exact state and clean dirtiness.
- Changing and reimporting only the second style's temporary floor PNG automatically changed Treatment's visible floor color. Occupancy, settings, reservations, structure, actual seam hashes and sentinels stayed exact. Original PNG bytes were restored and reimported; its metadata/GUID remained unchanged.
- Scene diagnostics now retain compact markers but print text only for the focused issue. Actual warning/advisory Focus controls showed one readable label, such as “Doorway frame” or “Small floor panel,” with location. Full identities, reasons, severity and controls remain in the Inspector. The native scene stayed clean and all 176 cells/output remained exact.

The earlier comparison of Undo group numbers across several native selection/navigation actions cannot isolate Focus's contribution; it is not used to claim an unchanged native Undo group. Automated read-only tests verify Undo-group preservation around the relevant APIs.

A script reload logged three built-in GameObjectInspector/TransformInspector/GridEditor stale-target exceptions. Raw stack traces are retained; selecting the room reopened its functioning Inspector. These were not C# compilation failures, and no label-code stack frame or scene state loss was established. The console was not cleared to conceal them.

## Verification and remaining work

| Check | Result | Limit |
|---|---|---|
| Synthetic Medbay control-path tests | Passed: 4/4 | Recorded before the label-only change; synthetic fixture is not native painting evidence |
| Focused diagnostic tests on current label source | Passed: 11/11 | Edit Mode |
| Panel diagnostic tests on current label source | Passed: 40/40 | Edit Mode |
| Native label source recompile | Passed: zero errors/warnings | Built-in Inspector reload exceptions recorded separately |
| Native two-style, settings, resize/erase, invalid boundary, Focus, rebuild, reopen and pixel refresh observations | Passed as described above | Exact snapshots/renders retained |
| Wholly empty layout with inactive records and restoration | Passed | Actual native Undo/Redo; exact state comparisons and final clean save |
| Current RoomAuthoring Edit Mode suite | Passed: 580/580 | Exact source/asset map; 313.158 seconds; no skipped or inconclusive tests |
| Current Play Mode tests | Not Run | Pixel/collision runtime compatibility checks belong to Step 3/final qualification |
| Pixel-perfect camera motion | Not Run | Phase 3 Step 3 |
| Final demo transfer and dependency qualification | Passed for native save/reopen/rebuild and exact file transfer | 29 dependencies; 49 new files including metadata; no Step 2 commit yet |
| Cross-owner overlap atomicity, S42 | Passed | Nine new regression cases, 42/42 focused, 580/580 full, and live Editor preview/feedback checks |
| Multiple pending Inspector Apply coordination, S49 | Known deferred limitation | RA-TD-001; one active panel-editing Inspector per layout is the interim workflow |

Detailed attempt files are retained under `.qrspi/floor-driven-room-authoring/verification/phase-3-step-2-*`, including source maps, NUnit XML, native baseline/archive comparison, topology, inverse pixel oracle, native snapshots, exact comparisons, static camera renders and label verification. Failed/inconclusive simulated drags are preserved and are not counted as successful authoring.

See [TechnicalDebt.md](TechnicalDebt.md) for acceptance boundaries. This is Step 2 qualification evidence. No broad waiver, production-art qualification or final runtime handoff is claimed here.

## Final demo asset transfer

Unity AssetDatabase moved 24 native fixture assets without changing their GUIDs; a 62-file recovery archive preceded the move. Temporary art/styles now live under feature-local `DemoAssets/Medbay`; managed panel resources use the normal `GeneratedFloorPanels` root. A copy of the native scene was saved and reopened as `Scenes/MedbayAuthoringDemo.unity`. Two rebuilds preserved exact cells, settings, reservations, connections, pixels, diagnostics, sentinels and resource count. The new scene was not added to shipping build settings. Only the three referenced immutable panel revisions were transferred; seven historical revisions remain in the isolated native project/archive.

The dependency closure has 29 assets, including existing source, occupancy Tile and URP package dependencies. Existing project dependencies were compared byte-for-byte and left untouched; 49 new scene/resource/metadata files were transferred from the qualified native project. A current [static native render](MedbayPanelingWorkflow.png) is retained; camera-motion qualification remains Step 3.

The first rebuild harness wrongly expected a true boolean: Rebuild returns `result.IsSuccessful`, which is false for the deliberately incomplete doorway-frame configuration. Exact before/after output and currentness checks passed. Its assertion is retained as a harness error, not a product regression. The first render envelope fell back to a string because it contained a Vector3; a second capture with primitive-array coordinates produced structured verification. Both attempts are retained.

S42 was reproduced through the actual current operations API on an unsaved preview clone. An overlapping 2×2 Box Fill at (11,2) produced two ownership issues and retained prior derived output, but persisted two new authored cells. The preview scene was closed; the saved native demo's inputs, cells, output and clean state remained unchanged. This is an atomic-authoring failure, not data loss in the saved demo. That historical failure was repaired and requalified as recorded below; it was not waived.

## Atomic-paint correction and verification

Plan27 reuses the corridor cross-owner check for direct room cell/box operations. A conflicting box writes nothing. A late conflict cancels the entire active room gesture, including earlier samples; the brush reports the affected cell/owners through existing feedback. A wrong-room edit cannot cancel another transaction. A valid incomplete preview remains a valid authored change.

Focused attempt5 passed 42/42 tests in 24.714 seconds, including nine new cases and existing room/corridor/Medbay/panel workflow coverage. The full current RoomAuthoring Edit Mode suite passed 580/580 in 313.158 seconds, with zero skipped/inconclusive tests. All 205 recorded source/asset/metadata files still matched the worktree and test clone after execution. The clone's previously recorded project/render configuration differences remain a limit; actual native checks use the preserved authoring project.

The native Editor had been closed. Two verified product files were backed up/synchronized with metadata unchanged; the official Unity 6000.6.4f1 Editor reopened the saved demo at the exact prior 176-cell/current/clean state. Compilation is current with zero reported errors/warnings, and the new overloads executed in the live Editor. Recompile also left a Pipeline command-name diagnostic in the console; it was not concealed or counted as a product compilation failure.

Native qualification uses unsaved preview clones and actual operations/selected ShipRoomBrush APIs. Direct cell/box conflicts preserve all 26 Supply cells. Late room Paint and Box Fill first staged 27 and 30 cells respectively, then rejected the overlap and restored exactly 26. Authored inputs, object identities, derived output and seven retained resource revisions were unchanged. A CUA screenshot showed the existing Tile Palette error box with cell (11,2), both owner identities and the rejection reason. This does not claim a successful simulated pointer drag. Original human-painted native acceptance evidence remains separate. The saved wing, all three spaces, connections, reservations, settings, sentinels and clean state match the pre-correction snapshot exactly. The current native static render was recaptured with the camera restored afterward.

Earlier attempts remain available: unsupported LINQ on Unity's bounds enumerator, two incorrect test namespace qualifiers, and two new-test failures at a later valid-fill Undo assertion because unset metadata serialized from null to empty. The bounds iterator and verified UnityEngine.GridBrushBase namespace corrected compilation. Explicit nonempty fixture metadata, matching existing layout fixtures, verifies actual text preservation without weakening equality assertions. Native setup attempts also recorded a closed Editor/default brush and inactive clone Tilemaps that needed Grid initialization; only qualification attempt4, with staged counts greater than 26, establishes late-stroke coverage. No earlier failure or inconclusive attempt is substituted for these passing results.

RA-TD-002 is resolved. RA-TD-001 remains deferred under its existing human acceptance and one-active-Inspector interim workflow. Current Play Mode tests, pixel-perfect camera motion and final independent qualification are Not Run in Step 2 and remain later plan work.
