# QRSPI Research: weapon-system

## Answers

Path notation: repository root is `/Users/jeremyvalentine/SOLITUDE`. Paths beginning `Assets/`, `ProjectSettings/`, or `Tools/` are relative to that root. Shortened paths beginning `Features/`, `Domain/`, `Application/`, `Core/`, `Systems/`, `Editor/`, `Tests/`, `Verification/`, or `Scenes/` are relative to `Assets/_Project/`. A bare filename or `:line` citation continues the most recently named source path in the same paragraph or code snippet. Sources were inspected, but no tests or Unity scenarios were executed in this research pass. All recursive source searches excluded `.qrspi/` trees. Negative findings describe the stated search scope, not uninspected vendor code.

### How are existing items identified, configured, serialized and presented, and which inventory and equipment metadata or runtime state do they currently carry?

`Assets/_Project/Features/Items/Core/ItemDefinition.cs:6-35` defines a ScriptableObject with display name, icon, description, pickup prefab, stackability/limit, consumable kind/amount and a serialized itemId. Its editor OnValidate fills an empty identity from the asset GUID (`:38-45`). `Features/Items/Database/ItemDatabase.cs:55-78` compiles definitions into a runtime ItemCatalog and separate presentation and consumable catalogs. `Domain/ItemCatalog.cs:5-15` makes ItemSpec immutable; its fields are ItemId, Stackable and MaxStackSize. `Domain/ItemStack.cs:4-11` contains only immutable definition and quantity. `Domain/ContainerSaveData.cs:6-15` persists slot index, itemId and quantity. Hotbar selection is separate runtime state, initially -1, and clears when its slot empties or owner becomes unavailable (`Application/RuntimePolicy.cs:151-166`). No dedicated equipped-item object or per-item action execution state was found in these item/container/hotbar models.

```csharp
// Domain/ContainerSaveData.cs:7
[Serializable] public class ContainerSlotSaveData { public int index; public string itemId; public int quantity; }
// Domain/ItemStack.cs:6-8
public ItemSpec Definition { get; }
public string ItemId => Definition.ItemId;
public int Quantity { get; }
```

### How do player input, facing, movement restrictions and environmental interactions currently work, and how are unavailable actions handled?

`Core/Input/UnityInputHost.cs:115-164` owns input: Move routes to PlayerMovement, Interact to PlayerInteractor, Use to one scene-owned vital-use callback, and number actions to hotbar selection. Gameplay delivery requires policy.Gameplay, a fresh control after resets, and no UI activation consumed that frame (`:58-89,153-160`). Interact binds E/gamepad east and Use binds F/gamepad west (`Core/Input/SOLITUDE_InputActions.inputactions:230-241,406-417`; keyboard/gamepad Use equivalence is asserted in `Tests/Editor/PlayerVitalsInputTests.cs:72-79`). PlayerMovement forwards vectors to MovementBehavior (`Features/Player/Movement/PlayerMovement.cs:11-12`). Movement normalizes vectors longer than 1 and uses Rigidbody2D.MovePosition in FixedUpdate; disposable movement blocks suppress effective movement (`Features/Creatures/Movement.cs:20-27,42-58`). HumanAnimationController writes MoveX/MoveY/IsMoving, including zero vectors, and exposes no facing property (`Core/Animation/Human/HumanAnimationController.cs:18-26`). PlayerInteractor chooses the nearest registered target by squared distance; it does not use facing (`Features/Player/Interaction/PlayerInteractor.cs:131-152`). It silently ignores disabled input, logs no target, sends Blocked feedback for CanInteract=false, and publishes InteractionTriggered only after an allowed target runs (`:97-128`).

```csharp
// Core/Input/UnityInputHost.cs:155
private void Interact(InputAction.CallbackContext context) { if (Fresh(context) && GameplayAllowed && interactor != null) interactor.TryInteract(); }
// Features/Creatures/Movement.cs:56-58
var effective=IsMovementBlocked?Vector2.zero:vector;
body.MovePosition(body.position + effective * moveSpeed * Time.fixedDeltaTime);
animationController?.SetMovement(effective);
```

### How do the introductory security enemy’s health, incoming damage, hit response, death, cleanup and drops currently work, and what configured health and combat values determine its expected survival against 10-damage hits?

