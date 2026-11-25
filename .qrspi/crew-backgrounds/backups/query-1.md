## Requirements Clarity

- How are playable colonists selected or assigned when starting a run or entering an authored encounter, and what character identity data follows the controlled colonist into gameplay?
- What supplies starting Health, Oxygen, Hunger, movement speed, carry capacity, and weapon damage, and how are Health segments and the fully fed state represented?
- What existing systems represent crew backgrounds and proficiency ranks, and how do they affect diagnostics and available interactions?

## Integration Points

- How are authored scrubber faults initialized, exposed to the player, and cleared, and how does the scrubber determine whether airflow is available?
- How do breakers, service panels, and corridor lights receive and report their power state, and what relationships between circuits can an authored room represent?
- How do repair interactions validate a replacement part or salvaged hardware, apply the repair, and update inventory?
- How do tool and repair interactions present choices, requirements, and consequences before commitment, and what in-world, visual, and audio feedback can they provide during and after the action?
- How are SEC-0 patrol routes authored and activated, and how are door closures on entry and reopenings on a system-state change handled?

## Edge Cases

- How are reusable items tracked when installed in an interactable, and what happens to the installed item and its effect when an interaction is interrupted, the player leaves and returns, or normal power is restored?
- How do authored encounter state and character configuration reset when replaying the same room with a different colonist?

## Success Criteria

- What focused tests or playtest setups already exercise character initialization, repair choices, and system-driven room state, and how do they establish a repeatable starting state and observe the outcomes?
