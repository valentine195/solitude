# QRSPI Research: player-vitals

## Answers

### How are health damage, healing, maximum health, and any existing passive recovery handled today?

Health is a continuous `Vital` with `Current`, `Max`, and `Normalized`. `Set` clamps a requested value to zero/maximum and publishes changes only when the resulting current value differs. `Add` applies a signed delta through `Set`; negative deltas decrease the value and positive deltas increase it. There is no separate damage/healing API in this model. `SetMax` changes the maximum and optionally clamps current health. The constructor assigns its values without validation or clamping.

Files: Assets/_Project/Features/Player/Vitals/Vital.cs:7, :16, :22, :41

```csharp
public float Normalized => Max <= 0f ? 0f : Current / Max;
var clamped = value < 0f ? 0f : (value > Max ? Max : value);
if (clamped == Current) return;
Current = clamped;
Changed?.Invoke(this);
public void Add(float delta) => Set(Current + delta);
```

The controller initializes and resets both vitals to serialized current values; it has no passive update method. Not found within `Assets/_Project` C# source: passive health recovery, a recovery rate, damage timestamp, or healing delay. Searches used case-insensitive `vitals|hunger|heal|damage|regen|medkit|medical|food|consum|segment`, followed by `Depleted|SetMax\(|\.Health|\.Oxygen|new Vital\(|Time\.(deltaTime|unscaledDeltaTime)|WaitForSeconds|Tick\(` in `Features`, `Core`, and `Application`. All recursive searches included `*.cs` and excluded every `.qrspi` tree.

Files: Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27

```csharp
if (Vitals == null) Vitals = new PlayerVitals(maxHealth, maxOxygen);
Vitals.Health.Set(currentHealth);
Vitals.Oxygen.Set(currentOxygen);
```

### Do health segments exist today, and how do they affect damage, healing, and displayed health?

Not found within `Assets/_Project` C# source or the current vitals prefab: health segments or segment boundaries. Searches: case-insensitive `segment` in the first-party C# search above, and `threshold|status|segment|marker|boundary` in `Features/Player/Vitals` and `UI` with explicit `*.cs`/`*.prefab` includes and all `.qrspi` exclusions. The authoritative model stores two floats; the view displays `Current / Max` directly, without rounding to segments.

Files: Assets/_Project/Features/Player/Vitals/Vital.cs:7; Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:47

```csharp
public float Max { get; private set; }
public float Current { get; private set; }
private void HandleChanged(Vital vital) => fillImage.fillAmount = vital.Normalized;
```

### What hunger, food consumption, food restoration, or hunger depletion behavior already exists?

The player vitals model contains only health and oxygen. Not found within `Assets/_Project` C# source: a hunger value, hunger depletion, food restoration, or food-use effect. Search patterns and source boundary are recorded in the first answer. Serialized searches for `food|hunger|medical|medkit` in `Assets/_Project` `.asset`, `.prefab`, and `.unity` files also found no corresponding authored food/medical definitions. Enumeration of `Features/Items/Definitions` `.asset` files found Battery, ReplacementFuse, and O2Canister definitions and their pickup assets.

Files: Assets/_Project/Features/Player/Vitals/PlayerVitals.cs:7

```csharp
public Vital Health { get; }
public Vital Oxygen { get; }
```

`food` does occur as a generic item ID in equipment-handoff tests; those occurrences grant, move, or remove inventory quantities, rather than restoring a vital. Item definitions contain display/world/inventory fields and an item ID, with no restoration amount or use-effect field.

Files: Assets/_Project/Tests/Domain/SurvivorEquipmentHandoffTests.cs:19, :26, :129; Assets/_Project/Features/Items/Core/ItemDefinition.cs:8, :16, :19, :29

```csharp
new ItemSpec("food", true, 5)
commands.Grant(inventory.Handle, "food", 5);
```

The O2Canister asset has descriptive text saying it can replenish oxygen, but no authored replenishment amount. No oxygen-use caller was found by the `.Oxygen`, consumption, and vitals searches above. The description alone does not establish an implemented effect.

Files: Assets/_Project/Features/Items/Definitions/O2Canister/O2Canister.asset:15

```yaml
DisplayName: O2 Canister
Description: A small canister containing O2. Can be used to replenish oxygen levels.
Stackable: 1
MaxStackSize: 99
```

