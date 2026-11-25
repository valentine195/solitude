# QRSPI Research: mechanical-tool-repairs

Source snapshot: HEAD `4c59a8f42d15f440a46449c10658b31b39f2c971`, inspected 2026-11-13. Working tree is dirty. Relevant modified files include `Core/Composition/SceneBindings.cs`, `Core/Input/UnityInputHost.cs`, input-action assets, Battery and ReplacementFuse definitions, ScrubberEncounterBuilder, and demo/production scenes; additional inventory UI/art changes are present. Findings describe the current working tree, not HEAD alone. Paths below are relative to `Assets/_Project/` unless stated otherwise. No tests were executed; test references establish existing assertions only. No source files were modified.

## Answers

### How is the maintenance wrench currently defined, obtained, and used for primary weapon attacks, and what determines whether it is available for contextual interactions?

`Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset:15-34` defines Maintenance Wrench, a nonstackable item with maximum stack 1, ID `104950f738ebc4af4a05ec473310a59a`, no pickup prefab, six tag references, and one Primary action (slot 0) referencing MeleeAttack. `Features/Items/Loadouts/MaintenanceStartingLoadout.asset:15-18` grants one wrench to the hotbar and selects slot 0. `Core/Composition/GameRuntimeConfig.asset:16` references this starting loadout; `Application/Items/StartingLoadoutCoordinator.cs:51-61` initializes only when the session flag is false, persists before selecting, and reports persistence failure. The authored tags are independently asserted as Item.Weapon, Item.Tool, Item.Improvised, Weapon.Melee, Damage.Blunt, Tool.Mechanical in `Tests/Editor/ItemActionAuthoringTests.cs:35-50`.

```yaml
# Features/Items/Loadouts/MaintenanceStartingLoadout.asset:15
Hotbar:
- Item: {fileID: 11400000, guid: 104950f738ebc4af4a05ec473310a59a, type: 2}
  Quantity: 1
SelectedSlot: 0
```

`Features/Player/Combat/PlayerItemActionComposition.cs:28-54` requires an enabled living, unstunned actor, gameplay policy, and a captured selected hotbar item, resolves its requested action, and starts a configured request. `Application/Items/EquippedItemView.cs:38-53` verifies current container ownership, selection and slot revision. `Core/Composition/SceneBindings.cs:203-210` binds Primary input to this action request. The shipped primary specification is authored at `Features/Items/Weapons/MaintenanceWrench/MeleeConfiguration.asset:15-27` and asserted at `Tests/Editor/ItemActionAuthoringTests.cs:44-50`: damage 10, range 1.25 tiles, arc 90°, .12/.10/.33 seconds windup/active/recovery, movement factor .65, Damage.Blunt damage tags.

```csharp
// Features/Player/Combat/PlayerItemActionComposition.cs:47
if (runner == null || !ActorAvailable || !policy.Gameplay || !equipment.TryCapture(out var binding)) return false;
var item = capabilities.Resolve(binding.ItemId); var action = item?.Resolve(slot);
```

Current contextual restoration has no wrench availability rule: `Features/World/Restoration/RestorationJunction.cs:12-18,28` receives a system, one required item, commands and player inventory only. It neither receives the capability catalog nor inspects equipped items or Tool.Mechanical. The existing wrench grant is in hotbar, whereas this restoration implementation accepts parts from `player.inventory` only (`Domain/Restoration/RestorableSystemState.cs:21-33`).

### Which existing damaged panel, replacement part, and associated door-access behavior can support the initial repair using one part?

There is no uniquely selected panel in the questions. Current source provides these distinct authored interactions:

- The ShipSystemRestoration demo's `Auxiliary power socket` references a `RestorableSystem` and Battery ID `c756a156ba7f34cb2b65e32264413a60`: `Scenes/Demos/ShipSystemRestoration.unity:12778,12873-12874`. Its SceneBindings list is at `:5170`; the builder connects the same system to the existing sliding door through RestorationDoorGate (`Editor/RestorationDemoBuilder.cs:38-44`). The Battery is stackable, max 99 (`Features/Items/Definitions/Battery/Battery.asset:15-26`). Restoration consumes exactly one; restoring access does not itself open the door (`Features/World/Restoration/RestorationDoorGate.cs:14,25-32`; `Features/World/SolitudeStart/SolitudeSlidingDoor.cs:21-29`). This is a disposable demonstration, not disk-persistent production wiring (`Core/Composition/GameCompositionRoot.cs:70-72,187-194`).
- The opening reference uses `OpeningPowerSocket`, with a serialized door and battery ID (`Features/World/OpeningSlice/OpeningPowerSocket.cs:11-24`). It checks possession in player inventory, directly calls door interaction, and sets a local Powered boolean. It does not remove the battery.
- The atmospheric scrubber has the dedicated Replacement fuse, ID `4716e84c715564ad2ac79c4be74037dd` (`Features/Items/Definitions/ReplacementFuse/ReplacementFuse.asset:15-26`). `Features/World/ScrubberEncounter/ScrubberEquipment.cs:13-21,32-45` installs one through restoration, then requires a separate restart. Its local state progresses room atmosphere, not the sliding-door gate (`Domain/Restoration/ScrubberEncounterState.cs:20-34`).

```csharp
// Editor/RestorationDemoBuilder.cs:42-43
var junction = socketGO.AddComponent<RestorationJunction>(); junction.Configure(main, battery.Definition.Item);
var door = All<SolitudeSlidingDoor>(scene).Single(); door.gameObject.AddComponent<RestorationDoorGate>().Configure(main);
```

Not found within `Features`, `Systems/GameplayTags`, and `Scenes` asset/scene searches: a literal `Damaged`/`damaged` named panel designation resolving the question to one target. Searches used `Damaged|damaged`, `RestorationJunction`, `requiredItem:`, `system:`, and the relevant item IDs, excluding all `.qrspi/` trees. This does not establish absence of a visually damaged panel.

### Which existing interaction entry points and callers would be affected by contextual repair, and how are contextual interactions kept separate from primary weapon actions?

`Systems/Interaction/Core/IInteractable.cs:7-11` and `InteractableBase.cs:6-14` define the contextual target contract; the player call site is `Features/Player/Interaction/PlayerInteractor.cs:97-128`. It checks input permission, current target and CanInteract, invokes Interact, forwards its result to feedback, and publishes InteractionTriggered for any attempt reaching an allowed target (including a blocked result returned by Interact). Target focus is selected from registered proximity targets (`:131-165`). `RestorationJunction.Interact` is a concrete entry point (`Features/World/Restoration/RestorationJunction.cs:25-34`). Tests also call it directly (`Tests/Editor/RestorationJunctionTests.cs:29-46`).

`Core/Input/UnityInputHost.cs:187-191,228` subscribes Interact and Primary separately. Interact calls PlayerInteractor; Primary has a separate scene lease pointing to PlayerItemActionComposition (`Core/Composition/SceneBindings.cs:210`). Existing action contexts are based on selected hotbar equipment, not the focused IInteractable (`Features/Player/Combat/PlayerItemActionComposition.cs:45-54`). `Core/Input/SOLITUDE_InputActions.inputactions:240` assigns keyboard E to contextual interaction, and `:438` assigns mouse left button to Primary.

```csharp
// Core/Input/UnityInputHost.cs:187,191,228
// Separate action subscriptions:
actions.Gameplay.Interact.performed += Interact;
actions.Gameplay.Primary.performed += Primary;
private void Interact(InputAction.CallbackContext context) { if (Fresh(context) && GameplayAllowed && interactor != null) interactor.TryInteract(); }
```

### How are gameplay tags assigned to items and matched against requirements, including whether Tool.Mechanical exists and how parent or child tags affect compatibility?

`Features/Items/Core/ItemDefinition.cs:26-29` authors GameplayTagDefinition[] and action bindings. `Features/Items/Database/ItemCapabilityCatalog.cs:37-63` resolves authored tags through a registry into immutable per-item capability snapshots keyed by ItemId. Missing registry or invalid references/duplicate action slots fail compilation. `Systems/GameplayTags/Assets/ToolMechanical.asset:15-18` defines Mechanical under Tool (`Systems/GameplayTags/Assets/Tool.asset:15-18`), and the wrench references its GUID at `Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset:27`.

