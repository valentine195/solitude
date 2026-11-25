# QRSPI Research: atmospheric-scrubber-encounter

## Answers

### How are room CO₂ levels represented today, and how do telemetry, warning lights, and sound expose elevated CO₂ and changes to it?
Not found within authorized first-party runtime code and serialized assets. Searches: `rg -n -i 'co2|scrubber|fuse|atmospher|restart|telemetry' Assets/_Project` restricted to `.cs`, `.unity`, `.prefab`, `.asset`, excluding Generated, Imported, .qrspi, and .agents; a second `.cs` search of Features, Domain, Core, Tests for `co₂|co2|scrubber|atmospher|telemetry|fuse|task|quest|oxygen|audio`. No atmospheric matches occurred. Restart matches were save-verification names; diffuse matches were font materials. These searches establish no CO₂ model, atmospheric telemetry, warning-light connection, or atmospheric sound behavior in that scope.

The existing oxygen data is a player vital initialized from serialized scalar values, without room input in this controller.
Files: Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:19, Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:36
```csharp
[SerializeField] private float maxOxygen = 100f;
Vitals = new PlayerVitals(maxHealth, maxOxygen);
Vitals.Oxygen.Set(currentOxygen);
```

### What behavior currently exists for atmospheric scrubber faults, fuse repair, operating controls, and restart outcomes?
Scrubber, fuse, and atmospheric restart behavior were not found in the bounded searches above. Comparable restoration is a single Boolean scene-owned state. Installation scans the bound player inventory, consumes one matching item through a command, and commits restored state before notifications. Outcomes distinguish restored, already restored, missing item, unavailable, rejected, and busy. There is no timed repair or restart in this method.
Files: Assets/_Project/Domain/Restoration/RestorableSystemState.cs:7, :17, :29; Assets/_Project/Features/World/Restoration/RestorationJunction.cs:22
```csharp
public enum RestorationOutcome { Restored, AlreadyRestored, MissingItem, Unavailable, Rejected, Busy }
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
```
The junction presents “Offline — install 1” plus the item name, “System restored,” “Requires 1 ... in inventory,” or “Installation unavailable; no item installed.”
Files: Assets/_Project/Features/World/Restoration/RestorationJunction.cs:22, :30
```csharp
case RestorationOutcome.MissingItem: return InteractionResult.Blocked("Requires 1 " + Requirement + " in inventory");
```

### Which existing room and equipment content supports an atmospheric scrubber encounter, and what other equipment or rooms share its atmospheric behavior?
No atmospheric equipment/room behavior was found in the stated code/asset searches. Current authored restoration content is auxiliary power and an independent service circuit in `Scenes/Demos/ShipSystemRestoration.unity`. Both junctions require the Battery asset, but use distinct system references. The main circuit supplies a door gate and a terminal; the service terminal has its own circuit. This is power restoration, with no inspected atmospheric connection.
Files: Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity:1013, :5220, :12261, :12373, :13079; Assets/_Project/Features/Items/Definitions/Battery/Battery.asset:15
```yaml
# main junction
system: {fileID: 895045449}
requiredItem: {fileID: 11400000, guid: c756a156ba7f34cb2b65e32264413a60, type: 2}
# independent junction
system: {fileID: 681488532}
```
The enabled build scene is Wakeup. The restoration scene is a separate disposable demo.
Files: ProjectSettings/EditorBuildSettings.asset:8; Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs:13
```yaml
- enabled: 1
  path: Assets/_Project/Scenes/Wakeup.unity
```

