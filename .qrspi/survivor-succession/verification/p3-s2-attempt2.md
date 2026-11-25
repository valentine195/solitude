# Phase 3 Step 2 attempt 2 — automated gate passed; manual HUD check failed

Source: p3-s2-attempt2.source.json, base ae579b10fbb43f7f5e6eadf8d614b6e785bc3b8a plus ten scoped files. Spec SHA-256 f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4; normalized Phase 3 SHA-256 2f72871d1479b7469180bd384385583292c0cc873435acbbe36b2777453655db. Approved inputs and all source/gate files rechecked unchanged. No commit; step blocked on mandatory manual check.

## Automated qualification

Command: DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-p3-gate --output Logs/SurvivorSuccession/p3-s2-attempt2

Actual exit 0/results.json Passed. 121 pure, 221 EditMode, 69 PlayMode, zero failures (411 executions). Authoring validation, native verification/shipping ARM64 builds and all 28 player save/recovery stages passed (42 total gate stages). Approved locker fixture repair passed. Source/gate authored scene bytes now match, including preserved full-frame camera viewport; explicit saved-viewport assertions pass. Automated facts establish the new Wakeup pickup/locker/death/recovery journey, repaired systems/terminals/door state and physical traversal, authored scrubber progress/reader continuity, and preserved reload-reset behavior. They do not establish manual HUD conformance.

## Manual checks

Used exact source copy in /private/tmp/solitude-succession-p2-s2-gate and explicit disposable save-directory argument. First visual Editor exited during startup; retry with unity open --wait remained connected. No product change for infrastructure retry. Primary Editor/save files untouched. Probe source and JSON under Logs/SurvivorSuccession/p3-s2-manual.

S1: initial active A 83412635-42b6-403f-b5a4-c53e6215bc7f, body/cryobay point (-0.89,-2.4), health/oxygen values 100. Actual awake position/world/HUD screenshot p3-awakening.png.

S2/S3/S4/S8: ordinary focused WorldPickup interaction collected one battery; test preparation moved it to hotbar and granted four inventory batteries. Actual death completed, A Dead, distinct B 9d0c4d20-c8e7-44cf-8e5f-ed64a3bf4d4c, zero carried items, five cached items, one acknowledgment. p3-ai-transition.png shows readable transition message and recovery prompt. Native recovery screenshot confirms actual world/camera and readable existing transfer panel.

S4/S5: B approached cache, focused interaction opened existing modal at scale zero. UI scroll/beginDrag/drop/endDrag handlers recovered all five units, cache empty, closing restored scale one. recorded recovered.json; no product source/asset edits during manual observation.

S7 manual FAILED: visible oxygen bar remains half filled even though current resource reports Max=100, Current=100, Normalized=1. Read-only UI probe finds active/enabled Oxygen VitalBarView with _vital=null and Image.fillAmount=0.5; Health view is bound and fill=1. editor-retry.log lines 953/954 and 970/971 contain NullReferenceException from VitalBarView.OnEnable at line 19 during startup. Thus the initial HUD binding can run before PlayerVitalsController/resources are ready, throw and never reconcile; resetting the retained survivor resource does not repair a missing subscription. This is a concrete readiness gap present at startup, not evidence that succession lost oxygen state. UI art/normalization remains deferred.

Capture-tool 1280 frames sometimes omitted world tilemaps; native ScreenCapture recovery frame shows them, and read-only TilemapRenderer probe confirms Floor/Walls/WallPanels enabled and active. Such intermittent capture frames alone are not world-persistence-failure evidence. Camera tracked B at (-0.19,-3,-10) after relocation. Remaining manual doorway/scrubber checks were not run after the HUD failure; automated checks of those scenarios passed.

## Proposed scoped repair

Make VitalBarView bind safely when its player/vital becomes ready, reconcile the current value, and maintain one subscription through enable/disable. Add a meaningful startup-order/re-enable/awakening HUD regression, without resetting character values just because UI opens and without restyling the HUD. This needs a human-approved plan file-scope amendment for Features/Player/Vitals/Views/VitalBarView.cs and a focused test (plus metadata if new). Preserve existing art and deferred normalization. Rerun the full gate and complete manual HUD/doorway/scrubber checks at a new snapshot. Await human decision; ten interrupted files remain uncommitted.
