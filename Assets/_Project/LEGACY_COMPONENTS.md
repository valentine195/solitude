# Legacy component inventory

Verified against repository source and serialized GUID references on 2026-10-08.
This is a retirement inventory, not authorization to remove anything. All listed
scripts and metadata remain intact. Paths are relative to `Assets/_Project`.

## Confirmed inert candidates

Source searches across `Assets`, `Packages`, and `Tools` found no C# consumers of
these five types beyond their declarations. Each class body is empty: the four
MonoBehaviours have no serialized fields, lifecycle callbacks, or runtime
methods. `GameInput` is an empty static class, not a component.

| Script | Classification and retained references | Current responsibility owner |
| --- | --- | --- |
| [Core/Input/GameInput.cs](Core/Input/GameInput.cs) | Inert source compatibility placeholder. No serialized GUID references or callers found; external consumers are not established. | `UnityInputHost` creates and disposes `SOLITUDE_InputActions`. |
| [Core/Input/InputRouter.cs](Core/Input/InputRouter.cs) | Inert serialized marker in all four composed gameplay/reference scenes below and `Core/Input/SYS_InputRouter.prefab`. | `InputPolicyCoordinator` controls policy; `UnityInputHost` applies it to actions and the bound player. |
| [Core/Input/Routers/GameplayRouter.cs](Core/Input/Routers/GameplayRouter.cs) | Inert serialized marker in the four composed scenes. It does not implement `IInputRouter`. | `UnityInputHost` routes gameplay actions to explicitly bound endpoints. |
| [Systems/Containers/Controller/ContainerInteractionHub.cs](Systems/Containers/Controller/ContainerInteractionHub.cs) | Inert serialized marker in the four composed scenes. | `GameCompositionRoot` creates shared `ContainerGestureCoordinator` with the Domain `ContainerInteractionController`; `SceneBindings` wires local controllers. |
| [Systems/Containers/Controller/ContainerUIController.cs](Systems/Containers/Controller/ContainerUIController.cs) | Inert serialized marker in the four composed scenes. | `SceneBindings` owns the endpoint registry/modal binding; `ContainerController` owns each active presenter. |

The four scene files containing direct script references are
`Scenes/Wakeup.unity`, `Scenes/Demos/ShipSystemRestoration.unity`,
`Scenes/Demos/AtmosphericScrubberEncounter.unity`, and
`Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity`.
`SYS_InputRouter.prefab` is also instantiated in both SolitudeStart scenes;
`Features/World/SolitudeStart/Editor/SolitudeStartBuilder.cs` still loads that
prefab. Removing the marker does not authorize deleting the prefab or its root.

Reference keys, in table order:
`6cfd9fb837efc449c9b22e0562c7ed8c`,
`0d3c26e7a4422422f8f42b81d451cbb0`,
`84184c7a6596d4b9f888719091399963`,
`cbc8ba7ada22747099c2cc3b5cef5048`, and
`b7b4ecfaaa5ba4300ba430676dea7f10`.
The router prefab retains an old `m_EditorClassIdentifier` and `InputEnabled`
value. Its `m_Script` GUID references the current empty `InputRouter`; historical
serialized labels/fields are not evidence of active routing. Preserve these
values until a deliberate future migration.

## Active and unclassified neighbors

| Code | Evidence / retirement status |
| --- | --- |
| `Core/Input/UnityInputHost.cs` and generated `SOLITUDE_InputActions.cs` | Active: `GameCompositionRoot` initializes/releases the host; `SceneBindings` binds endpoints and UI. The host subscribes action callbacks and applies input policy. Do not retire with the router markers. |
| `Systems/Containers/Controller/ContainerController.cs` and `ContainerPanel.cs` | Active: scene composition and modal binding call controller bind/unbind; the panel requires and forwards to a controller. Controllers own Application presenters. |
| `Systems/Interaction/Core/SceneInteractionBindings.cs` | Active standalone scene owner, with Awake/destroy binding lifecycle. Not a legacy marker despite its use in older scenes. |
| Other prototype helpers, input assets, and marker-like scripts | Unclassified by this focused inventory. Lack of a caller or an old name alone does not prove inertness or safe removal. |

## Safe future retirement

For `GameInput`, repeat whole-source and external-consumer checks before removing
the source asset and its metadata together. No serialized migration is currently
indicated by the GUID scan.

For each MonoBehaviour marker, first inventory its GUID references, prefab
instances/variants, builder dependencies, and any references to its component
file IDs. Remove only the marker component through Unity Editor APIs, preserve
other components and existing GUIDs, and migrate each affected scene/prefab
deliberately. Keep the script until no serialized or code consumers remain;
ensure builders do not recreate it. Then remove the script and metadata together,
validate metadata and missing-script references, and run composition/direct-entry,
input, modal, and scene lifecycle regressions. Do not combine retirement with
gameplay, persistence, or folder-layout changes.
