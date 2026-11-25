# Weapon system plan revisions

Approval scope: the human approved the original detailed manifest (`ceefc308ae279cfd9c52337e65b7ed0e34878e5a150e45b5b0dd4dddee7f359f`). The subsequent instruction “Just ignore the `_Project.zip` archives; those are for giving to chat mode for reviews.” authorizes the narrow review-export exception now recorded in the overview and Phase 1 Step 1. The human subsequently approved both Step 1.2 validation prerequisite repairs: existing ReplacementFuse catalog membership and the isolated EditMode room-verification flag. The latest human approval corrects the mistaken fuse registration, preserving its demo-only catalog membership and using direct capability checks plus the existing production gate. The broad all-pickup audit is retained unchanged with its single-catalog limitation documented. The human approved the shared generic swing view/sprite refinement for Step 1.3. All other steps remain unchanged.

Normalization: replace only status tokens on `Status marker` lines and Status cells in numbered phase rows of the overview table with `[ ]`. Preserve every other byte of UTF-8 content, including step order, scope, dependencies, prose and newlines. Progress markers do not affect approval digests.

Spec SHA-256: `78d09ac0e72bb413355cffdab28a21a5e501e69138731c6ad1f0863ff63a74f5`.

| Artifact | Normalized SHA-256 |
| --- | --- |
| [plan.md](/Users/jeremyvalentine/SOLITUDE/.qrspi/weapon-system/plan.md) | `4237e80a8a2629f7f654a62af338395e7fbaf05daa1973c311778cbbc2ea9d0e` |
| [plan-phase-1.md](/Users/jeremyvalentine/SOLITUDE/.qrspi/weapon-system/plans/plan-phase-1.md) | `5d93d6546db58f9a493041c2a57e1ac61c37eb15549f62da1814ba3ad7b4360e` |
| [plan-phase-2.md](/Users/jeremyvalentine/SOLITUDE/.qrspi/weapon-system/plans/plan-phase-2.md) | `29e98141120a5931e8eea8a14207a12d20cc0a7f281e2f261bd1f884bab0a782` |
| [plan-phase-3.md](/Users/jeremyvalentine/SOLITUDE/.qrspi/weapon-system/plans/plan-phase-3.md) | `486a1027edcb4eb5956ef4a45535afda19c3439f6d0a05cfa84b197fb2a07cda` |
| [plan-phase-4.md](/Users/jeremyvalentine/SOLITUDE/.qrspi/weapon-system/plans/plan-phase-4.md) | `387dee60e0864ae9c306237165a232ef23a8fd488fe31b11f905a22ff47d8aab` |

Validation performed: structural checks verified four phases, fourteen sequential steps, dependency agreement between overview and phase files, all required step fields, S1–S32 coverage and current spec/upstream provenance. No product tests or gameplay changes are claimed.

Human approved checkpoint review F1/S15 repair at HEAD 06349bf6f51539fe7d5fd964926a81db85752d20. The overview and Phase 1 plan now include the bounded retirement cleanup and real-input succession regression; all original steps and later phases remain unchanged. The above normalized digests identify this approved repair scope.

Phase 2 Step 3 resume: correct the stale wrench-specific view path to Systems/Combat/MeleeSwingView.cs under the already human-approved shared generic swing refinement. No behavior, dependency or other step changes.

Human approved checkpoint sec0-combat F1/S23 builder repair at HEAD 658caad57dd32eb9a165f6c4674bdb17a68d336c, its regression/full verification and fresh review. Overview and Phase 2 plan now record this bounded repair; remaining phases unchanged.

Human approved the bounded Phase 3.2 activation-fixture sequencing correction at HEAD 69c1fc7e75f95508692636781c60490c197bb5b1. The overview and Phase 3 revisions above incorporate that scope; other phases/spec unchanged.
