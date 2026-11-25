# Phase 3 Step 3 qualification — attempt 1

Spec SHA-256: `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`.
Approved Phase 3 normalized SHA-256: `78a395ddb462f2468b87505cc01445e6ae509ccbb65d5db0af3130486d1c69b7`. Upstream and all approved plan revisions revalidated unchanged.
Source: `phase3-step3-final-source-snapshot.json` (6746 Assets/Packages/ProjectSettings files). Main source stayed identical through verification. Effective isolated workspace: `phase3-step3-tested-workspace-snapshot.json`. The gate generates fixtures/settings and Unity updates three URP settings assets for import/build prefiltering; these changes exist only in the scratch project. All feature source and authored encounter assets match the recorded main source. No production settings changes are committed.

## Verification

Command from repository root:
`python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-scrubber-fullgate --output /Users/jeremyvalentine/SOLITUDE/.qrspi/atmospheric-scrubber-encounter/verification/full-gate-attempt-3`

Expected: full Passed verdict, discovery of new suites, zero failed/skipped tests, both native builds and all production save/recovery scenarios qualified. Actual: `full-gate-attempt-3/results.json` status Passed, platform macOS ARM64. Pure tests: 65 passed/0 failed. Editor: 118 passed/0 failed/0 skipped. PlayMode: 26 passed/0 failed/0 skipped. Both verification and shipping builds exit 0; shipping verification/test-assembly exclusion and standalone collection, process restart, capacity, corrupt/missing/incompatible saves, denied-read and deliberate process-interruption recovery checks pass. Expected killed processes exit -9 in interruption scenarios; the gate verifies each recovered snapshot.

Discovery was checked in XML, not inferred from process exit: ScrubberEncounterTests (6), ScrubberScreenCoordinatorTests (3), ScrubberEncounterAuthoringTests (1), ScrubberEncounterSliceTests (6), ScrubberEncounterLifecycleTests (3), existing RestorationDemoLifecycleTests (2). All passed. Prior gate attempt 1 remains historical failed build timeout; attempt 2 passed the pre-live-fix snapshot and is not relied on for these fixes.

| Criteria | Concrete coverage and result |
| --- | --- |
| S1/S2 | Fresh room, warning/vitals/save, presentation and metadata checks pass; live corrected warning and interface states readable. |
| S3/S4/S5 | Pickup once/capacity refusal, missing/wrong/hotbar/locker/removed fuse rejection, atomic repair and reentry checks pass; live collection/repair/refused restart match. |
| S6/S7 | Controlled timing, real runtime clock, screen open/away, actual pause suspension and duplicate transitions pass. |
| S8 | Composed input ownership, close input consumption, EventSystem submit, selection retention and production input regressions pass. Manual native arrow focus/walking inconclusive under waiver. |
| S9 | Deferred completion, expiry, no replay and persistent objective pass; live notice and completed history observed. |
| S10 | Reload at five stages resets pickup/inventory/equipment/warning/objective together, destroys old endpoints/audio and releases primary-screen lease; save sentinel bytes unchanged. Both directions of production/demo session mixing rejected without save changes. Existing restoration and complete Wakeup gate pass. |

Live details: `phase-3-live-inspection-attempt-1.md`. Speaker audibility/mix quality and manually walked end-to-end route remain waived/unconfirmed per `phase-3-manual-waiver.md`; user was away from computer. No unrun manual check is called passed. Independent QRSPI Review has not run and remains the next human gate.

`git diff --check` passes. Shipping scene list, production item database, Wakeup scene, save schema and unrelated assets unchanged relative to checkpoint `f8017dc`. Existing asset GUIDs/assembly boundaries preserved. Commit is recorded separately after exact-source comparison and staging.

Completed-step commit: `8caae48883deab6fa8111a3266fa87bd4dc42f1a`. Step 3 and Phase 3 markers completed after commit.
