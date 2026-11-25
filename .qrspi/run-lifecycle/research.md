# QRSPI Research: run-lifecycle

## Answers

### What currently happens when a colonist’s vitals reach a lethal state, including inventory disposition, player control, and any replacement-character behavior?

Health reaching zero is not currently connected to automatic succession. `PlayerVitalsController.Damage` applies recovery-rule results and publishes vitals changes; `PlayerVitalsComposition` advances those rules but does not request death. At zero health, vital consumable reservation returns `Dead`; SEC-0 target availability also becomes false. No runtime caller connecting a vital event or zero-health condition to `RequestDeath` was found in `Assets/_Project` C# excluding Tests, Editor, generated directories, and workflow directories (searched `RequestDeath|Health.*0|IsDead|Died|death`). The runtime `RequestDeath` calls occur in the succession coordinator and the succession composition's API/debug context menu.

Explicit succession gates input, resets held input, closes player screens, transfers both inventory and hotbar contents into a scene-local recovery container at the death position, retires the departing survivor identity, awakens the retained avatar at the awakening point with reset authored vitals, clears hotbar selection, and acknowledges the event. Empty equipment produces no recovery container. The successor has an unused pending GUID and becomes active after avatar activation. Success restores the transition gate; post-commit failure retains the pending transition and recovery ownership for retry.

Files: `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:108`, `:161`; `Assets/_Project/Features/Player/Vitals/PlayerVitalsComposition.cs:36`; `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs:37`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:57`, `:68`, `:81`, `:127`; `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs:59`, `:74`, `:88`.

```csharp
if (Vitals.Health.PreciseCurrent <= 0) return VitalUseOutcome.Dead;
// SurvivorSuccessionComposition
avatar.Awaken(awakeningPoint); context.Selection.Select(-1);
if (preparedRecovery != null && !preparedRecovery.IsPublished) preparedRecovery.Publish();
```

### How are a colonist’s initial vitals, inventory contents, and awakening location currently established?

Vitals are serialized maxima/current values, initialized in `Awake` and reset again by `SurvivorAvatarAdapter.Awaken`. Reset preserves vital objects, applies authored values clamped to maxima, and clears recovery timing. Wakeup authors health, oxygen and hunger current values at 100. Scene binding registers the fixed owners `player.inventory` and `player.hotbar` with their authored capacities and no initial loot; saved owner contents are restored if present, otherwise the new owner begins empty. Succession empties these owners through handoff rather than registering replacement owners. Scene initialization creates and activates a fresh survivor identity, then calls `Awaken`.

The awakening point is an explicitly serialized same-scene reference. Wakeup's CryobayAwakeningPoint is at `(-0.89, -2.4, 0)`. Authoring initializes a newly created point from the scene's authored player position. Awakening stops movement/body velocity, relocates the existing avatar/body, synchronizes physics and rebuilds proximity before resetting vitals.

Files: `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:21`, `:84`, `:285`; `Assets/_Project/Scenes/Wakeup.unity:6661`, `:2509`, `:6561`; `Assets/_Project/Core/Composition/SceneBindings.cs:134`, `:138`; `Assets/_Project/Domain/ContainerCommandService.cs:42`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:53`; `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:24`; `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs:71`.

```csharp
healthChange = Vitals.Health.ApplySilently(Vitals.Health.PreciseMax, Authored(currentHealth, Vitals.Health));
timing = default;
// SceneBindings
var inventoryBinding = session.Register(ContainerSaveSession.PlayerInventoryId, inventory.Capacity);
```

### What currently determines whether defeated enemies and collected supplies return after leaving and reentering a scene?

Collected world pickups use a persistent per-instance `SaveableId`; their IDs are retained by `ContainerSaveSession` and saved in `collectedPickupIds`. Registering an already collected pickup deactivates its GameObject. Locker contents are stored by persistent container ID; initial loot rolls only when there is no saved record. Production scene reload preserves the session. Disposable demo player scenes reset the session on each bind, clearing collected IDs and container records, so authored pickups return there.

