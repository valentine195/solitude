# QRSPI Approach: player-vitals

## Context

This feature adds hunger and segmented passive health recovery to the existing health/O2 systems and HUD. Food scarcity constrains recovery; empty hunger does not directly damage or kill the player. Food production, ship upgrades, and balancing through playtesting remain deferred.

The human already approved Option B: separate Unity-independent rules from runtime integration, retain authoritative Vital objects and HUD bindings, and use focused food/medical integration. This refresh preserves that decision and its rationale from `.qrspi/player-vitals/backups/approach-1.md:71–75`. The current request now explicitly records the approved dedicated hotbar Use action and shared gameplay-gated scaled clock. Current research supports that architecture; no conflict with the prior decision was found. The alternatives below explain tradeoffs without reopening approval.

## Constraints and Facts

- **Settled behavior:** `.qrspi/player-vitals/request.md:9–15,25–37` establishes four equal initial health segments, configurable segment count, exact-boundary stopping, hunger-threshold gating, medical healing across boundaries even when hungry, slow active-gameplay hunger depletion, fixed food restoration, and the required HUD markers/status. Numerical tuning is provisional; playtesting is deferred.
- **Settled interaction and timing:** `request.md:39–45` selects a dedicated Use action for the selected hotbar food/medical item. Hunger, regeneration, and the post-damage delay share scaled elapsed gameplay time, stopping during menus, pause, transitions, and other ineligible states. These are approved scope, rather than architectural suggestions awaiting approval.
- **Continuous authoritative values:** `Assets/_Project/Features/Player/Vitals/Vital.cs:7–12,22–46` stores floating-point current/maximum values, clamps setters, and exposes change/depletion events and signed addition. Segments can limit passive recovery without discretizing damage or medical healing. There is no distinct damage operation: observing a decrease cannot distinguish damage from reset or maximum clamping.
- **Retained identity is deliberate:** `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:27–32` retains Vital objects to preserve HUD subscriptions. Research `research.md:72–98` establishes retained-avatar awakening resets and authored initialization, with no vitals persistence in the inspected save path. New timing state must respect this lifecycle; a save-format extension is not implied.
- **Elapsed time and eligibility are separate:** Research `research.md:107–125` establishes scaled `Time.deltaTime`, pause scaling, and `InputPolicyCoordinator.Gameplay` eligibility. A primary screen such as the scrubber can block gameplay while time scale remains nonzero. Scaled delta alone therefore cannot satisfy the approved clock semantics.
- **Composition and assembly compatibility:** `Assets/_Project/Core/Composition/SceneFeatureContext.cs:15–23` exposes inventory commands, hotbar, selection, policy, and input; `SceneFeatureComposition.cs:5–12` establishes scene-owned initialization/release. `Assets/_Project/Domain/SOLITUDE.Domain.asmdef:2–5` has no engine references; Application references Domain (`Assets/_Project/Application/SOLITUDE.Application.asmdef:3–7`), and Runtime references both (`Assets/_Project/SOLITUDE.Runtime.asmdef:3–8`). A Domain rules component cannot depend on existing Runtime Vital types.
- **Use integration is new:** Research `research.md:129–147,194–205` finds digit-based hotbar selection and generic validated inventory removal, but no consumable effects, dedicated use pipeline, or full-value use policy. Selection can be empty and can clear after removal. Existing item definitions lack restoration metadata; item IDs participate in persistence (`research.md:45–49`). A focused use connection is needed even under the smallest candidate.
- **HUD foundation and notification limits:** Research `research.md:56–69,150–173` establishes continuous fills, immediate/late binding, disable/re-enable subscriptions, and a label-plus-bar layout. There are no existing segment/threshold markers or dynamic recovery status. `Vital.cs:43–46` does not notify on a maximum-only change when current stays unchanged. Health change events alone also cannot describe hunger or cooldown changes while health is constant.
- **Edge and verification limits:** Research `research.md:175–191` documents absent configuration validation, unclamped constructor values, and maximum-change notification gaps. `research.md:208–220` identifies existing HUD/reset, hotbar selection, input gating, and synthetic elapsed-time checks, with bounded absence of dedicated recovery/consumption checks. These are source findings, not passing verification results. No tests or playable checks were run for this shaping pass.

## Candidate Approaches

### Option A: Extend the Unity vitals controller directly

Own hunger, segment calculations, recovery timing/status, and tuning in the existing player controller. Preserve continuous Vital objects and extend the current HUD, with a focused adapter connecting the approved hotbar Use action to inventory removal and food/medical effects.

**Fit and strengths:** Supports all settled behavior with the smallest structural change and familiar scene authoring. Existing model identity and bindings can remain intact. It avoids moving Runtime types across assemblies.

**Costs and failure modes:** Gameplay eligibility still needs explicit integration. Controller lifecycle, reset handling, damage attribution, timing, and Inspector changes become tightly coupled. Health, hunger, delay, and configuration updates can produce inconsistent HUD status if handled separately. Independent mathematical/time verification is less convenient and more dependent on Unity fixtures.

**Operational and rollback concerns:** Initialization and retained-avatar resets are the principal lifecycle risks. Rollback is comparatively local, but input, scene/HUD references, and consumable metadata must stay consistent with the controller. No demonstrated vitals save-format migration is needed.

### Option B: Separate vitals rules from runtime integration — approved

