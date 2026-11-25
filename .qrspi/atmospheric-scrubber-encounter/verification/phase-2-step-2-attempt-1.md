# Phase 2 Step 2 — atomic installation and refusal feedback

Commit `f11a7774f8227704f6a1ab66afaf7177a5e67b69`; source snapshot `phase2-step2-source-snapshot.json`. Spec SHA-256 `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`; normalized phase plan SHA-256 `228139f6c11f67c0ee7e0cabac57b39e3282f208f089ab5b1867ac85915c170b`. All approved input digests verified unchanged before work and after verification.

The scene-local scrubber repair delegates to existing RestorableSystemState safeguards, using its atomic command callback and observer isolation. Equipment binds the exact authored fuse GUID to validated player inventory and the frozen presentation catalog. UI listens to inventory/state changes, rechecks at activation, updates disabled reasons and objective, and ignores hidden actions. Faulted restart returns the specified failure text. Repaired state remains OFFLINE, airflow STOPPED, CO₂ ELEVATED; restart explicitly unavailable until Phase 3.

| Criteria | Verification | Expected | Actual |
| --- | --- | --- | --- |
| S4/S5/S7; existing restoration regression | Bundled .NET SDK `dotnet run --project Tools/Verification/PureTests.csproj -p:NUnitPath=<absolute cached NUnit DLL>`; `phase2-step2-pure.log` | Missing/wrong/hotbar/locker, busy/unavailable and removed/stale-item attempts preserve fault/items; one repair commits before observers; reentry/duplicate and observer failures cannot consume again; restored objective with stopped airflow/elevated CO₂ | 64 passed, 0 failed; includes all existing RestorationTests |
| S2/S4/S5/S7; preserve S1/S3/S8/S10 | `unity test /private/tmp/solitude-scrubber-phase1 --mode PlayMode --filter SOLITUDE.Tests.ScrubberEncounterSliceTests` with isolated save directory; `phase2-step2-playmode-retry.xml` | Missing-item disabled control and exact faulted restart failure, stale UI activation, one installed fuse before inventory observer, duplicate/hidden action refusal, consistent reopened telemetry; previous pickup/input/save tests preserved | 4 passed, 0 failed, 0 skipped |
| Hygiene | `git diff --cached --check` | No whitespace errors | Passed |

The first PlayMode attempt (`phase2-step2-playmode.xml`) had one historical input-test assumption failure: once Restart became enabled, holding W navigated to it. The test now captures the actual selected control before Close and checks that selection on reopen, then explicitly selects Close for the EventSystem submit check. No failed result was treated as a pass; the final suite passed against the recorded source.

Both Phase 2 steps are committed and complete. No Phase 3 functionality or final qualification is claimed. Full player-build/regression gate and independent final review remain outstanding. Phase 1 manual visual/audio waiver remains historical and does not widen.
