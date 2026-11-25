# QRSPI Research: task-journal

## Inputs and scope

Project root: `/Users/jeremyvalentine/SOLITUDE`. Current first-party code, configuration, authoring assets and tests inspected independently. Paths below are relative to `Assets/_Project/` unless marked repository-root. `git status --short` at repository root returned no entries. No repository files changed; no tests or gameplay gates run. Tests below establish authored coverage, not passing results. CODEBASE_MAP.md was used only for navigation. Recursive searches excluded `.qrspi/**` and `**/.qrspi/**`; only the explicitly supplied questions were opened there.

Human-authorized proposal inputs were inspected under the supplied exceptions to ordinary Research isolation. Their content is distinguished from current source evidence. SHA-256 of exact input bytes:

| Input | SHA-256 |
| --- | --- |
| `/Users/jeremyvalentine/SOLITUDE/.qrspi/task-journal/queries.md` | `69041f2d27cadb179fc20d7c8c96062bdbddd3783ea9fdf6807f7f038a999662` |
| `/private/tmp/solitude-task-journal-spec.md` | `cf86cf85b24e6b6b77814f0b97cf6932bddee9331ae82653c4d1cfb0a6695dac` |
| Active: `/Users/jeremyvalentine/.codex/generated_images/01a1196b-4cc7-7460-9db0-24de9614ef56/exec-d6205182-f338-458b-8b33-4db57104b912.png` | `e8365a338b95d05d6d07be74c98d1c1d5a1ec2f991f988f8894ecffe2518e364` |
| Completed: `/Users/jeremyvalentine/.codex/generated_images/01a1196b-4cc7-7460-9db0-24de9614ef56/exec-e1e752f1-0edc-49bb-8494-325c6e4d40f1.png` | `91ddf756406691c5fe95db67bc9a1730ecb63ce7a2bceb833dd6187f9fa09ad1` |
| Empty: `/Users/jeremyvalentine/.codex/generated_images/01a1196b-4cc7-7460-9db0-24de9614ef56/exec-5cbbe45d-0e5e-43f8-9060-27f14012f3e9.png` | `e2d39f7915f2167c2dd4ed66cecc80879ea98f478ed0066cd18889fbd23e35ff` |

## Answers

### 1. What task or objective tracking currently exists, and how are task identity, player-facing text, task-specific stages, progress, completion, failure, and pinned state represented?

The current task-like model is a scrubber-specific derived objective. `ScrubberEncounterState` holds a repair state, Running and recoverySeconds; Operational is Running plus at least five recovery seconds. Objective, Fault and telemetry are derived strings, not separately stored task records. The default state is fuse absent, stopped and elevated room condition. Its four objective strings correspond to replace fuse, restart, wait, and completed atmosphere. Failed restart is feedback, not a persistent terminal task outcome.

Files: `Domain/Restoration/ScrubberEncounterState.cs:8–23`.

```csharp
public bool Operational => Running && recoverySeconds >= 5;
public string Objective => Operational ? "Room atmosphere restored — complete" : Running ? "Wait for room air to recover" : FuseInstalled ? "Restart the scrubber" : "Replace the scrubber fuse";
```

The authored fuse item has a stable item ID, but that is an inventory item identity, not task/source identity (`Features/World/ScrubberEncounter/ScrubberEquipment.cs:12–17`). Generic restoration has only IsRestored and command outcomes (`Domain/Restoration/RestorableSystemState.cs:7–15`). Shared task identity, task catalog, discovered state, failure state, progress records and pinned state were **not found within** first-party `Domain`, `Application`, `Core`, `Systems`, `Features`, `Tests`, `UI`. Searches: case-insensitive `journal|objective|taskstate|pinned|scrubber|restoration` in C#; then `journal|objective|pinned|task` in C#, inputactions and Markdown. Hits relevant to tasks were the scrubber objective, notice, associated tests, and UI guide references. This is a bounded absence finding, not an assertion about generated/vendor code.

### 2. Where can players currently view task or objective information, and what information, pinning behavior, and active or completed grouping do those views expose?

The scrubber equipment screen displays CURRENT TASK plus the derived Objective and current operating state, fault, fuse, airflow and room CO2. It refreshes on equipment changes and before showing. It has repair/restart/close controls and remembers selected UI object, but no task list, counts, Active/Completed tabs or pin control.

