## Requirements Clarity

- How are health damage, healing, maximum health, and any existing passive recovery handled today?
- Do health segments exist today, and how do they affect damage, healing, and displayed health?
- What hunger depletion, food consumption, and hunger restoration behavior already exists?
- How are health and O2 currently displayed and updated, including their status indicators?

## Scope Boundaries

- Which gameplay systems modify player health or hunger, and which existing consumers depend on those values?

## Technical Constraints

- How are player vital values and tuning settings configured, initialized, saved, and restored?

## Integration Points

- How is scaled gameplay elapsed time supplied, how is gameplay eligibility determined during menus, pause, transitions, and other inactive states, and how do existing passive changes and post-damage delays use them?
- How does selecting a hotbar item connect to input actions, item-use eligibility, consumable effects, and removal of consumed items?
- How does the vitals UI obtain changing values and settings, and what existing UI conventions support segment boundaries, threshold markers, and adjacent status text?

## Edge Cases

- How does existing health handling behave at zero health, full health, exact segment boundaries if present, and when maximum health changes?
- What happens when food or medical items are used at full hunger or health, and how are restoration amounts clamped and item consumption handled?

## Success Criteria

- What existing tests, playable checks, or validation conventions verify health changes, hotbar consumable use, hunger changes, gameplay-gated timing, and vitals UI updates?
