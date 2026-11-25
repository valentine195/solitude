## QRSPI Plan: weapon-system

## Inputs

- Project root: `/Users/jeremyvalentine/SOLITUDE`.
- Spec: `.qrspi/weapon-system/spec.md` — SHA-256: `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`.
- Research: `.qrspi/weapon-system/research.md` — SHA-256: `044c7f54d3dccd138a523de9472922db4a9dc50d1df950bc2f6e5395dc18567d`.
- Selected approach: `.qrspi/weapon-system/approach.md` — SHA-256: `366905bdc4e5d90108ba79c0cc0897bb7bd0f9d583b9652b0eb1968d2101f74a`.
- Approval: the human requested “Plan” after reviewing the spec, then approved the four-phase, fourteen-step structure. The human subsequently approved the complete detailed-plan manifest `ceefc308ae279cfd9c52337e65b7ed0e34878e5a150e45b5b0dd4dddee7f359f`.
- User-authorized scope exception after the metadata blocker: “Just ignore the `_Project.zip` archives; those are for giving to chat mode for reviews.” Step 1.1 now includes a narrow validation/Git-ignore policy for this review-export family and its regression checks; no archive deletion, move, untracking or sidecar commit is authorized. This does not change combat scope or criterion IDs.
- All spec input digests were checked against current artifacts before planning. The historical research question identifying SEC-0 is resolved in the settled request; no additional research pass is claimed.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Equipped wrench swing | none | Author and query tags, configure a wrench, select it through existing hotbar ownership, and perform a visible timed swing through real input in a disposable fixture. | 4 | [x] |
| 2 | SEC-0 combat | 1 | Deliver directional, obstructed, once-per-target melee damage through shared recipients, with SEC-0 defeat and restrained feedback. | 4 | [x] |
| 3 | Production starting equipment | 1 | Initialize the configured wrench once per new run, preserve save compatibility, and verify retry/resume/succession behavior. | 3 | [~] |
| 4 | Validation and documentation | 2, 3 | Qualify the authored encounter and production loadout together, measure allocations, document extension points, and run complete regression/build verification. | 3 | [ ] |

- Subsequent user approval of the two reported Step 1.2 blockers authorizes adding the existing ReplacementFuse catalog membership and passing the required room-authoring isolation flag in Tools/Verification/verify.py. No room behavior or test assertions are changed. Subsequent human approval removes the mistaken fuse addition after existing tests established intentional demo isolation; retain that boundary and use direct capability validation plus the existing Wakeup production gate.

- Human-approved Step 1.3 refinement: one shared MeleeSwingView receives the equipped sprite and configurable motion; replace the proposed wrench-specific view name and extend existing action definitions/configuration only for generic execution dispatch.

## Dependency outputs

- Phase 1 needs only current project contracts: containers, item database, input hosting, movement, vitals/stun and scene composition. It finishes with a manually seeded disposable swing fixture; it does not require damage or production auto-grant.
- Phase 2 consumes Phase 1's registry/compiled tag sets, authored wrench action/configuration, per-actor execution boundary, active-window callback, equipment binding and input route.
- Phase 3 consumes Phase 1's catalogued wrench, capability/equipment resolution, input route and scene composition adapter. Its startup and save tests need no incoming enemy damage. It can be implemented independently of Phase 2 despite being listed after it.
- Phase 4 consumes Phase 2's working hit/damage/feedback/defeat path and Phase 3's durable, configured production startup. Those outputs already include Phase 1; no redundant direct dependency on Phase 1 is recorded.

## Execution and verification rules

- This is a roadmap only. No implementation has started and no product verification has run. Run the QRSPI Implement skill only after approval of the overview and every phase file. The helper was unavailable on PATH during discovery; interactive artifact-based execution remains supported. Autoloop is not selected or authorized.
- Each step includes its meaningful tests alongside its source changes and ends at a recoverable, scoped change boundary. Preserve unrelated work. Do not stage, discard, or commit unrelated files. Recovery notes are options for a failed step, not authorization for destructive Git operations.
- Proposed new filenames identify the intended ownership and review surface. Do not create empty speculative directories or add new assemblies. Every new imported asset/script/folder has its proper `.meta`; preserve existing GUIDs and assembly ownership. A material change to step scope, dependencies or behavior requires plan reassessment without resetting completed markers.
- Use actual Unity asset/scene APIs and applicable Unity/art skills during implementation. Placeholder presentation is sufficient. Never let an asset builder overwrite manually painted room layouts. Reuse the existing SEC-0 encounter geometry and shipping scene list.
- Validate Spec and Plan provenance and exact approval before implementation/resume/review. Reports identify criteria, commands, observed outcomes, limitations and source snapshot. Store step reports in `.qrspi/weapon-system/verification/`; retain captures/logs outside Assets. A test that was only read is not a pass.
- Run focused Domain/Application/EditMode/PlayMode checks stated in each step. For source changes affecting Wakeup, inventory, hotbar or save recovery, follow `Tools/Verification/README.md` and run the complete isolated `python3 Tools/Verification/verify.py` gate with dedicated `--workspace`/`--output` paths. Its final `results.json` is required; `--skip-build` is partial evidence. Do not weaken existing assertions to make new tests pass. Update only fixtures/expectations genuinely changed by the specified starting wrench or save format.
- The final gate runs after the final source/fixture changes. Repeat a completed check only after relevant changes, failures or unresolved concerns justify it. Build verification must confirm shipping exclusion of test/verification assemblies.
- New recipient/action types beyond melee appear only as minimal test fixtures where needed to establish reuse. They are not shipped weapons/actions. The generic runner does not own melee-specific phases.