Files: `Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:41–46,70–104`.

```csharp
objective.text = "CURRENT TASK\n" + state.Objective.Replace("—", "-");
```

A separate component queues one completion notice on an observed transition to Operational, suppresses it while this equipment screen is open, and displays for four unscaled seconds. It initializes observedCompletion from current state, so late enable after completion does not replay it (`Features/World/ScrubberEncounter/UI/ScrubberTaskNotificationView.cs:13–35`). World room feedback shows SCRUBBER operating status and CO2 warning (`Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:22–31`). These show this encounter's state; they do not group tasks. Shared views and pinning were not found with the bounded searches in answer 1.

The actual demo references the scrubber screen and equipment; prefab instance overrides bind screen and completion notice to equipment (`Scenes/Demos/AtmosphericScrubberEncounter.unity:8448–8459,9331,9456–9460`; `Features/World/ScrubberEncounter/UI/ScrubberScreen.prefab:392–415`). The enabled production build scene is Wakeup (repository-root `ProjectSettings/EditorBuildSettings.asset:7–10`); this demo is not that enabled entry.

### 3. Which existing gameplay systems create or update tasks or objectives, and what boundaries govern their access to the tracked state?

ScrubberEquipment owns one readonly ScrubberEncounterState per component. Scene composition supplies the session command service and player inventory; no shared task service is created. Install routes through RestorableSystemState and removes one matching item only from a live player-inventory reader owned by that exact command service. Restart additionally requires equipment to be active and bound. Update advances state only while the inventory/service binding remains available.

Files: `Features/World/ScrubberEncounter/ScrubberEquipment.cs:14–28,32–51`; `Domain/Restoration/RestorableSystemState.cs:17–40`.

```csharp
!ReferenceEquals(commands.Read(inventory.Handle), inventory)
var result = commands.Remove(slot.Address, 1, () => IsRestored = true);
```

Generic RestorationJunction invokes its configured RestorableSystem.State.TryRestore using injected commands/inventory and maps outcomes into interaction feedback (`Features/World/Restoration/RestorationJunction.cs:12–33`). RestorableSystem owns scene instance state (`Features/World/Restoration/RestorableSystem.cs:8–12`). Composition discovers all scrubber equipment/screens in the player scene, initializes equipment first, screens second, and finally binds input (`Core/Composition/SceneBindings.cs:119–125`). There are no journal writes to trace in these paths or the bounded task searches.

### 4. How is task or objective state retained across scene changes, saved games, and the start of a new game?

Scrubber state is scene-owned and explicitly excluded from production saves. Closing its UI only hides the panel/releases the primary-screen lease; it does not recreate equipment state. ReleaseBinding detaches inventory/state observers and clears bindings, without resetting the readonly state. Actual scene destruction/reload constructs fresh equipment/state.

Files: `Domain/Restoration/ScrubberEncounterState.cs:5–9`; `Features/World/ScrubberEncounter/ScrubberEquipment.cs:14,48–53`; `Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:97–104`.

```csharp
// Scene-owned state; never included in production save data.
private readonly ScrubberEncounterState state = new ScrubberEncounterState();
```

SaveGameData v2 contains version/worldSeed/containers/collectedPickupIds; no task or scrubber fields (`Domain/SaveGameData.cs:9–14`). The persistent composition root owns container session and scene scopes, releases a scope on unload, and new save releases scopes, resets session and reloads the active scene (`Core/Composition/GameCompositionRoot.cs:45,76–80,131–161`; `Core/Composition/SceneBindings.cs:221–240`). Scrubber demo uses the RestorationDemoSession marker's new in-memory store; player demo binding resets disposable session and forbids mixing demo with production (`Features/World/Restoration/Demo/RestorationDemoSession.cs:9–16`; `Core/Composition/GameCompositionRoot.cs:56–61,122–126`). These are current persistence boundaries, not a journal retention contract.

### 5. What mechanisms currently communicate gameplay occurrences to other systems, and what delivery and subscription-lifetime behavior do those mechanisms provide?

There are synchronous instance C# events plus a static interaction event bus. Scrubber State.Changed publishes install, restart and terminal recovery. It invokes each subscriber separately and swallows observer exceptions; no replay/snapshot-subscription protocol or per-tick progress event exists (`Domain/Restoration/ScrubberEncounterState.cs:15,27,29–40`). RestorableSystemState similarly isolates observers but reports exceptions through ObserverError (`Domain/Restoration/RestorableSystemState.cs:43–53`). Container commands commit slots and worldCommit before Completed/slot notifications, guard publishing reentry, and isolate/report observers (`Domain/ContainerCommandService.cs:251–274`).

