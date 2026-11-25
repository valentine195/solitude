# QRSPI Spec: mechanical-tool-repairs

## Inputs

Paths are relative to `.qrspi/mechanical-tool-repairs/`.

- `request.md` — SHA-256 `1c564872433a4887dd6105e437439106f8a52af205e21d1e85b738eb740670aa`.
- `queries.md` — SHA-256 `c4e839cabbe98678c2f9c6dd4d5bb372ad8350e85bedd90e26552f21cda76f1e`.
- `research.md` — SHA-256 `374db9243ce4e4eb9d084852ff013b2ec4798117e1315f1d7d1dd256b050edc8`.
- `approach.md` — SHA-256 `6b6e8cd6a7a2b15805b68973a134564c30022bdc28cf5b826284fe6e3b38140c`.

Initial revision. Acceptance IDs S1–S20 are newly assigned; none retired. Human selected Option A and carried-tool compatibility. This document is proposed for approval; no implementation or test pass is claimed.

## Behavioral delta

Today the restoration junction consumes one exact inventory item and updates scene-owned restoration state. It has no reusable-tool eligibility check. The existing restoration demo intentionally resets on reload, and current runtime restoration is not connected to saved ship repair records.

The new repair uses the auxiliary power socket, one Battery and the linked door as its authored reference. A compatible mechanical tool carried in inventory or hotbar permits contextual repair. The tool need not be equipped or selected; it is retained. One Battery must be available in player inventory. A Battery held only in hotbar does not satisfy this consumable requirement. This preserves the selected approach's existing part-source boundary.

On successful repair, one Battery is consumed and the session records the completed repair together. The panel displays restored state and its door allows access. The player still uses the existing door interaction to open it. Repair is immediate; there is no timed repair, restart stage, progress minigame or repair-specific movement lock.

Tool compatibility uses hierarchical Tool.Mechanical matching. Other compatible item definitions may satisfy it without wrench-identity checks. Tags determine eligibility; they do not choose which behavior runs. Contextual interaction performs repair, while Primary continues to request the equipped item's existing action.

## Authoring and playable demonstration boundary

This slice provides a reproducible, opt-in production-session fixture containing the selected socket/Battery/door combination and the normal player composition. It exercises durable session behavior against explicitly disposable verification saves. It is a playable demonstration and integration scenario, not an addition to the shipping entry list or a redesigned room. The original ShipSystemRestoration demo retains in-memory storage and reset-on-reload behavior.

Shipping-world placement is not part of this revision. The fixture's physical placement and file location are authoring details for Plan; its observable interaction, production-session ownership and save isolation are requirements here. Existing manually painted layouts and unrelated scene edits must be preserved.

## Interaction and eligibility

The contextual route obeys existing interaction range/focus, actor availability, modal/input policy and scene-lifetime rules. It does not synthesize a weapon Primary request. No additional rule requiring an idle melee runner is introduced.

An eligible tool is a currently available stack whose item capabilities satisfy the configured hierarchical mechanical-tool requirement, in either of the player's two carried containers. Mere world proximity, another container's contents or an invalid/unregistered capability is insufficient. Tool selection does not alter the active hotbar slot, transfer the tool or consume it.

Rejected attempts give a clear reason through existing interaction feedback. Missing tool takes precedence over missing Battery when both are absent. Already repaired takes precedence over resource checks: a completed target reports that state without demanding another tool or Battery. Invalid configuration, stale ownership and busy transactions report unavailable/rejected rather than success.

Examples:

| State at contextual request | Result |
| --- | --- |
| Mechanical tool in hotbar, unselected; two inventory Batteries; unrepaired target | Repair completes; one inventory Battery remains; tool and selection unchanged; door access enabled. |
| Mechanical tool in inventory; one inventory Battery | Same successful repair; no equip step required. |
| Battery available, no compatible carried tool | Tool requirement feedback; no consumption or repair. |
| Tool available, Battery only in hotbar | Inventory Battery requirement feedback; no consumption or repair. |
| Completed target; tool/part later removed | Already-repaired feedback; access remains enabled; no new consumption. |
| Repeated request following successful commit but failed save | Completed in-memory repair remains authoritative; no second Battery consumed. |

## Contracts and durable state

The repair boundary accepts the actual actor/carried-owner context and an authored repair target identity/configuration. It reports committed, already repaired, missing tool, missing part, or unavailable/rejected outcomes through existing interaction-result conventions. It never accepts a stale prompt result as authorization. Requirements are evaluated against current authoritative state at commit.

The session owns the durable repair fact. Consumption and completed repair are one valid published outcome. Observers, save captures and retries cannot observe or serialize a consumed Battery with an unrepaired target, or a repaired target without its required cost. Failable validation must not occur after inventory mutation. Reentry cannot publish a second completion.

A committed repair becomes dirty/urgent for existing save handling. If persistence fails, the complete in-memory outcome is retained, saving remains retryable, and existing save-error feedback must not imply durable success. The live panel/door may display the committed in-memory state. A restart without successful persistence may recover the previous complete snapshot; both the part count and repair fact must belong to that same snapshot. No unconditional rollback of only one side and no repeated charge on a save retry.

Only the selected repair fact is owned by this feature. Existing repair-record storage is reused where compatible; no new top-level save version is required solely for this feature. Missing records mean unrepaired. The completed record represents a fully restored socket, without a separate restart/recovery stage. Other repair, door, discovery, survivor, expedition, collection and starting-loadout facts are preserved. Door access derives from the repair fact; this feature does not add an independent persisted permission, door-animation position or automatically open-door state.

