# Unified single-space checkpoint — passed

All **45 focused Edit Mode tests passed** (0 failed/skipped). The synthetic style anchor repair passed rendered-footprint assertions. Native Unity 6000.6.4f1 verification ran in disposable `SolitudeUnifiedNative2b4`, PID 62470, after the human closed the other Unity Editor and desktop targeting recovered.

| Native action | Actual result |
| --- | --- |
| Box Fill 8×6 | Exactly 48 cells/floors; one generation; south cap below floor |
| Extend four columns east | Exactly 72 cells/floors; original cells retained; obsolete east wall removed; one generation |
| Explicit Box Erase 2×2 | Only selected four cells removed; 68 floors; one generation |
| Undo/Redo | Exact authored/output/result/object snapshots restored |
| Remove Northwest sprite and Rebuild | 72 floors retained; only missing corner withheld; located current Incomplete preview |
| Focus Northwest | Frames/highlights corner; authored/output/dirty/generation state unchanged; actual Undo history proves no edit added |
| Restore sprite with native picker | Same-fixture sprite restored; complete 72-floor geometry recovered |
| Rebuild twice | Equivalent cells, geometry, placements and results |
| Native Save, open baseline, reopen walkthrough | 72 cells/floors, style/reference/global output identities and results persist; no regeneration/file rewrite |
| Other room/layout/same-name sentinels | Preserved |

Unity increments its current Undo group number during native GUI input; the initial counter-equality probe was therefore inconclusive. Actual native Undo after Focus reverted the preceding Rebuild and Redo restored it exactly, establishing that Focus added no Undo entry. An inconclusive asset drag was cancelled; correction used the native sprite picker instead.

**Isolation:** Of 7147 production file hashes, 7145 remain unchanged. The human confirmed “Yes I saved it” for the two changed files, `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity` and `Assets/_Project/Art/Tiles/Floors/RT_Floor.asset`. These intended external saves are preserved; a final comparison matches the initial snapshot plus exactly those acknowledged saves. Git status is unchanged. No agent scene/asset save, reload, close or painting commands targeted production. No production edits are included in the feature commit.

Step2b.4 execution is complete. Independent Phase2b review has not yet run. Approved Spec r4 (`cae50d38ff5b77bf1aa2ef9e0f4f5b02de2b6c478f908aa58705964f2adcc057`) and normalized Plan r7 (`27fcc225d305506965c811dd38beadcbc5548708201b1aae0dd74b9def9d8cd3`) and tested source hashes were rechecked before completion.

Evidence: QRSPI `verification/phase-2b-step-4-verification-attempt-6.json`, source manifest attempt4, Edit Mode XML attempt4, and `native-step4-attempt4/` snapshots/screenshots. Play Mode this step: Not Run (no runtime changes); earlier executed Phase2b.3 evidence retained. Production art qualification: Not Run. Prior failures and the old Phase2a fixture remain historical and untouched.
