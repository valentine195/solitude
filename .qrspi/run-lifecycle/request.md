# SOLITUDE — Run Lifecycle, Death & Persistence

Invoke $qrspi-workflow to investigate, design, and plan the death/reset/persistence mechanics for SOLITUDE’s first playable vertical slice.

Follow the full QRSPI workflow, including phase isolation and approval gates. Do not begin implementation until the specification and plan have been approved.

Context

SOLITUDE is a Unity 2D top-down science-fiction roguelite centered on restoring an abandoned generation ship.

When a colonist dies, the ship AI awakens another individual from cryosleep. Characters are expendable, but the player’s progress rebuilding the ship is persistent.

Existing systems include inventory and containers, consumable items, vitals, combat/enemies, interactions, doors, and repairable ship systems.

The immediate goal is to integrate these into a repeatable expedition through the cryobay, maintenance area, and atmospheric scrubber.

Desired behavior

1. Character lifecycle

* Each colonist has independent vitals and inventory.
* Death permanently ends the current character’s run.
* A new colonist awakens in the cryobay with starting supplies.
* Investigate whether existing lifecycle and scene-management systems can support this without substantial restructuring.

2. Enemy lifecycle

* Enemies in hostile areas reset between expeditions.
* Enemies in permanently reclaimed areas should eventually remain cleared.
* Avoid respawning enemies simply because a scene was unloaded and reloaded during the same expedition.

3. Item lifecycle

* Common supplies may replenish in limited quantities between expeditions.
* Unique items and permanent discoveries should not regenerate arbitrarily.
* Critical mission items must remain obtainable after death, preventing progression softlocks.
* Investigate recoverable equipment from deceased colonists, but treat corpse retrieval as a deferred enhancement unless it is exceptionally inexpensive.

4. Persistent ship state

* Completed repairs remain completed.
* Permanently powered doors and unlocked shortcuts remain unlocked.
* Persistent world changes survive character death and subsequent expeditions.
* Maintain a clear distinction between run-specific and ship-persistent state.

5. Expedition lifecycle

* Distinguish character death from expedition completion.
* Design so the two can eventually have different reset rules.
* For the initial vertical slice, both may trigger the same world-refresh mechanism where appropriate.

Investigation scope

Examine the existing codebase for:

* Player lifecycle and death handling
* Inventory ownership and item containers
* Enemy spawning and lifecycle
* Interactable and repairable object state
* Scene loading and transitions
* Existing persistence or save mechanisms

Reuse existing architecture where practical. Identify genuine gaps rather than introducing speculative systems.

Constraints

* Prefer the smallest cohesive implementation supporting the first vertical slice.
* Avoid a generalized roguelite framework, procedural generation, or unnecessary abstractions.
* Keep state ownership explicit and existing gameplay systems modular.
* Do not add corpse retrieval, dynamic enemy repopulation, or territory-control systems in this iteration.
* Research only until there is sufficient evidence to select a minimal approach.
* Preserve the isolation requirements of QRSPI, particularly the independence of Query from repository findings.

Acceptance scenarios

1. Colonist A awakens, obtains supplies, takes damage, and dies. Colonist B awakens with fresh vitals and starting supplies.
2. An enemy defeated during expedition A returns in hostile territory on the next expedition.
3. A permanent scrubber repair completed by colonist A remains completed for colonist B.
4. A shortcut unlocked during expedition A stays unlocked.
5. Critical repair components cannot become permanently unobtainable because of a character death.
6. Leaving and reentering a scene during one expedition does not unintentionally reset enemies or loot.
7. Existing inventory, interaction, and ship-system implementations remain reusable without introducing redundant lifecycle logic.

Deliverables

Use the standard QRSPI artifacts.

The specification should clearly establish:

* State ownership and lifetime
* Death and expedition transition behavior
* Persistence boundaries
* Minimal required changes to existing systems
* Automated and manual validation scenarios
* Features explicitly deferred beyond the vertical slice

Start with the Query phase. Do not independently advance through approval gates or begin implementation.

## Clarifications

1. What starting supplies should each newly awakened colonist receive?

Answer: nothing

2. What event counts as expedition completion in this vertical slice, and what should happen to the surviving colonist and their inventory afterward?

Answer: for this vertical slice no completion state

3. Which common supplies should replenish between expeditions, and what quantities or limits should apply?

Answer: bandage, ration, just 1-2 in the slice

4. Must ship progress survive closing and reopening the game in this iteration, or only character death and scene transitions during the current play session?

Answer: yes

5. Which approach should be used?

Answer: Proceed with A

6. On reopening, should the living colonist resume at the saved location or in the cryobay?

Answer: Saved location
