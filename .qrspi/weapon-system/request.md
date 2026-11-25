# SOLITUDE — Weapon System & Gameplay Tag Architecture

Implement a modular, data-driven weapon system for SOLITUDE, beginning with a standard maintenance wrench as the player’s first weapon.

The implementation must establish a maintainable foundation for combat, equipment, item capabilities, and gameplay tags without overengineering the current vertical slice. The immediate deliverable is a fully functional maintenance wrench that can attack and defeat the existing introductory enemy.

Core design principle: Use gameplay tags for classification and rule evaluation, composable actions for behavior, and ScriptableObjects for configuration.

## Required scope

- Inspect existing item/inventory definitions, movement/input, enemy health/damage/AI/death, equipment, ScriptableObject patterns, animation, scene initialization and spawning before designing changes. Extend existing architecture; do not duplicate inventory, health, damage or interaction systems.
- Use item definitions, general-purpose gameplay tags, composable item actions, runtime action execution and shared damage handling. Avoid rigid weapon inheritance and a comprehensive ability/effect framework.
- Provide a centralized registry of stable, serialized gameplay-tag definitions, multiple tags per item/entity, explicit exact and hierarchical matching, and required/any/excluded queries. Validate missing, invalid and duplicate definitions. Support editor inspection/assignment and efficient frequent queries without raw-string comparisons scattered through gameplay. Do not use GameObject.tag. Tags classify and evaluate rules; actions select executable behavior.
- Extend item definitions with stable identity, display name, description, icon/visual references, tags, available actions and existing inventory/equipment metadata. Keep definitions configuration-only and mutable execution state per runtime actor.
- Implement only MeleeAttackAction now. Actions are reusable across item identifiers, receive actor/equipped item/direction context, check eligibility, and execute through a small runtime lifecycle service/component. Future actions must not require changes to player movement or the melee implementation. Keep environmental interactions separate from combat and leave a future secondary/contextual action route possible.
- Automatically equip the maintenance wrench on a new run using a configurable starting loadout and existing inventory/equipment infrastructure. Different items must work without player-controller changes. Show equipped information in the existing HUD where practical. Permit future background-specific configurations without implementing backgrounds or bonuses.

## Maintenance wrench

A practical compact industrial maintenance tool repurposed as an improvised weapon. Use an industrial wrench silhouette, dark metal, restrained orange markings and compact swing compatible with the existing 32×48 player sprites and restrained pixel art. Placeholder assets are acceptable; production art must not block implementation.

Assign through its item asset: Item.Weapon, Item.Tool, Item.Improvised, Weapon.Melee, Damage.Blunt, Tool.Mechanical.

Configurable starting values:

| Property | Value |
| --- | --- |
| Base damage | 10 |
| Range | 1.25 tiles, translated using existing project scale |
| Arc | 90° forward |
| Windup | 0.12 seconds |
| Active | 0.10 seconds |
| Recovery | 0.33 seconds |
| Total | 0.55 seconds |
| Knockback | Light |
| Damage tag | Damage.Blunt |
| Durability | Unlimited |
| Ammunition | None |

Generally defeat the introductory security enemy in approximately 3–5 successful hits. Evaluate existing combat configuration before changing enemy balance; do not change it solely to force this target. Values are provisional and should be evaluated in gameplay.

## Execution and damage

- Idle → Windup → Active → Recovery → Idle, using configurable gameplay timing independent of animation authority and consistent across frame rates. Repeated input cannot bypass recovery. Damage occurs only in the active window. Death, interruption and unequip must clean up state. Dead or incapacitated actors cannot attack.
- Use existing input/facing conventions and movement model, with configurable restrictions; prefer modest wrench movement slowdown. The primary-action input requests the equipped primary action; weapon-specific behavior stays outside the player movement controller.
- Use Unity 2D physics and existing layers for directional range/arc checks, multiple targets, once per target per swing, self-exclusion and appropriate blocking geometry. Avoid continuously active damage colliders and excessive allocations. Keep target eligibility separate from geometry; existing untagged damageable entities remain supported.
- Integrate existing health/damage and preserve existing enemy death, animation, cleanup and drops. Keep damage calculation separate from hit detection. Damage context includes base amount, attacker, target, source item/action, impact position, direction and damage tags associated with the event/action configuration. Do not depend on a concrete wrench class or assume every effect shares item damage types. Provide extension points without implementing speculative modifiers/resistances.
- Successful hits apply damage, brief enemy flash, subtle supported knockback, configured impact audio and a small contact effect. Misses complete normally with optional swing audio and no hit feedback or damage. Missing visual/audio assets must be safe. Avoid large shake, exaggerated knockback, excessive particles, new floating numbers, long pauses and flashy combos.
- A future wrench may expose a separate RepairAction and mechanical-tool compatibility queries. Preserve that capability distinction; do not implement repair now.

## Exclusions

No additional playable weapons, ranged combat, ammunition/reload, durability degradation, crafting/upgrades, combos, charged attacks, stamina, blocking/parrying, new enemy types, AI redesign, status framework, full equipment UI, repair gameplay, background combat modifiers, or general-purpose ability/effect framework. Do not refactor unrelated systems.

## Verification and deliverables

Provide focused automated tests for tag assignment, exact/hierarchical queries, required/any/excluded rules, missing/invalid/duplicate definitions, stable serialized references and defined empty-set/query behavior.

Verify attack phases/timing, in/out of range and arc, once per target, multiple targets, no self-hit, blockers, interruption/death/unequip cleanup, missing feedback assets, primary input/start equipment, reusable alternate melee configuration, controller independence from item tags, and runtime isolation between actors sharing definitions. A second configuration used for testing does not expand the playable-weapon scope.

Validate movement, vitals, healing/consumables, enemy behavior and health/death, room generation, inventory and item handling. Create or update a minimal Unity scenario to spawn equipped, approach an existing introductory enemy, miss out of range, hit in range with feedback and defeat it. Provide Unity validation instructions.

Deliver reusable tag registry/queries/tests, item-action integration, melee implementation, wrench and attack assets, starting equipment, shared damage integration/feedback, combat tests, architecture documentation and a final summary of decisions, limitations and recommendations.

## Process

Follow the established QRSPI workflow with human gates. First inspect and document current relevant components, extension points, gaps, conflicts, dependencies and likely changed files. Propose the minimum viable architecture and explain ownership, ScriptableObject versus runtime responsibilities, public APIs and any additional abstraction. Implement the approved architecture incrementally, then validate and perform an independent adversarial architectural/functional review. Investigate unnecessary abstractions, behavior controlled by tags, duplicated damage, mutable definitions, input coupling, allocations, geometry, lifecycle/equipment races, serialization and future maintainability. Resolve significant findings before completion.

The successful outcome is a player who can equip a wrench, swing it, hit a security robot, and defeat it—using systems that will remain useful as SOLITUDE expands into combat, maintenance, survival, and ship restoration.

## Clarifications

Question: Does “introductory security enemy” refer to the current SEC-0 patrol unit or to another explicitly identified source asset?

Answer: Yes, SEC-0 would be an initial enemy
