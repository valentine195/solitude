---
name: solitude-ui
description: Implement or review SOLITUDE Unity UI using the project UI guide, authored prefabs, and actual-scene state and visual checks. Use for HUDs, interaction prompts, notifications, and equipment screens; preserve review-only or design-only scope when requested.
---

# SOLITUDE UI

Turn the current SOLITUDE UI direction into reusable presentation that works in the requested gameplay scene. Keep the task small and preserve existing gameplay systems.

## Establish the task

- Resolve the SOLITUDE repository root from the current project or this skill's location. Read applicable project instructions and the [UI guide](../../../Assets/_Project/UI/README.md). The guide owns visual and interaction rules; do not copy it into a competing design document. Explicit user choices govern the current task.
- Determine the mode: implement, review, or design/handoff. Review does not authorize fixes; design/handoff does not authorize code inspection or implementation when the user excludes them.
- Establish the target scene, owning prefab/view, entry interaction, and requested outcome. Use the user's named demo or scene. Never substitute Wakeup or the currently open scene without evidence that it is the target. If scene choice materially changes the result and remains ambiguous, ask while continuing independent asset inspection.
- Inspect only relevant assets, view/controller bindings, input/modal policy, and scene wiring. Check existing work before edits and preserve unrelated changes. Use project ownership rules for new assets.

## Implement

Prefer an authored prefab hierarchy with serialized labels, sprites, styles, anchors, and layout settings. Reuse suitable existing primitives; use sliced images when the approved design needs a scalable border or panel. Do not add decorative framing merely because a 9-slice asset exists. Runtime code should update content, state, visibility, and target position rather than construct a fixed production screen in Awake.

Keep views focused on presentation and existing index/event-based contracts. Resolve domain objects and actions through the existing controllers/presenters and services. Do not create a parallel interaction bus, input asset, modal policy, or UI framework for a single screen. Dynamic collections may instantiate authored item prefabs where the existing design requires them.

Use the installed Unity UI skill appropriate to the project's existing UI system when available. For uGUI, use Unity's prefab and RectTransform workflows. Prefer the Unity CLI or available Editor API for scene/prefab mutations and live inspection; consult its skill before command execution. A failed or unavailable Editor connection is not a reason to guess serialized asset contents. Use the SOLITUDE art pipeline only when new or modified artwork is actually needed.

Map the affected UI states before implementing. Follow the guide's independent interaction, condition, analysis, action-lifecycle, and visibility dimensions. A focused faulty unit must retain its fault label and gain separate action focus. Apply semantic color to the correct element and pair status with text or symbols. Resolve behavior that blocks implementation from the user, approved feature spec, or current source; do not invent a pause policy or turn provisional guide values into permanent requirements.

Wire the prefab into the requested scene and check scene builders that recreate it. Update an owning builder when necessary so regeneration preserves the change; avoid editing an unrelated scene just to demonstrate the prefab.

## Verify or review

Read [the verification checklist](references/verification.md) and select only checks relevant to the affected behavior. Start with bindings/compilation, then exercise the actual scene entry and transitions. Directly calling Show or setting a label is a useful diagnostic, but does not prove event wiring or the player journey.

Inspect screenshots at normal gameplay scale, including the combined states changed by the task. Compilation and tests cannot establish color, overlap, clipping, or readability. Fix concrete findings within implementation scope and rerun affected checks. In review mode, report findings with asset/source locations and evidence without modifying them.

If a live or manual check is unavailable, continue independent work and report precisely what remains unverified. Do not claim a visual pass from source inspection or require human approval solely because this skill was loaded. When running inside QRSPI, preserve its approved checks, role isolation, and gates; this skill does not waive them.

## Deliver

Report the changed assets and target scene, resulting behavior, relevant verification, and material limitations. Link screenshot/evidence files when useful; keep disposable outputs outside Assets. For review, lead with actionable findings and separate unavailable checks. For design/handoff, provide concrete components, layout, interactions, state rules, and acceptance checks while marking unresolved decisions. Do not claim implementation from a specification alone.
