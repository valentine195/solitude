## QRSPI Plan: mechanical-tool-repairs

## Inputs
- Spec: spec.md — SHA-256: 02a87da88f295cfd3fae2bcb41c91067ff5326decaba9fa7e7c17d3069d027b9
- Research: research.md — SHA-256: 374db9243ce4e4eb9d084852ff013b2ec4798117e1315f1d7d1dd256b050edc8
- Selected approach: approach.md — SHA-256: 6b6e8cd6a7a2b15805b68973a134564c30022bdc28cf5b826284fe6e3b38140c

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Persistent mechanical repair | none | Repair the selected socket with a carried mechanical tool and one inventory Battery, unlock its door, and preserve the coherent outcome across saves and lifecycle changes in an opt-in production-session fixture. | 5 | [ ] |

## Baseline and execution boundaries

Use a new isolated branch/check-out based on reviewed weapon commit `31caf33acc2554fcd5814a9e6f1fd1773c650986`. Do not integrate it into the main working tree, clean/stage concurrent inventory/input/scene edits, or continue work on the separate weapon feature. Preparation creates no product commit; the five behavior steps each produce one scoped commit by default. Copy only this feature's approved workflow inputs into the isolated root; never copy/edit helper state manually.

Research/Shape inspected the dirty main workspace at 4c59a8f. Before editing, confirm the selected baseline still supplies the cited tag, capability, interaction, session, restoration and door contracts. Record differences from Research. A material missing contract or conflict stops implementation for plan reassessment; do not silently import unrelated main-workspace changes. Use stable authored IDs and preserve existing GUIDs/assembly boundaries.

This is a single vertical phase because there are five steps total. It exits with the complete playable production-session fixture and durable repair behavior, not only an isolated data layer. The approved Spec explicitly defers shipping placement. Preserve the original disposable restoration demo and painted layouts. Any scene/asset authoring uses Unity Editor APIs/tools and additive component/configuration changes; disposable fixtures and captures stay outside shipping content.

## Verification and review

Only directly relevant checks run: transaction/eligibility tests with their code; contextual/door binding cases; actual fixture input journey; targeted save/recovery/lifecycle scenarios; and limited affected regressions. Record commands, expected/actual outcomes, source snapshots and retained-evidence limits. No blanket full suite or unrelated build target. Build the verification player only when a changed process scenario needs it; shipping guard checks inspect existing exclusion boundaries unless an actual changed shipping dependency warrants compilation.

Source changes after verification require appropriate new evidence; unchanged checks are reused with their exact source limits. Preserve failures and stop at the human gate. The phase ends with independent QRSPI conformance review against the approved Spec and exact isolated diff. Do not auto-merge or publish. Single-step execution is the default unless the human explicitly chooses another mode.

## Criterion coverage

| Step | Delivers or directly verifies | Preserves/reinforces |
| --- | --- | --- |
| 1 | S2–S4, S7–S8, S14–S15 | S9, S16, S19–S20 |
| 2 | S2–S3, S5–S6, S11, S13, S15 | S7–S8, S17, S19–S20 |
| 3 | S1, S5, S13, S18 | S2, S4, S6, S11, S17, S19–S20 |
| 4 | S9–S12, S14–S16 | S4, S7–S8, S18, S20 |
| 5 | S17, S19–S20 | S1–S16, S18 through targeted regression/source evidence |

The detailed plan and overview require human approval at their normalized revisions before implementation.
