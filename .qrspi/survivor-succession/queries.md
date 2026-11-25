## Requirements Clarity

- How is the player character currently created, placed at an awakening location, and made controllable?
- What character identity and character-owned state exist today, and where are they initialized, reset, or persisted?
- What death or incapacitation behavior currently exists, including debug triggers and its effects on gameplay?
- Which repair outcomes, system activations, door unlocks, and other permanent ship modifications currently remain intact when the player character changes?

## Scope Boundaries

- Which gameplay systems retain references to the player character, and how are those references established or refreshed when the character changes?
- Where are character-owned state and persistent ship-owned state currently separated or shared, particularly for the Air Scrubber and powered doors?

## Technical Constraints

- Which scene-transition and save/load assumptions depend on a single character identity, character instance, or inventory owner and would constrain later persistence of succession and recoverable equipment?
- What existing character data or ownership abstractions represent identity and could accommodate later traits, skills, injuries, or inactive colonists?

## Integration Points

- How are carried and equipped items owned, transferred, dropped at a world location, and recovered through interaction?
- How is the cryobay awakening location represented, and what existing behavior determines whether a replacement character can be created there automatically after death?
- How does the ship AI retain state and present acknowledgments or other feedback, and what existing event mechanisms connect it to gameplay transitions?

## Edge Cases

- What initialization behavior could give a newly created survivor items despite an empty-inventory baseline?
- What happens when the player character disappears during an interaction, a death trigger repeats, or replacement character creation cannot complete?

## Success Criteria

- What existing tests, debug tools, and verification conventions can demonstrate distinct survivor identities, automatic replacement with an empty inventory, continued control and interaction, retained ship modifications, recoverable equipment at the death location, and AI acknowledgment during runtime succession?
