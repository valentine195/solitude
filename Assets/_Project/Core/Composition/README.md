# Runtime composition

`GameCompositionRoot` owns the immutable catalog, session, pause/input policy, pickup collection and shared drag service. `GameRuntimeConfig` remains a designer-authored asset. `SaveGameService` only performs injected persistence and Unity lifecycle flushing.

`SceneBindings` explicitly lists the player, views, lockers and pickups. It binds inactive views and owns registration/presenter leases, including never-active pickups. Register dynamically spawned pickups through the owning scope's `RegisterPickup` and dispose the returned lease when despawning. Scope release runs before owner registrations are removed. One player scene is supported.

`SOLITUDE.Application` has no Unity references. Its coordinators own modal state, pause leases and transient selection. Container presenters observe read-only snapshots and route indexed, generation-qualified view events to the existing transfer service. Unity views resolve icons/text through their injected presentation catalog.

`UnityInputHost` owns one generated action asset shared with the EventSystem. It enables individual gameplay actions from policy; global inventory/back controls remain usable during container modals. Both modals pause the world and block movement, interaction and number keys. Escape cancels a drag first, then closes the screen. Closing releases only the modal's pause lease and preserves the base time scale.

Wakeup is wired through the Unity Editor migration (`SOLITUDE/Migrations/Compose Wakeup Runtime`). Legacy serialized input/controller markers remain inert to preserve script references. New scenes must explicitly configure a scope and use the existing persistent bootstrap or author their own bootstrap for direct entry. Runtime composition validation checks composed scenes during builds and is also available from `SOLITUDE/Validation/Validate Runtime Composition`.

Failed authoring or save restore gates input and preserves the original save. Recovery uses the bootstrap's explicit Start New Save command. It releases bindings, resets domain owners/facts, saves the fresh snapshot and reloads the active scene.

## Scene features

Add a feature-owned `SceneFeatureComposition` and reference it in the player
scope's serialized `features` list. List order determines feature Back priority;
the journal runs first and container modal Back runs last. Scene validation
requires every authored composition exactly once, in the same scene, including
inactive objects. Each feature validates its own endpoint references.

Features receive a read-only `SceneFeatureContext` after player inventory
registration. Its services are explicit; it provides no service lookup. The task
scope belongs to `SceneBindings`, which releases it after features detach their
publishers. A feature must unwind partial binding, tolerate repeated release,
and close screens before releasing input/policy dependencies. Scene release
unwinds features in reverse list order, including the feature whose bind failed.
Component enable/disable does not change scene ownership; endpoint components
retain their existing activation behavior.

`ScrubberComposition` owns demo equipment, screens and task sources. Migrate the
existing demo with `SOLITUDE/Migrations/Compose Scrubber Feature`; this preserves
the scene scope and journal reference. Its builder also authors that registration.
The disposable demo session/reset and production save isolation stay with
`GameCompositionRoot`.

## Interaction lifetime

Each composed player scene has a `SceneInteractionScope` owned by
`SceneBindings`. It creates `SceneInteractionEvents` and
`InteractionFeedbackService` after inventory registration and before feature
and input binding. It validates one local player and UI controller, discovers
inactive feedback receivers, and explicitly binds their dependencies.
`SceneFeatureContext.InteractionEvents` supplies the same event source to
feature task sources. There is no static interaction owner or service resolver.

`UIController` retains its serialized prompt/feedback views. Disable detaches
its subscription; enable attaches once and replays current focus. Player input
disable and transition close clear focus. Scene release detaches feature task
sources before disposing the interaction scope, which clears delegates and
feedback view references. Bind/release must work even when an endpoint has never
been active.

The older `SOLITUDE_Start` and `SOLITUDE_Expedition` scenes instead use
`SceneInteractionBindings` for direct-entry interaction ownership. That owner
cannot coexist with `SceneBindings`; it does not create a session, persistence,
or an input host. Its lifetime ends on explicit release or destruction.

## Add a scene feature

1. Place a `SceneFeatureComposition` subclass with its owning feature and author
   explicit references to its local equipment, screens, and task sources.
2. Implement `Validate` for required, unique, same-scene references and feature
   invariants. Include inactive endpoints. `SceneBindings.Validate` already
   rejects missing, duplicate, wrong-scene, and unlisted feature compositions.
3. Bind only the typed services needed from `SceneFeatureContext` in
   `Initialize`. Inventory is already available; use the provided task scope
   and interaction events. Record endpoints before binding so failures unwind.
4. Implement `TryBack`, `CloseForTransition`, and repeat-safe `Release`. Close
   screens and detach publishers/listeners before releasing their dependencies.
   Preserve endpoint enable/disable behavior rather than starting another scope.
5. Add the component exactly once to the scene's `features` list and update its
   editor builder. Deliberately migrate existing scenes through Unity's Editor
   API, retaining component instances, references, and metadata GUIDs.
6. Run runtime composition validation and focused binding/failure, activation,
   Back, and scene reload tests. Preserve production/demo save isolation.

A new feature can own its equipment, screens, tasks, and cleanup without adding
its concrete types to `SceneBindings`. A genuinely new shared dependency needs
an explicit typed context addition, not a `Resolve<T>()` hook. Features do not
need mirrored Domain/Application folders unless they have rules or workflows
that belong there.

See [architecture and placement](../../ARCHITECTURE.md),
[confirmed legacy components](../../LEGACY_COMPONENTS.md), and
[isolated verification](../../../../Tools/Verification/README.md).
