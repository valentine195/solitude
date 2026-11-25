# Phase 1 Step 4 — focused verification attempt 2 (failed)

Spec SHA-256: bd993d9564af62e92a8ccc723b89b42216216dab1556f4935c31d1a72bda43d5
Normalized plan overview: 5f0e171bbfd5349c8acdf2e2d4311987a7a742288c3421781c041c94e74dd353
Normalized Phase 1: 7b289187b5c7cdb857582666281d71587ca5e64aa1543b62fd4d07f19e9440cd
Normalized Phase 2: a74eb197ed164001df239fc832653a1d364356f1489503d8d84a8e0a8150039e
Base commit: 22308b960ac6977ad656f01e306ad981fce36f25

Actual: compilation completed; 27 passed, 1 failed, 0 skipped; CLI exit 8 TESTS_FAILED. Failed check: HungryPauseMaximumChangeAndPrimaryScreenUseActualSceneBindings (S9/S14 actual inventory input path). Fixture used a global Single<ContainerModalView> lookup, but opening inventory activates multiple panel views. Corrected to the concrete SceneBindings.inventoryModal endpoint.

Actual Wakeup damage-delay/recovery-to-ceiling and retained awakening/release tests passed, as did the 25 prior presentation/runtime/readiness tests. These are partial results; the phase is not qualified by this attempt. No commit or completion marker was made from either failed run. The user's ongoing instruction “Keep going” was applied to these narrow test-fixture corrections; behavior expectations were retained and production runtime unchanged.

Expected: all selected tests pass. Rerun evidence is separate. Command argv:

```
["/Users/jeremyvalentine/.unity/bin/unity", "test", "/private/tmp/solitude-vitals-p1-s3-s0l0tyv7/project", "--mode", "PlayMode", "--filter", "SOLITUDE.Tests.PlayerVitalsSliceTests;SOLITUDE.Tests.PlayerVitalsPresentationTests;SOLITUDE.Tests.VitalBarReadinessTests;SOLITUDE.Tests.PlayerVitalsRuntimeTests", "--output", "/private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/playmode.xml", "--timeout", "300", "--json", "--", "-nographics", "-logFile", "/private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/playmode.log", "-solitudeSaveDirectory", "/private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/saves"]
```

Reports: /private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/playmode.xml, /private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/runner.log, /private/tmp/solitude-vitals-p1-s4-focused2-71bs5m92/playmode-redacted.log. Disposable saves and source digest record are in the same attempt directory.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scenes/Wakeup.unity` | `69961c0bd0d984539e7c3ef9184b5aa93b74e4f459f81502721675620fda2455` |
| `Assets/_Project/Editor/PlayerVitalsAuthoring.cs` | `d97b00ddee8dcce169a218c663ea06897bd3226648774fffa325abfbef55fa30` |
| `Assets/_Project/Editor/PlayerVitalsAuthoring.cs.meta` | `0586b3ef596c1b1a0315e532f3e3c40a46b001da08a1d5ddd0041baf4292c932` |
| `Assets/_Project/Tests/Editor/PlayerVitalsAuthoringTests.cs` | `9f9f568b0bb131bc8cd37334fe3ec0ee72ae1d85d89767dd6564e2170d516e94` |
| `Assets/_Project/Tests/Editor/PlayerVitalsAuthoringTests.cs.meta` | `d070b272563f107a717285bff124f2b0ba55b2ffc950388f2de1f2665eefea0f` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs` | `2635712482989e8e729bd58d6ea37d09d8655d39a1ef9c694b326c5ca71cfe95` |
| `Assets/_Project/Tests/PlayMode/PlayerVitalsSliceTests.cs.meta` | `553cf31526a3146dd2d1bfb490bb744052ae98a9a525ab75ef97a903fa62b039` |
