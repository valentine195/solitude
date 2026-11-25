## QRSPI Plan: player-vitals

## Inputs

Project root: `/Users/jeremyvalentine/SOLITUDE`. SHA-256 digests are of exact input bytes.

- Spec: `.qrspi/player-vitals/spec.md` — SHA-256: `a2d29013720f27cf817b3e09f35a625519fc7a9cd75eb67f2f4602bd8df22b8f`
- Research: `.qrspi/player-vitals/research.md` — SHA-256: `ae2eb3abdceba9f17fa94d05d4c9635203b8c56cff1d240edd420cb2d11978de`
- Selected approach: `.qrspi/player-vitals/approach.md` — SHA-256: `f0caa809975d292bb10c358ced2328d71e0bac2a9f5891d4bc2e8a462e4eff7d`

The human approved the exact Spec above and subsequently approved two phases/eight steps. This is the detailed plan awaiting its own revision-specific approval; neither earlier approval authorizes implementation of an unseen plan.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Hunger and recovery in Wakeup | none | Deliver bounded hunger, segmented recovery, gameplay/lifecycle integration, and readable HUD markers/status in the production scene. | 4 | [x] |
| 2 | Hotbar consumables and qualification | 1 | Deliver coordinated food/medical use, eligible fresh input, authored definitions and Use hints, then qualify the complete feature. | 4 | [~] |

Phase 2 requires Phase 1's retained authoritative hunger/health operations, prepared effect/state-update boundary, lifecycle binding, and observable recovery presentation. Its dependency is on those concrete outputs, not merely execution order.

## Criterion Coverage

Step notation is phase.step. Coverage overlap is deliberate: early independent checks establish rules; later integration and visual checks establish the production behavior. Partial coverage never marks the whole criterion satisfied.

| Criterion | Primary delivery/checks | Integration/qualification |
| --- | --- | --- |
| S1 | 1.1, 1.2 | 1.4, 2.4 |
| S2 | 1.1 | 1.3, 1.4, 2.4 |
| S3 | 1.1, 1.2 | 1.4, 2.4 |
| S4 | 1.1, 1.2 | 1.4, 2.1, 2.3, 2.4 |
| S5 | 1.1 | 1.2, 1.4, 2.4 |
| S6 | 2.1, 2.2 | 2.3, 2.4 |
| S7 | 2.1, 2.2 | 2.3, 2.4 |
| S8 | 2.1, 2.2 | 2.3, 2.4 |
| S9 | 1.2, 2.2 | 1.4, 2.3, 2.4 |
| S10 | 1.2 | 1.3, 1.4, 2.2, 2.4 |
| S11 | 1.1, 1.2, 2.1 | 1.4, 2.3, 2.4 |
| S12 | 1.2, 1.3 | 1.4, 2.4 |
| S13 | 1.1, 1.2, 1.3 | 1.4, 2.3, 2.4 |
| S14 | 1.3, 1.4, 2.3 | 2.4 |
| S15 | 2.2 | 2.3, 2.4 |
| S16 | 2.1, 2.3 | 2.4 |
| S17 | 1.4 (phase qualification) | 2.4 (complete feature qualification) |

## Execution and Review Boundaries

- Execute small recoverable steps in their listed order. Phase 1 ends with a functional hunger/recovery HUD; Phase 2 adds the actual selected-hotbar consumable journey. No production food distribution or starter grants are added to make a demo.
- Follow repository placement rules; preserve existing GUIDs, assemblies, tests, and unrelated work. New Unity-imported files/folders need their generated `.meta` files. Do not move existing Vital types into Domain or maintain a competing authoritative vital model.
- New paths named in phase files are planned files, not claims that those files already exist. Keep Domain independent of Unity; Application references Domain; runtime adapters depend on both. `Tools/Verification/PureTests.csproj` already recursively includes Domain/Application source and tests.
- Before Unity scene/prefab mutations, use `unity:unity-cli` and the relevant Unity UI skill. Apply `solitude-ui` and `Assets/_Project/UI/README.md`; the approved vitals HUD/hint explicitly governs this feature where the general guide's minimal permanent HUD differs. Use authored labels/layout and a reusable authored marker template. Runtime may populate a configuration-dependent marker collection, rather than construct a fixed screen.
- Run focused checks with each behavior change, then the complete isolated verification gate at each phase boundary. Repeat a passing check only for a relevant change or unresolved concern. The gate alone does not prove new mathematical/UI behavior. No tests have run during planning.
- Evidence under `.qrspi/player-vitals/verification/` records criterion IDs, source revision/file digests, commands, actual outcomes, and limits. Screenshots/logs/builds/disposable saves stay outside Assets. Use isolated verification saves; never test against normal player saves.
- Phase review and final adversarial review are separate human-gated workflow actions. A phase pass covers only its snapshot; subsequent changes need their own checks/review. Publishing/pushing/merging and optional explanatory roles are not authorized by this plan.

## Main Risks and Recovery

The highest risks are interval/boundary precision, single advancement across lifecycle rebinding, and the inventory/effect publication boundary. Existing inventory removal commits its slots before invoking the optional world callback, while Vital notifications can throw after a value changes. A fallible arbitrary callback or raw event-triggering setter cannot be treated as an atomic use implementation. The plan requires a validated prepared effect with a non-failing mutation boundary and separately reported observer faults; rejection is distinct from committed success.

Each step describes a recoverable feature boundary. Preserve exact evidence and unrelated changes. Recovery is a design for narrow rollback/resume, not authorization for destructive Git operations. If required verification cannot run, label it unverified and keep the affected step incomplete; do not turn unavailable checks into a pass.

## Approval Revision Rule

Plan approval covers this overview and both `plans/plan-phase-1.md` and `plans/plan-phase-2.md`. For comparison, normalize only status tokens on `Status marker` lines and Status cells in the phase table to `[ ]`, then SHA-256 the UTF-8 text. Everything else remains significant. The gate supplies the resulting digests; progress-only marker changes do not invalidate approval.

## Out of Scope

All approved Spec exclusions remain: ship upgrades, production/distribution balance, starter grants, balancing playtests, combat/death/revival, O2 effects, generalized item effects, new inventory screens, gamepad hotbar navigation, rebinding UI, new production art, and vital save persistence.