A defeated-enemy state was not found within `Assets/_Project/Features`, `Assets/_Project/Scenes` and Domain runtime C# searched for `defeated|enemy|Death|Dead`. The implemented SEC-0 controller has patrol/investigation/pursuit/charge/recovery behavior and stun attacks, with scene-owned brain/navigation state recreated in Bind; no health/defeat save contract is present in that controller or SaveGameData. Consequently no current defeated-enemy persistence rule can be established.

Files: `Assets/_Project/Features/Items/Core/WorldPickup.cs:35`; `Assets/_Project/Domain/ContainerSaveSession.cs:36`, `:83`, `:91`; `Assets/_Project/Core/Composition/SceneBindings.cs:203`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:129`; `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs:61`, `:198`; `Assets/_Project/Domain/SaveGameData.cs:11`.

```csharp
if (service.IsCollected(pickupId)) gameObject.SetActive(false);
var candidates = !saved && source.Loot != null ? loot[source.Loot].Roll(...) : null;
```

### Which existing gameplay entry points initiate character replacement, a new expedition, or a restart, and which systems respond to those transitions?

Character replacement entry points are `SurvivorSuccessionComposition.RequestDeath(Guid)`, the `Debug/Retire Current Survivor` context menu, and coordinator retry exposed by `Debug/Retry Survivor Awakening`. These invoke the succession ports described above. `GameCompositionRoot.TransitionTo` exposes asynchronous single-scene loading. No gameplay caller of `TransitionTo` was found in first-party runtime C# (`rg TransitionTo`, excluding Editor/Tests/generated/workflow directories). `GameCompositionRoot.StartNewSave` and the forwarding SaveGameService context menu initiate a full saved-world reset and active-scene reload, releasing composed scopes, input/UI owners, and task journal; resume game/pause state; reset containers and collections; and flush the new save. The start completion trigger only displays Access granted and logs that the next area is not built.

No distinct new-expedition gameplay entry point was found within the same runtime C# scope searched for `NewExpedition|Restart|LoadScene|TransitionTo|StartNewSave`.

Files: `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:57`, `:58`, `:65`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:148`, `:157`; `Assets/_Project/Core/SaveLoad/SaveGameService.cs:46`; `Assets/_Project/Features/World/SolitudeStart/SolitudeStartCompletion.cs:17`.

```csharp
session.Reset(config.worldSeed, releaseOwners: true);
if (!string.IsNullOrEmpty(scene.path)) SceneManager.LoadScene(scene.path);
```

### Which inventory owners and item containers occur in the cryobay, maintenance area, and atmospheric scrubber, and how do their contents relate to individual colonists and the shared world?

Wakeup has the composed player inventory and hotbar, one authored locker of capacity 12 with persistent ID `c7320c2a-7a48-41c9-98b4-bf5976399466`, and two registered world pickups. It has CryobayAwakeningPoint and a succession feature. The atmospheric scrubber demo binds one world pickup and no lockers; its player owners use the same fixed IDs. Recovery containers are dynamically registered command owners named `survivor.recovery.<random GUID>`, with capacity equal to combined inventory/hotbar capacities and metadata identifying the departing survivor. They are not ContainerSaveSession bindings and forbid SaveableId/LockerContainer authoring. Locker IDs belong to world instances and player inventory/hotbar IDs are not colonist GUIDs; succession retains their readers and clears contents by handoff.

A separately identified maintenance area/container was not found within first-party Features/Scenes serialized assets and C# searched for the word `maintenance`; no mapping from that location label to an authored owner can be established from that scope.

Files: `Assets/_Project/Scenes/Wakeup.unity:6522`, `:13303`, `:13333`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:5324`; `Assets/_Project/Domain/ContainerSaveSession.cs:10`; `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs:38`, `:57`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:86`.

```csharp
instance.SourceSurvivorId = sourceSurvivorId;
instance.Container = context.Commands.Register("survivor.recovery." + Guid.NewGuid().ToString("N"), capacity);
```

### How are colonists, enemies, containers, repairable objects, and doors identified across scene transitions and saved-state restoration?

