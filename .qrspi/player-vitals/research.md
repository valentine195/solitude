# QRSPI Research: player-vitals

## Answers

All paths below are relative to `/Users/jeremyvalentine/SOLITUDE`. Evidence was read from current source and serialized assets; tests were not executed. The visible starting context contained no prior feature intent or Query reasoning. The explicit questions were read first, followed by `CODEBASE_MAP.md` for navigation only. No other Markdown or prior research was read.

For bounded absence findings, the following searches were performed with `rg -n`, excluding `.qrspi/**`, `**/.qrspi/**`, `*.md`, and `**/*.md`:

- **S1:** `Health|Hunger|Oxygen|Vitals|Heal|TakeDamage` in `Assets/_Project/{Features/Player,Features/Hotbar,Core,Tests,Verification,Domain,Application}`, including only `*.cs`.
- **S2:** `Hunger|hunger|\.Health|\.Oxygen|SetMax|Consum|Medical|Food|post.damage|regen|Regeneration` throughout `Assets/_Project`, including only `*.cs`.
- **S3:** `TakeDamage|Heal\(|Health|Hunger|hunger|Depleted|SetMax\(|Vitals` in `Assets`, including only `*.cs`, excluding `**/Art/**`, `**/ThirdParty/**`, and `**/Plugins/**`.
- **S4:** `Food|food|Medical|medical|hunger|Hunger|Heal|Damage|Regenerat|segment|threshold` in `Assets/_Project/{Features/Items,Features/Player,UI,Tests}`, including only `*.cs` and `*.asset`.
- **S5:** `Status|status|Threshold|threshold|Segment|segment|m_text: (HP|O2|NORMAL|STABLE|Normal|Stable)` in `Assets/_Project/{UI,Features/Player/Vitals}`, including only `*.cs`, `*.prefab`, and `*.unity`; direct inspection of `VitalSlider.prefab` and its Wakeup scene overrides.
- **S6:** `DisplayName:|[Ff]ood|[Mm]edical|[Mm]edkit|[Rr]ation|[Hh]unger|[Hh]eal` in `Assets/_Project/Features/Items`, including only `*.asset`.
- **S7:** `Time\.deltaTime|Time\.unscaledDeltaTime|\.Gameplay|SetTransition|SetReady|Depleted|Vital\b|Health|Hunger|hunger|Consume|Consumable|UseItem|Healing|Damage|segment|threshold` in `Assets/_Project/{Core/Composition,Core/SaveLoad,Domain,Application,Features,Tests,Verification}`, including only `*.cs`.

### How are health damage, healing, maximum health, and any existing passive recovery handled today?

Health is a `Vital`, exposing `Max`, `Current`, `Normalized`, `Changed`, and `Depleted`. `Set` clamps a supplied value to zero/maximum and returns without notification when unchanged. `Add` applies a signed delta through `Set`; negative and positive deltas therefore share the same clamping. Construction sets maximum and initial current directly, without validation or clamping. `SetMax` assigns maximum and optionally clamps current. The controller defaults health maximum/current to 100 and sets current during initialization, awakening reset, and editor validation.

Dedicated health damage/healing gameplay methods, passive health recovery, recovery tuning, and post-damage delays: **Not found within S1–S4 and S7**. The model supports direct changes, but the inspected runtime callers only initialize/reset values.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:7`, `:16`, `:22`, `:41`, `:43`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:19`, `:27`, `:35`.

```csharp
var clamped = value < 0f ? 0f : (value > Max ? Max : value);
if (clamped == Current) return;
Current = clamped;
Changed?.Invoke(this);
// ...
public void Add(float delta) => Set(Current + delta);
```

### Do health segments exist today, and how do they affect damage, healing, and displayed health?

