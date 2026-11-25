## Verdict: FAIL

Critical-source reconciliation can omit saved sources after restart, and delayed door initialization can reinstate a blocker over an already restored open shortcut.

## Scope

Reviewed all three steps of Phase 2: persistent repairs/scrubber state, permanent doors/power, and objective-bound component reconciliation. The phase supplies the reconciliation operation; automatic death integration remains Phase 3.

Exact diff command, executed from the supplied project root:

```sh
git diff 62b17b02ae1a067bea59d8b623b2854cad6d8675 0836bb852d55b9770deda335073ceaea15dc7d75 -- . ':!.qrspi'
```

- Base: `62b17b02ae1a067bea59d8b623b2854cad6d8675`.
- HEAD: `0836bb852d55b9770deda335073ceaea15dc7d75`.
- Product diff SHA-256: `2002f2a337fd38886f3a47536b25a5cb6f0bf86fd6194ca27e3e33e13df7055a`; all `.qrspi/` hunks excluded.
- Spec: `.qrspi/run-lifecycle/spec.md`, SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Overview: `.qrspi/run-lifecycle/plan.md`, normalized SHA-256 `0400704b96d53c17a17b380ae5a3b81b0b089b070bdd20478b0c5488072e5217`.
- Phase: `.qrspi/run-lifecycle/plans/plan-phase-2.md`, normalized SHA-256 `0195a7488b9931af5f0c06d0bbfa3ffe1a05918118ed6883094434792db6ba1f`.
- Normalization changes only status-marker tokens and overview phase-table Status cells to `[ ]`.
- Caller supplied successful digest-only upstream provenance and exact-revision approval; independent allowed-input fingerprints match.
- Working tree and index: clean before inspection and after diff; no untracked product sources. Review artifact is the only intended write.
- Prior review filenames listed: `phase-1.md`; its contents were not read. No supplemental reports.
- Snapshot verified unchanged immediately before verdict publication: HEAD, product diff, spec, normalized plans, all source hashes, and relevant working-tree/index status checked again.
- Verification source snapshot: clean working-tree files equal the recorded HEAD. This is static source inspection; no Unity tests, player builds, manual gameplay, or full verification gate were executed. Existing test sources were inspected without treating their assertions as executed results.
- Plan route differences: none identified.

