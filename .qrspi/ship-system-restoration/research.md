# QRSPI Research: ship-system-restoration

## Answers

Paths are repository-relative. Source inspection only; tests and scenes were not executed. The human-authorized exception for inherited ambient approval records was used; those records were not research evidence.

### How do existing interactions expose availability, prompts, inspection information, and action results to the player?

`IInteractable` exposes `Interact`, `GetPrompt`, and `CanInteract`, without a separate inspection API. The player checks availability and forwards results to feedback. Prompts update on focus-change events. Success messages appear green; other results appear red, defaulting to “Cannot interact.”

Files: `Assets/_Project/Systems/Interaction/Core/IInteractable.cs:9`; `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:80`; `Assets/_Project/Systems/Interaction/UI/Controllers/UIController.cs:35`; `Assets/_Project/Systems/Interaction/UI/Presenters/InteractionFeedback.cs:25`.

Snippet: `interactionPrompt.Show(evt.interactable.GetPrompt());` (`UIController.cs:44`).

### How do doors currently represent and enforce access restrictions, and how are those restrictions communicated?

General `Door` serializes `isLocked` but does not read it during interaction. Its closed branch returns “Door opened...” without opening; its open branch closes. `SolitudeSlidingDoor` blocks activation during/after opening and disables collision after its nine-frame animation. `OpeningPowerSocket` checks battery possession and reports a missing-battery message before invoking that door.

Files: `Assets/_Project/Features/Interactables/Doors/Door.cs:11`, `:21`, `:37`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:15`, `:30`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:18`.

Snippet: `public override bool CanInteract(PlayerInteractor player) => !opening && !open;` (`SolitudeSlidingDoor.cs:17`).

### Which existing gameplay components and scene assets participate in item acquisition, inventory transfer, door access, and environmental activation?

`WorldPickup` delegates collection to a service and hides collected objects. `PlayerInventory` exposes an injected container reader; `Locker` requests its transfer screen. `ContainerCommandService.Transfer` moves, merges, or swaps slots. The opening scene builder wires a battery pickup, locker, socket, sliding door, and first-contact trigger. The saved scene contains the corresponding door/socket/contact objects.

Files: `Assets/_Project/Features/Items/Core/WorldPickup.cs:46`; `Assets/_Project/Features/Player/Inventory/PlayerInventory.cs:8`; `Assets/_Project/Features/Interactables/Containers/Locker/Locker.cs:29`; `Assets/_Project/Domain/ContainerCommandService.cs:124`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:67`; `Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity:839`, `:1223`, `:11749`.

Snippet: `socket.door=door;socket.batteryItemId=battery.Definition.Item.ItemId;` (`OpeningSliceBuilder.cs:77`).

### What existing state or condition mechanisms already affect multiple scene objects, and where are their ownership boundaries?

`GameCompositionRoot` owns the inventory session, pickup service, pause/input policy, and scene scopes. Pause uses leases; input combines readiness, modal, transition, and pause state. Container commands own registered slot state. The opening socket owns local `Powered` state and a direct door reference; first contact reads socket and door state.

Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:24`, `:50`, `:69`; `Assets/_Project/Application/RuntimePolicy.cs:6`, `:30`; `Assets/_Project/Domain/ContainerCommandService.cs:17`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:11`; `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:14`.

Snippet: `public bool Gameplay => Ready && !modal && !pause.IsPaused;` (`RuntimePolicy.cs:35`).

### What initialization, enable/disable, scene unloading, and event subscription conventions constrain persistent scene state and its consumers?

The persistent root runs at execution order -2000, loads saves before binding scenes, subscribes to load/unload, and releases scene scopes on unload. Scope release disposes inventory, pickup, and UI bindings. UI subscribes/unsubscribes on enable/disable. Player disable clears focus. Sensors unregister on trigger exit/destruction, with no explicit disable handler. `SceneBindings` releases on destruction, not on disable.

Files: `Assets/_Project/Core/Composition/GameCompositionRoot.cs:15`, `:37`, `:108`, `:140`; `Assets/_Project/Core/Composition/SceneBindings.cs:199`; `Assets/_Project/Systems/Interaction/UI/Controllers/UIController.cs:25`; `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:65`; `Assets/_Project/Systems/Interaction/Core/InteractableProximitySensor.cs:39`.

Snippet: `SceneManager.sceneLoaded += Loaded; SceneManager.sceneUnloaded += Unloaded;` (`GameCompositionRoot.cs:55`).

### What guarantees do inventory operations provide for item identity, quantity checks, removal, failure reporting, and repeated requests?

Handles include identity and registration generation; slot addresses include revisions. `Remove` checks availability, revision, positive quantity, and sufficient contents before committing a cloned slot plan. Reusing a changed address returns `Stale`. `Grant` validates catalog membership, acceptance, quantity, and total capacity before mutation. Commit installs planned state before notifications, rejects reentrant commands, and catches observer errors. Removal is exposed on the concrete service, not its smaller transfer/grant interfaces.

Files: `Assets/_Project/Domain/IContainerReader.cs:6`, `:15`, `:42`, `:48`; `Assets/_Project/Domain/ContainerCommandService.cs:66`, `:80`, `:111`, `:228`.

Snippet: `if (stack == null || quantity > stack.Quantity) return new CommandResult(CommandStatus.InsufficientQuantity);` (`ContainerCommandService.cs:118`).

### How are interaction requests and resulting state changes dispatched, and can consumers obtain current state after missing an earlier notification?

Interaction uses a direct target call followed by static focus/trigger events. `Triggered` is published after any returned result when the initial availability check passed, including a failure result. The bus has no replay or current-state store. Containers expose fresh snapshots; socket, door, and input policy expose readable properties independently of events.

Files: `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:94`; `Assets/_Project/Systems/Interaction/Core/InteractionEventBus.cs:7`; `Assets/_Project/Domain/Container.cs:23`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:13`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:15`; `Assets/_Project/Application/RuntimePolicy.cs:34`.

Snippet: `OnTriggered?.Invoke(evt);` (`InteractionEventBus.cs:17`).

### How do current lights, doors, terminals, and other environmental objects expose their operational or visual state?

The animated door exposes `IsOpen` and controls sprite/blocker state. Socket exposes `Powered`; first contact exposes `Heard` and emits timed feedback. Locker presentation polls modal openness. The builder creates an intercom prop and sets global `Light2D` intensity/color during authoring.

Runtime terminal/light controllers: **Not found** by search `Light2D|Terminal|terminal|event |ISaveable` in `Assets/_Project/Features` and `Assets/_Project/Core/SaveLoad`, excluding Editor and all `.qrspi` trees.

Files: `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:15`, `:35`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:13`; `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:11`; `Assets/_Project/Features/World/OpeningSlice/OpeningLockerVisual.cs:11`; `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:79`, `:86`.