Keep segment limits, hunger gating, recovery delay, and recovery reasoning in a focused Unity-independent rules component. Runtime integration supplies eligible scaled elapsed time and lifecycle/damage context and applies effects to retained authoritative Vital objects. Domain rules use independent value/configuration inputs rather than referencing Runtime models or keeping a competing authoritative health copy.

The approved dedicated Use action connects selected hotbar food/medicine to focused effects and existing inventory command discipline. HUD fills retain their current binding foundation; markers and recovery status reflect the same configuration and reasoning as the rules.

**Fit and strengths:** Supports the approved behavior and architecture while respecting assembly direction. Boundary mathematics and eligible-time accumulation can be examined independently of scene ordering. Shared recovery reasoning reduces divergence between displayed status and healing. Configurable segments do not require implementing deferred upgrades or replacing continuous vitals.

**Costs and failure modes:** Requires more explicit wiring than A. Duplicate advancement, stale player bindings, retained cooldown state across awakening, and misclassified reset/clamping changes remain risks. Inventory removal and vital effects cross a mutation/notification boundary; partial failure or observer exceptions can cause lost items or duplicate effects if treated as safe to retry. Rules separation does not by itself solve those ownership issues.

**Verification and operations:** Independent rule verification is a better fit for boundary, threshold, delay, and elapsed-interval reasoning; Unity integration evidence remains necessary for input eligibility, HUD readiness/status, consumption, and retained-avatar lifecycle. This describes comparative verification needs, not a test plan. Scene release and gameplay eligibility must govern advancement together.

**Migration and rollback concerns:** Preserved model identity and narrow adapters keep compatibility costs moderate. No vital persistence extension is implied. Rollback still couples new input wiring, consumable definitions, and HUD references to the rules integration, but has less reach than a generalized effect system.

### Option C: Introduce a general data-driven consumable/effect system

Add a reusable authored item-effect layer and common use coordinator alongside independent recovery rules. Food and medicine become instances of a broader effect dispatch and consumption mechanism.

**Fit and strengths:** Can support the approved behavior and centralize inventory/effect handling. It becomes attractive if multiple additional item categories demonstrably need shared use semantics.

**Costs and failure modes:** Highest infrastructure and authoring cost. Research establishes no existing effect pipeline or requirement for broader effects. Generic dispatch adds validation, ordering, and duplicate-use concerns, and increases verification obligations beyond the requested food/medical connection. Expanding oxygen effects or other interaction surfaces would require additional scope approval.

**Migration and rollback concerns:** Existing item identity and persistence make asset replacement consequential. Effect definitions, dispatch, input, and presentation references become more interdependent, broadening rollback. This general framework is not approved scope.

## Recommendation

Continue with **Option B**, preserving the human's approval. It separates interacting boundary and timing rules from Unity lifecycle concerns, fits the current assembly/composition structure, and avoids the unsupported generalization of C. Current research resolves the prior discovery needs for the selected Use action and clock; it does not establish a reason to reconsider the architecture.

Within B, prefer marker overlays driven by shared configuration over four hard-coded bar slices, and a common recovery-state explanation for gameplay and HUD status. Prefer deliberate damage attribution over interpreting every health decrease as damage. Favor focused food/medical metadata and coordinated inventory/effect handling over a universal effect framework. These are architectural refinements, not additional human-approved behavioral rules or prescribed API contracts.

The approved scope includes the recorded recovery/hunger/UI behavior, Option B, the dedicated hotbar Use action, and the shared eligible scaled clock. It does not settle numeric tuning, full-value consumption policy, physical Use binding, or detailed lifecycle/status choices. Death integration, vitals saving, ship upgrades, food production, and generalized consumables remain outside this approval. No conflict gate is needed on the available facts; a later finding that makes the approved approach incompatible must return to the human rather than silently changing the decision.

## Decision

The human approved Option B on 2026-10-12: separate Unity-independent vitals rules from runtime integration, retain the existing authoritative Vital objects and HUD bindings, and use focused food/medical integration. Rationale: this keeps boundary and timing rules independently testable without the scope of a general consumable framework.

The human also approved a dedicated Use action for the selected hotbar food/medical item and a shared gameplay clock for hunger, regeneration, and the post-damage delay. These clarifications are recorded in request.md and require refreshed discovery before Spec.

Approved comparison revision before recording this decision: SHA-256 d8e476608618ae513c555155302b1f3164725928acec1e51a6d188d80d1cb2f6.

## Open Questions

- **Remaining behavioral choices for the human:** Full-hunger/full-health use policy is unspecified: consuming without benefit versus rejecting use is not established by existing behavior. The physical Use binding, provisional numerical values (including medical restoration), and hunger/recovery-state handling during awakening also remain unselected. Detailed status precedence is not settled by the example hungry message. These choices do not require reconsidering Option B and should not be inferred as approved scope.
- **Evidence gaps for Research:** No blocking source-evidence gap remains for this architectural comparison. HUD marker/status rendering and readability have not been verified visually; if visual evidence becomes necessary to choose the presentation approach, return that bounded question to Research. No rendering or runtime correctness is claimed here.
- **Resolved prior questions:** `request.md:39–45` settles activation and timing; refreshed `research.md:107–147` supplies their current integration evidence. The Decision text above is preserved verbatim, including its historical discovery requirement; this research refresh addresses that requirement without changing the decision or transferring its historical revision approval to new behavioral choices.