SurvivorState uses a random nonempty GUID and Pending/Active/Dead status. Scene initialization creates a new identity; release clears the coordinator. No survivor identity/status is in the save schema. Saved containers use strings: fixed player IDs, and authored SaveableId strings for lockers. World pickups use their own SaveableId, distinct from GUID-derived item type IDs. SaveableId can assign an empty runtime identity or create a missing scene GUID in the editor; it refuses replacing an existing runtime ID.

SEC-0 controllers bind serialized scene object references and runtime owners, with no saved identity field in their inspected controller/composition. RestorableSystem and ScrubberEquipment hold newly constructed state objects; doors refer to components and carry runtime booleans. These inspected objects are not represented in SaveGameData's fields.

Files: `Assets/_Project/Domain/Survivors/SurvivorState.cs:10`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:53`, `:150`; `Assets/_Project/Core/SaveLoad/SaveableId.cs:22`; `Assets/_Project/Features/Items/Core/ItemDefinition.cs:34`; `Assets/_Project/Features/World/Restoration/RestorableSystem.cs:8`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:14`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:14`; `Assets/_Project/Domain/SaveGameData.cs:11`.

```csharp
public SurvivorState() : this(Guid.NewGuid()) { }
public List<ContainerSaveRecord> containers = new();
public List<string> collectedPickupIds = new();
```

### What existing lifecycle or scene-management constraints affect replacing the current colonist and refreshing expedition state without rebuilding unrelated gameplay systems?

The bootstrap survives scene changes with DontDestroyOnLoad, owns the session/catalog/policy/input, and rejects duplicate roots. Only one composed player scene may exist. It rejects mixing disposable demo and production sessions. Each composed scene must have at most one SceneBindings. Succession requires exactly one same-scene succession composition, an active same-scene awakening point, a retained avatar with movement/vitals/body, valid inventory/hotbar readers, feedback and screen/input bindings, and an inactive validated recovery prefab. Prepare rejects busy commands or unavailable owners. Avatar awakening retains the same avatar and vital objects. Scene feature release is performed in reverse binding order before owner disposal.

Session reset replaces saved-world data and collection facts; with releaseOwners it disposes registered owner generations and clears container records. Neither the current survivor API nor the save schema contains an expedition object.

Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:43`, `:120`, `:125`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:33`, `:48`, `:75`; `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:17`; `Assets/_Project/Core/Composition/SceneBindings.cs:308`; `Assets/_Project/Domain/ContainerSaveSession.cs:91`.

```csharp
if (entry.HasPlayer && interactive != null) throw new InvalidOperationException("Only one local player scene may be composed.");
if (context.Commands.IsBusy || !context.Inventory.IsAvailable || !context.Hotbar.IsAvailable) ...
```

### How are scene transitions performed, and what happens to player vitals, inventory, enemies, and container contents when scenes unload and reload?

TransitionTo validates runtime startup readiness and that the scene is streamable, sets the policy transition gate, closes the interactive scene, and calls LoadSceneAsync Single. Unload releases scene bindings; load composes the next scene and clears the transition gate after startup/recovery completion. Policy Gameplay is false while transition is true. Release detaches features, interactions, task scope, modals, input, pickup/locker bindings and container owners. Disposing a saved owner captures its contents into the retained session before unregistering. Reload registers new owners from those records. New scene vitals initialize from serialized values rather than saved vitals. SEC-0 binds new scene state. Dynamic recovery owners are unregistered and destroyed by succession release. Disposable demo reload additionally resets the session.

Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:96`, `:138`, `:148`; `Assets/_Project/Application/RuntimePolicy.cs:36`; `Assets/_Project/Core/Composition/SceneBindings.cs:301`; `Assets/_Project/Domain/ContainerSaveSession.cs:129`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:285`; `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs:67`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:150`.

```csharp
policy.SetTransition(true); interactive?.CloseForTransition();
return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
session.Store(Id, ContainerSaveSerializer.Capture(Container));
```

### How are completed ship repairs recorded and restored, and how does the atmospheric scrubber’s repair state affect other gameplay systems?

Repairs currently record scene-owned in-memory state, not saved ship progress. RestorableSystemState consumes one required inventory item and sets IsRestored in the same command commit before observers run. ScrubberEncounterState wraps a repair state; fuse installation permits restart; restart sets Running; five seconds of advance establishes Operational. Neither state has a restore API or SaveGameData field. Same-scene succession retains these objects, while scene reload reconstructs them offline.

