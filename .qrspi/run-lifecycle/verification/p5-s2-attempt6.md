# Phase5 Step2 — completed qualification (attempt6)

## Revision and outcome

Implementation HEAD `9554b07254b7adb78384952a6e2d3f4fcbb924c0`, branch `codex/run-lifecycle-playable-slice`. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved normalized complete plan manifest `p4-f1-approved-plans.json`; Phase5 `4d16e3c19852cade8e15dff7f830414a5dd8f1c540bad49393311a660ff5aeeb`. Spec/upstream provenance and normalized plans rechecked. All 7,211 repository Assets/Packages/ProjectSettings fingerprints and the three Step1 changed-source hashes match their recorded snapshots; product tree clean. `p5-s2-attempt6-final.json` records the checks.

Step2 complete; Phase5 implementation complete. **Final independent scoped Review (including S14) is still required at its human gate.** No integration/push. No product change was needed for this verification-only step, so there is no empty commit; source remains the already committed/gated HEAD above.

## Full gate reuse and artifact checks

As expressly allowed by Step2, reuse unchanged Step1 complete gate: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p5-s1-attempt2`. Recorded `results.json` is Passed; 252 pure +376 EditMode +155 PlayMode tests (783 total), no failures/skips, authoring validation and both builds passed. 100 standalone invocations met expected success or deliberate termination contracts. Details: `p5-s1-attempt2.md`. Rechecked results and all 162 shipping DLL names: no SOLITUDE.Tests, SOLITUDE.Verification or NUnit. Preserved copied results in `p5-s2-attempt6/full-gate-results.json`; original builds/logs remain at the gate output path.

No source change or confirmed product failure requires repeating unchanged tests. Earlier scoped review `playable-slice-r2.md` is historical Phase4 evidence; it is not presented as final Review of Phase5.

## Native manual journey and criterion coverage

Actual Unity6000.6.4f1 Wakeup, macOS ARM64, disposable saves `/private/tmp/solitude-p5-manual-saves`. Native CUA keyboard/mouse supplied ordinary gameplay inputs. Read-only CLI snapshots use production SaveJsonCodec. No teleport, inventory grants, direct vitals mutation or debug death/succession call. New Save setup uses the existing public `StartNewSave` action via CLI, explicitly separate from manual death. All evidence paths below are within `p5-s2-attempt6/` unless stated otherwise.

| Criteria | Concrete inspection/check | Expected / observed |
| --- | --- | --- |
| S1 | `new-save.json`; initial manual entry; initialization regressions | Fresh identity/expedition at awakening, full authored vitals and empty inventory/hotbar. Passed. |
| S2 | Native conduit damage; `pre-hazard.json`, `post-hazard.json`, `after-gameplay-death.png` | A automatically replaced by B; fresh vitals, empty carried owners, new expedition, no pending death, identical shared storage. Passed. |
| S3 | Step1 transition retry/zero-health/commit fault scenarios and held-input/open-screen PlayMode cases | One forward successor, gated dead control, no duplicate refresh. Complete gate passed; not asserted from manual snapshots alone. |
| S4 | Native E disable SEC-0; subsequent reload; ordinary death | Hostility ceased, defeat survived reload, actor returned after death. Manual and PlayMode/standalone passed. |
| S5 | `before-scene-restart.json`, `after-scene-restart.json`, `living-restart.json` | Same identity/position, gear, defeat/collection facts and expedition after Play restart/full editor quit/reopen. Passed. |
| S6 | Native E bandage/ration collection and refreshed source visuals after death; allowance/full-owner/consumption tests | One authored supply of each type; refreshed once after death. Full-capacity/retry/no-duplication cases passed in gate. |
| S7 | JSON shared-container comparison across actual death, permanent O2 collection; full persistence regressions | Locker contents identical across death; permanent pickup facts retained. Passed. |
| S8 | `carried-fuse.json`, `lost-fuse-death.json`, `fuse-reacquired.json` | Carried fuse lost at ordinary hazard death; unfinished source available to successor; native E collected replacement. Shared/unloaded fuse and installed-objective suppression passed in Step1. |
| S9 | Native Install Fuse/Restart buttons, green operational scrubber; retained repair snapshots | One fuse consumed, installed/running/recovery5 facts survived death and reload. Atomic repair/interruption/no-offline-time scenarios passed in gate. |
| S10 | Native traversal before/after death and reload; `retained-shortcut.json/png`, `reloaded-traversal.json`, `pristine-shortcut-traversal.png` | Powered/unlocked/open retained; physical crossing succeeds immediately. Live sprite door_08 and blocker=false confirmed. Passed; see resolved visual suspicion below. |
| S11 | Native Cmd-P stop, Cmd-Q complete editor exit, direct reopen with same disposable save, native Play | Same living identity/expedition and actual location (-5.39,-0.90). `retained-shortcut.json` vs `living-restart.json/png`. Precise saved vitals/offline recovery and invalid-position fallback separately passed standalone/PlayMode. |
| S12 | Step1 combined-snapshot memory/write/activation/ack fault runs, repeated restarts | Complete old durable state or coherent successor; no acknowledged revival or mixed repair/gear. Passed. |
| S13 | Step1 legacy migration/absent/shared fuse, corrupt/incompatible backup candidates | Known facts retained, missing objective component reconciled, recovery used rather than silent New Save. Passed. |
| S14 | Complete existing Domain/Application/EditMode/PlayMode/standalone regressions; Phase4 independent review | Regression evidence passed. **Final independent scoped code Review remains pending.** |
| S15 | Ordinary native collection → SEC-0 disable → battery shortcut → scrubber fuse/restart → conduit death → successor | Complete production route and A→B journey possible with ordinary gameplay; actual HUD, E prompts, repair UI, blockers and enemy activity observed. Passed. |
| S16 | Native New Save, scene restart and complete editor quit/reopen; transition-cause tests | New Save clears ship/current progress; reopen preserves expedition, death alone refreshes it. Passed. |

Native action records and snapshots are preserved from earlier attempts; earlier failed/inconclusive records were not overwritten. Assertion checks compare actual snapshots for death identity/gear/storage/ship, lost fuse/reacquisition, reload equipment/defeat/collection facts and restart location. `pristine-resume.json` and `pristine-traversal.json` repeat restoration/traversal after restoring pristine repository folders.

## Resolution of attempt5 findings

**Door graphic:** Read-only live inspection confirmed IsOpen=true, sprite `door_08` (frame index8, rect512/0/64/64), blocker=false, no competing renderer at that door position. The authored nine-frame source ends in that dark open interior. Its lack of closed metal panels distinguishes it from frame0. Native crossing succeeded. The previous closed-looking interpretation was mistaken; no product repair. `door-diagnosis-open-frame.png`, source `Features/World/SolitudeStart/Art/door-opening-spritesheet.png`, `SolitudeSlidingDoor.ApplyPresentation`. This diagnosis resolves the suspicion rather than waiving an observed failure.

**Disposable workspace differences:** Preserved the seven original differing files and hashes in attempt5. They included generated shipping settings, demo fixture serialization and editor render settings. Closed Unity, restored repository Assets/Packages/ProjectSettings using rsync into the disposable project, and checked all 7,211 fingerprints before launching. After launch/qualification only `Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity` differed. Existing `Features/World/SolitudeStart/Editor/SolitudeRebuildOnce.cs` explicitly rebuilds that unrelated demo once per non-batch editor session via SolitudeStartBuilder; its output explains this difference. Wakeup, its runtime code/assets and rendering/project settings all match repository bytes after the repeat check. `p5-s2-attempt6-entry.json` and `p5-s2-attempt6-final.json` distinguish initial exact source from this generated demo output. No regenerated demo was copied back to the repository or committed.

## Limits and remaining gate

Native locker drag is inconclusive: event-only diagnostics showed tool-delivered HOVER/CLICK without BeginDrag/EndDrag/Drop. No manual transfer PASS claimed, and this is not a demonstrated product defect. Required shared-storage retention was directly checked using preexisting locker contents; existing EventSystem/command transfer regressions passed separately. No need for the human to perform that optional comparison to complete the specified journey.

Existing small/overlapping world labels and some white item icons are recorded visual observations, not a broad UI/art polish audit. The final pristine editor log contains no gameplay exceptions/assertions; it has an editor licensing-token update warning. Earlier setup errors remain in earlier logs and are not called gameplay passes. Gameplay was advancing (frame/time snapshots and visible native movement), and editor was left out of Play mode.

Qualification is macOS ARM64/Wakeup only. All saves/faults isolated; no user saves accessed. Final Review receives only its permitted spec/plan/code/exact scope inputs, not this implementation/test narrative. Optional explainer remains declined. Await the human gate for final independent Review and any later integration.
