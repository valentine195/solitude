## Requirements Clarity

- What determines starting Health, Oxygen, Hunger, movement speed, carry capacity, and weapon damage, and which values are shared across playable colonists?
- How are crew backgrounds and proficiency ranks represented, and which diagnostic information and repair actions currently depend on them?

## Scope Boundaries

- Which existing entry points let playtesters launch an authored encounter directly, and what setup or configuration does each accept?
- How does a launch assign the playable colonist and initialize the room's fault, and which parts of that initialization can vary independently while using the same room?

## Integration Points

- Which state determines whether the scrubber is producing airflow, and how do electrical faults and intake damage affect it?
- How do breakers, service panels, scrubbers, and corridor lights receive power, and what circuit relationships can an authored room represent?
- How do repair interactions check for a replacement part or salvaged hardware, update inventory, and apply the repair?
- How do diagnostics reveal a dead feed's breaker or a damaged linkage's replacement part, and which clues remain visible to a player without those diagnostics?
- How do tool interactions show available actions and costs before the player commits, and what visual and audio feedback can they provide during and after the action?
- How are SEC-0 patrol routes authored and activated in a maintenance room?
- How are doors triggered to close on player entry and reopen when a system such as airflow recovers?

## Edge Cases

- How does the game track a reusable item installed in an interactable, and what happens to the item and its effect if the action is interrupted, the player leaves and returns, or normal power is restored?
- Which character, inventory, and room states reset or persist when starting another launch with the same or a different background and fault?

## Success Criteria

- Which existing focused tests or playtest setups exercise direct encounter launches, character initialization, repair choices, or room responses to system changes, and how do they establish a repeatable starting state and observe results?
