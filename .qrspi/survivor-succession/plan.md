## QRSPI Plan: survivor-succession

## Inputs
- Spec: `.qrspi/survivor-succession/spec.md` — SHA-256: `f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4`
- Research: `.qrspi/survivor-succession/research.md` — SHA-256: `391048ad286c52b5522ad36720addd3bde651bab22556be95afaf90459c313ce`
- Selected approach: `.qrspi/survivor-succession/approach.md` — SHA-256: `2e35ba83cd0b50dbbc4eea1ca0d4e5cf90652b71bf46f329713f7b180dfb898e`

The human approved the exact spec and the three-phase, seven-step structure. This document and every phase file still require complete-plan review. No implementation is authorized by this planning task.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Basic succession | none | Empty-inventory debug death yields a distinct survivor at the cryobay, with reset state and AI acknowledgment. | 2 | [x] |
| 2 | Equipment recovery | 1 | Inventory and hotbar move together into independent death-location recovery ownership; ordinary interaction recovers items. | 3 | [x] |
| 3 | Repeated deaths and failure handling | 2 | Interrupted handoffs and repeated deaths remain safe; qualify the complete journey and existing systems. | 3 | [x] |

## Criterion Coverage

| Criteria | Steps delivering or preserving them |
| --- | --- |
| S1 | 1.1–1.2; 3.2 |
| S2 | 1.1–1.2; 2.3; 3.1 |
| S3 | 1.2; 2.3; 3.2 |
| S4 | 2.1–2.3; 3.1–3.2 |
| S5 | 2.2–2.3; 3.2 |
| S6 | 1.2; 2.3; 3.2 |
| S7 | 1.2; 2.3; 3.1–3.2 |
| S8 | 1.2; 2.3; 3.1–3.2 |
| S9 | 1.1; 2.3; 3.1–3.2 |
| S10 | 1.1–1.2; 2.1–2.3; 3.1 |
| S11 | 1.1–1.2; 2.1–2.3; 3.2 |
| S12 | 1.2; 2.1–2.3; 3.2 |

Overlap is deliberate: pure rules prove ownership and identity; composed scenes prove the same behaviors with actual input, physics, UI and ship endpoints. Only Phase 3 completion claims the whole spec.

## Scope and Implementation Boundaries

Retain the authored avatar and active-player container readers. Add small survivor rules in Domain, a coordinator with narrow ports in Application, and feature-owned Unity adapters under `Features/Player/Succession`. Reuse `SceneFeatureComposition`, existing policy/modal/feedback services and container transactions; add no global lifecycle manager or new assembly. New names below are proposed filenames, not existing APIs.

Recovery owners register directly with the existing command service and remain scene-owned runtime containers. Do not register them as authored lockers, route them through lazy loot, or change the save schema. This intentionally provides no reconstruction of recovery locations after reload/restart. Existing player/locker persistence retains its current contract; durable survivor, recovery and ship persistence is a separate follow-up.

Keep the current inventory/hotbar IDs and reader objects available so restoration and scrubber consumers remain bound. Do not use session Reset, whole-scene Release/reinitialize, or scene reload to implement death. Expose only narrow composition callbacks/readers required by this feature. Ordinary locker opening remains compatible when transfer presentation accepts recovery targets.

## Risks and Recovery Points

- Explicit reset can miss held input, proximity caches, UI leases or vital subscriptions: Phase 1 tests these before equipment is introduced.
- Equipment spans two sources: use one planned multi-container commit and retain a receipt before any failable Unity activation. Do not use sequential remove/grant operations or assume callbacks roll back an existing commit.
- Failed activation after equipment commit must resume forward from conserved recovery ownership; it must not revive the deceased or transfer again.
- Composition, scene builders and tests already have unrelated uncommitted changes. Reinspect before each implementation step; edit narrowly and never regenerate, stage or revert that work wholesale. If changes invalidate source assumptions, stop for reassessment.
- Commit/recovery points described in phase files concern only feature-owned changes. Any later commits must exclude unrelated work. Disable the opt-in composition to isolate an incomplete feature; do not reset persistent ship/player data.

## Verification and Evidence

Every step includes meaningful tests and expected results. Later implementation records criterion-linked outcomes against exact source revisions under `.qrspi/survivor-succession/verification/`; source inspection and this plan are not test passes. Use disposable saves and isolated Unity projects. Final qualification includes the complete `python3 Tools/Verification/verify.py` from the repository root with successful `results.json`, plus additional succession and restoration/scrubber checks. `--skip-build` is partial; the existing gate covers macOS ARM64/Wakeup and does not prove the new journey by itself.

Review imported assets in actual scenes after authoring with Unity tools. Preserve asset GUIDs, keep new imported content and metadata together, and keep reports/screenshots outside Assets. No art generation, UI redesign, scene-transition persistence or full save/load succession is planned.

## Revision Rule

Approval covers this overview and all three phase files. Normalize only status tokens on `Status marker` lines and Status cells in the overview table to `[ ]` before SHA-256 hashing UTF-8 content. Any other change requires reassessment; all current markers are unstarted. No source tests or verification have run.
