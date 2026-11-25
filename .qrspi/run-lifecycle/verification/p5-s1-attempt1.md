# Phase5 Step1 attempt1 — FAILED

Spec a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922. Approved normalized complete plans p4-f1-approved-plans.json. Exact source: p5-s1-attempt1-source.json; base c6c9272765920e284740d05fe575aedd0b790612. All3 modified source hashes remained stable. No source staging or commit.

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p5-s1-attempt1`.
Expected complete PASS including new combined fault scenarios. Actual: pure252, EditMode376, PlayMode155 (783 tests) passed; authoring and both macOS ARM64 builds passed; previous32 standalone scenarios passed; new combined qualification baseline, deliberate death-before-request kill and its resumed process exited as expected, then Python comparison failed with `Resumed death remained pending`. Remaining new scenarios not run. This is not a qualified phase.

## Failure diagnosis / proposed exact repair

The new verification helper QualificationSnapshot uses Unity JsonUtility.ToJson. For a null SavedDeathTransition, this produces an inline default object with empty id and stage0. The production SaveJsonCodec emits JSON null. Evidence: `death-before-request/qualification-observed.json` has default pendingDeath object, while its actual `solitude-save.json` has pendingDeath:null and the original living survivor/expedition. The verification process itself checks Coordinator.Pending==null and exits success. This is an observation serialization mismatch; no production lifecycle defect established.

Proposed patch `p5-s1-codec-repair-proposal.patch` replaces only QualificationSnapshot serialization with the production SaveJsonCodec using the composed catalog. It preserves actual null/precision semantics rather than weakening the runner's assertion. No plan amendment needed; helper file is already in Step1 scope. Patch SHA256: 13f7ea68d520ada6259a3f870388422c450e30fc5c76a75d6e9c9772f417e3d4.

After approval: apply exact patch, capture new source snapshot, rerun the entire gate including all previously passed and not-yet-run fault cases, then commit only on complete PASS. Keep candidate-before, candidate-after-crash, actual save/backup/temp, observation JSON and logs in this attempt as evidence. Apps remain under /private/tmp output, excluded from .qrspi copy. Step1[!]; Step2 remains not started.

QRSPI Implement requires: “In every mode, stop immediately and wait for the human when a step fails, a blocker appears, or a step can't be done as written.”
