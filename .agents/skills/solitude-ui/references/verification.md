# UI verification

Use this checklist for implementation or review. Select checks based on the task; do not expand a small prompt fix into a complete HUD audit. The current UI guide and approved feature criteria determine expected behavior.

## Authoring and ownership

- Confirm the requested scene and actual prefab instance, including overrides and serialized label/controller references. Confirm any generating builder produces the same intended presentation.
- Check the relevant sprite import/slicing, font glyph coverage, layout sizing, anchors, canvas scaling, and raycast settings. Preserve existing asset GUIDs.
- Verify compilation and relevant console errors. When reporting a pre-existing error, distinguish it from errors caused by the change.

## Reachable state checks

For each affected state, record the trigger, expected visible result, and observed result. Cover combinations when they alter presentation:

| Situation | Inspect |
| --- | --- |
| Focused faulty equipment | Fault text/symbol stays visible; action focus is distinct and uses the guide's semantic styling. |
| Unavailable action | Cannot activate; reason is readable. |
| Pending action, when supported | Progress reflects actual state; repeat activation cannot duplicate the operation. |
| Success or failure | Presentation follows the real action result; world label, diagram, details, and feedback agree where present. |
| Hidden or closed screen | UI no longer receives input; close does not activate a world action. |
| AI annotations, when present | Distinct from confirmed status; close or state changes remove stale annotations. |

Do not introduce absent states or mechanics just to fill the table.

## Actual scene journey

- Enter through the player-facing interaction and verify the existing event/controller path, rather than only invoking view methods.
- For prompts: approach, focus, change target, and leave range; check target positioning and hiding when a primary interface opens.
- For notifications: cause the actual feedback event; check the intended lane, timing, queue behavior if present, and lack of focus theft. A card shown by a direct call does not prove scene subscription.
- For screens: open, navigate and activate using supported inputs, close, and reopen. Verify focus retention, movement gating, and recovery/pause behavior against the approved feature contract.
- Check disable/re-enable or scene reload when subscriptions, state refresh, or cleanup changed. Add focused lifecycle tests only when they substantiate a material change.

## Gameplay-scale visual inspection

Capture the requested scene with affected states visible. Inspect text and glyph readability, semantic colors, status text, clipping, overlapping labels, panel placement, and preservation of the world view. Check supported viewport/scaling settings relevant to the change, especially world-to-screen prompts near edges. Do not assume the 32-pixel world tile grid defines every UI measurement.

A source color value does not prove the final rendered color: account for tint, disabled styling, material, and focus transitions. A screenshot of another scene does not verify this scene.

## Evidence and unavailable checks

Use compact evidence: check, trigger/command, actual outcome, scene, and screenshot or source location where useful. Label results passed, failed, or unverified; keep a user-approved waiver distinct from a pass. Do not infer audible correctness from an AudioSource state or require the user to be near the computer unless that specific sensory check is necessary. Compilation, state tests, visual inspection, and sound inspection establish different facts.