Health is one floating-point value. Display uses `Current / Max` (or zero for nonpositive maximum), with no segment calculation. Segment state, boundary behavior, segment damage/healing rules, and segment UI markers: **Not found within S2, S4, S5, and S7**. The authored bar has one fill Image.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:7`; `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:47`; `Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:87`, `:148`, `:425`.

```csharp
public float Normalized => Max <= 0f ? 0f : Current / Max;
private void HandleChanged(Vital vital) => fillImage.fillAmount = vital.Normalized;
```

### What hunger depletion, food consumption, and hunger restoration behavior already exists?

`PlayerVitals` contains Health and Oxygen only. Hunger values, hunger depletion/restoration, and food consumption behavior: **Not found within S1–S4, S6, and S7**. `ItemDefinition` has presentation, pickup, stacking, and identity fields, with no authored hunger/health restoration fields. The item asset search found Battery, Replacement fuse, and O2 Canister names; no food or medical asset match within that directory. Test fixtures use an item ID `food` for container handoff, not hunger restoration.

Files: `Assets/_Project/Features/Player/Vitals/PlayerVitals.cs:7`; `Assets/_Project/Features/Items/Core/ItemDefinition.cs:8`, `:16`, `:19`, `:29`; `Assets/_Project/Features/Items/Definitions/Battery/Battery.asset:15`; `Assets/_Project/Features/Items/Definitions/ReplacementFuse/ReplacementFuse.asset:15`; `Assets/_Project/Features/Items/Definitions/O2Canister/O2Canister.asset:15`; `Assets/_Project/Tests/Domain/SurvivorEquipmentHandoffTests.cs:19`, `:26`.

```csharp
public Vital Health { get; }
public Vital Oxygen { get; }
```

### How are health and O2 currently displayed and updated, including their status indicators?

`VitalBarView` selects Health or Oxygen using serialized `kind`, resolves `PlayerVitalsController.Instance`, subscribes to `Changed`, and immediately synchronizes the fill. OnEnable tries binding; LateUpdate retries until a Vital is available. OnDisable unsubscribes and clears its reference. The handler changes only `fillAmount`.

The prefab's health fill is a red Filled Image (`m_Type: 3`), using horizontal fill (`m_FillMethod: 0`) from origin zero. Its label is static TMP text `HP`. Wakeup overrides a second instance's label/name to `O2` and `kind` to 1; the HP instance is named HP and inherits health kind zero. Dynamic status indicators, severity labels, numeric labels, and threshold-driven color changes: **Not found within S5 and the inspected view/scene wiring**. Static bar color and HP/O2 labels are authored presentation, not value-dependent status logic.

Files: `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17`, `:22`, `:27`, `:41`, `:47`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:9`; `Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:140`, `:148`, `:151`, `:154`, `:223`, `:426`; `Assets/_Project/Scenes/Wakeup.unity:10428`, `:10548`, `:10612`, `:14679`.

```csharp
_vital = kind == VitalKind.Health
    ? controller.Vitals.Health
    : controller.Vitals.Oxygen;
_vital.Changed += HandleChanged;
HandleChanged(_vital);
```

### Which gameplay systems modify player health or hunger, and which existing consumers depend on those values?

The controller's Awake, ResetForAwakening, and OnValidate set health/O2 current values. `SurvivorAvatarAdapter.Awaken` calls ResetForAwakening after moving the retained avatar. `VitalBarView` consumes current/maximum through Normalized and Changed. Tests explicitly set health/O2 and inspect them. No hunger consumers or gameplay health damage/healing callers were found within S1–S3 and S7. No subscription to `Vital.Depleted` was found in those searches: zero health is not connected to succession by an inspected depletion subscriber. Succession exposes a separate `RequestDeath` entry point.

Files: `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27`, `:35`, `:44`; `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:24`, `:36`; `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:33`, `:37`, `:47`; `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:57`; `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs:71`, `:74`.

```csharp
// SurvivorAvatarAdapter.Awaken
vitals.ResetForAwakening();
// PlayerVitalsController.ResetForAwakening
Vitals.Health.Set(currentHealth);
Vitals.Oxygen.Set(currentOxygen);
```

### How are player vital values and tuning settings configured, initialized, saved, and restored?

The controller serializes four floats: maximum/current health and maximum/current O2, all defaulting to 100. Awake initializes through ResetForAwakening; this creates PlayerVitals only when absent, then sets both currents. Existing Vital objects are retained on subsequent resets. OnValidate sets currents only; it does not propagate edited maximum fields to existing Vital objects. Subsequent awakening resets likewise do not update Max on existing objects. Runtime changes are not copied back into the serialized current fields.

Wakeup serializes all four values as 100. The enabled build scene is Wakeup. SaveGameData contains version, world seed, containers, and collected pickup IDs. ContainerSaveSession.Capture captures containers and pickup IDs; SaveGameService writes that capture. Vital values/restoration settings in that save schema or a vital persistence adapter: **Not found within S1–S3 and S7, with the inspected save schema/capture path**. Reload-created vitals therefore use the controller's authored initialization rather than a health/O2 field from this save data.

