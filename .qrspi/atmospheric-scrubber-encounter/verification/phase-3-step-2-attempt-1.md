# Phase 3 Step 2 — once-only task notification

Commit `25432c87e5746e0e529cb268d7d5abf345acf30c`; source snapshot `phase3-step2-source-snapshot.json`. Spec SHA-256 `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`; normalized phase plan `78a395ddb462f2468b87505cc01445e6ae509ccbb65d5db0af3130486d1c69b7`. Exact input revisions unchanged.

S2/S6/S9: Completion queues one noninteractive notification in a top-right lane, deferred while the primary interface is open. Four-second unscaled dwell begins when display becomes available; no focus/selection change, no notification replay on reopening or reenabling. Completed objective remains in the screen. Warning and alert remain cleared at Operational.

Verification: isolated Unity Editor API `-executeMethod SOLITUDE.Editor.ScrubberEncounterBuilder.AddCompletionNotification` completed, preserving existing prefab/scene GUIDs (`phase3-step2-authoring.log`). `unity test /private/tmp/solitude-scrubber-phase1 --mode PlayMode --filter SOLITUDE.Tests.ScrubberEncounterSliceTests` with disposable save directory (`phase3-step2-playmode.xml`) passed 6 tests, failed 0, skipped 0. The new completion test verifies exact-once display, deferral, expiration, retained objective, no focus theft and no replay; prior input/repair/recovery checks remain passing. Source/staged whitespace check passed with conventional Unity YAML empty-field trailing spaces preserved.

Manual final layout/status/prompt/audio review waived by explicit Phase 3 override (see `phase-3-manual-waiver.md`), not passed. Step 3 full qualification remains outstanding. No independent review verdict is claimed.
