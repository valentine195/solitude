# Phase 3 Step 1 attempt 2 — passed and committed

Commit: ae579b10fbb43f7f5e6eadf8d614b6e785bc3b8a. Exact source: p3-s1-attempt2.source.json, base a74bc2c37a0e52298923ceca0915aeca1c5526bc plus four scoped files. Spec SHA-256 f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4; normalized approved Phase 3 SHA-256 2f72871d1479b7469180bd384385583292c0cc873435acbbe36b2777453655db. Upstream approved input hashes, source/gate bytes, staged bytes and final commit were rechecked unchanged. Working tree clean; diff check passed.

## Qualification

Command: DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-p3-gate --output Logs/SurvivorSuccession/p3-s1-attempt2

Expected: complete Passing result, no missing/failed checks. Actual: exit 0, results.json Passed, macOS ARM64; 121 pure, 220 EditMode, 66 PlayMode, zero failures (407 passing test executions). Authoring validation passed; verification/shipping native builds passed; all 28 production player save/restart/recovery stages passed (42 overall gate stages). Test metadata imported and validated. This is Step 1 qualification, not a final feature review or durable succession save/load guarantee.

## Changes and approved repair

Recovery Release now unregisters its nearby target before clearing context/deferred Unity destruction. Coordinator/runtime reset/ship code remains unchanged. New fault adapters exist exclusively in PlayMode test source, wrapping real production preparation, command commit, avatar reset and acknowledgment paths. Two pure tests add activation/acknowledgment reentrancy checks.

Human-approved attempt-1 repair changed only the mixed-item test's demo-only fuse to Wakeup's registered O2 canister (8d158110df66c4994b8d575233f619c5). Retained exact per-item totals, partial/empty/rejected deaths, stale identities and recoverability assertions. Production catalogs unchanged. Prior failed evidence remains preserved.

## Criteria

- S2/S8/S10: ReentrantActivationAndAcknowledgmentCannotStartAnotherDeath (two cases) passes; requests/retry during the boundary remain Busy, one commit/activation/acknowledgment and one distinct successor. UncertainAcknowledgmentRetriesWithoutReactivationOrAnotherMessage passes through real feedback: visible AI message delivered once, activated survivor and recovery owner retained, gameplay gated until retry completes, delayed death/retry do not replay.
- S4/S9/S10: RejectedPreparedHandoffReleasesOnlyItsEmptyOwnerAndCanTryAgain passes: inactive empty prepared owner removed on pre-commit rejection; carried stack references/quantities and older cache conserved; fresh attempt produces exactly one successor/cache/acknowledgment. RevisionChangeBetweenPreparationAndCommitRejectsTheWholeHandoff passes: real command revision change rejects atomically, preserves all equipment, then retries with seven total units.
- S2/S4/S7/S8/S10: TransferDragAndHeldPointerCannotActAfterCommittedFailureOrRetry passes: transfer modal closes, gesture canceled, pointer delivery blocked while held, committed six-unit cache stays inactive until activation; same pending successor/receipt after failed retry; stale captured presenter callbacks cannot mutate empty/new carried equipment or older/new cache contents. Release restores pointer input; commit and activation happen once.
- S2/S4/S8/S9: PartialCachesEmptyDeathAndRejectedAttemptConserveEveryItem passes across five distinct identities: earlier partial cache, later equipped cache, empty death and rejected missing-resource attempt retain 105 batteries plus three O2 canisters; no cache for empty death, four permanently retired identities, three independent nonempty cache owners, four success acknowledgments. Older cache opens through real proximity/production E interaction.
- S7/S10: ReleasedCacheDropsFocusSynchronouslyWithoutReleasingOtherOwners passes: disable preserves contents/owner, re-enable restores proximity, explicit release removes nearby target before destruction; player/hotbar readers remain registered. DeathClosesJournalAndInventoryAndSuppressesHeldMovementUntilRelease passes in the actual composed scrubber demo: screens close, held movement neutralized, new input works, equipped/empty deaths conserve caches.
- Regression: prior succession, transaction, inventory/pickup/locker, ship/door/scrubber and player save/recovery checks remain passing through the complete gate.

## Remaining work

Phase 3 Step 2 authored ship-scenario qualification/manual acceptance remains unstarted. Final review not performed. Previously deferred positive awakening-focus coverage and later UI normalization remain recorded follow-ups. No scene-transition or full save/load schema for survivor/cache/ship state is added.