The existing security NPC is SEC-0. Its current controller has movement, perception, charge, nonlethal stun and visual fields, but no health, incoming damage, hit response, death or drop implementation (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:9-44,181-218`). The authored prefab configures sight 6, attack range 1.8, windup .65 seconds, recovery 1.6 seconds, stun .8 seconds, patrol 1.1 and pursuit 2.5 (`Features/Npcs/Sec0/PF_Sec0.prefab:130-143`). Its fire method finds the closest solid ray hit and invokes PlayerStunReceiver only when the hit belongs to the bound target (`Sec0Controller.cs:189-195`). Release resets brain/navigation, clears binding and destroys active pulse effects; each effect also has a .4-second lifetime (`:185-186,208-218`). A 10-damage-hit survival count cannot be computed from current SEC-0 configuration because no recipient or health value exists there. Bounded search: `rg -n 'Damage|Health|Death|dead|drop|Drop|Audio|flash|knock' Assets/_Project/Features/Npcs/Sec0 -g '*.cs' -g '*.prefab'` found only the player's health eligibility check in `Runtime/Sec0Composition.cs:38`.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:195
if(hit!=null && hit.transform.IsChildOf(target) && receiver.TryStun(stunDuration)) StunsLanded++;
// Tests/PlayMode/Sec0RuntimeTests.cs:69-70
Charge();bot.Tick(1);Assert.AreEqual(1,bot.StunsLanded);Assert.IsTrue(stun.IsStunned);
Assert.AreEqual(health,player.GetComponent<PlayerVitalsController>().Vitals.Health.Current);
```

### What classification, capability or rule-query mechanisms already exist for items and entities, and where does current behavior depend on those mechanisms?

Items have stack rules (`Domain/ItemCatalog.cs:5-15`) and VitalConsumableKind None=0/Food=1/Medical=2 with Supported/IsValid queries (`Domain/Vitals/VitalConsumableSpec.cs:5-15`). Container registration evaluates an optional `Func<ItemSpec,bool>` once over the catalog and freezes accepted IDs (`Domain/ContainerCommandService.cs:36-40`). RestorationJunction requires an exact configured item ID (`Features/World/Restoration/RestorationJunction.cs:13-28`); ScrubberEquipment compares a constant fuse ID (`Features/World/ScrubberEncounter/ScrubberEquipment.cs:13-21`). Interactions use IInteractable and component lookup (`Systems/Interaction/Core/InteractableProximitySensor.cs:23-31`). TagManager has no custom Unity tags; layers include Interactable and Player (`ProjectSettings/TagManager.asset:6-38`). A legacy wakeup door sensor checks built-in Player tag (`Features/Interactables/Doors/Wakeup/InteractionZone.cs:19,32`). No general item/entity tag-set or rule-query class was found by `rg -n 'CompareTag|FindGameObjectsWithTag|Tag|tag'` over first-party Items, Player, Npcs, Domain, Application and Interaction C# source (the case-insensitive-looking substrings also return unrelated Stage names).

```csharp
// Domain/Vitals/VitalConsumableSpec.cs:11-13
public bool Supported => Kind == VitalConsumableKind.Food || Kind == VitalConsumableKind.Medical;
public bool IsValid => !string.IsNullOrWhiteSpace(ItemId) && Supported && Amount > 0
    && !double.IsNaN(Amount) && !double.IsInfinity(Amount);
```

### Which existing consumers of item definitions, inventory and equipment would be affected by adding tags, action configuration and equipped-item information?

Current data-flow consumers are the database compilers (`Features/Items/Database/ItemDatabase.cs:55-78`), runtime stack/catalog and container acceptance/transaction code (`Domain/ItemCatalog.cs:5-33`, `Domain/ItemStack.cs:4-11`, `Domain/ContainerCommandService.cs:24-40,132-175,210-255`), pickup and loot compilation (`Features/Items/Core/WorldPickup.cs:32,50`, `Features/Items/Loot/LootTable.cs:26,47`), save slots (`Domain/ContainerSaveData.cs:7-15`), catalog validation (`Editor/ItemCatalogValidator.cs:18-31`), composition (`Core/Composition/GameCompositionRoot.cs:59-62`, `Core/Composition/SceneBindings.cs:140-178`), exact-item restoration consumers (`Features/World/Restoration/RestorationJunction.cs:13-28`, `Features/World/ScrubberEncounter/ScrubberEquipment.cs:13-34`), selected consumable use (`Application/Vitals/SelectedVitalItemUseCoordinator.cs:22-34`), and UI presenters/views (`Application/ContainerPresenter.cs:59-83`, `Systems/Containers/Views/ContainerSlotView.cs:19-27`). These are observed dependencies; the effect of a future schema change depends on which fields/contracts it changes. Current selection is consumed by Hotbar, input, presentation, vitals and succession (`Features/Hotbar/Hotbar.cs:10-17`, `Core/Input/UnityInputHost.cs:128-164`, `Features/Player/Succession/SurvivorSuccessionComposition.cs:87,162`).

```csharp
// Core/Composition/SceneBindings.cs:145-147
selection = new HotbarSelectionState(hotbarBinding.Container); hotbar.Initialize(hotbarBinding.Container, selection);
foreach (var controller in controllers)
    controller.Initialize(presentations, gestures, policy, controller.DefaultSource == hotbar ? selection : null);
```

### Where do current damage producers and damage recipients share contracts, and which callers depend on their existing behavior?

PlayerVitalsController exposes Damage(double), delegating to pure VitalsRecoveryRules and applying accepted results (`Features/Player/Vitals/PlayerVitalController.cs:147-153`). The pure rule rejects invalid/nonpositive amounts, subtracts with a zero floor and resets recovery delay (`Domain/Vitals/VitalsRecoveryRules.cs:24-33`). It receives gameplayEligible, but the Damage body does not itself reject false eligibility. There is no shared IDamage/TakeDamage recipient contract found by `rg -n 'Damage\(|IDamage|TakeDamage'` across first-party Features, Core, Domain, Application and Systems C# files. Repository-wide first-party `.Damage(` callers found were tests, the controller's delegation and `Verification/Player/PlayerVitalsVerification.cs:59`; no runtime enemy damage producer was found in that search. SEC-0 instead shares a concrete PlayerStunReceiver contract (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:61-68,195`).

```csharp
// Features/Player/Vitals/PlayerVitalController.cs:150-153
var result = VitalsRecoveryRules.Damage(RulesSettings, ReadValues(), timing,
    publishing ? double.NaN : amount, GameplayEligible);
if (result.Outcome == VitalsRecoveryOutcome.Applied) Apply(result);
return result;
```

### What existing player-controller responsibilities and dependencies constrain routing primary input to an equipped action while retaining separate environmental interactions?

There is no monolithic player action controller in the inspected runtime ownership paths. UnityInputHost owns action enablement/freshness/UI suppression, binds movement/interactor/selection, and permits exactly one BindVitalUse owner (`Core/Input/UnityInputHost.cs:32-41,115-160`). PlayerVitalsComposition currently owns Use and resolves the selected hotbar slot via SelectedVitalItemUseCoordinator (`Features/Player/Vitals/PlayerVitalsComposition.cs:28-35`; `Application/Vitals/SelectedVitalItemUseCoordinator.cs:22-34`). Environmental Interact has its own callback and PlayerInteractor dependency (`UnityInputHost.cs:155`). SceneBindings creates containers and selection before features and binds gameplay endpoints after features (`Core/Composition/SceneBindings.cs:140-193`). Policy and scene lifetime therefore already affect each current input route.

```csharp
// Core/Input/UnityInputHost.cs:34-36
if (actions == null || callback == null) throw new InvalidOperationException("Use requires initialized input and a callback.");
if (vitalUseOwner != null) throw new InvalidOperationException("Use already has a scene owner.");
var owner = new object(); vitalUseOwner = owner; vitalUse = callback; CaptureHeldUse();
```

### What project conventions govern ScriptableObject configuration, stable serialized asset identity, validation and editor assignment, and what compatibility requirements apply to existing saved or authored references?

ItemDefinition and GameRuntimeConfig use CreateAssetMenu and serialized authoring fields (`Features/Items/Core/ItemDefinition.cs:6-35`; `Core/Composition/GameRuntimeConfig.cs:5-10`). Item identity is generated from the asset GUID only if empty; renamed display names do not determine save identity (`ItemDefinition.cs:28-45`). ItemDatabase validates compiled identity/stack rules and rejects null entries (`Features/Items/Database/ItemDatabase.cs:55-78`; `Domain/ItemCatalog.cs:10-28`), and its editor Populate From Project updates the serialized list and marks it dirty (`ItemDatabase.cs:103-117`). ItemCatalogValidator compiles all loot and checks pickup item membership/reference equality and positive quantity (`Editor/ItemCatalogValidator.cs:18-31`). UnityMetaValidator checks asset/meta pairing and unique GUIDs (`Editor/UnityMetaValidator.cs:15-24,57-65`). Saves refer to string item IDs, so existing saved IDs must remain resolvable by the current catalog (`Domain/ContainerSaveData.cs:7-15`; `ItemDatabase.cs:81-91`). Scene feature validation requires explicit unique same-scene references and rejects unbound feature components (`Core/Composition/SceneBindings.cs:104-116`). Existing stable enum numeric values are explicitly preserved for consumables (`Domain/Vitals/VitalConsumableSpec.cs:5`) and vital bars (`Features/Player/Vitals/Views/VitalBarView.cs:7-8`).

```csharp
// Features/Items/Core/ItemDefinition.cs:40-44
if (string.IsNullOrEmpty(itemId))
{
    string path = UnityEditor.AssetDatabase.GetAssetPath(this);
    if (!string.IsNullOrEmpty(path))
        itemId = UnityEditor.AssetDatabase.AssetPathToGUID(path);
}
```

### What Unity 2D physics configuration, collision layers, tile scale and blocking geometry govern directional melee targeting in the existing scenes?

Global queries hit triggers and start-inside colliders; auto-sync transforms is off; contact offset is .01 and the global collision matrix is all enabled (`ProjectSettings/Physics2DSettings.asset:21,41-56`). Layers include Default, Interactable and Player (`ProjectSettings/TagManager.asset:7-38`). SEC-0 uses an all-layer mask with an explicit no-trigger ContactFilter2D and ignores colliders under its own or target transform for sight/navigation (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:73-89`). The SEC-0 prefab uses a kinematic rigidbody (builder sets zero gravity/full kinematic contacts) and nontrigger .6x.34 box offset (0,.17) (`Editor/Sec0NpcBuilder.cs:50-52`; `Features/Npcs/Sec0/PF_Sec0.prefab:95-109`). Its demo has a one-unit grid and nontrigger box boundaries/cover: 14x.3 north/south, .3x10 east/west and 1x1 covers (`Scenes/Demos/Sec0Patrol.unity:4196,7777-7791`; `Editor/Sec0NpcBuilder.cs:118-133`). Other scene geometry includes solid collider shapes and tilemaps; the saved demo grid at `Scenes/Demos/Sec0Patrol.unity:2255` also has cell size (1,1,0). RoomGeometryWriter builds a noncolliding Generated Floor tilemap and wall SpriteRenderers, with 32-pixel anchor conversion (`Features/World/RoomAuthoring/Editor/RoomGeometryWriter.cs:74-93,115-119`); therefore its generated visual walls alone are not evidence of collision geometry.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:73-75
private ContactFilter2D Solids=>new ContactFilter2D{useLayerMask=true,layerMask=solidLayers,useTriggers=false};
private bool Ignore(Collider2D collider)=>collider==null || collider.transform.IsChildOf(transform)
    || (target!=null && collider.transform.IsChildOf(target));
```

### How are gameplay time, pause and frame-dependent updates handled, and what existing conventions affect configurable attack phases and movement slowdown?

PauseCoordinator owns disposable pause leases; any lease sets scale to zero, otherwise base scale applies (`Application/RuntimePolicy.cs:6-22`). TimeSystem publishes scale into Time.timeScale (`Core/Time/TimeSystem.cs:8-14`). Gameplay policy requires ready, no transition, no modal/primary screen and no pause (`Application/RuntimePolicy.cs:30-44`). Movement runs in FixedUpdate with scaled fixedDeltaTime (`Features/Creatures/Movement.cs:51-58`). SEC-0 runs Tick(fixedDeltaTime), validates finite positive elapsed, freezes when gameplay is unavailable and pauses visual animation speed; its brain owns windup/recovery timing (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:67,123-143`). Stun uses Update/deltaTime only while gameplay allowed and holds a movement block lease (`PlayerStunReceiver.cs:24-41`). No movement slowdown multiplier is present in MovementBehavior's inspected fields or update: it has a fixed serialized moveSpeed plus all-or-nothing blocks (`Features/Creatures/Movement.cs:15-27,56-58`). UI task notices separately use unscaledDeltaTime (`Core/Composition/SceneBindings.cs:198`).

```csharp
// Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs:33-34
if(!available()) { Clear();return; }
if(policy.Gameplay) Advance(Time.deltaTime);
```

### What existing physics-query and runtime-allocation patterns could support frequent tag queries and melee checks without introducing avoidable allocations?

SEC-0 preallocates 64-hit and 64-collider arrays, uses ContactFilter2D overloads of Raycast, BoxCast, OverlapBox and Rigidbody2D.Cast, and treats buffer saturation as blocked/no shot (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:24-25,85-115,163-165,189-195`). PlayerInteractor reuses a HashSet for proximity and selects a nearest target without new arrays on its normal Update path (`Features/Player/Interaction/PlayerInteractor.cs:18,38-42,131-152`). ItemCatalog and VitalConsumableCatalog use ordinal dictionary lookups (`Domain/ItemCatalog.cs:20,32-33`; `Domain/Vitals/VitalConsumableSpec.cs:19,31`). Relocation paths intentionally allocate: SurvivorAvatarAdapter uses OverlapBoxAll/LINQ (`Features/Player/Succession/SurvivorAvatarAdapter.cs:80-90`) and PlayerInteractor rebuild enumerates scene arrays (`Features/Player/Interaction/PlayerInteractor.cs:79-93`). No frequent general tag-query implementation was found in the classification search described above; these are existing query/storage patterns, not measured allocation results.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:24-25
private readonly RaycastHit2D[] hits=new RaycastHit2D[64];
private readonly Collider2D[] overlaps=new Collider2D[64];
// :87-89
Vector2 delta=to-from;int count=Physics2D.Raycast(from,delta.normalized,Solids,hits,delta.magnitude);
if(count==hits.Length) return false;
for(int i=0;i<count;i++) if(!Ignore(hits[i].collider)) return false;
```

### How are new runs initialized, starting items granted, equipment selected and actors spawned or restored, and where can configurable starting equipment participate?

GameCompositionRoot builds catalogs, loads save data and creates the ContainerSaveSession before binding scenes (`Core/Composition/GameCompositionRoot.cs:59-80,94-101`). New save resets session ownership and loads Wakeup for production (`:203-219`). SceneBindings registers player inventory and hotbar from their persistent IDs; it passes no initialLoot for them, and creates selection=-1 (`Core/Composition/SceneBindings.cs:140-145`; `Application/RuntimePolicy.cs:154`). Register supports optional initialLoot only when no saved owner exists (`Domain/ContainerCommandService.cs:25-54`), and Grant is an atomic catalog-validated API (`:132-144`). Current new-run GameRuntimeConfig contains database, loot tables and world seed, without a starting-equipment field (`Core/Composition/GameRuntimeConfig.cs:8-10`). SurvivorSuccessionComposition distinguishes disposable/fresh awakening, saved resume and scene arrival; it activates a SurvivorState, awakens or restores the existing configured avatar, initializes/stores lifecycle state, and clears selection (`Features/Player/Succession/SurvivorSuccessionComposition.cs:49-87`). Avatar awakening relocates and resets vitals; restoration uses saved location or safe fallback and restores vitals (`SurvivorAvatarAdapter.cs:30-47,94-109`). No configurable starting player-equipment grant was found in these initialization paths; existing loot applies to lockers (`SceneBindings.cs:209-213`) and pickups grant to inventory (`Domain/ContainerSaveSession.cs:52`).

```csharp
// Core/Composition/GameRuntimeConfig.cs:8-10
public ItemDatabase database;
public LootTable[] lootTables = new LootTable[0];
public int worldSeed = 12345;
// Core/Composition/SceneBindings.cs:144-145
var hotbarBinding = session.Register(ContainerSaveSession.PlayerHotbarId, hotbar.Capacity); bindings.Add(hotbarBinding);
selection = new HotbarSelectionState(hotbarBinding.Container); hotbar.Initialize(hotbarBinding.Container, selection);
```

### How are actor death, incapacitation, interruption, equipment replacement, despawning and scene transitions represented and communicated to active runtime behavior?

Player incapacitation uses PlayerStunReceiver's movement lease, Remaining timer and IsStunned; disable/destroy clears it (`Features/Npcs/Sec0/Runtime/PlayerStunReceiver.cs:16-17,24-48`). SEC-0 availability explicitly requires living active player vitals; unavailable target resets brain/navigation (`Sec0Composition.cs:37-38`; `Sec0Controller.cs:132-136`). Explicit survivor death enters SurvivorSuccessionCoordinator through RequestDeath; composition gates policy, resets held input, closes screens, prepares/commits inventory+hotbar handoff, awakens the same avatar and clears selection (`Features/Player/Succession/SurvivorSuccessionComposition.cs:89-103,105-164`). No automatic call from PlayerVitalsController's Damage to RequestDeath was found in the inspected damage path (`Features/Player/Vitals/PlayerVitalController.cs:147-153`). Inventory replacement is communicated through slot/capacity events (`Domain/IContainerReader.cs:31-39`); selection only clears when the active slot becomes empty/unavailable (`Application/RuntimePolicy.cs:163-165`). Scene transitions set policy transition, close current screens, flush, then load Single; scene release unbinds input and releases features (`Core/Composition/GameCompositionRoot.cs:180-201`; `Core/Composition/SceneBindings.cs:309-334`). SEC-0 OnDisable zeroes velocity; OnDestroy releases binding/effects (`Sec0Controller.cs:208-218`). There is no general action-interrupted event in these inspected contracts.

```csharp
// Features/Player/Succession/SurvivorSuccessionComposition.cs:100-103
public void SetTransitionGated(bool gated)
{
    context.Policy.SetTransition(gated);
    if (gated) { context.Input.ResetSurvivorInput(); context.ClosePlayerScreens(); }
}
```

### What current animation and visual presentation mechanisms can display a compact wrench swing without controlling gameplay timing?

The current player animation bridge implements only SetMovement and writes Animator movement parameters (`Core/Animation/Human/HumanAnimationController.cs:5-26`; `Core/Animation/ICreatureAnimationController.cs`, whose implementation is invoked from `Features/Creatures/Movement.cs:58`). SEC-0 demonstrates runtime-selected directional animation names, HasState guarding, and Animator.Play; gameplay state is independently advanced by SecurityBotBrain (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:140-143,197-206`). Its pulse visuals are instantiated at a directional offset/rotation and destroyed after .4 seconds, while the actual hit is resolved immediately by a physics query (`:181-195`). These are observed display mechanisms; no wrench-swing presentation component was found in the inspected Items/Player/Sec0 runtime paths.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:204-206
int hash=Animator.StringToHash("Base Layer."+clip);
if(visual.HasState(0,hash)) {visual.Play(hash,0,0);animationName=clip;}
else Debug.LogError("SEC-0 animation state missing: "+clip,this);
```

### What existing mechanisms support enemy flash, knockback, impact audio and contact effects, and how do they handle absent assets or unsupported recipients?

Not found within first-party SEC-0 runtime C# and prefab source: incoming-hit flash, knockback, impact audio, incoming contact effects, optional recipient effect interface, or corresponding asset fields. Search was `rg -n 'Damage|Health|Death|dead|drop|Drop|Audio|flash|knock' Assets/_Project/Features/Npcs/Sec0 -g '*.cs' -g '*.prefab'`, returning only player health availability. The existing outgoing stun pulse is mandatory authoring: ValidateAuthoring rejects null visual/controller/stunFxPrefab (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:47-57`), Fire directly instantiates it (`:185-186`), and unsupported nonplayer solid hits simply do not call the receiver (`:193-195`). Animator rendering returns for absent/disabled visual, but missing named states log errors (`:197-206`). Consumable recipients separately return Unsupported for unsupported effect kinds (`Features/Player/Vitals/PlayerVitalController.cs:192-200`); that is a vital-effect contract, not an enemy-hit feedback contract.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:52
|| visual==null || visual.runtimeAnimatorController==null || !visual.transform.IsChildOf(transform) || stunFxPrefab==null
// :199
if(visual==null || !visual.isActiveAndEnabled) return;
```

### How does the existing HUD obtain and update player item or equipment information?

SceneBindings initializes container controllers with ItemPresentationCatalog and selection only for controllers whose DefaultSource is hotbar, then binds default nonmodal sources (`Core/Composition/SceneBindings.cs:145-157`). ContainerPresenter subscribes to SlotChanged, CapacityChanged and selection.Changed; it maps each current slot to index/itemId/quantity/selected/active (`Application/ContainerPresenter.cs:59-83,94-104`). ContainerSlotView resolves icon by ID, writes quantity only above 1, and calls HotbarSlotDecorator with active state (`Systems/Containers/Views/ContainerSlotView.cs:19-27`). ItemPresentation contains display name, description, icon and pickup prefab only (`Features/Items/Database/ItemPresentationCatalog.cs:7-22`). No dedicated equipped-action HUD data source was found in these HUD paths.

```csharp
// Application/ContainerPresenter.cs:81-82
var slot = reader.GetSlot(index); snapshots[index] = slot;
view.Render(new SlotPresentation(index, slot?.Stack?.ItemId, slot?.Stack?.Quantity ?? 0, selected == index, selection?.ActiveIndex == index));
```

### How do existing damageable actors map their colliders to a damage recipient, including multiple colliders, actor self-colliders and damageable entities without classification tags?

A general damageable-collider mapping is not found within first-party Features/Core/Domain/Application/Systems C# searches for `IDamage|TakeDamage|Damage\(`. Existing player health has no collider-resolver contract (`Features/Player/Vitals/PlayerVitalController.cs:147-153`). SEC-0's outgoing stun resolves one bound PlayerStunReceiver ahead of time and checks whether the nearest hit transform belongs to the bound target; it skips colliders under itself (`Features/Npcs/Sec0/Runtime/Sec0Controller.cs:61-75,189-195`). Sight/navigation ignores all child colliders under itself and target; no Unity tag check is needed there. This is hierarchy membership, not deduplication of several damage recipients. Proximity sensors use `other.GetComponent<PlayerInteractor>()`, while their own target uses GetComponentInParent<IInteractable> (`Systems/Interaction/Core/InteractableProximitySensor.cs:23-31`). No untagged-damage-recipient fallback or multi-collider damage deduplication was found within those searched scopes.

```csharp
// Features/Npcs/Sec0/Runtime/Sec0Controller.cs:192-195
for(int i=0;i<count;i++)
    if(hits[i].collider!=null && !hits[i].collider.transform.IsChildOf(transform) && hits[i].distance<nearest)
    {nearest=hits[i].distance;hit=hits[i].collider;}
if(hit!=null && hit.transform.IsChildOf(target) && receiver.TryStun(stunDuration)) StunsLanded++;
```

### What existing inventory or equipment operations can remove or replace an item during an action, and what guarantees do those operations provide about item ownership and actor lifetime?

ContainerCommandService can Remove, consume a vital item, Transfer (merge or swap), Restore, Resize and hand off both inventory/hotbar (`Domain/ContainerCommandService.cs:78-124,163-201,210-255`). Addresses contain owner generation, slot index and revision (`Domain/IContainerReader.cs:6-21`); stale addresses return Stale, unavailable owners return Unavailable, and reentrant commands return Busy (`ContainerCommandService.cs:60-75,126-130`). Transfer plans both sides before one Commit; handoff validates all captured clocks/capacities before committing and records an idempotent receipt (`:101-124,223-235`). Unregister marks the old reader unavailable and emits capacity change (`:60-66`). Selection observes slot/capacity changes but does not own an item or lock a selected slot (`Application/RuntimePolicy.cs:151-166`). These guarantees concern container ownership, not Unity actor lifetime; no actor lifetime lock is represented in IContainerReader or SlotAddress (`Domain/IContainerReader.cs:15-40`).

```csharp
// Domain/ContainerCommandService.cs:126-130
private CommandStatus Address(SlotAddress address, out ContainerState state)
{
    if (!Resolve(address.Container, out state)) return CommandStatus.Unavailable;
    if (address.Index < 0 || address.Index >= state.Slots.Length) return CommandStatus.InvalidSlot;
    return state.Revisions[address.Index] == address.Revision ? CommandStatus.Applied : CommandStatus.Stale;
}
```

### Which current systems share item configuration across actors, and how do they separate mutable per-actor execution state from shared definitions?

The composition root compiles one session ItemCatalog/ItemPresentationCatalog and passes them into containers, loot, pickups and scenes (`Core/Composition/GameCompositionRoot.cs:59-62,94-99,175`). ItemSpec and ItemPresentation have get-only properties, and the database snapshots authored fields during compilation (`Domain/ItemCatalog.cs:5-15`; `Features/Items/Database/ItemPresentationCatalog.cs:7-22`). ContainerCommandService copies catalog specifications into its own ItemCatalog, keeps owner states by ID, and stores separate arrays per owner (`Domain/ContainerCommandService.cs:17-24,32-55`). Immutable ItemStack instances carry quantity/definition, while revisions and slots belong to container state (`Domain/ItemStack.cs:4-11`; `Domain/IContainerReader.cs:15-29`). SelectedVitalItemUseCoordinator keeps per-instance usingItem/released flags and a transaction, separate from the shared effect catalog (`Application/Vitals/SelectedVitalItemUseCoordinator.cs:8-20,22-36`). No mutable attack execution state exists in the inspected shared item definitions.

```csharp
// Application/Vitals/SelectedVitalItemUseCoordinator.cs:14-15
private readonly VitalConsumableTransaction transaction = new VitalConsumableTransaction();
private bool usingItem, released;
// Domain/ContainerCommandService.cs:24
public ContainerCommandService(IItemCatalog catalog) => this.catalog = new ItemCatalog((catalog ?? throw new ArgumentNullException(nameof(catalog))).Items);
```

### What existing automated test infrastructure and conventions support asset validation, serialized-reference checks, 2D physics, timed runtime behavior and independent actors sharing configuration?

Domain tests are Editor-only NUnit tests with noEngineReferences and only SOLITUDE.Domain (`Tests/Domain/SOLITUDE.Tests.Domain.asmdef:1-13`). PlayMode tests reference runtime/application/domain, Input System, UI and TMP (`Tests/PlayMode/SOLITUDE.Tests.PlayMode.asmdef:1`). Tests use authored prefab instantiation, generated actor fixtures, Physics2D.SyncTransforms, direct Tick/Advance for deterministic timers and UnityTest waits for actual physics frames (`Tests/PlayMode/Sec0RuntimeTests.cs:27-46,73-88`). Input tests create temporary ScriptableObject definitions, set private IDs, compile databases, and inject real keyboard/gamepad state (`Tests/Editor/PlayerVitalsInputTests.cs:30-38,49-79`). ValidationTests checks deterministic scene validation and authored scene script references (`Tests/Editor/ValidationTests.cs:26-37`). WakeupValidation emits structured findings and validates manifests, catalogs and preview scenes (`Editor/WakeupValidation.cs:18-24,49-85`); UnityMetaValidator validates metadata (`Editor/UnityMetaValidator.cs:44-65`). `Tools/Verification/verify.py:16-23,37-61` copies an isolated project, runs pure tests, authoring, EditMode/PlayMode and builds. Shared immutable definitions/per-owner state can be instantiated via the current test fixtures, but a dedicated independent-actors-sharing-attack-configuration test was not found in the searched item, SEC-0 and vitals test files; no attack configuration currently exists in those runtime definitions.

```csharp
// Tests/PlayMode/Sec0RuntimeTests.cs:75-78
Charge();policy.SetTransition(true);bot.Tick(100);Assert.AreEqual(SecurityBotState.Charging,bot.State);Assert.AreEqual(0,bot.ShotsFired);
policy.SetTransition(false);bot.Tick(1);Assert.IsTrue(stun.IsStunned);
float remaining=stun.Remaining;policy.SetTransition(true);stun.Advance(100);Assert.AreEqual(remaining,stun.Remaining);
policy.SetTransition(false);stun.Advance(.2f);Assert.AreEqual(remaining-.2f,stun.Remaining,.0001f);
```

### What existing tests or validation scenarios cover movement, vitals, healing and consumables, enemy behavior and death, room generation, inventory and item handling?

Observed coverage declarations include movement stopping/resumption during stun (`Tests/PlayMode/Sec0RuntimeTests.cs:80-88`); scaled runtime vitals, damage delay, frame subdivision equivalence and invalid inputs (`Tests/PlayMode/PlayerVitalsRuntimeTests.cs:61,83,98,200,318`); actual Wakeup healing/pause/scene bindings (`Tests/PlayMode/PlayerVitalsSliceTests.cs:57-91`); real Use input and holding/fresh-press rules (`Tests/Editor/PlayerVitalsInputTests.cs:40-79`); selected-slot resolution, unsupported items, policy/release and foreign-reader rejection (`Tests/Application/SelectedVitalItemUseCoordinatorTests.cs:39-76`); atomic vital-effect reservations (`Tests/Domain/VitalConsumableTransactionTests.cs:19-50`); SEC-0 detection, telegraphed shot, dodging, freeze and reset (`Tests/Domain/SecurityBotBrainTests.cs:8-56`; `Tests/PlayMode/Sec0RuntimeTests.cs:48-88`); deterministic room geometry/erasure and validation preservation (`Tests/Editor/RoomGeometryTests.cs:10-24`; `Tests/Editor/RoomValidationTests.cs:13-74`); and catalog validation, atomic nonstackable grants, stale addresses, replacement generation, restore and observer safety (`Tests/Domain/ContainerBoundaryTests.cs:32-206`). Enemy health/death coverage was not found in the SEC-0 tests inspected; their hit assertion is explicitly nonlethal. None of these tests were run in this pass.

```csharp
// Tests/Domain/ContainerBoundaryTests.cs:83
[Test] public void NonstackableGrantUsesOnePerSlotAndIsAtomic()
// Tests/Domain/SecurityBotBrainTests.cs:51
[Test] public void ResetCancelsAnUnfinishedAttack()
// Tests/Editor/RoomValidationTests.cs:74
[Test] public void ValidationDoesNotChangeSerializedStateOutputOrDirtiness()
```

### Which existing Unity scenario and validation tools can exercise starting equipment, approaching the introductory enemy, missing, hitting with feedback and defeating it?

The authored disposable SEC-0 demo is `Assets/_Project/Scenes/Demos/Sec0Patrol.unity`; Sec0NpcBuilder exposes prefab/demo authoring menu commands and CapturePreview for an actual running scene (`Editor/Sec0NpcBuilder.cs:22-25,62-66,95-113`). Its authored-scene test loads that scene, checks runtime ready/disposable state, places player in front of SEC-0, waits for a stun, checks unchanged health and reload reset (`Tests/PlayMode/Sec0EncounterTests.cs:18-39`). Runtime tests exercise cover cancellation, sidestep misses, direct stun hit and movement recovery (`Tests/PlayMode/Sec0RuntimeTests.cs:55-88`). WakeupValidation and ItemCatalogValidator expose menu/CI entry points (`Editor/WakeupValidation.cs:48-55`; `Editor/ItemCatalogValidator.cs:12-13`); verify.py runs NUnit suites plus player scenarios (`Tools/Verification/verify.py:16-35,54-61`). No scenario for configurable starting equipped weapon, player melee miss/hit feedback or defeating SEC-0 was found within the inspected SEC-0 scenario/test/builder files and player/item runtime paths. Those current files have no player attack or SEC-0 damage/death route, so the existing demo cannot demonstrate those outcomes as implemented.

```csharp
// Tests/PlayMode/Sec0EncounterTests.cs:32-36
Vector2 target=(Vector2)bot.transform.position+bot.Facing*1.1f;
player.transform.position=target;player.GetComponent<Rigidbody2D>().position=target;Physics2D.SyncTransforms();
for(int i=0;i<150 && bot.StunsLanded==0;i++) yield return new WaitForFixedUpdate();
Assert.That(bot.StunsLanded,Is.GreaterThan(0),"Composed target should receive a short-range stun.");Assert.IsTrue(stun.IsStunned);
Assert.AreEqual(100,player.GetComponent<PlayerVitalsController>().Vitals.Health.Current);
```

## New Questions

- Does “introductory security enemy” refer to the current SEC-0 patrol unit or to another explicitly identified source asset?
