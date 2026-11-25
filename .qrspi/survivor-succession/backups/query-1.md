## Requirements Clarity

- How is the player character currently created, placed at an awakening location, and made controllable?
- What character identity and character-owned state exist today, and where are they initialized, reset, or persisted?
- What death or incapacitation behavior currently exists, including debug triggers and its effects on gameplay?
- Which repair outcomes, system activations, door unlocks, and other permanent ship modifications currently survive runtime changes, scene transitions, and save/load?

## Scope Boundaries

- Which gameplay systems retain references to the player character, and how are those references established or refreshed when the character changes?
- Where are character-owned state and persistent ship-owned state currently separated or shared, particularly for the Air Scrubber and powered doors?

## Technical Constraints

- What scene and save/load assumptions depend on a single player identity, character instance, or inventory owner?
- What existing character data or ownership abstractions could support distinct survivors and later traits, skills, injuries, or inactive colonists?

## Integration Points

- How are carried items and equipped items owned, transferred, dropped into the world, and recovered through interaction?
- How is the cryobay awakening location represented and accessed from the scenes in which the player can die?
- How does the ship AI retain state and present acknowledgments or other feedback, and what existing event mechanisms connect it to gameplay transitions?

## Edge Cases

- What happens to carried equipment, dropped items, and their locations when scenes unload or a save is loaded?
- What existing protections or failure behavior apply when a player character disappears during an interaction or when character creation cannot complete?

## Success Criteria

- What existing tests, debug tools, and verification conventions can demonstrate distinct character creation, continued control and interaction, retained ship modifications, and equipment recovery across runtime succession, scene transitions, and save/load?
