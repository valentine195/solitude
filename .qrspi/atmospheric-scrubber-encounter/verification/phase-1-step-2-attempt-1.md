# Phase 1 Step 2 — safe primary interface

Spec SHA-256: `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`.
Normalized phase plan SHA-256: `98fec9402f320c0158175adb5c73262d35d5665a164cb9935a2c0b29160c3b19`.
Base HEAD: `f8017dc`; source snapshot: `phase1-step2-source-snapshot.json` (current draft file content hashes). Approved upstream revisions unchanged. No commit or staging performed.

Implemented a leased nonpausing primary screen, mutually exclusive inventory ownership, scene teardown/transition cleanup, Back priority, same-frame close consumption, selected-control retention, and explicit UI action references to the single composed asset.

| Criteria | Verification | Expected | Actual |
| --- | --- | --- | --- |
| S8 | Bundled .NET SDK, `Tools/Verification/PureTests.csproj`, absolute NUnitPath; `phase1-step2-pure-tests.log` | Lease blocks world/inventory while pointer stays enabled, scale remains 1, failed binding releases, existing container checks pass | 60 passed, 0 failed |
| S8, S7 | `unity test /private/tmp/solitude-scrubber-phase1 --mode EditMode --filter SOLITUDE.Tests.UnityInputCompositionTests` with isolated save directory; `phase1-step2-input-bound.xml` | Composed asset UI navigation/submit enabled, inventory ignored, close frame suppressed, later input resumes, independent pause/containers preserved | 1 passed, 0 failed, 0 skipped |
| S2, S7, S8; preserve S1/S10 | Same isolated project, `--mode PlayMode --filter SOLITUDE.Tests.ScrubberEncounterSliceTests`; `phase1-step2-submit.xml` | Real keyboard movement/interact/inventory blocked, close does not activate underlying target, EventSystem submit closes, proximity/reopen retains selection, transition closes; initial room/save/vitals unchanged | 2 passed, 0 failed, 0 skipped |
| Source hygiene | `git diff --check` | No whitespace errors | Passed |

Initial attempts are retained: missing test type qualification, an overbroad inventory-view assertion, and a UI navigation failure. The navigation failure exposed implicit UI action reference binding; explicit references corrected it. The submit test initially manually updated input before the frame's EventSystem processing; queuing into the normal frame loop verified actual submit delivery successfully.

## Remaining gate
Phase 1 Step 1 requires manual label readability and audible warning checks. These remain unrun because the macOS session is locked. Step 2 code and automated verification are complete, but completion markers and commits remain pending the ordered Phase 1 checkpoint; no incomplete Step 1 dependency was committed. Step 2 marker is `[!]`, and Phase 1 is not complete. No Phase 2 work began. Full player-build/regression qualification remains scheduled for Phase 3.