`Domain/GameplayTags/GameplayTagSet.cs:18-24,28-49` stores explicit assignments and all ancestors. Hierarchical matching is default: an assigned child satisfies its ancestor; an assigned parent does not satisfy a child. Exact matching considers only explicit assignments. `Domain/GameplayTags/GameplayTagQuery.cs:20-28` applies required/all, excluded/none and any-of conditions using one match mode and rejects different registry snapshots. `Systems/GameplayTags/GameplayTagRegistryAsset.cs:44-50` rejects unregistered tag references.

```csharp
// Domain/GameplayTags/GameplayTagSet.cs:23,48-49
for (var ancestor = tag; ancestor != null; ancestor = ancestor.Parent) hierarchical.Add(ancestor);
internal bool Contains(GameplayTag tag, GameplayTagMatch match)
    => match == GameplayTagMatch.Exact ? exact.Contains(tag) : hierarchical.Contains(tag);
```

### How does inventory identify and count a required replacement part, remove exactly one, and report failure without consuming unrelated items?

Inventory exposes slot snapshots carrying stable ItemId, quantity, and revision-address (`Domain/IContainerReader.cs:15-39`; `Domain/ItemStack.cs:4-11`). Restoration scans current inventory slots for exact ItemId equality, takes the first matching nonempty stack, and calls Remove(address, 1). It does not aggregate a count across containers; the required count is one (`Domain/Restoration/RestorableSystemState.cs:29-38`). Scrubber diagnosis similarly checks Any matching slot (`Features/World/ScrubberEncounter/ScrubberEquipment.cs:19-21`).

`Domain/ContainerCommandService.cs:182-194` validates address, positive quantity and sufficient stack quantity before cloning the slots, decrementing only that slot and committing. It reports Unavailable/InvalidSlot/Stale for invalid ownership/index/revision (`:126-130`) and InsufficientQuantity when empty or insufficient. An unsuccessful removal leaves restoration false because the world callback only runs during commit (`Domain/Restoration/RestorableSystemState.cs:33-35`).

```csharp
// Domain/ContainerCommandService.cs:189-193
var stack = state.Slots[address.Index];
if (stack == null || quantity > stack.Quantity) return new CommandResult(CommandStatus.InsufficientQuantity);
var plan = (ItemStack[])state.Slots.Clone();
plan[address.Index] = quantity == stack.Quantity ? null : new ItemStack(stack.Definition, stack.Quantity - quantity);
Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan }, worldCommit);
```

### How do contextual interactions inspect available player items, distinguish reusable tool requirements from consumed part requirements, and display missing requirements?

There is no reusable-tool/consumed-part requirement distinction in the inspected restoration targets. RestorationJunction has only requiredItem and always delegates one-item consumption; OpeningPowerSocket separately implements a possession-only battery check; ScrubberEquipment implements one consumed fuse (`Features/World/Restoration/RestorationJunction.cs:12-18,28`; `Features/World/OpeningSlice/OpeningPowerSocket.cs:18-24`; `Features/World/ScrubberEncounter/ScrubberEquipment.cs:19-39`). No capability/tag check exists in these methods.

SceneBindings injects the live player inventory and commands into junctions (`Core/Composition/SceneBindings.cs:156-159`). A junction's normal prompt names the part even when missing; missing inventory feedback occurs on interaction. It reports unavailable configuration, already restored, missing part, or generic installation failure (`Features/World/Restoration/RestorationJunction.cs:20-34`). UIController polls GetPrompt in LateUpdate and refreshes changed text without another focus event (`Systems/Interaction/UI/Controllers/UIController.cs:48-61`). InteractionFeedbackService displays blocked messages in red and success messages in green (`Systems/Interaction/UI/Presenters/InteractionFeedbackService.cs:15-23`).

```csharp
// Features/World/Restoration/RestorationJunction.cs:31-33
case RestorationOutcome.AlreadyRestored: return InteractionResult.Blocked("System already restored");
case RestorationOutcome.MissingItem: return InteractionResult.Blocked("Requires 1 " + Requirement + " in inventory");
default: return InteractionResult.Blocked("Installation unavailable; no item installed");
```