Scrubber feedback controls equipment/status color, warning visibility, alarm and running audio. Task source/screen observe equipment and show installation/restart/recovery/completion stages. No scrubber-to-player oxygen simulation or scrubber-to-door dependency was found in Features runtime C# searches for `ScrubberEquipment|ScrubberEncounterState|Operational` outside scrubber-owned files; its authored consequences are UI/tasks/audio. The generic restoration demo separately uses RestorableSystem for terminal and door power.

Files: `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17`; `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:8`, `:24`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:47`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:22`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs:64`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs:217`.

```csharp
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
public bool Operational => Running && recoverySeconds >= 5;
```

### How are powered doors and unlocked shortcuts recorded and restored, and how do their interaction and power requirements respond to a completed repair?

SolitudeSlidingDoor has scene-local opening/open booleans. It initializes its blocker enabled, animates nine poses, then sets open and disables collision. A RestorationDoorGate subscribes to a referenced RestorableSystemState and reconciles AllowsAccess with IsRestored. A gated closed door reports No power and blocks interaction until the repair completes; completing a repair allows a subsequent interaction, it does not automatically open the door. OpeningPowerSocket is a separate scene-local bridge: it checks for a battery in player inventory, requests door interaction and sets Powered, without consuming the battery. Neither power nor open state is saved. Same-scene succession preserves them; demo reload resets them.

No separately modeled permanent unlock/shortcut record was found within first-party Features/Scenes/Domain C# and serialized assets searched for `unlock|shortcut`; SaveGameData has no door/unlock field.

Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:15`, `:21`, `:31`, `:45`; `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs:14`, `:25`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:18`; `Assets/_Project/Domain/SaveGameData.cs:11`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs:199`, `:211`.

```csharp
if (!HasPower) return InteractionResult.Blocked("No power — restore system");
private void Reconcile() => restored = system != null && subscribed != null && subscribed.IsRestored;
```

### What save mechanisms exist, which character and world state do they retain across closing and reopening the game, and when is that state captured and restored?

Production uses `persistentDataPath/solitude-save.json` (Editor/verification may override the directory through `-solitudeSaveDirectory`). The version-2 schema retains world seed, indexed item/quantity container records, and collected pickup IDs. It does not retain survivor identity/status, avatar position/vitals/recovery timing, recovery-container contents, NPC state, repair state, door state or task journal. Player equipment is retained through fixed saved owner IDs. Dirty container commands cause saving; Flush captures complete session state in LateUpdate, application pause, quit, destroy, and release, unless writes are disallowed, integrity failed, commands are busy, state is clean, or recovery awaits completion. Startup loads before creating and binding the session.

SaveRecoveryStore validates encoded bytes, writes durable temporary data, replaces primary while retaining a backup (preserving corrupt candidates separately), and recovers a valid backup when possible. Recovery completion occurs after composed startup succeeds. Disposable restoration/scrubber demos configure an in-memory store and reset their session on scene binding.

Files: `Assets/_Project/Core/SaveLoad/SaveGameService.cs:18`, `:39`, `:47`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:59`, `:73`; `Assets/_Project/Domain/SaveGameData.cs:9`; `Assets/_Project/Domain/ContainerSaveSession.cs:38`, `:78`; `Assets/_Project/Application/Persistence/SaveRecovery.cs:46`, `:69`, `:82`; `Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs:16`.

```csharp
private void LateUpdate() => Flush();
private void OnApplicationQuit() => Flush();
files.WriteDurable(Path + ".tmp", text);
files.Replace(Path + ".tmp", Path, previous);
```

### How are critical repair components acquired, stored, transferred, consumed, and removed, and which current paths can leave required components unavailable after death?

Battery and replacement fuse are stackable item types (maximum 99), with authored pickups granting one each. Pickup collection grants to player inventory and atomically marks the instance collected; full inventory leaves collection unapplied. Containers use command transactions for transfers. Restoration requires the component in `player.inventory` and removes one atomically with the repair fact. A component in hotbar or a locker/recovery cache is not eligible until transferred back into inventory. OpeningPowerSocket only checks a battery and does not consume it.

