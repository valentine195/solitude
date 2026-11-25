# Phase 1 repair and selected hungry notification — attempt 1

User authorization: “Got it; fix it and implement the new hungry notification”. Repair scope: F1 from reviews/phase-1.md; S11/S13 plus selected two-line S13/S14 presentation. Phase 2 remains untouched. Existing completed step markers and prior FAIL review retained.

Approved spec SHA-256: bd993d9564af62e92a8ccc723b89b42216216dab1556f4935c31d1a72bda43d5.
Approved normalized plan SHA-256: 5f0e171bbfd5349c8acdf2e2d4311987a7a742288c3421781c041c94e74dd353; phase 1: 7b289187b5c7cdb857582666281d71587ca5e64aa1543b62fd4d07f19e9440cd. All approved upstream/input digests revalidated unchanged before execution and after verification. Normalization only status marker tokens and overview Status cells.

## Changes and criterion evidence

- S11/S13 / F1: keep requested settings distinct from observed retained maxima. Ordinary health/hunger/O2 value events retain rejected requests. Only an actual supported maximum change reconciles that dimension; an unrelated maximum never clears another invalid setting. Explicit Configure restores valid operation. Regression tests cover null, negative, zero, NaN, infinity and float-unrepresentable maxima, all three current-value event sources, awakening, disabled operations, and per-dimension explicit correction. All new cases Passed.
- S13/S14: authored prefab preserves its GUID and existing view binding. Amber HUNGRY with a separately authored muted Recovery paused label underneath; detail clears for all other reasons and observes existing precedence. Nutrition label replaces FOOD; thinner reserve sits under health, retaining continuous normalized values and configured cutoff. Existing health/O2 bars and markers retained. Actual scene inherits updated prefab; live Editor inspection found exactly one status view and a linked RecoveryDetail, clean Wakeup scene. No runtime fixed hierarchy construction.
- S10/S12/S13/S14: presentation lifecycle/threshold/invalid configuration/maximum/ceiling/pause tests and actual Wakeup slice checks Passed, including the two-line subtitle assertion through composed gameplay.

## Verification

Full qualification command: `python3 Tools/Verification/verify.py --workspace PROJECT --output OUTPUT` (exact paths below). No skip-build flags. Result Passed, process exit 0. 162 pure + 265 EditMode + 103 PlayMode = 530 Passed; zero failed/skipped. Both native ARM64 builds passed, shipping exclusion validation passed, all complete save/recovery/action checks passed. Deliberate interruption exits are validated by their restart checks, not treated as normal successful exits.

PROJECT: `/private/tmp/solitude-vitals-repair-project-yjjp5q_x`
OUTPUT: `/private/tmp/solitude-vitals-repair-gate-gj6l23bk`
Raw evidence: command.json, results.json, source-digests.json, editmode.xml, playmode.xml, pure-tests.runner.log, native build logs, and scenario outputs.

Native capture command and raw outputs: `/private/tmp/solitude-vitals-repair-visuals-vkh12ygp` (command.json, exit.json, player.log, captures/result.json). Process exit 0 and ten state captures successful. Capture hook verifies heading and subtitle against actual controller reasons. All ten frames visually inspected: full/delayed/recovering/hungry/boundary at 1920×1080 and 1280×720. Hungry is amber above muted Recovery paused; other states clear the subtitle. No clipping or overlap; world/player visible; nutrition/health/O2 and cutoff/segment markers visible at both sizes. Screenshots copied beside this artifact for durable preview; pixel/digest checks recorded in p1-repair-1/file-inspection.json. Repeated tool previews sometimes mask unchanged portions as black; source PNG pixels remain present, checked directly. No production rendering changes were made for this preview artifact.

## Source snapshot

Base HEAD: `5a61d7d9187f6508d168ae4269e0b2c40392e985`.
Dirty product diff SHA-256: `3beed449546f0b3a1fe8e90a572b6c8f8b4c4af38a8a34637f0be2e161c93748` (`git diff 5a61d7d9187f6508d168ae4269e0b2c40392e985 -- . ':(exclude).qrspi'`). Seven scoped modified files, no untracked product files or unrelated work. Verification source digests still match all seven current source files; source-digests.json retained next to screenshots. Commit recorded after successful qualification.

## Limits and next gate

This is implementation evidence, not an independent review verdict. The existing phase-1 FAIL remains historical for its old snapshot. A fresh checkpoint re-review is pending before Phase 2. Native qualification covers macOS ARM64, Wakeup, and these two resolutions; no new balance or other-platform claims.

Committed repair snapshot: `3aa065991386d0375ff45911f30566297cbe5ea0`. Working tree clean after commit.
