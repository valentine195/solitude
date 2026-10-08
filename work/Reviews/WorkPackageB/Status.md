# Work Package B — interaction lifetime

Implemented B only, on top of the existing A changes. Work Package C has not started. No commits or staging were performed; the working tree still includes A and unrelated saved scene/review changes.

## Ownership and dependency decisions

`SceneBindings` creates a `SceneInteractionScope` after player inventory registration and before feature/input binding. The scope creates disposable `SceneInteractionEvents` and `InteractionFeedbackService`, validates exactly one local player/UI controller, and explicitly binds the player, controller, and feedback receivers. Discovery includes inactive components and stays within the scene. This is specific endpoint discovery, with no type activation, service lookup, DI framework, or global state.

The scope releases player focus, UI subscriptions, feedback receivers, view references, and event delegates. Initialization failures unwind bindings. Feature release runs first, so scrubber task sources unsubscribe before their event source is disposed. `SceneFeatureContext` exposes the specific event dependency to `ScrubberComposition`.

The two older SolitudeStart scenes have no `SceneBindings`, so a small `SceneInteractionBindings` component owns the same scope for direct entry. It validates unique ownership and rejects coexistence with `SceneBindings`. Component enable/disable does not end the scene lifetime. The existing legacy input behavior in these scenes remains outside this change.

There are no static compatibility shims. The entire Assets/Packages/Tools source search found no remaining calls to `InteractionEventBus` or static `InteractionFeedback`. Player targeting, proximity caching, tie ordering, task eligibility, messages, colors, timing, input controls, and save behavior are preserved. Disabling input clears accepted focus; transitions clear focus and feedback. UI enable replays the scope's accepted focus and attaches only once.

## Changed files and authoring

All paths below are relative to `Assets/_Project` and describe B's delta, excluding A and unrelated changes.

| Files | Purpose |
| --- | --- |
| `Systems/Interaction/Core/SceneInteractionEvents.cs`; `UI/Presenters/InteractionFeedbackService.cs` | Replace the two static owners with disposable instance services. Moved with Unity AssetDatabase; original `.meta` contents/GUIDs retained. |
| `Systems/Interaction/Core/{SceneInteractionScope,SceneInteractionBindings,IInteractionFeedbackReceiver}.cs` | Scene ownership, direct-entry ownership, and explicit feedback receiver contract; new matching metadata files. |
| `Core/Composition/{SceneBindings,SceneFeatureContext}.cs`; `Features/World/ScrubberEncounter/{ScrubberComposition,ScrubberTaskSource}.cs` | Create/release the interaction scope and inject its events into feature-owned task binding. |
| `Features/Player/Interaction/PlayerInteractor.cs` | Explicit event/feedback bind and release, focus clearing on input disable/component disable. |
| `Systems/Interaction/UI/Controllers/UIController.cs`; `UI/Views/{InteractionPromptView,InteractionFeedbackView}.cs` | Explicit dependencies, duplicate-safe subscription lifecycle, late-enable prompt replay, authoring validation, stale feedback coroutine cleanup. Initially inactive notification roots can still activate to show feedback. |
| `Features/World/{OpeningSlice/OpeningFirstContact,SolitudeStart/SolitudeStartCompletion}.cs` | Inject local feedback; release references and stop pending contact fragments on scene release. |
| `Editor/{InteractionScopeMigration,CompositionValidator}.cs`; `Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs` | Deliberate standalone migration, validation, and continued builder authoring. Migration refuses dirty scenes and avoids saving already migrated scenes. |
| `Features/World/SolitudeStart/Scenes/{SOLITUDE_Start,SOLITUDE_Expedition}.unity` | Add one standalone owner per scene. No existing object IDs or serialized reference values removed. Existing view field names retained. |
| `Tests/Editor/{InteractionScopeTests,ScrubberTaskSourceTests,RestorationJunctionTests}.cs`; `Tests/PlayMode/{InteractionScopeLifecycleTests,ProductionLifecycleTests,RestorationTerminalTests}.cs` | Per-test dependencies and regression coverage for isolation, activation/rebind cycles, partial failure, destroyed listeners, direct entry, inactive feedback receivers/views, and repeated transition cleanup. |

