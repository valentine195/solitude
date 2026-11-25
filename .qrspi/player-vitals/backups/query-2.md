## Requirements Clarity

- How are health damage, healing, maximum health, and any existing passive recovery handled today?
- Do health segments exist today, and how do they affect damage, healing, and displayed health?
- What hunger, food consumption, food restoration, or hunger depletion behavior already exists?
- How are health and O2 currently displayed and updated, including their status indicators?

## Scope Boundaries

- Which existing gameplay systems can change player health or consume medical items and food, and where are those interactions controlled?

## Technical Constraints

- How are player vital values and tuning settings configured, initialized, saved, and restored?

## Integration Points

- What existing timing and pause behavior governs passive player-state changes and delays following damage?
- How does the vitals UI obtain changing values and settings, and what existing UI conventions support boundaries, threshold markers, and adjacent status text?

## Edge Cases

- How does existing health handling behave at zero health, full health, exact segment boundaries if present, and when maximum health changes?

## Success Criteria

- What existing tests, playable checks, or validation conventions verify health changes, consumable effects, passive recovery timing, and vitals UI updates?
