# Ship System Restoration

## Goal

Design the minimum reusable Ship System Restoration feature needed for SOLITUDE’s first playable vertical slice.

The target early-game loop is:

Discover problem → Diagnose → Acquire requirement → Restore system → World changes → New access

## Target Scenario

The initial implementation should support:

1. Player wakes in a partially powered cryopod bay.
2. Exit door cannot operate because local power is offline.
3. Player inspects a power junction.
4. Junction indicates that a battery/power cell is required.
5. Player obtains the required item from the environment or a container.
6. Player installs it.
7. Local power is restored.
8. Relevant environmental state changes.
9. The exit door becomes usable.
10. An AI terminal or fragment becomes available.

## Existing Systems

The project already has foundations for:

* player movement
* IInteractable interactions
* interaction/event messaging
* item definitions
* player inventory
* containers and item transfer
* doors and door states
* basic UI
* scene-based Unity gameplay

Inspect their real implementations as needed. Prefer extending or composing existing systems rather than replacing them.

## Key Investigation Questions

Determine the minimum architecture necessary to support this slice:

* How should a restorable ship system represent state?
* How should required items be represented and consumed/installed?
* How should doors, lights, terminals, and other scene objects react to restoration?
* Can the existing event infrastructure handle those reactions?
* How can consumers depend on restored state without direct object-to-object coupling?
* What state will eventually need persistence?
* Should inspection/diagnostics be included now or deferred?

Do not design a realistic electrical simulation unless it is actually the simplest implementation.

## Constraints

Favor the smallest sound implementation.

Avoid:

* generalized frameworks for hypothetical future needs
* power-grid simulation
* quest systems
* crafting systems
* broad state-machine frameworks
* ECS conversions
* major rewrites
* abstractions that do not directly simplify this vertical slice

The goal is not SOLITUDE’s final ship-simulation architecture.

The goal is a reusable foundation sufficient for the first several early-game restoration interactions.

## Workflow Instructions

Run the full QRSPI workflow for the feature above, using the workflow’s normal phase isolation, approval gates, and artifact conventions.

Optimize for low token usage:

* investigate only files relevant to the current phase
* avoid broad repository exploration
* prefer references to existing types over reproducing source
* stop research once there is sufficient evidence to make the phase decision
* keep phase outputs concise

## Clarifications

1. Should this feature deliver a wired, playable cryopod-bay scenario, or reusable components with a demonstration scene?

   Answer: Let’s do a set of reusable components with a demonstration scene

2. Which environmental changes should restoration visibly produce, and does the AI endpoint need working content or only become available for interaction?

   Answer: Possibly lighting up a terminal screen. Placeholder working content. I think we don’t need to go too in depth here yet, this is just a feature slice
