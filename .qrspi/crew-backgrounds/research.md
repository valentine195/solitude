# QRSPI Research: crew-backgrounds

## Answers

Paths beginning with `Assets/_Project/` are repository-relative. Source inspections establish behavior and authored configuration. Test citations describe assertions in the source.

### What determines starting Health, Oxygen, Hunger, movement speed, carry capacity, and weapon damage, and which values are shared across playable colonists?

- The scene avatar's serialized vital settings supply maximums and awakening currents. Awake and survivor awakening use `ResetForAwakening`, which bounds the configured currents against the live vital maximums and clears recovery timing. Wakeup and the three composed demos each serialize 100 for all three maximums and all three starting currents. Production resume restores saved values rather than reapplying the authored fresh-start values. Files: `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:22`, `:78`, `:88`, `:120`, `:331`; `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:30`, `:94`; `Assets/_Project/Scenes/Wakeup.unity:6659`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:5906`; `Assets/_Project/Scenes/Demos/Sec0Patrol.unity:8336`; `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity:5334`.
- `PlayerMovement.SetInput` passes the movement vector to `MovementBehavior`. The prefab and behavior default to base speed 5 world units per second. The physics step multiplies it by normalized input and the smallest active speed restriction, with zero effective movement when blocked. Files: `Assets/_Project/Features/Player/Movement/PlayerMovement.cs:11`; `Assets/_Project/Features/Creatures/Movement.cs:16`, `:23`, `:54`, `:68`; `Assets/_Project/Features/Player/PF_Player.prefab:188`.
- Carry limits are container slots with item stack limits. The player prefab's inventory and hotbar capacities are 30 and 9 respectively. The three demos override the inventory to 24 slots. Scene binding registers each owner using its capacity and separately handles migration to carried equipment. Files: `Assets/_Project/Features/Player/PF_Player.prefab:48`, `:106`; `Assets/_Project/Features/Player/Inventory/PlayerInventory.cs:10`; `Assets/_Project/Core/Composition/SceneBindings.cs:159`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:15304`; `Assets/_Project/Scenes/Demos/Sec0Patrol.unity:20614`; `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity:18638`. Weight or encumbrance model not found within `Assets/_Project/**/*.cs` for `max.?weight|carry.?capacity|weight.?capacity|encumbrance`.
- Weapon damage comes from the action configuration on the currently held item. The Maintenance Wrench has `BaseDamage: 10`. The resolver creates a damage context from that value for an eligible contact. Succession reuses the same scene avatar and awakens it at the same authored point, so these fields, container capacities, and item tuning are shared by consecutive survivors in that scene. Files: `Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs:45`; `Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset:15`; `Assets/_Project/Systems/Combat/MeleeHitResolver.cs:145`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:159`.

```csharp
body.MovePosition(body.position + effective * moveSpeed * Time.fixedDeltaTime);
```

### How are crew backgrounds and proficiency ranks represented, and which diagnostic information and repair actions currently depend on them?

Not found within first-party C# under `Assets/_Project` for `proficiency|colonist|crewbackground|crew background|backgroundid|backgroundtype`. The survivor snapshot fields describe identity, life status, scene and position, vitals, and recovery timing. Scrubber access checks for an available screen, installation checks the required item and inventory, and restart checks fuse state. The generic junction checks its configured item and system, and the terminal reports its configured message when powered. None of those call paths reads survivor identity or proficiency. Files: `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs:8`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberAccess.cs:14`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:19`, `:32`; `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:24`; `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:20`; `Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:25`.

```csharp
public string id;
public SurvivorLifeStatus status = SurvivorLifeStatus.Active;
public string scenePath;
```

### Which existing entry points let playtesters launch an authored encounter directly, and what setup or configuration does each accept?

- In the Editor, open `AtmosphericScrubberEncounter.unity`, `ShipSystemRestoration.unity`, or `Sec0Patrol.unity` directly and enter Play. The scene supplies the authored player, equipment, and room entities. A disposable marker switches the composition root to in-memory storage. Scrubber has a scene-specific item catalog/runtime config. The other two scenes reference the shared config. Runtime config supplies item catalog, starting loadout, loot tables, equipment flow, and world seed. Files: `Assets/_Project/Features/World/Restoration/Demo/README.md:3`; `Assets/_Project/Features/Npcs/Sec0/README.md:3`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:1988`, `:10055`; `Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberRuntimeConfig.asset:15`; `Assets/_Project/Scenes/Demos/Sec0Patrol.unity:3797`, `:13567`; `Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity:2204`, `:15254`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:59`, `:70`.
- PlayMode tests load a specific scene asset path in `LoadSceneMode.Single` through `EditorSceneManager.LoadSceneAsyncInPlayMode`. Where tests need a production save sentinel or production resume, the existing verification runner passes an isolated `-solitudeSaveDirectory`. The authored Editor menus for the three demos build scene assets and reject an existing target scene. Build Settings enable only Wakeup. The public runtime `TransitionTo(string scenePath)` requires a ready runtime and a streamable destination, flushes the outgoing save, and loads the single scene. The root rejects mixed demo and production player scenes. Files: `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:51`; `Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs:18`; `Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs:43`; `Tools/Verification/verify.py:23`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:269`; `Assets/_Project/Editor/RestorationDemoBuilder.cs:23`; `Assets/_Project/Editor/Sec0NpcBuilder.cs:71`; `ProjectSettings/EditorBuildSettings.asset:7`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:184`, `:209`. Background or fault launch parameters not found within these entry points or first-party Editor/verification searches for `launch(er)?|command.line|executeMethod|Play Scene`.

```csharp
EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single))
```

### How does a launch assign the playable colonist and initialize the room's fault, and which parts of that initialization can vary independently while using the same room?

The scene supplies its avatar and awakening marker. Where survivor succession is composed, a disposable launch creates and activates a fresh survivor identity and calls `avatar.Awaken(awakeningPoint)`. A production launch without saved lifecycle also awakens a new survivor and starts an expedition. A production resume reuses the saved identity and vitals and resolves its position. On scene arrival, the avatar uses authored arrival instead of the prior scene's coordinates. Files: `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:16`, `:49`; `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:30`, `:94`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:1163`.

