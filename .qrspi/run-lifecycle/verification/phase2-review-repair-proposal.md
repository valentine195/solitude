# Proposed repairs after independent Phase 2 review

Review result: FAIL at HEAD `0836bb852d55b9770deda335073ceaea15dc7d75`. [Full review](../reviews/phase-2.md), artifact SHA-256 `f9b37b6620824ce11e17c1d193c9aa5cf0d0d9f01b0f4f2862680dee2f6ecadd`.

F1 / S8: saved critical-source policies are absent from a new session until their scene binds, so a death candidate prepared before that visit can leave a lost component unavailable. Proposed repair persists/reconstructs the bounded policy, checks old-record compatibility, and adds restart-with-unbound-source regressions.
F2 / S10: initially inactive door Awake overwrites restored frame/collision. Proposed repair makes initialization honor already hydrated state and tests inactive binding/activation.

The independent review used static source traces; it ran no tests. The earlier full-gate PASS is evidence for its executed cases, not proof these uncovered cases work. No fixes have been applied.

Propose appending one atomic repair Step 4, preserving Steps 1–3 completion/history. Spec and other phases unchanged. Human approval requested for these exact proposed revisions (normalizing only status markers):
- [Overview proposal](phase2-review-repair-overview-proposal.md): `dc6e3dec5d02179bc7c8fa565ab91e422de6afbaf5d6ad6d8e4c633eff4444cb`.
- [Phase 2 proposal](phase2-review-repair-phase-proposal.md): `bc733c6ee01970b3a5a493d42292baf0f4b886ae3d4f1953f0546fd43b2ef5a5`.

After approval: adopt these files, implement only Step 4, run the full gate, commit feature-only fixes, and pause for a new independent Phase 2 review. If verification fails, follow the existing human failure gate. No primary checkout integration, publishing, or Phase 3 work authorized by this proposal.
