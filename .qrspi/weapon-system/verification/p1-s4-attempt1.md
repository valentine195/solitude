# Phase 1 Step 4 — attempt 1

Outcome: BLOCKED on verification failures. No files staged or committed.

Human approved Step 4 after commit d4bb772adfa5605962699194347254f26b681658. Spec SHA-256 `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`; normalized overview `3de5bf9dc968659266df2a1d2d521cf71ff9344d30bc45c38463665368f61b47`; normalized Phase 1 `b499d89078b2a5c615d983a7d061ec728ceacabec20c87045bbfd33316ae3c9d`. All plan revisions and upstream provenance verified on entry. Twelve scoped files match p1-s4-attempt1-source.json; rechecked after failure. Existing unrelated SOLITUDE_Start scene modification preserved and excluded; review archives untouched.

## Implemented draft, not accepted as complete

Authored Primary Button with mouse left and gamepad right trigger through Unity InputAction APIs and regenerated wrapper with installed InputActionCodeGenerator.GenerateWrapperCode. Initial external script used a nonexistent GenerateCode API; compiler feedback was corrected before execution. Existing nonempty map/action/binding identities and every pre-existing binding property/control scheme are preserved. Unity assigned IDs to 38 previously empty UI binding IDs during authoring; no existing stable IDs changed. Input .meta GUID preserved.

UnityInputHost has a scene-owned Primary lease, held-control release tracking, reset/disable/release cleanup, and a queued LateUpdate dispatch intended to wait for same-frame UI. Mouse dispatch checks actual EventSystem pointer-over-UI. Game root compiles capabilities and passes them to scene ownership; SceneBindings binds actor action execution and releases its input lease/execution on scene release. Existing hotbar icon/highlight reused. No new HUD, production auto-grant, saved equipment field or combat damage added.

New isolated fixture/tests use actual Wakeup root, player, input, containers and HUD, manually grant wrench through existing commands, and require -solitudeSaveDirectory before loading gameplay. Those PlayMode journeys have not run yet because the gate stopped in EditMode.

## Criterion-linked verification

Full command: python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-weapon-system-p1-s2-attempt2 --output Logs/WeaponSystemP1S4Attempt1.
Expected S7/S9/S10/S11/S15/S16/S24/S30 plus full regression pass. Actual: pure harness passed (269), authoring passed, EditMode **514 passed / 2 failed / 0 skipped**, exit 8. No PlayMode/build/player verdict. Authoritative p1-s4-attempt1-results.json and p1-s4-attempt1-editmode.xml.

Failures:
1. ItemActionInputTests.RealPrimaryControlsRespectFreshnessPolicyOwnershipAndUIConsumption, line 66: first synthetic mouse press stays at callback count 0 after a yield instead of 1. Deferred frame handling/actual player-loop scheduling needs tracing; the same-frame equality guard may discard a press received after LateUpdate. This is a diagnosis hypothesis, not a verified cause. Preserve UI arbitration and held-input rules while repairing delivery.
2. Existing TaskJournalCompositionTests.SceneReleaseRemovesOnlyItsScopeAndOptionalInitializerIsCompatible, line 29: requires the last optional SceneBindings.Initialize parameter to remain TaskJournal. Appending capabilities violated this established contract. Proposed repair: configure capabilities separately before Initialize, preserving its old signature and positional callers rather than weakening the test.

Source compiler feedback during drafting corrected a Hotbar namespace/type ambiguity in the fixture before running the gate. No test assertion was removed or suppressed. C# compile succeeded before validation. The metadata/binding identity test passed, but the new behavioral input test failed; no passing primary-delivery claim is made.

Step 4 and Phase 1 marked blocked per qrspi-implement Error Handling. Awaiting human authorization to repair the two implementation issues within this step. Phase review and Phase 2 have not started.
