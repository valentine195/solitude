# Phase 3 Step 2 — attempt 1 (blocked)

## Inputs and snapshot
- Criteria: S2, S3, S12, S14.
- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Approved normalized Phase 3 SHA-256: `5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29`.
- All plan revisions: [approved manifest](p3-s1-repair-approved-plans.json).
- Base: `7efca2abc4b388bc7b1352943a68cef28414f278`.
- Tested uncommitted files: [source hashes](p3-s2-attempt1-source.json), unchanged after failure. No files staged or committed.

## Attempted implementation
Production vital notifications latch the expected colonist identity at the session root and gate input immediately. Succession runs after publication, retaining its pending request through failed preparation, save failure and scene release. Lethal saved health is processed before readiness. Existing demo policy remains isolated. New-colonist awakening restores authored maxima, values and timing while retaining readers; ordinary vitals reset behavior remains compatible.

## Verification

```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p3-s1 --output /private/tmp/solitude-run-lifecycle-p3-s2-attempt1
```

Expected: complete gate Passed. Actual: **Failed** (runner exit 1; PlayMode exit 8).
- Pure Domain/Application: 236 passed, 0 failed.
- Authoring validation passed.
- EditMode: 356 passed, 0 failed.
- PlayMode: 135 passed, 1 failed.
- Builds and standalone scenarios were not reached; no full qualification or manual journey claimed.

Five new production tests passed: actual Damage automatically retires A and awakens B with fresh vitals/empty gear/retained readers (S2/S14); preparation failure and duplicate notifications remain gated and recover without another health event (S3); durable-write failure retains one receipt and retries forward (S3/S12); death during already-running travel returns one successor to cryobay (S3); saved lethal health completes before readiness and queued death rejects ordinary travel (S12). Existing tests passed, but the remaining required journal/held-input case prevents completion.

## Failure and concrete repair proposal
`RunLifecycleDeathTests.OpenJournalAndHeldMovementCloseWithoutMovingTheSuccessor` throws `Sequence contains no elements` at line 150 while looking up `TaskJournalView`, before it mutates health. The production Wakeup scene has no authored journal view. The new test fixture omitted it. Existing optional SceneBindings journal composition is available, as is `Assets/_Project/Systems/Tasks/UI/TaskJournal.prefab`.

Repair only the already-approved `Tests/PlayMode/RunLifecycleDeathTests.cs`: during its sceneLoaded authoring callback, instantiate the existing journal prefab into the disposable scene and supply it to SceneBindings.journalView before root binding. Keep the real input, open-journal, held-movement, death, and post-awakening assertions intact. Add the required editor namespace import within the existing UNITY_EDITOR guard. No shipping scene or journal product code needs editing, and no plan revision is required. Rerun the complete gate as attempt 2; do not advance to Step 3.

Full [failure](p3-s2-attempt1/failures.json), [results](p3-s2-attempt1/results.json), logs/XML and disposable saves are preserved. Phase 3 and Step 2 marked blocked. QRSPI Implement requires “If a step fails, stop and report the issue” and “Wait for the human decision.” Product source has not changed after failure inspection.