### What existing persistence behavior applies to collected or consumed repair items, equipment faults, room conditions, and completed tasks across leaving a room or reloading play?
Production saves record container contents and collected pickup IDs. Successful inventory commands mark the session dirty; collection atomically grants its full quantity and records the pickup ID. Capture serializes live container contents and collected IDs, so consumed inventory quantity is reflected in the next successful save. The schema has no equipment faults, room conditions, or completed-task records.
Files: Assets/_Project/Domain/SaveGameData.cs:9; Assets/_Project/Domain/ContainerSaveSession.cs:38, :41, :78
```csharp
public int worldSeed;
public List<ContainerSaveRecord> containers = new();
public List<string> collectedPickupIds = new();
var result = Commands.Grant(inventory.Container.Handle, itemId, quantity, () => collected.Add(pickupId));
```
Saving flushes in LateUpdate, application pause/quit, destruction, and release, subject to dirty/integrity/busy/recovery guards. These are attempted writes, not guarantees of successful disk I/O.
Files: Assets/_Project/Core/SaveLoad/SaveGameService.cs:39, :45
```csharp
try { store.Write(session.Capture()); session.MarkSaved(); }
catch (Exception failure) { Debug.LogError("[SaveGameService] Failed to save: " + failure.Message); }
private void LateUpdate() => Flush();
```
Restoration itself is owned by each RestorableSystem instance and is not in this save schema. Staying in the same scene keeps that instance's Boolean; replacing it creates new state. The demo explicitly uses memory files and resets its session when its player scene is rebound; its reload test expects empty inventory, uncollected batteries, offline terminals, and unrestored systems. Production reload coverage expects collected pickup identities to survive.
Files: Assets/_Project/Features/World/Restoration/RestorableSystem.cs:8; Assets/_Project/Features/World/Restoration/Demo/RestorationDemoSession.cs:13; Assets/_Project/Core/Composition/GameCompositionRoot.cs:125; Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs:65; Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:60
```csharp
private readonly RestorableSystemState state = new RestorableSystemState();
if (entry.HasPlayer && disposableSession)
    session.Reset(config.worldSeed, releaseOwners: true);
```
The existing start completion is a local Boolean set on player trigger entry through an open door; it emits “Access granted” feedback/logging. No persistence registration exists in that component.
Files: Assets/_Project/Features/World/SolitudeStart/SolitudeStartCompletion.cs:11, :13
```csharp
if (completed || door == null || !door.IsOpen || other.GetComponent<PlayerInteractor>() == null) return;
completed = true;
```

### How does the player discover and access equipment interactions, and how are available actions and their results presented?
A proximity sensor registers its parent interactable on player trigger entry and unregisters on exit/destruction. PlayerInteractor selects the nearest registered MonoBehaviour by squared distance while input is allowed. Its action checks CanInteract, invokes Interact synchronously, sends the result to feedback, and emits an interaction-triggered event. The emitted event can occur even when the target returns a blocked result after allowing CanInteract.
Files: Assets/_Project/Systems/Interaction/Core/InteractableProximitySensor.cs:23, :28, :39; Assets/_Project/Features/Player/Interaction/PlayerInteractor.cs:80, :114
```csharp
player.Register(interactable);
if (!currentTarget.CanInteract(this)) { InteractionFeedback.Handle(InteractionResult.Blocked()); return; }
var result = currentTarget.Interact(this);
InteractionFeedback.Handle(result);
```
UIController refreshes focused target prompt text each LateUpdate; the prompt follows its target in screen space and has an authored default input label E. Results appear in a transient feedback view. Junction and terminal interactions expose one prompt/action rather than an equipment menu.
Files: Assets/_Project/Systems/Interaction/UI/Controllers/UIController.cs:46; Assets/_Project/Systems/Interaction/UI/Views/InteractionPromptView.cs:15, :26; Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:25
```csharp
private void LateUpdate() => RefreshPrompt();
public override string GetPrompt() => IsPowered ? "Read terminal" : "Terminal offline";
```

### How are items placed, discovered, and collected within a room?
WorldPickup is an interactable with an authored PickupDefinition and required SaveableId. Definition supplies item, quantity, and success text. SceneBindings registers configured pickups; registration validates unique instance identity and immutable reward. Collected instances deactivate on binding or successful collection. They use the same proximity/focus interaction path described above.
Files: Assets/_Project/Features/Items/Core/WorldPickup.cs:9, :20, :43; Assets/_Project/Features/Items/Core/PickupDefinition.cs:8; Assets/_Project/Core/Composition/SceneBindings.cs:95, :120; Assets/_Project/Domain/PickupCollectionService.cs:27
```csharp
[RequireComponent(typeof(SaveableId))]
if (service.IsCollected(pickupId)) gameObject.SetActive(false);
```
Battery has a sprite/pickup prefab and a one-item pickup definition with “Picked up battery” feedback. The playable restoration slice test discovers two authored battery pickups and interacts with them through player focus.
Files: Assets/_Project/Features/Items/Definitions/Battery/Battery.asset:16; Assets/_Project/Features/Items/Definitions/Battery/BatteryPickup.asset:15; Assets/_Project/Features/Items/Definitions/Battery/PF_Battery.prefab:68; Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:99
```yaml
quantity: 1
successText: Picked up battery
```
Collection with insufficient capacity returns InventoryFull without recording collection. WorldPickup presents “Inventory full” and keeps the object active for that outcome.
Files: Assets/_Project/Domain/ContainerSaveSession.cs:46; Assets/_Project/Features/Items/Core/WorldPickup.cs:51
```csharp
return InteractionResult.Blocked(result == CollectResult.InventoryFull ? "Inventory full" : "Pickup unavailable");
```

