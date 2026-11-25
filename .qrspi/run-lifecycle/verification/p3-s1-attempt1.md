# Phase 3 Step 1 — attempt 1 (blocked)

## Exact inputs and source
- Criteria supported by this step: S2, S3, S8, S12, S14, S16. This is transition infrastructure; automatic lethal-vitals triggering remains Step 2.
- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Approved normalized Phase 3 plan SHA-256: `666492730a4a50dcecee990115837b0beabf4f19f40316034eeb947aaf758a90`.
- All approved plan revisions: [manifest](p3-s1-approved-plans.json).
- Base commit: `d07ace57916e75533e915e097224e0e041e05ba5`.
- Uncommitted implementation snapshot: [source hashes](p3-s1-attempt1-source.json). Rechecked after the gate; unchanged. Nothing staged or committed.

## Changes attempted
Reuse the staged succession coordinator at root/session lifetime and inject it into the existing production scene adapter. Commit loss of carried inventory/hotbar, successor/expedition identities, and critical-source reconciliation together while retaining shared stores and ship facts. Persist the committed receipt before activation, then persist activation and acknowledgment reservation before releasing control. Recover saved committed/activated receipts forward. Same-scene awakening retains avatar/readers; death in another production scene loads the cryobay. Demo ports remain available.

## Verification
Command from the worktree root:

```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p3-s1 --output /private/tmp/solitude-run-lifecycle-p3-s1-attempt1
```

Expected: complete `results.json` Passed, including both builds and all standalone scenarios.
Actual: **Failed**, runner exit 1, PlayMode exit 8. Pure Domain/Application: 236 passed, 0 failed. Authoring validation passed. EditMode: 356 passed, 0 failed. PlayMode: 116 passed, 13 failed. Builds and standalone scenarios were not reached. This is not a qualified source change.

S3/S12: new production preparation-retention and committed-resume Application tests passed. S2/S8: atomic loss/source reconciliation/shared-storage tests passed. S14/S16: all four new production-root PlayMode tests passed (retained authority through travel/New Save; durable failure gate and no cache; return from another production scene; saved committed-receipt recovery). All 13 tests in RunLifecycleShipPersistenceTests passed. Existing regression failures still block the step and S14 conformance.

## Failures and interpretation
- Eleven failures across SurvivorSuccessionSliceTests, SurvivorSuccessionFailureTests, and SurvivorSuccessionShipCompatibilityTests exercise legacy cache/recovery/precommit assumptions against production Wakeup. They now reach the intentionally changed production death ports. Their cache-specific setup must explicitly exercise disposable/demo ports while production tests continue asserting permanent gear loss. Some expectations for invalid awakening/recovery configuration also need correct policy separation; genuine production awakening validation must remain pre-commit.
- PoweredDoorFirstContactAndPickupRemainLiveAcrossDeath failed before its test-specific succession fixture was installed: the root's new `Single()` injection requires a succession composition in an existing modular reference scene that has none. This is an implementation regression. Injection should tolerate an absent optional feature and leave existing reference-scene composition usable.
- RunLifecycleResumeTests.SceneArrivalKeepsColonistVitalsButUsesAuthoredArrivalPosition failed an exact Vector2 comparison with identical rounded display coordinates. Investigate body/Transform precision and use a stable physics observation or justified tight tolerance; do not weaken saved-location or vital checks.

Full names/messages/stack traces: [failures](p3-s1-attempt1/failures.json). Raw gate verdict: [results](p3-s1-attempt1/results.json). Test reports/logs and disposable save snapshots are retained in the same directory. All failures remain unresolved; no product edits after failure inspection.

## Stop and proposed repair
Step 1 and Phase 3 are marked `[!]`. QRSPI Implement says: “If a step fails, stop and report the issue” and “Wait for the human decision.” No automatic retry or fix was performed.

Propose a revised Step 1 file scope adding the three legacy succession PlayMode test suites and RunLifecycleResumeTests.cs, with a test-only disposable fixture if needed through Editor/PhaseFiveFixtureBuilder.cs. Keep production cache-free; preserve real demo recovery interaction/failure coverage. Repair optional root injection and validate awakening before irreversible loss, diagnose the location assertion, then rerun the complete gate under a new attempt name. This proposal does not change the spec or authorize Step 2.

Proposed normalized Phase 3 repair revision: `5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29` — [full proposed phase](p3-s1-repair-phase-proposal.md). Other plan revisions and spec remain unchanged under [the approved manifest](p3-s1-approved-plans.json). The proposal has not been adopted.
