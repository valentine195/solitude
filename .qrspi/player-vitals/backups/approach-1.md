# QRSPI Approach: player-vitals

## Context

This feature adds hunger and segmented passive health recovery to the existing health/O2 systems and HUD. The intended pressure is reduced access to recovery when food is scarce. Hunger itself does not damage or kill the player. Food production, ship upgrades, and balancing through playtesting are deferred.

The human has already settled four equal initial health segments, configurable segment count, recovery stopping at an exact boundary, a simple hunger threshold, medical healing across boundaries regardless of hunger, slow hunger depletion during active gameplay, and fixed food restoration. These decisions constrain every candidate; they are not alternatives being reopened. Recovery delay, rate, threshold, depletion, and restoration amounts remain provisional tuning choices. No prior approach was supplied.

Research establishes a small continuous-vital model and an event-driven HUD, but no existing passive recovery or gameplay food/medical use. This comparison concerns ownership of the new rules and their integration, rather than a redesign of inventory, oxygen, succession, or persistence.

## Constraints and Facts

- **Human direction:** `.qrspi/player-vitals/request.md:9–15` records recovery, hunger, UI, and deferred upgrades; `:25–37` settles active-gameplay depletion, provisional tuning, four equal segments, and exact-boundary stopping. Configurable segment count does not authorize implementing medical-system upgrades.
- **Current values are continuous:** `Assets/_Project/Features/Player/Vitals/Vital.cs:7–12` stores current/maximum values and publishes change/depletion events. `:22–46` clamps setters and exposes signed addition and maximum changes. Segment boundaries can therefore constrain passive recovery without quantizing damage or medical healing.
- **Damage has no distinct identity today:** `Vital.cs:41` implements addition through the general setter. A decrease alone cannot distinguish damage from reset or maximum clamping. Treating every decrease as damage is a compatibility risk, not an established requirement.
- **Model identity matters:** `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27–32` retains vital objects specifically to preserve HUD subscriptions. `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs:24–36` resets them during awakening. New recovery state must account for that lifecycle; replacing the bound model casually would break an existing pattern.
- **Active gameplay is broader than an advancing Unity clock:** `Assets/_Project/Application/RuntimePolicy.cs:36–37` excludes not-ready, transition, modal, primary-screen, and paused states. `Assets/_Project/Core/Time/TimeSystem.cs:10–14` applies pause scale to Unity time, while `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:68–71` gates transitions without acquiring a pause lease. Scaled time alone cannot establish gameplay eligibility.
- **Composition can supply the necessary dependencies:** `Assets/_Project/Core/Composition/SceneFeatureContext.cs:15–23` exposes inventory commands, carried containers, selection, and gameplay policy. `Assets/_Project/Core/Composition/SceneFeatureComposition.cs:5–12` supplies scene-owned initialization/release. `Assets/_Project/Application/Tasks/TaskNoticeCoordinator.cs:86–97` demonstrates a separately advanced coordinator with gameplay gating.
- **The current HUD has a reusable foundation:** `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17–47` handles late availability, immediate synchronization, and disable/re-enable subscriptions. `Assets/_Project/Features/Player/Vitals/Views/VitalSlider.prefab:53–70` uses a horizontal layout; `:423–425` binds fill and kind. Segment/threshold decoration and adjacent recovery text extend this foundation, but text cannot depend solely on health-value changes because hunger and delay can change while health stays constant.
- **Maximum changes have an existing notification gap:** `Vital.cs:43–46` does not publish a change when only the maximum changes and current remains unchanged. A marker/fill solution relying exclusively on the current event could become stale. Invalid maximum/configuration values and floating-point boundaries are additional risks identified in `.qrspi/player-vitals/research.md:208–233`.
- **Consumption needs a new connection:** Research `:42–71` and `:107–134` found no gameplay food/medical effects. `Assets/_Project/Features/Items/Core/ItemDefinition.cs:8–30` contains presentation, inventory, and identity fields, rather than effects. `Assets/_Project/Domain/Restoration/RestorableSystemState.cs:29–35` demonstrates inventory removal coupled to a state commit before notification; this is relevant precedent, not an existing consumable-vitals implementation.
- **Assembly ownership constrains reuse:** `Assets/_Project/Domain/SOLITUDE.Domain.asmdef` has no engine references; Application references Domain, and `Assets/_Project/SOLITUDE.Runtime.asmdef` references both. Existing feature-local vitals belong to Runtime. A Domain rules component cannot depend on those Runtime types. `README.md` assigns independent rules to Domain, coordination to Application, and runtime adapters/composition to Core or their concrete feature.
- **Persistence and verification evidence are limited:** Research `:149–159` found no player-vitals save fields; `:235–269` records existing HUD/reset assertions and bounded absences of recovery/consumable tests. Those are source findings, not successful test runs. No tests or playable checks were executed for shaping.

