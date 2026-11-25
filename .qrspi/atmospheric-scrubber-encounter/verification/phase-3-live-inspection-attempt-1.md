# Phase 3 live Unity inspection — attempt 1

User requested: “Perform your check you need with the Mac open”. Main Unity Editor, encounter scene, Game view Full HD (1920×1080) displayed at 0.66× on the 3024×1890 window. Native keyboard/mouse actions used through CUA; Unity API inspected state and positioned the player at interaction points. This is a visual/action walkthrough, not proof of manual walking: brief native movement taps did not span a physics update. Speaker audio could not be heard by these tools. Earlier explicit manual-check waiver remains applicable to those limitations; no unobserved check is labeled passed.

Spec SHA-256: `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`.
Phase 3 normalized SHA-256: `78a395ddb462f2468b87505cc01445e6ae509ccbb65d5db0af3130486d1c69b7`.
Final source locator: `phase3-step3-final-source-snapshot.json`. First walkthrough preceded final draw-order/font fix; fresh second playback confirmed corrected source.

## Findings and scoped corrections

- S1/S2: world CO2 warning rendered behind wall geometry. Runtime draw-order experiment made it visible. Corrected the authored MeshRenderer order to 2500 (above tile maximum 2000), increased font size from 3 to 4.5 and raised anchored Y from 7.4 to 7.7. Authoring test asserts warning above wall draw order. Authored through Unity API; existing GUID preserved; scene diff limited to warning properties.
- S2: font lacks subscript 2; replaced presentation CO₂ with CO2. Replaced schematic arrow/dot and objective dash typography with supported ASCII in view/builder, retaining categorical meanings.
- Live Editor had cached an older scene/catalog while disk authoring changed. Explicit fresh scene open/import resolved references. Final playback shows no warnings/errors, correct running-audio assignment and one notification component. Initial stale-import warnings remain historical, not an unresolved runtime failure.

## Concrete observations

| Criteria | Expected | Actual |
| --- | --- | --- |
| S1/S2 | Initial elevated warning, red offline equipment, fuse present | Fresh second playback visibly shows readable CO2 ELEVATED above red scrubber and one fuse; audio API alert=true, motor=false. |
| S2/S4 | Screen shows blown fuse and missing-item prerequisite; faulted restart fails | Keyboard E opened screen; Install Fuse disabled with exact inventory prerequisite; native Restart click displayed “Restart failed: blown fuse. Replace fuse first.” Telemetry stayed OFFLINE/BLOWN/STOPPED/ELEVATED. |
| S3/S5 | Collection once, then repair changes fuse but stays offline | E collected fuse, pickup disappeared and feedback named replacement fuse. Native Install click showed INTACT/OFFLINE/STOPPED/ELEVATED, updated task and “Fuse replaced. Restart scrubber.” |
| S6 | Restart starts confirmed running/recovery; normal only after recovery | Native Restart click showed green RUNNING, airflow ACTIVE, CO2 RECOVERING and waiting objective; later screenshot showed NORMAL and completed objective. Actual five-second threshold qualified by runtime suite, not screenshot timing. |
| S8 | Back closes primary screen, prompts hidden while open, no accidental action | Native Escape closed screen, E reopened. Screen screenshots show hidden world prompt and no control/prompt collision. Held movement, navigation/submit, same-frame close-input consumption qualified by composed-input tests; manual movement/native arrow focus was inconclusive. |
| S9 | Room restored, deferred notification, retained history, once | After completion with screen open, API warning=false, alert=false, motor=true, notification count=0. Native Escape revealed green equipment with no warning and top-right readable completion notice, separate from central world prompt. Reopening showed RUNNING/NORMAL/completed objective; notification count remained 1. |
| S7/S10 | Lifecycle state/leases/save coherence | Live restart of Play mode reset scene; rigorous five-stage reload/session-isolation coverage is in lifecycle tests and final gate. |

## Limits and disposition

Audibility/mix quality and a manually walked end-to-end route remain unconfirmed under the prior explicit waiver. Audio source playback state was checked directly, not inferred from graphics. User replied “I am not near my computer”; no speaker audibility confirmation was obtained. Targeted/full automated qualification must pass the exact final source before Step 3 completion. This is implementer inspection, not independent QRSPI Review.