Bounded absence search: `Tool.Mechanical`, requirement classes and restoration/tag symbols in `Domain`, `Features`, `Systems`, `Core`, and `Tests`; then direct inspection of junction, opening socket and scrubber methods. No generic contextual tool requirement evaluator was found in those owners.

### What existing restoration infrastructure can unlock access through the selected door, and how does it handle repeated restoration requests?

RestorableSystem owns one in-memory RestorableSystemState (`Features/World/Restoration/RestorableSystem.cs:8-12`). RestorationDoorGate subscribes to that state's Changed event, reconciles immediately on enable/configure, unsubscribes on disable, and AllowsAccess requires enabled gate, live system and restored state (`Features/World/Restoration/RestorationDoorGate.cs:14-38`). SolitudeSlidingDoor checks gate access before starting animation and blocks repeated requests while opening/open (`Features/World/SolitudeStart/SolitudeSlidingDoor.cs:16-29`); its final animation frame clears the blocker (`:38-46`). It restores permission to open; the player must separately interact with the door.

RestorableSystemState returns Busy during reentry and AlreadyRestored after success, before scanning/removing inventory (`Domain/Restoration/RestorableSystemState.cs:19-20`). A success emits one state publication; observer exceptions are isolated (`:35,43-52`).

```csharp
// Features/World/Restoration/RestorationDoorGate.cs:28-32
subscribed = system != null ? system.State : null;
if (subscribed != null) subscribed.Changed += Reconcile;
Reconcile();
// ...
private void Reconcile() => restored = system != null && subscribed != null && subscribed.IsRestored;
```

### How are persistent world objects identified, how is their restored state saved and loaded, and when is that state reapplied relative to scene initialization?

SaveableId has a serialized per-instance string, one-time missing editor GUID assignment, and guarded runtime identity assignment (`Core/SaveLoad/SaveableId.cs:22-42`). WorldPickup reads this identity, registers a reward and deactivates already collected objects during Initialize (`Features/Items/Core/WorldPickup.cs:18-38`). SceneBindings registers pickups before player inventory/interaction initialization (`Core/Composition/SceneBindings.cs:153-159`). ContainerSaveSession restores saved container records by identity before returning each registered live container (`Domain/ContainerSaveSession.cs:79-89`).

SaveGameData v4 contains ship state and containers (`Domain/SaveGameData.cs:12-23`), including RepairSaveRecord{id, installed, running, recoverySeconds} and DoorSaveRecord{id, powered, unlocked, open} (`Domain/Expeditions/ExpeditionState.cs:94-107`). Ship validation rejects duplicate IDs, contradictory repair state and open-but-locked doors (`:54-89`). StoreShipState clones/validates and marks dirty/urgent (`Domain/ContainerSaveSession.cs:124-129`); Capture includes cloned ship and live containers (`:97-104,164-173`). SaveJsonCodec validates serialized repair/door fields (`Core/SaveLoad/SaveJsonCodec.cs:65-75`).

```csharp
// Domain/ContainerSaveSession.cs:124-129
public void StoreShipState(ShipStateSnapshot ship)
{
    if (Commands.IsBusy || HasIntegrityFailure) throw new InvalidOperationException("Session is not ready for ship changes.");
    if (ship == null) throw new ArgumentNullException(nameof(ship));
    var candidate = Capture(); candidate.ship = ship.Clone(); candidate.ValidateLifecycle();
    data = candidate; IsDirty = true; RequiresImmediateSave = true;
}
```

A restoration-to-ship-state capture/reapplication runtime binding was **not found** in searches of `Assets/_Project` C# using `StoreShipState\(`, `ship\.repairs`, `ship\.doors`, plus `RepairSaveRecord`/`repairs` searches in runtime owners. StoreShipState callers found were tests only. Current RestorableSystemState has no restore/import method (`Domain/Restoration/RestorableSystemState.cs:10-55`), RestorableSystem has no identity/save integration (`Features/World/Restoration/RestorableSystem.cs:6-12`), and ScrubberEncounterState explicitly states it is scene-owned and not production-saved (`Domain/Restoration/ScrubberEncounterState.cs:5-8`). Therefore no current restoration reapplication timing is established. General boot loads save before binding scenes and enables input only after binding/start-loadout initialization (`Core/Composition/GameCompositionRoot.cs:73-99`); feature initialization occurs before action input binding (`Core/Composition/SceneBindings.cs:193-210`).