### How are health and O2 currently displayed and updated, including their status indicators?

The enabled build scene is Wakeup. It has HP and O2 instances of the VitalSlider prefab. The prefab defaults to `kind: 0` (Health), labels the row `HP`, and references a fill image. Wakeup's O2 instance overrides its label to `O2` and kind to `1` (Oxygen). The prefab fill is a horizontally filled Unity UI Image, initially authored at 0.5; binding immediately reconciles it with the vital. Its authored color is red.

Files: ProjectSettings/EditorBuildSettings.asset:8; Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:140, :148, :223, :425; Assets/_Project/Scenes/Wakeup.unity:10427, :10611, :14678

```yaml
# VitalSlider.prefab
m_Type: 3
m_FillMethod: 0
m_FillAmount: 0.5
fillImage: {fileID: 9112781867610621289}
kind: 0
# Wakeup.unity O2 instance
propertyPath: m_text
value: O2
propertyPath: kind
value: 1
```

The view subscribes to `Vital.Changed`, updates fill immediately, unsubscribes on disable, and retries binding in `LateUpdate` when a player is not yet available. It changes only `fillAmount`; it has no dynamic text, status, or color handling. Not found within the current vitals C# and prefab: a status indicator, low-value threshold, or adjacent dynamic status text (`threshold|status|segment|marker|boundary` search).

Files: Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17, :22, :27, :41, :47

```csharp
_vital.Changed += HandleChanged;
HandleChanged(_vital);
if (_vital != null) _vital.Changed -= HandleChanged;
```

Wakeup contains RGB overrides under the O2 prefab instance, targeting fileID `2460997679024269163`. The current prefab's fill component is `9112781867610621289`, and the former ID is absent from that prefab. Those override records do not establish a changed current fill color. Runtime rendering was not checked.

Files: Assets/_Project/Scenes/Wakeup.unity:10511; Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:127, :425

### Which existing gameplay systems can change player health or consume medical items and food, and where are those interactions controlled?

The current health-changing production callers found are controller initialization/Inspector validation and survivor awakening. Awakening calls `ResetForAwakening`, retaining existing Vital objects and setting their current values to the serialized values. Succession initialization also awakens the retained avatar. Death requests have an explicit coordinator entry point and an Inspector context-menu action; no subscription to `Vital.Depleted` was found in first-party C# source.

Files: Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27, :35, :44; Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:24, :36; Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:53, :57

```csharp
avatar.Awaken(awakeningPoint);
public SuccessionOutcome RequestDeath(Guid expectedSurvivor)
    => Coordinator?.RequestDeath(expectedSurvivor) ?? SuccessionOutcome.Failed;
```

Not found within `Assets/_Project` C# source: gameplay medical-item consumption, food consumption, or damage/healing callers beyond the setters/reset described above. Searches used the first answer's patterns plus `commands.Remove|Commands.Remove|\.TryRestore|RequestDeath\(` in `Features`, `Application`, and `Domain`. Hotbar input selects a slot; it does not invoke a vital effect.

Files: Assets/_Project/Core/Input/UnityInputHost.cs:115; Assets/_Project/Features/Hotbar/Hotbar.cs:16

```csharp
public void SetActive(int index) => selection?.Select(index);
```

An existing item-consumption interaction installs one configured required item from player inventory for world-system restoration. It removes one item and commits restoration state together, without modifying vitals. Scrubber installation delegates to that same restoration state.

Files: Assets/_Project/Features/World/Restoration/RestorationJunction.cs:25; Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17, :31; Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:23

```csharp
if (slot.IsEmpty || slot.Stack.ItemId != itemId) continue;
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
```

### How are player vital values and tuning settings configured, initialized, saved, and restored?

The controller has four serialized floats, each defaulting to 100: maximum/current health and maximum/current oxygen. Wakeup serializes all four as 100. Awake creates the PlayerVitals model through ResetForAwakening. Creation constructs each Vital at its maximum, then the reset sets current values. Subsequent resets keep the objects and their existing maxima. OnValidate changes current values only; editing serialized maxima after model creation does not call SetMax. No additional vital tuning configuration was found in the searched vitals/source references.

Files: Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:19, :27, :35, :44; Assets/_Project/Features/Player/Vitals/PlayerVitals.cs:10; Assets/_Project/Scenes/Wakeup.unity:6178

