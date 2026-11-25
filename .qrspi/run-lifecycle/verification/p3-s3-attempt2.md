# Phase 3 Step 3 — attempt 2: PASSED

Commit: `1892df57bc02362c11d81291dcace071d2010798`; parent: `13c9f1709ea46d4097f6545adb3d46734eaaea71`. Product tree/index clean after commit. Feature remains in the isolated managed worktree; no integration into the primary checkout.
Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved plan manifest: `p3-s1-repair-approved-plans.json`; phase 3 normalized SHA-256 `5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29`. All approved upstream and plan revisions matched before execution and after verification. Exact 11 source hashes: `p3-s3-attempt2-source.json`; unchanged through verification and commit.

## Change

Two explicit source identities use the saved expedition's collection facts. The existing item-grant transaction consumes each one-item allowance before publishing. Capacity failure leaves it available. The existing death commit replaces expedition facts; ordinary collections, discoveries and shared storage retain their existing lifetime. The opt-in authored supply marker and existing command publication reconcile loaded pickup visibility, including inactive collected objects. Scene rebinding reads saved facts without issuing a refresh. No new reset authority or registry.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p3-s1 --output /private/tmp/solitude-run-lifecycle-p3-s3-attempt2`.
Expected: complete passing gate, source-bound criterion checks below. Actual: `results.json` Passed, macOS ARM64. 244 pure tests, 364 EditMode tests, 138 PlayMode tests (746 executions), authoring validation, verification and shipping builds, 32 standalone save/restart/interruption scenarios passed. Deliberate process kills are expected in interruption scenarios. Logs/XML/results and disposable save snapshots are retained in `p3-s3-attempt2/`; builds remain at the command's output path outside Assets.

| Criteria | Concrete evidence | Actual result |
| --- | --- | --- |
| S6 | `ExpeditionRefreshTests.FirstAllowanceCommitsWithGrantAndConsumptionDoesNotRenewIt`, `CapacityFailureDoesNotConsumeOrDirtyAllowance`, all three `DeathReplacesRemainingAllowanceOnceAndRetryCannotRenewIt` cases, `OnlyTwoExplicitSingleItemSourcesAreAcceptedAndDuplicateBindingsCannotGrant`, `SharedStorageCanAccumulateTwoItemsPerExpeditionAndNewSaveStartsOver` | Passed; first allowance, atomic observation, full inventory, zero/one/two prior collections, retry, duplicate binding and locker accumulation preserve bounded world allowance. |
| S5/S7 | `BindingAndReopeningPreserveAllowanceEncounterAndPermanentFacts`; `RunLifecycleRefreshTests.LoadedSuppliesRefreshAtDeathWhileOrdinaryPickupStaysHiddenAndReopenDoesNotRefresh` | Passed; rebind/reopen preserve expedition facts; ordinary collections/discoveries stay persistent. Existing pickup/locker regressions passed in full suite. |
| S16 | The above rebind/reopen test and shared-storage/New Save test | Passed; reentry/reopen do not renew; New Save returns first allowance and clears shared storage. Existing travel/death tests passed. |
| S14 | Both new PlayMode refresh tests, including `CapacityRejectionLeavesLiveAllowanceAndUnloadedSourceUsesCurrentExpedition`; complete existing suite | Passed; existing pickup commands and scene ownership remain reusable; loaded inactive supply objects show immediately on death commit, unloaded sources read the same saved expedition. |

## Limits and next gate

The new PlayMode supply fixtures exercise live WorldPickup adapters with the real session/death transaction and codec. The full gate's standalone scenarios qualify existing save/recovery paths; they do not author or walk the final bandage/ration production route. Phase 4 supplies production authoring and encounter binding; phase 5 retains manual journey and feature-specific process interruption qualification. No independent Phase 3 review has run. Phase 3 implementation is complete; human gate before review or next phase.

## Prior attempt

Attempt 1 failed on two new test assertions calling a concrete-only method through IContainerReader. Human approved the test-only replacement with GetSlot(index).IsEmpty. Attempt 1 evidence is retained unchanged. No assertions or product behavior were removed to pass.