### How are persistent world-state changes and consumed inventory items coordinated during an interaction and subsequent saving?

Within current restoration, inventory decrement and the in-memory IsRestored boolean commit together before command or slot observers (`Domain/Restoration/RestorableSystemState.cs:32-35`; `Domain/ContainerCommandService.cs:366-375`). ContainerSaveSession subscribes to command completion to set dirty (`Domain/ContainerSaveSession.cs:44`), and prevents capture during commands (`:97-99`). SaveGameService refuses flushing during collection, recovery or integrity failure, captures living state, writes the complete session snapshot, then marks saved (`Core/SaveLoad/SaveGameService.cs:46-62`). Autosave retains dirty state and retries failures (`:68-75`), and transition flushes before loading another scene (`Core/Composition/GameCompositionRoot.cs:208-218`).

```csharp
// Domain/ContainerCommandService.cs:366-369
foreach (var p in publications) { p.State.Slots = p.Slots; p.State.Revisions = p.Revisions; p.State.Clock = p.Clock; }
worldCommit?.Invoke();
var change = new ContainerChangeSet(handles, lastChanges);
if (Completed != null)
```

The current restoration boolean is not copied into saved ship state by that callback. StoreShipState is separately prohibited during a busy command (`Domain/ContainerSaveSession.cs:126`). Accordingly the existing transaction establishes coherent in-memory part removal/restoration, and the save schema can contain inventory plus ship facts, but no runtime restoration path was found that puts both into that saved snapshot (bounded searches described above). Demo restoration instead resets inventory and world together on scene binding (`Core/Composition/GameCompositionRoot.cs:187-194`).

### What existing safeguards handle repeated interaction requests or changes to inventory between checking requirements and applying an interaction?

Restoration checks live inventory on each attempt; no diagnosis result is cached as authority. Guards verify correct inventory ID, same command-owned reader, availability, nonempty item ID, restoring flag, restored flag and command busy state (`Domain/Restoration/RestorableSystemState.cs:19-33`). SlotAddress includes container generation, index and revision (`Domain/IContainerReader.cs:6-21`); Address validation rejects stale revisions (`Domain/ContainerCommandService.cs:126-130`). Commands reject nested operations while executing/publishing (`:20-21,68-72`). Both inventory and world booleans are committed before observers, with restoration's guard retained until publication ends (`Domain/Restoration/RestorableSystemState.cs:26-40`).

```csharp
// Domain/Restoration/RestorableSystemState.cs:19-25
if (restoring) return RestorationOutcome.Busy;
if (IsRestored) return RestorationOutcome.AlreadyRestored;
if (commands == null || inventory == null || !inventory.IsAvailable ||
    inventory.Handle.Id != ContainerSaveSession.PlayerInventoryId ||
    !ReferenceEquals(commands.Read(inventory.Handle), inventory) || string.IsNullOrWhiteSpace(itemId))
    return RestorationOutcome.Unavailable;
if (commands.IsBusy) return RestorationOutcome.Busy;
```

These guards currently concern the consumed part; there is no tool requirement check to guard in this path (`Features/World/Restoration/RestorationJunction.cs:25-34`).

### How do existing restoration and persistence behaviors handle interacting with an already restored target or loading its state after leaving and returning to its scene?

Already restored junctions keep the same active interaction eligibility, show "System restored", and return a blocked "System already restored" result without consuming another part (`Features/World/Restoration/RestorationJunction.cs:22-32`; `Domain/Restoration/RestorableSystemState.cs:19-20`). Disabling/re-enabling the same object preserves its owned state, and gates reconcile on enable (`Features/World/Restoration/RestorableSystem.cs:8`; `RestorationDoorGate.cs:20-32`). The door blocks repeat opening requests (`Features/World/SolitudeStart/SolitudeSlidingDoor.cs:22-25`).

