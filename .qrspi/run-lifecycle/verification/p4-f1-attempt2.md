# Phase4 review F1 repair — attempt2 PASSED

Commit: `c6c9272765920e284740d05fe575aedd0b790612` on `codex/run-lifecycle-playable-slice`. Base: `6bcc5c6cf1ba81aa816750f5fd056aa3e1cae929`. Product worktree clean; primary checkout unchanged. No pushing or integration.
Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved complete plan manifest: `p4-f1-approved-plans.json`; Phase4 normalized SHA-256 `6b38838e9a2aaf438e002807ae98b055f7aa269faec39c727e257e73a52e0f26`. Approved source snapshot: `p4-f1-attempt2-source.json`. Every source hash, spec/upstream provenance and approved normalized plan hash rechecked before commit; index matched tested source. Markers completed after commit.

## F1 / S4 repair

The encounter adapter tracks the reconciled expedition ID and requests an owned controller reset on a changed expedition, even when both old/new defeat facts are false. Existing same-expedition notifications retain live patrol state. Binding initializes the adapter without inventing a new expedition or resetting unchanged facts repeatedly. Reset uses the existing controller path to restore authored pose, physics, presentation, patrol/brain/navigation and counters; no separate AI serialization or lifecycle owner.

`RunLifecycleEncounterTests.DeathRestoresStillLivePatrolOncePerExpedition` first establishes attack activity and a displaced/rotated live bot, commits death in the same loaded scene without disabling it, and checks authored position/rotation, patrol/facing, zero attack counters and restored physics/visuals. A duplicate pending-death receipt and same-expedition lifecycle publication do not reset the displaced actor again. Existing disabled-actor, binding, codec-resume and production input journeys remain passing.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-f1-attempt2`.
Expected complete gate PASS. Actual `results.json` Passed. Pure252, EditMode376, PlayMode155 (783 total), zero failures/skips. Authoring validation, verification/shipping macOS ARM64 builds, all32 standalone save/restart/recovery/interruption scenarios passed. Raw logs/XML/results in `p4-f1-attempt2/`; applications remain in temporary output outside Assets.

S4 expected: every accepted expedition advance resets the loaded hostile encounter, including a still-live actor; ordinary notifications and duplicate receipt preserve idempotence. New regression passed. S2/S8/S15 integrated journey and all existing compatibility assertions passed in the full gate. This is implementation verification, not an independent reviewer verdict; prior `reviews/playable-slice.md` remains historical FAIL for its original snapshot until a fresh review of this commit.

## Approved test corrections

Human approved exact `p4-f1-test-repair-proposal.patch` and `p4-f1-plan-proposal/plan-phase-4.md`. Replay assertion now occurs before StoreLifecycle clears the pending receipt. Vitals boundary test checks authoritative double with unchanged tolerance, and compares UI fill to public normalized float; timing/status/oxygen/hunger assertions preserved. No vitals production code changed. Prior failed attempt remains preserved.

## Gate

Phase4 implementation complete, including F1 repair. Stop at human gate; offer fresh independent Phase4 re-review. Phase5 qualification has not begun. No explainer requested.
