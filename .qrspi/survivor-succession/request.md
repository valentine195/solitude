# SOLITUDE — Survivor Succession

## Requested workflow scope

Use the repository’s installed QRSPI-X workflow to investigate and plan this feature. Begin with the appropriate requirements/discovery phase, following the workflow’s instructions.

Do not implement anything yet. Inspect the existing codebase, identify reusable capabilities, evaluate architectural implications, and produce a focused implementation plan. Keep the investigation and outputs concise.

## Feature: Survivor Succession

SOLITUDE is a top-down sci-fi survival roguelite set aboard an abandoned generation ship. Players control colonists awakened from cryosleep by the ship’s fragmented AI. Each colonist is mortal, but the ship and its restoration progress persist across successive survivors.

Core requirement: When the current survivor dies, the AI must be able to awaken a new colonist who continues restoring the same persistent ship without losing the accomplishments of previous survivors.

## Expected Behavior

1. Survivor identity: Each awakened colonist has a distinct identity and character-owned state.
2. Death: A survivor can die, permanently ending that character’s active participation. A debug-triggered death is sufficient for the initial implementation.
3. Succession: Following death, a new colonist awakens in the cryobay and becomes the player-controlled character.
4. Ship persistence: Previously completed repairs, activated systems, unlocked doors, and other permanent ship modifications remain intact.
5. Inventory recovery: Equipment carried by the deceased survivor is left at their death location and can be recovered by subsequent survivors.
6. AI continuity: The ship’s AI retains continuity across survivors and acknowledges the transition.
7. Future extensibility: The design should accommodate survivor traits, skills, injuries, and a surviving colonist roster without requiring those features now.

## Architectural Constraints

* Integrate with the existing player, inventory, interaction, ship-system, and scene infrastructure.
* Clearly distinguish survivor-owned state from persistent ship-owned state.
* Avoid coupling ship systems to the current survivor or the roguelite lifecycle.
* Prefer existing abstractions over new frameworks or manager hierarchies.
* Preserve compatibility with the existing Air Scrubber and powered-door functionality.
* Avoid unnecessary changes to unrelated systems.
* Identify whether persistence across scene transitions, runtime deaths, and full save/load cycles requires separate work.

## Acceptance Scenario

1. Survivor A awakens in the cryobay.
2. Survivor A collects equipment and makes a permanent change to a ship system.
3. Survivor A dies through a debug action.
4. Survivor B awakens in the cryobay with a distinct identity.
5. Survivor B finds the ship system in its previously modified state.
6. Survivor B travels to Survivor A’s death location and recovers their equipment.
7. Gameplay continues normally without resetting the ship.

## Out of Scope

* Combat and enemy AI
* Procedural generation
* Randomized survivor traits or character classes
* Full expedition management
* Advanced character progression
* Elaborate death animations or narrative sequences

## QRSPI Investigation Focus

Evaluate:

* Existing capabilities that can be reused
* Gaps in player lifecycle and state persistence
* How inventory ownership and recovery should work
* Architectural boundaries and potential coupling
* Minimal implementation phases and testing strategy
* Risks, unresolved decisions, and opportunities to simplify

Primary design objective: Establish survivor succession as an independent gameplay capability while keeping the ship’s restoration systems persistent and reusable, even if the game later pivots away from roguelite mechanics.

## Clarifications

1. Must the initial implementation preserve succession and recoverable equipment through scene transitions and full save/load, or should those be identified as separate follow-up work?

Answer: Follow up

2. Should a replacement survivor awaken automatically after death, or should the player explicitly initiate awakening?

Answer: Automatically for now, but make it extensible in the future (e.g., selecting from a list)

3. What should a new survivor start with: an empty inventory, standard starting equipment, or another defined baseline?

Answer: Empty

4. What is sufficient AI acknowledgment initially: a brief visible message, an existing dialogue interaction, or another form of feedback?

Answer: Yes