Each scrubber component constructs a fresh state independently of the survivor. The default booleans and timer establish the same blown-fuse/offline fault on every fresh instance. Scene object wiring, vitals, spawn, inventory capacity, and runtime catalog/loadout are distinct authored inputs. Scrubber composition injects command service, inventory, catalog, input policy, and task endpoints, with no entry argument for fault type or background. An individual instance can evolve independently through install, restart, and recovery. Files: `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:13`, `:22`; `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:8`, `:16`, `:23`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs:57`; `Assets/_Project/Core/Composition/SceneBindings.cs:159`, `:207`.

```csharp
first = new SurvivorState(); first.TryActivate(); avatar.Awaken(awakeningPoint);
private readonly ScrubberEncounterState state = new ScrubberEncounterState();
```

### Which state determines whether the scrubber is producing airflow, and how do electrical faults and intake damage affect it?

`Running` determines the displayed airflow state. A blown fuse prevents restart. Installing the fuse clears the fault and leaves the unit offline until restart. Airflow becomes active immediately on restart, and the room remains recovering until five seconds of accumulated scaled update time passes, making `Operational` true. The intake and distribution captions show the same airflow status. Separate intake damage, feed loss or a breaker state not found within first-party `Features`, `Domain`, and `Systems` C# for `breaker|linkage|\bairflow\b|service panel|power circuit|circuit`. Files: `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:10`, `:16`, `:24`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:42`; `Assets/_Project/Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:92`.

```csharp
public string AirflowStatus => Running ? "ACTIVE" : "STOPPED";
public bool Operational => Running && recoverySeconds >= 5;
```

### How do breakers, service panels, scrubbers, and corridor lights receive power, and what circuit relationships can an authored room represent?

The restoration demo uses a per-instance `RestorableSystem` boolean. Each junction points to one system and required item. A terminal and door gate can point to the same system. The demo's primary battery junction restores the exit gate and cyan terminal together. Another junction powers only its separately configured green terminal. A single producer state can therefore drive multiple consumers and separate system objects remain independent. Files: `Assets/_Project/Features/World/Restoration/RestorableSystem.cs:8`; `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:12`; `Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:31`; `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs:25`; `Assets/_Project/Editor/RestorationDemoBuilder.cs:41`; `Assets/_Project/Features/World/Restoration/Demo/README.md:5`.

