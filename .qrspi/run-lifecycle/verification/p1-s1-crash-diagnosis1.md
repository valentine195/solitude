# Phase 1 Step 1 — native PlayMode crash diagnosis 1

Status: diagnosis complete; implementation step still blocked/uncommitted. This is not a full verification pass.

## Authorization and inputs

The human approved diagnosis in disposable copies while leaving unrelated NPC content untouched. Spec: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`; normalized Phase 1: `1a9ab19d1248feaa7206a44014068d0aca27763f7c010b1122b86069ba05d0a7`. S1/S13/S14 full-gate qualification remains incomplete.

Source snapshot: attempt 3 manifest `/private/tmp/run-lifecycle-p1-s1-attempt3-source.json`. Original dirty snapshot: `/private/tmp/run-lifecycle-p1-s1-baseline.json`. All source and unrelated dirty hashes still match after diagnosis. No files in the main project changed during diagnosis.

## Comparison

A copy-on-write clone of the isolated verification project was made at `/private/tmp/solitude-run-lifecycle-p1-s1-baseline`. The six changed tracked source/test files were restored from baseline HEAD `bb25ca0cb4d85d0458e86645c5f16e22d40c2f2c`; the newly added lifecycle classes/tests and metadata were moved outside that copied project's Assets tree. Existing NPC code, authoring, scene and test contents were identical. Comparison manifest: `/private/tmp/run-lifecycle-crash-diagnostic/comparison-inputs.json`.

1. Existing feature gate with `-nographics`: SIGSEGV, no PlayMode verdict (attempt 3).
2. Pre-step runtime plus identical NPC content, targeted Sec0EncounterTests with `-nographics`: reproduced SIGSEGV, exit 6, no verdict.
3. Current feature runtime and unchanged Sec0EncounterTests, graphics enabled (omit `-nographics`): exit 0; 1 passed, 0 failed, 0 skipped. XML: `/private/tmp/run-lifecycle-crash-diagnostic/feature-graphics.xml`.

Commands:

`/Users/jeremyvalentine/.unity/bin/unity test /private/tmp/solitude-run-lifecycle-p1-s1-baseline --mode PlayMode --filter SOLITUDE.Tests.Sec0EncounterTests --output /private/tmp/run-lifecycle-crash-diagnostic/baseline-headless.xml --timeout 300 --json -- -nographics -logFile /private/tmp/run-lifecycle-crash-diagnostic/baseline-headless.log -solitudeSaveDirectory /private/tmp/run-lifecycle-crash-diagnostic/baseline-headless-saves`

`/Users/jeremyvalentine/.unity/bin/unity test /private/tmp/solitude-run-lifecycle-p1-s1 --mode PlayMode --filter SOLITUDE.Tests.Sec0EncounterTests --output /private/tmp/run-lifecycle-crash-diagnostic/feature-graphics.xml --timeout 300 --json -- -logFile /private/tmp/run-lifecycle-crash-diagnostic/feature-graphics.log -solitudeSaveDirectory /private/tmp/run-lifecycle-crash-diagnostic/feature-graphics-saves`

## Finding and limits

The pre-step baseline reproduces the crash, so lifecycle changes are not required. The headless log identifies Null Device and a native `GfxDevice::DrawSharedGeometryJobs` / tilemap render stack. It originates from `Editor/Sec0NpcBuilder.cs:107` SubmitRenderRequest, called by `Tests/PlayMode/Sec0EncounterTests.cs:26` CapturePreview. This test attempts actual screenshot rendering under a Null graphics device. Keeping the test unchanged and enabling graphics passed it. This supports a test-runner/render-mode mismatch; it does not qualify the entire suite or assert that all Unity native rendering defects are understood.

Attempt 3's 223 pure and 343 EditMode tests remain passing evidence for their recorded snapshot. No builds, player restart/recovery cases or complete PlayMode suite have passed in this feature yet.

## Proposed correction — awaiting approval

Add `Tools/Verification/verify.py` to Phase 1 Step 1's scope. Run PlayMode with graphics enabled; retain `-nographics` for nonrendering authoring/EditMode operations. Preserve all tests, screenshot capture and disposable saves, with no NPC source/test changes. Rerun the complete unfiltered verification gate including player builds and recovery scenarios. Present the updated normalized plan revision before resuming. Do not substitute this one-test diagnostic pass for required full qualification.
