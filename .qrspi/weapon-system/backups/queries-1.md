## Requirements Clarity

- How are existing items identified, configured, serialized and presented, and which inventory and equipment metadata or runtime state do they currently carry?
- How do player input, facing, movement restrictions and environmental interactions currently work, and how are unavailable actions handled?
- How do the introductory security enemy’s health, incoming damage, hit response, death, cleanup and drops currently work, and what configured health and combat values determine its expected survival against 10-damage hits?
- What classification, capability or rule-query mechanisms already exist for items and entities, and where does current behavior depend on those mechanisms?

## Scope Boundaries

- Which existing consumers of item definitions, inventory and equipment would be affected by adding tags, action configuration and equipped-item information?
- Where do current damage producers and damage recipients share contracts, and which callers depend on their existing behavior?
- What existing player-controller responsibilities and dependencies constrain routing primary input to an equipped action while retaining separate environmental interactions?

## Technical Constraints

- What project conventions govern ScriptableObject configuration, stable serialized asset identity, validation and editor assignment, and what compatibility requirements apply to existing saved or authored references?
- What Unity 2D physics configuration, collision layers, tile scale and blocking geometry govern directional melee targeting in the existing scenes?
- How are gameplay time, pause and frame-dependent updates handled, and what existing conventions affect configurable attack phases and movement slowdown?
- What existing physics-query and runtime-allocation patterns could support frequent tag queries and melee checks without introducing avoidable allocations?

## Integration Points

- How are new runs initialized, starting items granted, equipment selected and actors spawned or restored, and where can configurable starting equipment participate?
- How are actor death, incapacitation, interruption, equipment replacement, despawning and scene transitions represented and communicated to active runtime behavior?
- What current animation and visual presentation mechanisms can display a compact wrench swing without controlling gameplay timing?
- What existing mechanisms support enemy flash, knockback, impact audio and contact effects, and how do they handle absent assets or unsupported recipients?
- How does the existing HUD obtain and update player item or equipment information?

## Edge Cases

- How do existing damageable actors map their colliders to a damage recipient, including multiple colliders, actor self-colliders and damageable entities without classification tags?
- What existing inventory or equipment operations can remove or replace an item during an action, and what guarantees do those operations provide about item ownership and actor lifetime?
- Which current systems share item configuration across actors, and how do they separate mutable per-actor execution state from shared definitions?

## Success Criteria

- What existing automated test infrastructure and conventions support asset validation, serialized-reference checks, 2D physics, timed runtime behavior and independent actors sharing configuration?
- What existing tests or validation scenarios cover movement, vitals, healing and consumables, enemy behavior and death, room generation, inventory and item handling?
- Which existing Unity scenario and validation tools can exercise starting equipment, approaching the introductory enemy, missing, hitting with feedback and defeating it?
