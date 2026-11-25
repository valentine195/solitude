## Requirements Clarity

- How is the playable colonist selected or assigned when starting a run or entering an authored encounter, and what identity data follows that colonist into gameplay?
- What determines starting Health, Oxygen, Hunger, movement speed, carry capacity, and weapon damage, and which values are shared across playable colonists?
- How are crew backgrounds and proficiency ranks represented, and which diagnostic information and repair actions currently depend on them?

## Integration Points

- How can separate authored fault setups initialize the same scrubber room, and which state determines whether the scrubber is producing airflow?
- How do breakers, service panels, scrubbers, and corridor lights receive power, and what circuit relationships can an authored room represent?
- How do repair interactions check for a replacement part or salvaged hardware, update inventory, and apply the repair?
- How do diagnostics reveal a dead feed's breaker or a damaged linkage's replacement part, and which clues remain visible to a player without those diagnostics?
- How do tool interactions show available actions and costs before the player commits, and what visual and audio feedback can they provide during and after the action?
- How are SEC-0 patrol routes authored and activated in a maintenance room?
- How are doors triggered to close on player entry and reopen when a system such as airflow recovers?

## Edge Cases

- How does the game track a reusable item installed in an interactable, and what happens to the item and its effect if the action is interrupted, the player leaves and returns, or normal power is restored?
- Which character, inventory, and room states reset when replaying the same room with a different background or fault setup?

## Success Criteria

- Which existing focused tests or playtest setups exercise character initialization, repair choices, or room responses to system changes, and how do they establish a repeatable starting state and observe results?
