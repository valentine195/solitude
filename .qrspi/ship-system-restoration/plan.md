## QRSPI Plan: ship-system-restoration

## Inputs

Paths are relative to `/Users/jeremyvalentine/SOLITUDE`; digests are SHA-256 of exact input bytes.

- Spec: `.qrspi/ship-system-restoration/spec.md` — SHA-256: `3396d3146f9a39644b41dafcb38afb8321933f670d07f1fc36288b0a2c97479f`
- Research: `.qrspi/ship-system-restoration/research.md` — SHA-256: `73c7fceb71878834a1bda0eac8b01764d762979c70fff3c24ce6d12384b6db95`
- Selected approach: none; Shape skipped.
- Spec provenance checked against current request, queries, and research. Human approval: “continue” following the exact spec revision above.

| Phase | Name | Depends On | Description | Steps | Status |
|-------|------|------------|-------------|-------|--------|
| 1 | Reusable restoration demo | none | Install one inventory item, restore independent systems, enable a door and terminal, and demonstrate repeatable isolated play | 5 | [x] |

## Scope and architecture

Use a small pure restoration model in the existing Domain assembly, a scene-owned component exposing its state, and a junction using existing inventory commands. Door and terminal consumers reference the system, never the junction or each other. Use current-state reads plus local state-change notifications; do not repurpose interaction-trigger events as successful restoration events. No global registry, state framework, save schema change, or production scene migration.

Keep each step's code and tests together. Step 1 supplies restoration to steps 2–3; step 4 composes their outputs into a demo; step 5 verifies that integrated demo and shared-code compatibility. These are within-phase prerequisites, not artificial phase dependencies.

## Criterion coverage

| Step | Outcome | Criteria |
|---|---|---|
| 1 | Safe installation and junction diagnostics | S1–S4, S5 notification isolation, S7 junction feedback, S9 inventory compatibility |
| 2 | State-gated door access | S5, S6, S9 ungated-door compatibility |
| 3 | Powered terminal with placeholder content | S1, S5, S7 |
| 4 | Separate, reusable, resettable demonstration | S1–S3, S6–S9 |
| 5 | Full-loop, lifecycle, and regression verification | S1–S9 |

## Risks and verification

The highest risks are synchronous inventory observers seeing inconsistent state, direct door interaction bypassing the gate, and the persistent composition root retaining demo inventory on reload. Steps 1, 2, and 4 explicitly verify these boundaries. No gameplay tests ran during planning. Step 1 now includes the human-approved verification-runner repair: a disposable Git index for authoring validation, preserving source tracked-path membership.

Run focused Domain/EditMode/PlayMode checks during implementation. The shared inventory/composition changes require the full `python3 Tools/Verification/verify.py` gate from the repository root; `results.json` must pass, and `--skip-build` does not qualify the change. Run restoration-specific checks in addition to the Wakeup gate. Never launch verification against normal saves.

Preserve all existing dirty files, particularly untracked `Tools/Verification/PureTests.csproj`, skill installations, and `.gitignore`. Do not stage or commit unrelated work. Recovery means reverting only feature-owned changes with explicit authorization, not resetting the working tree.

## Approval revision rule

Approval covers this overview and `plans/plan-phase-1.md`. For approval SHA-256, normalize only step status tokens on status-marker lines and the overview table's Status cells to `[ ]`, preserving every other byte. Current markers are already `[ ]`, so current raw and normalized digests match. Progress alone will not invalidate approval. Any other plan change requires reassessment.
