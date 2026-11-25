## Verdict: PASS

No validated correctness defect or source-level divergence from S1–S16 was found in the stable five-phase feature snapshot; runtime qualification was not rerun in this read-only review.

## Scope

Project root: `/Users/jeremyvalentine/.codex/worktrees/run-lifecycle-ship-persistence/SOLITUDE`.

Reviewed the complete feature, all five phase plans, current dependency context, and the exact product diff:
```sh
git diff bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c -- . ':(exclude).qrspi'
```

The supplied base is the feature starting revision. No merge-base substitution was made. The diff contains 360 changed files: 74 C# files, one Python file, 72 PNG files, 161 metadata files, 51 serialized Unity assets/scenes, and one README. C# changes were inspected with surrounding implementation/test context; serialized assets were inspected through their complete document/field structures, identities, references, and authored endpoint wiring. PNG checks establish file integrity and dimensions, not visual quality.

Plans served only to establish intended coverage. No material alternative implementation route requiring a separate scope note was identified. The SEC-0 dependency additions present in the supplied exact diff were included, not omitted because the plan describes earlier NPC work.

Snapshot:
- Base: `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`.
- HEAD: `9554b07254b7adb78384952a6e2d3f4fcbb924c0`.
- Exact diff SHA-256: `503560d540c747694b03894d589a3ba71d007e792e97c5657030514596b33e40`.
- Product fingerprints exclude `.qrspi/`. Spec and plan fingerprints are recorded separately.
- Spec: `.qrspi/run-lifecycle/spec.md`, SHA-256 `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Instruction packet: `.qrspi/run-lifecycle/review-inputs/final-contract.md`, SHA-256 `374fb758b0ca933e76622f3a9b1e03461adf3cdf1d1b3ed0db83f6526da2d3dd`, independently matched before review.
- Caller-provided digest-only upstream provenance: Spec Inputs and Plan Inputs matched. Upstream narrative was not opened.
- Approved normalized plan fingerprints independently matched:
  - `.qrspi/run-lifecycle/plan.md`: `dc6e3dec5d02179bc7c8fa565ab91e422de6afbaf5d6ad6d8e4c633eff4444cb`.
  - `.qrspi/run-lifecycle/plans/plan-phase-1.md`: `961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089`.
  - `.qrspi/run-lifecycle/plans/plan-phase-2.md`: `bc733c6ee01970b3a5a493d42292baf0f4b886ae3d4f1953f0546fd43b2ef5a5`.
  - `.qrspi/run-lifecycle/plans/plan-phase-3.md`: `5729e01c88632a53a843e24478178c0dda4c7a6b7a533d1ac0e68ed293ac2d29`.
  - `.qrspi/run-lifecycle/plans/plan-phase-4.md`: `6b38838e9a2aaf438e002807ae98b055f7aa269faec39c727e257e73a52e0f26`.
  - `.qrspi/run-lifecycle/plans/plan-phase-5.md`: `4d16e3c19852cade8e15dff7f830414a5dd8f1c540bad49393311a660ff5aeeb`.

Plan normalization changed only the status tokens on `- [ ] Status marker` lines and the overview-table Status cells to `[ ]`; all remaining bytes were significant.

Working tree/staged status: `git status --short --untracked-files=all` was empty before/after scope capture and before publication. No untracked product file escaped review. The working-tree source therefore also matched the recorded HEAD/index snapshot. No product file was edited.

Snapshot verified unchanged before verdict publication: HEAD, exact diff digest, exact spec digest, all six normalized plan digests, and all 421 inspected file digests below matched the captured values. The assigned artifact was absent before creation.

Prior review files were listed: `phase-1.md`, `phase-2.md`, `phase-2-r1.md`, `phase-3.md`, `playable-slice.md`, and `playable-slice-r2.md`. Their verdicts/findings were not used as verification or substituted for independent review. No verification reports, implementation notes, request/query/research/approach contents, or historical work/Reviews reports were read.

## Findings

No validated findings.

| Severity | Blocking | Category | Location | Description | Suggested Fix |
|----------|----------|----------|----------|-------------|---------------|

## Evidence

All paths below beginning with `Domain/`, `Core/`, `Features/`, `Application/`, `Editor/`, `Tests/`, or `Verification/` are relative to `Assets/_Project/`.

Independently performed checks:
- Ran the exact diff and captured its SHA-256; independently matched the supplied base/HEAD/diff and approved spec/plan revisions.
- Inspected command publication ordering, lifecycle initialization/restoration, death preparation/commit/activation/acknowledgment, scene-release ordering, live and unloaded owner capture, and primary/backup save recovery. These are source traces, not claims that their tests passed.
- Read the feature and regression tests, including lethal-vitals, retained-reader, held-input, failed-write/retry, critical-source, supply, encounter, repair/door, migration, and resume cases. Inspected the built-player scenario implementations in `Verification/Player/SolitudeCompositionSmoke.cs` and `Tools/Verification/verify.py`; neither runner was executed.
- A non-mutating Python inspection read all changed serialized files and metadata: 161 changed metadata GUIDs were present and unique within the changed set; no pre-existing changed metadata GUID was replaced. All changed imported files had tracked metadata, and no changed metadata file was orphaned.
- Read all 51 changed serialized Unity files and checked their 820 document identifiers for duplicates within each file: none. Inspected production scope/feature lists, stable repair/door/source/encounter identities, quantity-one supply definitions, patrol prefab/animation wiring, and the explicit production/demo split.
- Compared the four complete Wakeup Tilemap serialized documents against the supplied base: byte-for-byte unchanged. The route adds authored objects without replacing painted Tilemaps.
- Parsed all 72 PNG files: PNG signature, chunk boundaries and CRCs, terminal IEND, and dimensions passed; every image is 32×32. This is an integrity check, not a rendered-art review.
- Parsed `Tools/Verification/verify.py` with Python `ast.parse`: passed without executing the runner or creating bytecode.
- Inspected changed Domain/Application imports: no Unity imports were introduced.
- `git diff --check bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c -- . ':(exclude).qrspi'` returned 2 for 1,219 trailing-whitespace diagnostics, all in Unity-generated serialized/metadata fields. No other diagnostic type was reported; these were not promoted to a product defect.

Runtime limits: no Unity Editor, build, Domain/Application test execution, EditMode/PlayMode execution, standalone restart/interruption run, or manual production journey was performed. The envelope permitted non-mutating source checks and excluded mutable editor/build commands and implementer verification reports. PRESENT below means independently established implementation/source conformance, with the supporting scenarios inspected; it does not claim those runtime scenarios were rerun or that external qualification evidence was accepted. Compilation, physics/input behavior in a running player, rendered presentation, timing under actual execution, and the full macOS ARM64 gate remain outside this review's independently executed evidence.

### Lifecycle, death, and persistence trace

`Features/Player/Succession/SurvivorSuccessionComposition.cs:61` distinguishes disposable awakening, missing lifecycle initialization, saved resume, pending receipt recovery, and scene arrival. `ContainerSaveSession.TryInitializeLifecycle` at `Domain/ContainerSaveSession.cs:188` refuses a second initialization. Production obtains its coordinator from `GameCompositionRoot.BindSuccession` at `Core/Composition/GameCompositionRoot.cs:244`, preserving session authority across scene adapters.

The vital observer queues only the current identity. `QueueLethalDeath` at `Core/Composition/GameCompositionRoot.cs:215` gates input without taking a snapshot during vital publication. `ProcessPendingDeath` at line 222 defers while publishing/busy/travelling and retains pending work across frames; `LateUpdate` retries without another lethal event. The coordinator retains the proposed successor on preparation failure and advances each stage only after its corresponding port succeeds.

`CommitDeath` at `Domain/ContainerSaveSession.cs:208` first validates a detached candidate, clears only the two carried owners, installs one successor/expedition receipt, and reconciles critical sources. `DiscardDepartingEquipment` at `Domain/ContainerCommandService.cs:126` commits loss and candidate facts under the existing command publication boundary. Identical receipt replay does not repeat that command. Shared owners and permanent ship facts are preserved.

`GameCompositionRoot.TryActivate` at line 299 saves the committed loss before relocating or loading cryobay, then saves fresh activation. `TryAcknowledge` at line 326 durably removes the receipt before success/ungating; a failed removal save restores the pending receipt in memory. The coordinator's stage and receipt prevent repeated equipment loss or fresh identity selection. The existing store performs whole-snapshot validation and temporary-file promotion; after successful death acknowledgment, both the primary and the rotated preceding activation snapshot contain the successor rather than predecessor equipment.

### World ownership and restore trace

`ContainerSaveSession.Collect` at `Domain/ContainerSaveSession.cs:124` prepares supply/critical facts, commits them with `Commands.Grant`, and leaves them unused on capacity rejection. A new expedition is created only in death commit; scene binding does not renew allowance. `CriticalComponentSourceState.HasRetainedComponent/Reconcile` at `Domain/Restoration/CriticalComponentSourceState.cs:53` considers saved/live records through the complete captured snapshot and declared shared owners, excludes departing equipment at death, and suppresses completed objectives. Saved source policy is reconstructed before an unloaded-source death boundary; unresolved old metadata rejects rather than silently skipping reconciliation.

Repair consumption's trusted world callback installs the permanent fact before observers: `Domain/Restoration/RestorableSystemState.cs:45` and `ContainerSaveSession.RepairBinding.Prepare`. Scrubber binding restores installation, running state and elapsed active recovery; only supplied active simulation deltas advance its five-second recovery. Door binding applies saved open frame and blocker before gameplay readiness and Awake respects already applied state.

`Sec0ExpeditionEncounter.Interact/Reconcile` stores expedition defeat before presentation, disables the actor's physics/collider/visual activity through the owning controller, and resets authored patrol when the expedition ID changes. A duplicate receipt or same-expedition notification cannot request another patrol refresh.

## Open Questions

None preventing source-conformance determination. The runtime limits above remain explicit; no unavailable runtime check was treated as a product defect or replaced with an implementer test claim.

## Supplemental Reviews

None provided.

## Spec Conformance

- PRESENT S1 — Fresh production binding awakens one active colonist and calls the guarded session initialization with one expedition. Both player owners register without awakening loot. Wakeup authors 100/100 health, oxygen and hunger; `ResetForAwakening(true)` restores authored values and recovery timing. Independently traced `SurvivorSuccessionComposition.Initialize`, `ContainerSaveSession.TryInitializeLifecycle`, and the saved scene's bindings/vital fields; initialization/resume tests were inspected, not run.
- PRESENT S2 — Actual lethal health reaches the production observer and queued death contract; production commits permanent inventory/hotbar loss without creating a recovery owner. Cryobay activation resets vitals, selection, motion and input through the retained avatar, with shared records unchanged. Source: `SurvivorSuccessionComposition.ObserveLethalHealth`, root lines 215–241 and 289–324, session lines 208–236, and `SurvivorAvatarAdapter.Awaken`. The authored hazard calls the normal Damage contract; lethal and input-journey tests cover this entry point in source.
- PRESENT S3 — Current-identity checks, retained failed preparation, stage/receipt reuse, exact duplicate commit matching, command/vital publication deferral, and serialized travel prevent another successor/advance. Policy remains gated until staged success; the root retries retained work every eligible LateUpdate. Source: `SurvivorSuccessionCoordinator.RequestDeath/Run/Fail`, root `TransitionTo/QueueLethalDeath/ProcessPendingDeath`, and `CommitDeath`. Fault/duplicate/stale/held-input cases were independently inspected.
- PRESENT S4 — Production has one stable `maintenance.sec0` interaction adapter in the composed SEC-0 feature. Defeat commits into the current expedition before activity is suspended; rebind reads that fact, and a changed expedition resets authored patrol once. Source: `Sec0ExpeditionEncounter.cs:58` and `:78`, `Sec0Controller.cs:216`, production scene feature wiring, and encounter/input-journey test implementations.
- PRESENT S5 — Session capture retains expedition defeat/collection facts alongside living identity/vitals and carried owners; releasing a scene stores owners instead of refreshing world facts. Resume restores the snapshot, with scene-entry kind separated from awakening. Source: `ContainerSaveSession.Capture/ContainerBinding.Dispose`, `GameCompositionRoot.ReleaseScene`, `SurvivorSuccessionComposition.Initialize`, codec lifecycle fields, and encounter/resume/reopen scenario sources.
- PRESENT S6 — Only two designated source IDs are accepted as supply sources, each quantity one. Production source definitions resolve bandage Medical/25 health and ration Food/25 hunger. Grant failure cannot consume the prepared allowance; collection and item grant share one command commit; death replaces expedition facts, leaving stored supplies intact. Source: `ExpeditionState.cs:12`, `PickupCollectionService.Register`, `ContainerSaveSession.Collect/CommitDeath`, authored item/pickup/prefab/catalog fields, and allowance/consumption test implementations.
- PRESENT S7 — Death clears only player inventory/hotbar and replaces expedition state. Ordinary collected IDs, complete shared owner records, and `ShipStateSnapshot.permanentDiscoveryIds` remain in the candidate and clone/codec paths; ordinary collected pickups stay suppressed on rebind. Source: `CommitDeath`, `Capture/Clone`, `SaveGameData.Migrate`, `WorldPickup.Initialize/RefreshAvailability`, and existing pickup/container persistence regression sources.
- PRESENT S8 — Death candidate loses carried fuse, then reconciles objective policy against complete persistent owner records. Declared saved locker fuse suppresses replacement; installed repair suppresses permanently. The source policy survives unload/reopen and missing old policy blocks unsafe reconciliation. The production source lists the scene locker and no prerequisites. Source: `CriticalComponentSourceState.cs:53`, `ContainerSaveSession.cs:60` and `:100`, `CriticalComponentSource.BuildState`, Wakeup serialized source fields, and loaded/unloaded ownership/death journey test implementations.
- PRESENT S9 — Installation removal and installed fact use one command commit, capture is rejected during publication, and retries observe installed state before consuming again. Scrubber saves running and partial active recovery; binding restores those values without offline time advancement. Source: `RestorableSystemState.TryRestore`, `RepairBinding.Prepare`, `ScrubberEncounterState.BindPersistence/TryRestart/Advance`, and `ScrubberEquipment.Initialize/Update`; coherent-publication, partial-recovery and repair-interruption scenarios were inspected.
- PRESENT S10 — Permanent power/unlock/open facts are session records; generic repair bindings and door hydration occur before feature/input readiness. Door hydration directly reconciles frame/blocker rather than replaying power checks; Awake preserves already restored open state. Source: `SceneBindings.Initialize`, `SolitudeSlidingDoor.cs:54` and `:65`, `RestorationDoorGate.Reconcile`, `OpeningPowerSocket`, and the actual shortcut/socket scene references plus door tests.
- PRESENT S11 — Pause/quit explicitly capture clean living state; startup chooses the saved streamable scene before binding. Restore applies saved precise values/recovery delay and saved coordinates; floor/collision checks select a validated same-scene fallback without changing identities. Source: `SaveGameService.cs:52` and `:66`, root startup/capture methods, `SurvivorAvatarAdapter.cs:96`, and `PlayerVitalsController.RestoreVitals`. No wall-clock/offline advancement exists in this path; process/resume/fallback scenario sources were inspected.
- PRESENT S12 — Validated complete candidates, command publication exclusion, whole-file save promotion, committed/activated receipts, durable acknowledgment and gated retry preserve coherent ownership. On restart a committed receipt only recovers forward; lethal saved health queues death before readiness. The preceding activated backup after acknowledgment is also successor-owned. Source: root `BindSuccession/TryActivate/TryAcknowledge/FinishStartup`, `SaveGameService.TrySaveTransition`, `SaveRecoveryStore.Write/Load`, and the application/PlayMode/built-player fault scenario implementations. Interruptions were not executed here.
- PRESENT S13 — Versions 1/2 migrate known containers/seed/collections while leaving missing lifecycle for one authored initialization. Critical-source first binding reconciles missing components against preserved equipment/shared owners. Typed v3 validation rejects malformed/contradictory state, unsupported versions remain incompatible, and the existing recovery store does not silently replace corrupt data with a new world. Source: `SaveGameData.Migrate/ValidateLifecycle`, `SaveJsonCodec.Decode/Lifecycle`, `ContainerSaveSession.BindCriticalSource`, `SaveRecoveryStore.Load`, and migration/recovery fixture implementations.
- PRESENT S14 — Existing command/readers remain the mutation/read boundary; the production root owns one coordinator while demo composition retains local recovery-cache ports and explicit disposable resets. Repair/door/scrubber adapters use optional production persistence. No new Unity dependency was introduced into changed Domain/Application source, and verification player code remains define-constrained. Source: command/session implementations, root `BindScene/BindSuccession`, composition lifecycle, `SOLITUDE.Verification.Player.asmdef`, and preserved compatibility/input/persistence tests. Existing GUIDs and painted Tilemaps were independently checked.
- PRESENT S15 — Wakeup contains the connected existing painted floor and authored cryobay, supplies, critical fuse source, reachable shared locker, maintenance SEC-0, scrubber, powered shortcut and live-conduit hazard. Scope references include the required pickups/features; the manifest and build settings retain Wakeup as production entry. The hazard uses the existing Damage contract and lethal observer, so debug death is unnecessary. Source: complete Wakeup serialized wiring, `RunLifecycleAuthoring/RunLifecycleValidator`, `ExpeditionHazard`, and the real-input production journey implementation. Actual traversal/rendering/manual gameplay was not repeated.
- PRESENT S16 — Travel and living capture/rebind retain the expedition; only accepted death creates its replacement. No completion/extraction state or player refresh command was added. Explicit New Save clears lifecycle, ship, collection and owner records and reloads cryobay. Source: root `TransitionTo/StartNewSave`, `ContainerSaveSession.Reset/CommitDeath`, `SurvivorSuccessionComposition.Initialize`, and transition-cause/new-save scenario sources.

## Inspected source fingerprints

Origin: working tree, clean and matching the recorded HEAD/index. These include complete changed-file context, serialized/binary source inputs, metadata used to resolve scripts, unchanged dependencies, and project conventions read. Every digest below was rechecked immediately before verdict publication. Paths are relative to the pinned project root.

| Path | SHA-256 |
|------|---------|
| `AGENTS.md` | `071edc83056da4baca88f63b4bd6a30ba9d6750b38f691738eaf9323e86f01b6` |
| `Assets/_Project/Application/Persistence/SaveRecovery.cs` | `c16677a896bd7e724a5ed2ea72e2a36a136771421a205b7a474ca83b5d829e06` |
| `Assets/_Project/Application/RuntimePolicy.cs` | `3e1ac34b6c013fad11b50cb5b759584845c25cc18e1378c73981cd65329916a6` |
| `Assets/_Project/Application/Survivors/ISurvivorSuccessionPorts.cs` | `9491eeb4ccf53cd8f507489959d183142ecdb9ba2f5af7ade049ff1722fe147d` |
| `Assets/_Project/Application/Survivors/ISurvivorSuccessionPorts.cs.meta` | `1d4aa13875f1a7d47566d6208f19d12f2a17792f39d7f03e54fc7d2b6951f484` |
| `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs` | `8833ac2d49d6cd19b41bef6b5a7045c19322f41f504b2cb6aa54fd8be921c046` |
| `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs.meta` | `4aed5276d56be45c7fadbb4e43e3eb7e7c1b1837d0f65c086327c8327af4fef6` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `0aaea25ee22b96f49cc2613427a61c0ee40d9b9111674f1f6850a3380c0bc37c` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs.meta` | `86aecc60a704cd1b37614fe5801e4e1221a6d3310eb1be3e96eff62e679d3a99` |
| `Assets/_Project/Core/Composition/README.md` | `0c748deea4aa6bc0bd88b3c6b59a31da038849fc241a171508ec5a968f79d311` |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `d28dd8a1eaba5eeee206b03e77ababcf6f1d97b76b127cfaa5fef5417023e02c` |
| `Assets/_Project/Core/Composition/SceneBindings.cs.meta` | `5e8f2bec4f50020a664e38beee3ba3a82ea9c5b5f2641cae2d14ce21dd219ce5` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `c03e9edd6067eeff3da708fc6ada96d82d9ce247b5b0a79116c00fd0e4b864ba` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs.meta` | `bef95b995b83c739652a843a398b05fc90b7a7cf2a4168c95b9c2c3230e6828e` |
| `Assets/_Project/Core/Input/UnityInputHost.cs` | `93bfe194075b3295ca6f4aeba4b38bfce78749f7df39e0d66692f800b7927330` |
| `Assets/_Project/Core/SaveLoad/SaveFileStore.cs` | `99fc2675b5bcaa23e8425905957fcc10ec92231b08fd4d66add1c991fe0f00d5` |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `2b9de884c4861b1ae90c2cfc0368c82fd932b8d43f71541a8ceb6af22b8fcc5b` |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs.meta` | `4409a7612e7e41476e23f63dd742251bfc4ad64e46d61ee2b4850dbe7698118d` |
| `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs` | `8cafa578a06dab3eb654af8b36c44aecb315e99896f8e6091a4b4a1b8171cb54` |
| `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs.meta` | `7d4448f74fa15e7895bd5bfdfa1f14d6b87436a8c9dc78edc1cf5d4292db2da8` |
| `Assets/_Project/Core/SaveLoad/VerificationFaults.cs` | `1f2ea4e66ea59e77cfa264f9ede35f54081ed396c2b1c9f94cc3a7d7d75c2a3c` |
| `Assets/_Project/Core/SaveLoad/VerificationFaults.cs.meta` | `2869082787127b0d43c040b45b2c88a1d9f8923457f2047df1c2802816ea2bfb` |
| `Assets/_Project/Domain/ContainerCommandService.cs` | `e68c15c96fb79d54e6669d6875abda3f73d650b07de81009edb36e9a4d72cc6d` |
| `Assets/_Project/Domain/ContainerCommandService.cs.meta` | `8e2e3852d605db7a80409bbd662a6e3773c492c642f741fd6afba0af0f58b880` |
| `Assets/_Project/Domain/ContainerSaveData.cs` | `d76b381a69061abf010198f44be71ebcfe6b6a8b30ee446b47bb77764db50140` |
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `688d7315a24849b019e10d7ace7f78eabedca65e01f365b903f75b436af5e588` |
| `Assets/_Project/Domain/ContainerSaveSession.cs.meta` | `aed0ab6bbf0e17acd323c4bb4e1c45fab34d1144a39c1fa402e774e4a8e5820e` |
| `Assets/_Project/Domain/Expeditions.meta` | `5bb8f688445883c97edb44ed9d83b6c68c09ee8bb9d1bb4f57f105f1713c156b` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs` | `1e6418e988b976c8f642c92ea81343b7540cb18e1a84bef0bdbe073f5bde6aa9` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs.meta` | `af5288266a648807708eff8b93bf9524e69a65007b9a68310bdaee94fa16e2b5` |
| `Assets/_Project/Domain/Npcs.meta` | `63296eeb442baef8c1f1b6cff913604a13660f0713fafd332caba3fed79e7bf6` |
| `Assets/_Project/Domain/Npcs/SecurityBotBrain.cs` | `49cc433c3e230d40eee99ffbfdeebad659be549c92271c11ad9afbdc872693c4` |
| `Assets/_Project/Domain/Npcs/SecurityBotBrain.cs.meta` | `3a2cf30246383a574da8a1cc0a27457b44617d4706a3af555e0236f29c913f56` |
| `Assets/_Project/Domain/PickupCollectionService.cs` | `c53cfadd10a00a6705d89badc842d23a9d51486b2cbdd1503fda7450a8d249a0` |
| `Assets/_Project/Domain/PickupCollectionService.cs.meta` | `e097f85bd2f03902175e6fa2902aae7ef9338d72eb56045173081a44170d40f0` |
| `Assets/_Project/Domain/README.md` | `d3fbdce7507f3be23eb68873742aa8dc57c5fba0f6c61da0c2075f28858ebe50` |
| `Assets/_Project/Domain/Restoration/CriticalComponentSourceState.cs` | `b33b193b365ffaa49d859ef38601ce97ad8fd08798915d10cee3b8421475188f` |
| `Assets/_Project/Domain/Restoration/CriticalComponentSourceState.cs.meta` | `f4bf80b6002c7649fb7a9832320228c78166f7e79edea76cc9fa074955c8a09f` |
| `Assets/_Project/Domain/Restoration/RestorableSystemState.cs` | `73ac2b91948bc7c3d861d596b51a29c2087860a17ac068cfc5a783c81fd66eca` |
| `Assets/_Project/Domain/Restoration/RestorableSystemState.cs.meta` | `06e2792d245151c0f6877beba36659613400a7ebd0041623e7853bcc6e7170b6` |
| `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs` | `941996d6c7a98c8ca9b203b2649fac5ca0c223b675002c6eb0d1869d4858b3fc` |
| `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs.meta` | `dfefdfd313a4cffeec21db603552166a5361b748edbaef1afdcf9ee4e136df29` |
| `Assets/_Project/Domain/SaveGameData.cs` | `f8266cf885cf4d2d7cf0d9875d048a80f32c11dfb892ed2a0a35fdfc9fc4bac3` |
| `Assets/_Project/Domain/SaveGameData.cs.meta` | `0a224c174a2fd3b12e906d54f75655de093219a976b605572439c437d7d62972` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs` | `25777c7c148840eabd4108a8e3e5629550da3d949a0e0b60fe18c28fea94af85` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs.meta` | `863b0010f908fd9d712ad86c2f6b309751707b715b5a7cb94f0f7c21c58606a0` |
| `Assets/_Project/Editor/CompositionValidator.cs` | `4a84c314ee438e30744697e5a2a293d1424a8516c0273a6784f71cc63dbafc97` |
| `Assets/_Project/Editor/CompositionValidator.cs.meta` | `46162e3d541a49cd1ff24d3ec1013a7d519ac5b4cbfa52407f6d3d3106ba61a3` |
| `Assets/_Project/Editor/ItemCatalogValidator.cs` | `3e63e6ecd2d61c4691d91264b2f718f3a0c6945a2dc4ac0feb7d44f168d777ce` |
| `Assets/_Project/Editor/ItemCatalogValidator.cs.meta` | `a498b9be79b7d96c9f657a74e1294593a29be6bf0bd6c48059bd7b67011e54bf` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `37397f161e0a605828c0cffaa0f9693df7dd4846dc194b77e63a83b8f59627e0` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs.meta` | `5ab4c030ae85c2eeadf95581384a8b18543fa7632147b14145ea5a736759afbf` |
| `Assets/_Project/Editor/RunLifecycleAuthoring.cs` | `6a78994ff5fb62ac1b6b5123c3c124a9f44057686d98ec41fc9b78f9bd8d4faa` |
| `Assets/_Project/Editor/RunLifecycleAuthoring.cs.meta` | `400ee824e95ea9553de192bdc4056695d433d966df69450eb1018e8e02b3caf5` |
| `Assets/_Project/Editor/RunLifecycleValidator.cs` | `44135512ceffde62b420e711e71a1bd95b2b05c5608134b037c6864317fe0635` |
| `Assets/_Project/Editor/RunLifecycleValidator.cs.meta` | `1e31f0b96549c1e25c3dd3b4f3494ca7dff4ebb54a62cd947ae8a8f080cc4edf` |
| `Assets/_Project/Editor/SaveableIdValidator.cs` | `46bd870a3c854b91fdda05f314c2cc45cb5acfe7dbb46df08db1185dbbe5e1f7` |
| `Assets/_Project/Editor/SaveableIdValidator.cs.meta` | `b3e0e12feed65fd648eb19e188b65ccbd3372ecc9a3e0aa08fc53bbe0f114a39` |
| `Assets/_Project/Editor/Sec0NpcBuilder.cs` | `53a15186a4266f8639d3758366c3c946af84352a6d5fa4cecc0724dba8792ad0` |
| `Assets/_Project/Editor/Sec0NpcBuilder.cs.meta` | `db21562191c6ffee4a57796b2b3c02a9be9d1630f1f3b0e63a83dcdd18d4bfa2` |
| `Assets/_Project/Editor/WakeupBuildManifest.asset` | `f7968d034a52ec727c86550ea112c906bf3f50ac70d5eef6b244ac45835c8e06` |
| `Assets/_Project/Editor/WakeupValidation.cs` | `f17d00e4e39257f1312abe517165723954d224f3130be8dc4cfcff4a5288ff74` |
| `Assets/_Project/Editor/WakeupValidation.cs.meta` | `1fa8e7d5eac9977c6deca06094ec3ee0740853af5e9d7ba5dfefbc55389ff5f4` |
| `Assets/_Project/Features/Creatures/Movement.cs` | `646956edb8b0df5f101467d7fcda976c1be752ded7ded0efce4f5ef7456ea070` |
| `Assets/_Project/Features/Creatures/Movement.cs.meta` | `7e50806e30ff8a7c08b33eb9407dbe3f7f8b0b14aad98cf23a5541c0340a41fc` |
| `Assets/_Project/Features/Items/Core/WorldPickup.cs` | `190fb740efffdbf78ddeb847a643d9127b58b82108d80d190ab117fc11b0b6de` |
| `Assets/_Project/Features/Items/Core/WorldPickup.cs.meta` | `bc1fd9700778ca5d2fc6209a1fc79da25926244aa06e2c61e11c0dbbb6ca7ebb` |
| `Assets/_Project/Features/Items/Definitions/Bandage.meta` | `b84646fc354b37fc0a7714720b628f77b96a5a395d2c8a0363e4a7d8ba4d9f4f` |
| `Assets/_Project/Features/Items/Definitions/Bandage/Bandage.asset` | `2936c9c91a9f08f02048f4f41c850b46b1dee2973741bad0f7ac83ae06b2a8e1` |
| `Assets/_Project/Features/Items/Definitions/Bandage/Bandage.asset.meta` | `a1895df66fcb761b4b6fca1829e848e1ed8a43a6e6022b5ce99bed91fb0b0ca7` |
| `Assets/_Project/Features/Items/Definitions/Bandage/BandagePickup.asset` | `d4f934df58d365ca464b880ddd2aeebb62a5ceece959ae0df629077f42d69bb2` |
| `Assets/_Project/Features/Items/Definitions/Bandage/BandagePickup.asset.meta` | `04cc66c46da67d6438811faacd9b56555b108ecadfd2f4da36aab2bc1ba6682d` |
| `Assets/_Project/Features/Items/Definitions/Bandage/PF_Bandage.prefab` | `15b00f998e6ecfc108e3216ff436c274a8d4459c62f1bf239b7862f88751f323` |
| `Assets/_Project/Features/Items/Definitions/Bandage/PF_Bandage.prefab.meta` | `7f2e4ff3762a3518d8bfaf14059f67bf6d19e7830cfa49196255cb40f70387c5` |
| `Assets/_Project/Features/Items/Definitions/Ration.meta` | `9a7c26e1afaeaf9a49ad83106eb0c0c5c7b91cc684d451ac956165e535232ef2` |
| `Assets/_Project/Features/Items/Definitions/Ration/PF_Ration.prefab` | `25e603208c9ed3836e9206046344e67d94bc1e1bead74079d2dd3a211e10f2eb` |
| `Assets/_Project/Features/Items/Definitions/Ration/PF_Ration.prefab.meta` | `f82563460d94d268bc601e35639a561bffcb40b4ea30e14f3dbc2ab16b72e3c7` |
| `Assets/_Project/Features/Items/Definitions/Ration/Ration.asset` | `8cdaf3e68edc63024cdd0222b5ee77bb430335c2ac218ee544ea6ecbbf8257dc` |
| `Assets/_Project/Features/Items/Definitions/Ration/Ration.asset.meta` | `719572b8904c4da0fd6fd7115eb09d7cb0c729ad9f51536ccb6f0758c1f50f96` |
| `Assets/_Project/Features/Items/Definitions/Ration/RationPickup.asset` | `2a630b71b8d9b0e13bae555366acd4516d46a370f219945e53adae13e29103ae` |
| `Assets/_Project/Features/Items/Definitions/Ration/RationPickup.asset.meta` | `d0f51dd730ff501c28de41f8e731ae23fd73613afb582dcb5b1272e7d17d4bde` |
| `Assets/_Project/Features/Items/Resources/Database.asset` | `b56fe776010bca42f67f889d7244ccb5f81a8a5da5c79f487e4affc9d8c4c05d` |
| `Assets/_Project/Features/Npcs.meta` | `001fa499e4a46d21cf9efef72a72a3ef19c7475c18f159de8ed93d01cbfa086d` |
| `Assets/_Project/Features/Npcs/Sec0.meta` | `721f40e4dc9fd3bbabd63a94dced2825686fc58363b3a493511e4f7595fe4132` |
| `Assets/_Project/Features/Npcs/Sec0/Animations.meta` | `07c7887eb3536b1118b39cb6e88a0a996d8aabc39b90def1e4736ae6df4132f4` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_east.anim` | `1bc624c4f94107b7e33016ac5ed15c72f9abebd3c7d7583c871cb66844759a1e` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_east.anim.meta` | `c992005274316d2b0df64de2bcad7cbeae49036883c846959dcc9d840df021da` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_north.anim` | `9842912752ce3e5cd63f11fc7351021baa61532758d723e3ad08feaf38e113cd` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_north.anim.meta` | `28cca4205611bc4e45e8065767404add0b4bc9ad267005caeeb3f172a7ef1840` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_south.anim` | `395e84f4baebeddeab4f1d3021a7681c3e5af7e60a4812ad5dd788a55cf0a331` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_south.anim.meta` | `d40277ec2e23168576ab3a59321f4bb67c4a40deb6011fe485704ccb5a366561` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_west.anim` | `f17e8d3e5e269b3e4d78e9cc00f296a200661f8151050ed54013b358f0768e39` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/AlertBlink_west.anim.meta` | `5f78dab69fcc17126d5a8f225288ae4b6de907619ce62a17bbde83345d88d461` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_east.anim` | `6462be3ac4603808a9e188c2605e25fa678bfce097997eacd6c2cd8e51564611` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_east.anim.meta` | `3b5f5c85b97725ccb8a7cc42472d61cf7a6f263e09a211158ad7ac7da3ec4e36` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_north.anim` | `18c3ce62c85e6227043f8af327bd5c1d321e50cce1b7b1622a36ebe6d303014e` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_north.anim.meta` | `501dc0833ecb52346078cfd1fa41eb3ab8c52ce7fa5f95b14540d5991ec9688b` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_south.anim` | `25fc3ae3d6da308448a18e3ebf5ccf35309e79d9b3a7005b31966b8361438aa3` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_south.anim.meta` | `22f730bfcbe81c1a2366e0ea089917955240850f6010487fa99874cadd6b96eb` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_west.anim` | `8a4b26f0ecd1097ad7ff37d8f01f95ee4797b12be9fb97a505499d797202f917` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Charge_west.anim.meta` | `f52a06b24ecc1e85e988ca3b37ee3a1dfafde7fb06ce6115dc639b05778608a3` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Fx_backup.anim` | `c3b998df3e3e94c51e413e36b61d0dfd47b5c7843ee6a5d7a48b71568bf0acd8` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Fx_backup.anim.meta` | `65e017da27bf2d46c0646236ed8c14eae1f8bc7b070b00e7661ab6e5fd0b32f4` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Fx_stun.anim` | `6d3a98188b549b5eeef6608a101ca924dde03eaa345ff2bad5490f586c4830f0` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Fx_stun.anim.meta` | `b82cd93de28937d5851dde8329fdd19a07585d4096f13ceda3a5fbafe94018c5` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_east.anim` | `e31e5a928facae6a22dc6fc67318d9bdf2b951a88e136b10c785460ced372645` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_east.anim.meta` | `d6def2f702fd0a183ce5a97668b2f1bf123593e3bb49450fd1ab09184180617d` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_north.anim` | `c806f54b4ab973193bbb86938aed6184fcf54df6ed1133069a5eaf71079cdf75` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_north.anim.meta` | `4d5249c0d41d9d3a35c8ab9488a8cd9c9862e983b1f04fee84741d041795a67e` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_south.anim` | `94abcb51add22ea9b5df51715d1bba18b96da46474759572243a7c8d013e5b9a` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_south.anim.meta` | `27124b42d4ab19d01b8b3cb32a66dfc2acad56038fdff7550333da0c96710f87` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_west.anim` | `13ec8fe86707b8f3a6ef4e0305ce90cf73ad18c4826ca31fa4d24e203b1ccdf0` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_alert_west.anim.meta` | `39547f233ed165e65c307784a70f622a51c101be25ca48c22922e2d47f86990a` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_east.anim` | `ff5ed2be01da88fc2ed79d017d2f864b1d6b8234ff1c3d28440f6e133df250f5` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_east.anim.meta` | `381771478276878021d68ec42d1e6dbdfc64fa7dfe1064418c8d5bb41603d4e6` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_north.anim` | `559ca16702de54ee44c96ba9e6353a1256e47f1fb4acc0575f7c7ac25ba3cb3f` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_north.anim.meta` | `b3097c4ef001659ecf4ef23a772f7b90ff8373ed47d3c217d03e626ae6d95273` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_south.anim` | `e7d97eb778e0415998f9223d658b47dc91e48f746adde6feb13b2ae4904deb46` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_south.anim.meta` | `e7d3849c769a36a62b195a1d657652aaf4299128d6db081ea0d297522d79ca53` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_west.anim` | `d813150169898811c76d27c9a747f48d0c97a3c4ea5a87e677929bfca03ab64f` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_hostile_west.anim.meta` | `2dcb6477ed9e62214acfe98e5124f5e17f1fdcf267c0b9acf00547b5429bd373` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_east.anim` | `eff318ece330e6cb0155b1f08983a8d568c0bbab08d4d4328ea16284cfa07ad7` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_east.anim.meta` | `d50acd5201c82c44dc0667fc63b3778bf1782414e0d4ab7691101b2cac692dc4` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_north.anim` | `17997d658aeb5a9edd5f339f4516a3dd84c126cf75558c7a92f1b43be6469999` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_north.anim.meta` | `3e73a20ec97a4e6f110363d0ebdde5d42a5b5130c71fe98c5cf3cd07a00bbe4d` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_south.anim` | `ac58350d31562f0b4b07a9a47e31e9aaf272f03cbac41854bfbfc7ecb37c43b0` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_south.anim.meta` | `dbe64961f89f0c5b980d9cf05c2031d5845465db3f1865c19994228b67ee7149` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_west.anim` | `6688745848df838af8d3339d2e5c12646791a47c4abfe1b6fa08d33efc80ce0e` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Idle_routine_west.anim.meta` | `77e04cd8af54a6a2bada8230817c8c02022f453b03a6fc6c40a2f5e71157286b` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_east.anim` | `db42d7640ffd4c7cc76c49fb6487ad6b2a65b89b9f6bd696b248f9bd13caefe7` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_east.anim.meta` | `c765c816878db41fc35e3dd105b0e056564c21bed4463437292cadb0f17d8561` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_north.anim` | `5b465ec0f8a5cf0bf6edef4bd5ae421571a17a11576b260eb40e888a445d7426` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_north.anim.meta` | `2e789c5034e1a8f25dcae5b0ef02b39099683a4acbde6f2f3a8a5d72b37eaf0c` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_south.anim` | `0fb5c5ff3456e95e5aad3885c23511eda524fb73600ced4539f644f486241080` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_south.anim.meta` | `f59353975de88ddf8ec4e73433bcf41c97c9461de8d5ad335b80221d9ab01dee` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_west.anim` | `9f5a772dcb8d68f101565302ff91df15693711fb14cb3b023e6437aa7775d4e1` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_alert_west.anim.meta` | `dff423462841095f31aa5199d37b835c648c89d2c28472b241a2a568c405817a` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_east.anim` | `ae8bf445ccb80fd4223e21f6def15cf2c7552f61af353dba872a13f05bb93831` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_east.anim.meta` | `44eed2936945c75ffaad965398ef62eec1ca783db603a76c1bb059055374e88b` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_north.anim` | `395db2aea44966e108bd4947e16f414448f6900bd2cec4ceefc58452a3efa629` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_north.anim.meta` | `a2639f88bfe1b082a9f062eea10a6974d161f02edb4008cec3b68993e191f4a3` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_south.anim` | `061ff2dde2ec6d28b01efe4cce0c2f3e249dfae9e89b19627cdf8d758ffadfc6` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_south.anim.meta` | `01c46c7538ec4919673b243a5096c3a926a8e5c334389691a71575b44c377a70` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_west.anim` | `4f16d8a8f5090d72bac6bdf70ee7ce69dbe8d5b65929b0e37202c393bf57d6e8` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_hostile_west.anim.meta` | `f106c708c53011893c0a216de6cdda52766314f91be6c35a236b4c45563604d2` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_east.anim` | `a0db90d4773a930742607bb9edc2b266bccc6d1d30d5d2ec1fad0bb21daa2560` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_east.anim.meta` | `a0e23766f418fd2fca03172aaaeea69c2a7779442a1262a77422f432cd93dc6a` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_north.anim` | `832ca0be9349a8c7d779d08880105ece818e6613a14102134cdd3723e3d77e0f` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_north.anim.meta` | `5f32e00a1b57254e038b5d3f3aa593281b4996e50014941b09a025f973ca6ee7` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_south.anim` | `f0885571d3823797bc6f8159fa87749f64eb0ab06be0aa8959185b4c32ea627e` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_south.anim.meta` | `744b1149c1a433c98d149f14af8dd8ba761fd2bf9029288e4deb2add6faf41bc` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_west.anim` | `d1c3037c6fa8742adef472b0d2967c09abeda9c9990dcedf068d2cde2e8cb0d9` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/Move_routine_west.anim.meta` | `72297d6cffda3b5ad58885d420927a38f52f9da1052206ee752905352f4928c8` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0BackupFx.controller` | `0fc53b4d6e83d060161ca397688d83b46dc0a8fbb95384db3430b17b30fbb098` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0BackupFx.controller.meta` | `f08cbe5e5dbb157c2115096a616df4b0a78a9139e45d9506c3fee500f0583fed` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0StunFx.controller` | `57518fbef89395434f4b9447df3321291b34d5c8125dc9b6018d2f1125935db1` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0StunFx.controller.meta` | `8f8203372d2c43bce55c2ac87df37b849ee606d2ce70cf09d585b38f9c0dca23` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0Visual.controller` | `5199ace283ffbbe7ca21b7fdc1b422259c41b2d7a881ee514005f6fd7a33c4d8` |
| `Assets/_Project/Features/Npcs/Sec0/Animations/PF_Sec0Visual.controller.meta` | `9c7e93523a5c2e94a622d3f7e2bf116837abdce0a9e4f779d269ee8b09010a6b` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab` | `8616a6f12a8dd66125a7f58c689163f128bf829bc48e03febe917559c7209b7e` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab.meta` | `1ac94499b3e82b5d4f01280c389151631def74d2d39b545c7ae20fe39ccdad96` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0BackupFx.prefab` | `eff2d0b0bdcdb5f03b97c82013e141895d9165c9c2828759fc6ba3845d9c071a` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0BackupFx.prefab.meta` | `d9893ed3317471b1d5e989ec336e4b515e511d3342f2b93f870d822d752fa394` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0StunFx.prefab` | `8db41cb910cf5e317740cdadc46400a9f157f819600c22c5533cdaa7e88b535d` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0StunFx.prefab.meta` | `131944bcd9fb21ca88172f7f83d06f8e7aea57569db71158f94228a4fa12dc29` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0Visual.prefab` | `9c824fe416c899a10ff9b29b6a1c10ca4784432d0bce8976fb97231c0c2b6256` |
| `Assets/_Project/Features/Npcs/Sec0/PF_Sec0Visual.prefab.meta` | `d5090179824fd8b7e731d9f83516d488be925b91bec9229b98e9553c87970320` |
| `Assets/_Project/Features/Npcs/Sec0/README.md` | `8b84ae5fe8f33f77c1f2972f3f9e4889be7317e96c78efe49d06fbd006ec3a13` |
| `Assets/_Project/Features/Npcs/Sec0/README.md.meta` | `cf5bd745ad95680a240d17dab0c3fe3fd6b2c5306b0a0528d8089704899cb512` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime.meta` | `0c47b071c5846d74bd3c3e240b2bec64c26b4a3ee36498a1d1a13d7b99655219` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs` | `912ef96ca69da46849500715d1d71c4996471ffbd26f2a63a63ba63651a74d31` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs.meta` | `aba02587566925789cb27f227d045674633317e8a34f6786c6d88c226825e5c2` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs` | `df6359f34b142e9fa944b322fd77da9fe889b97db60cddb540951ce79bd9a434` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs.meta` | `86a1635ab467cd8adcc87a7c1dbdab4c53af90a79e979e1d92385c83fdf9b6f1` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs` | `0e9d8c868a58205b163a3de26f009446ab47814647dc2e080bafd4151439ce0f` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs.meta` | `eeb157ef1c16158e829c8b74fb68049d67d0bb68bf84cdd3db76dbd507c1d041` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0ExpeditionEncounter.cs` | `b9085f53449ba6c6cbd4370c9c3071ca1df98fb8cf2e7cfa9293cbb2182fe6e8` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0ExpeditionEncounter.cs.meta` | `c8e168e4080945e29c1d8cd9013aaf4cc008272bee64597e9116b6e1fb829962` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Navigator.cs` | `c017e3759dac8d8bdfa4ae0e2a10e555c7b50b31d26708a2033e49b67acb0606` |
| `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Navigator.cs.meta` | `986f531ea67465d151ab3b14e830c6bf08524a2bfac7d9ec989154b4deef8106` |
| `Assets/_Project/Features/Npcs/Sec0/Sec0Signal.mat` | `59f833341cb3bf3fbf0f18c50128e99dedb0a02d3c6613737721495770fe6c0c` |
| `Assets/_Project/Features/Npcs/Sec0/Sec0Signal.mat.meta` | `e903f3d37e81e85df7905979d3c135061e2ab4963269e9cc88cb0be54b61a5f0` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites.meta` | `038b55cf2cb080dbdd7e32a25889dbad325e99dd7a71788c3e67e48bbe74231f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_0.png` | `62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_0.png.meta` | `67384f9d231e941fd20eb6d58a73882c10a6bde638464515fb86eb1a7312bfd4` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_1.png` | `240805e0987bb3a9c3fb15eef22dbf1bc8adc4cb1bdec2d6f1732072ffd5bb95` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_1.png.meta` | `271042f23e665ec23865eb96a9e690d2b051350574baabe8644a7e30a47a73db` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_2.png` | `1d58cb7564a1bf9a1febfbf4c0a61345b01e731ce4a8438d6897b6d039547320` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_2.png.meta` | `f6e5509f496f00cf15041ab3112ffefe65eca83b3ca344cd7c15ea93d3aef9e8` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_3.png` | `dcb328836f0c029a983fab5102ebf8b3a9eb6457a8c182392ba39cceac64a743` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_east_3.png.meta` | `420d553bf1f2120f6930f5c920b9e75a810fa8a8546a22d7dcefc49a78c722b3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_0.png` | `d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_0.png.meta` | `706213f7afab071d806f7587cbdd860dc8dac13fef1109c9c0320effbf6519f8` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_1.png` | `4dc15ab4b258821475425ed85fa3be61bac6101aa1669fcdc93a44ab9a12a172` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_1.png.meta` | `3b90db9ab75b35196718f8b9d7293c05261b46fb6c91e85b6af8c9b00e712459` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_2.png` | `3507ec5570f42e54264333a32809f86b4e7b3507381f52979d275456d02f261b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_2.png.meta` | `b403b5c5f1c4117e0c8ce7d542d28b7f2d4164fe4c4015e11b3e4f7c9e3d3356` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_3.png` | `d5d1e09445ce1871ea41c68039cc1fd5d9546122c24c68a144fe324e42cbaf84` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_north_3.png.meta` | `dfc5b1d279a690fbad041162e7ba5db8d189ce89b35ed5b0c9224adffee5e28b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_0.png` | `1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_0.png.meta` | `1e76990f6ffe6375220cfd1bfd825860ece7cf0d166079ac02431d8b6c4538e4` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_1.png` | `c42ba421111ece7e67c07c786e2de25fce05b27ce5354d2e7dd66a6872bef8cf` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_1.png.meta` | `a0cf63ba5b0835acc78a561088d1a5f915ce50daa90f485ecec2948d751117de` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_2.png` | `89dfe8bc0d29fb14bc8d5b4ee5febbbeacfde64e39a015918cbeceb2d8cd219e` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_2.png.meta` | `aad47ce3dbe999343590afadd26003b77b17728990da683b2654a594139fb0e3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_3.png` | `106af2ee5bd3b2af53eb705b6916393d319b94c4b2b9c9ca243b02aa5b10cf7e` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_south_3.png.meta` | `b60975d622d72738a64c8aa6784115fb5be0048e1e393d6b88a29869f56e459a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_0.png` | `20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_0.png.meta` | `02677f54f3346541523c371a1ddc9e95ce5bb01e27589ee98153a4ea7ea19998` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_1.png` | `57c164cf50ea4514aceda339939f26fbed47446b36e231c7156db19d7f993094` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_1.png.meta` | `f89f7cbaf70c457badd3945d49fc77f2939d27ba85b37e2123d55b01cf41c6f7` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_2.png` | `355d0309c5449c00b002eae64489bc42a2dd99a73557ada8f550759e93f6a445` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_2.png.meta` | `7b4cafa21f74cf93d682a576f9f43acfdf0a1f54ec5d4c3c170b6a8287a19702` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_3.png` | `f9622b258f155a204c1835ec7d550462f0c985d7bf15e2093790a61085a79e00` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_alert_west_3.png.meta` | `74d58fc4ec0dddc6e2c9e799cccd7fea29c12e350773496d85f54f089900772e` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_0.png` | `b0b6d3fc7c990afa4fc0e8ccf547b72a525bec7b2086ecc53868004a93659d59` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_0.png.meta` | `9a74b83c08bdbd0569a246fedddc9cd9ac1262f88b29288381a8c1ed6399c60c` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_1.png` | `2deedd54ec93f904c96250233450bf43368791e29867bb1c3e68dd7b2f77e7b4` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_1.png.meta` | `666c7105c901dd5a648d5b493a56adef040b5acf160a4fa2dee961a3fef07081` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_2.png` | `14404e3fa8bf4684a7d33e01836e4cbb606c08c7741cae2ad01bfb2d2135de94` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_2.png.meta` | `c6c641ca7b016a171b7613031f66f5a74c69c2f049dcf62dc0bebe0364706149` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_3.png` | `7b76e461eca260f84646434dbd3b9635e4247a528134bcdae45c65b28d65b5b1` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_backup_fx_3.png.meta` | `7b6afb5118f2b3ae7bc49f2e210fc0e83293902a717c76562b3f63283b07bf09` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_0.png` | `62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_0.png.meta` | `f5d3a21ea0c5a6ed2cf37b7c54ecc36b4e420f0cfe464970d2ec6757585d16e3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_1.png` | `38027d7526628cb860c0e98684a5103eb19ea3cafa9ced9543df00ad1e160eaa` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_1.png.meta` | `3ae7498f8ae14210e0181aaf636cf93d295b62643033bd5efaafc07eec5f2000` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_2.png` | `62794194dd56d97a0b454185fc65960557c6b5b70628761be9ad293e3c6c30c6` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_2.png.meta` | `441c980e31da3e26c9e0d906106f6a3fc307819837500c2603aa86cfbeb08fe9` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_3.png` | `38027d7526628cb860c0e98684a5103eb19ea3cafa9ced9543df00ad1e160eaa` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_east_3.png.meta` | `a55f38e664f1a0727f223739293cde400ed0cb205bf4136b2bbd459bfdbab46d` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_0.png` | `d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_0.png.meta` | `c67a17b010031140a1825e05f966701d4674c8cd471d9498c5e04bd66cb82ee2` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_1.png` | `ca270bd9ac63a47ddb8c363601be7d36eebd9879420794afeadd89f9182deac5` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_1.png.meta` | `59031921851c829d0803b8c25147eb4b57e6e21f99e8520ab7cdece1323c49ef` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_2.png` | `d5193de522b265f639713b5f0d578886e9e4ec17de2d08054c690a2e79f4de13` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_2.png.meta` | `8f588cbe352207a0c8334e9163a193f379679ff1f8214405892a5268959224f9` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_3.png` | `ca270bd9ac63a47ddb8c363601be7d36eebd9879420794afeadd89f9182deac5` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_north_3.png.meta` | `9e201bc9e4f5f0126597f7765d2fe4aa418958cb787257a4c24f060afeb386c3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_0.png` | `1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_0.png.meta` | `f49450c05573107cf4286bf861a328b2281644ddb54d1564bec8653c300dd3f2` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_1.png` | `b0d8d36a893735187ba17ce8c6b58fa33b543dced5cba28502d9a1c3dea48c71` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_1.png.meta` | `6dafa1159312deb3c952e8da4c9d386e8eb7023fd0504f6afa82731a7496925b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_2.png` | `1b6e3d9c79efb24af942a4dacffdc12d5ac32648967121b73d4a9ddbde43547f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_2.png.meta` | `d5770f8c55da6d51996aa10b630a097f17e3dcf3c1244fdf0f74ae0e275b2f1f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_3.png` | `b0d8d36a893735187ba17ce8c6b58fa33b543dced5cba28502d9a1c3dea48c71` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_south_3.png.meta` | `3c4cdc50d036d0404dc586e3fafcdcc463ba069af266f856071c38ac07661cae` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_0.png` | `20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_0.png.meta` | `598779a83b184839c83560f091ba07220ae603bc67c046d508b88b2ff9579e29` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_1.png` | `2e362567818645810033474dde2cc0c897aabaf1ac1d5fd1375878d8194573fc` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_1.png.meta` | `2c09c34f99a62445d1327dc9515ce5959ce6a85b39b74bcf30996d520fddbe03` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_2.png` | `20822a47998d3f254aedf17bd5af98cf9a4b23fceb65e0dc4261a40eb58df1ea` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_2.png.meta` | `b7142bfa1e20fa93377f3e789c9b9abccb563df7b9ada169c6c13304d8a57cc5` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_3.png` | `2e362567818645810033474dde2cc0c897aabaf1ac1d5fd1375878d8194573fc` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_blink_west_3.png.meta` | `de2047b6b9826744fba42dd85161bc1aba88452b4a64e2b3a6b6220518f8640b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_0.png` | `ec382c3eef610eda2505efcb4b60f2a21c6647192fbd07ffde3523dac653179f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_0.png.meta` | `1792bc78a1c23ba604981d14535967f0f9fea288491f8a3e424e87098c1b7f95` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_1.png` | `01cec20e816265635b5c7894acc131d59f38b461dda934e0c7129e9f1ce97c9a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_1.png.meta` | `7ec7fb60b491c359e8a89cd327d790d465f3fce2f410116c543240820d0304bd` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_2.png` | `96761652ee899efffb2b93600f462dcaa84f54178cb8c626d44406eb7b400544` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_2.png.meta` | `8e7227b22d5b2689c6f4f91e346bc68a8a827c88c57fb58d20bdd911f3f731bd` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_3.png` | `9a4aa120e9c58fbbb172131076ffe7b27d1b16c1c38778b900099a1b77c4e5e3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_east_3.png.meta` | `58b4f9410a15bd38ffdbd3a98f76acc16bd8e583a343b5cacc89cc707c45dfbe` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_0.png` | `85e307239f82d84fe42bc0f00df8e622825df3f706f2fc65d70c1f01a81d2f96` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_0.png.meta` | `57ea27de2c8edb7a5ee37c016de18cfb7d1e1faa24c22f35013553052fd2232f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_1.png` | `9fd5a8a8dbe7fed89bb8b3b2fc6123f18aaa62e44c74215ef334bb22b77b799b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_1.png.meta` | `fa1a84378b1dad064c51c640e1da2e9debff14def9122bbb1eb4034216c85470` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_2.png` | `006d796e239abfe401206da893d8db858fbf6a96c3dc0e9745a625ac8859bb71` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_2.png.meta` | `19ce49374a33bf87ebd49c1a4c13e8394aba97b87736ebd23c6c774df08fcfb1` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_3.png` | `0b3f10172d89b1a9dc96a76b739d3a175eab20560923d53d7bfebae04a38acf8` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_north_3.png.meta` | `1205649ac722503fa28a113c7963db9f3f90970b7b3d5334deb7bcdf177c1b8a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_0.png` | `9c2fae14223bf1040724559f837cbe73f4dc59f56a15baafff834657f60f4ade` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_0.png.meta` | `2ea60507b243b6d421886a8a59430af1943330048a507e8142acb142850e987a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_1.png` | `077c8c6fbc57ddd962855e52534b5bae375b01dda321a6f0b8bea43fce1ecd28` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_1.png.meta` | `b06976da0d72f590c9ff69e8f61e5e6dce88b8b78c220db29d8caa96c5b21b4b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_2.png` | `68b9f2be869fb77fa7318a2ce74ff087bf6e11c986d632438b0599c00e97cf55` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_2.png.meta` | `481290d9ddcd911057499564368744672c55c2e77101d8e7567e6f3d040b75ec` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_3.png` | `d177579872fa3221a59203e97d720aa1304af78e905f26cd3ed1e42befec463b` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_south_3.png.meta` | `7f8fb136870c8c9c0225d6634767184d4335a18f46e6df35165ff90bd0e8b6ec` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_0.png` | `8058280c938aae5134ab6f4074aa612836e55f76900f3644c6cca017a9311769` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_0.png.meta` | `e04d5a7a6920a2a681ffe2b75dcd2c56f2281ee0b792e119b8d1b7a782c1b2f0` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_1.png` | `2c9baf2bbdb245169703d15dc0fccc53e2b2117b087b454e5222dcb8d8de6ac0` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_1.png.meta` | `d8956e4325c0d099b067701a89785f7e7a5be5c77c0d64af178394a85babb399` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_2.png` | `bf53415eaaa5e2db03f02c640cdf172a39b43eb38435fbea27e64beef80460a5` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_2.png.meta` | `201cb7c7e56241b215a88863c03d051f86459c96101c05cdfc3a59cb612c6bf1` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_3.png` | `1d32a2a795a05b04bff7d494220958010b2a9884f3427c967e37b12bea72d3f6` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_hostile_west_3.png.meta` | `59cff2b8e29a1d834f69d3cda420cc3367eaf1a621aee89132a02cc9846c1ae4` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_0.png` | `e6f714e3059ebd93e5b301a355dff32eb7af7685dbcfee3583c48d3bd974660d` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_0.png.meta` | `12e21cf011ed80d19898b16f6a1045d6410f14cb5d1894fc03cfcf69fe850a23` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_1.png` | `f2f8e16dda07a687f552e60b83221ea880de7b0448e131169b8e9c226150eb7a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_1.png.meta` | `68129d453800b24c5f684f6f40721b26ddda1b123e56958f36b3bf0455738f9a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_2.png` | `5ece372060a3906eb4261b928366d951a626f4876971cbe00b4f06f0106f99a1` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_2.png.meta` | `696815c845c1474776e1d8467e87fdb0aacd7355484786ed8b9aedb4303cc709` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_3.png` | `46c9d6fd78d471202b41fa7a765875675e9ef861cb5f63a085039c77a1f26551` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_east_3.png.meta` | `3d4bb5025f6544d4defa870bd849cbaadaad9dfaa75e236497d3fc46dbe68e9f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_0.png` | `6c83e58a456aa4fb896e02a6724e4b6b0c827715e5fbf1f8c02f83ac0e688852` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_0.png.meta` | `00801bb9d07651fa5b17dbcf33e53ab708e776969a533c75aabe1c2a623e2f3a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_1.png` | `acc6f76327476b59335032ce705b6b473868a6c0a067231b5b580c00ef483521` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_1.png.meta` | `16fd3166fb60fd16dbb0ba3cb6ca50648f5789f47978ec68b64748451aad49fe` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_2.png` | `231277021704839b2d5adadeb49d9e6252a6ea159e6e3fd4b3fcf4877f2ab3d9` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_2.png.meta` | `8578d0073562d6895270c5986facfe2f1f4c2458f284688b1672019e2e3682b5` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_3.png` | `ce0805c327d263efce4466f7594b969e7a20906441304091e73c35b24c951adb` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_north_3.png.meta` | `28fcd680a8b75cb3227d6f0277d67bd80c9cbd30965caca610623b14395d715a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_0.png` | `f88dc3aa3ba56d0665edcdfb645a52b0f49b42e2fe6c1d8da1cadd3082f7ef3f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_0.png.meta` | `5ba897b9ebe82363cbbe67d5c9c60b7a02e8b102d07fd043a85f9052a30761b3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_1.png` | `ed512800536281510072ec1fac37f63a6a3b496295722669e068fcf1e6158a43` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_1.png.meta` | `f540bc8f41a17569e70a7316deace26fa09b078043ea2ab049a02e3854d7d572` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_2.png` | `781ad4a24fb015dcd80f22a9974c4fd9a2571e36a206a48638fca7acecefcbe3` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_2.png.meta` | `d950f62164b6d0b6df4b7eb4ba574079a06f367533104c8499573d176c0c4e4f` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_3.png` | `146b10945a842222ff93f986ed38d8d9ba7bbe37af06c8e58ac8a4a811631a83` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_south_3.png.meta` | `65ca65522ff99bf313223774ca6a3dc001b5617a9c0e9df8d18f058aebd172bb` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_0.png` | `eaa6b4bb4507b466186a72833256ec9dcb0f683bd181bba22a1f5334078ae64d` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_0.png.meta` | `6bf23e8e9fbcdaee84627473e71b903510f6e6546ee13225d60bf562aa18b43e` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_1.png` | `141ab31fa970d12a3449b8ae122795509994da6bf7a3053da007045fa5379f13` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_1.png.meta` | `f65e0bbebcc9befd6734fab2960b1d176b49f3e38ea5c693bb3484a92c0699bb` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_2.png` | `2f2bad5e803e7778f3e326e46d975c8548126ff09c10c366a1c399aabf76c6b0` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_2.png.meta` | `ed836da3ee7b9c59414c9f4ff1a3b246d6ed9694a7f47d07e665e22419e88b19` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_3.png` | `a28b061487d5a605fd3c6ab09124a9f409cbeca8a86771990aef45ffd8932076` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_routine_west_3.png.meta` | `e0b578e1b3ac5e11b6f263fde30f5487d06a82030b42f284543e7363e02ceba0` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_0.png` | `bc9ac1296b5a58850774505f17f7cc107056ceafe18c6dbe8c3fe7327f957967` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_0.png.meta` | `74871bb873cd924eda4713dae88f20cecbe345d4ce2d1b99f006896e6aa30a05` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_1.png` | `ed6b6ba6b613586af60d3eda7578b8e00ab65ee6e2a4dc5af85c4a3d0c08f2a9` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_1.png.meta` | `c0c9b3fb0e4d5a7afda6e0c49536232ac1133eea0e875b3c7467708792275d96` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_2.png` | `b2f873459cffb405285aa358c928f35edb0a0dd99a63eff768dcf817c5cb0616` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_2.png.meta` | `a5280cf0023a53316c2d58d9ba5fc8a7dc1f8979db2cb7ef782517a2b5280571` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_3.png` | `6e50c7e149c660502039d558280c121d3a30f78a1af613c38219b6dddb69f97a` |
| `Assets/_Project/Features/Npcs/Sec0/Sprites/sec0_stun_fx_3.png.meta` | `6e53665ffffd761c7aa92fb5261383f587f353200cee269a09d4531049273285` |
| `Assets/_Project/Features/Player/Movement/PlayerMovement.cs` | `3833c86292bbf776b53ca9a57da40f92a47183102c971a2d0563999795cb41d7` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs` | `c5d35d7fdf74b06246edbf681cacd22678e305d93e610d0d5d168cfb6765be3e` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs.meta` | `d9c8c75699e40ca6b3deefca256b0c60c6930570b055a149401eba45016a0201` |
| `Assets/_Project/Features/Player/Succession/SurvivorAwakeningPoint.cs` | `daea862966989015bc471838f80e785a38a63f27e7e952d6e24e1d065d5e7f89` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs` | `59b68114b684d76c3e1a198e3e75dae1e8cc754a4553702188078429867e369c` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs.meta` | `8d635e410076b4241d17eff0bfddbb46e0b7714fb093e3a2da7d02e5e7b0f33a` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs` | `28e02047bd64d56b49a0caaed17e978c33aed433c93925bbbe9a9710524bbe16` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs.meta` | `98ddfdda50a170297c9c7a25caa8bd9fd9f9b7d04d3bfaeb2f02d012464f7e6c` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalsComposition.cs` | `62ebb61c30ede902a93a93d26bf7ba3686d58e0c740a2192e460c66273c0c6d9` |
| `Assets/_Project/Features/World/Expedition.meta` | `71f4f2663a6e9de712ddf277997a5b2c9720ad783a48c9540fd94999d0cae433` |
| `Assets/_Project/Features/World/Expedition/ExpeditionHazard.cs` | `0b3751897b33b88b01f26144011446498dba9f6d3ca493e96fbf9a77a13c8850` |
| `Assets/_Project/Features/World/Expedition/ExpeditionHazard.cs.meta` | `d7f0f0307737c7086c0b0c60cc0c2dd57d182b2705c1550ed33ce5dc2bd9be24` |
| `Assets/_Project/Features/World/Expedition/ExpeditionSupplySource.cs` | `51e0d2719423b9aedd1506cc52dd17641d39ac186988966c8de74be461b351d3` |
| `Assets/_Project/Features/World/Expedition/ExpeditionSupplySource.cs.meta` | `5b3a98e875ea5d7147b9cd3693c697b58290946355086351c43001b6b5fba891` |
| `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs` | `14a0c63155f088f0e20302e4f476ab5d7b7984bab91fc39f97a979cbe10c6a57` |
| `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs.meta` | `f1825677a978e9134297733fd093b1849d49815d724aad8eddc0abf110de529d` |
| `Assets/_Project/Features/World/Restoration/CriticalComponentSource.cs` | `c2f4a559f76289c886c86f678116d9b996b1423aa168238883a4e8ecc39ff716` |
| `Assets/_Project/Features/World/Restoration/CriticalComponentSource.cs.meta` | `c4e3833ce890f36716c1fd9c08a14f5b0606f5f2fce0ee0e9a93082bce37f8a6` |
| `Assets/_Project/Features/World/Restoration/RestorableSystem.cs` | `0f713bf2bc1205f2e17117ea268978885c578b3f1949e9bffc2e986003876a7a` |
| `Assets/_Project/Features/World/Restoration/RestorableSystem.cs.meta` | `174a4510996a0f28bb66b2ec08e08f065c4a869140c28c3005bbf4c9decc12e4` |
| `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs` | `083aa388b8f946a8166ecc6200f8116fe073375c105281679917e1ef8c2f87a8` |
| `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs.meta` | `9f3816e70dc11d51d235ffef36d9092e639319e32e92aa517501e985483ed452` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs` | `f33dd1541d49ae562c76733d43e1803427c9372381ee14f7df0a246f6e5dd8dd` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs.meta` | `de2afff5d6cdd40fa9cbc45878361c4018853afb48e21cb497ec7bbb73ce138c` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs` | `922e34d35b88ff279d40b5bda2b4969e544f2e4c8df075f6f2c89ba160f97f1c` |
| `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs.meta` | `8d281b45fea81cbdc8f8c38811b7fc73919db86e85fee80451781d3072766485` |
| `Assets/_Project/Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs` | `15b84cbcbe4ed3dba503c9dd81372390969a52863f9472ed3b581b839e38a3bb` |
| `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs` | `103fc78f0bae1b5bc627e77641afeb335a7ff55e714745814d0e0b9257dd14a4` |
| `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs.meta` | `a791eadc31402968e572bc5c0e1f7a63d48390ab4597e38ad0eac01ca42f5a22` |
| `Assets/_Project/Scenes/Demos/Sec0Patrol.unity` | `4ce21ff4da9d422a9e3a3ee897921b0cf98b141614214db8a6d39bf7e01526c9` |
| `Assets/_Project/Scenes/Demos/Sec0Patrol.unity.meta` | `d8970958bac7eb7199cac00c4d8c25ff91bfc214ccd6b8f76cff1ecd1cbbeee0` |
| `Assets/_Project/Scenes/Wakeup.unity` | `1732ec878ff74d452ebd9c637e11027d9b57444e0246f1aa14dd8161ebae2505` |
| `Assets/_Project/Tests/Application/RunLifecycleTransitionTests.cs` | `2803a35949a2dda96eed1003928cd574f36bb447b1955e65dff3b52337ac1ccd` |
| `Assets/_Project/Tests/Application/RunLifecycleTransitionTests.cs.meta` | `d3b661da5e777e43a7a8cdc443975150a7c1dec381eff404c35fb5fe2df93f9e` |
| `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs` | `0b02df3d8eedbabf6ca6b6262273bc3e9d6825070bc55d39637fab94b87478f3` |
| `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs.meta` | `e045bde2b5568496bc3095cd39dba5e6a2a8a9caed0ef7dd51ca5e88804e697f` |
| `Assets/_Project/Tests/Domain/CriticalComponentSourceTests.cs` | `bb59dee8cb4edb88b17233d780959abea8aa6ace24dae2ce998de664c4205000` |
| `Assets/_Project/Tests/Domain/CriticalComponentSourceTests.cs.meta` | `6e891258fa08d59f3578410823a9cef7b8c9c78d07ca8a262534bf7f62d75766` |
| `Assets/_Project/Tests/Domain/ExpeditionRefreshTests.cs` | `583fa42c32327dc0d02482844ef09d0f5b4592aa87ae099dda07465af21b6fd9` |
| `Assets/_Project/Tests/Domain/ExpeditionRefreshTests.cs.meta` | `d0e1790e90a487fa7e3d510f853bbe08b6c81852c245b2a67ae7efd572f9d5f0` |
| `Assets/_Project/Tests/Domain/RestorationTests.cs` | `9ae5106ee3f83995ad048c1a07611654fd4fe7646b3b7f798dfa756b8474db47` |
| `Assets/_Project/Tests/Domain/RestorationTests.cs.meta` | `bea075613a2714178da71e6e8bc97f8abf61445cd6d960ce7565a709372c97dc` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs` | `405e72fae823918e5ee01fcefc4f9085c55a0d46b831c180517e5c271c513615` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs.meta` | `5c277efeaa2156af83b12914fe62f0548252230281dcc9d6c929e6c5ffce044f` |
| `Assets/_Project/Tests/Domain/ScrubberEncounterTests.cs` | `7eac7d60f01da0b7ed4f6f0e1a7bbcd8066f6bf93f7ca535dac55aa25620fc5d` |
| `Assets/_Project/Tests/Domain/ScrubberEncounterTests.cs.meta` | `4b9f8e75df974ff1ed36b1c8dee1cdda59c8e1bd5ded6b2cbbe163192c5e8d44` |
| `Assets/_Project/Tests/Domain/SecurityBotBrainTests.cs` | `85a1ef21bc362d66d3810969a78f02619fd0163699d729f60540ad785ef33c2d` |
| `Assets/_Project/Tests/Domain/SecurityBotBrainTests.cs.meta` | `5ad5aeb0908177d5155a45363dd537093cc18e2e28239ea54c4c20a6c20ca091` |
| `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs` | `ab876387200f049e0cab4047b7952b02258c6a917aaf7a7d754fe973bff0fa37` |
| `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs.meta` | `860c54b3bd9e12fad458ebbf736105f750280d6dcb03d2066538cd845b569844` |
| `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs` | `f8654ae1ecf647bd8318cde8ed3b96d9e1687f320fe3a50444d78240a16974b3` |
| `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs.meta` | `208e15da5480fe77daa5a2c550e73962ef13000171a73302d887651267d14acd` |
| `Assets/_Project/Tests/Editor/RunLifecycleAuthoringTests.cs` | `f40405717b08135894a126957efc8b19b0c40585b9373887c0d2bb1496dcec0d` |
| `Assets/_Project/Tests/Editor/RunLifecycleAuthoringTests.cs.meta` | `0278c285068ce8dc292c91d7950bdb0cdcfee27300c6fd1454185262230cd81a` |
| `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs` | `c1bf2a09def781c9beb04b434bc8be9f781a1f3170f745c764392bf521a50f3a` |
| `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs.meta` | `04af4f51fbb50554a8ccc52b93c0a9d4acc63715f3e695f84b212a3d3dcf7c16` |
| `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs` | `514064ea4f676f8f1e006775a84e63e76b02b1104c288eb35cf94fdebcc15f04` |
| `Assets/_Project/Tests/Editor/ScrubberEncounterAuthoringTests.cs.meta` | `cb1325702aac931ef9791ce359c2a41b5794211c1634177775e2fa0456655cb1` |
| `Assets/_Project/Tests/Editor/ScrubberTaskSourceTests.cs` | `20c1e7dacbfddd9c05a15d4b06273dedebc54465423d816af535c7788ce7c3e1` |
| `Assets/_Project/Tests/Editor/ScrubberTaskSourceTests.cs.meta` | `c763ed7c59df4403a134b01894262f665474b48c42d68950618bb56b06d53ef7` |
| `Assets/_Project/Tests/PlayMode/InteractionScopeLifecycleTests.cs` | `ca6aeb4c22ae470ddc074f3971011f2846a8f2b4911f1e621a63aae45121dc54` |
| `Assets/_Project/Tests/PlayMode/InteractionScopeLifecycleTests.cs.meta` | `7c6d636af4373b201ad70171d630da9ba257e3b5f0c0a439a7f63b0fea728108` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs` | `c847775cac1fae70a7150246042ea00694f196003294e6112c37aff85dc1ddad` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs.meta` | `553cf31526a3146dd2d1bfb490bb744052ae98a9a525ab75ef97a903fa62b039` |
| `Assets/_Project/Tests/PlayMode/RestorationDoorTests.cs` | `35b3ed3b99535f62f138b96a00ee3deab74929d07c94d8e41eca4a4fed9c4cf1` |
| `Assets/_Project/Tests/PlayMode/RestorationDoorTests.cs.meta` | `4276672d9aecd9454a0d06b44e589e2b0908c88d966127b8883f5b34b3ebc51a` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleDeathTests.cs` | `6b66ff22c3d2fcdfa64635164a599c7d8f85454ef69e644ae0ed178b712f4281` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleDeathTests.cs.meta` | `0c34f9df70c0dc496d84a30fef52b26e838b158bcc82057bf85eabb4b0eb6409` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleEncounterTests.cs` | `de7f57320bebd9307b36a72c157464fbaee096cc5f6dbdad600eb44e7bc2e08b` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleEncounterTests.cs.meta` | `392fc1b10f31edb72521a79017b9ec62964bca8f1c79b67e68b8ff9b0beca4ce` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleRefreshTests.cs` | `a6da7bbc9db81e9ae5db4e9765da6d25bd697c2001418e65f733d15b781a61af` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleRefreshTests.cs.meta` | `82af43b2aeacc31aec4aed6c122e9985eae099806ff90acc2575f9e8c1c82b00` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `a301d581b72d6e5c469ea2d82f23aa9fa38a3f2f8087657e35a8da08a2570dc6` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs.meta` | `a0972c016bcdec679462df74e30f69ee577d1073348798266d1a7278f7ef4b30` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleShipPersistenceTests.cs` | `36f6d15856d511d18a4306b26b34f29261a5df1e64f0afbb85e9664695e7a67a` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleShipPersistenceTests.cs.meta` | `a9ead1e9dd04b760aa288b92ed118fac0f23ae6a87eb3c15355de3e50b9f7350` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleSliceTests.cs` | `be85f163a862a034062b24cae5a072a7d76af129f755db707aacffce6f789ac1` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleSliceTests.cs.meta` | `fd3ae8c8a1c9f1a8e44e4dfc2044a2554a42e01c27d2f35977c45ddafbc403c0` |
| `Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs` | `6ac7ecd50c7905ed7374baeb94de130f5a78b1badde5f5bc3dc7a0a4d83f166f` |
| `Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs.meta` | `dd8774231dc0ec3fb08c989bc64ad85bc5be2fb2ea47604945724a667c4747a2` |
| `Assets/_Project/Tests/PlayMode/Sec0RuntimeTests.cs` | `cb8284df7f76a5e944b3aa712cc22d560c4f2682caec3802f7ffe1af8684414a` |
| `Assets/_Project/Tests/PlayMode/Sec0RuntimeTests.cs.meta` | `6c017708019dcee2feb0c714e14cad2ea5da15758606396f7ed4abbc422d4f8c` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs` | `2af09398934064f21e7962ea941aa680cb1a31accad0487a2741cc8f9afcf7a0` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs.meta` | `ef491cf9eff187dd220e68be60aecf863c0b3f786055fa3162078e0b8537a9c0` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs` | `750ab689fcf14ebe099c1bfdffede93dc98eaade1ffeee1503aa05b2ed793473` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs.meta` | `acc2cd3fa0ab3307caa3f173fe5fe7402d915e98f8e414d8f4a9e5abb392229e` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs` | `43b41b10f62344552e49e84a5d16e3a63ee6790806eb25d15eb4ccbaa6d86b58` |
| `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs.meta` | `8bbb829c6a75f40eb090956038cfe0fb9d8c4a4e403da7c5b647a5e8691ab063` |
| `Assets/_Project/Verification/Player/SOLITUDE.Verification.Player.asmdef` | `29bfd85394ab1b1a1dd723a445b94dd7e1534b5046fbd8d277c3ec949242f451` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `a4a0e72911c39aff47070f13b099152a0908d029ffabbbc89c9ef6ef711a1947` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs.meta` | `758b4e70b310ac785640b4ba8e21228d5aff22946a6572f413380f76b58ce467` |
| `ProjectSettings/EditorBuildSettings.asset` | `c2d68dc476e1e2de0fafccc29e7f73e79f5629b98e5971e275766bfd6e9dbe8e` |
| `README.md` | `564b3e6fd0eaa012bf1a086bfb9813a8a3d1a2da2d5816373e23cfc5dcf351fc` |
| `Tools/Verification/verify.py` | `208fb40da2b290e2ac3b3718fa5b5db8ec090f560163f19ef2923ad553e48a7b` |

