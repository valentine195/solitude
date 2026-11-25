# Deferred follow-ups

## Container UI normalization — human-deferred visual finding

Human instruction: "leave this; i want to redesign the container screen to match the rest of the aesthetic during a later UI normalization pass".

Keep the current **Recovered equipment** title for now. It wraps awkwardly into four lines in the existing narrow transfer panel. Address this during a later container-screen redesign aligned with `Assets/_Project/UI/README.md`; do not redesign the screen or shorten the title as part of Survivor Succession. Preserve existing reader/presenter/transfer/modal ownership contracts during that future pass. This is deferred visual work, not a visual-verification pass.

Evidence: `verification/p2-s2-attempt2.md`, source snapshot `verification/p2-s2-attempt2.source.json`, and `Logs/SurvivorSuccession/p2-s2-attempt2/manual-recovery-transfer.png`. The full automated gate passed against the unchanged source.

## Phase 1 F1 / S7 — human-deferred interaction regression coverage

Add a positive PlayMode regression proving a successor immediately focuses and uses an interactable at the awakening location, without leaving and re-entering the sensor. Phase 1 checkpoint at `080fee4b63c335f88faea4360f4a353a92db8063`; review `reviews/phase-1.md`. No current behavior bug was demonstrated. The user explicitly deferred this before Phase 2; it remains future technical debt.
