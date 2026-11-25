# Phase 2 Step 3 attempt 1 — failed

Spec SHA-256: f2253ab3429d7c5a34c8e9275f9efac61be9e3f4c30781393ad7d86e68ba85a4. Normalized approved phase 2 digest: 579df41af7633663158efc27efd331ea52ba3633c68d11cd83f6423ac3a8af9a. Exact seven-file source snapshot: p2-s3-attempt1.source.json, base HEAD 94dee5d3994cf958d37e02efb475cdb0871fba09. Source digests matched after verification. No commit.

## Required gate

Command: DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-p2-s3-gate --output Logs/SurvivorSuccession/p2-s3-attempt1

Actual: exit 1, results.json Failed; pure 119 passed, EditMode 218 passed, PlayMode 58 passed / 1 failed. Gate stopped at PlayMode, so native builds/player stages were not run. Authoring passed. New equipped death/gesture cleanup, missing recovery conservation, real committed-handoff activation failure and retry, powered-door/pickup equipment recovery, and running installed scrubber continuity tests passed. These establish narrower S2–S8/S10 facts, not a completed phase or whole-spec verdict.

S9/S5 failed: RepeatedEquippedDeathsKeepPartialCachesAndRecoverThroughInteraction expected the desired recovery cache but PlayerInteractor currentTarget was another recovery object, at OpenRecovery line 172. Cause unresolved; investigate the actual positions, focus-selection and trigger lifecycle before choosing a production or fixture repair. Equipment duplication/loss was not reported by this assertion.

## Actual Wakeup UI

Used stopped disposable Step 2 visual project, copied all seven exact changed source files and restarted it with an explicit disposable save directory. Diagnostic reset first failed because StartNewSave releases bindings before asynchronous rebinding; the setup was rerun after those frames completed, without another reset. No product source was changed to handle that diagnostic error.

Actual equipped death moved four inventory batteries plus two batteries in the final hotbar slot into one 33-slot cache. A/B identities were distinct, contents total six, final recovery slot quantity two. Real focused PlayerInteractor.TryInteract opened the existing transfer screen, IsOpen true, scale zero, two ScrollRects.

S4/S5 functional visual check FAILED: screenshot Logs/SurvivorSuccession/p2-s3-ui/scroll-top.png shows first grid columns clipped and their icons inaccessible. Read-only layout diagnosis: target viewport width 290.26, content width 425.27; inventory viewport width 253, content width 388.01; both content widths exceed their viewport by 135.01. Stretched content retains a horizontal sizeDelta from existing scene overrides. Target first slot center local X -180.64 is beyond viewport X minimum -145.13. Anchors are (0,1)/(1,1), pivot (.5,1), anchoredPosition zero. Proposed repair: normalize scrolling content horizontal extent to its viewport during bind, strengthen the full-capacity test to check both X and Y reachability (existing test only checks Y), and recheck visible item transfer. Deferred aesthetic normalization stays deferred.

Await human approval for scoped UI geometry repair, investigation/conditional repair of repeated-cache focus, and full gate rerun. Preserve all uncommitted integration changes.