```csharp
worldCommit?.Invoke();
foreach (Action<ContainerChangeSet> observer in Completed.GetInvocationList())
    try { observer(change); } catch (Exception error) { Report(error); }
```

InteractionEventBus exposes OnFocusChanged and OnTriggered; direct multicast Invoke gives no replay, durable storage, registration handle or observer-exception isolation (`Systems/Interaction/Core/InteractionEventBus.cs:7–17`). Triggered payload contains interactable/player, no InteractionResult (`Systems/Interaction/Core/InteractionEvents.cs:16–25`). PlayerInteractor checks CanInteract, calls Interact, handles its returned result, then publishes Triggered regardless of returned success/block outcome (`Features/Player/Interaction/PlayerInteractor.cs:94–111`). Thus reaching Interact and successfully opening a screen are distinct facts.

UIController subscribes/unsubscribes focus events on enable/disable (`Systems/Interaction/UI/Controllers/UIController.cs:28–37`). Scrubber screen subscribes through Initialize and detaches through Release; equipment detaches its inventory and state observers on ReleaseBinding (`Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:41–46`; `ScrubberEquipment.cs:22–28,48–53`). Room feedback and completion notice use OnEnable/OnDisable subscription pairs (`ScrubberRoomFeedback.cs:16–20,33–36`; `UI/ScrubberTaskNotificationView.cs:13–17,32–35`). Consumers reconcile from current state on enable/show; this is not bus event replay.

### 6. How are existing player-facing screens opened, closed, and coordinated with gameplay input and other open screens?

InputPolicyCoordinator grants one primary-screen lease only while Ready and not modal/primary-screen/paused. A primary screen blocks Gameplay and Inventory while retaining Pointer. ScrubberScreenCoordinator owns this lease, catches display Show failure and releases it, supports Back, and disposes idempotently. It does **not acquire pause**; simulation continues on its screen (`Application/RuntimePolicy.cs:30–53`; `Application/ScrubberScreenCoordinator.cs:20–42`).

```csharp
if (!Ready || modal || primaryScreen || pause.IsPaused) return null;
```

Inventory/transfer modal preparation is followed by a pause lease and modal gate; failure releases temporary binding/lease, closing cancels gesture and releases only its lease (`Application/RuntimePolicy.cs:88–130`). Pause leases preserve base speed/unrelated leases (`RuntimePolicy.cs:6–28`). TimeSystem writes pause.Scale into Time.timeScale (`Core/Time/TimeSystem.cs:10–14`).

UnityInputHost shares its UI action asset with the EventSystem, gates movement/interact/hotbar using Gameplay, inventory using Inventory, and Back using Ready. Consumed UI activation suppresses gameplay for that frame. Scrubber Close consumes activation; it preserves selected EventSystem object and clears selection on hide. Back checks scrubber screens before container modal; transition closes both (`Core/Input/UnityInputHost.cs:22–44,67–91`; `Features/World/ScrubberEncounter/UI/ScrubberScreenView.cs:77,97–104`; `Core/Composition/SceneBindings.cs:145–153`). Losing gameplay permission clears interaction focus/prompts (`Features/Player/Interaction/PlayerInteractor.cs:23–27`). Journal input/action and `<Keyboard>/j` were not found within `Core/Input/SOLITUDE_InputActions.inputactions` using `journal|Journal|<Keyboard>/j`.

### 7. How is a broken scrubber represented, and what existing detection or player interaction behavior establishes that the player has discovered its fault?

Broken condition is the initial authoritative state: FuseInstalled=false, Running=false, Fault=Blown fuse, BLOWN fuse, STOPPED airflow, OFFLINE equipment and ELEVATED room CO2 (`Domain/Restoration/ScrubberEncounterState.cs:8–22`). Room feedback reconciles immediately on enable and presents warning/alarm/status without recording discovery (`Features/World/ScrubberEncounter/ScrubberRoomFeedback.cs:16–29`).