No prefab edits were needed: views retain their authored references and scene-level ownership supplies dependencies. Four composed gameplay/reference scenes need no new serialized owner. The atmospheric demo scene was not edited by B. Domain/Application code and assembly definitions are unchanged. The demo's disposable persistence implementation is unchanged.

## Verification

- Live Unity 6000.6.4f1 recompile: completed, zero errors/warnings (`Logs/WorkPackageB/recompile-final.json`).
- Domain/Application pure runner: **82 passed, 0 failed** (`pure-final.log`); two existing unused test-event compiler warnings. An initial invocation used an unresolved NUnit path; correcting it to the installed absolute DLL path resolved that tooling error.
- Final Unity EditMode: **170 passed, 0 failed** (`edit-complete.xml`). Tests cover two actual scene scopes plus independent focus/trigger services, UI subscription cycles, standalone ownership validation, invalid/wrong-scene references, partial unwind, and task-source rebinding. EditMode explicitly invokes runtime lifecycle callbacks; PlayMode separately verifies real listener destruction.
- Unity PlayMode with the saved workspace authoring: **37 passed, 3 failed** (`play-final.xml`). All new B PlayMode tests and repeated production transitions passed. The inactive saved fuse pickup fails pickup and task-journal expectations. Reload also logs a destroyed `ContainerSlotView` reference. The same reload exception reproduced using the pre-B A implementation with the saved scene (`play-pre-b-saved-scene.xml`); it is not introduced by B. No assertions were removed or expected logs suppressed.
- Unity PlayMode with original authored demo values in the disposable verification project: **40 passed, 0 failed** (`play-original-authoring.xml`). This includes pickup/install/restart/recovery/reset, task journal, modal/Back behavior, production/demo save isolation, and B's scene lifetime tests. Only the disposable project's demo scene was replaced; the saved source scene remains untouched. Other source code, including current journal controls, was retained.
- `UnityMetaValidator.ValidateForCI`, runtime composition validation, and explicit validation of all six gameplay/reference scenes passed (`validation.json`). The standalone migration rerun left both scene files byte-identical (`migration-idempotence.json`). Serialized ID/reference preservation, original service metadata, and unchanged assembly boundaries are recorded in `serialization-check.json`.

Commands were run from `/Users/jeremyvalentine/SOLITUDE`: Unity bundled dotnet `run --project Tools/Verification/PureTests.csproj -p:NUnitPath=<absolute installed NUnit DLL>`; `unity test /private/tmp/solitude-work-package-b --mode EditMode|PlayMode --filter SOLITUDE.Tests --output <XML> --timeout 600`; fixture setup through `SOLITUDE.Editor.PhaseFiveFixtureBuilder.Prepare`; live validator/migration calls through `unity command eval <code> 60000 --project-path /Users/jeremyvalentine/SOLITUDE`. PlayMode saves were isolated with `-solitudeSaveDirectory` under `Logs/WorkPackageB`.

## Outstanding risks and verification limits

The saved demo authoring remains an integrated acceptance blocker: its inactive pickup and container reload failure need a separate decision/investigation. B is green against original authored gameplay, but the complete saved workspace is not green. No standalone player build or manual visual/audio playthrough was performed. Automated checks verify messages and colors, but do not replace visual review. Existing unrelated changes were preserved.

The next ship system can add a feature-owned composition and serialized scene reference without adding its equipment, screens, tasks, or cleanup logic to `SceneBindings`. If it needs interaction signals, the typed feature context supplies the scene event source. New shared dependencies should be explicit additions to that context rather than a resolver.
