# Run lifecycle — Phase 1 Step 2 — Attempt 1

Status: FAILED / not complete. No staging or commit. Step 1 remains complete at `3576e16dc12ff7eded770d50b1783d9dd2ce1c6a`.

## Approved inputs

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Normalized Phase 1 SHA-256: `f337f4ca68ddf627908698208185820f2b9f410fd6ecc323ebe63351802932ea`.
Mode: single step. Criteria: S1/S5/S11/S14. Step 2 implements production fresh/resume/arrival binding, validated vital/location capture/restore, and same-scene floor/solid-geometry fallback; demos retain disposable awakening. Process startup scene selection/checkpoint capture remains Step 3.

## Required verification

`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p1-s2 --output /private/tmp/solitude-run-lifecycle-p1-s2-attempt1`

Expected: full gate passes, including added production PlayMode resume tests and existing initializer compatibility.
Actual: pure tests 223 passed/0 failed; authoring validation passed; EditMode 342 passed/1 failed, 0 skipped/inconclusive. PlayMode, builds and player process scenarios did not run. No manual gameplay check ran. No completion claim for S1/S5/S11/S14.

## Failure

- `SOLITUDE.Tests.TaskJournalCompositionTests.SceneReleaseRemovesOnlyItsScopeAndOptionalInitializerIsCompatible`: Expected: <SOLITUDE.Tasks.TaskJournal>   But was:  <System.Boolean>

SceneBindings.Initialize appended optional persistentLifecycle after optional TaskJournal. Existing TaskJournalCompositionTests explicitly verifies that TaskJournal remains the last optional argument. The new bool breaks that contract despite source-compatible defaults for some callers.

## Proposed repair — waiting for human approval

Preserve the original Initialize signature without an overload, and configure the production lifecycle flag through a separate internal composition-root setup method before Initialize. Keep all existing callers/reflection compatibility and retain the task-journal assertion unchanged. Both SceneBindings.cs and GameCompositionRoot.cs are already planned Step 2 files. Rerun the full gate. No repair has been applied.

## Preservation and snapshot

All pre-existing dirty files remained byte-identical to the Step 2 baseline. Tested feature sources still match the captured manifest. Existing completed-step source is unchanged; no unrelated NPC, movement or scene edits. Working-tree changes remain uncommitted. Logs and failed verdict: `/private/tmp/solitude-run-lifecycle-p1-s2-attempt1/`.

| Feature file | SHA-256 |
| --- | --- |
| `Assets/_Project/Core/Composition/GameCompositionRoot.cs` | `880efb7e4d7df3badbdb2e87bb50a89a02931b1649121bdec04b50ec1f95f484` |
| `Assets/_Project/Core/Composition/SceneBindings.cs` | `58f360dba756bdf826b80f9aa0122b8830cf796a88586c15d34c43699aee1558` |
| `Assets/_Project/Core/Composition/SceneFeatureContext.cs` | `c03e9edd6067eeff3da708fc6ada96d82d9ce247b5b0a79116c00fd0e4b864ba` |
| `Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs` | `fb82fcda907e04d88ec1dd3833018c502b3222dd4fe69a57952923d5bbb5cef4` |
| `Assets/_Project/Features/Player/Succession/SurvivorAvatarAdapter.cs` | `a6d50a4db12c9352c0418c6b9b17b4cc33959908d5e62bc09085de787334add1` |
| `Assets/_Project/Features/Player/Vitals/PlayerVitalController.cs` | `65672610bac847a325cc6bfb563068f34617e78203bb3324826a586398f95509` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs` | `b7ce3efc5ddd3344d91393441a52f26a2a0f43371bbf7024973b1fc2220b77b6` |
| `Assets/_Project/Tests/PlayMode/RunLifecycleResumeTests.cs.meta` | `a0972c016bcdec679462df74e30f69ee577d1073348798266d1a7278f7ef4b30` |
| `Assets/_Project/Editor/CompositionValidator.cs` | `4a84c314ee438e30744697e5a2a293d1424a8516c0273a6784f71cc63dbafc97` |