## Candidate Approaches

### Option A: Extend the Unity vitals controller directly

Keep hunger, recovery timers, segment calculations, tuning, and recovery status inside the existing player controller. Retain the continuous Vital objects and extend the current HUD. A narrow food/medical adapter connects inventory use to these controller-owned effects.

**Strengths:** Smallest structural change; scene authoring already provides current/maximum values. Existing HUD and awakening bindings remain familiar. This is compatible with the settled rules and does not require moving existing model types across assemblies.

**Costs and risks:** Gameplay-policy injection is still needed; a simple controller Update using delta time would miss transition and screen gating. Timing, Inspector updates, resets, and damage classification share a lifecycle-heavy component. Independent rules verification is harder, with more reliance on Unity fixtures. Status updates can become scattered among health, hunger, and clock callbacks. Raw setter observation risks counting reset/clamping as damage.

**Operational and rollback implications:** Scene/controller initialization is the main failure boundary. Rollback is comparatively local, although newly authored HUD and consumable references must remain consistent with the reverted controller. There is no demonstrated need for a vitals save-format migration.

### Option B: Separate vitals rules from runtime integration

Use a focused Unity-independent rules component for segment limits, hunger gating, recovery delay, and recovery status, with a thin runtime adapter responsible for eligible elapsed time, player lifecycle, and effects on the retained Vital objects. Keep authoritative current values in one place; a rules component in Domain can work from independent value/configuration inputs without referencing Runtime types or maintaining a competing copy of health. Coordination and Unity presentation respect the existing assembly direction.

The existing bars remain continuous fills with segment and threshold overlays; status reflects the same recovery reasoning used by gameplay. Food and medicine use a focused connection to inventory commands rather than introducing a universal item-effect framework.

**Strengths:** Separates mathematical and temporal rules from scene ordering, making exact boundaries, delayed recovery, hunger gating, and large elapsed intervals easier to reason about and verify independently. A single explanation of recovery state reduces disagreement between HUD text and actual healing. Retained model identity and current initialization behavior can remain compatible. Configurable segment count is accommodated without building deferred upgrades.

**Costs and risks:** More explicit wiring than A. The adapter must avoid duplicate advancement, stale player bindings, and inherited cooldown state after awakening. Damage attribution still needs deliberate ownership; separating rules does not magically classify existing setters. Cross-assembly extraction must avoid reverse dependencies or unnecessary movement of existing assets/types. Inventory removal and vital effects introduce a notification/failure boundary: observer failure after mutation must not lead to replaying consumption.

**Operational and verification implications:** Recommend gameplay-policy-gated scaled elapsed time for passive systems, including recovery delay; that clock choice is a recommendation, not a settled human decision. It freezes during excluded gameplay states and follows the existing simulation scale. Independent rule checks would cover the numerical/time concerns, while Unity checks would cover composition, HUD readiness/status, consumption, and retained-avatar reset. This is a comparison of verification needs, not a test plan or executed evidence.

