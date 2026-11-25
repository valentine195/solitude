# Phase 1 Step 5 acceptance

**Physical playthrough pending.** Both approved fixes passed focused automation. Four implementation steps are complete. Step 5 is open for S12 physical verification.

Source: `010b0009d63802a1380d14d49d693b995581f598` plus the three working-tree changes recorded by `git diff --binary HEAD -- Assets`, SHA-256 `a4c2570618ffd4417b0dc982a30d6d2f68d7988a8dccc8a9e9009bdfc0c6ef06`. Unity Editor 6000.6.4f1. Spec SHA-256 `dd8f5bb2d90028526e6c5dcee74e6a8bf40f16f36bfdd06ab033ad73d1b9f339`. Normalized plan SHA-256 `f77e5ce2a9f606ac0388c81dad4aad85a4d89135228c833e7bd6679669b6abfc`. Normalized phase plan SHA-256 `2b73d3c1741fda262c44d2e564beff4fd2b81695d0e3185bf69e90800d911bdd`. Normalization replaces only overview and step status markers with `[ ]`.

| Criterion | Result | Observation |
| --- | --- | --- |
| S1 | Passed in PlayMode | Each launch configuration staged and loaded the same scene with the expected background and fault before input readiness. |
| S2 | Passed in PlayMode | Starting shared player values matched and diagnosis and repair requirements reflected the selected ranks. |
| S3 | Passed in PlayMode | Both backgrounds received the dead-feed and intake clues, including the trained clue at specialist rank. |
| S4 | Passed in PlayMode | Both backgrounds walked the standard breaker route across the active patrol and restored normal power and lights without a jumper. |
| S5 | Passed in PlayMode | The panel showed the lighting tradeoff. Engineer bypass started airflow, darkened the corridor, and installed the carried jumper. Technician was refused with the item present. |
| S6 | Passed in PlayMode | The main feed took over without resetting recovery. A full inventory refused reclaim and the same jumper remained installed until successful recovery. |
| S7 | Passed in PlayMode | Both backgrounds used the actual pickup, spent one replacement part, and started airflow. Missing and incorrect material and the second repair target were exercised. |
| S8 | Passed in PlayMode and EditMode | Technician used one salvage and left the standard part in place. Engineer was refused with salvage. Tests covered either choice on the shared repair control. |
| S9 | Passed in PlayMode and EditMode | Cancellation, invalidated inputs during work, and repeat attempts preserved inventory and equipment state until the valid commit. |
| S10 | Passed in PlayMode and EditMode | Standard and specialist repairs started recovery. Assertions covered status, audio, the pause interval, automatic opening, and traversal with simulated keyboard input. |
| S11 | Passed in PlayMode | Tests covered active-session persistence, fresh state after bypass, invalid launch, and later valid and ordinary launch. |
| S12 | Simulated input passed. Physical playthrough pending. | All four gamepad cases used the authored interact button to inspect and repair, picked up required materials, and moved through the automatic return door with stick input. The shared button closed inventory once, and Start still handled pause. The fixture positioned the player near interaction targets. macOS reported zero connected game controllers. Physical keyboard and controller playthroughs and in-room prompt readability are still unverified. |
| S13 | Passed in PlayMode | All four disposable sessions preserved the exact isolated production save bytes after pickup and flush. Retained restoration, scrubber and SEC-0 cases passed. The production-to-scrubber case used the production save codec, reached the transition and confirmed that the production session rejected it without changing the save. |
| S14 | Passed in PlayMode and EditMode | All four setups loaded the same room. The saved Floor, Boundary Caps and North Wall Faces were present. Floor and Boundary Caps tiles supported a native EditMode change and Undo on the scratch scene. |


## Attempt 2 runs

The focused runs used a disposable Unity project and save directory supplied by `-solitudeSaveDirectory`. The four gamepad cases, save isolation case and Tilemap edit/Undo case reused the supplemental fixtures from attempt 1. [Attempt 1 report](p1-s5-attempt-1.md).

- PlayMode filter `SOLITUDE.Tests.(CrewBackgrounds(FinalAcceptance|RepairJourney|PlaytestLaunch)Tests|RestorationDoorTests|RestorationSliceTests|ScrubberEncounterLifecycleTests|Sec0EncounterTests)`. 29 passed. [XML](p1-s5-attempt-2-playmode.xml) · [Supplemental fixture](p1-s5-attempt-1-acceptance-fixture.cs).
- EditMode filter `SOLITUDE.Tests.(CrewBackgrounds(EncounterState|FinalRoomAuthoring)Tests|UnityInputCompositionTests|TaskJournalInputTests|PlayerVitalsInputTests|ItemActionInputTests)`. 11 passed. [XML](p1-s5-attempt-2-editmode.xml) · [Supplemental fixture](p1-s5-attempt-1-room-fixture.cs).

## Physical acceptance still due

Use Unity's **SOLITUDE → Demos → Crew Backgrounds** menu. Run each named launch once with a physical keyboard and once with a physical controller. For each run, check that the fault clue and material or proficiency requirement are readable, that repair feedback makes the result clear, and that the character can walk through the door after the recovery interval.

| Launch | Route to exercise |
| --- | --- |
| Engineer / Loss of Power | Collect the jumper, read the lighting tradeoff, and bypass at the service panel. |
| Engineer / Broken Intake Linkage | Pick up the replacement and fit it at the intake. |
| Technician / Loss of Power | Follow the dead feed across the patrol route and reset the main breaker. |
| Technician / Broken Intake Linkage | Pick up salvage and rig the linkage at the bracket. |