Fresh scene objects construct fresh restoration state; there is no loaded restoration-state adapter in the searched runtime owners (see persistence answer). The demo explicitly resets its session on rebinding (`Core/Composition/GameCompositionRoot.cs:187-194`). Existing lifecycle assertions expect all systems unrestored, terminals unpowered and pickups/inventory reset on demo reload (`Tests/PlayMode/RestorationDemoLifecycleTests.cs:63-74`). Saved container bindings, by contrast, capture state on disposal and restore it on registration (`Domain/ContainerSaveSession.cs:79-89,182-187`).

```csharp
// Tests/PlayMode/RestorationDemoLifecycleTests.cs:66-69 (existing assertions; not executed)
yield return LoadDemo(); yield return null;
Assert.AreSame(previous, Root); Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored));
Assert.IsTrue(All<RestorationTerminal>().All(t => !t.IsPowered));
```

### What existing tests, fixtures, or manual verification conventions can verify tool-tag compatibility, missing-requirement feedback, retention of the wrench, consumption of one replacement part, door access restoration across saving and loading, and preservation of primary weapon behavior?

Existing source coverage and limits:

- Tag hierarchy/exact compatibility and composed queries: `Tests/Domain/GameplayTagTests.cs:36-75`; foreign/missing reference checks at `:101-123`. Shipped wrench tags and primary configuration: `Tests/Editor/ItemActionAuthoringTests.cs:35-50`.
- Exact-one consumption, unrelated-item preservation, committed state visible to observers, repeat/reentrant outcomes: `Tests/Domain/RestorationTests.cs:22-34`. Wrong/hotbar/locker items: `:36-48`; busy/unavailable no-loss: `:50-60`; observer failures: `:62-72`; stale removal: `:84-89`.
- Junction prompts, configuration failures and prompt refresh while focused: `Tests/Editor/RestorationJunctionTests.cs:29-46,48-73`.
- Gate access, direct/player attempts, separate door opening and collider release, late enable/re-enable/missing-system behavior: `Tests/PlayMode/RestorationDoorTests.cs:43-69`.
- Demo reload resets, in-memory-only save isolation: `Tests/PlayMode/RestorationDemoLifecycleTests.cs:45-75`. This asserts reset rather than persistent restored-door access.
- Persistence schema preservation of stored ship facts with inventory/container serialization: `Tests/Editor/ContainerPersistenceTests.cs:246-266`; JSON round-trip repair/door records: `Tests/Editor/SaveJsonCodecTests.cs:92-101`; immutable snapshots and contradictory state rejection: `Tests/Domain/RunLifecycleStateTests.cs:80-106,143-153`. These do not establish restoration-target reapplication.
- Primary action phase, cancellation, owner loss and multiple actor behavior: `Tests/PlayMode/PlayerItemActionRuntimeTests.cs:56-116`; lockout/reentrancy: `Tests/Application/ItemActionRunnerTests.cs:27-59`. `Tests/PlayMode/WeaponSystemTestFixtures.cs:33` resolves the authored wrench. DamageDelivery assertions ensure Tool.Mechanical does not enter damage tags (`Tests/PlayMode/DamageDeliveryTests.cs:47-68`).

```csharp
// Tests/Domain/RestorationTests.cs:29-34 (existing assertions; not executed)
Assert.AreEqual(RestorationOutcome.Restored, Restore());
Assert.IsTrue(inventorySawState); Assert.IsTrue(restorationSawInventory);
Assert.AreEqual(RestorationOutcome.Busy, reentry);
Assert.AreEqual(RestorationOutcome.AlreadyRestored, Restore());
Assert.AreEqual(1, changes); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
Assert.AreEqual(3, inventory.GetSlot(1).Stack.Quantity);
```

The current restoration tests use cells/other items and do not exercise a retained reusable wrench requirement. No test asserting repaired-door access reapplication after production save/load was found in the restoration and lifecycle test files examined, consistent with the missing runtime adapter above. No tests were run.

Manual convention: `Features/World/Restoration/Demo/README.md:3-9` describes opening the demo directly, WASD/E, diagnosing junction before collecting a battery, installing, separately opening door, reading terminal, and repeating with an independent system. It explicitly documents inventory-only requirements, in-memory storage and reset on reload, and no extraction/disk persistence.

## New Questions

- Which authored panel/system, replacement item, and scene are intended as the selected repair target?
