# Phase 2 Step 3 — attempt 2 (PASS)

## Approved scope and exact source
- Criteria **S7, S8, S13, S14**, single-step mode; Step 3 and Phase 2 complete.
- Commit `0836bb852d55b9770deda335073ceaea15dc7d75`, base `c18e6b29f6cd4b77b543bdef4419e71fe7d5200b`.
- Managed worktree `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`, clean after commit. Primary checkout not integrated.
- Spec SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Normalized Phase 2 plan SHA-256 `0195a7488b9931af5f0c06d0bbfa3ffe1a05918118ed6883094434792db6ba1f` (status tokens only normalized). [All approved plan hashes](p2-s3-attempt2-approved-plans.json). Upstream provenance/plan revisions rechecked before execution and commit.
- [12-file source/meta manifest](p2-s3-attempt2-source.json), SHA-256 `073e178e3f36a3f4801359b8c01f8dd48175cb9539eada919d02b502f7db0ed8`. Every tested file verified against committed bytes.

## Resulting behavior
Authored critical pickups map a stable source/objective/item identity to protected shared-owner IDs. The session retains those authored mappings across scene release. A narrowly scoped death-preparation operation accepts a post-equipment-loss snapshot and returns an isolated reconciled snapshot, without mutating/publishing the session. Phase 3 must include that result in its death commit.

Lost components replenish only at that boundary or first registration of a previously unmapped legacy source. Existing policy records prevent scene reload/rebinding from becoming a refresh. Live containers override saved records; unloaded saved shared owners also prevent duplicates. Missing policy records reconcile legacy collected IDs while preserving ordinary collections and usable carried legacy components. Installation suppresses critical-source availability before repair commit notifications. Capacity rejection leaves availability intact; successful collection commits the grant and collected/availability facts together.

Optional WorldPickup adapters bind through existing pickup composition and observe their objective for immediate suppression. CriticalComponentSource and RunLifecycleValidator validate identities, same-scene repair objectives, complete declared scene-locker coverage and protected access. WorldPickup caches its optional source component; managed release remains safe after native Unity destruction. Ordinary pickups and lockers retain existing semantics.

## Verification
Command from source root:
```sh
python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p2-s1 --output /private/tmp/solitude-run-lifecycle-p2-s3-attempt2
```
Expected full gate PASS. Actual **PASS**, runner exit 0: **225 pure + 345 EditMode + 123 PlayMode = 693 tests passed**, zero failures/skips. Authoring checks, verification build, shipping build and **32 standalone save/restart/recovery scenarios passed** on macOS ARM64.

- **S7:** ordinary collection IDs preserved in reconciliation; current inventory/container, pickup and persistence regressions pass.
- **S8:** domain tests cover lost collected sources, unchanged session during preparation, unloaded locker retention, live owner precedence, installation suppression, capacity rejection, coherent grant publication, duplicate mapping and undeclared-owner rejection. PlayMode covers capacity/source visibility, non-farming travel, installation and disk reopen suppression, and invalid storage authoring.
- **S13:** legacy v2 collected critical source with missing component recovers without clearing unrelated collection IDs; existing carried legacy component prevents replacement. Completion is not inferred from missing old repair facts.
- **S14:** existing lifecycle, demo, scrubber, inventory, door, interaction and recovery suites pass. The 14 teardown failures from attempt 1 are resolved by the approved source-cache repair; failed evidence retained separately.

## Limits and deferred integration
Source and protected storage are deliberately required to be accessible without repair prerequisites for this vertical slice. Validation checks declared access dependencies and scene-owner coverage, not navigation geometry; manual reachability is part of production route authoring/qualification. Unknown shared owners containing critical components reject preparation rather than duplicating supply.

The death-preparation API is implemented and tested, but lethal succession does not call it yet; that is Phase 3. No manual death journey or production-route qualification claimed. New critical fixtures are PlayMode production-root/disk-codec checks; standalone scenarios cover existing durability/recovery. Primary unrelated NPC/art changes are excluded from this isolated qualification. Primary baseline differences after verification: 0 paths; no primary product files edited.

## Evidence and next gate
[Result](p2-s3-attempt2/results.json), [EditMode XML](p2-s3-attempt2/editmode.xml), [PlayMode XML](p2-s3-attempt2/playmode.xml), [pure log](p2-s3-attempt2/pure-tests.runner.log), [PlayMode log](p2-s3-attempt2/playmode.log). Raw logs/builds: `/private/tmp/solitude-run-lifecycle-p2-s3-attempt2`.
Phase 2 commits: `700f4e21982404aad7df283e27fcf80bb32be5c6`, `c18e6b29f6cd4b77b543bdef4419e71fe7d5200b`, `0836bb852d55b9770deda335073ceaea15dc7d75`; phase base `62b17b02ae1a067bea59d8b623b2854cad6d8675`.

Paused at Phase 2 completion. Next decision: independent Phase 2 review, authorize Phase 3, or stop. No integration into primary, push or publication performed.