Explicit death transfers all carried inventory/hotbar contents into recoverable scene-local ownership, without component-specific exemptions. Death therefore leaves the successor's repair inventory empty until recovery/transfer. Recovery owners are not saved and are destroyed on scene release. In production a collected pickup's ID remains saved even if its component was placed into an unsaved death cache, so unloading/closing before recovering that cache leaves the component absent from saved equipment and its pickup still collected. Consumed components are likewise gone from saved inventory, while repair state itself is not saved. These statements follow the inspected save schema, collection, consumption and release paths; tests were not run.

Files: `Assets/_Project/Features/Items/Definitions/Battery/Battery.asset:20`; `Assets/_Project/Features/Items/Definitions/ReplacementFuse/ReplacementFuse.asset:19`; corresponding `BatteryPickup.asset:16`, `ReplacementFusePickup.asset:16`; `Assets/_Project/Domain/ContainerSaveSession.cs:41`; `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:22`, `:33`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:89`, `:150`; `Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.cs:58`, `:103`; `Assets/_Project/Domain/SaveGameData.cs:11`.

```csharp
inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId
var result = Commands.Grant(inventory.Container.Handle, itemId, quantity, () => collected.Add(pickupId));
context?.Commands.Unregister(Container.Handle);
```

### Where are bandages and rations supplied in the slice, and what existing item or container behavior governs their quantities, replenishment, and distinction from unique items or permanent discoveries?

Not found within `Assets/_Project`: word-boundary case-insensitive searches for `bandage|bandages|ration|rations` across authored `.asset`, `.prefab`, `.unity` and first-party `.cs` (excluding Tests, generated/workflow directories) returned no matches. The inspected authored item definitions are Battery, O2Canister and ReplacementFuse. LockerLootTable rolls battery quantity 1–3 or oxygen canister quantity 1–2, with 1–3 rolls. Wakeup references this table for its one locker.

ItemDefinition supports Stackable/MaxStackSize and consumable kind/restoration amount; it contains no unique/permanent-discovery flag. Current supply replenishment follows owner/collection identity: no reroll for a saved locker, no recollect for a collected pickup; session reset clears these facts. Medical/food consumable infrastructure exists, but that does not establish authored bandage/ration supply.

Files: `Assets/_Project/Features/Items/Core/ItemDefinition.cs:20`; `Assets/_Project/Features/Items/Loot/LockerLootTable.asset:15`; `Assets/_Project/Scenes/Wakeup.unity:13305`; `Assets/_Project/Core/Composition/SceneBindings.cs:203`; `Assets/_Project/Domain/ContainerSaveSession.cs:44`, `:91`.

```csharp
public bool Stackable = true;
public int MaxStackSize = 99;
public VitalConsumableKind ConsumableKind;
public double RestorationAmount;
```

### What safeguards currently prevent repeated death handling or conflicting transitions when death occurs during an interaction, repair, or scene change?

The succession coordinator rejects reentrant requests as Busy, rejects stale survivor IDs, reserves a single pending transition, remembers committed equipment receipt/stage, and resumes forward without repeated transfers/activation. Successor identities cannot be reused; acknowledgment delivery reserves its transition ID before feedback. Preparation and commit reject busy commands or stale handoff state. Pre-commit failure cancels preparation and ungates; post-commit failure retains ownership and remains gated for retry. The transition policy blocks gameplay/input; succession resets held input and closes screens/gestures. Repair has its own restoring reentrancy guard and command busy check, and commits consumption and completion together. Vitals mutation also has a publication/reservation gate.

There is no automatic lethal-state dispatcher to coordinate with scene changes in the inspected runtime scope (question 1). `TransitionTo` checks root State and streamability, but has no explicit pending-succession/coordinator check; succession RequestDeath checks expected identity/coordinator execution, but no explicit scene-load operation field. These are the current checks, not evidence of tested arbitration for simultaneous lethal state and scene load.

Files: `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs:34`, `:67`, `:84`, `:110`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:68`, `:111`, `:136`; `Assets/_Project/Domain/ContainerCommandService.cs:103`; `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:19`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:148`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:185`.