The scrubber has a separate fuse/restart state and a local status renderer and audio. Its scene has authored global lighting. Breaker, service-panel feed switching, and corridor-light dependencies not found within first-party `Features`, `Domain`, and `Systems` C# plus `Scenes` and `Features` serialized scenes/prefabs/assets for `breaker|linkage|service.?panel|circuit`. Within these inspected implementations the authored power relation is a referenced binary state; there is no model of upstream/downstream feeds or a circuit graph. Files: `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:8`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:22`; `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:2051`.

```csharp
powered = system != null && subscribed != null && subscribed.IsRestored;
```

### How do repair interactions check for a replacement part or salvaged hardware, update inventory, and apply the repair?

Both the atmospheric fuse and the generic junction call `RestorableSystemState.TryRestore` with a single item identity. The scrubber's replacement fuse comes from its authored catalog/pickup. The junction takes a serialized `ItemDefinition` and the demo supplies the battery definition. The command validates the live registered player inventory, rejects busy and already-restored operations, finds a matching stack, then removes one from the slot address with a callback that marks the system restored before inventory notifications. The restore event follows a successful command. Empty, wrong-container, busy, or stale cases do not perform a repair. Salvage-specific checks or multiple repair material choices not found in `Assets/_Project/Features/World`, `Features/Items`, and `Domain/Restoration` for `salvage|hardware|repair`; the paths take one exact item ID. Files: `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:335`, `:346`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:13`, `:32`; `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:12`, `:25`; `Assets/_Project/Editor/RestorationDemoBuilder.cs:42`; `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17`.

```csharp
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
if (result.Status != CommandStatus.Applied) return RestorationOutcome.Rejected;
```

### How do diagnostics reveal a dead feed's breaker or a damaged linkage's replacement part, and which clues remain visible to a player without those diagnostics?

The scrubber screen exposes operating state, fault text, fuse status, airflow, CO2, and the current task on ordinary access. Inventory availability controls the install button and its reason text, which names one replacement fuse when absent. The world shows scrubber status and indicator color plus a CO2 warning and alert. The journal can reveal the fuse instruction on focus or successful access and tracks repair stages. The restoration demo junction names its exact required part in the normal focus prompt. An offline terminal and door say to restore power, and the opening reference socket reports a battery hint on interaction. Dead-feed breaker and damaged-linkage diagnostics not found within `Assets/_Project/Features`, `Domain`, `Systems` C# and `Scenes` serialized source for `breaker|linkage|service.?panel|diagnos`. Files: `Assets/_Project/Features/World/ScrubberEncounter/ScrubberAccess.cs:14`; `Assets/_Project/Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:88`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:20`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:22`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs:45`, `:61`; `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:21`; `Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:25`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:21`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:14`.

```csharp
system.IsRestored ? "System restored" : "Offline — install 1 " + Requirement;
```

### How do tool interactions show available actions and costs before the player commits, and what visual and audio feedback can they provide during and after the action?

