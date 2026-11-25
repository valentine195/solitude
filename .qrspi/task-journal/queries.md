## Requirements Clarity

1. What task or objective tracking currently exists, and how are task identity, player-facing text, task-specific stages, progress, completion, failure, and pinned state represented?
2. Where can players currently view task or objective information, and what information, pinning behavior, and active or completed grouping do those views expose?

## Scope Boundaries

3. Which existing gameplay systems create or update tasks or objectives, and what boundaries govern their access to the tracked state?

## Technical Constraints

4. How is task or objective state retained across scene changes, saved games, and the start of a new game?

## Integration Points

5. What mechanisms currently communicate gameplay occurrences to other systems, and what delivery and subscription-lifetime behavior do those mechanisms provide?
6. How are existing player-facing screens opened, closed, and coordinated with gameplay input and other open screens?
7. How is a broken scrubber represented, and what existing detection or player interaction behavior establishes that the player has discovered its fault?
8. How are scrubber fuse replacement, restart, recovery progress, and recovered condition represented, and which observable occurrences distinguish those stages?

## Edge Cases

9. How does existing task or objective tracking handle repeated discovery or progress signals, updates for unknown tasks, updates after completion or failure, and removal of abandoned tasks?

## Success Criteria

10. What existing tests or validation conventions cover task or objective tracking, scrubber restoration stages, gameplay signal delivery, and player-facing screen interaction?
