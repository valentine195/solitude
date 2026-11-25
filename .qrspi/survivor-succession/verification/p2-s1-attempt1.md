# Phase 2 Step 1 — equipment handoff verification

- Source commit: `756d0dcc8526afb2d5ca1cd5423fd893504f8334` (same product bytes as the recorded pre-commit source snapshot).
- Spec SHA-256: `f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4`.
- Approved normalized Phase 2 plan: `67c7472b71c62f3373c486ca7c166a2a76cd7c4c7622a9352de0a6ba3283e631`.
- Snapshot: `p2-s1-attempt1.source.json`; source files matched the isolated gate and remained unchanged before commit.
- Execution: interactive, single step. Runtime death integration remains deferred to Step 3.

## Criterion-linked evidence

| Criteria | Check | Expected | Actual |
| --- | --- | --- | --- |
| S4 | SurvivorEquipmentHandoffTests.BothFullSourcesMoveTogetherWithoutMergingOrChangingReaders | Mixed/max stacks across both full sources conserved, independent destination, sources empty, reader objects retained | Passed; individual stack references retained; 27 fuse units, 2 tools, 5 food units |
| S9 | ReceiptPreventsRepeatingConsumedTransitionEvenAfterNewEquipmentArrives; EmptyZeroCapacitySourcesCanCommitOnceWithoutItems | Retry returns original receipt without transferring new items; empty transition consumed once | Passed |
| S10 | InvalidAndInsufficientTargetsLeaveAllOwnersUnchanged; ChangedThenRestoredSlotsRejectStalePlansWithoutMutation (all 3 owners); ReplacedOwnerGenerationRejectsPreparedHandoff (all 3 owners); ResizeRejectsPreparedHandoffAndPlanCannotCrossServices | Invalid/filter/capacity/stale/unavailable/cross-service requests reject without mutation or receipt | Passed |
| S10 | EveryObserverSeesCompleteOwnershipAndReceiptWithReentrancyBlocked; ThrowingObserversCannotLoseReceiptOrSuppressOtherNotifications | Complete ownership and receipt visible before callbacks; reentrancy returns Busy; throwing observers cannot interrupt commit | Passed |
| S11/S12 | Full Domain/Application suite, Unity EditMode/PlayMode, authoring, verification/shipping macOS ARM64 builds, production player save/recovery scenarios | Existing transfers/filter/revision/publication behavior and gameplay/persistence gate preserved; successful results.json | Passed: 119 pure, 217 EditMode, 51 PlayMode, zero failures; both builds and all 28 player stages passed |

## Command

```sh
DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-p2-s1-gate --output Logs/SurvivorSuccession/p2-s1-attempt1
```

Exit 0; `Logs/SurvivorSuccession/p2-s1-attempt1/results.json` status `Passed`, macOS ARM64. No skip-build. `git diff --check` passed. Tests are independent of Unity in Domain/Application and also compiled/executed by Unity. No manual gameplay claim for this pure command step; equipped death and recovery interaction are not wired yet. Phase 1 review F1/S7 remains human-deferred technical debt recorded in decisions.md, not fixed by this work.