### How are possession checks, repair-item use, and item consumption handled in existing interactions?
Restoration requires the exact configured item ID in the current registered player-inventory container; hotbar/locker possession alone is insufficient. It validates inventory handle identity and reader instance, checks busy state, then removes one matching item with restoration as the command commit callback. Unavailable, missing, or rejected attempts do not set restoration in this method.
Files: Assets/_Project/Domain/Restoration/RestorableSystemState.cs:21, :29; Assets/_Project/Core/Composition/SceneBindings.cs:98
```csharp
inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId ||
!ReferenceEquals(commands.Read(inventory.Handle), inventory)
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
```
OpeningPowerSocket is a separate older possession-only interaction: it reads inventory slots for battery ID, delegates to a door, and marks Powered without consuming the battery.
Files: Assets/_Project/Features/World/OpeningSlice/OpeningPowerSocket.cs:16
```csharp
bool battery = inventory?.Container?.GetSlots().Any(s => !s.IsEmpty && s.Stack.ItemId == batteryItemId) == true;
var result = door.Interact(player);
Powered = true;
```

### How are equipment state changes connected to room conditions and task progress?
The inspected restoration consumers are a terminal and door gate. They subscribe to the system's Changed event, reconcile on enable, and unsubscribe on disable. Terminal maps restored state to powered screen color/message; gate maps it to access permission. Gate reconciliation does not open the door automatically. No room-atmosphere or task-progress consumer was found by the bounded searches above.
Files: Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:32, :39; Assets/_Project/Features/World/Restoration/RestorationDoorGate.cs:25
```csharp
if (subscribed != null) subscribed.Changed += Reconcile;
powered = system != null && subscribed != null && subscribed.IsRestored;
private void Reconcile() => restored = system != null && subscribed != null && subscribed.IsRestored;
```
Independent systems are tested as independent, and restoration slice coverage asserts the main circuit does not restore the service circuit.
Files: Assets/_Project/Tests/Domain/RestorationTests.cs:74; Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:110
```csharp
Assert.IsTrue(main.IsRestored); Assert.IsFalse(other.IsRestored);
```

### What existing UI guidance and reusable presentation patterns apply to equipment faults, telemetry, controls, missing-item feedback, and failed restarts?
Documentation guidance is unresolved under the explicit source restriction excluding UI README and design/proposal documents. Current code provides target-following E prompts, transient result feedback, offline/online terminal screen colors, and inventory requirements in junction messages. The feedback presenter uses success versus non-success messages; the view converts green success to its authored default text color and leaves blocked feedback red, replacing the previous timer. Telemetry/fault dashboards and failed scrubber restart presentation were not found in the bounded searches above.
Files: Assets/_Project/Systems/Interaction/UI/Presenters/InteractionFeedback.cs:25; Assets/_Project/Systems/Interaction/UI/Views/InteractionFeedbackView.cs:11, :16, :25; Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:12, :44; Assets/_Project/Features/World/Restoration/RestorationJunction.cs:30
```csharp
if (!string.IsNullOrEmpty(result.Message)) view?.Show(result.Message, Color.green);
view?.Show(message, Color.red);
label.color = color == Color.green ? defaultColor : color;
yield return new WaitForSeconds(duration); // authored default 3 seconds
```