Files: `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:19`, `:27`, `:35`, `:44`; `Assets/_Project/Features/Player/Vitals/PlayerVitals.cs:10`; `Assets/_Project/Scenes/Wakeup.unity:6178`; `ProjectSettings/EditorBuildSettings.asset:8`; `Assets/_Project/Domain/SaveGameData.cs:9`; `Assets/_Project/Domain/ContainerSaveSession.cs:78`; `Assets/_Project/Core/SaveLoad/SaveGameService.cs:39`.

```csharp
if (Vitals == null) Vitals = new PlayerVitals(maxHealth, maxOxygen);
Vitals.Health.Set(currentHealth);
Vitals.Oxygen.Set(currentOxygen);
```

```csharp
public int version = CurrentVersion;
public int worldSeed;
public List<ContainerSaveRecord> containers = new();
public List<string> collectedPickupIds = new();
```

### How is scaled gameplay elapsed time supplied, how is gameplay eligibility determined during menus, pause, transitions, and other inactive states, and how do existing passive changes and post-damage delays use them?

TimeSystem applies `PauseCoordinator.Scale` to Unity `Time.timeScale`; its base scale is 1, any pause lease forces scale zero, and base scale accepts finite values from 0 to 10. Scaled elapsed seconds are supplied directly as `Time.deltaTime` in existing world updates. This is separate from InputPolicyCoordinator eligibility: Gameplay requires readiness, no transition, no container modal, no primary screen, and no pause.

Inventory/transfer modals acquire pause leases and set modal gating. Manual pause acquires an independent lease. Task journal acquires both primary-screen and pause leases. Scrubber screen acquires only the primary-screen lease, so gameplay input can be ineligible while time scale remains nonzero. Startup marks ready after binding/recovery; failures mark unready and acquire pause; transitions set transition gating, and releasing the interactive scene marks unready.

The scrubber's passive room recovery advances with `Time.deltaTime` whenever its inventory binding is Available; it does not consult `policy.Gameplay`. Its state accumulates up to five seconds only when Running and not Operational. This changes room state, not player vitals. Task notices provide a different convention: SceneBindings supplies unscaled delta, and the coordinator counts it only while visible and Gameplay is true. Vital passive changes or post-damage timers using either mechanism: **Not found within S1–S4 and S7**. PlayerVitalsController has no timed Update.

Files: `Assets/_Project/Core/Time/TimeSystem.cs:8`, `:11`; `Assets/_Project/Application/RuntimePolicy.cs:9`, `:18`, `:36`, `:45`, `:96`; `Assets/_Project/Core/Game/GameManager.cs:13`; `Assets/_Project/Application/Tasks/TaskJournalScreenCoordinator.cs:25`; `Assets/_Project/Application/ScrubberScreenCoordinator.cs:24`; `Assets/_Project/Core/Composition/GameCompositionRoot.cs:76`, `:90`, `:100`, `:143`, `:152`; `Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:19`, `:47`; `Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:12`, `:29`; `Assets/_Project/Core/Composition/SceneBindings.cs:190`; `Assets/_Project/Application/Tasks/TaskNoticeCoordinator.cs:87`.

```csharp
public bool Ready => ready && !transition;
public bool Gameplay => Ready && !modal && !primaryScreen && !pause.IsPaused;
```

```csharp
private void Update() { if(Available) state.Advance(Time.deltaTime); }
// ScrubberEncounterState.Advance
if(!Running || Operational) return;
recoverySeconds=Math.Min(5,recoverySeconds+activeSeconds);
```

### How does selecting a hotbar item connect to input actions, item-use eligibility, consumable effects, and removal of consumed items?

SceneBindings registers the player hotbar container, creates HotbarSelectionState, supplies it to hotbar presenters, and binds it to UnityInputHost. Hotbar1–9 actions bind to keyboard digits 1–9. Their performed callbacks select the corresponding zero-based index; they do not invoke an item-use operation. Actions are enabled only when policy.Gameplay is true; callbacks also require fresh input after survivor reset and no UI activation consumed that frame.

Selection begins at -1, accepts an available reader and index -1 through capacity-1, and ignores unchanged/invalid selections. Empty slots can be selected. Selection clears when the active slot changes to empty or capacity/availability invalidates it. Presenters render active highlighting from ActiveIndex; pointer selection separately toggles a presentation-selected index. HotbarController is a serialized compatibility adapter only.

