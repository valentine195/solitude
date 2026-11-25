# Phase 3 Step 2 — attempt 2 (passed)

## Inputs and source
- Criteria: S2, S3, S12, S14.
- Commit: `13c9f1709ea46d4097f6545adb3d46734eaaea71`, based on `7efca2abc4b388bc7b1352943a68cef28414f278`.
- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Approved normalized Phase 3 plan SHA-256: `5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29`.
- Complete revisions: [approved manifest](p3-s1-repair-approved-plans.json); normalize only status-marker tokens and overview Status cells to `[ ]`.
- Tested source: [exact hashes](p3-s2-attempt2-source.json). Source, upstream provenance and approved plan hashes rechecked before commit; committed files rechecked afterward. Product worktree and index are clean.

## Behavior delivered
Production health notifications latch the expected colonist identity at the session root and gate input immediately. The root waits for safe post-publication execution, then uses the existing durable succession coordinator. Failed preparation or saving retains the same request, successor and receipt and automatically retries without another health notification. Scene teardown cannot lose the session-owned request. Ordinary travel cannot overtake queued death; an already-running scene transition resolves before succession continues to cryobay.

A lethal saved snapshot completes death before readiness. Awakening restores authored maxima, values, recovery timing, body/input/selection and proximity using existing adapters and retained readers. Existing demo succession policy and ordinary vitals reset behavior remain compatible. Oxygen/hunger damage rules were not changed.

The approved repair adds the existing journal prefab only to the disposable test scene, before root composition. The original open-journal, real input, held-movement and death assertions remain intact. Shipping scenes and journal implementation were not edited.

## Verification

```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p3-s1 --output /private/tmp/solitude-run-lifecycle-p3-s2-attempt2
```

Expected and actual: full gate **Passed**, macOS ARM64. [Final results](p3-s2-attempt2/results.json).
- 236 pure Domain/Application tests passed.
- Authoring checks passed.
- 356 Unity EditMode tests passed.
- 136 Unity PlayMode tests passed.
- Total automated test executions: 728; no failures.
- Both verification and shipping ARM64 builds succeeded.
- All 32 standalone scenarios passed, including existing save/recovery interruption and living-colonist reopen cases.

## Criterion evidence
| Criteria | Observable validation |
| --- | --- |
| S2/S14 | Actual `Damage(1000)` reaches zero and automatically awakens a new colonist with authored vitals/maxima, empty inventory/hotbar, cleared selection/timing/body velocity, no cache, and retained vital/container readers. No debug death request is used. Existing inventory/demo regressions pass. |
| S3 | Invalid preparation keeps dead control gated, gear/expedition unchanged until commit, and the same proposed successor. Duplicate health presentations do not create another replacement. Open journal closes; held old movement cannot move the successor; fresh movement works after release. Lethal notification during travel survives release and returns one successor to cryobay. |
| S12 | Save failure retains one committed receipt and expedition and retries forward when storage returns. Saved lethal health completes before readiness; readiness observation detects no playable lethal/pending state. Existing committed receipt and persistence regressions pass. |

All six RunLifecycleDeathTests scenarios passed, as did existing relevant regressions. Logs, reports and disposable saves are under [attempt 2](p3-s2-attempt2/). Failed [attempt 1](p3-s2-attempt1.md) remains preserved separately.

## Limits and gate
No manual gameplay journey or independent Phase 3 review has run. Death-specific built-player interruption qualification remains later planned work; the existing standalone gate does not by itself establish every death-specific crash checkpoint.

Changes remain in the isolated managed worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`; primary-checkout gameplay/art/NPC files were not modified or integrated.

Step 2 is complete and committed. Phase 3 remains in progress with Step 3 unstarted. Next decision: continue to the approved Step 3 (one bandage/one ration allowance per expedition while preserving permanent loot), request an isolated checkpoint review, or stop. No Step 3 implementation has begun.
