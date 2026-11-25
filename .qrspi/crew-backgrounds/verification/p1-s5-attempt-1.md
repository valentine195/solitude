# Phase 1 Step 5 acceptance

**Blocked.** Four implementation steps are complete. The final gate has a controller interaction failure, an older scrubber test that fails at production save setup, and pending physical-device verification.

Source: `010b0009d63802a1380d14d49d693b995581f598`. Unity Editor 6000.6.4f1. Spec SHA-256: `dd8f5bb2d90028526e6c5dcee74e6a8bf40f16f36bfdd06ab033ad73d1b9f339`. Normalized plan SHA-256: `f77e5ce2a9f606ac0388c81dad4aad85a4d89135228c833e7bd6679669b6abfc`. Normalized phase plan SHA-256: `9a825159feeddba74c5d5e6e50d036c0625b2868e0df7b22bb720d9ccd70d45e`.

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
| S12 | Blocked | All four simulated gamepad journeys failed on the first Interact press. The press left the gameplay input policy disabled. The shared input asset binds Interact and Cancel to buttonEast, and the host enables both during exploration. Physical keyboard and controller playthroughs and prompt readability remain pending. macOS showed a keyboard and no connected gamepad. |
| S13 | Partial | All four disposable sessions preserved the exact isolated production save bytes after pickup and flush. Restoration, the scrubber's other retained cases, and SEC-0 passed. The production-to-scrubber transition case did not reach the transition. Its save fixture uses JsonUtility, and the production startup rejected those bytes with no valid committed save or backup. The same test file and shared input bindings are unchanged from the feature baseline. |
| S14 | Passed in PlayMode and EditMode | All four setups loaded the same room. The saved Floor, Boundary Caps and North Wall Faces were present. Floor and Boundary Caps tiles supported a native EditMode change and Undo on the scratch scene. |

## Runs

Both runs used a disposable copy of the feature project and a temporary save directory supplied by `-solitudeSaveDirectory`.

- PlayMode filter `SOLITUDE.Tests.(CrewBackgrounds(FinalAcceptance|RepairJourney|PlaytestLaunch)Tests|RestorationDoorTests|RestorationSliceTests|ScrubberEncounterLifecycleTests|Sec0EncounterTests)`. 24 passed and 5 failed. Supplemental preset tests stage the actual scene, position the character within reach of the first target, and send the live input asset a simulated button press. [XML](p1-s5-attempt-1-playmode.xml) · [Supplemental fixture](p1-s5-attempt-1-acceptance-fixture.cs).
- EditMode filter `SOLITUDE.Tests.CrewBackgrounds(EncounterState|FinalRoomAuthoring)Tests`. 6 passed. [XML](p1-s5-attempt-1-editmode.xml) · [Supplemental fixture](p1-s5-attempt-1-room-fixture.cs).

## Proposed recovery for approval

Include a scoped shared controller fix so the interact button performs the world action during exploration and retains cancel behavior in menus. Update the existing scrubber regression fixture to use the production save codec, preserving the transition and save-integrity assertions. Rerun the four simulated journeys and the directly affected input and demo checks, then complete the four physical keyboard/controller journeys.
