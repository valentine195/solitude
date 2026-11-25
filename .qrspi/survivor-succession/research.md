# QRSPI Research: survivor-succession

## Answers

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`. Findings describe current source. Tests were inspected, not executed. All recursive searches excluded `.qrspi/` trees.

### How is the player character currently created, placed at an awakening location, and made controllable?

The player is an authored `PF_Player` prefab instance. Wakeup serializes position (-0.89, -2.4, 0); the Atmospheric Scrubber demo uses (23, 4.5, 0). The opening builder places an existing player at (4,3,0), while the older start builder instantiates the prefab in the Editor. Runtime composition discovers SceneBindings, initializes serialized player endpoints, binds input, and enables readiness. No runtime player factory was found searching first-party C# for `Instantiate(`, `respawn`, and `awak`.

Files: `Assets/_Project/Features/Player/PF_Player.prefab:107`, `Assets/_Project/Scenes/Wakeup.unity:17445`, `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:14503`, `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:61`, `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:109`, `Assets/_Project/Core/Composition/SceneBindings.cs:180`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:73`.

```csharp
input.Bind(movement, interactor, selection, () => modal.ToggleInventory(), Back, game.TogglePause, journalScreen);
// FinishStartup:
State = RuntimeStartupState.Ready; policy.SetReady(true);
```

### What character identity and character-owned state exist today, and where are they initialized, reset, or persisted?

Inventory and hotbar receive container readers under fixed save owner IDs. Their prefab children have capacities 30 and 9; Wakeup overrides inventory capacity to 24. Vitals Awake creates Health/Oxygen using authored maxima/current values; Wakeup and scrubber scene current values are 100. Hotbar selection is created per scene initialization. Container contents persist through the save session and owner disposal. Start New Save clears the session and reloads the scene. Save version 2 contains world seed, container records and collected pickup IDs, without character/vital/position records. No survivor identity model was found in first-party C# searches for `survivor|colonist|character.?id|traits|injur`.

Files: `Assets/_Project/Domain/ContainerSaveSession.cs:10`, `Assets/_Project/Features/Player/PF_Player.prefab:48`, `Assets/_Project/Features/Player/PF_Player.prefab:106`, `Assets/_Project/Scenes/Wakeup.unity:17489`, `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:36`, `Assets/_Project/Scenes/Wakeup.unity:6097`, `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:5533`, `Assets/_Project/Core/Composition/SceneBindings.cs:135`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:156`, `Assets/_Project/Domain/SaveGameData.cs:9`.

```csharp
public const string PlayerInventoryId = "player.inventory";
public const string PlayerHotbarId = "player.hotbar";
Vitals = new PlayerVitals(maxHealth, maxOxygen);
Vitals.Health.Set(currentHealth);
Vitals.Oxygen.Set(currentOxygen);
```

### What death or incapacitation behavior currently exists, including debug triggers and its effects on gameplay?

Vital.Set emits Changed and emits Depleted once upon reaching zero; positive recovery rearms depletion. No death/incapacitation consumer, Depleted subscription or debug kill action was found within `Assets/_Project` C# using `death|incapac|respawn|\.Depleted\s*[+]|Kill`, or in inspected UnityInputHost callbacks. Vitals controller initializes/validates values only. The inspected depletion path does not disable movement, replace the player, drop items or reload scenes. Start New Save is an existing context action that resets and reloads.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:22`, `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27`, `Assets/_Project/Core/Input/UnityInputHost.cs:50`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:156`.

```csharp
if (Current <= 0f && !_wasDepleted) {
    _wasDepleted = true; Depleted?.Invoke(this);
} else if (Current > 0f) {
    _wasDepleted = false;
}
```

### Which repair outcomes, system activations, door unlocks, and other permanent ship modifications currently remain intact when the player character changes?

No runtime character-change operation was found, so its retention behavior is unimplemented. Existing modifications belong to scene objects: restoration state, scrubber fuse/running/recovery, sliding-door opening/open/collision, socket Powered, and first-contact Heard. Scrubber ReleaseBinding preserves equipment state, but recovery advances only while its inventory binding is available. Reload destroys scene instances; tests expect repair, terminals, doors and scrubber progress to reset. Production container records and collected pickup facts survive scene reload. Repair/door flags are absent from the save schema.

Files: `Assets/_Project/Features/World/Restoration/RestorableSystem.cs:8`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:14`, `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:18`, `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:13`, `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:20`, `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:127`, `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:114`, `Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:84`, `Assets/_Project/Domain/SaveGameData.cs:11`.

```csharp
private readonly ScrubberEncounterState state = new ScrubberEncounterState();
private void Update() { if(Available) state.Advance(Time.deltaTime); }
// ReleaseBinding:
state.Changed-=Publish; commands=null; inventory=null;
```

### Which gameplay systems retain references to the player character, and how are those references established or refreshed when the character changes?

SceneBindings retains serialized player endpoints. UnityInputHost.Bind replaces movement/interactor/selection and neutralizes old input. SceneInteractionScope retains and binds one interactor. Scrubber equipment and restoration junctions retain inventory readers; scrubber task sources retain the interactor. Modals/presenters retain reader/source endpoints. Camera follow retains a Transform with Configure; VitalBarView resolves the singleton controller on OnEnable and retains its Vital. Sensors retain players from trigger enter/exit. Opening socket looks up inventory from each interacting actor. Standalone SceneInteractionBindings resolves one scene interactor. These owners refresh through explicit Initialize/Bind/Configure or scene lifecycle; no character-replacement refresh was found.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:21`, `Assets/_Project/Core/Input/UnityInputHost.cs:61`, `Assets/_Project/Systems/Interaction/Core/SceneInteractionScope.cs:32`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberComposition.cs:66`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs:31`, `Assets/_Project/Features/World/Restoration/RestorationJunction.cs:17`, `Assets/_Project/Core/Composition/SceneBindings.cs:141`, `Assets/_Project/Features/World/SolitudeStart/SolitudeCameraFollow2D.cs:9`, `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17`, `Assets/_Project/Systems/Interaction/Core/InteractableProximitySensor.cs:19`, `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:18`, `Assets/_Project/Systems/Interaction/Core/SceneInteractionBindings.cs:22`.

```csharp
if (this.movement != null) this.movement.SetInput(Vector2.zero);
if (this.interactor != null) this.interactor.SetInputAllowed(false);
this.movement = movement; this.interactor = interactor; this.selection = selection;
```

### Where are character-owned state and persistent ship-owned state currently separated or shared, particularly for the Air Scrubber and powered doors?

Player inventory/hotbar and lockers share save-session container storage under different IDs. Scrubber, restoration and door state is scene-local outside that schema. Repair atomically consumes an inventory item and commits restoration. Repair and scrubber availability require the fixed player inventory owner and matching live reader. Door gates/terminals observe restoration state without retaining the player. OpeningPowerSocket is a separate implementation: it checks a battery in the actor's inventory and sets Powered without consuming the battery.

Files: `Assets/_Project/Domain/SaveGameData.cs:13`, `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:19`, `Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs:25`, `Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:32`, `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:16`.

```csharp
inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId ||
!ReferenceEquals(commands.Read(inventory.Handle), inventory)
// Repair commit:
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
```

### Which scene-transition and save/load assumptions depend on a single character identity, character instance, or inventory owner and would constrain later persistence of succession and recoverable equipment?

Composition allows one local player scene; interaction validation requires exactly one scene-local interactor. Player owner IDs are fixed; duplicate registration throws. Disposal captures contents under the owner ID, and registration restores those contents. WorldPickup addresses the fixed player inventory irrespective of its actor argument. Save data contains containers and authored pickup collection IDs, without survivor/world-drop positions. TransitionTo loads Single-mode and closes screens. Demo and production sessions cannot mix; demo player-scene binding resets owners.

Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:125`, `Assets/_Project/Systems/Interaction/Core/SceneInteractionScope.cs:23`, `Assets/_Project/Core/Composition/SceneBindings.cs:131`, `Assets/_Project/Domain/ContainerSaveSession.cs:60`, `Assets/_Project/Domain/ContainerSaveSession.cs:129`, `Assets/_Project/Features/Items/Core/WorldPickup.cs:46`, `Assets/_Project/Domain/SaveGameData.cs:11`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:148`.

```csharp
if (entry.HasPlayer && interactive != null)
    throw new InvalidOperationException("Only one local player scene may be composed.");
if (bindings.ContainsKey(id))
    throw new InvalidOperationException("Container identity already registered: " + id);
bool existing = TryGetContainer(id, out var saved);
```

### What existing character data or ownership abstractions represent identity and could accommodate later traits, skills, injuries, or inactive colonists?

No character record, roster, traits or injury abstraction was found within first-party C# (`Assets/_Project`, patterns `survivor|colonist|character.?id|traits|injur`). Existing identities are container IDs/generation GUIDs, saveable scene/pickup IDs and MonoBehaviour instances. ContainerHandle identifies container lifetime. Vital describes scalar Max/Current/events. Container interfaces provide reads/transfers/grants, without character metadata. Source does not establish future support for the absent character fields.

Files: `Assets/_Project/Domain/IContainerReader.cs:6`, `Assets/_Project/Domain/Container.cs:15`, `Assets/_Project/Core/SaveLoad/SaveableId.cs:33`, `Assets/_Project/Features/Player/Vitals/Vital.cs:7`, `Assets/_Project/Domain/IContainerReader.cs:31`.

```csharp
public string Id { get; }
public Guid Generation { get; }
public bool Equals(ContainerHandle other) =>
    Id == other.Id && Generation == other.Generation;
```

### How are carried and equipped items owned, transferred, dropped at a world location, and recovered through interaction?

Inventory/hotbar are separate containers. Transfer moves, swaps or partially merges stacks using owner generations and slot revisions. Drag preview retains source ownership; Drop transfers to another slot. Inspected hotbar exposes selection without a separate equipment model. WorldPickup registers an authored immutable reward and atomically grants/records collection before deactivation. SceneBindings.RegisterPickup permits additional runtime registration. No death-location/corpse/world-drop API was found in first-party C# for `drop.*world|world.*drop|death|equipment`, beyond scrubber equipment and UI slot Drop.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:131`, `Assets/_Project/Domain/ContainerCommandService.cs:125`, `Assets/_Project/Domain/ContainerInteractionController.cs:14`, `Assets/_Project/Features/Hotbar/Hotbar.cs:14`, `Assets/_Project/Features/Items/Core/WorldPickup.cs:20`, `Assets/_Project/Domain/ContainerSaveSession.cs:41`, `Assets/_Project/Core/Composition/SceneBindings.cs:186`.

```csharp
var result = collection.TryCollect(pickupId,
    ContainerSaveSession.PlayerInventoryId, reward.ItemId, reward.Quantity);
if (result == CollectResult.Collected || result == CollectResult.AlreadyCollected)
    gameObject.SetActive(false);
```

### How is the cryobay awakening location represented, and what existing behavior determines whether a replacement character can be created there automatically after death?

Wakeup's Cryopod contains Transform and SpriteRenderer only. OpeningSliceBuilder authors cryopod props and independently positions the player at (4,3,0). Other inspected scenes have different positions. No runtime awakening/spawn marker or replacement behavior was found in first-party C# searches for `Cryo|cryo|awak|respawn|Instantiate(`. Existing cryopod authoring supplies no automatic replacement decision.

Files: `Assets/_Project/Scenes/Wakeup.unity:529`, `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:60`, `Assets/_Project/Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs:109`, `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity:14503`.

```csharp
Prop("Cryopod 01","cryopod",3,5.5f,true);
Prop("Cryopod 02","cryopod",5,5.5f,true);
var player=All<PlayerInteractor>().Single().gameObject;
player.transform.position=new Vector3(4,3,0);
```

### How does the ship AI retain state and present acknowledgments or other feedback, and what existing event mechanisms connect it to gameplay transitions?

AI-like content inspected consists of OpeningFirstContact's instance-local Heard and three-message coroutine, and RestorationTerminal's authored powered message. First contact requires a player collider, socket power and an open door; it sets Heard before the coroutine and publishes injected scene feedback. Release stops the coroutine. Terminal observes restoration Changed and returns an InteractionResult. SceneInteractionEvents offers FocusChanged/Triggered; PlayerInteractor publishes them, and feedback service renders results. No persistent AI state or death/succession event was found within first-party C# searches for `AI|Acknowledge|death|survivor`.

Files: `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:13`, `Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:14`, `Assets/_Project/Systems/Interaction/Core/SceneInteractionEvents.cs:9`, `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:101`, `Assets/_Project/Systems/Interaction/UI/Presenters/InteractionFeedbackService.cs:15`.

```csharp
Heard = true;
contact = StartCoroutine(Contact());
// Contact:
feedback?.Handle(InteractionResult.Success(fragment));
```

### What initialization behavior could give a newly created survivor items despite an empty-inventory baseline?

PlayerInventory initialization grants no items. Player owner registration supplies no initialLoot, and fresh ContainerState slots are empty. Existing contents under fixed owner IDs are restored; disposal retains them for registration. Locker loot is rolled lazily into locker ownership. World pickups grant on interaction. Demo rebinding resets the session, while production rebinding restores it.

Files: `Assets/_Project/Features/Player/Inventory/PlayerInventory.cs:11`, `Assets/_Project/Core/Composition/SceneBindings.cs:131`, `Assets/_Project/Domain/Container.cs:15`, `Assets/_Project/Domain/ContainerCommandService.cs:40`, `Assets/_Project/Domain/ContainerSaveSession.cs:60`, `Assets/_Project/Domain/ContainerSaveSession.cs:133`, `Assets/_Project/Core/Composition/SceneBindings.cs:196`, `Assets/_Project/Features/Items/Core/WorldPickup.cs:46`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:129`.

```csharp
public void Initialize(IContainerReader reader) => Container = reader;
// Owner disposal:
session.Store(Id, ContainerSaveSerializer.Capture(Container));
```

### What happens when the player character disappears during an interaction, a death trigger repeats, or replacement character creation cannot complete?

No death/replacement flow exists within the bounded first-party C# searches above, so repeated death and creation failure have no implemented handling. Vital depletion is edge-triggered until positive recovery. Interactor disable clears focus; destruction releases events/feedback. Input callbacks and camera check missing Unity endpoints. SceneBindings teardown follows its own destruction/scene release, rather than player destruction. Scrubber task discovery requires its stored active player; vital views retain their old Vital until re-enabled. Container unregister invalidates handles and cancels drag through CapacityChanged; command publication blocks reentrant registration/unregistration/capture. Composition initialization failures unwind bindings and disable gameplay through Failed/readiness/pause.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:30`, `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:27`, `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:79`, `Assets/_Project/Core/Input/UnityInputHost.cs:84`, `Assets/_Project/Features/World/SolitudeStart/SolitudeCameraFollow2D.cs:23`, `Assets/_Project/Core/Composition/SceneBindings.cs:309`, `Assets/_Project/Features/World/ScrubberEncounter/ScrubberTaskSource.cs:45`, `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17`, `Assets/_Project/Domain/ContainerCommandService.cs:58`, `Assets/_Project/Domain/ContainerInteractionController.cs:20`, `Assets/_Project/Domain/ContainerSaveSession.cs:78`, `Assets/_Project/Core/Composition/SceneBindings.cs:182`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:87`.

```csharp
private void Interact(InputAction.CallbackContext _) {
    if (GameplayAllowed && interactor != null) interactor.TryInteract();
}
if (IsBusy) throw new InvalidOperationException("Command publication in progress.");
```

### What existing tests, debug tools, and verification conventions can demonstrate distinct survivor identities, automatic replacement with an empty inventory, continued control and interaction, retained ship modifications, recoverable equipment at the death location, and AI acknowledgment during runtime succession?

No succession-specific test was found in `Assets/_Project/Tests` using `survivor|colonist|death|respawn|character.?id`. Existing neighboring coverage includes production transition/rebinding and pickup retention; interaction/feedback and first-contact teardown; restoration inventory consumption, independent systems, movement/door passage and terminal feedback; scrubber recovery/reset lifecycle; transfer conservation, stale owners, duplicate IDs and pickup reentrancy. These were read, not run. Start New Save is the existing context action. Verification uses an isolated `/tmp/solitude-*` source copy, authoring validation, EditMode/PlayMode, shipping/verification builds and player restart/save-recovery scenarios. `-solitudeSaveDirectory` isolates persistence.

Files: `Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:42`, `Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:84`, `Assets/_Project/Tests/PlayMode/InteractionScopeLifecycleTests.cs:62`, `Assets/_Project/Tests/PlayMode/InteractionScopeLifecycleTests.cs:127`, `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:75`, `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:181`, `Assets/_Project/Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:114`, `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs:219`, `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs:275`, `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs:82`, `Assets/_Project/Core/Composition/GameCompositionRoot.cs:156`, `Tools/Verification/verify.py:5`, `Tools/Verification/verify.py:53`, `Assets/_Project/Core/SaveLoad/SaveGameService.cs:23`, `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:127`.

```csharp
Assert.Greater(player.transform.position.y, door.transform.position.y + 1.2f,
    "Player traverses doorway using real movement/physics");
yield return Focus(terminal); player.TryInteract();
StringAssert.Contains("AI fragment", Feedback);
```

## New Questions
