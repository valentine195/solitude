# Final F1 repair / Phase 3 Step 3 — PASS qualification

Commit 64ae846597a0d2c13815d4f7a23ed6fb5b583374; base 69afe43795ee8510dff48b745c70826e0bdc3d5c. Snapshot final-f1-repair.source.json records exact three source hashes, approved spec/upstream input hashes, normalized plan digests and results.json digest. All inputs rechecked before commit, tested copies and staged bytes identical. Failed review final.md remains historical evidence.

S4/S5/S9: SurvivorRecoveryContainer listens for slot changes, rejects empty stale interaction, and withdraws exhausted presentation/proximity in LateUpdate after command service is idle. Existing OnDisable closes the modal and unregisters proximity; the registered container is retained until scene release. No shared focus framework, positions or ship systems changed.

S5/S9 regression: SamePositionDeathsRemainRecoverableThroughOrdinaryFocusAfterExhaustion creates real A→B→C equipped deaths at identical coordinates with 3 and 5 units across inventory/hotbar. Natural proximity selects first target; production keyboard E and real UI drag/drop handlers recover it, next target gains focus, all eight units recovered. Empty owners remain registered, positions unchanged, retired identities preserved and exhausted targets no longer in nearby set. PASS. No manual cache Release or target override. Existing partial/full-capacity, gesture and scrolling checks pass; nonempty reopen still resets scrolling, exhausted reopen expectation now asserts withdrawal.

S12 command: DOTNET_CLI_HOME=/private/tmp/solitude-succession-dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-succession-f1-gate --output Logs/SurvivorSuccession/final-f1-repair

Actual exit 0 / results.json Passed: 121 pure + 221 EditMode + 71 PlayMode = 413 executions, zero failures. Complete gate, both native ARM64 builds, authoring/metadata validation and 28 player scenarios / 42 total stages. macOS ARM64/Wakeup qualification; no durable survivor/cache or scene-transition persistence claimed. Fresh final review still required and authorized.
