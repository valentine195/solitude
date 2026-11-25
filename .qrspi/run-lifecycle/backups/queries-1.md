## Requirements Clarity

- What currently happens when a colonist’s vitals reach a lethal state, including inventory disposition, player control, and any replacement-character behavior?
- How are a colonist’s initial vitals, starting inventory, and awakening location currently established?
- What currently determines whether defeated enemies and collected supplies return after leaving and reentering a scene?

## Scope Boundaries

- Which existing gameplay entry points initiate character replacement, a new expedition, or a restart, and which systems respond to those transitions?
- Which inventory owners and item containers occur in the cryobay, maintenance area, and atmospheric scrubber, and how do their contents relate to individual colonists and the shared world?

## Technical Constraints

- How are colonists, enemies, containers, repairable objects, and doors identified across scene transitions and saved-state restoration?
- What existing lifecycle or scene-management constraints affect replacing the current colonist and refreshing expedition state without rebuilding unrelated gameplay systems?

## Integration Points

- How are scene transitions performed, and what happens to player vitals, inventory, enemies, and container contents when scenes unload and reload?
- How are completed ship repairs recorded and restored, and how does the atmospheric scrubber’s repair state affect other gameplay systems?
- How are powered doors and unlocked shortcuts recorded and restored, and how do their interaction and power requirements respond to a completed repair?
- What save or persistence mechanisms exist, what state do they retain, and when is that state captured and restored?

## Edge Cases

- How are critical repair components acquired, stored, transferred, consumed, and removed, and which current paths can leave required components unavailable after death?
- What existing item or container distinctions govern renewable supplies, unique items, and permanent discoveries?
- What safeguards currently prevent repeated death handling or conflicting transitions when death occurs during an interaction, repair, or scene change?

## Success Criteria

- What automated tests and manual validation conventions currently cover character death or replacement, inventory initialization, and scene transitions?
- What automated tests and manual validation conventions currently cover defeated enemies, collected loot, persistent repairs, door unlocks, and save restoration?
