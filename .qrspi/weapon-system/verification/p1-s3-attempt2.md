# Phase 1 Step 3 — attempt 2

Outcome: **PASS**. Shared timed melee execution and a generic equipped-sprite swing are implemented. Real Primary input, actual damage and starting grants remain later steps.

## Authorization and exact source

The human approved Step 3 after explicitly confirming a generic Swing receives the equipped wrench sprite and future melee items reuse that same view. Plan refinement uses Systems/Combat/MeleeSwingView rather than the proposed wrench-specific class and extends existing action dispatch/configuration. Spec SHA-256: `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`.
Normalized overview SHA-256: `3de5bf9dc968659266df2a1d2d521cf71ff9344d30bc45c38463665368f61b47`.
Normalized Phase 1 SHA-256: `b499d89078b2a5c615d983a7d061ec728ceacabec20c87045bbfd33316ae3c9d`.
Remaining phases unchanged. All upstream provenance and normalized plan revisions checked on entry and after verification. `p1-s3-attempt2-source.json` records 31 scoped files; working tree, index and isolated gate copy match. No source changed during this final verification run. Prior passing attempt 1 is retained as historical evidence only for its source snapshot.

## Delivered ownership

- Pure MeleeAttackTiming defines Windup/Active/Recovery/Idle and explicitly reports processing of Active, including a coarse step crossing its entirety. Invalid elapsed values cannot mutate it. Configured timing fields use double precision with preserved serialized names; default boundaries are .12/.22/.55.
- Generic ItemActionRunner owns one request/execution and the committed lockout, uses no item IDs, tags or melee phases to select behavior, freezes during gameplay gates and preserves lockout after cancellation. Retiring the actor releases it. Reentrant start/callback/cleanup cannot retain a detached execution.
- Shared action definitions dispatch their own configured execution; MeleeAttackAction constructs generic MeleeAttackExecution with an immutable compiled specification. No wrench class/behavior branch exists.
- ItemActionContext captures actor, item/slot binding, normalized facing, tile scale, actor-owned movement/animation/view ports and the active-evaluation callback. Melee execution rechecks context before evaluation and after reentrant callbacks. IsEvaluatingActive is authoritative while delivering a crossed active interval; completion is exposed only after cleanup, even on coarse advances.
- PlayerItemActionComposition binds existing equipment/policy/vitals/stun services and requests any configured slot through shared dispatch. It does not implement an attack or add inventory ownership. Bind/Release are ready for Step 4 scene/input wiring; no new production input/scene grant was added here.
- MovementBehavior retains last meaningful movement direction, defaults down, and adds scoped speed restrictions. Strongest slowdown wins; existing full movement blocks prevail. Animation receives a scoped facing override while ordinary movement input may change. Cleanup releases only the action's leases.
- One MeleeSwingView receives the equipped HeldSprite, phase progress and reusable Rest/Start/End angles plus held distance. Its actor-authored hand offset accommodates the current player sprite pivot. It reuses its renderer across swings. Empty optional presentation remains safe. No per-weapon animation classes or behavior selection by tags.
- A 16x24 placeholder wrench PNG is imported at 32 PPU, point filtering, no mipmaps/compression, handle pivot. Item Icon/HeldSprite share this industrial dark-metal/orange asset. Existing item, action and configuration identities remain intact.

## Criterion-linked verification

| Criteria | Command / concrete check | Expected | Actual |
| --- | --- | --- | --- |
| S12/S13/S14; S8 generic runner | Unity-bundled dotnet harness via full gate | Boundary/coarse-step/zero-window/invalid-time checks, request spam/cancel lockout, freeze, reentrancy and non-melee test execution pass | **269 passed, 0 failed**; p1-s3-attempt2-pure.log. Includes 7 timing cases and 5 runner cases. |
| S8/S11/S15/S16/S24 | Full isolated PlayMode suite, including PlayerItemActionRuntimeTests | Shared assets produce independent actor state; death/stun/disable in all phases, pause/screens, transition, same-ID replacement, owner loss and release clean up; facing and restrictions compose; null view safe | **138 passed, 0 failed/skipped**, including 9 new runtime cases; p1-s3-attempt2-playmode.xml. Coarse Active callback asserts execution is still live and reentrant unequip cleans up. |
| S7/S8/S24/S30 existing authoring preservation | Full isolated EditMode suite | Existing authoring, configured wrench values, shared immutable configuration and optional assets remain valid | **514 passed, 0 failed/skipped**; p1-s3-attempt2-editmode.xml. |
| S29/S30 regressions | python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output Logs/WeaponSystemP1S3Attempt2 | Complete gate without skip-build, including movement/vitals/consumables/containers/room authoring and production-root lifecycles | **Passed**, 46 recorded stages/scenarios, both native ARM64 builds, shipping assembly exclusion and player restart/interrupted-write/lifecycle-recovery scenarios. Authoritative p1-s3-attempt2-results.json; full outputs under Logs/WeaponSystemP1S3Attempt2. |
| S16/S24 presentation | Live temporary additive-scene render via production MeleeAttackExecution/MeleeSwingView, with actual authored player sprite; inspect PNG using view_image | Compact native-pixel held-sprite motion at windup/active/recovery, correct relative scale, restrained silhouette | Inspected p1-s3-swing-preview-final.png: .06/.17/.385-second poses. Initial external lighting/framing corrected as documented in attempt 1. This is pose inspection, not real-input or full character-animation gameplay evidence. |
| S24/S30 imports/configuration | Live run_script ValidateSwing.Run | Tag/capability/metadata validators pass and sprite import matches project scale | All true; 16x24, 32 PPU, total duration .55. p1-s3-attempt2-validation.json; repeated against staged tracked pairs in p1-s3-attempt2-index-validation.json. |
| S30 scoped preservation | Source/index SHA-256 comparison and staged C# whitespace check | Commit only tested feature files; preserve asset GUIDs, assemblies, input, scenes and shipping entry list | Passed. Existing unrelated SOLITUDE_Start modification preserved and excluded; review archives untouched. No changes to existing metadata, input assets or build settings. |

The broad all-pickup catalog audit retains its previously documented single-catalog limitation; the approved production Wakeup gate and direct capability validation are used. No weakened or skipped product tests. Runtime allocation measurement remains Phase 4. Effects/audio/hit detection/damage are Phase 2, and real-input dry-swing fixture is Step 4. No damage or automatic starting weapon is claimed yet.

Commit is recorded after creation below.

Commit: `d4bb772adfa5605962699194347254f26b681658`. All 31 committed files match the final tested snapshot. Step 3 marked complete after commit. Phase 1 remains in progress; Step 4 has not started.
