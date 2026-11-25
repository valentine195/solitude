# Phase 2 Step 4 — attempt 2 (PASS; independent re-review pending)

## Scope and exact inputs
- Criteria S8/S10/S13/S14, review findings F1/F2; single repair step.
- Commit `d07ace57916e75533e915e097224e0e041e05ba5`; repair base `0836bb852d55b9770deda335073ceaea15dc7d75`. Full Phase 2 base remains `62b17b02ae1a067bea59d8b623b2854cad6d8675`.
- Managed worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`, clean after commit. Primary checkout not integrated.
- Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Approved normalized overview `dc6e3dec5d02179bc7c8fa565ab91e422de6afbaf5d6ad6d8e4c633eff4444cb`; Phase 2 `bc733c6ee01970b3a5a493d42292baf0f4b886ae3d4f1953f0546fd43b2ef5a5`.
- [All approved plan hashes](p2-s4-attempt2-approved-plans.json), only status tokens normalized. All upstream provenance and plan revisions rechecked before execution and commit.
- [Seven-file source manifest](p2-s4-attempt2-source.json), SHA-256 `128e2bbfb7bde17387403f7f88b833e1d8c2c2e924bc3829574378aaf2891c52`; every tested file checked against committed bytes after verification.

## Repairs
F1/S8: CriticalSourceSaveRecord now carries optional item/shared-owner policy IDs with deep cloning/validation. New sessions reconstruct policies before source scene binding. Reconciliation traverses every saved source record rather than only this process's bindings. Old metadata-free records remain readable; unresolved unfinished objectives reject death preparation until authored policy is known. Completed objectives can be safely suppressed without guessing component policy. Binding checks saved/authored consistency and does not refresh an existing source merely on travel.
F2/S10: Door Awake and persistence binding share presentation reconciliation using the current open state. Delayed Awake no longer reinstates a closed frame/blocker over an open shortcut.
The approved attempt-1 test-isolation repair detaches the fixture Author hook before teardown New Save, so source metadata is cleared before restoring the non-fuse production catalog. Runtime validation remains strict.

## Verification
Command from source root:
```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s4-attempt2
```
Actual **PASS**, exit 0; **229 pure + 349 EditMode + 125 PlayMode = 703 tests passed**, zero failed/skipped. Authoring validation, verification/shipping macOS ARM64 builds, and all **32 standalone save/restart/recovery scenarios passed**. The completed result was recovered on continuation; exact tested source and approved inputs were unchanged, so no additional rerun was necessary.

- S8/F1: new-session, no-source-binding loss reconciliation returns obtainable source without early publication; retained unloaded locker and installed objective prevent replacement. PlayMode confirms metadata survives actual disk encoding/decoding before a new session prepares reconciliation.
- S13: metadata-free saved record cannot silently pass an unresolved death boundary; authored mapping upgrades it without renewing the current expedition. Malformed policy and mismatched authoring rejected; deep-copy isolation covered.
- S10/F2: initially inactive door hydrated before Awake, then activated, retains open frame, disabled blocker and coherent interaction state. Existing door regressions pass.
- S14: full pickup, inventory, composition, door, scrubber, lifecycle, demo and save-recovery suites pass; prior failed attempt retained separately.

## Limits and next gate
Automatic death integration remains Phase 3; no manual production journey claimed. Tests cover the review's concrete triggers, but the old `reviews/phase-2.md` FAIL remains historical and is not replaced by implementation evidence. New independent Phase 2 review is pending at this commit. Its scope must include the whole phase plus repair, not only the repair diff.

Primary baseline comparison: 2 previously fingerprinted paths differ from the earlier baseline; this step did not modify primary product files and did not stage/commit any of its unrelated changes. No integration, push or publication.

[Results](p2-s4-attempt2/results.json), [EditMode XML](p2-s4-attempt2/editmode.xml), [PlayMode XML](p2-s4-attempt2/playmode.xml), [pure log](p2-s4-attempt2/pure-tests.runner.log), [PlayMode log](p2-s4-attempt2/playmode.log). Full raw logs/builds at `/private/tmp/solitude-run-lifecycle-p2-s4-attempt2`.

Paused at the repair-step gate, awaiting authorization for independent re-review. Phase 2 implementation markers are complete; Phase 3 not begun.
