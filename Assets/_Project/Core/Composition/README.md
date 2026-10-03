# Runtime composition

`GameCompositionRoot` owns the immutable catalog, session, pause/input policy, pickup collection and shared drag service. `GameRuntimeConfig` remains a designer-authored asset. `SaveGameService` only performs injected persistence and Unity lifecycle flushing.

`SceneBindings` explicitly lists the player, views, lockers and pickups. It binds inactive views and owns registration/presenter leases, including never-active pickups. Register dynamically spawned pickups through the owning scope's `RegisterPickup` and dispose the returned lease when despawning. Scope release runs before owner registrations are removed. One player scene is supported.

`SOLITUDE.Application` has no Unity references. Its coordinators own modal state, pause leases and transient selection. Container presenters observe read-only snapshots and route indexed, generation-qualified view events to the existing transfer service. Unity views resolve icons/text through their injected presentation catalog.

`UnityInputHost` owns one generated action asset shared with the EventSystem. It enables individual gameplay actions from policy; global inventory/back controls remain usable during container modals. Both modals pause the world and block movement, interaction and number keys. Escape cancels a drag first, then closes the screen. Closing releases only the modal's pause lease and preserves the base time scale.

Wakeup is wired through the Unity Editor migration (`SOLITUDE/Migrations/Compose Wakeup Runtime`). Legacy serialized input/controller markers remain inert to preserve script references. New scenes must explicitly configure a scope and use the existing persistent bootstrap or author their own bootstrap for direct entry. Runtime composition validation checks composed scenes during builds and is also available from `SOLITUDE/Validation/Validate Runtime Composition`.

Failed authoring or save restore gates input and preserves the original save. Recovery uses the bootstrap's explicit Start New Save command. It releases bindings, resets domain owners/facts, saves the fresh snapshot and reloads the active scene.
