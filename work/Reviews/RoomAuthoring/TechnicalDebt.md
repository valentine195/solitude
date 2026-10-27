# Room authoring technical debt

## RA-TD-001 — Coordinate panel Apply across multiple pending Inspectors

- Status: Open; human accepted deferral on 2026-10-27 to proceed beyond Phase 2f.
- Source: QRSPI phase-2f-r9 F1 / S49 at commit `df09231c22ac1c03a081dde8ecbe3e03a003bbfb`.
- Trigger: Two Inspectors hold independent unapplied panel settings/reservation drafts in the same layout; a compatible artwork import is queued; Apply in one Inspector synchronously rebuilds while the other draft remains pending.
- Effect: The imported artwork publishes before the other draft resolves and its preview clears. The other draft remains pending; same-space Apply can then report a stale baseline. No authored-data deletion was established.
- Interim workflow: Finish or cancel the other pending panel drafts in the layout before applying panel inputs or refreshing artwork. Prefer one active panel-editing Inspector per layout.
- Repair: Coordinate synchronous Apply with other pending draft participants while permitting its initiating draft to finish. Preserve remaining drafts/previews and queued imports on refusal; provide actionable feedback.
- Verification: Actual attached room/layout Inspectors and different member spaces; settings and reservation drafts; compatible import; Apply/refusal/retry; exact generation counts, draft/preview/resource preservation and Undo/Redo.
- Acceptance boundary: Only this multi-Inspector S49 finding is deferred. The requirement is retained; no blanket S49 or final-qualification waiver. Phase 3/4 must report this known limitation rather than claim complete compliance.

## RA-TD-002 — Adjudicate Room Paint/Box overlap atomicity

- Status: Open structural follow-up, carried forward from phase-2f-r7/r8/r9; separate from RA-TD-001 deferral.
- Criterion: S42, earlier structural phase. Room Paint/Box can retain authored overlapping cells after derived generation refuses, unlike the corridor preflight.
- Next action: Reproduce and assess atomic cross-owner placement in a scoped structural follow-up. This is not resolved or waived by the multi-Inspector deferral.
