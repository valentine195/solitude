# Phase 1 Step 4 verification

Source commit `010b0009d63802a1380d14d49d693b995581f598` from `2d05402d531372ee06ea6239c067b62ffd711f58`. Spec SHA-256 `dd8f5bb2d90028526e6c5dcee74e6a8bf40f16f36bfdd06ab033ad73d1b9f339`. Staged binary diff SHA-256 `d91ab466b08eb9026a5d5d7d7098c88f8250a4385a81f164d533fcc304d75cf1`. Unity Editor 6000.6.4f1. All three painted Tilemap documents match Step 3 byte for byte. Both linkage actions bind to one repair component and use its existing typed consume-and-repair operation.

| Criteria | Expected | Observed |
| --- | --- | --- |
| S2, S8 | Specialist Field Repair can rig the intake with salvage. The standard repair remains available. | The Technician collected the authored salvage, rigged at the bracket, and left the actual replacement pickup in place. The Engineer with salvage could inspect the bracket and received the Specialist Field Repair refusal. With both materials, the Technician walked to the normal socket and used the replacement part, preserving the salvage. |
| S8, S9 | Only the intended material is consumed. Missing material, cancellation, access-point changes and repeat attempts leave inventory and fault state coherent. | PlayMode verified the missing-salvage refusal, Escape, and walking across the shared control's proximity zone mid-action. Both materials stayed present after the interruption. Each choice consumed its selected material through the same operation. Trying the other route after completion spent nothing. The two domain cases verified one unit consumed and one completion in either order. |
| S10 | The rig uses the same airflow recovery and opens a traversable door. | Observed recovering status and audio playback, then walked through the automatically opened return door using simulated keyboard input. |

- PlayMode filter `SOLITUDE.Tests.(CrewBackgrounds(RepairJourney|PlaytestLaunch)Tests|RestorationDoorTests)`. [16 passed](p1-s4-attempt-2-playmode.xml).
- EditMode filter `SOLITUDE.Tests.CrewBackgroundsEncounterStateTests`. [5 passed](p1-s4-attempt-2-editmode.xml).
- The first runner attempt arrived before the separate Editor finished shutting down and ran zero cases. The second attempt completed. The named Technician linkage launch reached Ready with the bracket and both supplies. The live console reported zero runtime errors. Unity's incidental PlayerSettings serialization was discarded after tests.
- Authored the specialist choice as a second access point on the same control. The salvage and bracket reuse existing prototype art. Final physical-device and compatibility checks follow in Step 5.
