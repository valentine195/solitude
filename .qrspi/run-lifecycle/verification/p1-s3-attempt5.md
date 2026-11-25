# Run lifecycle — Phase 1 Step 3 — Attempt 5

Status: PASSED / complete. All three Phase 1 steps complete. Single-step mode; no later phase started and no independent review performed.
Commit: `62b17b02ae1a067bea59d8b623b2854cad6d8675`. Parent: `fe68a638ace5b8dbcd4739e17413f03f9808ad71`.

## Approved revisions

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Normalized Phase 1 SHA-256: `961eefaf4366bc834b4061d3ce7d4d01b8f7c231fc3fbe742c04e7afb07bf089`.
Normalized overview: `0400704b96d53c17a17b380ae5a3b81b0b089b070bdd20478b0c5488072e5217`.
Upstream provenance and all other approved phase revisions rechecked before execution and after verification. The companion approved-plans manifest identifies every revision. Normalize only status-marker tokens and overview Status cells to [ ]; every other byte remains significant.

Human approved Step 3 and subsequent repairs: finally-clean the intentionally incompatible test snapshot; diagnose recovery precision against baseline; add PlayerVitalsSliceTests.cs to the plan and align three assertions with the established domain boundary tolerance; use the accessible Settings.MaxHealth property; pause snapshot-only verification processes from startup. The revised plan digest above was shown before execution. No runtime recovery rule changed, no tests skipped, and no backup assertion removed. Earlier failed attempts and baseline failure diagnosis remain retained.

## Final behavior

SaveGameService requests a composed living-state capture before dirty writes and explicit/pause/quit checkpoints, even when movement/vitals did not dirty containers. Clean LateUpdate does not write every frame. Root captures the authoritative active identity, precise vitals/recovery, scene and body position through the existing avatar adapter. Travel flushes before loading without changing the expedition. Startup validates that the saved scene is streamable and loads it before binding a player; unavailable scenes preserve primary/backup bytes and expose existing failed/recovery state. Production New Save returns to Wakeup and clears lifecycle/ship/expedition state and carried equipment. Disposable-demo behavior remains separate. Existing avatar Capture was reused without another avatar source change.

Verification-only fixtures add a second disposable production scene and exact living snapshot process cases. Snapshot-only inspections acquire an independent startup pause lease, retaining original backup/notice/checkpoint assertions while preventing those read-only fixtures from simulating live hunger before inspection. Shipping exclusion remains enforced by the existing build check.

## Verification

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s3 --output /private/tmp/solitude-run-lifecycle-p1-s3-attempt5` with required Unity/.NET filesystem access.
Expected: complete gate passes. Actual: exit 0; results.json status Passed. macOS ARM64, Unity 6000.6.4f1.

- Pure Domain/Application: 223 passed, 0 failed.
- Authoring validation passed.
- EditMode: 343 passed, 0 failed/skipped/inconclusive.
- PlayMode: 122 passed, 0 failed/skipped/inconclusive; all 11 RunLifecycleResumeTests cases passed.
- Verification and shipping builds passed, including exclusion of test/NUnit/verification assemblies from shipping.
- All 32 player scenarios passed: collection; restart/New Save; 10 save-candidate/recovery cases; four interrupted-write checkpoints and their restarts; four interrupted-recovery checkpoints and their restarts; four new living-colonist process cases.
- New process cases: clean-session orderly quit captures exact changed position/vitals/recovery and equipment; restart preserves the exact living snapshot and expedition with no offline gain; unsafe saved location falls back while preserving vitals/identity/equipment; unavailable saved scene blocks startup and preserves primary/backup bytes.
- Missing/corrupt-primary backup-byte checks and all interrupted-recovery backup preservation assertions passed.
- Whitespace check passed. No manual gameplay journey or independent QRSPI Review ran; those are not claimed by this result.

## Criterion-linked evidence

| Criterion | Concrete checks | Expected | Actual |
| --- | --- | --- | --- |
| S1 | FreshBindingInitializesOneColonistAndRebindingDoesNotAwakenAgain; lifecycle-write/resume | One initial living identity; reopening retains it with authored initialization applied once | Passed |
| S5 | ProductionSceneReloadPreservesIdentityPreciseVitalsTimingPositionAndEquipment; StartupSelectsSavedProductionSceneBeforeBindingAndNewSaveReturnsToCryobay; lifecycle-resume | Scene rebind, alternate-scene startup and process reopen preserve current colonist, expedition, vitals and carried owners | Passed |
| S11 | CleanSessionCheckpointsCaptureLiveMovementVitalsAndRecovery; startup routing/invalid-scene fixtures; lifecycle-write/resume/fallback/unavailable-scene | Explicit/pause/quit checkpoints capture clean-session movement/vitals; exact saved resume without offline gain; valid safe fallback; incompatible scene remains recoverable and preserves bytes | Passed |
| S14 | Complete domain/authoring/EditMode/PlayMode/build/recovery gate; unchanged demo and retained-reader cases | Existing modular inventory/input/interaction/save-recovery behavior remains usable; all required regression checks pass | Passed for Phase 1 scope |
| S16 | StartupSelectsSavedProductionSceneBeforeBindingAndNewSaveReturnsToCryobay; lifecycle-resume; existing New Save/domain reset checks | Travel/reopen retains expedition; explicit New Save clears recorded progress/carried items, replaces identities and returns to cryobay | Passed for Phase 1 scope |

Phase 1 now delivers living-colonist persistence/resume. Persistent repairs/shortcuts, death transitions and expedition refresh remain later approved phases; this is not full-feature completion or final acceptance.

## Source snapshot and preservation

The seven source files in the companion manifest match the tested source, copied Assets workspace files, staged index and committed bytes. The runner executes from the repository root. Commit contains exactly these seven paths. All 371 pre-existing dirty files remain byte-identical and were excluded from staging/commit. Remaining working-tree dirt is exactly that original baseline, and the index is empty. No main-checkout scene/prefab files were edited by Step 3; temporary scenes were generated through Unity Editor APIs only in the isolated workspace.

Pre-Step-3 baseline manifest SHA-256: `1978e38a6f74580e05943c8b4038f10121a842cac36035f776569f7feaa79639`.
Feature source manifest SHA-256: `c4b43a69dc282e051b858359d2bdc9d6d9fa57d469f424c6863afb0905eb3783`.
Results SHA-256: `843dc47420d9588639a159e74c4c468898a608209dd8d5ad5f85947a88f42183`.
Logs/XML/builds/disposable save scenarios: `/private/tmp/solitude-run-lifecycle-p1-s3-attempt5`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/SaveLoad/SaveGameService.cs` | `d376b2eec7c3e8d9de372118afa4d03b9be08a909e6ea11454197f50e732855f` |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `318b8404cb4993ece859a277f3a65d908197cbeb3e3ed0f6b862a68f683dd322` |
| `Assets/_Project/Verification/Player/SolitudeCompositionSmoke.cs` | `a0e41261b2804801ca44dadde1370b967c383911ef3651f7786de5e7491d2b0e` |
| `Assets/_Project/Editor/PhaseFiveFixtureBuilder.cs` | `cc5f161908ff2f656ab86c5ec9a61c7be4f84d48ad692a2263f969684bafb247` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `2032516df089a9539bcaebcfb08e685a4df8bf46e9a9c84d7a50fca160022b74` |
| `Tools/Verification/verify.py` | `128771b1bc386f1024bd74fc0e7250c1924bacc0022f76e429eb123b36cde552` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs` | `d79d4e9d782338909f22649e6963fd3d9164a2343eacd9b4bf9cad2ecb59ea04` |