```csharp
[SerializeField] private float maxHealth = 100f;
[SerializeField] private float maxOxygen = 100f;
[SerializeField] private float currentHealth = 100f;
[SerializeField] private float currentOxygen = 100f;
```

SaveGameData version 2 contains world seed, container records, and collected pickup IDs. Capture clones that data, captures registered containers, and copies collected IDs. SaveGameService flushes that capture. There is no field or capture/restore connection for player vitals in these persistence sources; initialization uses scene/controller values instead.

Files: Assets/_Project/Domain/SaveGameData.cs:9; Assets/_Project/Domain/ContainerSaveSession.cs:78; Assets/_Project/Core/SaveLoad/SaveGameService.cs:39

```csharp
public int version = CurrentVersion;
public int worldSeed;
public List<ContainerSaveRecord> containers = new();
public List<string> collectedPickupIds = new();
store.Write(session.Capture());
```

### What existing timing and pause behavior distinguishes active gameplay and governs passive player-state changes and delays following damage?

InputPolicyCoordinator defines gameplay eligibility using readiness, transition state, modal state, primary-screen state, and pause state. PauseCoordinator returns zero scale while any pause lease exists; TimeSystem applies that scale to Unity Time.timeScale. Manual pause and container modals acquire leases. Composition initializes this shared policy/clock and marks readiness after startup.

Files: Assets/_Project/Application/RuntimePolicy.cs:10, :36, :96; Assets/_Project/Core/Time/TimeSystem.cs:10; Assets/_Project/Core/Game/GameManager.cs:13; Assets/_Project/Core/Composition/GameCompositionRoot.cs:57, :76

```csharp
public bool Ready => ready && !transition;
public bool Gameplay => Ready && !modal && !primaryScreen && !pause.IsPaused;
public float Scale => leases.Count == 0 ? baseScale : 0;
private void Apply() => Time.timeScale = pause.Scale;
```

No vital timer or post-damage delay was found within the source searches in the first answer. Vital.Set/Add themselves have no clock/pause dependency, so explicit changes are not gated by gameplay eligibility. Other current systems demonstrate different clocks: scrubber recovery advances with scaled `Time.deltaTime`; task notices receive unscaled delta time but decrement only when visible and `policy.Gameplay` is true. Survivor transition gating changes input policy and closes screens; that method does not acquire a pause lease.

Files: Assets/_Project/Features/Player/Vitals/Vital.cs:22; Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:47; Assets/_Project/Core/Composition/SceneBindings.cs:190; Assets/_Project/Application/Tasks/TaskNoticeCoordinator.cs:87; Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:68

```csharp
private void Update() => taskNotices?.Advance(Time.unscaledDeltaTime);
if (visible && policy.Gameplay && queue.Count != 0)
    queue[0].Remaining -= elapsedSeconds;
```

### How does the vitals UI obtain changing values and settings, and what existing UI conventions support boundaries, threshold markers, and adjacent status text?

The view resolves PlayerVitalsController.Instance, which lazily uses FindAnyObjectByType. It chooses health/oxygen from serialized kind and observes the model directly. It has only fillImage and kind as serialized UI settings. No settings service, polling of vital values after binding, or maximum-change event exists in this path.

Files: Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:9; Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:12, :27

```csharp
if (fillImage == null || controller == null || controller.Vitals == null) return;
_vital = kind == VitalKind.Health ? controller.Vitals.Health : controller.Vitals.Oxygen;
_vital.Changed += HandleChanged;
```

Current vitals layout uses a HorizontalLayoutGroup with spacing 12 for label/bar, a TMP label, a white background Image, and one stretched fill child. Wakeup arranges vitals with a VerticalLayoutGroup, padding 12 and spacing 10. No boundary/threshold marker or adjacent status-text convention was found in `Features/Player/Vitals` and `UI` current C#/prefab searches (`threshold|status|segment|marker|boundary`). This is a bounded absence finding, not a claim about all other feature-local UI.

Files: Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:32, :53, :60, :114, :214, :346, :394; Assets/_Project/Scenes/Wakeup.unity:10350

```yaml
m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
m_Spacing: 12
# Bar children
m_Children:
- {fileID: 4997609250372371078}
```

### How does existing health handling behave at zero health, full health, exact segment boundaries if present, and when maximum health changes?

