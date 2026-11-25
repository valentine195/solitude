## QRSPI Plan: atmospheric-scrubber-encounter

## Inputs
- Project root: `/Users/jeremyvalentine/SOLITUDE`.
- Spec: `spec.md` — SHA-256: `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`.
- Research: `research.md` — SHA-256: `95d0ee277b0d33a8dbd2ee60f75577cfae772fcbee3b5d457da87a405331866d`.
- Selected approach: none; Shape skipped.
- Spec provenance verified against current request, questions, research and supplemental UI guide. Human approved the exact spec and this three-phase/seven-step breakdown in chat.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Discover and inspect | none | A faulted room with warning and inspectable scrubber; safe nonpausing interface input. | 2 | [x] |
| 2 | Collect and repair | 1 | One fuse pickup, exact inventory requirement, atomic repair and failure feedback. | 2 | [x] |
| 3 | Restart and recover | 2 | Five-second recovery, synchronized world/task feedback, reload isolation and regression qualification. | 3 | [x] |

## Criterion coverage
Coverage is incremental: a phase does not claim full conformance for criteria whose remaining behavior belongs to a later phase.

| Criteria | Planned steps |
| --- | --- |
| S1 | 1.1 initial room/alert/vitals; 2.1 reachable fuse; 3.1 no vital changes during recovery |
| S2 | 1.1 screen and semantic statuses; 2.2 and 3.1–3.2 updated states |
| S3 | 2.1 pickup capacity and identity |
| S4 | 2.2 rejected installation and faulted restart |
| S5 | 2.2 exact once-only consumption and Repaired Offline |
| S6 | 3.1 running/recovery timing; 3.2 completion |
| S7 | 1.2 inspection lifecycle; 2.2 duplicate repair; 3.1 recovery/duplicate restart |
| S8 | 1.2 primary interface and input ownership |
| S9 | 3.2 completion notification and synchronized feedback |
| S10 | 1.1 disposable startup; 3.3 coherent reset, save isolation and regression gate |

## Scope and execution controls
- All source paths in phase files are relative to the project root. New Unity assets/folders require their `.meta` files; retain existing GUIDs and assembly boundaries.
- New runtime/presentation content belongs in `Assets/_Project/Features/World/ScrubberEncounter`; rules in Domain; coordinators in Application; editor builder in Editor; tests in existing test assemblies.
- Reuse existing room art, fonts and shared primitives. No new art generation is required. Apply the SOLITUDE art pipeline for any necessary visual asset integration and Unity skills for live scene/UI edits during implementation. Create/edit scenes, prefabs and authoring assets through Unity APIs, not handwritten YAML. Do not overwrite existing demo or production scenes.
- Use the existing `RestorationDemoSession` marker for a fresh in-memory encounter session. Use encounter-local runtime config/catalog; leave production item database and shipping build scene list unchanged.
- The tree already contains extensive unrelated changes. Before implementation, capture the exact starting status and resolve any helper dirty-tree refusal through the owning workflow; never bypass it or stage, revert, commit, or clean unrelated work. This plan is not permission to push, publish, merge, or deploy.
- Shared modal policy changes are the principal regression risk. Preserve container pause/drag/close semantics; the new screen owns a disposable nonpausing input lease and rejects inventory opening while active.
- Step recovery means restoring only the step's own changes relative to its recorded pre-step snapshot. Shared dirty files require patch-level recovery; never wholesale restore them from HEAD.
- Default execution is interactive, with phase reviews before the next phase. Autoloop is optional only after explicit authorization of exact approved spec/plan revisions and resolved phase scope; it permits one repair per phase and stops before final review.

## Verification and evidence
Run meaningful targeted Domain/Application and Unity checks with each step. After all changes, run the complete `python3 Tools/Verification/verify.py` gate from the project root, plus scrubber PlayMode and manual readability/audio checks. Require a successful final `results.json`; `--skip-build` is partial evidence only. Confirm the full gate actually discovers the new tests; run a targeted suite when discovery does not include them.

Keep criterion-linked reports and screenshots outside Assets, under this feature's `verification/` or `work/Reviews`. Record source revision and relevant dirty-file digests with results. Unrun, failed or partial checks remain explicit. Nothing has been implemented or tested by writing this plan.

## Approval revision rule
Approve this overview and all three phase files together. For approval digests, normalize only step-status tokens on status-marker lines and Status cells in the phase overview to `[ ]`, then hash UTF-8 bytes. All other content is significant. Initial markers are already `[ ]`, so raw and normalized digests match.
