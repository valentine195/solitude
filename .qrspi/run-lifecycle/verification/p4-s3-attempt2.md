# Phase 4 Step 3 — full gate attempt 2 (FAILED)

Spec SHA-256: `a9dc706c5f1628759fbfaed64ffc89258aeaead70a1d24987b9b4ce6b9882922`.
Approved complete plans: `p4-s3-repair-approved-plans.json`. Phase 4 normalized revision: `950ac227e506bf44556612ffa4578958b6dc24838361c44937a40861dc59a1ff`.
Exact source: `p4-s3-attempt2-source.json`, base `6e42cdc02d46bd298125ef9f2462207d3da468af`. All 14 changed/untracked file hashes unchanged after gate. No Step 3 staging or commits.

Command: `python3 Tools/Verification/verify.py --workspace /private/tmp/solitude-run-lifecycle-p4-s1 --output /private/tmp/solitude-run-lifecycle-p4-s3-attempt2`.
Expected: full gate PASS. Actual: pure252/252, authoring PASS, EditMode376/376, PlayMode153/154. Builds/process scenarios not run because gate stopped at PlayMode.

The approved socket selection, disposable isolation and UI-submit repairs passed. S2/S15 and integrated S4/S6/S9/S10: the input journey repaired the scrubber, opened the shortcut, disabled SEC0, died from ordinary conduit damage and observed selective refresh, persistence after scene revisit, and empty successor equipment. Installed fuse stays suppressed (S8). Two generated world-camera PNGs exist under `p4-s3-attempt2/playmode-saves/`; restored-scrubber PNG was visually inspected. Captures exclude overlay UI and temporarily disable PixelPerfectCamera for render export; full visual qualification remains later work.

## Remaining failure and corrected diagnosis

`LostCarriedFuseReturnsButAReachableStoredFusePreventsReplacement` passes death and lost-carried-fuse reacquisition, then fails its focus assertion. Focus is the existing PF_Door. The prior diagnosis incorrectly treated the existing door at (2,4) as the locker. The actual LockerContainer and Locker interaction are at (-6,4); its sensor center is (-6,4), radius1. LockerContainer itself is not IInteractable, so the expected focus must be its separate Locker component.

Proposed test-only repair: travel via (-2,3) along the existing upper passage, then approach the actual locker at its position+(1.2,-1), approximately(-4.8,3). This approaches its east sensor edge while avoiding the nearby scrubber's sensor. Assert focus is locker.GetComponent<Locker>(), then E and preserve every storage/death/no-duplication assertion. Ordinary focus/movement remain mandatory; no teleport, direct Open, disabled collision, or forced registration. Approach is source-derived and still requires runtime verification.

Exact proposed patch: `p4-s3-locker-repair-proposal.patch`. No plan amendment is needed; only the already-scoped journey test changes. Patch SHA-256: fb1e2c456e7dd23b28d93fa5a0fb12f27ed9dc0b1afe7ad3a8bb75b830651278.

On approval, apply the patch, capture a new source snapshot, rerun full gate, inspect screenshots, and commit only after PASS. All source is preserved uncommitted; step marked [!].

QRSPI Implement requires: “In every mode, stop immediately and wait for the human when a step fails, a blocker appears, or a step can't be done as written.”
