# Run lifecycle — Phase 1 Step 1 — Attempt 1

Status: FAILED / not complete. No feature commit or staging.

## Approved inputs

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Phase 1 normalized SHA-256: `a3f84cae603c41dc451894a155391f99fdb7385d3e2b6a8e0467df612873ba70`.
Execution mode: single step. Scope: S1, S13, S14 support in saved snapshot/migration; no live resume or death wiring in this step.

## Verification

Command from repository root:

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s1 --output /private/tmp/solitude-run-lifecycle-p1-s1-attempt1`

Expected: complete gate passes, plus new migration/lifecycle/codec/Unity-serialization checks.
Actual: 223 pure Domain/Application tests passed, zero failed. Required Unity authoring launch failed to compile. `Tests/Editor/SaveJsonCodecTests.cs:9` reports CS0246: Newtonsoft namespace unavailable to the EditMode test assembly. The new codec tests imported Newtonsoft.Json.Linq. No EditMode/PlayMode verdict, builds, or player scenarios ran. No manual gameplay verification ran. Therefore S1/S13/S14 step qualification remains incomplete.

Proposed repair: remove the new test-only Newtonsoft.Linq dependency using deterministic JSON fixtures/string mutation in the already planned SaveJsonCodecTests.cs; preserve existing assembly boundaries and rerun the complete required gate. No repair has been applied. Await human instruction under interactive QRSPI failure handling.

Artifact-only checks: approved Spec/Plan inputs matched before execution; diff whitespace check passed. All pre-existing dirty files remained byte-identical before and after verification. Existing staged diff remains empty. Baseline source HEAD: `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`.

## Gate result

```json
{
  "status": "Failed",
  "error": "authoring: exit 6; see /private/tmp/solitude-run-lifecycle-p1-s1-attempt1/authoring.runner.log",
  "results": [
    {
      "name": "copy-Assets",
      "seconds": 2.64,
      "exit": 0
    },
    {
      "name": "copy-Packages",
      "seconds": 0.03,
      "exit": 0
    },
    {
      "name": "copy-ProjectSettings",
      "seconds": 0.02,
      "exit": 0
    },
    {
      "name": "prepare-git",
      "seconds": 0.02,
      "exit": 0
    },
    {
      "name": "reset-verification-index",
      "seconds": 0.01,
      "exit": 0
    },
    {
      "name": "copy-verification-index",
      "seconds": 0.02,
      "exit": 0
    },
    {
      "name": "pure-tests",
      "seconds": 3.15,
      "exit": 0
    }
  ]
}
```

Runner/Unity logs and pure test output: `/private/tmp/solitude-run-lifecycle-p1-s1-attempt1/`. Isolated copied source: `/private/tmp/solitude-run-lifecycle-p1-s1/`. Snapshots/baseline hashes are also retained at `/private/tmp/run-lifecycle-p1-s1-baseline.json` and `/private/tmp/run-lifecycle-p1-s1-source.json`.

## Feature source snapshot

These exact working-tree bytes supplied this attempt. The unrelated dirty baseline is part of the copied test workspace and is preserved separately; it is not feature-owned work.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Domain/SaveGameData.cs` | `f8266cf885cf4d2d7cf0d9875d048a80f32c11dfb892ed2a0a35fdfc9fc4bac3` |
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `ab4d18a615158e6318b82af226734cd836bf927b474385c7a506ce42a75aa317` |
| `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs` | `8cafa578a06dab3eb654af8b36c44aecb315e99896f8e6091a4b4a1b8171cb54` |
| `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs` | `dbdada2369846027dde309fe114c126bb468a304f339a7d74fefa9fd4b61640e` |
| `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs` | `24f24b43354a7906389267a9a66a4b7852622af4219cbf10c1caf0b1f75c39e6` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs` | `25777c7c148840eabd4108a8e3e5629550da3d949a0e0b60fe18c28fea94af85` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs.meta` | `863b0010f908fd9d712ad86c2f6b309751707b715b5a7cb94f0f7c21c58606a0` |
| `Assets/_Project/Domain/Expeditions.meta` | `5bb8f688445883c97edb44ed9d83b6c68c09ee8bb9d1bb4f57f105f1713c156b` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs` | `b6d37b430cd962b3130251ab478a5f86e3fd827934dc223df8da90ffe550383d` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs.meta` | `af5288266a648807708eff8b93bf9524e69a65007b9a68310bdaee94fa16e2b5` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs` | `405e72fae823918e5ee01fcefc4f9085c55a0d46b831c180517e5c271c513615` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs.meta` | `5c277efeaa2156af83b12914fe62f0548252230281dcc9d6c929e6c5ffce044f` |
