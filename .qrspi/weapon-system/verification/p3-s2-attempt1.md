# Phase 3 Step 2 — attempt 1

Outcome: FAIL at PlayMode gate; stopped without staging or commit. Current source is an uncommitted implementation draft. S25/S26 have passing focused checks but Step 2 is not qualified; phase/step [!]. Normal user saves were not used. Both builds and process checks were not reached.

## Approval and source

Human approved moving activation-required baseline fixture changes into Step 2; new Step 3 restart/failure-injection coverage remains deferred. Updated normalized overview 4237e80a8a2629f7f654a62af338395e7fbaf05daa1973c311778cbbc2ea9d0e, Phase 3 486a1027edcb4eb5956ef4a45535afda19c3439f6d0a05cfa84b197fb2a07cda shown before execution. Spec remains 78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5. Upstream/approved plan digests checked. Baseline HEAD 69c1fc7e75f95508692636781c60490c197bb5b1.

46-file snapshot p3-s2-attempt1-source.json matches current working and isolated gate bytes. No source staged. Existing SOLITUDE_Start.unity, .impeccable/, PRODUCT.md and concurrent work/Reviews/InventoryPrototype/ remain outside this task, untouched and excluded. Initial preserved-work digests are retained separately.

## Draft implementation

Activated CurrentVersion 4, removed candidate factories/private staging codec path, and kept legacy v1–v3 migration completed without retroactive equipment. Strict public Boolean validation uses the validated case-sensitive fact so conversion aliases cannot override it. New/reset v4 remains pending; pending carried snapshots reject.

ContainerSaveSession/ContainerCommandService prepare the complete ordered hotbar grant before mutation and publish completion plus grants atomically under existing busy/publication guards. StartingLoadoutCoordinator owns retries and defers initial selection until persistence succeeds. Already-completed sessions skip grants and do not restore selection. StartingLoadoutDefinition is configuration only; default asset grants one maintenance wrench and selects slot 0. GameCompositionRoot completes recovery, binds/awakens the player, initializes equipment and flushes before readiness. Explicit RetryStartingEquipment restores persistence availability and retries in-memory completion without duplicate grants.

Authoring used Unity APIs. Default loadout is linked to the main config and the existing scrubber demo config/catalog; its database needed the existing wrench and shared tag-registry references. CompositionMigration is the actual bootstrap-config authoring owner and now supplies the default reference when missing. Existing player prefab/painted scenes do not require changes because the established dynamic action/view binding already supplies execution. Production/item catalog validators verify the loadout reference. Default configuration/catalog IDs remain stable; authoring JSON retained.

Baseline fixtures mark genuinely played v4 snapshots complete, preserve legacy wire cases, remove duplicate manual wrench grants, and adjust actual process item/selection expectations. Controlled lifecycle suites verify and remove the real starter before seeding their exact old equipment sets; no recovery/collection assertions were relaxed. Three tests that open a second, fresh demo inside their test bodies were missed by that setup adjustment.

## Verification and failures

Live source recompile completed successfully, no errors. Authoring transport/nested execution both succeeded; two runtime configurations compiled. Unity CLI updated through its self-update command from beta.15 to beta.17 per the Unity skill; Editor version/package unchanged.

Command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output /Users/jeremyvalentine/SOLITUDE/Logs/WeaponSystemP3S2Attempt1.
Expected: full tests/build/recovery gate passes. Actual: results.json Failed at playmode exit 8. Pure 300 passed/0 failed; Editor 566 passed/0 failed/0 skipped; PlayMode 226 total, 223 passed, 3 failed, 0 skipped. No builds/process qualification claimed.

All three StartingWeaponTests passed: real new run is durably equipped and actual Primary swings; a temporary authoring loadout selects slot 1 after awakening; failure writing completed grants keeps controls closed, leaves one in-memory wrench, retains a valid pending disk snapshot, and explicit retry saves/selects once without duplicates. All 16 new StartingLoadoutCoordinatorTests pass in pure/Editor runs, including atomic publication, rejected quantities/capacity/selection/filter/ownership, reentrancy, thrown/failed flush, immutable spec, replay and moved equipment.

Failing existing cases:

1. SurvivorSuccessionFailureTests.DeathClosesJournalAndInventoryAndSuppressesHeldMovementUntilRelease, line 291: expected cache quantity 6, observed 7. This body destroys the prepared root, opens a new scrubber demo, then seeds 4+2 supplies. Its fresh demo now starts with one wrench; the recovery cache correctly includes it.
2. SurvivorSuccessionShipCompatibilityTests.AuthoredScrubberKeepsRunningAndReaderBindingsAcrossDeathButResetsOnReload, line 235: expected cache quantity 6, observed 7. This body likewise opens a new demo after Prepare, then seeds 4+2 fuses. The additional recovered item is its fresh starter; subsequent expected inventory total would also need the controlled fixture setup.
3. SurvivorSuccessionSliceTests.OpenJournalClosesWithoutRebindingAuthoredDemo, line 289: expected cache quantity 2, observed 3. A fresh demo opened inside the case receives one wrench before its seeded equipment.

These failures occur after the new demos load successfully and the preceding succession/UI/state assertions pass. No runtime grant/transaction correction is indicated by these failures. Tests stopped the gate as required. The missing fixture setup must be repaired and checked before any successful Step 2 commit.

## Proposed bounded repair

Only these three test files: invoke the existing WeaponSystemTestFixtures.ClearStartingWeaponForSeededScenario helper after each affected fresh demo load and before exact equipment seeding/composition. That helper explicitly verifies the real completed starter grant, removes it with the existing command, clears fixture selection and flushes; original cache totals/recovery/UI/ship assertions remain intact. No gameplay/schema/authoring changes or count-assertion weakening proposed. Rerun complete isolated gate with unique attempt-2 evidence. Await human approval under qrspi-implement failure handling.
