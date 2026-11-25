# QRSPI Approach: survivor-succession

## Context

The feature introduces distinct mortal survivors who automatically succeed one another in the same live ship scene. The replacement awakens in the cryobay with an empty inventory; the deceased survivor’s carried equipment remains recoverable at the death location. Ship restoration and AI continuity span survivors.

Scene transitions and full save/load are follow-up work. The current scope therefore favors preserving the live scene rather than serializing and reconstructing the ship on death. Traits, skills, injuries, and roster selection need an extension boundary, not implementation now.

## Constraints and Facts

- Player endpoints are authored and retained by `SceneBindings`; initialization also binds containers, interaction, UI, restoration junctions, and scene features (`Assets/_Project/Core/Composition/SceneBindings.cs:21`, `:131`, `:166`).
- Inventory and hotbar use fixed active-player IDs. Registration restores existing contents; disposing and registering them again does not itself produce an empty survivor (`Assets/_Project/Domain/ContainerSaveSession.cs:10`, `:60`, `:129`).
- The interaction scope requires exactly one scene-local player interactor, including inactive objects discovered by its endpoint search (`Assets/_Project/Systems/Interaction/Core/SceneInteractionScope.cs:21`).
- Repair, scrubber, and door outcomes live on world objects. Reload currently resets those outcomes. Scrubber availability also depends on its bound inventory reader; its recovery stops while that binding is unavailable (`Assets/_Project/Features/World/ScrubberEncounter/ScrubberEquipment.cs:14`, `:19`, `:47`; `Assets/_Project/Tests/PlayMode/RestorationSliceTests.cs:127`).
- Vitals expose depletion events, but there is no death consumer or survivor identity model. Vital views retain the Vital resolved when enabled (`Assets/_Project/Features/Player/Vitals/Vital.cs:22`; `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs:36`; `Assets/_Project/Features/Player/Vitals/Views/VitalBarView.cs:17`).
- Container commands already support transfers with owner generations and slot revisions. They do not provide a complete death handoff across both player containers (`Assets/_Project/Domain/ContainerCommandService.cs:125`).
- Existing feedback can display an AI acknowledgment, but first-contact state is scene-local and no succession event exists (`Assets/_Project/Features/World/OpeningSlice/OpeningFirstContact.cs:20`; `Assets/_Project/Systems/Interaction/Core/SceneInteractionScope.cs:19`).
- Neither a runtime awakening marker nor a death-location recovery object exists. Save data lacks survivor records, recovery locations, and restoration flags (`Assets/_Project/Scenes/Wakeup.unity:529`; `Assets/_Project/Domain/SaveGameData.cs:9`).

These are source findings. Tests were inspected, not run.

## Candidate Approaches

### Option A: Retained avatar with distinct survivor records

Keep the authored player GameObject and its existing bindings. Treat it as the controllable representation of the active survivor, separate from the survivor’s identity and lifetime. Death retires that identity, places its inventory and hotbar contents in a recoverable world container, and hands the avatar to a fresh survivor at an authored awakening location.

**Strengths:** Lowest compatibility cost for current input, interaction, camera, UI, fixed inventory IDs, and ship-system bindings. Keeping the world objects alive naturally preserves runtime repairs and door states. Distinct survivor records provide room for later character attributes and selection without imposing a roster framework now.

**Costs and risks:** Resetting character-owned state must be explicit; Unity initialization will not run again merely because identity changes. Vitals, selection, movement input, focus, open screens, and pending gestures can leak across survivors if omitted. Fixed player container IDs must represent active-player access, not permanent survivor identity. Future character components must participate in the handoff.

**Operational behavior and verification:** A guarded transition must conserve equipment and prevent repeated death or partial recovery creation from advancing succession twice. Verification emphasizes repeated succession, state isolation, inventory-plus-hotbar conservation, normal interaction afterward, and unchanged ship outcomes. Failed transitions should keep gameplay gated and preserve recoverable ownership.

### Option B: Fresh player prefab with explicit rebinding

Retire the old player object and instantiate a replacement prefab in the existing ship scene. Survivor identity follows the new object; scene composition reconnects its inventory, hotbar, input, interaction, camera, vital views, and feature consumers.

**Strengths:** Fresh component initialization creates a clearer physical boundary between characters and reduces accidental retention of component-local state. This better fits future survivors with substantially different bodies or component configurations.

**Costs and risks:** Requires a broader player-rebinding capability. Overlapping old and new interactors conflict with current validation, and simultaneous registration conflicts with fixed container IDs. A corpse cannot simply retain the old player hierarchy. Stale readers, singleton resolution, UI subscriptions, and feature references create additional failure paths. Replacing the prefab still does not solve inventory transfer or AI continuity.

**Operational behavior and verification:** Replacement failure requires coordinated recovery of both object lifetime and bindings. Verification must cover all retained player references, owner-registration ordering, teardown, and interrupted replacement. Rollback is more involved because destroying the old object removes the previous usable endpoint.

## Recommendation

Choose **Option A** for the initial runtime scope.

The request requires a new survivor identity, not necessarily a new Unity object. Retaining the authored avatar preserves the project’s existing composition assumptions while establishing an explicit boundary between survivor state and ship state. Keep succession coordination within existing composition patterns, without making ship systems aware of death or survivor selection.

For recovery, prefer one dedicated container at each death location over converting every stack into an authored pickup. Existing transfers support partial recovery and inventory capacity limits; recovery ownership must remain separate from the active survivor. Conserving both inventory and hotbar contents remains necessary under either option.

Keep AI continuity outside the active survivor record and use existing visible feedback for the initial acknowledgment. The human confirmed that a brief visible AI message is sufficient.

Option A’s principal tradeoff is explicit reset discipline. Option B becomes more attractive if later survivor differences require separate component configurations. Separating identity from the avatar now leaves that evolution available. Durable survivor, ship, and recovery persistence remains follow-up work; saving container contents alone would not preserve recovery locations or restoration state.

## Decision

Option A — retained avatar with distinct survivor records. Approved by the human for approach revision `f7e4f5a2e9cfc73fb9825093f34238847dfb509f964d3c0f7d47359d724192ed`. This minimizes changes to existing composition and ship bindings for runtime succession; character-owned state resets explicitly and equipment moves to independent recovery ownership.

## Open Questions

