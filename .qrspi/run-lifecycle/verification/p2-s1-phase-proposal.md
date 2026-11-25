## Phase 2: Persistent ship progress

### Dependencies
- Depends on: 1
- Why: Requires phase 1's versioned saved snapshot, session dirty/capture contract and restore-before-control scene binding.

### Step 1: Persist repair consumption and scrubber state together
- [ ] Status marker
- Criteria: S9, S14.
- Files: `Domain/Restoration/RestorableSystemState.cs`, `Domain/Restoration/ScrubberEncounterState.cs`, `Domain/ContainerSaveSession.cs`, `Features/World/Restoration/RestorableSystem.cs`, `Features/World/ScrubberEncounter/ScrubberEquipment.cs`, `Features/World/ScrubberEncounter/ScrubberComposition.cs`, `Tests/Domain/RestorationTests.cs`, `Tests/Domain/ScrubberEncounterTests.cs`, `Tests/PlayMode/RunLifecycleShipPersistenceTests.cs` (new), `Core/Composition/SceneBindings.cs`.
- Changes: Bind stable authored ship-system IDs to saved installation/repair/running/recovery facts. Make item removal and the permanent result one command-boundary snapshot; no notification/save can observe consumption without installation. Restore facts without consuming again; freeze scrubber advance while unloaded and closed. Restore feedback/task state through existing observers. Preserve scene-local disposable demo state when no production persistence binding exists. Use the existing SceneBindings owner to bind and release generic RestorableSystem persistence before junction interactions/control and before inventory ownership is released. ScrubberComposition retains ownership of scrubber equipment binding; do not add a second lifecycle owner.
- Tests: S9 — consume exactly one fuse, capture/reload installation and partial recovery, restart/save/rebind, then operational state; no extra consumption or offline time. Inject command/save observation and assert no consumed-only snapshot. S14 — existing restoration/scrubber/demo compatibility tests pass; production persistence tests do not assert demo resets. Run full required gate.
- Recovery: Undo feature-only adapter changes or resume from the complete saved repair snapshot. Never attempt rollback by returning a consumed item without removing its corresponding permanent fact.
- Risk: High — consumable loss and permanent progress must remain coherent under callbacks/saving.

### Step 2: Restore permanent power and shortcuts before traversal
- [ ] Status marker
- Criteria: S10, S14.
- Files: `Features/World/Restoration/RestorationDoorGate.cs`, `Features/World/SolitudeStart/SolitudeSlidingDoor.cs`, `Features/World/OpeningSlice/OpeningPowerSocket.cs`, `Domain/ContainerSaveSession.cs`, `Core/Composition/SceneBindings.cs`, `Editor/SaveableIdValidator.cs`, `Tests/PlayMode/RunLifecycleShipPersistenceTests.cs`, `Tests/PlayMode/RestorationDoorTests.cs`.
- Changes: Add stable production identities for permanent door/power facts. Apply saved permission/open state and correct blocker immediately before gameplay; restoration must not replay battery checks or unlock transactions. Preserve existing ungated/gated interactions and demo-only behavior. Missing/duplicate persistent identities fail validation.
- Tests: S10 — unlock, unload/rebind and reopen; door permits interaction/traversal and correct collision immediately, repair power remains present without another item. Closed unpowered door still rejects. S14 — disposable door animation and repair-gate tests remain valid. Run required full gate.
- Recovery: Preserve door asset GUIDs and restore only newly authored persistence fields if a binding fails. Keep feedback and collider reconciliation idempotent so retry cannot charge or relock the door.
- Risk: Medium — restoring visual openness without collider or gate permission would block the route.

### Step 3: Reconcile objective-bound critical sources with saved storage
- [ ] Status marker
- Criteria: S7, S8, S13, S14.
- Files: `Domain/ContainerSaveSession.cs`, `Domain/PickupCollectionService.cs`, `Domain/Restoration/CriticalComponentSourceState.cs` (new), `Features/Items/Core/WorldPickup.cs`, `Features/World/Restoration/CriticalComponentSource.cs` (new), `Editor/RunLifecycleValidator.cs` (new), `Tests/Domain/CriticalComponentSourceTests.cs` (new), `Tests/PlayMode/RunLifecycleShipPersistenceTests.cs`.
- Changes: Author objective/source/item/shared-owner relationships only for slice-critical items. Read live and unloaded saved container records to identify accessible retained supply. For unfinished objectives with lost components, reconcile the source at permitted death/migration boundaries; completed objectives suppress their sources permanently. Do not change ordinary collection/locker semantics or permit repeated in-expedition collection. Validate source accessibility, protected storage and unique IDs; fail authoring for storage behind its own prerequisite. Complete legacy reconciliation after objective authoring is available. Expose a narrowly scoped session operation consumed by phase 3's death commit.
- Tests: S8 — source already collected plus no surviving component becomes available at the reconciliation boundary; saved unloaded locker component prevents replacement; permanent installation prevents replacement; full-inventory rejection preserves source. S7 — ordinary pickups/locker loot are untouched. S13 — legacy collected fuse with missing inventory is reconciled without clearing other collected IDs. S14 — collection/capacity/transaction regressions pass, plus full gate.
- Recovery: Keep a snapshot of original collection records for comparison. Reconcile through session facts, not arbitrary GameObject activation, so retry is repeatable. If an objective mapping is wrong, stop and fix its authored mapping rather than resetting all pickups.
- Risk: High — checking only loaded owners duplicates items; collected-only legacy saves can otherwise softlock.
