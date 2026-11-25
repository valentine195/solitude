# Plan revision 3 — candidate for human approval

Status: Draft. Approval is pending for this exact normalized bundle; this manifest is not approval evidence. Spec revision 2 is approved at `5e259da45c328e87a404d2cbcc83da875d486a611823ef1760ae03865685cf71`.

Inserts one five-step Phase 2a before artwork integration. Phase 1 and Phase 2 files and completed markers are unchanged. Unfinished Phase 3 consumes the expanded geometry/result/UX contracts and adds production concave/partial-preview evidence. All active criteria S1–S29 are mapped; prior evidence is historical for its recorded target.

Normalization: only status-marker tokens on marker lines and overview Status cells become [ ]; all other UTF-8 content is significant. Bundle hashes path + NUL + normalized bytes + NUL in the table order. Raw hashes describe this drafting snapshot; status-only progress may change them without changing normalized approval revisions.

| Path | Raw SHA-256 at drafting | Normalized SHA-256 |
|---|---|---|
| `.qrspi/floor-driven-room-authoring/plan.md` | `8be517489c3d8ef61692c6d3d76654d8fd69e735528f40ffca746cc434366584` | `fb58d8d2442d22734ebf7198e178fed751d968d1ec8a7f26cde12ab2e1eae446` |
| `.qrspi/floor-driven-room-authoring/plans/plan-phase-1.md` | `60237a2fc8de0a85ba0ee9b5cb7799df53439ce99931931531c2465dee2cc79e` | `828428714b2516021c7f3158f660d4535b56b840dd9245e4fc76092f3b5a145c` |
| `.qrspi/floor-driven-room-authoring/plans/plan-phase-2.md` | `1ec659b5bd8edfde28e60d74c37910b96b7580bb1794e9c13871369197e5e899` | `75acbf25e24d49c9fc1dda5f936b6ff40207f9cb7b9b1dc395fb20363859c178` |
| `.qrspi/floor-driven-room-authoring/plans/plan-phase-2a.md` | `7aa8037f0346b90520106b9db3f915ca6e6dd9c09f1efa77636c49d35eca0fba` | `7aa8037f0346b90520106b9db3f915ca6e6dd9c09f1efa77636c49d35eca0fba` |
| `.qrspi/floor-driven-room-authoring/plans/plan-phase-3.md` | `63128e7cf432fdee0066e89acbcca5227efa5526334ae20c80aa77bb5d0d59ef` | `63128e7cf432fdee0066e89acbcca5227efa5526334ae20c80aa77bb5d0d59ef` |

Candidate normalized bundle SHA-256: `cf6677e80ed013e888b39a0722aa3edb3c2924075dd3fc0582c9603f06d01c72`.

Verification: exact Spec/Research/Approach hashes, completed-phase byte preservation, dependency references/order/acyclicity, step markers and S1–S29 coverage checked by `verification/check_plan_r3_candidate.py`. No implementation, tests, builds or renders were run by planning. Existing `check_approved_inputs.py` still targets historical approved plan revision 2 and must not be used as proof of this candidate's approval. On human approval, record the exact bundle through the helper and update the active approval checker without rewriting historical manifests.
