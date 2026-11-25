# Phase 1 Step 2 verification

Source commit `ee136230fa337ce2ed7b222bcddc2f063282f80c` from `fcb844e5a318047464109ae5af69327590144227`. Approved spec SHA-256 `dd8f5bb2d90028526e6c5dcee74e6a8bf40f16f36bfdd06ab033ad73d1b9f339`. Staged binary diff SHA-256 `0e9067264e900b8ac1a3b2108cba9493a133e6a58f27bc5d4f76c3a6a5180970`. Unity Editor 6000.6.4f1. Unity's incidental PlayerSettings serialization was discarded. All three painted Tilemap documents match the Step 1 commit byte for byte.

| Criteria | Expected | Observed |
| --- | --- | --- |
| S2, S3 | Both backgrounds share stats, see their trained clues, and can perform the two standard repairs. | Reran the four setup tests and exercised both faults with both backgrounds. The displayed diagnosis traced the feed to the breaker and identified the replacement linkage. |
| S4 | Both backgrounds can walk past the patrol and reset the breaker with the light on. | Observed the bound patrol moving. Walked the wall-side path past it using keyboard events, inspected the breaker, and waited for the shared action to commit. Airflow resumed with the light enabled. |
| S7 | Only the configured correct part is consumed, exactly once. | Picked up the scene item through input. Missing and wrong parts gave a readable refusal. Successful work consumed one unit. A separate fixture using the same control and a different configured part consumed only its part, leaving the room scrubber and its part intact. The domain test observed inventory and repaired state together during publication. |
| S9 | Preview, cancellation, stale material, and repeated inputs cannot spend items or change repair state early. | Preview preserved the fault. Escape canceled work with the item intact. Removing the part before completion produced the missing-item refusal. Repeated input during and after work preserved the expected inventory and state. |
| S10 | A committed repair drives status and sound, pauses the recovery clock, and opens a traversable door after recovery. | Four keyboard-driven repair journeys observed stopped and recovering states, runtime audio playback, and an automatic opening after recovery. While paused, time held and the door stayed blocked. The player walked through the opening using keyboard events after its collider cleared. Three retained door cases passed for the existing manual call paths. |

- PlayMode filter `SOLITUDE.Tests.(CrewBackgrounds(RepairJourney|PlaytestLaunch)Tests|RestorationDoorTests)`. [10 passed](p1-s2-attempt-2-playmode.xml).
- EditMode filter `SOLITUDE.Tests.CrewBackgroundsEncounterStateTests`. [2 passed](p1-s2-attempt-2-editmode.xml).
- A live named Editor launch reached Ready with a bound patrol and the correct fault. The console check reported zero runtime errors for that launch. The tests used simulated keyboard events and checked AudioSource playback state. Physical device and final preset-wide compatibility checks follow in Step 5.
- The linkage pickup uses existing prototype art with its own item identity, label, and catalog entry.
