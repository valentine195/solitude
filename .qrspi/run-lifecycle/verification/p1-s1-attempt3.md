# Run lifecycle — Phase 1 Step 1 — Attempt 3

Status: FAILED / infrastructure crash / not complete. No commit or staging.

## Approved inputs and repair

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Phase 1 normalized SHA-256: `1a9ab19d1248feaa7206a44014068d0aca27763f7c010b1122b86069ba05d0a7`.
The human approved adding PickupPersistenceTests.cs and switching SaveGameData round-trip helpers to the production SaveJsonCodec, preserving the assertions and using CurrentVersion. That exact plan adjustment was recorded/shown before execution. All other normalized plan revisions remain the approved originals. Runtime schema unchanged from attempt 1.

Criteria: S1, S13, S14 snapshot/migration support. Full step qualification remains incomplete.

## Verification

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s1 --output /private/tmp/solitude-run-lifecycle-p1-s1-attempt3`

Expected: complete gate passes.
Actual:
- Pure Domain/Application: 223 passed, 0 failed.
- Authoring validation passed.
- EditMode: 343 passed, 0 failed, 0 skipped/inconclusive. Codec tests: 24 passed; container persistence: 21 passed; pickup persistence: 14 passed.
- Fixtures prepared successfully.
- PlayMode launcher exited 6: Unity process stopped with SIGSEGV. No playmode.xml verdict was produced. The native crash trace includes Sec0EncounterTests.AuthoredDemoBindsPatrolsStunsAndReloadsWithDisposableState at line 26. This test and the NPC implementation are pre-existing unrelated dirty work. Their presence in the stack does not establish the root cause or prove independence from feature changes.
- Builds, shipping checks and process save/recovery scenarios did not run. No manual gameplay validation ran.

## Next decision

Proposed next scope: diagnose/reproduce the native crash using disposable source snapshots, including a comparison with the pre-step runtime source where useful, while leaving unrelated NPC code untouched. Do not skip the encounter test or call the incomplete gate passed. Any required product/plan expansion must return for approval. No diagnostic rerun or NPC fix has been attempted after this gate failure.

## Preservation

All original dirty files remain byte-identical to the pre-step baseline. All tested feature source hashes still match. Index is empty; no feature commit exists. Baseline HEAD: `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`.
Logs, runner result and native trace: `/private/tmp/solitude-run-lifecycle-p1-s1-attempt3/`. Feature manifest: `/private/tmp/run-lifecycle-p1-s1-attempt3-source.json`. Baseline: `/private/tmp/run-lifecycle-p1-s1-baseline.json`.

## Feature source snapshot

| File | SHA-256 |
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
