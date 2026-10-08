# Work Package A implementation plan

Baseline: only two unrelated untracked art-review directories. Work is limited to scrubber composition, deliberate demo migration, its editor builder/validators, and regression tests.

## Dependency map
GameCompositionRoot owns ContainerSaveSession, ItemPresentationCatalog, TaskJournal, InputPolicyCoordinator and UnityInputHost. It discovers inactive SceneBindings, enforces one player scene and disposable/production isolation, resets the demo session on entry, and releases scenes. No changes planned here.
SceneBindings registers inventory/hotbar before binding equipment; creates one scene task scope; owns optional journal and container modals. Current scrubber wiring scans all inactive equipment/screens and initializes serialized taskSources.
ScrubberEquipment binds commands/inventory/catalog; ScrubberScreenView binds policy/input and equipment changes; ScrubberTaskSource binds journal/scope/policy/player with fuse-catalog/inventory eligibility, watches equipment/access and the existing static focus bus. Static interaction migration is deferred to B.

## Design and order
Add a SceneFeatureComposition MonoBehaviour base with Validate, Initialize(context), TryBack, CloseForTransition and Release. Use a serialized base-component array and scene-local coverage validation instead of discovery activation. The immutable context exposes commands, inventory reader, presentations, policy, input host, journal, scene task scope and interactor only. No resolver, reflection or DI framework.
ScrubberComposition explicitly owns scene equipment/screens/taskSources; validates scene membership, completeness, duplicate ownership, screen equipment and source access associations. Bind after player registration, including disabled/inactive endpoints. Track each endpoint before its bind for partial-failure cleanup. Unwind features in reverse order; detach sources, dispose screens, detach equipment before scene input/container leases. Feature component activation does not own scene lifetime; SceneBindings does.
Preserve Back priority journal -> feature array order -> modal. Scene owns task scope release.

## Serialization and authoring
Only AtmosphericScrubberEncounter.unity serializes taskSources. ScrubberScreen.prefab supplies the screen with a scene equipment override; preserve that override, all existing IDs and metadata. Add ScrubberComposition to the existing scene scope object through Unity editor APIs, move taskSources ownership and serialize feature reference. Update EnsureJournal/build authoring to retain journalView and author feature registration. CompositionMigration/WakeupValidation already use generic SceneBindings.Validate and need no scrubber branches. Add editor validation for endpoints without a composition. No scenes are rebuilt for migration.

## Verification
Record baseline pure and EditMode tests, then run all pure/EditMode tests and applicable PlayMode composition/task/demo/production suites in an isolated Unity project with disposable saves and fixture setup. Validate authored scenes and compile via Unity. Extend existing tests for authored bindings, missing/duplicate/wrong-scene endpoints, repeated release, inactive activation and failure unwinding. Report unrun checks precisely.
