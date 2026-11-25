# Phase5 Step1 — qualification attempt2 PASSED

## Revisions and outcome

Commit `9554b07254b7adb78384952a6e2d3f4fcbb924c0`, base `c6c9272765920e284740d05fe575aedd0b790612`, branch `codex/run-lifecycle-playable-slice`. Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved normalized complete plans: `p4-f1-approved-plans.json`; Phase5 normalized SHA-256 `4d16e3c19852cade8e15dff7f830414a5dd8f1c540bad49393311a660ff5aeeb`. Exact three-file source manifest: `p5-s1-attempt2-source.json`. Spec/upstream provenance, all normalized plan digests and every source byte rechecked before commit; staged bytes matched tested source. Worktree clean; no integration/push.

Step1 complete after commit. Step2 remains not started. Phase5 not complete and final review not performed.

## Change

VerificationFaults supplies a verification-only checkpoint observer and explicit checkpoint entry. Existing physical-file boundaries drive the new labels; shipping compiles out observer/control bodies and the verification player assembly is excluded. No production lifecycle/save/repair code changed.

SolitudeCompositionSmoke creates an isolated combined snapshot through existing catalog/collection, encounter, socket and locker services. It invokes existing production succession authority and repair operation, intercepts memory/physical-write stages, and serializes observations through production SaveJsonCodec. Python clones each case, preserves baseline/candidate bytes and verifies restart invariants. Direct succession calls are deliberate fault-qualification controls; the ordinary input/hazard journey is separately covered by the existing PlayMode test and still requires Step2 manual qualification.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p5-s1-attempt2`.
Expected complete PASS. Actual: final results.json Passed, macOS ARM64. 252 pure,376 EditMode,155 PlayMode tests (783 total), no failures/skips. Authoring validation and verification/shipping builds passed. 100 standalone invocations passed their expected result or deliberate-kill contract: original32 plus68 combined qualification invocations. Deliberate kills are required outcomes, not unexpected failures. Runner rejects unexpected player exceptions. Shipping build checks exclude verification/test/NUnit assemblies.

| Criteria | Concrete scenario/check | Expected and actual |
| --- | --- | --- |
| S2/S3/S4/S6/S7/S12 | death-before-request, death-after-memory; committed/activated stages each at before-temp/after-temp/before-commit/after-commit; acknowledged-after-commit | Each restart is complete durable old living state or the committed successor, with coherent gear/expedition/storage/ship facts. Accepted successor has empty carried owners and full authored vitals; enemy/supply facts refreshed. A second process restart preserves successor and expedition IDs. All passed. |
| S3/S12 | death-write-rejected | Injected IOException leaves durable primary bytes unchanged and pending transition gated; disabling rejection and retrying completes one successor. Passed. |
| S2/S3/S12 | zero-health-startup | Zero-health saved survivor finishes succession before observation, no pending delivery remains. Passed. |
| S8/S9/S12 | repair-before-memory/after-memory; before-temp/after-temp/before-commit/after-commit | Repair installed fact and exactly one consumed fuse remain coherent across kills; old durable snapshot retains fuse/uninstalled objective, committed snapshot contains installed objective/no fuse. Other gear, shared contents and expedition unchanged. All passed. |
| S5/S11/S16 | scene-before-release/scene-after-release and prior lifecycle-write/resume/fallback/unavailable-scene | Scene release/restart preserves collected/defeated/ship/carried state; prior living precise location/vitals/identity and unavailable-scene byte-preservation tests passed. |
| S8/S13 | legacy-component-False/True | Version2 candidate with missing carried component regenerates unfinished source; retained shared fuse suppresses replacement. Shared saved slots preserved, migrated version3. Passed. |
| S8/S13 | migration-before-temp/after-temp/before-commit/after-commit | Forced termination around migration saves preserves legacy shared ownership and coherent component reconciliation on retry. All passed. |
| S8/S9 | death-stored-fuse and prepare-installed-fuse/death-installed-fuse | Shared stored component and installed objective both suppress replacement after death while equipment loss and permanent progress remain coherent. Passed. |
| S7/S9/S10/S13 | combined-corrupt/combined-incompatible plus original recovery matrix | Corrupt primary recovers combined snapshot from exact backup; incompatible primary blocks and preserves primary/backup bytes. Original interrupted recovery/unknown-item/capacity/read-denial cases passed. |
| S16 | independent-new-save | New Save changes colonist/expedition, empties carried owners, clears encounter/supply allowances independently of travel/death. Passed. |
| S2–S13/S16 integration | Existing pure/EditMode/PlayMode regression suites | Relevant transition retries, pending receipt validity, codec/migration, source/storage/repair/door persistence and actual input/hazard journeys all passed in the complete gate. |

## Durable evidence

Raw results, logs, XML and disposable scenario sets are in `p5-s1-attempt2/`. Interrupted cases preserve `candidate-before/` and `candidate-after-crash/` alongside actual primary/backup/temp/receipt files and observations. Migration/ownership input JSON is retained where injected; legacy component inputs are preserved in the candidate-after-crash directory before execution. All are disposable fixtures. Applications remain at `/private/tmp/solitude-run-lifecycle-p5-s1-attempt2/{verification,shipping}.app`, outside Assets. No normal user save accessed.

Attempt1 remains preserved. Human approved exact `p5-s1-codec-repair-proposal.patch`: observation snapshots now use production codec instead of Unity JsonUtility, whose null-reference inline serialization fabricated an empty pendingDeath object. No runner assertion was weakened and no production behavior changed.

## Limits and next gate

This qualifies macOS ARM64, production Wakeup, the explicit fault points and captured snapshots. It does not qualify other platforms, performance or every possible kill timing. No new manual journey or independent Phase5/final review is claimed. Step2 may reuse this exact complete gate if relevant source remains unchanged, then must perform the production manual/input/HUD/collision/visual journey and final coverage handoff before independent Review. Stop under single-step mode.
