# Run lifecycle — Phase 1 Step 1 — Attempt 4

Status: COMPLETE for this step. Full required gate passed; feature-only commit `3576e16dc12ff7eded770d50b1783d9dd2ce1c6a`.

## Approved scope and provenance

- Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
- Phase 1 normalized SHA-256: `f337f4ca68ddf627908698208185820f2b9f410fd6ecc323ebe63351802932ea`.
- Overview and phases 2–5 remain at their approved revisions.
- The human approved the test-only dependency repair, pickup regression scope/production-codec correction, then native-crash diagnosis and the PlayMode graphics runner correction. The corresponding plan updates and normalized revisions were shown before execution. No NPC code/test changes or test exclusions.
- Mode: single step. Steps 2 and 3 have not started.

## Delivered behavior and criterion-linked results

S1 support: version-3 lifecycle/expedition state can be initialized once with authored inputs; capture/decode does not generate another identity. Current live scene initialization/resume is unchanged until subsequent steps. New Save clears saved lifecycle, ship, expedition and collection facts.

S13 support: v1/v2 known inventory/locker/seed/collection facts migrate to v3, with missing lifecycle left uninitialized. Strict codec/domain checks reject missing required fields, invalid identities/numbers and contradictory pending-death/ship facts. Legacy objective-component reconciliation remains phase 2 work; this is not a claim that all of S13 is delivered.

S14 support: deep cloning isolates caller/captured snapshots from session state, rejected updates do not replace or dirty valid state, and owner-release/collection/capacity behavior remains intact. Existing persistence tests now use the same codec as production instead of Unity inline-null serialization. No assembly boundary changes. The required regression/build/player gate passed.

Concrete new checks include RunLifecycleStateTests; SaveJsonCodecTests explicit malformed/legacy/wire fixtures; ProductionSerializationAndOwnerReleaseRetainLifecycleAndShipFacts; and existing container/pickup regression assertions. No gameplay death wiring, saved-location runtime restoration or manual acceptance journey is claimed at this step.

## Verification command, expected and actual result

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s1 --output /private/tmp/solitude-run-lifecycle-p1-s1-attempt4`

Expected: complete gate reports Passed on the current source, with valid lifecycle/migration round trips and unchanged existing behavior.
Actual: `results.json` status `Passed`; exit 0. Pure Domain/Application tests: **223 passed, 0 failed**. EditMode: **343 passed, 0 failed, 0 skipped**. PlayMode: **111 passed, 0 failed, 0 skipped**. Authoring passed; verification and shipping native ARM64 builds passed; collection/restart/New Save, corrupt/missing/incompatible saves, capacity/read denial, interrupted writes and interrupted backup recovery all passed. Shipping verification/test assembly exclusions are included in the existing build checks. Qualified platform: macOS ARM64/Wakeup. No manual gameplay test ran.

Full runner stages:

| Stage | Exit | Passed tests where reported |
| --- | --- | --- |
| copy-Assets | 0 | — |
| copy-Packages | 0 | — |
| copy-ProjectSettings | 0 | — |
| prepare-git | 0 | — |
| reset-verification-index | 0 | — |
| copy-verification-index | 0 | — |
| pure-tests | 0 | — |
| authoring | 0 | — |
| editmode | 0 | 343 |
| prepare-fixtures | 0 | — |
| playmode | 0 | 111 |
| restore-scope | 0 | — |
| verification-build | 0 | — |
| shipping-build | 0 | — |
| world-collect | 0 | — |
| world-restart-newsave | 0 | — |
| partial-temp | 0 | — |
| missing-primary | 0 | — |
| corrupt-primary | 0 | — |
| both-corrupt | 0 | — |
| temp-only | 0 | — |
| incompatible-primary | 0 | — |
| unknown-item | 0 | — |
| capacity-primary | 0 | — |
| capacity-backup | 0 | — |
| read-denied | 0 | — |
| write-before-temp | -9 | — |
| restart-write-before-temp | 0 | — |
| write-after-temp | -9 | — |
| restart-write-after-temp | 0 | — |
| write-before-commit | -9 | — |
| restart-write-before-commit | 0 | — |
| write-after-commit | -9 | — |
| restart-write-after-commit | 0 | — |
| recovery-after-receipt | -9 | — |
| restart-recovery-after-receipt | 0 | — |
| recovery-after-temp | -9 | — |
| restart-recovery-after-temp | 0 | — |
| recovery-before-commit | -9 | — |
| restart-recovery-before-commit | 0 | — |
| recovery-after-commit | -9 | — |
| restart-recovery-after-commit | 0 | — |

Reports/XML/logs/builds and disposable save scenarios remain outside Assets at `/private/tmp/solitude-run-lifecycle-p1-s1-attempt4/`. Gate verdict and exact source manifest are also preserved alongside this report as `p1-s1-attempt4-results.json` and `p1-s1-attempt4-source.json`. Previous attempts and crash diagnosis remain unmodified historical evidence.

## Source snapshot and preservation

Source parent HEAD: `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`. The commit contains exactly the 14 feature files below. Their committed bytes match the tested working-tree manifest; copied C# inputs were also compared with the isolated test workspace. Every pre-existing dirty file remains byte-identical to the recorded baseline, and the index is empty after commit. Unrelated NPC, movement, scene and art work was never staged or committed. Existing dirty files were included as unchanged environment inputs to the full gate, so this is not a clean-HEAD-only qualification.

Diff whitespace checks passed. Approved Spec input hashes and normalized Plan revisions were checked before execution and again after verification, before staging. No required check was skipped; no known failed result remains for the tested snapshot. This is implementation evidence, not independent QRSPI Review.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Domain/SaveGameData.cs` | `f8266cf885cf4d2d7cf0d9875d048a80f32c11dfb892ed2a0a35fdfc9fc4bac3` |
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `ab4d18a615158e6318b82af226734cd836bf927b474385c7a506ce42a75aa317` |
| `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs` | `8cafa578a06dab3eb654af8b36c44aecb315e99896f8e6091a4b4a1b8171cb54` |
| `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs` | `c1bf2a09def781c9beb04b434bc8be9f781a1f3170f745c764392bf521a50f3a` |
| `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs` | `ab876387200f049e0cab4047b7952b02258c6a917aaf7a7d754fe973bff0fa37` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs` | `25777c7c148840eabd4108a8e3e5629550da3d949a0e0b60fe18c28fea94af85` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs.meta` | `863b0010f908fd9d712ad86c2f6b309751707b715b5a7cb94f0f7c21c58606a0` |
| `Assets/_Project/Domain/Expeditions.meta` | `5bb8f688445883c97edb44ed9d83b6c68c09ee8bb9d1bb4f57f105f1713c156b` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs` | `b6d37b430cd962b3130251ab478a5f86e3fd827934dc223df8da90ffe550383d` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs.meta` | `af5288266a648807708eff8b93bf9524e69a65007b9a68310bdaee94fa16e2b5` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs` | `405e72fae823918e5ee01fcefc4f9085c55a0d46b831c180517e5c271c513615` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs.meta` | `5c277efeaa2156af83b12914fe62f0548252230281dcc9d6c929e6c5ffce044f` |
| `Assets/_Project/Tests/Editor/PickupPersistenceTests.cs` | `f8654ae1ecf647bd8318cde8ed3b96d9e1687f320fe3a50444d78240a16974b3` |
| `Tools/Verification/verify.py` | `cb7ad022d23930caeeb721725c55c32f07f17cca9d2f5b06ccf15f38f281ac60` |
