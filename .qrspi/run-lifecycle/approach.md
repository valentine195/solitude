# QRSPI Approach: run-lifecycle

## Context

The slice needs repeatable death and awakening, world refresh between expeditions, and ship progress that survives both death and application restart. Human clarifications override the initial brief: new colonists receive nothing; there is no expedition-completion event in this slice; replenishment is limited to bandages and rations, roughly 1–2; ship progress must persist to disk. Corpse retrieval, territory control, dynamic repopulation, and a generalized roguelite framework remain outside scope.

Existing succession already retains the avatar, resets authored vitals, clears carried ownership by equipment handoff, and handles retries. Existing production persistence retains equipment, lockers, and collected pickups. The missing connection is a coherent lifetime boundary: lethal vitals do not request succession; ship repairs and doors are scene-local; expedition identity and defeated-enemy state do not exist in the save model.

## Constraints and Facts

- The bootstrap survives scene loads and owns the saved session. It supports one composed player scene and rejects mixing disposable demos with production (`Assets/_Project/Core/Composition/GameCompositionRoot.cs:48,79,125–134`). Reusing that lifetime is compatible with the current architecture; turning a demo into production requires explicit authoring rather than assuming its persistence behavior changes automatically.
- Succession is currently scene-owned and constructs a fresh identity during initialization. Awakening preserves the avatar and resets vitals. Its staged coordinator guards duplicate death and resumes committed work (`Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:44–55,127`; `Assets/_Project/Application/Survivors/SurvivorSuccessionCoordinator.cs:34,59–110`). A scene rebind currently changes colonist identity independently of death.
- Version 2 saves contain container records and collected pickup IDs, without repair, door, colonist, expedition, or NPC records (`Assets/_Project/Domain/SaveGameData.cs:9–14`). Session capture and cloning explicitly copy those fields; new state must participate throughout persistence, including validation and recovery (`Assets/_Project/Domain/ContainerSaveSession.cs:78,113`). The existing durable save/backup path can be reused (research, save mechanisms answer).
- Saved pickup facts prevent arbitrary recollection. Saved lockers do not reroll initial loot. Full session reset clears these facts and is used by new-save/demo flows (`Assets/_Project/Domain/ContainerSaveSession.cs:36–44,91`; research, collected supplies answer). That reset is unsuitable for ordinary death because it erases persistent world progress.
- Repair item removal and the scene-local repair fact commit together before notification (`Assets/_Project/Domain/Restoration/RestorableSystemState.cs:17–35`). Preserving this relationship in the saved snapshot matters: consumed components paired with an unsaved repair can softlock progression. Scrubber installation, running, and recovery are separate state dimensions (`Assets/_Project/Domain/Restoration/ScrubberEncounterState.cs:8–36`).
- Current corpse caches are unsaved, destroyed on scene release, and receive all carried items (`Assets/_Project/Features/Player/Succession/SurvivorSuccessionComposition.cs:81–100,150`). They cannot be the critical-component guarantee and should not become a new dependency of this slice.
- Research found no defeated-enemy implementation, authored bandage/ration definitions, production scene transition caller, or separately identifiable maintenance area. SEC-0 currently patrols and stuns; the scrubber is a disposable demo. These are real slice/content gaps, not existing capabilities to wire together (research, enemy, supplies, owners, and entry-point answers).

## Candidate Approaches

### Option A: Persistent session state with selective expedition refresh

Keep the existing bootstrap/session as the authoritative lifetime above scenes. Separate saved ship facts, current expedition facts, and current colonist ownership there; scene components bind to that state rather than create new lifecycle identities whenever loaded. Retain the existing avatar awakening and coordinator's transition/retry pattern. A small application-level transition owner arbitrates death and scene loading; scene adapters handle presentation and reset behavior.

Death replaces the colonist and advances the expedition once. Refresh only authored hostile encounters and designated supply sources, while preserving ordinary world containers, discoveries, repairs, and shortcut facts. Stable object identities and expedition collection/defeat records prevent scene revisits from being treated as new expeditions. There is no completion trigger now, but the refresh boundary can distinguish its future cause without implementing another loop.

