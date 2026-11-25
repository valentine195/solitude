# SOLITUDE architecture and ownership

Ownership follows lifetime and gameplay responsibility. Folder symmetry is not
a requirement. See the [repository placement rules](../../README.md) and
[composition guide](Core/Composition/README.md) for authoring details.

| Owner | Responsibility and lifetime |
| --- | --- |
| `GameCompositionRoot` | Persistent bootstrap: session, frozen item catalogs, persistence wiring, task journal, shared container gesture service, pause/input policy, and scene discovery/release. Supports one composed player scene. Production and disposable demo sessions cannot be mixed. |
| `SceneBindings` | Scene coordinator: authored player/container/pickup endpoints, registration and presentation leases, per-scene task scope, interaction scope, and the ordered list of feature compositions. Registers inventory before features; releases features before their shared dependencies. |
| `SceneFeatureComposition` | Feature-local equipment, screens, task sources, validation, binding, Back handling, transition close, and cleanup. `ScrubberComposition` is the example. Serialized list order controls feature Back priority. |
| `SceneInteractionScope` | Scene-local focus/trigger events and feedback service; explicitly binds player, UI, and feedback receivers, including inactive objects. `SceneBindings` owns it in composed scenes; standalone `SceneInteractionBindings` owns it in the two SolitudeStart scenes. |

The session journal outlives a scene; its scene task publishers do not. The
feature context carries explicit, read-only service references and the scene
task scope. It is not a service locator. Back priority is journal, then feature
screens, then container modal; modal Back cancels a held drag before closing.
Transitions gate input and close scene screens. Release unwinds features in
reverse list order, including a feature whose initialization failed.

## Assembly boundaries and feature placement

`SOLITUDE.Domain` contains engine-independent rules, state, policies, and
transactions. `SOLITUDE.Application` contains engine-independent workflows,
coordinators, and presentation orchestration and references Domain. Both
assemblies have `noEngineReferences: true`.

`SOLITUDE.Runtime` references those assemblies and Unity packages. `Core` owns
Unity composition and adapters; `Systems` owns shared runtime services/views;
`Features` owns concrete gameplay and its exclusive authoring, prefabs, screens,
and scene compositions. Editor tools and tests keep their existing assembly
boundaries. A feature only needs Domain rules or Application workflows when
there is meaningful behavior to extract; a Unity-only feature need not have
either layer or its own assembly definition.

Dynamic pickups still register through the current owning
`SceneBindings.RegisterPickup`. Retain the returned lease and dispose it when
despawning; scene release also clears remaining pickup bindings. Do not bypass
this path or move registration into a global feature service.

Keep existing serialized scene/prefab references and `.meta` GUIDs. Use deliberate
Editor migrations for binding changes; do not rebuild scenes to replace a
migration. No folder moves are needed to adopt feature ownership. The scrubber
demo remains intentionally disposable and does not write production world state.
Retained inert scripts are cataloged in [LEGACY_COMPONENTS.md](LEGACY_COMPONENTS.md).