Stable authored identities survive save/load. Duplicate bindings, missing identities and invalid requirement references must reject binding/requests before they can consume items or overwrite another target's progression. Malformed saved state uses existing validation/recovery behavior and must not silently become an empty repair history.

Saved repair facts are reconciled to panel presentation and door access before ordinary interaction becomes ready. Scene unload, reopening, process restart and successor awakening retain completed ship repairs within the same run. New-run reset clears them through the existing new-run boundary; it does not change starting-loadout grant rules.

## Integration and compatibility

Reuse item definitions/capability snapshots, gameplay tags, contextual input/results, carried-container ownership and transactions, ship save records/recovery, and existing restoration/door presentation. No duplicate inventory or save service is introduced. Existing item IDs and asset GUIDs remain stable. Ordinary unconfigured restoration interactions and the existing demo remain usable.

Research reflects the current dirty working tree at HEAD 4c59a8f; it is not a claim that the separate reviewed weapon branch is integrated. Plan must identify the implementation baseline and preserve concurrent inventory/input/scene work. No merging of that branch or completion of its remaining weapon validation is authorized by this Spec.

## Acceptance criteria

| ID | Testable outcome | Expected focused evidence |
| --- | --- | --- |
| S1 | The selected socket/one inventory Battery/linked-door combination is configurable and reachable in the opt-in production-session fixture. | Authored references and actual composed interaction journey. |
| S2 | A compatible tool in either inventory or hotbar qualifies without selection; tool quantity, position and hotbar selection remain unchanged. | Carried-container and unselected-tool cases. |
| S3 | Tool.Mechanical or an authored descendant qualifies hierarchically; an unrelated tool or parent-only Tool assignment does not satisfy the child requirement. A second test-only compatible item works without wrench-specific behavior. | Existing tag-query semantics plus repair eligibility cases. |
| S4 | Exactly one Battery is removed from inventory on first successful repair. Zero inventory Batteries or hotbar-only Batteries reject without mutation. | Cost-source/count cases, preserving other stacks. |
| S5 | Contextual input performs repair without a melee request; Primary remains on the existing equipped action path. Existing interaction policy/focus/availability rules apply. | Actual-input routing and rejected-policy cases. |
| S6 | Missing requirements and already-repaired/unavailable outcomes produce accurate feedback with the stated precedence. Rejection consumes nothing. | Result/prompt cases including both resources missing. |
| S7 | Published part consumption and repair completion are atomic. Observer-triggered capture/reentry cannot observe or produce a partial result. | Existing transaction-boundary assertions and reentrant attempts. |
| S8 | Repeated interaction, stale ownership or changed inventory between prompt and execution cannot consume twice, grant free repair or publish a partial mutation. | Repeat, stale and changed-state cases. |
| S9 | Save failure preserves a complete in-memory result and retry never charges again; successful retry persists that result. Failure is surfaced through existing save feedback. | Injected write failure and retry assertions. |
| S10 | Interrupted durable writes/recovery yield an old or new complete part-count/repair snapshot, never a mixture. | Directly affected disposable-save checkpoint scenarios. |
| S11 | Successful save plus process reload restores completed panel state and door access before ordinary interaction readiness, with the consumed Battery still absent. | Production-session reload/ordering scenario. |
| S12 | Scene departure/return and successor activation within the run preserve repair and do not consume a new part; existing action/input cleanup still holds. | Focused lifecycle scenarios. |
| S13 | Unrepaired targets deny door access; repaired targets permit existing door interaction. Repair alone does not open the door or persist animation progress. | Door permission/opening separation. |
| S14 | Missing repair records load as unrepaired; valid legacy migration preserves existing item/world facts without inventing a repair or refund. Other ship records remain unchanged. | Codec/session compatibility cases. |
| S15 | Target identity is stable; duplicate/missing binding IDs or invalid requirements reject before mutation. Malformed saves follow established recovery/blocking rules. | Authoring/identity and recovery cases. |
| S16 | Starting a new run clears repair through existing reset semantics and preserves once-only starting equipment behavior. | New-run reset with loadout preservation. |
| S17 | The original restoration demo retains disposable storage and reset behavior; ordinary restoration targets keep their existing behavior. | Relevant existing demo/restoration regressions. |
| S18 | The fixture is playable through normal contextual input using disposable saves; no shipping entry list or painted layout is replaced. | Actual-scene journey and source/asset-reference inspection. |
| S19 | Implementation adds no item-name/tag dispatch, duplicate inventory/persistence framework, unrelated UI refactor or speculative tool systems. | Scoped architecture/source review. |
| S20 | Relevant transaction, compatibility, feedback, reload and lifecycle checks are run with exact source evidence; unchanged passing evidence is reused honestly. | Focused results and source snapshots; no blanket full-suite requirement. |

## Verification limits

Reading existing tests is not evidence of passing execution. Use focused Domain/Application, Editor and composed PlayMode checks for the changed boundaries; use only relevant standalone checkpoints where actual durable restart behavior requires them. Record save isolation, source snapshot, actual results, retained-evidence limits and untested behavior. No tests or implementation changes have been performed for this Spec.

## Out of scope

Additional playable tools or panels; new replacement-part content; crafting, durability, repair minigames, timed or interruptible repair actions; background bonuses; general requirement/ability/effect or live-world save-provider frameworks; door-animation persistence; new enemies or combat changes; inventory UI redesign; production room-layout redesign or shipping-world placement; conversion of existing disposable demos to disk persistence; automatic integration of the separate weapon branch.
