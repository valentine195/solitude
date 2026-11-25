# Phase4 F1 repair — attempt1 FAILED

Spec: a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922. Approved plans: p4-s3-repair-approved-plans.json. Exact 3-file source snapshot: p4-f1-attempt1-source.json, base6bcc5c6. All hashes unchanged after gate. No repair commit/staging.

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-f1-attempt1`.
Expected complete PASS. Actual: pure252 passed, authoring passed, EditMode376 passed, PlayMode153 passed /2 failed. Builds/scenarios not run.

F1/S4 implementation tracks reconciled expedition ID and requests owned controller reset even for false-to-false defeat state when ID changes. New regression passed authored position/rotation/facing, counters, enabled physics/visual and live encounter assertions. It failed line166: test clears pendingDeath via StoreLifecycle(active,...), then expects replay of departed survivor death to return true. Existing contract returns idempotent true only while receipt exists; after clearing it, departed ID is rejected. Proposed correction: replay before StoreLifecycle, then preserve the same-expedition notification and no-second-reset assertions.

Other failure: PlayerVitalsSliceTests.ActualWakeupClockDelaysThenRecoversToBoundaryWithoutChangingOxygen expected50 +/-0.00001 but public float Current was49.999988555908203. Vital stores authoritative double, exposes Current as float; recovery boundary tolerance applies to double. Float rounding can exceed that narrow tolerance. Proposed test-only correction checks private authoritative double via existing Field helper with unchanged tolerance; checks UI fill equals public Normalized float. All timing/oxygen/hunger/status assertions retained. No vitals runtime changes. This diagnosis follows source; new check still needs verification.

Exact proposed test patch: p4-f1-test-repair-proposal.patch. Proposed plan adds only this existing vitals test correction to Phase4 Step1 file scope: p4-f1-plan-proposal/plan-phase-4.md, complete normalized manifest proposed-approved-plans.json in that directory. Approval would authorize applying both test corrections and rerunning full gate. Prior review remains FAIL until a fresh review. Step1 marked[!]; current repair source preserved uncommitted.

QRSPI Implement: “In every mode, stop immediately and wait for the human when a step fails, a blocker appears, or a step can't be done as written.”
