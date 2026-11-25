# QRSPI Approach: mechanical-tool-repairs

## Context

The request makes tools useful through contextual repair while retaining the wrench's primary melee action. The selected reference combination is the auxiliary power socket, one Battery, and its linked restoration-gated door. Success restores access; the existing door interaction can still open the door separately. Tool compatibility uses gameplay tags, and the tool is retained. Repair progress must survive saving/loading.

`background.md` records human selection of this reference combination, not selection of an implementation approach or permission to change the demo's disposable lifecycle. Research describes dirty working-tree source at HEAD `4c59a8f42d15f440a46449c10658b31b39f2c971`; this comparison includes the current source inspected during Shape. No tests were run.

## Constraints and Facts

Paths below are relative to `Assets/_Project/` unless otherwise stated.

- The wrench already has Tool.Mechanical and a Primary melee action; the starting loadout puts it in the hotbar (`Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset:15-34`, `Features/Items/Loadouts/MaintenanceStartingLoadout.asset:15-18`; research). Checking only player inventory would miss the starting tool.
- Hierarchical gameplay-tag matching supports children satisfying parent requirements; capability snapshots already exist (`Domain/GameplayTags/GameplayTagSet.cs:18-49`, `Features/Items/Database/ItemCapabilityCatalog.cs:37-63`; research). Compatibility need not depend on the wrench's item identity.
- Junctions currently receive only inventory and commands, before hotbar initialization. Scene features subsequently receive inventory, hotbar, capabilities, feedback and a production-only lifecycle session (`Core/Composition/SceneBindings.cs:156-201`, `Core/Composition/SceneFeatureContext.cs`). Existing feature composition is a suitable boundary for repair binding without routing repair through Primary.
- Restoration currently removes one exact item and commits its scene-owned boolean before inventory observers. It guards reentry and repeated success (`Domain/Restoration/RestorableSystemState.cs:16-40`). The door gate reconciles immediately and on state changes (`Features/World/Restoration/RestorationDoorGate.cs:14-38`).
- The existing save schema already contains repair records and door records, with identity and consistency validation (`Domain/Expeditions/ExpeditionState.cs:40-107`). Research found no runtime restoration capture/reapplication binding. `StoreShipState` rejects busy commands; capture is also prohibited during commands (`Domain/ContainerSaveSession.cs:97-129`). Calling that public method in the current removal callback would fail after inventory mutation, rather than establish a safe transaction.
- A session-owned world/inventory commit precedent already exists for starting-loadout initialization and pickup collection (`Domain/ContainerSaveSession.cs:46-68`). Command commit callbacks run before command/slot publication (`Domain/ContainerCommandService.cs:366-375`). Such callbacks must contain trusted, prevalidated writes, not failable validation or public save calls.
- The demo resets its session on player-scene binding, and production/demo sessions cannot mix (`Core/Composition/GameCompositionRoot.cs:184-194`). Its lifecycle tests deliberately expect reset. Repository `README.md` keeps demos nonshipping, source within its owners, and review outputs outside Assets.

## Candidate Approaches

### Option A: Session-owned persistent repair, existing scene presentation

Make the durable repair fact belong to the production save session, with the existing restoration object and gate presenting that fact. Keep repair-specific requirement evaluation small: inspect live carried inventory and hotbar capabilities for a compatible reusable tool, and retain the existing inventory-only exact Battery requirement. This possession interpretation fits “has the necessary tool” and the starting loadout without requiring hotbar selection.

The session coordinates the inventory decrement with a prevalidated repair-record change at the existing command commit boundary. Scene binding reapplies the durable repair state before gameplay input becomes available. Door permission derives from repaired state, avoiding a second independently writable access flag merely for this feature. This does not require persisting door animation progress or adding a restart/recovery mechanic.

Strengths: one durable authority survives scene destruction; the saved Battery count and repair fact share a publication boundary; existing tags, interaction feedback, door gate and persistence format remain useful. It resembles existing session-owned pickup/loadout transactions rather than introducing a general requirement framework.

Costs and risks: the session gains a narrow repair workflow and restoration needs a loaded-state presentation path. Validation must finish before commit; copying the current public StoreShipState call into a callback is unsafe. Stable authored identity, duplicate binding rejection, missing-record defaults and preserving unrelated ship records need careful treatment. Scene views must not become competing authorities.

Verification emphasis: focused transaction/reentry and failure assertions, capability checks across the two carried containers, and a production-session save/load and scene-return scenario. Existing melee tests remain evidence only for their unchanged source snapshot. No full-suite run is implied.

Migration/rollback: reuse the existing schema where its semantics fit, treating absent repair records as unrepaired. Preserve authored IDs once saves exist. Reverting presentation must not silently erase existing ship records or reinterpret consumed Batteries.

### Option B: Scene-owned restoration with registered save bindings

Keep RestorableSystemState authoritative while its scene is alive. Add a narrow session registration/capture/reapplication mechanism for restoration objects, analogous to container bindings. A bound object's state contributes its repair record to every coherent save capture and retains that record on unbind. Tool checking remains the same small carried-item capability check as Option A.

Strengths: preserves the existing restoration transaction and event model more directly. The same binding concept could support several scene-owned systems later without moving each workflow into the save session.

Costs and risks: registration, capture, teardown and reload now become correctness-critical. The current save session captures only stored ship facts and live containers, so this is new lifecycle infrastructure, not a component that can simply call StoreShipState in Changed. Save capture must see live repair state whenever it sees the consumed Battery, including observer-triggered attempts and scene transitions. An unload before capture, duplicate registrations or stale teardown could otherwise lose or overwrite progress. Saved and live representations require explicit ownership rules.

Verification emphasis: Option A's focused behavioral scenarios plus registration ordering, capture during/after publication, unbinding, and scene destruction. More lifecycle cases are required even for one panel.

Migration/rollback: the same existing repair schema can be retained, but every live binding must preserve its last committed fact when removed. Removing the binding infrastructure risks dropping progress that exists only in scene objects.

## Recommendation

Recommend **Option A**. For one panel, a session-owned repair fact is the smaller durable design: it uses an existing transaction precedent and avoids introducing a registry of live world-state providers. Option B is reasonable if several scene-owned persistent systems are an immediate requirement; that broader infrastructure is not established by this brief.

Use the selected socket/Battery/door combination in production-session wiring or a focused production-session verification fixture while retaining the existing demo's reset behavior. The exact scene placement is a subsequent design choice, not evidence that the demo already persists. Neither option calls for rebuilding a room, overwriting painted layouts, or converting the demo to disk persistence.

Prefer a carried compatible tool in inventory or hotbar, independent of selection, and hierarchical Tool.Mechanical matching. These are recommendations for human selection, not established existing repair behavior. Both options retain contextual interaction feedback and leave the wrench's Primary action on its current path.

## Decision

Human approved Option A: session-owned persistent repair with existing scene presentation, and compatible tools carried in either inventory or hotbar without requiring selection. The selected reference is the auxiliary power socket, one inventory Battery, and its linked door. The rationale is one durable authority and coherent part consumption/repair publication, avoiding an additional live-world save-binding registry. Existing disposable demo reset behavior is preserved. Production-session demonstration/placement is specified at the Spec gate; no implementation is authorized by this decision.

## Open Questions

No source-evidence gap currently requires a Research return. Production scene placement and approval of the recommended carried-tool interpretation remain design choices for the human/Spec stage; they must not be presented as facts already determined by Research.