Source files read in full (working-tree origin; unchanged dependencies additionally compared to HEAD):

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `d28dd8a1eaba5eeee206b03e77ababcf6f1d97b76b127cfaa5fef5417023e02c` |
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `2a8526533bc1654ce7eaacc70ac1506cb51be730f178c2f9aaadbf8f9e449ae2` |
| `Assets/_Project/Domain/PickupCollectionService.cs` | `a87ab61efde2e7908062e971c1a620ceba05ad970c48b40098f6535c28ca2eb6` |
| `Assets/_Project/Domain/Restoration/CriticalComponentSourceState.cs` | `f7727cb36ce27fa3a881f1ea81df9bfd39f05d22ba2c5106dd880c2b1d9b2fca` |
| `Assets/_Project/Domain/Restoration/CriticalComponentSourceState.cs.meta` | `f4bf80b6002c7649fb7a9832320228c78166f7e79edea76cc9fa074955c8a09f` |
| `Assets/_Project/Domain/Restoration/RestorableSystemState.cs` | `73ac2b91948bc7c3d861d596b51a29c2087860a17ac068cfc5a783c81fd66eca` |
| `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs` | `941996d6c7a98c8ca9b203b2649fac5ca0c223b675002c6eb0d1869d4858b3fc` |
| `Assets/_Project/Editor/RunLifecycleValidator.cs` | `8dc5dfeb34271d6b35937a974967fc520b5d1bfd36779b78fc50679840a43c49` |
| `Assets/_Project/Editor/RunLifecycleValidator.cs.meta` | `1e31f0b96549c1e25c3dd3b4f3494ca7dff4ebb54a62cd947ae8a8f080cc4edf` |
| `Assets/_Project/Editor/SaveableIdValidator.cs` | `46bd870a3c854b91fdda05f314c2cc45cb5acfe7dbb46df08db1185dbbe5e1f7` |
| `Assets/_Project/Features/Items/Core/WorldPickup.cs` | `c5b92595309e7a6450e3cb94e068d020f86317aad0f417737996aa93038105e3` |
| `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs` | `14a0c63155f088f0e20302e4f476ab5d7b7984bab91fc39f97a979cbe10c6a57` |
| `Assets/_Project/Features/World/Restoration/CriticalComponentSource.cs` | `c2f4a559f76289c886c86f678116d9b996b1423aa168238883a4e8ecc39ff716` |
| `Assets/_Project/Features/World/Restoration/CriticalComponentSource.cs.meta` | `c4e3833ce890f36716c1fd9c08a14f5b0606f5f2fce0ee0e9a93082bce37f8a6` |
| `Assets/_Project/Features/World/Restoration/RestorableSystem.cs` | `0f713bf2bc1205f2e17117ea268978885c578b3f1949e9bffc2e986003876a7a` |
| `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs` | `083aa388b8f946a8166ecc6200f8116fe073375c105281679917e1ef8c2f87a8` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs` | `f33dd1541d49ae562c76733d43e1803427c9372381ee14f7df0a246f6e5dd8dd` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs` | `922e34d35b88ff279d40b5bda2b4969e544f2e4c8df075f6f2c89ba160f97f1c` |
| `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs` | `f759915a5c545b5030617c4335d48153eaa0eb5e3f93e4d7a004fae5648118e7` |
| `Assets/_Project/Tests/Domain/CriticalComponentSourceTests.cs` | `2a078910de578d25efcdb0259d423b31d49c1f090f39bb261c1f95de39d593b5` |
| `Assets/_Project/Tests/Domain/CriticalComponentSourceTests.cs.meta` | `6e891258fa08d59f3578410823a9cef7b8c9c78d07ca8a262534bf7f62d75766` |
| `Assets/_Project/Tests/Domain/RestorationTests.cs` | `9ae5106ee3f83995ad048c1a07611654fd4fe7646b3b7f798dfa756b8474db47` |
| `Assets/_Project/Tests/Domain/ScrubberEncounterTests.cs` | `7eac7d60f01da0b7ed4f6f0e1a7bbcd8066f6bf93f7ca535dac55aa25620fc5d` |
| `Assets/_Project/Tests/PlayMode/InteractionScopeLifecycleTests.cs` | `ca6aeb4c22ae470ddc074f3971011f2846a8f2b4911f1e621a63aae45121dc54` |
| `Assets/_Project/Tests/PlayMode/RestorationDoorTests.cs` | `589b87337395bec697f86873c04e70fe64d90882c79d3d6a1211081f040806ee` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleShipPersistenceTests.cs` | `b4ce88b1188b1f8009eb542fb698f7efe3ccb27408df8aa7b7485ffd93ae3806` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleShipPersistenceTests.cs.meta` | `a9ead1e9dd04b760aa288b92ed118fac0f23ae6a87eb3c15355de3e50b9f7350` |
| `AGENTS.md` | `071edc83056da4baca88f63b4bd6a30ba9d6750b38f691738eaf9323e86f01b6` |
| `README.md` | `564b3e6fd0eaa012bf1a086bfb9813a8a3d1a2da2d5816373e23cfc5dcf351fc` |
| `Assets/_Project/Domain/SaveGameData.cs` | `f8266cf885cf4d2d7cf0d9875d048a80f32c11dfb892ed2a0a35fdfc9fc4bac3` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs` | `b6d37b430cd962b3130251ab478a5f86e3fd827934dc223df8da90ffe550383d` |
| `Assets/_Project/Domain/ContainerCommandService.cs` | `8cb9957bdcacf3551e5ceb147b9df979548b032841acc585d2a73e641f863511` |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `c03e9edd6067eeff3da708fc6ada96d82d9ce247b5b0a79116c00fd0e4b864ba` |

## Findings

| Severity | Blocking | Category | Location | Description | Suggested Fix |
| --- | --- | --- | --- | --- | --- |
| HIGH | yes | Spec conformance | `Assets/_Project/Domain/ContainerSaveSession.cs:87` | F1 / S8: A newly reopened session reconciles only policies bound during this process. A saved, collected critical source in an unvisited scene remains unavailable after the prepared death boundary, even with empty equipment and no retained component. Later binding does not repair that existing record. | Make authored policies available for every saved critical source at reconciliation, or persist enough policy/boundary information to reconcile safely before its next presentation. Reject incomplete reconciliation rather than silently publishing it. |
| HIGH | yes | Spec conformance | `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:66` | F2 / S10: An initially inactive authored door is restored by SceneBindings before its Awake runs. Activating it later resets its blocker and sprite to closed while `open` remains true, so interaction cannot reopen it. | Make Awake respect already restored state, or establish initialization before applying persistence; cover inactive-at-bind activation. |

## Evidence

### F1 / S8

- Trigger: Collect a critical fuse for an unfinished objective, save while alive in another production scene, reopen there, then supply a valid post-equipment-loss snapshot to the Phase 2 death-reconciliation operation before visiting the source scene. No saved shared owner contains that fuse.
- Expected: The prepared reconciliation snapshot makes the lost component's authored source obtainable for the successor, including content unloaded at the boundary. The spec requires saved progress across restart and reacquisition after death outside the cryobay.
- Actual source trace: `ContainerSaveSession.cs:22` initializes an empty in-memory policy dictionary; the constructor at 30–49 migrates/clones saved records but never populates policy mappings. Only `BindCriticalSource` populates it (75). `ReconcileCriticalSourcesForDeath` loops that dictionary alone (87), so the existing saved source remains `available=false` and its collected ID remains present. Rebinding later sees an existing record and skips the entire reconciliation branch (68–74). `IsCriticalSourceSuppressed` (51–54) and collection (93) continue to reject it.
- Supporting dependencies: `CriticalSourceSaveRecord` in `Domain/Expeditions/ExpeditionState.cs` persists only source ID, objective ID and availability, without item/storage policy. `GameCompositionRoot.CreateSession` creates a fresh session and pickup service; its saved-scene startup path binds only the restored scene. `SceneBindings.Initialize` registers authored pickups in that scene. There is no upstream all-scenes policy registration.
- The new `LostCollectedComponentReturnsOnlyInPreparedDeathSnapshotAndOrdinaryLootIsUntouched` test disposes a lease in the same session, which deliberately retains its dictionary entry; it does not cover reopening before reconciliation with that source unloaded. The unloaded-locker test also binds the source first.
- This finding concerns the candidate-producing operation implemented in this phase, not the absence of automatic death wiring scheduled for Phase 3. No executable reproduction was run; the complete state/data-flow above establishes the omission.

### F2 / S10

- Trigger: An authored door GameObject starts inactive on scene load and its saved door record is unlocked/open. SceneBindings binds it, then ordinary activation makes the door active.
- Expected: Saved shortcut openness, presentation and collision agree immediately when the door becomes available.
- Actual source trace: `SceneBindings.Initialize` enumerates `GetComponentsInChildren<SolitudeSlidingDoor>(true)` and calls `BindPersistence`, including inactive doors. `SolitudeSlidingDoor.cs:58–61` sets `open=true`, renders the open sprite and disables the blocker. Unity defers Awake for an initially inactive GameObject until activation. The subsequent Awake at 66–71 unconditionally renders frame zero and enables the blocker. It does not clear `open`; `CanInteract` rejects open doors, and Update exits because binding set `opening=false` (59, 75). The blocker therefore remains enabled.
- No upstream authoring guard excludes inactive doors; validation also enumerates them with `true`. The production persistence fixture creates active doors, so its restored-collision assertions do not exercise this ordering.
- This is static analysis of initialization order and the explicit writes; no Unity execution is claimed.

## Open Questions

None preventing the findings. Full gate, standalone restart, manual traversal and later death integration remain qualification work; this review makes no runtime qualification claim.

## Supplemental Reviews

None provided.

## Spec Conformance

- NOT IN SCOPE S1 — Fresh colonist initialization belongs to Phase 1.
- NOT IN SCOPE S2 — Production lethal succession and equipment loss belong to Phase 3.
- NOT IN SCOPE S3 — Death retries and gameplay gating belong to Phase 3.
- NOT IN SCOPE S4 — Hostile encounter behavior belongs to Phase 4.
- NOT IN SCOPE S5 — Full colonist/encounter/expedition reload behavior is delivered by other phases; Phase 2 ship facts are assessed under S9/S10.
- NOT IN SCOPE S6 — Expedition supplies belong to Phases 3/4.
- PRESENT S7 — Within Phase 2 preservation scope, critical reconciliation changes only its own source ID; ordinary container records and unrelated collection IDs are cloned and retained. Register/TryGetContainer/Capture preserve unloaded records and overlay live owners.
- DIVERGED S8 — Bound-policy reconciliation checks live captured and unloaded saved storage, suppresses completed objectives and preserves capacity rejection; F1 leaves saved unbound policies unreconciled across process restart. Actual death triggering/commit and production journey are later-phase work.
- PRESENT S9 — Within Phase 2 repair scope, RepairBinding prepares validated ship facts before Remove's trusted world commit; ContainerCommandService commits inventory and world facts before observer publication. Capture refuses busy commands. Scrubber rebind hydrates installation/running/recovery together, caps recovery at five seconds, and advances only loaded active simulation. Restart/rebind does not consume another fuse. Static evidence only; actual death integration and standalone qualification remain later.
- DIVERGED S10 — Saved door records restore permission/open state and blockers for initialized active doors; missing/duplicate IDs fail validation. F2 breaks collision/presentation on delayed Awake of initially inactive authored doors.
- NOT IN SCOPE S11 — Living saved-location resume belongs to Phase 1.
- NOT IN SCOPE S12 — Durable death commit/fault recovery belongs to Phases 3/5.
- PRESENT S13 — Within Phase 2 legacy critical-source scope, missing policy records reconcile on first authoring bind using carried and saved shared contents while retaining unrelated collections. Existing migration and validation keep old container/collection data and reject unsupported lifecycle data. Broader recovery qualification is Phase 5.
- PRESENT S14 — Within Phase 2 compatibility scope, Domain remains Unity-independent, existing command authority owns grants/removals, optional bindings preserve disposable local state, and SceneBindings/ScrubberComposition retain their ownership boundaries. Relevant regression test sources were read; none executed here. F1/F2 are recorded against their specific behavioral criteria.
- NOT IN SCOPE S15 — Production route authoring belongs to Phase 4.
- NOT IN SCOPE S16 — Transition-cause policy belongs to Phases 1/3; Phase 2 does not add a general refresh trigger.