```csharp
if (executing) return SuccessionOutcome.Busy;
if (Current == null || Current.Id != expectedSurvivor || Current.Status != SurvivorLifeStatus.Active)
    return SuccessionOutcome.StaleSurvivor;
```

### What automated tests and manual validation conventions currently cover character death or replacement, inventory initialization, and scene transitions?

Coordinator tests cover consecutive/stale death requests, preparation failure, post-commit retry and receipt behavior. PlayMode succession slice tests exercise empty/equipped death, retained avatar/vitals/readers, selection, held movement, recovery interactions and authoring failures. Failure tests cover rejected/stale handoffs, activation/acknowledgment retry, item conservation, and death while journal/inventory is open. PlayerVitals runtime/slice tests cover authored awakening values and scene release. These call RequestDeath explicitly; they do not establish automatic death from lethal vitals. Pickup persistence tests cover fixed inventory initialization/restoration and scene unload/reload.

Verification tooling copies Assets/Packages/ProjectSettings into a disposable workspace, runs pure tests, authoring validation, EditMode and PlayMode tests, then verification/shipping builds. The built-player smoke workflow runs collection and restart/new-save scenarios. A visual verification controller configures representative vital states; context menus provide explicit succession debug triggers. Markdown manual guides were excluded by the research source contract, so their conventions were not inspected. No tests or manual validation were executed in this pass.

Files: `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs:58`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs:61`, `:111`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionFailureTests.cs:154`, `:210`, `:278`; `Assets/_Project/Tests/PlayMode/PlayerVitalsRuntimeTests.cs:121`; `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs:88`; `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs:53`, `:256`; `Tools/Verification/verify.py:38`, `:52`, `:60`; `Assets/_Project/Verification/Player/PlayerVitalsVerification.cs:59`.

```csharp
Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(first.Id));
// Verification tool
 tests('EditMode','editmode')
 tests('PlayMode','playmode')
```

### What automated tests and manual validation conventions currently cover defeated enemies, collected loot, persistent repairs, door unlocks, and save restoration?

PickupPersistenceTests assert collection round-trip inventory/facts, atomic capacity failure, duplicate identities, scene-unregistration retention, reset, write failures and presentation across reload. The built-player composition smoke validates collected pickup/inventory/locker state after process restart and new-save reload. SaveRecoveryTests/SaveJsonCodecTests exercise recovery and save validation; verification.py additionally mutates isolated saves for partial temp, absent/corrupt/incompatible primary, unknown items, capacity incompatibility and read denial.

RestorationSlice/RestorationDoor/RestorationTerminal/RestorationDemoLifecycle tests cover component consumption, power reconciliation, door animation/traversal, repeated interaction and disposable reload. SurvivorSuccessionShipCompatibilityTests explicitly assert repairs/terminal power/open door/collected pickups persist across same-scene succession, then assert repaired systems and doors reset on demo scene reload; scrubber likewise stays running across succession and resets installation/running/recovery seconds on reload. Thus existing coverage establishes scene-lived repair persistence, not saved ship repair/shortcut persistence. No defeated-enemy or permanent-shortcut persistence tests were found within Tests C# searches for `defeated|enemy|shortcut|unlock`; SEC-0 is currently a stun/patrol controller (question 3). Manual Markdown guides were not inspected; the source smoke/verification harness and debug triggers are the inspected validation conventions. No passing execution is claimed.

Files: `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs:53`, `:142`, `:229`, `:256`; `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:109`; `Assets/_Project/Tests/Application/SaveRecoveryTests.cs:36`, `:47`, `:79`; `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs:14`, `:15`; `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:75`; `Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs:44`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs:199`, `:210`, `:217`; `Tools/Verification/verify.py:65`.

```csharp
Assert.IsTrue(system.IsRestored); Assert.IsTrue(terminal.IsPowered); Assert.IsTrue(door.IsOpen);
// After demo reload
Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored));
Assert.IsFalse(One<SolitudeSlidingDoor>().IsOpen);
```

## New Questions

