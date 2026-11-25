## QRSPI Plan: run-lifecycle

## Inputs

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`; SHA-256 hashes exact bytes.

- Spec: `.qrspi/run-lifecycle/spec.md` — SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`
- Research: `.qrspi/run-lifecycle/research.md` — SHA-256: `ff73bede328b807e408c677bb719019b4f32d16b793f62bc521c696fa7a2a9d3`
- Selected approach: `.qrspi/run-lifecycle/approach.md` — SHA-256: `241783ca0605f360192f72e86186087709b11724ffdad45e446346080d1e3541`

The human approved the exact Spec revision above, then the five-phase/14-step structure. This complete plan still awaits approval. No implementation has started.

| Phase | Name | Depends On | Description | Steps | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | Living-colonist resume | none | Save and restore one living colonist at the saved scene/location without refreshing the expedition. | 3 | [x] |
| 2 | Persistent ship progress | 1 | Retain repairs and shortcuts; reconcile critical sources with saved ownership. | 4 | [x] |
| 3 | Death and expedition refresh | 1, 2 | Commit one durable succession, trigger it from lethal health, selectively renew designated supplies. | 3 | [x] |
| 4 | Playable slice | 2, 3 | Bind an interaction-disabled hostile actor, two consumables, and the connected production route. | 3 | [x] |
| 5 | Qualification | 4 | Qualify interruptions, migration, regressions and the complete production journey. | 2 | [x] |

Phase files: `plans/plan-phase-1.md` through `plans/plan-phase-5.md`. Each phase ends with an integrated, testable behavior. Supporting schema work is completed with live binding/resume in phase 1; later phases do not remain isolated data-layer work.

## Execution boundaries

- Run QRSPI from the repository root. Use artifact-only interactive gates while `qrspi-x` is unavailable. No autoloop is authorized.
- Recheck approved Spec provenance and complete-plan revisions before execution. Step status changes do not change approval; other edits do.
- Existing dirty work includes `Features/Creatures/Movement.cs`, `Scenes/Wakeup.unity`, and untracked SEC-0/NPC code, tests, scenes and art. Do not stage, commit, discard or overwrite it. Before an overlapping step, identify a stable execution snapshot and distinguish feature edits from existing content; if that cannot be done, stop for resolution. Untracked existing NPC content remains unrelated work, even when referenced by this plan.
- Every Files list is relative to `Assets/_Project` unless prefixed `Tools/` or `.qrspi/`. `(new)` means an intended feature-owned file, not a file to create during planning. Include Unity metadata for new imported assets. No generic reset registry, second save system or new assembly is planned.
- Scene/prefab authoring uses Unity Editor APIs under the Unity CLI skill at implementation time, preserving existing GUIDs. Use the relevant SOLITUDE art/UI skills only if actual art/UI edits become necessary; the plan reuses existing visuals and screens.
- Keep verification fixtures and saves disposable. Reports, screenshots, builds and logs stay outside Assets; save criterion-linked results under `.qrspi/run-lifecycle/verification/` with source snapshot identifiers. Never run process fault scenarios against the normal player save.
- Each step is a recoverable feature-only change. Recovery notes mean restoring that step's own changes or resuming from recorded evidence; never use broad Git reset/cleanup or restore unrelated files.

## Criterion coverage

| Criterion | Delivery steps | Qualification |
| --- | --- | --- |
| S1 | 1.1, 1.2, 1.3 | 5.2 |
| S2 | 3.1, 3.2, 4.3 | 5.1, 5.2 |
| S3 | 3.1, 3.2 | 5.1 |
| S4 | 4.1 | 5.1, 5.2 |
| S5 | 1.2, 1.3, 3.3, 4.1 | 5.1, 5.2 |
| S6 | 3.3, 4.2 | 5.1, 5.2 |
| S7 | 2.3, 3.3 | 5.1, 5.2 |
| S8 | 2.3, 3.1, 4.3 | 5.1, 5.2 |
| S9 | 2.1 | 5.1, 5.2 |
| S10 | 2.2 | 5.1, 5.2 |
| S11 | 1.2, 1.3 | 5.1, 5.2 |
| S12 | 3.1, 3.2 | 5.1 |
| S13 | 1.1, 2.3 | 5.1 |
| S14 | 1.1–1.3, 2.1–2.3, 3.1–3.3 | 5.2 and subsequent independent review |
| S15 | 4.3 | 5.2 |
| S16 | 1.3, 3.1, 3.3 | 5.1, 5.2 |

Overlap is intentional: delivery checks isolate a behavior; phase 5 verifies the combined saved snapshot and actual production journey. Reading existing tests is not passing evidence.

## Risks and qualification

Highest risks are whole-snapshot save validation/migration, equipment disposal during scene release, and retry after durable death commit. Phase 3 must not expose playable dead state or acknowledge an unsaved successor. Phase 4 must reconcile existing scene/NPC changes before touching overlapping assets.

Use meaningful existing Domain/Application, EditMode and PlayMode checks at each step. For source affecting Wakeup, inventory, hotbar, lockers or save recovery, the step's completion includes the full `python3 Tools/Verification/verify.py` gate from an isolated workspace with disposable saves; it may include that step's additional filters/scenarios. Do not repeat an unchanged full gate without a new change/failure/concern. Feature-specific faults and process restart scenarios remain required even when the existing gate passes. `--skip-build` is partial and cannot qualify source changes.

The final gate is macOS ARM64/Wakeup and requires passing `results.json`, feature-specific S1–S16 evidence, and a manual journey. Final independent QRSPI Review is a separate human-gated stage after implementation, not a claimed result of this plan. Planning has run no gameplay tests/builds.

## Approval revision normalization

For approval hashes, normalize only the token on `- [ ] Status marker` lines and the Status cells in this overview table to `[ ]`. Keep every other byte significant. Approval covers this overview and all five phase files. Actual normalized hashes are presented at the complete-plan gate in the conversation.
