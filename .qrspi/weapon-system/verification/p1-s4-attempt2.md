# Phase 1 Step 4 — attempt 2

Outcome: BLOCKED on one fixture assumption; no staging or commit.
Human authorized repairing deferred delivery and initializer compatibility. Spec remains `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`; normalized overview `3de5bf9dc968659266df2a1d2d521cf71ff9344d30bc45c38463665368f61b47`, Phase 1 `b499d89078b2a5c615d983a7d061ec728ceacabec20c87045bbfd33316ae3c9d`. All plan/provenance/source hashes verified on resume. Twelve-file final source snapshot p1-s4-attempt2-source.json matches isolated gate and working tree; rechecked after failure.

## Repairs and evidence

Primary pending input may reach the next LateUpdate instead of being discarded by strict frame equality. A UI-consumption version invalidates pending activation across that boundary. The press position is captured and raycast against existing UI after EventSystem processing; moving away before dispatch cannot turn a UI press into an attack. Held-control/lifecycle cancellation remains intact. The Editor coroutine test waits through two frame boundaries so its assertion follows actual player-loop LateUpdate. That real-input test now passes through mouse/trigger hold/repress, policy gates, reset, duplicate-owner, release/rebind, disable/re-enable, UI consumption and host reinitialization cases.

SceneBindings.Initialize preserves its existing signature with TaskJournal last and optional. Game root configures capabilities separately before initialization, following existing lifecycle configuration ownership. The existing initializer-contract regression passes without modifying it.

## Criterion-linked verification

Command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output Logs/WeaponSystemP1S4Attempt2.
S7/S9/S10/S11/S15/S16/S24/S30: pure 269 passed; authoring passed; **516 Editor passed, 0 failed/skipped**. PlayMode **141 passed, 1 failed, 0 skipped**. Builds/player scenarios did not run. Retained p1-s4-attempt2-results.json, p1-s4-attempt2-editmode.xml and p1-s4-attempt2-playmode.xml.

The actual HUD click/consumed-frame suppression, replacement/transfer/release and held-mouse scene-reload tests passed. ActualPrimaryControlsDriveSelectedSpriteAndHotbarWithoutRepeatingOrConsuming passed its preceding mouse/trigger/hold/HUD/wrench-Use/empty-selection assertions but failed at line 79 while looking for a production item with ConsumableKind != None. Exception: Sequence contains no matching element. Production catalog entries before the wrench are Battery and O2Canister, both actionless and not food/medical vitals consumables. This fixture assumption was incorrect; it is not evidence of a product input failure.

Proposed human decision: change only that PlayMode fixture's predicate to choose an existing actionless catalog item and assert no attack or consumption. Retain consumable-specific coverage in ItemActionAuthoringTests.LegacyActionlessItemsAndConsumablesStillCompileWithoutRegistry and actual Use/Primary separation in ItemActionInputTests/PlayerVitalsInputTests. Do not add a production consumable, mutate the catalog or weaken the existing gameplay tests. Rerun the full gate.

Unrelated SOLITUDE_Start scene modification and review archives remain preserved/outside scope. Step 4 and Phase 1 marked blocked under qrspi-implement Error Handling. No Phase 2 work started.