For a nonnegative maximum, Set clamps below-zero requests to zero and above-maximum requests to Max. An unchanged result emits neither Changed nor Depleted. A changed result reaching zero emits Changed first, then Depleted once; increasing above zero clears the depleted flag. A Vital constructed at zero is already unchanged on Set(0), so that call does not emit depletion. No death listener was found by `Depleted` search in `Assets/_Project` C# source.

Files: Assets/_Project/Features/Player/Vitals/Vital.cs:16, :22

```csharp
if (Current <= 0f && !_wasDepleted)
{
    _wasDepleted = true;
    Depleted?.Invoke(this);
}
else if (Current > 0f) _wasDepleted = false;
```

Full-health positive additions clamp back to the same current value and emit nothing. Segment boundaries have no distinct behavior because no segment model was found. SetMax defaults to clamping; reducing the maximum below Current changes Current and emits its events. Raising Max, or lowering it while Current remains within range, leaves Current unchanged and emits no Changed event, although Normalized changes. Consequently a bound bar remains at its previous fill until a later Changed event or rebind. SetMax(..., false) can retain Current above Max. Max is not validated; Normalized returns zero for nonpositive maxima. The constructor also accepts out-of-range starting values without clamp.

Files: Assets/_Project/Features/Player/Vitals/Vital.cs:9, :16, :43; Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:37, :47

```csharp
public void SetMax(float newMax, bool clampCurrent = true)
{
    Max = newMax;
    if (clampCurrent) Set(Current);
}
```

### What existing tests, playable checks, or validation conventions verify health changes, consumable effects, passive recovery timing, and vitals UI updates?

VitalBarReadinessTests is a Unity PlayMode coroutine test. It creates a view before the player, sets oxygen to 40/30/20, checks fills 0.4/0.3/0.2, checks disabled-view unsubscription and re-enable reconciliation without healing, then checks reset retains the Vital object and produces full fill.

Files: Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs:14, :45

```csharp
oxygen.Set(30);
Assert.AreEqual(.3f, image.fillAmount);
Assert.AreEqual(20, oxygen.Current, "Binding must not heal the survivor.");
player.ResetForAwakening();
Assert.AreSame(oxygen, player.Vitals.Oxygen);
Assert.AreEqual(1, image.fillAmount);
```

SurvivorSuccessionSliceTests explicitly sets health/oxygen to 12/9, requests death, then asserts retained health identity and both values reset to 100. Ship compatibility tests also assert full reset. Scrubber encounter tests verify room interactions leave health, oxygen, and save content unchanged. These tests do not establish health-driven death or consumable recovery.

Files: Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs:61, :71, :85; Assets/_Project/Tests/PlayMode/SurvivorSuccessionShipCompatibilityTests.cs:126; Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:183, :199, :221, :236

```csharp
vitals.Health.Set(12); vitals.Oxygen.Set(9);
Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(first.Id));
Assert.AreSame(health, One<PlayerVitalsController>().Vitals.Health);
Assert.AreEqual(100, vitals.Health.Current);
```

TaskNoticeCoordinatorTests checks that primary-screen, pause, transition, and not-ready time do not consume notice dwell; this verifies an existing gameplay-gated timer, not vital recovery. SurvivorSuccessionComposition exposes Inspector actions to retire a survivor and retry awakening. SolitudeCompositionSmoke is a player verification host with assertion checks, isolated-save-directory arguments, and result output; inspected checks cover composition, input, pause, inventory, and persistence rather than vital effects.

Files: Assets/_Project/Tests/Application/TaskNoticeCoordinatorTests.cs:47; Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:58, :65; Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:24, :29, :32, :74

```csharp
using (pause.Acquire()) { notices.Advance(200); Assert.IsNull(display.Last); }
```

Not found within `Assets/_Project/Tests`, `Verification`, and `Editor` C# searches: dedicated tests/validation for health delta clamping, zero/depletion events, maximum-change UI behavior, health segments, medical/food effects, or passive vital recovery timing. Searches used case-insensitive `vital|health|oxygen|hunger|heal|regen|medical|food|damage`, plus the first answer's broader `consum|segment` search and direct inspection of VitalBarReadinessTests. Reading these tests establishes assertions present in source only. No tests or playable checks were executed.

## New Questions
