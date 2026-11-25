# Phase 3 Step 1 — restart and active-time recovery

Spec SHA-256 `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`; normalized phase plan SHA-256 `78a395ddb462f2468b87505cc01445e6ae509ccbb65d5db0af3130486d1c69b7`. Source snapshot `phase3-step1-source-snapshot.json`; base HEAD `f11a777`. Inputs unchanged. Work is uncommitted and unstaged.

Implemented confirmed running before success feedback, duplicate-safe restart, five-second equipment-owned active timer, actual-pause suspension, categorical airflow/CO₂/objective states, green word-labeled schematic/world indicator, recovering warning and a looping operating AudioSource. Warning/alert reconcile on Operational, and disable/unload stops both sources. Authoring through isolated Unity Editor API preserved scene GUID; no production scene/catalog changes.

| Criteria | Verification | Expected and actual |
| --- | --- | --- |
| S6/S7 | Bundled SDK `dotnet run --project Tools/Verification/PureTests.csproj` with absolute NUnitPath; `phase3-step1-pure.log` | Controlled timing before/at five seconds, faulted refusal, repaired restart once, repeated calls cannot reset timer, completion events once, atomic repair regressions preserved: 65 passed, 0 failed |
| S1/S2/S6/S7; preserve S3–S5/S8/S10 | Isolated `unity test /private/tmp/solitude-scrubber-phase1 --mode PlayMode --filter SOLITUDE.Tests.ScrubberEncounterSliceTests` with disposable save directory; `phase3-step1-playmode.xml` | Clock advances while primary screen open; close/leaving proximity preserves recovery; actual pause suspends; running sound source plays, warning recovers then clears, alert source stops at NORMAL, schema/details match, health/oxygen and production sentinel unchanged. 5 passed, 0 failed, 0 skipped |
| Authoring/hygiene | `unity run` isolated project `-executeMethod SOLITUDE.Editor.ScrubberEncounterBuilder.AddRunningAudio`; `phase3-step1-authoring.log`; C# `git diff --check` | Editor authoring exit 0, GUID preserved; no source whitespace errors |
| S6 manual sound | Listen to operating sound and state changes | UNRUN. Automated `isPlaying` is not an audible quality check |

Step 1 completion is pending the Phase 3 manual-check decision asked asynchronously. Prior override was recorded only for Phase 1; it is not silently widened. Step 1 marker `[!]`; Steps 2/3 remain unstarted. No completion notification, reset qualification, full verification gate or independent final Review is claimed.