Snippet: `InteractionFeedback.Handle(InteractionResult.Success(fragment));` (`OpeningFirstContact.cs:22`).

### What existing interfaces present item requirements, missing requirements, and successful interaction feedback?

Requirements use prompt/result strings. Socket prompts connection, reports missing battery through `Blocked`, and returns success text. `InteractionResult` contains type/message only. Pickup feedback distinguishes full inventory from unavailable pickup.

Files: `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:14`, `:20`, `:24`; `Assets/_Project/Systems/Interaction/Core/InteractionResult.cs:11`; `Assets/_Project/Features/Items/Core/WorldPickup.cs:56`.

Snippet: `public readonly string Message;` (`InteractionResult.cs:14`).

### How do existing interactions handle repeated activation, unavailable items, disabled targets, and interrupted or failed operations?

Socket availability becomes false after activation, but `Interact` does not recheck `Powered`. It checks possession without removing the item, ignores the returned door result, and sets `Powered`. SlidingDoor rechecks availability; missing/invalid animation data prevents its update from finishing. Already-collected pickups cannot grant twice. Target selection skips destroyed components but does not check enabled/active status. The interaction interface has no cancellation operation.

Files: `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:15`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:20`, `:32`; `Assets/_Project/Domain/ContainerSaveSession.cs:43`; `Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:114`; `Assets/_Project/Systems/Interaction/Core/IInteractable.cs:7`.

Snippet: `var result = door.Interact(player);` followed by `Powered = true;` (`OpeningPowerSocket.cs:22–23`).

### How do current systems distinguish independent instances and restore initial behavior when scenes reload or objects reactivate?

`SaveableId` provides explicit editor GUID assignment and guarded runtime assignment. Container registrations get fresh generations; disposal captures state and unregisters. Pickup initialization reads saved collection facts. Socket/contact/door flags are ordinary instance fields without persistence hooks or enable-reset code; recreated instances start false. Door `Awake` initializes its sprite/blocker.

Files: `Assets/_Project/Core/SaveLoad/SaveableId.cs:22`; `Assets/_Project/Domain/IContainerReader.cs:10`; `Assets/_Project/Domain/ContainerSaveSession.cs:129`; `Assets/_Project/Features/Items/Core/WorldPickup.cs:20`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:13`; `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:11`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:14`, `:25`.

Snippet: `if (service.IsCollected(pickupId)) gameObject.SetActive(false);` (`WorldPickup.cs:37`).

### What automated tests or manual verification conventions cover interactions, inventory changes, doors, and event-driven scene behavior?

Domain NUnit tests cover removal validity, atomic grants, reentrancy, observer errors, stale addresses, and generations. Editor tests cover pickup persistence and rejected collection. PlayMode tests cover enable cycles, unloading, collection reload, and reset; setup requires an isolated save-directory argument. A player smoke runner checks input, UI, transfer, pickup, and persistence.

Door/socket/contact tests: **Not found** by `Door|Interact|OpeningPower|OpeningFirst` search in `Assets/_Project/Tests` and `Assets/_Project/Verification` C# files, excluding all `.qrspi` trees. No tests ran during research.

Files: `Assets/_Project/Tests/Domain/ContainerBoundaryTests.cs:41`, `:83`, `:92`, `:110`, `:131`, `:140`; `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs:53`, `:68`, `:111`; `Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:22`, `:39`, `:48`, `:60`; `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:87`, `:125`.

Snippet: `[UnityTest] public IEnumerator CollectionSurvivesActualPlayerSceneReloadAndNewSaveClearsIt()` (`ProductionLifecycleTests.cs:60`).

### What existing fixtures or playable scenes can verify requirement acquisition, state changes, and resulting access changes together?

The opening scene contains wired door/socket/contact objects, with a builder linking the battery pickup. Its `OpeningReviewSession` configures in-memory saves. Lifecycle tests load Wakeup and additive Context/Unrelated fixtures, covering collection and scope behavior rather than powered access. No scene was played during research.

Files: `Assets/_Project/Art/SOLITUDE/Editor/OpeningSliceBuilder.cs:21`, `:33`, `:70`, `:77`, `:80`, `:87`; `Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity:839`, `:11749`; `Assets/_Project/Features/World/OpeningSlice/OpeningReviewSession.cs:9`; `Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:31`, `:50`, `:54`.

Snippet: `new SaveRecoveryStore("opening-art-review", new MemoryFiles(), new SaveJsonCodec(database.BuildRuntimeCatalog()))` (`OpeningReviewSession.cs:14`).

### What item definitions, inventory ownership rules, and container or pickup behavior govern obtaining and spending a required item?

`ItemDefinition` holds display/stack settings and asset-GUID-derived identity. Scene bindings register separate fixed inventory/hotbar IDs. Socket reads only inventory. Locker loot rolls only when no saved container exists, seeded by world seed and instance identity. Pickups grant to fixed player inventory. Concrete `Remove` spends from one revision-addressed slot; the socket does not call it.

Files: `Assets/_Project/Features/Items/Core/ItemDefinition.cs:8`, `:19`, `:29`; `Assets/_Project/Domain/ContainerSaveSession.cs:10`; `Assets/_Project/Core/Composition/SceneBindings.cs:96`, `:126`; `Assets/_Project/Features/Items/Core/WorldPickup.cs:49`; `Assets/_Project/Domain/ContainerCommandService.cs:111`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:18`.