## Criterion coverage

Step locators are phase.step. Overlap is intentional: early steps establish contracts; later steps verify integrated behavior.

| Criteria | Delivering / preserving steps |
| --- | --- |
| S1, S2 | 1.1; S2 also 1.2 configuration validation |
| S3 | 1.1 multi-tag sets; 2.2 untagged recipients |
| S4, S5, S6 | 1.1 |
| S7 | 1.2 configuration/catalog; 1.4 authored fixture |
| S8 | 1.2 reusable definitions; 1.3 runtime isolation/test-only alternate execution; 2.4 integrated reuse |
| S9 | 1.2 selection/revision binding; 1.4 actual HUD/input |
| S10 | 1.4 |
| S11 | 1.3 eligibility/clock policy; 2.4 actual stun/death; 3.3 lifecycle |
| S12, S13, S14 | 1.3 timing/lockout; 2.2 actual hit timing |
| S15 | 1.2 ownership invalidation; 1.3 cleanup; 2.4 combat interruption; 3.3 successor/rebind |
| S16 | 1.3 |
| S17, S18, S19 | 2.2 |
| S20 | 2.1 damage contract; 2.2 geometry-to-delivery integration |
| S21 | 2.1 |
| S22 | 2.1 defeat lifecycle; 2.4 integrated encounter |
| S23 | 2.3; 4.1 actual visual journey |
| S24 | 1.2 invalid configuration; 1.3 missing swing presentation; 2.3 missing hit/audio support |
| S25 | 3.2; 4.1 actual input/scene integration |
| S26 | 3.1 schema/migration; 3.2 atomic grants; 3.3 failure/restart/recovery |
| S27 | 3.3 |
| S28 | 4.1 |
| S29 | 2.4 SEC-0 regressions; 3.3 lifecycle/recovery regressions; 4.3 full gate and room/vitals/container suites |
| S30 | 1.1/1.4 authoring metadata; 4.3 final validation/build inclusion |
| S31 | 1.1 compiled query design; 2.2 bounded physics/deduplication; 4.2 measured allocations |
| S32 | 4.2 documentation; 4.3 evidence inventory; subsequent independent Review |

## Main risks and recovery boundaries

- Equipment changes can leave the selected index unchanged. Step 1.2 validates container handle/generation and selected-slot revision/content changes, including replacement by the same definition. No new item ownership or instance-ID system is added.
- Coarse updates can skip a short active window, and cancellation can reset attack recovery. Step 1.3 proves interval crossing and committed lockout before physics integration; Step 2.2 proves real hit counts.
- Hit geometry must consider collider shapes, not just pivots, and world cover must have actual colliders. Step 2.2 pairs representative geometry tests with conservative bounded-query behavior.
- SEC-0 displacement must share its current movement authority. Step 2.3 does not create a second Rigidbody2D driver. Defeat must also prevent a pending outgoing shot and release pulse objects.
- Save version 4 is irreversible for older executables unless a separate migration/backup decision is made. Use disposable saves for development; retain old-version fixtures. Step 3.1 prepares/tests the schema without activating a production v4 writer; Step 3.2 activates v4 together with grant/flush/readiness integration; Step 3.3 verifies failure checkpoints and forward recovery. This staging boundary avoids an intermediate unfinishable production startup, and introduces no lasting version-selection framework. Never infer “needs wrench” from current inventory emptiness on an already completed save.

## Human gates and final review

Detailed-plan approval covers this overview and `plans/plan-phase-1.md` through `plans/plan-phase-4.md` at their recorded normalized SHA-256 revisions. Normalize only the status token on `Status marker` lines and the overview table's Status cells to `[ ]`; all other content remains significant. The manifest `plan-revisions.md` records those revision identifiers.

Use normal interactive step/phase gates unless the human explicitly selects another authorized mode. After all implementation phases, request a new isolated adversarial Review against the exact spec, normalized plan and source/diff snapshot. The reviewer receives no implementation narrative or claimed tests. Review must inspect tag/behavior separation, runtime state isolation, duplicate damage/health, input ownership, allocations, geometry, lifecycle races, serialization and extension cost. Revalidate the review snapshot before relying on the verdict. Significant findings require scoped repair and re-review; a test pass alone does not complete S32.

- Human-approved Phase 1 checkpoint repair F1/S15: reset action runtime on committed survivor succession, preserve same-actor commitment, add actual-input succession regression, and rerun the full isolated gate. See the Phase 1 review-repair section; no later-phase scope is authorized.

- Human-approved Phase 2 checkpoint F1 repair: preserve hit feedback in the SEC-0 prefab builder and qualify the shared construction/save path through disposable prefab regression tests; rerun full verification and independent QRSPI review before Phase 3. Original steps remain complete.

- Human-approved Phase 3 sequencing correction: move only activation-required baseline fixture updates into Step 3.2 so its complete gate can validate v4/default-wrench behavior; keep new process-restart and failure-injection scenarios in Step 3.3.
