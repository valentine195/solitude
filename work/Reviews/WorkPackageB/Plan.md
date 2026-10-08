# Work Package B plan

Replace InteractionEventBus and static InteractionFeedback with disposable scene-owned SceneInteractionEvents and InteractionFeedbackService. Preserve their metadata GUIDs and every authored view field. Search found PlayerInteractor, UIController, ScrubberTaskSource, OpeningFirstContact, SolitudeStartCompletion, plus three test fixtures using static APIs.

SceneBindings creates/releases SceneInteractionScope after inventory bind and before feature/input bind. The scope validates and discovers inactive scene-local UIController/player and explicit IInteractionFeedbackReceiver components; it creates the services and binds endpoints. It releases player focus, subscriptions, receivers and feedback view references on failure/unload. SceneFeatureContext exposes the specific event source to ScrubberComposition. Task source activation/publisher eligibility remain unchanged.

Four composed scenes need no new serialized field. Two uncomposed SolitudeStart scenes need a small SceneInteractionBindings owner, authored/migrated through Unity editor APIs. Their existing inert legacy input behavior is outside B. Update their builder to retain direct-entry ownership.

UIController detaches on disable/release; re-enable subscribes once and replays current focus. PlayerInteractor targeting/range/order stay unchanged. Feedback service preserves message/color semantics and releases the view; view hide stops stale coroutines. Opening contact fragments stop when their scene binding releases.

Run scoped service/UI/player tests, task-source subscription cycles, feedback receiver and scene reload/unload tests, plus existing pure/EditMode/PlayMode suites in an isolated project. Retain the saved inactive scrubber pickup and camera/UI overrides; report their known failures separately. Stop after B.