Snippet: `public const string PlayerInventoryId = "player.inventory";` (`ContainerSaveSession.cs:10`).

### What persistence facilities and stable object identifiers already exist for inventory, doors, and other world state?

Save version 2 stores world seed, containers, and collected pickup IDs. Session capture includes unloaded records. `SaveGameService` writes through recovery storage to `solitude-save.json` in `persistentDataPath` by default, flushing in LateUpdate and lifecycle callbacks. `SaveableId` identifies instances; item IDs identify definitions. Door/socket/contact state is absent from the inspected schema, and those components have no save interface. `LockerContainer` implements `ISaveable<ContainerSaveData>`.

Files: `Assets/_Project/Domain/SaveGameData.cs:9`; `Assets/_Project/Domain/ContainerSaveSession.cs:51`, `:78`; `Assets/_Project/Core/SaveLoad/SaveGameService.cs:18`, `:39`, `:47`; `Assets/_Project/Core/SaveLoad/SaveableId.cs:22`; `Assets/_Project/Features/Items/Core/ItemDefinition.cs:29`; `Assets/_Project/Features/Interactables/Containers/Locker/LockerContainer.cs:10`; `Assets/_Project/Features/Interactables/Doors/Door.cs:8`; `Assets/_Project/Features/World/SolitudeStart/SolitudeSlidingDoor.cs:7`; `Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:9`; `Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:8`.

Snippet: `public List<string> collectedPickupIds = new();` (`SaveGameData.cs:14`).

## New Questions
