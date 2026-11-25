## QRSPI Plan: floor-driven-room-authoring

Revision 3 draft for human review, using approved Spec revision 2. Insert one five-step Phase 2a before artwork integration. This uses the phase-insertion mechanism rather than restarting or renumbering completed work. No new implementation has begun.

## Inputs

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`; SHA-256 hashes identify exact file bytes.

- Spec: `.qrspi/floor-driven-room-authoring/spec.md` — SHA-256: `5e259da45c328e87a404d2cbcc83da875d486a611823ef1760ae03865685cf71`
- Research: `.qrspi/floor-driven-room-authoring/research.md` — SHA-256: `0fcb77321743205e1a95524a6737e797b4fbf7e7bbf5aeb4fd2463602d47e1c5`
- Selected approach: `.qrspi/floor-driven-room-authoring/approach.md` — SHA-256: `0ed06af62ea3e51b3a828f58c400ffb901423815511393fd5b077aa9901c1dca`

The human approved this exact Spec after reviewing its expanded scope. This draft plan is not yet approved. Earlier plan approvals and completed-phase evidence remain historical; they do not verify changed/new criteria in this revision.

## Phase overview

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Create and rebuild a room | none | Historical completed rectangle baseline: owned room/style, deterministic Rebuild, safety, persistence and runtime-mask exclusion. | 4 | [x] |
| 2 | Paint and edit rooms | 1 | Historical completed editing baseline: reusable palette/brush, batch operations, gesture Undo/Redo and explicit box modes. | 4 | [x] |
| 2a | Follow irregular layouts with useful partial previews | 1, 2 | Preserve valid output, follow cutouts/holes and oriented joins, show selectable unresolved regions, and verify coherent editing/restoration with synthetic art. | 5 | [~] |
| 3 | Integrate Standard artwork | 1, 2, 2a | Verify prepared convex/concave art and native joins, bind Standard, demonstrate complete/incomplete irregular editing, and finish production verification/guide. | 4 | [ ] |

- [Phase 1 details](plans/plan-phase-1.md) — unchanged, historical completed work.
- [Phase 2 details](plans/plan-phase-2.md) — unchanged, historical completed work.
- [Phase 2a details](plans/plan-phase-2a.md) — new work for approved Spec revision 2.
- [Phase 3 details](plans/plan-phase-3.md) — unfinished art phase updated for the expanded target.

Phase 2a directly consumes Phase 1's room/style/output ownership and writer/lifecycle contracts and Phase 2's brush, batch and Undo workflow. Phase 3 directly uses original room/asset setup, the palette/brush workflow, and Phase 2a's extended joins/results/selectable preview. Dependencies name required outputs rather than table order.

## Revision and execution boundaries

- Completed Phase 1 and 2 files, IDs, filenames and step markers are unchanged. Their tests/reviews/commits retain their original snapshots. Changed/new Spec criteria need Phase 2a and later evidence; completed markers are not a claim that revised requirements already pass.
- Retain 32×32 cells/floors, 32 PPU, 32×64 or 64×64 walls and 32×32 cap/corner canvases. One configured structural north repeat width per style; no mixed-width solver, clipping, scaling, replacement art or import rewrites.
- Follow cutouts, holes, concave corners, separated components under one room identity, and distinct diagonal boundary branches. Partial results retain current valid floors/architecture and locate unresolved dependencies. Room-wide unsafe ownership/coordinate/overlap conditions still refuse destructive writes.
- Phase 2a is a complete synthetic geometry/editor slice. Its five steps each pair behavior and verification: partial generation; irregular placements; selectable guides; restoration/runtime boundary checks; native walkthrough and guidance. Standard art is not a prerequisite for that work and synthetic sprites are not production-art evidence.
- Phase 3 remains conditional on actual prepared assets. Step 1 refreshes factual asset evidence through isolated owning Query/Research skills as needed, records unavailable/failed readiness honestly, and stops before asset binding if inputs become stale. Changed Research requires Spec/Plan reassessment and exact-revision approval; never just replace stored hashes to make a check pass.
- Use supported Unity asset/scene APIs and relevant Unity editor/UI guidance; use the SOLITUDE art pipeline for production asset validation/integration. Setup can create missing assets/scaffolding but cannot overwrite painted layouts. All new Unity files need metadata; preserve existing GUIDs and assembly ownership.
- Keep editor guides/selection and missing-floor cell visibility out of runtime floor queries, colliders and builds. No asynchronous observer or runtime generation loop; external-tool/config edits have explicit Validate/Rebuild recovery.
- Preserve dirty production scenes, original art, NPC work, root README and unrelated documents. Record baselines; never stage, discard, commit or clean unrelated work. Phase 2a explicitly includes only the feature-owned durable `Docs/Architecture/RoomAuthoring.md` update, with unrelated additions preserved. No external messages, pushes, publication or integration are authorized by this plan.
- Review assets/configuration facts before claiming complete art fit. Full-height walls near narrow holes may produce local unresolved placements; don't hide an art constraint by falsely calling the whole geometry unsupported or the partial preview production-complete.
- Each implementation step records criterion-linked expected/actual results and exact source/configuration hashes, commits before its completed marker, and stops on failure under QRSPI Implement. Phase execution covers all five Phase 2a steps only when explicitly selected. Independent checkpoint Review and final Review remain separate human gates. Autoloop requires separate explicit authorization.

## Criterion coverage

The table below maps every active criterion to current-revision work. Unchanged completed phases remain baseline context, not substitutes for changed assertions. `phase.step` identifies a detailed step; repeated coverage is intentional. S9/S23 need production evidence even after Phase 2a passes.

| Criterion | Current-revision planned steps |
| --- | --- |
| S1 reusable styles | 2a.1, 2a.2, 2a.5, 3.2, 3.4 |
| S2 explicit identity | 2a.1, 2a.4, 2a.5, 3.4 |
| S3 authoritative occupancy | 2a.1, 2a.2, 2a.4, 2a.5, 3.4 |
| S4 grid/native art | 2a.1, 2a.2, 2a.5, 3.1–3.4 |
| S5 create/paint | 2a.1, 2a.3–2a.5, 3.2, 3.4 |
| S6 rectangle geometry | 2a.1, 2a.2, 2a.5, 3.4 |
| S7 floor coverage | 2a.1, 2a.2, 2a.5, 3.2–3.4 |
| S8 wall coverage | 2a.1, 2a.2, 2a.5, 3.1–3.4 |
| S9 production perspective/joins | 3.1, 3.3, 3.4 |
| S10 expansion | 2a.1, 2a.2, 2a.5, 3.3, 3.4 |
| S11 erasure/empty | 2a.1, 2a.2, 2a.4, 2a.5, 3.4 |
| S12 determinism | 2a.1, 2a.2, 2a.4, 2a.5, 3.4 |
| S13 missing assets | 2a.1–2a.5, 3.1, 3.4 |
| S14 ownership isolation | 2a.1, 2a.2, 2a.4, 2a.5, 3.4 |
| S15 Undo/Redo | 2a.1, 2a.2, 2a.4, 2a.5, 3.4 |
| S16 persistence | 2a.1, 2a.2, 2a.4, 2a.5, 3.2–3.4 |
| S17 validation/recovery | 2a.1, 2a.3–2a.5, 3.4 |
| S18 lifecycle | 2a.1, 2a.3–2a.5, 3.4 |
| S19 runtime compatibility | 2a.3–2a.5, 3.3, 3.4 |
| S20 preservation | 2a.1, 2a.4, 2a.5, 3.2, 3.4 |
| S21 verification evidence | 2a.1–2a.5, 3.3, 3.4 |
| S22 demo/guide | 2a.5, 3.2, 3.4 |
| S23 asset readiness | 3.1–3.4 |
| S24 irregular boundaries | 2a.2, 2a.5, 3.1–3.4 |
| S25 components/contact | 2a.2, 2a.4, 2a.5, 3.2, 3.4 |
| S26 partial preview | 2a.1–2a.5, 3.1–3.4 |
| S27 selectable feedback | 2a.3, 2a.5, 3.2–3.4 |
| S28 correction/dependencies | 2a.1–2a.5, 3.1–3.4 |
| S29 incomplete restoration | 2a.1, 2a.4, 2a.5, 3.2–3.4 |

## Verification and approval

Use isolated fixtures/projects/disposable saves. Run focused tests for changed behavior and the actual native UX checks specified in each step. Evidence must match current source; do not repeat broad passing tests without a new change, failure or unresolved concern. Inconclusive native input is not a pass. Production seam renders are additional to code assertions.

Phase 3 retains the existing complete isolated gameplay gate because final integration affects floor/awakening behavior. Its `results.json` and build results must pass; a partial/skip-build/unavailable gate does not qualify final completion. Phase 2a uses focused room Editor/PlayMode/lifecycle checks without modifying gameplay consumers.

This plan drafting ran no implementation tests, builds or renders. Approval covers the overview plus all four phase files at [plan-revisions-r3.md](plan-revisions-r3.md). Normalize only step status-marker tokens and overview Status cells to `[ ]`; all other UTF-8 content is significant. Phase 1/2 normalized digests remain their historical approved values. Prior revision manifests are preserved.
