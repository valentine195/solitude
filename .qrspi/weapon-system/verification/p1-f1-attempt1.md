# Phase 1 checkpoint repair F1 — attempt 1

Outcome: PASS for repair implementation and required verification. A fresh independent checkpoint review has not run; the previous FAIL remains historical evidence for its original snapshot.

## Scope and approval

Human approved fixing F1/S15, adding a succession regression, and rerunning verification. Failed review: reviews/equipped-wrench-swing.md at HEAD 06349bf6f51539fe7d5fd964926a81db85752d20. Spec SHA-256: 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5. Approved repair scope recorded in Phase 1 review-repair section; normalized overview d825b24d16443e871862dd17eb230f025b3a4416f7e34469b3e3b522d87bd8dc, normalized Phase 1 5d93d6546db58f9a493041c2a57e1ac61c37eb15549f62da1814ba3ad7b4360e. Upstream provenance and all five normalized plan revisions checked before implementation and after verification.

Three-file source snapshot: p1-f1-attempt1-source.json. Working tree, isolated verification workspace and staged index matched its exact content hashes after verification. No source assets, metadata, input definitions, save schemas, loadouts or scene files were added/modified by this repair. Existing unrelated SOLITUDE_Start.unity edit preserved outside commit.

## Fix

PlayerItemActionComposition.RetireActor disposes the departed actor's runner (including timers/execution/owned movement and visual leases) and installs an empty runner without releasing scene equipment/input bindings. SurvivorSuccessionComposition invokes it in TryActivate, the existing gated post-commit activation path, before awakening the successor. Failed pre-commit transitions and ordinary same-actor cancellations still use Cancel and retain committed duration. No successor discovery service, new identity registry, transaction changes or broader lifetime framework was introduced.

## Criterion-linked verification

- F1/S15: CommittedSuccessionDiscardsDepartedLockoutButRejectedRetirementPreservesIt passes in the real composed Wakeup PlayMode fixture with isolated saves. It exercises .05/.15/.30 seconds (windup/active/recovery) and keeps frame-driven input live at Time.timeScale zero so waiting for input cannot drain the inherited timer. Each case first rejects retirement before commitment and checks same runner/current survivor/cooldown retained, then commits succession and verifies old runner disposed, fresh empty runner, no swing renderer or movement slowdown, same wrench transferred from recovery ownership, held Primary suppressed, fresh actual mouse Primary accepted immediately with full .55 commitment, and a stale departure request cannot reset that commitment. No gameplay time advances between successor activation and fresh-input assertions.
- S10/S11/S14/S16: actual Primary freshness and input-policy tests plus existing lifecycle, movement/facing, cooldown/cancellation and runtime isolation tests remain passing. Regression adds no production loadout or damage behavior.
- S30: complete isolated authoring, suites, native builds and standalone persistence/recovery gate passes. Existing metadata, catalog, shipping assembly and scene validation are included. No manual gameplay/art capture or physical device session is claimed.

Command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output /Users/jeremyvalentine/SOLITUDE/Logs/WeaponSystemP1F1Attempt1.
Expected: complete gate passes including new regression and old suites/builds. Actual: results.json Passed, 46 stages/scenarios completed; 269 pure tests passed/0 failed; 516 Editor passed/0 failed/0 skipped; 143 PlayMode passed/0 failed/0 skipped; verification and shipping macOS ARM64 builds succeeded. Recovery crash-injection exits were expected and validated by the gate. Retained results/XML/pure runner outputs alongside this report. Live source Editor compilation also completed with no errors; CLI beta15 update check confirmed current.

After verification, all three working/isolated/index hashes, approved input revisions and staged paths were rechecked; staged whitespace check passed. No unrelated work staged.

Commit: `d303c994473075d4168c506afeb131a83deb9f66`; committed source matches the tested three-file snapshot.

## Next gate

Offer fresh Phase 1 checkpoint review of the repaired snapshot. Do not treat passing implementation tests as independent review acceptance. SEC-0 damage and production starting equipment remain outside this repair.