For machinery, the generic junction focus prompt states the quantity and item before interaction. The scrubber shows install and restart controls, highlights the focused enabled button in amber, disables install with a live inventory reason, disables restart once running, and reports the result after activation. The actual item mutation rechecks availability. The encounter turns its status indicator from red to green and starts the running sound on restart, updates the CO2 warning through recovery, then stops the alert and hides the warning when operational. The task source publishes completion. Files: `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:22`; `Assets/_Project/Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:57`, `:88`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:19`, `:32`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:22`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs:61`.

The inventory separately exposes equip/unequip and in-hand tool state. The tool slot accepts tagged items with a primary or contextual action. Item action requests resolve the held item's action slot and validate the actor and gameplay state. The Maintenance Wrench uses a melee action with swing presentation, optional swing audio, recipient hit feedback and impact effect/audio, and feedback cleanup when the action ends. No equipment-tool repair action or repair cost preview beyond the machinery affordances was found within the repair handlers above and first-party `Features`, `Systems`, `Application`, `Domain` for `ToolAction|SelectedTool|ItemActionSlot.Contextual|PlayerEquipmentSlot.Tool`. Files: `Assets/_Project/Systems/Containers/Views/InventoryScreenView.cs:481`; `Assets/_Project/Features/Items/Core/CarriedEquipment.cs:34`; `Assets/_Project/Features/Player/Combat/PlayerItemActionComposition.cs:45`; `Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset:17`; `Assets/_Project/Systems/Combat/MeleeImpactFeedback.cs:24`, `:40`, `:72`.

```csharp
if(install!=null) install.interactable=equipment.InstallReason.Length==0;
if(installReason!=null) installReason.text=equipment.InstallReason;
```

### How are SEC-0 patrol routes authored and activated in a maintenance room?

The SEC-0 prefab contains a moving rover and four separate waypoint transforms under the stationary prefab root. The controller serializes waypoint order, speeds and pauses, validates at least two same-scene waypoints and valid tuning, and advances cyclically after its dwell time at a waypoint. `Sec0Composition` explicitly binds the scene's bots, the local player's stun receiver and vitals, and the gameplay policy when that composition is in `SceneBindings.features`. An unbound bot stays inert. Gameplay and player availability gate updates; scene release unbinds both. The dedicated patrol scene demonstrates this wiring. No SEC-0 feature authored in `AtmosphericScrubberEncounter.unity` was found by inspecting its named objects and the scrubber builder; the observed SEC-0 wiring is in the patrol demo. Files: `Assets/_Project/Features/Npcs/Sec0/PF_Sec0.prefab:127`, `:250`; `Assets/_Project/Features/Npcs/Sec0/README.md:33`, `:64`; `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Controller.cs:62`, `:73`, `:144`, `:178`; `Assets/_Project/Features/Npcs/Sec0/Runtime/Sec0Composition.cs:14`, `:26`; `Assets/_Project/Editor/Sec0NpcBuilder.cs:82`; `Assets/_Project/Editor/ScrubberEncounterBuilder.cs:296`.

```csharp
bot.Bind(this,player.transform,player,context.Policy,Available);
```

### How are doors triggered to close on player entry and reopen when a system such as airflow recovers?

The restoration door starts closed and colliding. Its `RestorationDoorGate` subscribes to the configured `RestorableSystemState.Changed` and updates permission to interact. After restoration the player separately interacts to open it, then the nine-frame animation disables its blocker. The door has no post-open closing behavior. Entry triggers in the inspected door/world components expose prompts, first-contact text, or completion signals. The older generic `Door` has an interaction branch that closes an already-open door. Entry-driven closing or scrubber-driven reopening not found within first-party C# under `Features/World`, `Features/Interactables/Doors`, and nearby interaction systems for `OnTriggerEnter2D|OnTriggerExit2D|CloseDoor|SetOpen|airflow`. Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:16`, `:21`, `:31`; `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs:14`, `:25`; `Assets/_Project/Features/Interactables/Doors/Door.cs:21`; `Assets/_Project/Features/Interactables/Doors/Wakeup/InteractionZone.cs:17`; `Assets/_Project/Features/World/SolitudeStart/SolitudeStartCompletion.cs:17`; `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:21`.

```csharp
private void Reconcile() => restored = system != null && subscribed != null && subscribed.IsRestored;
```

### How does the game track a reusable item installed in an interactable, and what happens to the item and its effect if the action is interrupted, the player leaves and returns, or normal power is restored?

The junction and scrubber consume one item and store an installed/restored boolean, with no retained item instance or extraction API. Installation is synchronous. Closing the screen before clicking leaves the inventory and state alone, a stale or rejected command does not set the flag, and closing after a successful click retains both facts. Walking away and returning in the same scene preserves the state. Scrubber recovery continues with its screen open or away from the machine and stops ticking under actual pause. Unloading and loading a fresh room recreates the state. Files: `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:12`, `:17`; `Assets/_Project/Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:104`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:47`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:179`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:115`.

The earlier opening socket is the observed non-consuming example. It checks for a battery in inventory, requests door opening, and sets a local `Powered` flag. It does not reserve the battery or observe its continued presence. A reusable installed-item owner, interruption rollback of a completed installation, and normal/mains power handoff not found within `Features/World` and `Domain/Restoration` C# for `installed|\bPowered\b|battery|portable power|circuit`. The ship save record has installed/running/recovery fields, with no installed item ID. Files: `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:11`; `Assets/_Project/Domain/Expeditions/ExpeditionState.cs:94`.

```csharp
public bool IsRestored { get; private set; }
public bool Powered { get; private set; }
```

### Which character, inventory, and room states reset or persist when starting another launch with the same or a different background and fault?

Each disposable player-scene binding resets the container/save session and task journal, disables production lifecycle persistence, and rebinds scene-owned endpoints. A composed survivor awakens with a new identity and authored vitals. Inventory ownership and starting loadout reinitialize, pickups and locker contents reset, and scene-owned repair, recovery, door, and NPC states start over. A fresh scrubber has its replacement fuse pickup, alert and warning active, zero recovery and stopped airflow. Same-scene proximity changes do not reload this state. Direct demo startup selects an in-memory backend and cannot mix with a production player scene. Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:70`, `:92`, `:184`; `Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs:9`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:58`; `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs:286`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:115`; `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:122`; `Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs:37`.