Consumable effects, item-use eligibility, and a hotbar consumption/removal call: **Not found within S1–S4 and S7, plus the inspected input/selection/presenter chain**. A general container Remove command exists: it validates address/quantity, rejects insufficient quantity, decrements a stack or removes it when the quantity equals the stack, commits, and returns Applied. This API has no vital effect in its implementation.

Files: `Assets/_Project/Core/Composition/SceneBindings.cs:138`, `:185`; `Assets/_Project/Core/Input/SOLITUDE_InputActions.inputactions:50`, `:286`, `:297`, `:374`; `Assets/_Project/Core/Input/UnityInputHost.cs:28`, `:51`, `:85`, `:104`, `:115`; `Assets/_Project/Application/RuntimePolicy.cs:151`; `Assets/_Project/Application/ContainerPresenter.cs:82`, `:89`, `:94`; `Assets/_Project/Features/Hotbar/HotbarController.cs:3`; `Assets/_Project/Features/Hotbar/HotbarSlotView.cs:9`; `Assets/_Project/Domain/ContainerCommandService.cs:162`.

```csharp
private void Hotbar(InputAction.CallbackContext context)
{ if (Fresh(context) && GameplayAllowed && int.TryParse(context.action.name.Substring(6), out int index)) selection?.Select(index - 1); }
```

```csharp
plan[address.Index] = quantity == stack.Quantity
    ? null : new ItemStack(stack.Definition, stack.Quantity - quantity);
Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan }, worldCommit);
```

### How does the vitals UI obtain changing values and settings, and what existing UI conventions support segment boundaries, threshold markers, and adjacent status text?

The vitals view obtains a singleton controller through lazy FindAnyObjectByType lookup and binds its selected Vital. Its own serialized configuration is only `fillImage` and `kind`; current/max arrive through the Vital object. Changed triggers fill updates, and binding synchronizes initial fill. There is no subscription specifically for settings/maximum changes.

The authored prefab uses RectTransforms, a HorizontalLayoutGroup, LayoutElements, a TMP HP label, a Bar Image, and its child Fill Image. Fill stretches across its parent. These establish a label-plus-bar layout; the inspected prefab has no adjacent dynamic status text, segment boundary objects, or threshold marker objects. Corresponding vitals-specific conventions: **Not found within S4–S5 and the inspected prefab/view/scene overrides**.

Files: `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:12`, `:27`, `:37`, `:47`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:9`; `Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:30`, `:53`, `:114`, `:170`, `:223`, `:306`, `:329`, `:366`, `:394`, `:425`.

```csharp
[SerializeField] private Image fillImage;
[SerializeField] private VitalKind kind;
// ...
_vital.Changed += HandleChanged;
HandleChanged(_vital);
```

```yaml
m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
# TMP label, line 223
m_text: HP
# Fill RectTransform, lines 114–115
m_AnchorMin: {x: 0, y: 0}
m_AnchorMax: {x: 1, y: 1}
```

### How does existing health handling behave at zero health, full health, exact segment boundaries if present, and when maximum health changes?

For a normal positive maximum, Set clamps below-zero values to zero and above-maximum values to maximum. Repeating an unchanged zero/full value returns before events. A changed value emits Changed first, then emits Depleted if current is nonpositive and depletion has not already been recorded. A later positive Set rearms depletion. Construction at zero does not itself emit Depleted, and setting that unchanged zero still returns early. No depletion gameplay subscriber was found within S3. Segment-boundary special handling: **Not found within S2, S4, and S5**.

SetMax assigns Max, optionally calls Set(Current), and otherwise publishes no event. Increasing maximum or decreasing it while current remains within range causes no Changed event, so a bound bar does not automatically refresh its ratio in that case. Decreasing below current with clamping enabled changes current and emits Changed. With clampCurrent false, current can remain above Max and Normalized can exceed one. Constructor/SetMax have no validation for negative/nonfinite maximum; Normalized returns zero when Max is nonpositive. The Set clamp is exactly the ternary shown below, rather than a general invalid-number check. Editing controller maximum fields on an initialized instance does not call SetMax.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:9`, `:16`, `:22`, `:30`, `:35`, `:43`; `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:37`, `:47`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:35`.

```csharp
var clamped = value < 0f ? 0f : (value > Max ? Max : value);
if (clamped == Current) return;
// ...
public void SetMax(float newMax, bool clampCurrent = true)
{
    Max = newMax;
    if (clampCurrent) Set(Current);
}
```

### What happens when food or medical items are used at full hunger or health, and how are restoration amounts clamped and item consumption handled?

Food/medical use, restoration amounts, full-value eligibility rules, and a consumption transaction coupled to vitals: **Not found within S1–S4, S6, and S7**. Consequently no current food/medical-use behavior at full hunger/health was established. This is distinct from the existing generic APIs: Vital.Add routes through clamped Set, and general container Remove decrements/removes valid quantities without knowing health or hunger. ItemDefinition contains no restoration amount or consumable type field.

Files: `Assets/_Project/Features/Player/Vitals/Vital.cs:22`, `:41`; `Assets/_Project/Features/Items/Core/ItemDefinition.cs:8`, `:19`, `:29`; `Assets/_Project/Domain/ContainerCommandService.cs:162`; `Assets/_Project/Core/Input/UnityInputHost.cs:115`.

```csharp
public void Add(float delta) => Set(Current + delta);
// ContainerCommandService.RemoveCore
if (quantity < 1) return new CommandResult(CommandStatus.InvalidQuantity);
if (stack == null || quantity > stack.Quantity)
    return new CommandResult(CommandStatus.InsufficientQuantity);
