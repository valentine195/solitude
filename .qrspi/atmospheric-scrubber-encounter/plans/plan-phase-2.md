## Phase 2: Collect and repair

### Dependencies
- Depends on: 1
- Why: Requires Phase 1's authored room, disposable catalog/config, scrubber state reader and safely composed screen/access endpoint. This phase produces the installed-fuse state and action contract needed by Phase 3.

### Step 1: Collect one replacement fuse within the room
- [x] Status marker
- Criteria: S1, S3 — complete same-room item availability and collection rules.
- Files: new `Assets/_Project/Features/Items/Definitions/ReplacementFuse/ReplacementFuse.asset`, `ReplacementFusePickup.asset`, `PF_ReplacementFuse.prefab`; `Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberItemDatabase.asset`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs`; new `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs`; include imported metadata.
- Changes: Author a fuse definition with stable GUID-derived identity, one-item pickup and clear icon/name using existing visual assets where suitable. Register it in the local catalog and place one reachable pickup in the room with a unique scene-instance SaveableId. Reuse WorldPickup and SceneBindings registration; no new inventory collection path. Keep the production database unchanged. Remove no existing definitions or pickups outside the encounter.
- Tests: S1 — item reachable from initial spawn without repair prerequisites; S3 — normal focus/input grants exactly one fuse, repeat activation never grants again, full inventory preserves pickup and shows capacity feedback. Authoring checks validate catalog membership and unique instance identity; run relevant existing pickup tests.
- Recovery: Recover only the fuse assets, local catalog entry and encounter pickup wiring; retain existing item/catalog GUIDs and scene metadata. The Phase 1 inspection slice remains playable.
- Risk: Medium — catalog identity and scene pickup wiring determine whether collection is available and repeat-safe.

### Step 2: Install atomically and expose repair failures
- [x] Status marker
- Criteria: S2, S4, S5, S7 — installed-fuse state, inventory requirement, refusal and repetition safety.
- Files: `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs`, `UI/ScrubberScreenView.cs`; `Assets/_Project/Core/Composition/SceneBindings.cs`; `Assets/_Project/Tests/Domain/ScrubberEncounterTests.cs`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs`.
- Changes: Bind validated player inventory and existing command service to the encounter endpoint. Require the exact fuse in that inventory and consume one item with the repair commit in the same atomic command, following existing restoration safeguards. Enter Repaired Offline before observers/feedback. Provide missing-item disabled reason; faulted Restart remains an actionable failure with the exact blown-fuse reason. A completed repair disables installation and advances objective to restart, while CO₂ and airflow remain unchanged. Recheck inventory on activation; reject stale/unavailable/busy attempts without partial mutations. Post-repair restart is explicitly unavailable until Phase 3 delivers it; do not claim the final outcome early.
- Tests: S4 — missing/wrong/hotbar-only/other-container items, unavailable inventory and stale removal consume nothing and preserve fault; faulted restart returns the required reason. S5 — one successful repair consumes exactly one fuse, leaves airflow stopped and CO₂ elevated, commits before notification and rejects duplicate/reentrant consumption. S2/S7 — all labels update consistently across close/reopen. Run existing RestorationTests as regression evidence.
- Recovery: Restore this step's action and binding patches while retaining collection/inspection. Do not alter existing restorable-system semantics to approximate the separate repair/restart states.
- Risk: High — incorrect callback ordering or stale inventory access could lose an item without committing repair.
