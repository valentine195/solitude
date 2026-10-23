# Structural Medbay authoring walkthrough

Status: structural native walkthrough passed. Automated control-path tests and native authoring are recorded separately. Production-art quality remains pending.

Use an isolated Unity 6000.6.4f1 verification project launched with `-solitudeRoomAuthoringVerification`. Save an untitled disposable baseline before setup. Choose **SOLITUDE > Room Authoring > Create Structural Medbay Walkthrough**. Each invocation requires a fresh GUID-suffixed ShipLayoutSmoke folder; an existing destination is refused before changes. The scaffold contains three empty spaces and separate ownership/gameplay sentinels. It does not paint the acceptance layout.

## Intended native layout

Select each space through the layout Inspector, then **Edit Space in Tile Palette**. Paint Authored Occupancy with ShipRoomBrush. Generated Floor is output, not a paint target.

| Space | Intended region | Expected occupied cells |
|---|---|---:|
| Medbay Corridor | Width 3: horizontal centerline (1,1) to (17,1), bend from (17,1) to (17,10), T branch from (9,1) to (9,7) | 102 |
| Treatment Room | Box Fill cells (0,3) through (7,8) | 48 |
| Supply Room | Box Fill cells (11,3) through (15,8), then Box Erase cells (14,7) through (15,8) | 26 |

The regions are target instructions, not a scripted scene-generation recipe. Actual committed native cells must be inspected and recorded separately. Expected total: 176. The corridor remains one ID across its bend and branch.

Select **Structural Medbay Layout > Boundary connections**. Enable **Select boundary in Scene**, click a shared boundary, then inspect its integral start/orientation and width before placing. Horizontal width runs east; vertical width runs north. Escape exits selection.

| Connection | Canonical start | Width | Negative / positive owner |
|---|---|---:|---|
| Doorway | H (2,3) | 1 | Corridor / Treatment Room |
| Doorway | H (12,3) | 1 | Corridor / Supply Room |
| Open passage | H (5,3) | 2 | Corridor / Treatment Room |

Expect four opening unit edges, no structural wall sprites across these gaps, and one shared partition per remaining canonical edge. Doorway frames are currently unconfigured: two local Frame warnings and Incomplete preview are expected. Missing frames never fill openings. Synthetic line/dot modules qualify structural control paths only; production perspective, seams, two-style art transitions, accent/damage treatments and frame bindings remain later milestones.

## Editing and recovery checks

- Erase one Supply Room floor cell adjoining its doorway. The connection stays authored at the same ID/address; nonempty invalid connectivity refuses publication and marks retained geometry stale. Native Undo restores the floor, gap and result without manual rebuild; Redo/Undo repeats the transition.
- Save, reopen and rebuild twice. Stable space/connection IDs, occupancy, generated placements and unrelated sentinels must remain equivalent.
- Inspect missing-floor feedback by temporarily removing the synthetic style's floor binding in an Undoable edit, rebuilding and viewing the editor-only cell guides. Restore the binding and rebuild; guides must disappear and admitted floors return.
- Validate and focus a located issue without changing cells, output, scene dirtiness or Undo state.
- Runtime floor checks must accept generated admitted floor, exclude holes/authored masks/diagnostic guides/architectural sprites, retain solid blockers and avoid runtime generation.

## Evidence

Current automated execution: 322/322 Edit Mode tests and 2/2 Play Mode floor-compatibility tests passed. The source maps and NUnit reports are retained under `.qrspi/floor-driven-room-authoring/verification/phase-2d-step-4-*`. Earlier failed fixture metadata evidence is retained. Native input and screenshots now establish the structural workflow described below.


## Observed native result

Unity 6000.6.4f1, PID 31551, explicitly selected project `/private/tmp/solitude-layout-phase2c-step2-20261021/SolitudeBoundaries2c2`, using an APFS copy of the installed official Unity app at `../MedbayUnity.app` to distinguish it from the production Editor. The production project was not targeted. The saved fixture is `Assets/_Project/Tests/ShipLayoutSmokeaad37fed1d4f46ae8719e2b9fbd783ad/MedbayStructural.unity` in that disposable project. Layout ID: `1299084db2ba4b079efd3e7f8a1036c6`.

The user painted the initial 57-cell horizontal strip. Native computer-use gestures painted the bend, branch, both rooms and concave notch; Inspector controls authored all three connections. No acceptance occupancy or structural walls were positioned through scripts. The empty scaffold and temporary style-binding checks used Unity CLI; final inspection was read-only.

| Space | Actual ID | Cells / generated floors |
|---|---|---:|
| Medbay Corridor | `62cb2af6aa5f4bd9a37672c0e5729519` | 102 / 102 |
| Treatment Room | `7d1b8b0198b84786959f472a28c12c6e` | 48 / 48 |
| Supply Room | `454b5c2dfde841af9ac4e26842565c54` | 26 / 26 |

All cells match the regions above, totaling 176. Actual connection IDs are `50d8e13dad314c2f9bea0405bd913964` (Treatment doorway), `c07f6ec0211b4d3fb09e4e11df040362` (Supply doorway), and `83bc3d0af96c47428462be3905211ca9` (wide open passage). Addresses, widths and owner pairs match the connection table. An accidentally selected wide Doorway was explicitly Undone before placing the intended Open passage; intermediate evidence is retained.

Read-only resolution found 385 distinct canonical edge addresses, 66 exterior edges, 25 remaining partition edges and four opening edges. The closed layout has 29 partition edges; exactly four become openings. Structural span length is 91, equal to exterior plus partition edges. Exterior corner classification contains six convex and two concave corners. The corridor remains connected across the bend and T branch. Each native bend/branch gesture added one generation; the human initial stroke's generation count is not used to claim batching.

Native Box Erase at Supply cell (12,3) reduced occupancy to 175 while retaining all 176 generated floors. The result became Refused/current=false with a located stale-connection diagnostic and the same connection IDs. Native Undo restored the exact connected snapshot; Redo reproduced the stale state; a second Undo again restored the exact snapshot. Sentinels remained unchanged.

Temporarily clearing the synthetic floor binding through an Undo-recorded style edit produced zero generated floors and visible orange editor-only cell guides across the authored footprint. Restoring the binding and rebuilding restored the exact connected snapshot and all 176 floors. Issue selection/focus was checked through the Editor API and preserved the authored/output snapshot, scene dirtiness and Undo group. The Mac locked after visual inspection; save/reopen and two rebuilds were therefore executed through the explicitly targeted Unity CLI rather than keyboard input. Each matched the original connected snapshot. The final scene is saved and clean. Legacy/other-layout snapshots remain unchanged; the same-name gameplay sentinel remains at (24,12,0) with only its Transform.

Native results and screenshots are `phase-2d-step-4-native-*.json/png` in the verification directory. `native-ready.png` was captured after the human's first 57-cell strip, despite its filename. The stale and missing-floor screenshots show feedback in the actual Editor. Missing doorway frames yield exactly two expected local art warnings, so the final status is current IncompletePreview. Production perspective, seams, multiple production styles, accents and damage remain Not Run for this structural phase.
