## Phase 5: Qualification

### Dependencies
- Depends on: 4
- Why: Requires phase 4's production route, normal encounter interaction, consumables and lethal gameplay path, plus their complete save/death/ship contracts supplied transitively by phases 1–3. Process/manual qualification must exercise actual integrated content.

### Step 1: Qualify interrupted saves, death and legacy recovery
- [x] Status marker
- Criteria: S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13, S16 — intentional combined-snapshot qualification.
- Files: `Core/SaveLoad/VerificationFaults.cs`, `Verification/Player/SolitudeCompositionSmoke.cs`, `Editor/PhaseFiveFixtureBuilder.cs`, `Tests/Application/SaveRecoveryTests.cs`, `Tests/Application/RunLifecycleTransitionTests.cs`, `Tests/Editor/SaveJsonCodecTests.cs`, `Tools/Verification/verify.py`, `.qrspi/run-lifecycle/verification/p5-s1.md` (evidence).
- Changes: Extend existing verification-only fault hooks and disposable process scenarios to interruption around death commit/durable save/activation, repair consumption, migration reconciliation and scene release. Include backup/corrupt/incompatible candidates and write rejection. Reuse existing process runner and recovery path; no shipping fault API. Capture exact source snapshot, criteria, expected/observed values and candidate bytes. Correct implementation defects only within the approved step scope, then rerun the affected checks.
- Tests: S12 — forced termination resumes a complete old durable snapshot or committed successor; never mixed gear/repair state, repeated supplies, or revival after durable acknowledgment. S3 — retries/zero-health startup stay gated until completion. S5/S11 — living position/state/defeat/collection round-trip. S8/S9/S13 — legacy absent component, saved locker ownership and consumed repair facts remain coherent. S16 — New Save independent from travel/death. Run isolated built-player scenarios plus relevant pure/EditMode/PlayMode tests and full gate for changed qualifying source. No user save access.
- Recovery: Save all failed snapshot/fault evidence before rerun. Resume each fixture from its own captured candidate set; preserve backups/corrupt files as evidence. A failing result is not a qualified phase; do not waive it by rerunning only an easier scenario.
- Risk: High — process ordering/owner disposal defects may appear only after termination.

### Step 2: Complete regression, production journey and review handoff
- [x] Status marker
- Criteria: S1–S16; S14 additionally requires subsequent independent scoped Review.
- Files: `Tools/Verification/verify.py` (execute; modify only if uncovered qualification is missing), `Tests/PlayMode/RunLifecycleSliceTests.cs`, `.qrspi/run-lifecycle/verification/p5-s2.md` (evidence), `.qrspi/run-lifecycle/plan.md` and phase markers (progress only).
- Changes: Run the complete isolated gate for the final source snapshot, preserve passing results/logs/builds and relevant test reports outside Assets, and perform the actual production manual journey. Check prior steps' evidence remains tied to current relevant sources. Record criterion coverage, risks/limitations, visual observations and source fingerprints for the human; do not supply implementation/test claims as inputs to the fresh independent reviewer. No new optional systems or broad cleanup.
- Tests: S1–S16 — empty initial colonist; ordinary collection/disable/repair/unlock; scene revisit; lethal A→B; enemy/supply refresh with persistent scrubber/shortcut/storage; lost-fuse recovery; living saved-location restart; New Save; input, HUD, collision and interaction presentation. Require passing final `results.json`, meaningful feature tests and no unexpected player exceptions. Check shipping build excludes verification/test/NUnit assemblies. Scope is macOS ARM64/Wakeup; other platforms are not qualified. If the same complete source snapshot was already fully gated in Step 1, reuse that matching evidence instead of repeating unchanged checks; rerun after any source change or uncovered failure.
- Recovery: If final sources/evidence change, stop completion and identify the criterion/step needing requalification. Preserve completed markers and prior evidence; add repairs through the owning step or an explicitly reviewed plan change, not by rewriting history. Leave final Review and any integration action for their human gates.
- Risk: Medium — a pass from a stale snapshot or demo fixture cannot qualify the production loop.