ScrubberAccess displays Access scrubber; CanInteract requires a screen reference, and Interact's success depends on screen.Open returning true (`Features/World/ScrubberEncounter/ScrubberAccess.cs:9–16`). Screen.Open returns coordinator Opened; Show validates authoring and refreshes telemetry before displaying (`UI/ScrubberScreenView.cs:64–85`).

```csharp
return screen.Open() ? InteractionResult.Success() : InteractionResult.Blocked("Scrubber interface unavailable");
```

There is no stored player discovery flag or distinct fault-discovered event in the scrubber files inspected. Automatic room warning, proximity focus and successful access are existing observable behaviors, but none records a discovered task. The task/discovery absence search was answer 1's first-party search plus current complete ScrubberEncounterState, Equipment, Access, RoomFeedback, screen and notice reads. Triggered is not equivalent to successful access (answer 5).

### 8. How are scrubber fuse replacement, restart, recovery progress, and recovered condition represented, and which observable occurrences distinguish those stages?

Install delegates to repair.TryRestore. One fuse removal and IsRestored=true commit together before inventory notifications; repair publishes Changed once and repeat installs return AlreadyRestored (`Domain/Restoration/RestorableSystemState.cs:19–40`). This becomes FuseInstalled=true and Objective=Restart while Running stays false (`Domain/Restoration/ScrubberEncounterState.cs:10–23`). Equipment emits install feedback and inventory changes also refresh equipment consumers (`Features/World/ScrubberEncounter/ScrubberEquipment.cs:27–39`).

Restart returns false until fuse exists or when already running; successful restart sets Running=true and publishes Changed. RecoverySeconds increases using nonnegative finite activeSeconds, caps at five and publishes only when Operational becomes true. Intermediate progress is readable from RecoverySeconds but emits no state Changed; after completion Advance returns without updating (`Domain/Restoration/ScrubberEncounterState.cs:24–40`).

```csharp
if(!Running || Operational) return;
recoverySeconds=Math.Min(5,recoverySeconds+activeSeconds);
if(Operational) Publish();
```

Equipment.Update advances with Time.deltaTime while bound; actual pause makes deltaTime zero through TimeSystem, whereas its current equipment screen does not pause (`ScrubberEquipment.cs:47`; `Core/Time/TimeSystem.cs:11`; answer 6). Terminal condition is Operational plus NORMAL room, completed Objective; Running remains RUNNING. Room warning/alarm end on Operational, running sound begins at Running (`ScrubberRoomFeedback.cs:25–29`). The completion notice listens directly to State.Changed and requires Operational (`UI/ScrubberTaskNotificationView.cs:19–22`).

### 9. How does existing task or objective tracking handle repeated discovery or progress signals, updates for unknown tasks, updates after completion or failure, and removal of abandoned tasks?

Shared task/update/discovery/removal APIs were not found in the bounded scope and patterns of answer 1. There is no known-task registry or persistent failed/abandoned/pinned state to exercise. Current scrubber-specific repeat handling is explicit: repeat install returns AlreadyRestored, repeat restart returns false, Advance does nothing before Running or after Operational. State never moves back to offline and exposes no reset method (`Domain/Restoration/RestorableSystemState.cs:19–20`; `ScrubberEncounterState.cs:24–35`). Missing/unavailable/busy install leaves existing state intact (`RestorableSystemState.cs:21–40`).

```csharp
if(!FuseInstalled || Running) return false;
if(!Running || Operational) return;
```

Completion notice guards observedCompletion, queues once, and clears its queue on disable; scene reload produces a fresh notice/state. This handles one encounter completion, not arbitrary duplicate task signals or abandonment (`Features/World/ScrubberEncounter/UI/ScrubberTaskNotificationView.cs:13–35`). Feedback text containing “Restart failed” is not task failure (`ScrubberEncounterState.cs:22`).

### 10. What existing tests or validation conventions cover task or objective tracking, scrubber restoration stages, gameplay signal delivery, and player-facing screen interaction?

No shared journal/pin/task lifecycle tests were found in answer 1's bounded search. Existing tests cover current encounter and neighboring contracts:

- Domain: fresh fault/objective, atomic repair/reentry, unsuitable inventories, unavailable/busy repair, observer isolation, one successful restart and five-second terminal recovery (`Tests/Domain/ScrubberEncounterTests.cs:12–63`); generic restoration atomicity/observer isolation (`Tests/Domain/RestorationTests.cs:23–32,62–71`); container committed-state/reentry and throwing observers (`Tests/Domain/ContainerBoundaryTests.cs:92–119`).
- Application: primary equipment screen blocks gameplay without pausing, failed Show releases lease, unready/paused/transition rejects (`Tests/Application/ScrubberScreenCoordinatorTests.cs:14–41`); container modal pause, independent pause preservation and activation failure (`Tests/Application/ApplicationCompositionTests.cs:41–90`).
- Unity input: real keyboard and EventSystem share asset/policy, primary screen prevents inventory/hotbar, closing consumes activation, teardown order remains safe (`Tests/Editor/UnityInputCompositionTests.cs:30,66–86`). Focus-event prompt refresh and disable hiding are exercised (`Tests/Editor/RestorationJunctionTests.cs:48–75`). Direct OnTriggered/payload/throwing interaction-bus tests were **not found within Tests** using `OnTriggered|InteractionTriggeredEvent|OnFocusChanged`; focus testing also uses `InteractionFocusChangedEvent` at the cited junction test.
- Actual scrubber scene PlayMode: E access, blocked movement/inventory, Escape+E safe return, selection retention, UI submit close (`Tests/PlayMode/ScrubberEncounterSliceTests.cs:51–85`), navigation/fault color (`:93`), pickup/repair stages (`:118,154`), recovery with screen open and actual pause (`:178–198`), completion deferral/expiry/no replay (`:200–213`), and fault inspection (`:215–231`).
- Lifecycle: reload resets encounter/inventory/notice/audio and releases old endpoints; production/demo mixing is rejected without modifying save (`Tests/PlayMode/ScrubberEncounterLifecycleTests.cs:40–81`). Authoring: catalog/item identity, scene pickup registration, room status/audio and completion notice wiring (`Tests/Editor/ScrubberEncounterAuthoringTests.cs:15–43`).

Representative authored assertion (`Tests/Application/ScrubberScreenCoordinatorTests.cs:18–19`):

```csharp
Assert.IsFalse(policy.Gameplay); Assert.IsFalse(policy.Inventory);
Assert.IsTrue(policy.Pointer); Assert.AreEqual(1, pause.Scale);
```

Repository-root `Tools/Verification/verify.py:35–59` copies source to an isolated workspace, mirrors the tracked Assets index, runs pure Domain/Application tests, authoring, Unity EditMode/PlayMode and verification/shipping builds. Its test command filters SOLITUDE.Tests and supplies a disposable save directory (`:17–22`). The README describes full vs partial qualification and results.json verdict (`Tools/Verification/README.md:10–38`). This Research pass has not run any of these checks.

## Reference alignment (authorized proposals, not current code)

The standalone proposal is explicitly a draft, not an approved implementation (`/private/tmp/solitude-task-journal-spec.md:3`). All three mockups were visually inspected. Active shows task list/counts and selected task details with location, confirmed fault and next step; Completed shows operational condition/result; Empty shows separate empty-list/no-selection messages. These are image observations, not evidence of existing UI. The proposal describes this structure at `:30–40`.

Current code supplies scrubber fault, the four derived steps, final condition, repair authority and equipment input ownership (answers 1, 3, 6–8). It does not supply the proposed shared journal record/service, source identity/generation, reader snapshot/subscription, tabs/counts, task ordering, preserved journal navigation or generic notice lane in the bounded scope of answer 1. The existing scrubber notice only defers for its equipment screen and uses unscaled dwell, unlike the proposal's exploration-wide notice policy (`proposal:101–107`; current `ScrubberTaskNotificationView.cs:24–30`). Proposed journal pause differs from the current unpaused equipment screen (`proposal:21–23`; current coordinator and tests cited in answer 6/10). Proposed no disk persistence aligns with current scrubber save exclusion, but proposed journal source scopes are not implemented (`proposal:26,67,130–136`; answer 4).

The older proposal excludes pinning, failed/abandoned task states and global auto-discovery (`proposal:25,40,160–162`). Supplied later human clarifications take precedence: both automatic and player-initiated discovery; task-specific stages; failed tasks probably in Completed; abandoned tasks removed; pinning included; scrubber example follows fault → fuse → restart → recovery → completed, with no failure/abandonment demonstration. Neither older mockups nor current code shows a pin affordance. These clarifications are authorized intent context, not inferred source behavior. No updated spec or plan is drafted here.

## New Questions

