# Atmospheric scrubber encounter

I’d work on **one complete ship-repair encounter**, using the atmospheric scrubber as the first example.

It connects exploration, interaction, systems, and UI into something playable:

1. **Discover:** The player encounters a room with an observable atmospheric problem.
2. **Investigate:** Accessing the scrubber reveals its fault, telemetry, and available controls.
3. **Resolve:** The player performs one meaningful repair step—such as replacing a fuse—then restarts it.
4. **Observe:** The equipment, room conditions, and task state visibly respond.

Keep the first version small: **one room, one fault, one repair item, one successful outcome**. Include clear feedback when the player lacks the item or the restart fails.

This would test whether SOLITUDE’s central experience—understanding and restoring a failing ship—is satisfying. It also gives the new UI guide a concrete first application.

I’d defer additional standalone screens until that encounter works. The map and AI overlay will be easier to design once we know what information players actually need during a repair.

The next concrete deliverable should be a short encounter spec defining the fault, player actions, system transitions, and world feedback.

## Clarifications

1. Should the encounter use a blown fuse and replacement fuse, or do you want a different specific fault and repair item?
   - Approved answer: blown fuse and one replacement fuse.
2. Should restarting before the repair be the only restart failure, with a restart guaranteed to succeed after the correct repair?
   - Approved answer: restart fails before repair and succeeds after repair.
3. Should the player find the repair item within the encounter’s room, or arrive with it from elsewhere?
   - Approved answer: the replacement fuse is found within the same room.
4. What atmospheric problem should the player notice, and should it affect gameplay or provide visible and audible feedback only?
   - Approved answer: elevated CO₂, conveyed through telemetry, warning lights, and sound; no damage mechanic initially.
