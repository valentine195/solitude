# Phase 4 Step 1 — attempt 1: PASSED

Feature commit: `ef62c7830eb16a970f853d9fd357b7f2e2cf758d`. Parent prerequisite commit: `0ca012c4aa320bff0d18630c5cb82d5d28deb3bf`; reviewed Phase 3 base: `1892df57bc02362c11d81291dcace071d2010798`.
Branch: `codex/run-lifecycle-playable-slice`, isolated worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`. Main checkout product files remain unchanged and clean.
Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`. Approved normalized complete-plan manifest: `p3-s1-repair-approved-plans.json`; Phase 4 SHA-256 `3943182fee737d224ffc2eadaab9369e8a256767ac7fd734084320bb4fe541c8`.
All upstream provenance, approved plan hashes, and 263 source digests matched before and after verification and before completion. Exact verification snapshot: `p4-s1-attempt1-source.json`; source baseline/provenance for imported NPC content: `p4-s1-prerequisite-source.json`.

## Change

An optional scene-owned SEC-0 adapter uses existing production session expedition facts and the ordinary PlayerInteractor dispatch. It stores defeat before stopping movement, attacks, visuals and solid collision. Rebinding/reopening reads the existing fact. After a death command completes publication, it restores the authored position, patrol and physics once for the new expedition. No second save authority, enemy health framework or general AI serialization was added. Actors without the adapter retain their existing demo behavior.

The NPC prerequisite was copied byte-for-byte from the user's checkpoint `8a6d2a6` and committed separately, before the feature delta. No main-checkout scene/art replacements or unrelated runtime optimizations were imported. The complete verified overlay equals the final two-commit product snapshot.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-s1-attempt1`.
Expected: full passing authoring/test/build/restart gate and criterion-specific encounter behavior.
Actual: `results.json` Passed, macOS ARM64; 252 pure tests, 372 EditMode tests, 150 PlayMode tests; zero failures. Authoring validation and both verification/shipping builds passed. All standalone save/restart/recovery scenarios passed; deliberate process kills are expected in interruption scenarios.

| Criteria | Concrete evidence | Actual |
| --- | --- | --- |
| S4 | `RunLifecycleEncounterTests.OrdinaryInteractionStopsPhysicsAndAttacksAndCannotDefeatTwice` | Passed: ordinary PlayerInteractor dispatch records one defeat without advancing expedition; a charging actor loses collision, simulated body, visuals and attacks; duplicate attempts do not add facts. |
| S4 | `DeathRestoresOneAuthoredPatrolAndRebindingDoesNotAdvanceAgain` | Passed: duplicate death receipt restores one authored patrol; collider/body/visual return, original position is restored, rebind retains expedition, actor can fire again. |
| S5 | `DefeatSurvivesSceneRecreationAndCodecProcessResumeFixture` | Passed: unload/recreated scene and a codec-reconstructed session preserve defeat, survivor and expedition identities; defeated actor cannot attack or collide. |
| S4/S5 | `MissingDuplicateAndForeignPlayerBindingsAreRejectedWithoutFacts` | Passed: missing/duplicate identity validation and unavailable/foreign-player interactions leave facts unchanged. |
| S14 preserved | Existing SecurityBotBrain/Sec0Runtime/Sec0Encounter tests plus complete gate | Passed: navigation/stun/demo behavior and existing lifecycle, inventory, interaction and save recovery regressions remain compatible. |

Raw logs/XML/results and disposable save snapshots are retained under `p4-s1-attempt1/`; built applications stay at `/private/tmp/solitude-run-lifecycle-p4-s1-attempt1`, outside Assets. This step made no production scene/prefab layout changes. The standard-interaction test registers its fixture target before using PlayerInteractor; it does not claim an authored production proximity/input journey.

## Limits and next gate

The codec reconstruction is the encounter-specific process-resume fixture; the standalone gate covers existing save/recovery behavior, not a manually played production SEC-0 route. Actual supply/route authoring remains steps 4.2/4.3. Dedicated death-specific process interruption and manual A→death→B qualification remain Phase 5. No independent Phase 4 review has run. Single-step mode stops after this committed step; next is Phase 4 Step 2.
