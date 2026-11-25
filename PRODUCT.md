# SOLITUDE

<!-- impeccable:product-schema 1 -->

## Platform

Native Unity desktop game for PC and Mac. Keyboard and controller are both required input methods.

Design, generate, and review game UI at **1920×1080 (16:9) by default**. Prioritize standard desktop game resolutions. Phone and tablet layouts and verification are out of scope unless explicitly requested. A narrow chat/browser preview is not the design target; inspect the UI at the game resolution rather than redesigning it around that preview.

Impeccable's web/ios/android/adaptive platform categories do not describe this project; do not infer a web target or mobile platform from this record.

## Users

The primary audience is young adult men and women who enjoy science fiction and indie roguelites such as Hades. Hades is an audience reference, not a requirement to copy its interface or mechanics.

## Product Purpose

SOLITUDE is a top-down roguelite aboard a generation ship. Players go on runs to recapture and restore ship areas that have degraded and fallen to hostile enemies over time.

The UI helps players understand their current situation, choose and carry out actions, and interpret ship condition and restoration feedback while keeping exploration readable.

## Operating Context

Players alternate between world exploration and focused interfaces. The existing UI guide describes exploration readouts, contextual interactions, task notifications, inventory, deck maps, ship-system controls, and AI diagnostic overlays. These are documented interface directions, not a claim that every surface is implemented or shipping.

The project uses Unity; its Editor version is recorded in `ProjectSettings/ProjectVersion.txt`. Runtime presentation belongs with the owning feature or shared system. Shared UI primitives belong in `Assets/_Project/UI`.

## Capabilities and Constraints

- Support keyboard and controller throughout gameplay UI. Exact bindings and device-specific navigation details remain to be resolved where not already implemented.
- Preserve the run-based recapture and restoration premise. Run length, progression, failure consequences, and persistent restoration rules are not established by this record.
- Preserve existing gameplay systems, authored prefab ownership, and project assembly boundaries when implementing UI.
- The existing UI guide documents unresolved interface sizing, pause behavior, typography and spacing values, action consequences, and other interaction decisions. Consult current implementation and approved feature requirements before resolving them.

## Brand Commitments

The product name is SOLITUDE. Its UI should be functional, modern, and evocative of a generation ship without skeuomorphism.

`Assets/_Project/UI/README.md` remains the authority for existing visual and interaction direction. This product record does not replace that guide or establish a competing design system.

## Evidence on Hand

- `README.md`: project premise, asset ownership, placement, and safe-move rules.
- `Assets/_Project/UI/README.md`: established UI direction and explicit open decisions.
- `CODEBASE_MAP.md`: navigation to existing runtime systems; confirm behavior in current source.
- Existing UI prefabs include `Assets/_Project/Features/Player/Vitals/Views/PlayerVitalsHud.prefab` and shared container presentation under `Assets/_Project/Systems/Containers/Prefabs`.
- Product audience, roguelite premise, release platforms, input requirements, and non-skeuomorphic UI constraint were confirmed by the project owner during init.

## Product Principles

1. Keep the world readable while giving players enough information to act.
2. Make interaction, navigation, and feedback usable with both keyboard and controller.
3. Communicate ship condition, available actions, and actual outcomes clearly; distinguish AI interpretation from confirmed status.
4. Support the recapture and restoration loop without inventing unapproved progression or action rules.
5. Maintain a functional, modern generation-ship identity through the established UI guide.

## Accessibility & Inclusion

No additional accessibility feature requirements are in scope for now, as confirmed by the project owner. This does not remove existing UI-guide requirements for readable labels and text or symbol equivalents for status colors.

## Open Decisions

- PC operating-system scope beyond the confirmed PC/Mac targets, minimum supported desktop resolution, and additional aspect ratios beyond the default 1920×1080 (16:9) design target.
- Detailed run progression and persistence rules where not defined in approved gameplay requirements.
- Interaction and presentation decisions explicitly marked unresolved in the UI guide.
