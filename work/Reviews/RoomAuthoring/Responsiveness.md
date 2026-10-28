# Phase4 Step2 automated/API measurement checkpoint — native qualification deferred

Spec5 SHA256 `b9a81579cbe4931cf6b6e03c74610e867d7a754acd28fd09cbd8f158bc15e4c1`; normalized Plan27 bundle `9d866594dd8fcd96124658010d3f64869de6c27b759a0b737cd947a50e5b6782`. HEAD `1c7f4247bbd16b1fe0056fb655744e17d8c6ac6b` plus the four owned measurement test files, exact source/config hashes in phase-4-step-2-resume-evidence-attempt-2.json. Automated/API checkpoint is committed separately; Step4.2 remains incomplete.

Actual isolated Unity EditMode attempt4 passed 2/2 focused tests. Apple M1 Pro, 8 processors, 16GB RAM, Unity6000.6.4f1, macOS27.0.1. One warmup, five equivalent rebuilds; three gestures, seeds, and style roundtrips. Times are Stopwatch wall-clock milliseconds. No speed threshold was invented. Medbay/Cryobay temporary-art results are IncompletePreview; larger fixture is Complete. Measurements establish response/resource behavior without claiming full style completeness.

| Fixture | Cells | Median rebuild / gesture / seed / style roundtrip ms | Active textures/chunks | Active RGBA MiB | Store revisions warmup → after changes |
|---|---:|---|---:|---:|---:|
| Medbay176 | 176 | 281.5 / 295.1 / 468.6 / 625.1 | 9 | 2.25 | 6 → 10 |
| Cryobay83 | 83 | 96.6 / 100.1 / 243.9 / 208.7 | 4 | 1.00 | 1 → 5 |
| Larger32x32 | 1024 | 476.2 / 434.1 / 767.3 / 990.3 | 16 | 4.00 | 1 → 5 |

S5/S12/S14/S21/S41/S42/S47/S64: three multi-sample gestures per fixture applied once each; equivalent repeats added zero revisions. Every distribution, seed/settings, object/renderer count and native-pixel allocation estimate is in the raw measurement JSON. Estimated RGBA is active texture payload only, excluding overhead, meshes, retained historical textures and Unity memory. Retention counts are immutable store revisions, not evidence of active texture leakage. Medbay's default store includes pre-existing saved resources; synthetic fixture stores start separately.

S59: five seeded immutable resource revisions survive repeat rebuilds, Undo/Redo, saved-scene close/reopen; saved reference cleanup refused; cleanup without explicit end-Undo refused; one verified unreferenced old revision removed only after explicit end-Undo, leaving four with saved hash unchanged and no missing sprites. Actual test passed. Resource test uses only disposable scene/assets and clears its own batch Undo history, never the live original Undo history.

S49/S60/S64: current broader asset-import/loop and local resource-limit checks remain phase4 step1 evidence at unchanged product source. Step2 focused tests do not independently replace those checks. Existing RA-TD-001 multi-Inspector limitation remains accepted/deferred with no blanket S49 waiver.

Native API measurements passed the same assertions in an ephemeral script with no domain reload. Median rebuild/gesture/seed/style-roundtrip ms: Medbay2255.1/2344.3/2922.8/4962.3; Cryobay670.4/834.0/1072.6/1452.1; larger2683.3/2804.0/3124.8/5813.4. Native results are materially slower than batch; no acceptance is inferred. Original Medbay authored data/output/cells remained exact and scene clean; Undo label restored after new groups reverted, group number26→267. Native immutable Medbay store revisions10→14; synthetic stores1→5 each. Full distributions/settings/hardware in phase-4-step-2-native-api-attempt-2.json, current source/config attribution in phase-4-step-2-native-source-attempt-2.json. Native visual response observations: NotRun because CUA returned locked Mac; unlock is required. Explicit human acceptance of any unresolved measured responsiveness/retention limits remains pending. Step3 full gate, admitted prepared-resource shipping-player proof and independent final review have not been run by this step.

Preserved failures: attempt1 compile qualification, attempt2 multiple-layout selector, attempt3 saved-resource store mismatch. Corrected only new test harnesses under standing bounded-fix authorization; production behavior/source untouched. Native API computation must preserve saved Medbay and original Undo; it cannot substitute for visual observation.

## Headless continuation decision

Direct human instruction: “Do what you can without the Mac. Defer these checks until later”. This records execution order only; no criterion, measured performance or retention policy is accepted or waived. Existing successful executions are reused only after confirming the exact four harness hashes against their recorded source snapshot and parsing the real NUnit XML (2 passed, 0 failed). No new measurements or native observations are claimed.

Return checklist: observe actual native gesture/regeneration responsiveness on all three fixtures; inspect retained immutable resources and explicit cleanup boundaries; obtain explicit acceptance of unresolved measured latency/retention limits. Step4.2 remains blocked/deferred. Independent Step4.3 headless checks proceed under the direct instruction; final independent review and human integration decision remain outstanding.
