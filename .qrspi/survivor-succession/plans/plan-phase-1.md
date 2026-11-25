## Phase 1: Basic succession

### Dependencies
- Depends on: none
- Why: Uses current authored player composition, containers, input policy and feedback. No later-phase output is required for the empty-equipment path.
- Outcome: A working empty-inventory succession slice. Until Phase 2, a death with carried items is explicitly rejected without mutation; this intermediate phase does not satisfy the full equipment criteria.
- Paths below are relative to the repository root. New files require Unity metadata where applicable.

### Step 1: Model distinct survivors and guarded transitions
- [x] Status marker
- Criteria: S1, S2, S9, S10, S11 — supports identity, irreversible retirement, automatic selection and safe continuation.
- Files: new `Assets/_Project/Domain/Survivors/SurvivorState.cs`; new `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs` and `ISurvivorSuccessionPorts.cs`; new `Assets/_Project/Tests/Domain/SurvivorStateTests.cs` and `Assets/_Project/Tests/Application/SurvivorSuccessionCoordinatorTests.cs`.
- Changes: Give survivors runtime-unique identities and active/dead status. Define a small guarded prepare/commit/activate/acknowledge flow with explicit completion/failure state; separate successor choice from retirement. Use narrow ports for avatar reset, equipment handoff and feedback, with no Unity or ship-system references. Bind death requests to the expected survivor identity and retain sufficient completion state to reject stale requests and resume failures. No roster framework or serialized character schema.
- Tests: S1/S2/S9 — A→B→C identities differ, retired records never become active, and delayed A requests cannot retire B. S10 — fake preparation failure produces no successor; fake post-commit failure retains the same pending successor and receipt for retry. S11 — compile existing Domain/Application assemblies without Unity references.
- Recovery: Establishes independently tested rules with no runtime wiring. Resume from failing pure tests; revert only these new feature files if necessary.
- Risk: Medium — the completion boundary must prevent identity or equipment work from repeating.

### Step 2: Compose empty-equipment awakening and reset in Wakeup
- [x] Status marker
- Criteria: S1, S2, S3, S6, S7, S8, S10, S11, S12.
- Files: new `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs`, `SurvivorAvatarAdapter.cs`, `SurvivorAwakeningPoint.cs`; existing `Assets/_Project/Core/Composition/SceneBindings.cs`, `SceneFeatureContext.cs`, `Core/Input/UnityInputHost.cs`, `Features/Player/Interaction/PlayerInteractor.cs`, `Features/Player/Vitals/PlayerVitalController.cs`; new `Assets/_Project/Editor/SurvivorSuccessionAuthoring.cs`, `Tests/Editor/SurvivorSuccessionAuthoringTests.cs`, `Tests/PlayMode/SurvivorSuccessionSliceTests.cs`; existing `Assets/_Project/Scenes/Wakeup.unity`. Update `Application/RuntimePolicy.cs` or the movement adapter only if existing reset APIs are insufficient.
- Changes: Author an opt-in composition and valid cryobay point through Unity tooling, preserving existing scene wiring. Initialize the first identity and placement; expose a debug/context action capturing the current identity. Gate input, close existing screens and cancel gestures explicitly even when no modal is open; neutralize held actions and movement, clear/rebuild proximity focus after relocation, clear selection, reset the existing Vital objects and reposition the retained avatar. Preserve camera and reader bindings. Publish one acknowledgment through scene feedback and retain AI continuity outside survivor state. Do not reload or release the ship scope. Validate wrong-scene/missing awakening configuration. Reject nonempty inventory/hotbar deaths until the recovery path exists.
- Tests: S1–S3 — actual Wakeup empty death yields B at the point with baseline vitals, empty containers and fresh selection. S7 — held input/open inventory or journal/pending gesture does not leak; movement, focus, camera and vital display work after reset. S6/S8 — an existing powered-door/first-contact state is preserved and one acknowledgment appears. S10 — invalid point and temporary nonempty-path rejection leave identity/items unchanged. S11/S12 — one interactor remains composed and relevant input/modal/composition checks pass; unconfigured scenes behave as before.
- Recovery: Working empty-death slice, isolated by opt-in composition. If authoring fails, restore only feature-added references/assets using a scoped patch; preserve unrelated scene changes. Leave equipped death blocked until Phase 2.
- Risk: High — stale input/nearby caches, screen leases and callbacks can leak across identity changes.