### What happens today when a player repeats item collection, repair, or restart actions, closes the equipment interface during an action, or leaves and returns before an action completes?
Repeated collected pickup requests return AlreadyCollected before granting again; WorldPickup deactivates and emits no new success text for AlreadyCollected. Repeated restoration returns AlreadyRestored before scanning or removing more inventory; reentrant restoration returns Busy. These actions complete synchronously, so the inspected pickup/junction implementation has no in-progress equipment interface or pending repair/restart to close or abandon. Scrubber restart repetition was not found in the bounded searches.
Files: Assets/_Project/Domain/ContainerSaveSession.cs:43; Assets/_Project/Features/Items/Core/WorldPickup.cs:51; Assets/_Project/Domain/Restoration/RestorableSystemState.cs:19; Assets/_Project/Features/World/Restoration/RestorationJunction.cs:31
```csharp
if (collected.Contains(pickupId)) return CollectResult.AlreadyCollected;
if (restoring) return RestorationOutcome.Busy;
if (IsRestored) return RestorationOutcome.AlreadyRestored;
```
Terminal reads are repeatable and do not consume an item; reenabling reconciles a missed restoration event. Leaving proximity unregisters target focus. Scene unload releases its scope; subsequent demo reload resets state as detailed above. A container modal has a separate transition close path.
Files: Assets/_Project/Features/World/Restoration/RestorationTerminal.cs:27, :31; Assets/_Project/Systems/Interaction/Core/InteractableProximitySensor.cs:39; Assets/_Project/Core/Composition/GameCompositionRoot.cs:131; Assets/_Project/Core/Composition/SceneBindings.cs:136
```csharp
public void CloseForTransition() => modal?.Close();
```

### What existing tests, playable checks, or validation conventions verify room item collection, equipment interaction, inventory use, atmospheric changes, task completion, and visible or audible feedback?
Existing tests were read, not executed in this research pass. RestorationTests covers atomic consumption/state notification, once-only installation, missing/wrong/container-only possession, busy/unavailable attempts, reentry/observer failures, independent systems, and stale removal. PickupPersistenceTests covers round-trip identity/inventory preservation, full-capacity refusal, and other registration/reload cases.
Files: Assets/_Project/Tests/Domain/RestorationTests.cs:22, :36, :50, :62, :74, :84; Assets/_Project/Tests/Editor/PickupPersistenceTests.cs:53, :68
```csharp
Assert.AreEqual(RestorationOutcome.AlreadyRestored, Restore());
Assert.AreEqual(1, changes); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
Assert.AreEqual(CollectResult.InventoryFull, Collect("pickup.a", 2));
Assert.IsFalse(service.IsCollected("pickup.a"));
```
RestorationSliceTests is a playable Unity test using real focus/input, inventory transfers, collision/movement, feedback/prompt assertions, terminal color, repeat interaction, independent circuits, and reload reset. DemoLifecycleTests verifies normal-save isolation. ProductionLifecycleTests checks collection across an actual player-scene reload/new save. Separate RestorationTerminalTests and RestorationDoorTests cover terminal/gate lifecycle and missing dependencies.
Files: Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:75, :93, :110, :117, :127; Assets/_Project/Tests/PlayMode/RestorationDemoLifecycleTests.cs:44; Assets/_Project/Tests/PlayMode/ProductionLifecycleTests.cs:60; Assets/_Project/Tests/PlayMode/RestorationTerminalTests.cs:39; Assets/_Project/Tests/PlayMode/RestorationDoorTests.cs:43
```csharp
Assert.AreEqual("System restored", Prompt); Assert.AreEqual("System restored", Feedback);
Assert.IsTrue(terminal.IsPowered); Assert.AreNotEqual(off, screen.color);
```
Pickup authoring validation audits scene instance IDs, checks definitions/catalog membership, and rejects prefab-shared instance IDs. A player smoke check verifies save restart does not duplicate a pickup or change inventory quantities.
Files: Assets/_Project/Editor/PickupIdentityValidator.cs:17, :19, :34, :42, :55; Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:105
```csharp
if (string.IsNullOrWhiteSpace(pickup.PickupId)) errors.Add($"Missing pickup identity at {location}.");
```
Atmospheric-change, scrubber-restart, task-completion, and audible atmospheric-feedback test coverage was not found in the authorized Tests `.cs` searches for `co₂|co2|scrubber|atmospher|telemetry|fuse|task|quest|oxygen|audio`; oxygen matches were inventory persistence test items. No broader coverage claim is made.

## New Questions
