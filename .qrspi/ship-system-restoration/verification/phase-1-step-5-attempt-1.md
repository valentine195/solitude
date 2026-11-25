# Phase 1 Step 5 — attempt 1

Status: BLOCKED on required manual readability check (S7). No commit; base HEAD `902b646b027daf3438e1f423c63c6f45b09476b2` plus the untracked test files hashed below. Step 5 is not complete; final Review has not run.

## Approved inputs

- `request.md`: `c5702ddd00b33fe951f763ee1087a47a02ef234f094354437d864a356e8207b8`
- `queries.md`: `15cb549d1444abdb39646930abacf3f4c01698df5bdd88e92d5dff594101f3d8`
- `research.md`: `73c7fceb71878834a1bda0eac8b01764d762979c70fff3c24ce6d12384b6db95`
- `spec.md`: `3396d3146f9a39644b41dafcb38afb8321933f670d07f1fc36288b0a2c97479f`
- `plan.md`: `53cc6bfe3e5e1c80b588d794a9012f3ec5c36ef6b05a41ddfa76bb285d9fc66b` (normalized status markers)
- `plans/plan-phase-1.md`: `0e74806e1b54e8cf4e902beef04d801d34f442eaf0de0527357fe63fb0bbc97c` (normalized status markers)


## Automated evidence

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-restoration-step1 --output /private/tmp/solitude-restoration-step5-attempt1`
Expected full passing result. Actual: `Passed`, 56 pure, 108 EditMode, 17 PlayMode tests, zero failures; authoring validation, verification/shipping builds, save/restart/recovery scenarios pass. RestorationSliceTests.FullLoopThroughSceneTargetingTransferPassageAndReload is explicitly present and passed.

- S1: initial Offline and independent second system; first installation leaves second Offline.
- S2: scene proximity sensors select junction; empty-inventory and locker-only attempts fail without restoration and report requirement.
- S3: real pickups provide two cells; container command transfer to/from locker; installation leaves one unit.
- S4: repeat via PlayerInteractor preserves remaining unit and exactly one transition. Focused tests continue covering invalid/reentrant cases.
- S5: scene terminal disabled across restore reconciles on activation and repeated enable cycles.
- S6: actual scene player movement/physics cannot traverse closed doorway; restoration leaves blocker closed; subsequent routed interaction opens it, and movement traverses doorway.
- S7: actual scene prompt updates without refocus; terminal state/color and repeatable feedback string asserted. These string assertions do not establish visual readability (failed below).
- S8: full-loop reload resets systems, terminal, door, pickups and inventory; sentinel unchanged. Existing lifecycle tests also pass.
- S9: configuration-only second set works; full shared-code gate passes. Locker transfer uses existing command API; this new test does not simulate drag gestures.

## Manual visual check — FAILED

Live demo opened via Unity CLI; Ready with disposable session. Scene targeting and PlayerInteractor calls produced correct prompt and feedback strings. Capture `step5-offline-prompt.png` shows the battery requirement, wrapped but readable. Capture `step5-terminal-content.png` includes the Canvas HUD: powered cyan terminal and open door are visible, but the green placeholder content overlaps the white interaction prompt and wraps into a narrow column, splitting the last word. Expected separately readable prompt and terminal content; actual overlap fails S7/readable-content verification.

For captures, the player was positioned via Rigidbody2D API; one pickup was invoked directly, junction/door/terminal through PlayerInteractor. Time was paused after terminal interaction to retain feedback for capture. Physical passage is verified by the automated test, not claimed from this manual positioning. Play mode stopped after inspection.

Proposed repair, awaiting human approval: adjust prompt/feedback RectTransform layout in this demo only through Unity APIs; update RestorationDemoBuilder to reproduce the layout. Add scene layout assertions, repeat the visual check, and rerun the required gate. This adds the builder/scene to Step 5's repair scope; no global UI redesign.

## Source snapshot

- `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs`: `35a5e8c8851774187b690ff0eed4b4910d7aa66fc6e35050a32cbe8ddce171f4`
- `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs.meta`: `8ffef603998146d2a6dc260da5d85e7be77501cbcf8ae0574db578c4c8c2e82a`

Changed source bytes match the tested workspace. Approved provenance/revisions rechecked. Full logs/builds: `/private/tmp/solitude-restoration-step5-attempt1/`; summary/XML/validation copied to `phase-1-step-5-attempt-1-results/`.
