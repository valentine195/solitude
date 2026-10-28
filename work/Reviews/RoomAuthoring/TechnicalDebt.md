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

## RA-TD-002 — Atomic Room Paint/Box ownership rejection

- Status: Resolved in Phase 3 Step 2 under approved Plan27; earlier failing observations remain retained.
- Criterion: S42, preserving S12/S14/S27/S29.
- Original trigger: Room Paint/Box could retain authored overlapping cells after derived generation refused, unlike corridor preflight.
- Repair: Reuse cross-owner preflight for direct cell/box writes. Reject a mixed box before mutation; cancel the whole room gesture if a later sample conflicts. Existing brush feedback identifies the affected cell and both owners. Erasing and corridor preview/width behavior are preserved.
- Verification: Nine new regression cases cover direct cell/box calls, translated member/independent-layout/legacy ownership, wrong edit protection, late Paint/Box conflict, unchanged output identities/resources, absence of partial Undo, and successful follow-up fill/erase/Undo/Redo. Focused checks passed 42/42; the current RoomAuthoring Edit Mode suite passed 580/580. Actual Unity preview-clone checks preserved Supply's 26 cells; late gestures staged 27/30 then restored 26; the saved 176-cell wing, settings, reservations, connections, sentinels and resource count stayed exact. The Tile Palette displayed the located rejection.
- Evidence: `work/Reviews/RoomAuthoring/MedbayPanelingWorkflow.md` and `.qrspi/floor-driven-room-authoring/verification/phase-3-step-2-atomic-*`.
- Boundary: This closes only RA-TD-002. RA-TD-001 remains the separately accepted multi-Inspector limitation; no blanket S49/final qualification waiver.
