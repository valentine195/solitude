# Phase 2 Step 1 — coordinated selected-item vital use

Execution: single-step scope authorized by Start; subsequent Continue arrived during the active step. Step 2 input, Step 3 definitions/hints, and final integration remain pending. No plan step changes.

## Inputs and snapshot

Project root: `/Users/jeremyvalentine/SOLITUDE`. Spec SHA-256 `a2d29013720f27cf817b3e09f35a625519fc7a9cd75eb67f2f4602bd8df22b8f`; normalized overview `41535179a89ccdd3d5146a6e1b07a723b8727dfaa03cfc600f14a29e6346ffd8`; normalized phase 2 `a74eb197ed164001df239fc832653a1d364356f1489503d8d84a8e0a8150039e`. All upstream/provenance and approved revisions checked before implementation and after verification; only progress markers changed. Phase 1 review phase-1-r1 remains evidence for its recorded prior snapshot.

Base HEAD `3aa065991386d0375ff45911f30566297cbe5ea0`. Exact verified source manifest: `p2-s1/source-digests.json` (all changed/new source and metadata). Working tree contained only these scoped changes; existing asset GUIDs, item IDs and assembly boundaries preserved. New GUIDs generated for new source/folder metadata only. No prefab, scene, input, save schema, starter grants, or shipping consumable-definition changes.

## Changes and criterion-linked observations

- S6/S8: focused Food/Medical/None specs and immutable ID-keyed catalog; selected-slot coordinator reads the active slot at attempt time. Domain transaction validates address/quantity/identity before reserving a prevalidated bounded effect. Trusted runtime reservation applies only non-publishing prepared value writes inside the inventory commit. All revision planning is complete first; external observers run after both ownership and vitals are applied. Completion releases the reservation and isolates observer faults. Result distinguishes Ready reservation from Committed use. Command/coordinator/controller mutation guards prevent reentrant use.
- S4/S6: tests establish food 90 -> 100, medical 90 -> 100, and medical 30 -> 55 while hunger is zero, one item consumed, no cross-vital mutation, and delay retained. Food during pending/completed delay permits only the subsequently eligible recovery interval. Runtime probe checks the actual controller/composition adapter and catalog, not only a fake target.
- S7/S11: no selection/empty/unsupported, invalid or nonfinite amounts, full/zero-health/invalid configuration, unavailable/gated/released targets, stale address, mismatched identity and insufficient quantity preserve inventory/effects. Rejected metadata remains invalid instead of being silently fixed. Tests assert unchanged revisions where applicable.
- S8: deliberate inventory/vital/diagnostic observer exceptions preserve one committed use and coherent observer snapshots. Reentrant attempts return Busy, old address reuse is Stale, and removing the last selected item clears selection. Callback observations are captured and asserted outside the isolated callbacks, so swallowed assertion failures cannot fake a pass.
- S16: old definitions default to None; item catalog/stacking/transfer/save identity remain unchanged. New effect data is compiled alongside existing presentations at session startup. No new save fields. Existing catalog/command/selection/save regressions ran in the complete gate. Future food/medical definition authoring and final feature persistence checks remain later phase work.

## Verification

1. Direct pure command: bundled Unity .NET SDK `dotnet run --project Tools/Verification/PureTests.csproj -p:NUnitPath=<existing Unity NUnit DLL>`; exact DLL is `/Users/jeremyvalentine/SOLITUDE/Library/PackageCache/com.unity.ext.nunit@0198eae3b53e/net472/unity-custom/nunit.framework.dll`. Result 183 Passed, 0 Failed. Raw log copied into p2-s1.
2. Complete gate: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-vitals-repair-project-yjjp5q_x --output /private/tmp/solitude-vitals-p2-s1-gate-yytepxwz`. Exit 0, final results.json Passed. 183 pure + 286 EditMode + 103 PlayMode = 572 Passed, zero failures/skips. Both native ARM64 builds passed, metadata/authoring/shipping exclusion validation passed, all normal and interrupted-save/recovery scenarios passed. Deliberate crash exits are qualified by restart checks. Raw command/results/XML/logs/scenario outputs reside in `/private/tmp/solitude-vitals-p2-s1-gate-yytepxwz`. Source-digest manifest rechecked against current bytes after completion.
3. Real runtime-adapter probe: Unity CLI `command eval_file --project-path /Users/jeremyvalentine/SOLITUDE --caller plugin --skill unity-cli --format json /private/tmp/solitude-vitals-p2-s1-runtime-probe.cs 60000 --timeout 70`. A disposable additive scene and transient definitions bind the actual player composition/controller to real container/selection/catalog/policy objects. Ten checks Passed, including observer exceptions/reentrancy and hungry medical use. CLI result confirms success and its finally restores the original scene with unchanged dirty state. Source and raw result copied to p2-s1. First attempt had a 20-second main-thread dispatch timeout and is retained as inconclusive; the subsequent 60-second attempt succeeded. It is not counted as a passing first attempt or as a Unity automated test case.

## Limits and next gate

Implementation evidence, not an independent review verdict. Production F/gamepad Use input, fresh/held input checks, shipped Ration/Medical Kit definitions, hints and full feature scene journey are Steps 2–4. Step 1 calls the system endpoint directly; no claim of player-facing consumable activation yet. HUD rendering/layout unchanged. macOS ARM64/Wakeup qualification only; no broader platform or balance claims.

Next: Step 2, fresh eligible Use input through existing production input and lifecycle policy.

Committed verified snapshot: `7d23e01c6b6c95fc39ddce4f419d9e855ec0e665`; working tree clean. Source and approved artifacts revalidated before commit. Scoped staged whitespace check passed with per-command `core.whitespace=-blank-at-eol` to preserve standard Unity metadata empty-string formatting; no persistent Git config changes.