**Migration and rollback implications:** Narrow adapters and preserved Vital identity keep compatibility costs moderate. No vitals persistence extension is implied. Reverting the rules and their adapter is more contained than reverting a general effects system, though scene/HUD references and any new consumable definitions remain coupled to that change.

### Option C: Introduce a data-driven consumable/effect system

Combine vitals recovery rules with a reusable item-effect layer whose authored definitions describe food, medicine, and potentially other vital effects. A common use coordinator owns consumption, policy checks, effect execution, and presentation feedback. Recovery itself remains independently governed by the settled segment/hunger rules.

**Strengths:** Strong fit if multiple upcoming item types need reusable use behavior. Centralized item-use handling can keep inventory accounting and effect application consistent, with authorable restoration amounts. It can support the current food/medicine behavior without changing the human's recovery decisions.

**Costs and risks:** Highest complexity and authoring footprint. No existing effect metadata or use pipeline was established, so this adds infrastructure as well as the requested vitals feature. Generic effect composition creates extra failure ordering, validation, and duplicate-use concerns. It can draw oxygen effects, new input flows, and wider inventory UI into scope without evidence that they are needed now. Verification must cover generic dispatch and authored definitions in addition to vitals behavior.

**Migration and rollback implications:** Existing item identities and authored assets would need careful compatibility treatment. Inventory persistence makes deletion/recreation of item definitions consequential (`ItemDefinition.cs:23–30`). Rollback is broader because use wiring, effect definitions, and presentation references become interdependent. This option is compatible in principle, but its additional infrastructure is not approved scope.

## Recommendation

Recommend **Option B**. The request introduces interacting timing, threshold, segment, and lifecycle rules, while the current project already supports separate independent rules and composed runtime adapters. B offers a clearer place to reason about those interactions than controller-only ownership, without the unproven generalization cost of C.

Retain continuous authoritative vitals and their object identity. Prefer generated marker overlays driven by the same configuration as recovery over four hard-coded bar slices: this fits configurable segment count and the existing fill-based HUD with less layout/state duplication. Prefer a focused food/medical integration using the existing inventory command discipline. Both are architectural recommendations, not approval to expand inventory interaction scope.

The approved feature direction is the behavior recorded in request.md. This recommendation does not approve a specific architecture, clock interpretation, item activation gesture, tuning values, death integration, saving vitals, ship upgrades, food production, or a generic consumable framework. The Decision remains unselected.

## Decision

The human approved Option B on 2026-10-12: separate Unity-independent vitals rules from runtime integration, retain the existing authoritative Vital objects and HUD bindings, and use focused food/medical integration. Rationale: this keeps boundary and timing rules independently testable without the scope of a general consumable framework.

The human also approved a dedicated Use action for the selected hotbar food/medical item and a shared gameplay clock for hunger, regeneration, and the post-damage delay. These clarifications are recorded in request.md and require refreshed discovery before Spec.

Approved comparison revision before recording this decision: SHA-256 d8e476608618ae513c555155302b1f3164725928acec1e51a6d188d80d1cb2f6.

## Open Questions

- **Human clarification through Query:** Research establishes that gameplay food/medical consumption is absent, but the inputs do not select an activation gesture or delivery surface. Confirm the intended minimal player-facing consumption interaction before expanding hotbar/inventory behavior. This is missing intent, not a repository fact to infer through more source searching.
- **Human clarification through Query:** Active gameplay is settled for hunger depletion; scaled versus real eligible seconds and whether the post-damage delay shares that clock are not explicitly settled. The recommendation above supplies a compatible choice for human consideration.
- **Research evidence:** No blocking source-evidence gap was found for choosing between these architectures. Runtime marker/status layout and the stale O2 color overrides reported in research.md:103–105 remain unverified visually. If rendering evidence becomes necessary to select a different HUD approach, return that bounded question to Research; this shaping pass did not run the game or tests.
