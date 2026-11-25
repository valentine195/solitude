# Phase 3 manual-check override

Human explicitly instructed "Override and move to phase 3" after being asked to decide whether to waive Phase 3 manual sound, layout, and end-to-end checks. This authorizes phase execution through Steps 1–3, accepting automated evidence for those checks; manual checks remain waived and unrun, not passed. Full automated regression/player-build qualification and final independent Review are not waived.

Step 1 committed as `de184ac1ec0d5dce54cce9635902c60401a52946`; source matches `phase3-step1-source-snapshot.json`; 65 pure and 5 composed scene tests passed (see `phase-3-step-1-attempt-1.md`). This resolves the historical blocker in that attempt.

Spec SHA-256 `b487402c716f8e7d191e6cac3b4624368e3a3376dccadb7cd495c683e25b012f`; normalized Phase 3 plan SHA-256 `78a395ddb462f2468b87505cc01445e6ae509ccbb65d5db0af3130486d1c69b7`.