For critical components, prefer a narrowly authored recoverable source tied to its unfinished objective: make a lost component obtainable again when no accessible saved owner retains it, and stop replenishing after permanent consumption/progress. This avoids corpse persistence and duplication from blindly restoring every critical pickup. Newly awakened equipment stays empty; ordinary carried equipment is lost in the production policy, while existing recovery behavior can remain isolated to its established demonstrations.

Strengths: best reuse of same-scene succession; preserves live readers and ship presentations; avoids unnecessary scene teardown; makes lifetime ownership explicit. Costs: each eligible feature needs an explicit reset/restore adapter, and the transition commit must join equipment loss, successor identity, expedition advancement, critical-source reconciliation, and saving coherently. Retry or restart must not repeat supply grants or restore the departed colonist's gear. Scene binding must restore current colonist state rather than call fresh awakening on every revisit.

Verification focus: duplicate lethal notifications, failure after transition commit, quit/reopen around death, saved repair consumption, critical components held in lockers, and repeated scene revisits. Save migration must preserve known inventory/collection records and default new facts conservatively; old saves cannot reconstruct repairs that were never recorded.

### Option B: Rebuild the scene at each death, overlay persistent progress

On death, commit a new colonist/expedition snapshot and load the cryobay scene through the existing root. Reconstruct authored enemies and supplies naturally, then apply permanent repairs, doors, collection exceptions, and current expedition facts from the saved session. Ordinary scene travel still needs expedition records; reloading alone cannot distinguish travel from death.

Strengths: scene reconstruction resets transient AI, animation, and interaction state consistently; fewer in-place refresh adapters; useful if production play will already traverse separate scenes. Costs: current succession's same-scene coordinator, awakening reference, recovery ownership, and receipts cannot simply survive teardown. Transition authority must move above scenes and handle partially loaded/rebound successors. Persistent ship state and critical-item policies are still required, so reload removes only part of Option A's work.

Failure modes include old owners capturing departing equipment during release, loading failure after death commit, and unlocking gameplay before all saved facts are restored. Verification needs interrupted unload/load and owner-disposal coverage in addition to Option A's persistence cases. Save migration is substantially the same; rollback to older code after a schema change needs an explicitly supported compatibility policy rather than assuming the backup makes downgrades safe.

## Recommendation

Recommend **Option A**. The retained-avatar succession and persistent bootstrap already provide its core lifetimes. Selective refresh adds the missing ownership boundaries without making every death a scene-loading transaction. Use explicit records and adapters for this authored slice, avoiding a generic reset registry, territory model, or universal entity snapshot.

Persist permanent repair and shortcut facts alongside existing containers, and treat ordinary scene travel as continuation of the same colonist and expedition. Reuse the durable save path with a coherent snapshot and forward recovery for interrupted death transitions. A distinct transition cause preserves room for future expedition completion, while only death is active now.

Prefer one authored bandage and one ration per expedition as the conservative interpretation to confirm in Spec. Prefer resuming the living colonist and current expedition on reopening, with their equipment and vitals, so restart does not silently refresh hostile areas or combine a new identity with old equipment. Exact resume placement remains a behavioral decision.

The architecture recommendation does not imply a defeat-capable enemy or connected production route already exists. Their minimum playable scope must be explicit before the implementation plan. No tests were run during shaping.

## Decision

Option A selected by the human: "Proceed with A". Use persistent session state with selective expedition refresh and retained-avatar succession, reusing the existing bootstrap and awakening lifecycle while adding explicit state lifetimes. The human subsequently selected "Saved location" for reopening. Resume the living colonist and expedition at the saved location with retained vitals and equipment. Carry the original playable acceptance scope and Option A equipment-loss/critical-source policies into the specification. One bandage plus one ration is the proposed concrete supply budget for Spec review; the minimum enemy and route scope must also be explicit in Spec.

## Open Questions

None blocking Spec drafting. The specification approval gate will review the concrete minimal enemy/route scope and one-bandage/one-ration supply budget alongside the selected Option A and saved-location resume behavior.
