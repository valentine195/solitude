# QRSPI Spec: Survivor Succession

## Inputs

Exact-byte SHA-256; paths are relative to `/Users/jeremyvalentine/SOLITUDE`.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/survivor-succession/request.md` | `859656bb28771d78e2cd0fe2e86df190dcf127f117ea0e6d3e6ad5d76bf8c9c8` |
| `.qrspi/survivor-succession/queries.md` | `6d76e4a8129179ced840ec22a89a18c6ab47d62e358ffa24df2b093dad00f9b9` |
| `.qrspi/survivor-succession/research.md` | `391048ad286c52b5522ad36720addd3bde651bab22556be95afaf90459c313ce` |
| `.qrspi/survivor-succession/approach.md` | `2e35ba83cd0b50dbbc4eea1ca0d4e5cf90652b71bf46f329713f7b180dfb898e` |

The human selected Option A against approach revision `f7e4f5a2e9cfc73fb9825093f34238847dfb509f964d3c0f7d47359d724192ed`; the input above includes the recorded decision. Initial spec: criteria S1–S12 are new; none are changed or retired. Provenance does not constitute spec approval.

## Behavioral Delta

**Before:** one authored player has no survivor identity or death workflow. Inventory/hotbar contents use fixed player owner IDs. Ship outcomes and AI first-contact state belong to live scene objects and reset on scene reload.

**After:** a scene configured for succession has an active survivor identity distinct from its retained player avatar. The initial survivor awakens at an authored cryobay location. A debug death permanently retires that survivor, leaves their carried items at the death location, and automatically awakens a distinct successor using the same avatar. The scene and its ship objects remain live throughout.

New survivors start with empty inventory and hotbar, the scene’s authored awakening vital baseline (default full health and oxygen), neutral movement, and fresh hotbar selection. Previous interaction focus, open screens, and pending item gestures cannot act on behalf of the successor. Capacity and ordinary item/interaction rules remain unchanged.

Death preserves every inventory and hotbar item in one independently owned recovery container at the death location. Subsequent survivors can use normal container interaction to recover some or all contents. Capacity failures leave uncollected items in recovery ownership. Empty deaths need no recovery object. Multiple nonempty deaths leave independent containers; recovery does not depend on the deceased being active.

Example: A dies with two repair fuses in inventory and one tool in the hotbar after repairing a system. B awakens with neither item, sees the system still repaired, and recovers those exact quantities from A’s death location. If B lacks capacity, the remaining items stay there. A later death does not erase A’s remaining cache.

The AI retains its scene-lifetime continuity and displays one brief visible acknowledgment for each successful succession. Existing first-contact knowledge is not reset or replayed by succession.

## Contracts and State Ownership

- **Survivor lifecycle:** exposes the current identity and active/dead status, accepts a debug death directed at that identity, and reports transition completion or failure. A completed death is irreversible. Repeated or stale requests for the same survivor cannot retire the successor or create duplicate recovery contents.
- **Successor choice:** automatic initially. Choosing a successor remains distinct from retiring a survivor and activating the avatar, so future roster selection or character attributes need not change ship systems. No roster or selection UI is required now.
- **Character-owned state:** identity, life status, vital baseline, carried contents, selection and transient player actions belong to the active survivor. Reusing the avatar must not revive or reuse the deceased identity. Identity/status must be inspectable for verification; a character sheet is not required.
- **Recovery ownership:** item identities and quantities, source survivor identity, death location and remaining contents belong to each recovery record/container. Contents are conserved across the complete handoff, including inventory and hotbar together. A rejected recovery transfer changes neither side.
- **Ship ownership:** repairs, system activation/recovery, door state, locker contents, collection facts and AI continuity are unaffected by survivor retirement. Ship-system state and rules must not depend on survivor identities, death handling, or successor choice.
- **Compatibility boundary:** existing fixed player container IDs continue to identify active-player access, not a permanent survivor. Existing consumers must see the successor’s empty contents without stale actions or duplicate owner registration. No global save reset or scene reload is part of death.
- **Failure:** missing awakening/recovery prerequisites reject completion without losing or duplicating items or making a successor active. An interrupted handoff keeps gameplay gated until consistent ownership and one active survivor can be established; retries cannot repeat committed transfers. A failed transition produces diagnosable failure information, not a success acknowledgment.

No durable save-schema change is included. Recovery locations, survivor records and ship restoration across scene unload or process restart require separate persistence work. Existing container serialization alone does not provide that guarantee.

## Integration and Preserved Behavior

Use existing Domain/Application/runtime ownership and scene composition boundaries. The retained avatar continues to serve input, interaction, camera, vital views and container presentation. Recovery uses existing container transfer behavior. AI acknowledgment uses existing visible feedback. Scenes opt into succession through authored awakening configuration; unrelated scenes retain their current behavior.

Wakeup supplies the player acceptance scenario. Air Scrubber/restoration and powered-door scenes remain compatible, including progression, repair consumption and door traversal after succession. World systems retain state through the transition and continue functioning afterward. Existing pickup collection, lockers, inventory capacity, hotbar and production save/recovery behavior remain valid outside the newly introduced runtime-only succession state.

## Acceptance Criteria

| ID | Observable requirement |
| --- | --- |
| S1 | In a configured scene, survivor A has a distinct inspectable active identity and begins at the authored cryobay awakening location. |
| S2 | Debug death targeting A retires A permanently and automatically activates one distinct B. Repeated or delayed A requests create no extra successor/cache and do not kill B. |
| S3 | B has empty inventory and hotbar, authored awakening vitals and fresh selection; A’s transient state does not carry over. |
| S4 | A’s inventory and hotbar items appear at A’s death location under independent recovery ownership with exact identities/quantities; none remain carried by B or are duplicated/lost. |
| S5 | B recovers items through ordinary interaction. Partial/full-capacity and repeated recovery attempts preserve all remaining quantities and ordinary transfer rules. |
| S6 | A’s repairs, activated systems, powered/unlocked doors, scrubber progress, lockers and collected pickup facts remain unchanged by death; normal world progression continues afterward without scene reload or save reset. |
| S7 | After succession, movement, camera, vitals display, focus and interaction serve B normally. Death during held movement, an open screen or an item gesture leaves no stale action affecting B or recovery contents. |
| S8 | Successful succession displays one brief visible AI acknowledgment. Prior first-contact knowledge persists, and failed/repeated requests do not emit duplicate success acknowledgments. |
| S9 | Across A→B→C, identities remain distinct and dead survivors remain retired; independent recovery containers from earlier deaths remain recoverable. An empty death creates no items. |
| S10 | Invalid awakening/recovery configuration or an injected handoff failure cannot lose/duplicate equipment, activate a partial successor, or leave two active survivors; retry preserves already committed ownership. |
| S11 | Survivor state/choice is independent of ship state. Ship features require no dependency on succession or a roster; existing authored player bindings and assembly boundaries remain valid. |
| S12 | Existing inventory/hotbar, pickup, locker, restoration, scrubber, powered-door and production save/recovery regression checks continue to satisfy their established behavior. |

## Verification Expectations

- **Domain/Application:** S2–S5, S9–S10: identity/life transitions, equipment conservation across both containers, rejected/stale requests, capacity limits, repeated deaths and failure/retry cases.
- **EditMode/authoring and dependency checks:** S1, S4, S10–S11: valid awakening/recovery authoring, independent ownership and preserved assembly/system boundaries.
- **PlayMode:** S1–S10: acceptance journey, repeated succession, held input/open-screen/gesture cases, empty deaths, AI feedback and retained restoration/scrubber/door behavior in actual composed scenes.
- **Regression qualification:** S12 and the affected portions of S6–S7: relevant existing tests plus the complete isolated `python3 Tools/Verification/verify.py` gate from the repository root. Require successful `results.json`; `--skip-build` is partial evidence. The current gate qualifies macOS ARM64/Wakeup and does not replace additional succession and demo checks.
- **Manual Unity observation:** S1, S4–S8: clear cryobay placement, visible recovery interaction/AI message, camera/vitals presentation and normal traversal after death.

All checks are proposed. No implementation or verification has run for this spec.

## Out of Scope and Follow-up

Scene-transition succession, full save/load persistence of survivors/recovery locations/ship modifications, combat/enemy AI, procedural generation, randomized traits/classes, skills/injuries/progression, a selectable surviving roster, expedition management, elaborate death visuals and narrative sequences. No unrelated system redesign or manager hierarchy is authorized.

## Open Questions

None.
