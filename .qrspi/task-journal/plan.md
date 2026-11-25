## QRSPI Plan: task-journal

## Inputs

- Spec: `.qrspi/task-journal/spec.md` — SHA-256: `b2875cdba9c829988e5d3320633c838862e2de5e7debed4a6d7ba470ca987ff8`
- Research: `.qrspi/task-journal/research.md` — SHA-256: `564cb9591bbfbf900ff92d2081d07c277fe0cbf11775061724b6f50918fa971b`
- UI reference: `.qrspi/task-journal/design/task-journal-pinned-active.png` — SHA-256: `7948d782ab5bb1b155cfb296abcc70ad685ffdca5a3a07a357af6fa59b238cd6`
- Selected approach: not applicable; Shape was skipped before Spec.

Human approval: “Go” accepted this exact Spec and linked mockup; “Approved” accepted the two-phase, nine-step structure below. Full plan content still requires review before execution.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Scrubber journal with pinning | none | A player discovers the scrubber, opens the journal, reads current repair state, pins/unpins, closes safely, and sees progress/completion on return. | 6 | [x] |
| 2 | Notifications and qualification | 1 | Shared task notices replace the demo's legacy completion notice; authoring/lifecycle diagnostics and full regression/visual evidence qualify the finished slice. | 4 | [~] |

Phase 2 needs Phase 1's TaskJournal observation contract, composed scene source/policy bindings, authored journal UI, and working demo. Phase order is not itself a dependency. Phase 1 is a working journal slice; legacy completion presentation remains until Phase 2 replaces it. Phase 1 does not claim final S9 conformance.

## Coverage

| Criteria | Delivering steps | Integration/qualification overlap |
| --- | --- | --- |
| S1–S4 | 1.1, 1.2 | 1.3, 2.3, 2.4 |
| S5, S16 | 1.3 | 1.5, 2.4 |
| S6–S7 | 1.2, 1.4 | 1.5, 2.3, 2.4 |
| S8 | 1.2, 1.5 | 2.4 |
| S9 | 2.1, 2.2 | 2.4 |
| S10–S11 | 1.1, 1.3 | 2.1, 2.3, 2.4 |
| S12 | 1.3–1.5 | 2.2–2.4 |
| S13 | 1.1–1.3 | 2.3, 2.4 |
| S14 | 1.1, 1.2, 1.4, 1.5 | 2.3, 2.4 |
| S15 | 1.1, 1.2 | 2.3, 2.4 |
| S17 | 1.5 | 2.2, 2.4 |

Overlap checks composition of existing behavior; it does not duplicate implementation or mark unrun checks passed.

## Execution and verification rules

Repository root is `/Users/jeremyvalentine/SOLITUDE`; file lists in phase documents are relative to it. New source names are proposed owned files. Include their Unity-generated `.meta` files and populated folder metadata, preserve existing GUIDs and assembly boundaries, and never create speculative empty folders. All new Domain/Application tests use `SOLITUDE.Tests`, existing Test/TestCase conventions and the current pure runner's supported lifecycle attributes.

- Follow qrspi-implement's chosen execution mode and human gates; default mode is one step. One owned commit per verified step is the default. This plan does not authorize pushing or integration.
- With full-plan approval, Phase 2 Step 4 is an explicit evidence-only exception to the default one product commit per step: freeze product source, record the tested existing commit and source fingerprint, and do not create an empty commit or commit ignored workflow evidence. Earlier verified product commits remain intact.
- Check exact upstream provenance and relevant dirty-tree/source snapshots before and after each step. Preserve unrelated work. Save criterion-linked commands, results, source hashes and commit identifiers in uniquely named `.qrspi/task-journal/verification/` reports.
- For pure checks, run the existing `Tools/Verification/PureTests.csproj` through the project's Unity-bundled .NET SDK and cached NUnit path, as resolved from `verify.py`. Keep generated build intermediates outside Assets; source-directory writes need normal permission. Run the whole pure suite, not new implementation-mirroring assertions only. Compile/import new Unity assets and retain console diagnostics.
- For Unity source/asset operations, use the unity-cli skill and supported Editor APIs; uGUI authoring uses solitude-ui plus the applicable Unity UI skill. Runtime views update serialized authored controls and instantiate authored row prefabs; do not build the production hierarchy in Awake. Do not hand-edit scene/prefab YAML.
- A step marked **full gate** runs `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-task-journal-p<phase>-s<step> --output /private/tmp/solitude-task-journal-p<phase>-s<step>-attempt<attempt>` from repository root, without `--skip-build`. A nonzero exit, missing/failed results.json, failed suite/build or player recovery scenario blocks that step. This qualifies Wakeup/macOS ARM64 and is supplemented by the named demo journey/visual checks.
- Stage only the verified owned files and metadata pairs. Check staged pairs/GUIDs and confirm staged source equals the tested candidate; never stage unrelated files or broaden the disposable runner index to hide a failure. Any required metadata check or source mismatch blocks commit. Rerun affected verification only when changes/findings justify it.
- Failed required checks, unavailable actual-scene verification, or a plan/spec mismatch stop execution. Retain evidence and source; no automatic destructive rollback. Recovery below names coherent checkpoints, not authorization to revert commits.

## Risks and boundaries

Highest risks are input/pause cleanup, two discovery paths sharing one occurrence, stale publisher ownership, and duplicate/late completion notices. Tests must preserve the existing unpaused equipment screen and scene-reset/save isolation. Failure/abandonment contracts are exercised through pure/temporary fixtures; no such scrubber story branches are added. Pinning remains journal-only. Wakeup scene, save schema, repair cost/duration, shipping scene list and unrelated UI remain unchanged.

## Plan review

All steps are unstarted. No implementation or tests have run for this plan. Approval covers this overview and both phase files by their normalized SHA-256 revisions. Normalize only status-marker tokens in phase files and Status cells in this overview to `[ ]`; all other bytes remain significant. Request reassessment for substantive plan changes and preserve existing progress markers.

After approval, choose single step, Phase 1, full execution with phase gates, or explicitly authorized Autoloop. Autoloop requires its separate resolved-scope entry gate and stops before final review.

## Authorized 2026-10-08 amendment

Phase 1 Step 6 repairs F1/F2 and removes pinning under the explicit user instruction. S14 is retired and S18 replaces pin requirements. Earlier five completed steps remain historical. Phase 2 pin references are superseded; notifications stay deferred.

## Authorized Phase 2 entry amendment

“commit and continue” on 2026-10-08 approves committing the current workspace checkpoint and proceeding with Phase 2. Phase 1 Step 6 visual qualification moves to Phase 2 Step 4; automated repair qualification is complete. Pinning remains removed and tracked tasks deferred. No visual check is waived.