```

### What existing tests, playable checks, or validation conventions verify health changes, hotbar consumable use, hunger changes, gameplay-gated timing, and vitals UI updates?

Existing inspected verification covers:

- **Vitals UI:** VitalBarReadinessTests creates the view before the player, verifies delayed binding, initial/current fill, synchronous Changed updates, unsubscribe while disabled, reconciliation on re-enable without healing, and retained Vital identity/full fill after awakening reset. Files: `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs:15`, `:38`, `:45`, `:48`, `:53`, `:55`, `:58`.
- **Health/O2 reset:** SurvivorSuccessionSliceTests explicitly sets health 12/O2 9, requests death through the succession API, and asserts retained health identity and both restored to 100. Files: `Assets/_Project/Tests/PlayMode/SurvivorSuccessionSliceTests.cs:61`, `:71`, `:74`, `:85`.
- **Vitals isolation:** ScrubberEncounterSliceTests asserts unchanged health/O2 during encounter inspection/completion and checks save isolation. Files: `Assets/_Project/Tests/PlayMode/ScrubberEncounterSliceTests.cs:183`, `:199`, `:221`, `:229`, `:236`.
- **Hotbar selection/removal:** ApplicationCompositionTests checks duplicate selection suppression, empty-index selection, clearing after removing the selected stack and shrinking capacity, and transient selection. Files: `Assets/_Project/Tests/Application/ApplicationCompositionTests.cs:141`.
- **Input gating:** UnityInputCompositionTests checks digit selection, rejection during inventory/primary screens and independent pause, consuming the close-activation frame, and transition rejection. Files: `Assets/_Project/Tests/Editor/UnityInputCompositionTests.cs:60`, `:62`, `:71`, `:76`, `:78`, `:84`, `:85`.
- **Gameplay-gated elapsed time:** TaskNoticeCoordinatorTests advances synthetic elapsed seconds and verifies primary screens, pause, transition, and unready states do not consume visible dwell. Files: `Assets/_Project/Tests/Application/TaskNoticeCoordinatorTests.cs:47`.
- **Playable verification:** SolitudeCompositionSmoke checks modal time scale/gameplay policy, hotbar input rejection, inventory-to-hotbar transfer, independent pause ownership, and restoring slow-motion scale after modal closure. Files: `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs:134`, `:135`, `:155`, `:161`, `:163`.

Dedicated damage/healing/segment/max-change tests, hotbar consumable-use tests, hunger-change tests, and vital passive/post-damage timing tests: **Not found within S1–S4 and S7's Tests/Verification scope**. The existing tests above establish coverage intent only; no tests or playable checks were run during this research.

```csharp
oxygen.Set(30);
Assert.AreEqual(.3f, image.fillAmount);
viewObject.SetActive(false);
oxygen.Set(20);
Assert.AreEqual(.3f, image.fillAmount, "Disabled view unsubscribes.");
```

Files: `Assets/_Project/Tests/PlayMode/VitalBarReadinessTests.cs:49`.

```csharp
using (policy.AcquirePrimaryScreen())
{ Assert.IsNull(display.Last); notices.Advance(200); Assert.AreEqual(3, notices.RemainingSeconds); }
```

Files: `Assets/_Project/Tests/Application/TaskNoticeCoordinatorTests.cs:50`.

## New Questions
