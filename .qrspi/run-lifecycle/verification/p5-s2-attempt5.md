# Phase5 Step2 attempt5 — BLOCKED at visual qualification

HEAD `9554b07254b7adb78384952a6e2d3f4fcbb924c0`; repository product source remains clean. Spec `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. All 7,211 repository source fingerprints still match Step2 entry manifest. No product edits, no new commit. Evidence directory: `p5-s2-attempt5/`. Earlier attempts remain intact. Step2 remains [!], Phase5 incomplete; final independent Review not started.

## Completed ordinary-input observations

Native Unity Wakeup play, CUA keyboard/mouse; read-only CLI snapshots through production SaveJsonCodec. No teleport, inventory grant or debug death call. The separate New Save setup invokes the existing public StartNewSave action via Unity CLI in disposable save storage; it resets the world, not a death.

- S1/S16: New Save produced new colonist/expedition, authored full vitals, empty carried owners, unpowered/closed shortcut, uninstalled/stopped scrubber and available fuse. `new-save.json`.
- S2/S4/S6/S7/S9/S10/S15: Previously collected bandage/ration/fuse/battery/O2, disabled SEC-0 through E, powered shortcut, installed fuse and restarted scrubber through real buttons. Reached operational green. Entered actual locker UI. Ordinary conduit damage caused automatic A→B, full fresh vitals, empty carried owners, new expedition, empty defeat/supply facts, persistent shared contents and identical completed ship facts. `pre-hazard.json`, `post-hazard.json`, `after-gameplay-death.png`. JSON assertions passed. Refreshed hostile and supply sprites observed. No successor cache invoked.
- S10: Native walk after death reached (-5.39,-0.90), crossing retained shortcut. `retained-shortcut.json/png`.
- S5/S11/S16: Stopped Play, quit the editor entirely with Cmd-Q, relaunched the isolated project with the same disposable save argument, entered Play. Same living identity/expedition and actual position (-5.39,-0.90). `living-restart.json/png`. Exact no-offline-recovery timing is qualified by the standalone gate, not inferred from delayed manual snapshots.
- S8: New Save, ordinary fuse collection, conduit death while carrying it, successor empty gear/source available; native E collected the replacement once. `carried-fuse.json`, `lost-fuse-death.json`, `lost-fuse-awakening.png`, `fuse-reacquired.json`. Assertions passed.
- S4/S5/S6/S9/S10/S16: Repeated ordinary battery/power, SEC-0 disable, replacement-fuse installation/restart and bandage collection; stopped/restarted Play. Same identity/location, expedition defeat/collection facts, equipment and completed ship facts. `before-scene-restart.json`, `after-scene-restart.json`. Native walk crossed restored shortcut to (-5.71,-0.70). `reloaded-traversal.json`, `reloaded-shortcut-traversal.png`.

## Findings preventing completion

1. **S10 / restored door presentation:** The final screenshot shows a closed-looking shortcut graphic after scene reload, despite saved open=true and successful traversal through it. Earlier opening showed the dark open doorway. Collision passed; visual restoration is unresolved and cannot be labeled passed. Need scoped diagnosis of sprite/animation/presentation binding, then an approved repair through the owning step and affected qualification. Root cause is not yet established.
2. **Manual workspace provenance:** Final byte check found seven disposable workspace files differing from repository entry fingerprints: two non-Wakeup scenes, four rendering assets/settings and ProjectSettings/ProjectSettings.asset (see `workspace-drift.json`, exact copies in `workspace-drift/`). Includes verification-generated shipping product settings and Editor serialization changes. All relevant Wakeup/code/assets fingerprints remain matched, and all repository files remain unchanged; nonetheless, this is not a complete byte-identical manual workspace claim. Preserve and account for generated differences or restore a fresh exact isolated source before final qualification. The Step1 full gate remains its own recorded passing snapshot; do not equate final workspace bytes with that snapshot.

## Locker gesture clarification

The earlier requested human drag is no longer needed to continue the required death journey. Read-only runtime event subscriptions showed CUA drag delivered HOVER/CLICK but no BeginDrag/EndDrag/Drop. That gesture cannot qualify native transfer and is not evidence of a product transfer bug. Existing EventSystem/command transfer tests are separate passing evidence. Shared locker contents themselves were directly compared before/after native death and retained. Optional native drag remains inconclusive; no gesture handlers or product source were altered.

## Existing gate and limits

Step1 `p5-s1-attempt2.md` records complete macOS ARM64 gate PASS: 783 tests, 100 standalone invocations, both builds and shipping exclusions. It supplies S3/S6/S7/S8/S9/S11/S12/S13/S14/S16 fault/regression evidence; it does not waive the visual/manual failure. Final S1–S16 matrix and S14 independent scoped Review remain outstanding.

HUD, E prompts, inventory opening/closing, actual scrubber controls, hostile activity and physical traversal were observed. Small/overlapping world labels and some existing white item icons remain visual observations, not a broad polish audit. No new unexpected gameplay exception was found in the restarted editor log; the initial log retains earlier setup errors (untitled-scene additive open and failed diagnostic command), so it is not represented as a clean entire editor log. Editor left out of Play mode. All saves disposable; no user saves touched. No push/integration.

QRSPI Implement requires: “In every mode, stop immediately and wait for the human when a step fails, a blocker appears, or a step can't be done as written.” Paused for the concrete visual/provenance findings. Proposed next authorized scope: diagnose the shortcut visual, preserve/account for generated workspace differences, make a bounded owning-step repair if confirmed, then rerun required affected and complete qualification. No broader cleanup.
