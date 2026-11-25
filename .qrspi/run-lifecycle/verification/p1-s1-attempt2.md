# Run lifecycle — Phase 1 Step 1 — Attempt 2

Status: FAILED / not complete. No commit or staging.

## Approved scope

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Phase 1 normalized SHA-256: `a3f84cae603c41dc451894a155391f99fdb7385d3e2b6a8e0467df612873ba70`.
Criteria: S1, S13, S14 snapshot/migration support. Human approved the attempt-1 test-only repair. Only SaveJsonCodecTests.cs changed between attempts: dependency-free explicit JSON fixtures, retaining invalid/missing-field coverage and adding a valid wire-fixture control test. Runtime schema and assembly references unchanged.

## Verification

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s1 --output /private/tmp/solitude-run-lifecycle-p1-s1-attempt2`

Expected: full gate passes. Actual: pure tests 223 passed, zero failed; authoring validation passed; EditMode 337 passed, 6 failed, zero skipped/inconclusive. PlayMode, builds and player scenarios did not run. S1/S13/S14 step qualification remains incomplete.

## Failures

- `SOLITUDE.Tests.ContainerPersistenceTests.LockerInventoryAndHotbarRoundTripTogether`: System.IO.InvalidDataException : Invalid lifecycle identity.
- `SOLITUDE.Tests.ContainerPersistenceTests.SavingDuringDragRetainsEveryItem`: System.IO.InvalidDataException : Invalid lifecycle identity.
- `SOLITUDE.Tests.ContainerPersistenceTests.UnitySerializationAndOwnerReleaseRetainLifecycleAndShipFacts`: System.IO.InvalidDataException : Invalid lifecycle identity.
- `SOLITUDE.Tests.PickupPersistenceTests.CollectionRoundTripPreservesInventoryAndFact`: System.IO.InvalidDataException : Invalid lifecycle identity.
- `SOLITUDE.Tests.PickupPersistenceTests.VersionOneMigrationPreservesExistingWorldAndContainers`: Expected: 2   But was:  3
- `SOLITUDE.Tests.PickupPersistenceTests.WorldPresentationStaysCollectedAfterSceneUnloadAndReload`: System.IO.InvalidDataException : Invalid lifecycle identity.

Five failures occur in test-only JsonUtility round-trips of SaveGameData. Unity materializes nullable inline lifecycle/transition objects as empty records; the schema correctly rejects their missing identities. Production SaveJsonCodec retains explicit nulls. A bounded first-party runtime C# search for JsonUtility/SaveGameData found production SaveGameData serialization only in SaveJsonCodec; runtime verification uses JsonUtility for unrelated result/expected/container-payload types. One existing PickupPersistenceTests assertion hardcodes version 2.

## Proposed correction requiring approval

Add `Tests/Editor/PickupPersistenceTests.cs` to Phase 1 Step 1 Files for affected regression coverage. Update SaveGameData round-trip helpers in ContainerPersistenceTests and PickupPersistenceTests to use the production SaveJsonCodec; preserve their inventory/pickup/owner assertions. Rename the new UnitySerializationAndOwnerRelease test to reflect production codec serialization. Replace the old version-2 assertion with SaveGameData.CurrentVersion. Keep malformed-v3 rejection strict; do not normalize corrupt identities into fresh state. Rerun the full gate. No correction or plan-content update has been applied.

## Source and preservation

All original dirty files remain byte-identical to the pre-step baseline. Source HEAD remains `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`. No user save accessed. Logs/XML/result: `/private/tmp/solitude-run-lifecycle-p1-s1-attempt2/`. Source snapshot manifest: `/private/tmp/run-lifecycle-p1-s1-attempt2-source.json`.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Domain/SaveGameData.cs` | `f8266cf885cf4d2d7cf0d9875d048a80f32c11dfb892ed2a0a35fdfc9fc4bac3` |
| `Assets/_Project/Domain/ContainerSaveSession.cs` | `ab4d18a615158e6318b82af226734cd836bf927b474385c7a506ce42a75aa317` |
| `Assets/_Project/Core/SaveLoad/SaveJsonCodec.cs` | `8cafa578a06dab3eb654af8b36c44aecb315e99896f8e6091a4b4a1b8171cb54` |
| `Assets/_Project/Tests/Editor/SaveJsonCodecTests.cs` | `c1bf2a09def781c9beb04b434bc8be9f781a1f3170f745c764392bf521a50f3a` |
| `Assets/_Project/Tests/Editor/ContainerPersistenceTests.cs` | `24f24b43354a7906389267a9a66a4b7852622af4219cbf10c1caf0b1f75c39e6` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs` | `25777c7c148840eabd4108a8e3e5629550da3d949a0e0b60fe18c28fea94af85` |
| `Assets/_Project/Domain/Survivors/SurvivorSnapshot.cs.meta` | `863b0010f908fd9d712ad86c2f6b309751707b715b5a7cb94f0f7c21c58606a0` |
| `Assets/_Project/Domain/Expeditions.meta` | `5bb8f688445883c97edb44ed9d83b6c68c09ee8bb9d1bb4f57f105f1713c156b` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs` | `b6d37b430cd962b3130251ab478a5f86e3fd827934dc223df8da90ffe550383d` |
| `Assets/_Project/Domain/Expeditions/ExpeditionState.cs.meta` | `af5288266a648807708eff8b93bf9524e69a65007b9a68310bdaee94fa16e2b5` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs` | `405e72fae823918e5ee01fcefc4f9085c55a0d46b831c180517e5c271c513615` |
| `Assets/_Project/Tests/Domain/RunLifecycleStateTests.cs.meta` | `5c277efeaa2156af83b12914fe62f0548252230281dcc9d6c929e6c5ffce044f` |
