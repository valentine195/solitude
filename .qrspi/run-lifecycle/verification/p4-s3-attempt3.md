# Phase 4 Step 3 — full gate attempt 3 (PASSED)

## Revision and outcome

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Approved complete plan manifest: `p4-s3-repair-approved-plans.json`. Phase 4 normalized SHA-256: `950ac227e506bf44556612ffa4578958b6dc24838361c44937a40861dc59a1ff`.
Source snapshot: `p4-s3-attempt3-source.json`, based on `6e42cdc02d46bd298125ef9f2462207d3da468af`. All 14 changed/new file hashes were rechecked after verification and match the committed files. Spec/upstream input provenance and every normalized approved plan revision were rechecked before commits. Status-only completion markers preserve approval.

Prerequisite commit: `be1af7ee0126c0d93f87c817d758938eca686929` imports only the exact previously committed primary Wakeup scene from `3839559`, matching the preserved committed-primary baseline. Step 3 commit: `6bcc5c6cf1ba81aa816750f5fd056aa3e1cae929`. Branch: `codex/run-lifecycle-playable-slice`. Isolated worktree clean; primary product checkout remains clean and feature has not been integrated or pushed.

## Change

Wakeup now binds a production scrubber/screen, objective fuse source, SEC0 encounter, one bandage and ration source, powered permanent shortcut, existing persistent locker mapping, and bounded conduit damage using the existing vitals contract. Reuses existing visuals and composition services. Supplies, encounters, repairs, critical-source suppression and succession use the existing session; no parallel save mechanism. New authoring validation checks route endpoints and production catalog references.

Existing scene IDs/GUIDs are preserved. Four original painted Tilemap serialized blocks are byte-identical to the committed-primary baseline; no painted layout was regenerated. Existing locker loot and source content are preserved. Disposable generated succession fixture excludes only the new production route; production/resume remain intact. Existing tests select their own fixture identities and retain their assertions.

## Verification and criterion evidence

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-s3-attempt3`.
Expected: complete gate PASS. Actual: `results.json` Passed. Pure252, EditMode376, PlayMode154 (782 total), no failures/skips. Authoring validation, verification and shipping macOS ARM64 builds passed. All32 standalone save/restart/recovery/interruption scenarios passed; deliberate interruption exits expected. Raw logs/XML/results/save fixtures are in `p4-s3-attempt3/`; built applications remain in the temporary output outside Assets.

| Criteria | Concrete check | Actual |
| --- | --- | --- |
| S15 / S4 / S6 | RunLifecycleSliceTests.InputJourneyRepairsScrubberUnlocksShortcutAndGameplayDamageRefreshesOnlyExpedition | Real WASD/proximity/E input collects supplies/fuse/battery, powers shortcut, disables SEC0, opens real scrubber UI, submits install/restart through EventSystem, observes repaired room state. Passed. |
| S2 / S9 / S10 | Same input journey, actual ExpeditionHazard damage and successor/revisit assertions | No debug death calls; lethal damage produces a new colonist and expedition at awakening, empty inventory/hotbar and no recovery cache; repairs/shortcut retained, supplies/actor refreshed, ordinary O2 pickup stays collected. Revisit retains successor/expedition/repair. Passed. |
| S8 | RunLifecycleSliceTests.LostCarriedFuseReturnsButAReachableStoredFusePreventsReplacement | Carried fuse is lost on gameplay death, source returns and is reacquired; ordinary movement reaches existing locker through upper passage and E opens modal; existing transfer command stores fuse; next gameplay death does not generate replacement, exact storage count including one fuse retained. Passed. |
| S8 | Installed source assertion in first journey | Installed fuse source remains suppressed after death and scene revisit. Passed. |
| S15 / S2 | RunLifecycleAuthoringTests.ProductionRouteHasCompleteBindingsAndPreservesSafeAwakeningAndSharedStorage and WakeupValidation | Complete production bindings, reachable endpoint floor cells, safe awakening, non-disposable session, shared critical storage/catalog/build inclusion. Passed. Input journey supplies actual reachability evidence. |
| S7 / compatibility | Existing catalog, critical-source, ship-persistence, disposable succession and full regression suites | All existing assertions retained and passed; permanent pickup collection survives selective death refresh. |

Two world-camera screenshots `p4-s3-attempt3/playmode-saves/route-scrubber-restored.png` and `route-successor-awakening.png` were visually inspected. Scrubber feedback is green after repair; successor returns to cryobay with actor/supply presentation refreshed. Export temporarily disables PixelPerfectCamera, changes render target, and omits overlay UI; these images are supporting world-state evidence, not complete visual/UI qualification. Existing UI regressions pass. Manual journey/visual qualification and dedicated death-process interruption coverage remain Phase 5 scope.

## Approved repairs and gate

Prior failed attempts preserved. Human approved fixture isolation/socket selection/frame-driven Submit/locker approach repairs, then the exact `p4-s3-locker-repair-proposal.patch`. Final correction targets the actual locker at (-6,4) via upper passage and asserts its Locker interaction component. No product layout or interaction policy changed to satisfy the test. Source snapshot was stable throughout verification and commit staging matched it exactly.

Step 3 and Phase 4 marked complete after commits. Stop at the approved step/phase boundary. Next decision is independent Phase 4 checkpoint review; Phase 5 has not started.