Production startup resumes saved colonist, expedition, location, vitals and carried containers, while `Start New Save` resets the save and loads the cryobay. The save schema includes ship repair/door records, but live scrubber state is marked scene-owned and its composition has no save adapter. Uses of `repairs|RepairSaveRecord|DoorSaveRecord` in first-party C# under `Assets/_Project` were the schema, codec, and tests. Background and fault selectors not found in the launch or initialization paths above, so no same/different-selector persistence branches were found. Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:76`, `:223`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:79`; `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs:169`; `Assets/_Project/Domain/Expeditions/ExpeditionState.cs:42`, `:94`; `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs:66`; `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:5`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs:57`.

```csharp
session.Reset(config.worldSeed, releaseOwners: true);
entry.ConfigureLifecyclePersistence(!disposableSession);
```

### Which existing focused tests or playtest setups exercise direct encounter launches, character initialization, repair choices, or room responses to system changes, and how do they establish a repeatable starting state and observe results?

- `ScrubberEncounterSliceTests` loads the actual scene by asset path in single mode. Tests act through the composed input, pickup, inventory, and screen, checking full-inventory pickup rejection, requirement rechecks, successful install while stopped, restart/recovery, modal/proximity/actual pause behavior, notification delivery, and that inspection leaves vitals and saves alone. `ScrubberEncounterLifecycleTests` repeatedly loads the scene at successive interaction stages, verifies disposable mode, baseline equipment/pickup/inventory/warning/audio/task state, and compares production sentinel bytes. Files: `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:51`, `:118`, `:155`, `:179`, `:201`, `:221`; `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:51`, `:115`.
- `ScrubberEncounterTests` constructs state and test containers to check the default fault, wrong and non-inventory items, consumed quantity, reentry, stale or busy/unavailable operations, and explicit five-second time advancement. `RestorationTests` tests corresponding generic transactions, observer behavior, independent systems and pickup-based repair. `RestorationJunctionTests` checks diagnosis text, installation, focused prompt refresh, and binding release. Files: `Assets/_Project/Tests/Domain/ScrubberEncounterTests.cs:12`, `:23`, `:33`, `:41`, `:55`; `Assets/_Project/Tests/Domain/RestorationTests.cs:22`, `:36`, `:74`; `Assets/_Project/Tests/Editor/RestorationJunctionTests.cs:29`, `:48`, `:82`.
- `RestorationSliceTests` loads the authored demo and drives scene targeting, battery pickup and transfer, junction installation, gated passage, terminal use and reload. It checks independent terminal/system state and verifies that a reload creates unpowered systems and a closed door. `RestorationDoorTests` uses an authored-in-test system, gate, door, collider, and cell inventory to observe blocked interaction before repair, permitted interaction afterward, and blocker state after animation. `RestorationDemoLifecycleTests` checks disposable startup and reload against isolated normal-save bytes. Files: `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:34`, `:75`; `Assets/_Project/Tests/PlayMode/RestorationDoorTests.cs:37`, `:43`, `:62`; `Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs:43`.
- `RunLifecycleResumeTests` primes or clears an isolated save, directly loads scenes, and checks fresh vs. saved survivor identity, expedition, exact vitals, restore position, equipment, and fresh disposable awakening. `Sec0EncounterTests` loads the saved scene, observes movement on patrol and stun with unchanged Health, then reloads and verifies transient stun and shot count are cleared. The verification runner supplies the isolated save directory, and the manual guides describe direct Editor play and replay. Files: `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs:46`, `:91`, `:169`, `:286`; `Assets/_Project/Tests/PlayMode/Sec0EncounterTests.cs:18`; `Tools/Verification/verify.py:23`; `Assets/_Project/Features/World/Restoration/Demo/README.md:3`; `Assets/_Project/Features/Npcs/Sec0/README.md:3`. These assertions were inspected without running the tests.

```csharp
Assert.IsFalse(fresh.State.FuseInstalled);
Assert.IsFalse(fresh.State.Running);
Assert.AreEqual(0,fresh.State.RecoverySeconds);
```

## New Questions
