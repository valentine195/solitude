# Task journal

Status: Draft for human review. Initial formal QRSPI Spec; no implementation or test qualification is claimed.

## Inputs

Paths are relative to the repository root `/Users/jeremyvalentine/SOLITUDE`, except absolute proposal paths. SHA-256 records exact bytes used, not approval.

| Input | SHA-256 |
| --- | --- |
| `.qrspi/task-journal/request.md` | `3c42a6d8bb393ec7b205526ea77414cceb9c5218043068446740dc63fdbadba9` |
| `.qrspi/task-journal/queries.md` | `69041f2d27cadb179fc20d7c8c96062bdbddd3783ea9fdf6807f7f038a999662` |
| `.qrspi/task-journal/research.md` | `564cb9591bbfbf900ff92d2081d07c277fe0cbf11775061724b6f50918fa971b` |
| Earlier standalone proposal: `/private/tmp/solitude-task-journal-spec.md` | `cf86cf85b24e6b6b77814f0b97cf6932bddee9331ae82653c4d1cfb0a6695dac` |
| Project UI guide: `Assets/_Project/UI/README.md` | `a87fa092e17bc433dbb3958e6d9b6dc849a2f886e5cd7ce50f254c997a0d3305` |
| Spec mockup: `.qrspi/task-journal/design/task-journal-pinned-active.png` | `7948d782ab5bb1b155cfb296abcc70ad685ffdca5a3a07a357af6fa59b238cd6` |

No `approach.md` exists. Shape is skipped because the requested progression, source contract proposal, and researched boundaries support defining the delta directly. The user authorized use of the earlier spec and mockups during Research; its input table records all three earlier image revisions. Their absence of pinning does not override subsequent human choices.

Mockup generation prompt: [design/task-journal-pinned-active-prompt.txt](design/task-journal-pinned-active-prompt.txt). Generated with the built-in image tool using the earlier Active mockup as its edit target. Prompt SHA-256: `26a1e868e53f16d92c7af8c3882da6908ed7cd2119270c2728853dd751d3b9f8`.

## Revision summary

This is the first formal spec. S1–S13 retain the corresponding requirements from the earlier standalone proposal, with refinements below. Added S14 (pinning), S15 (failure/abandonment and task-specific stages), S16 (automatic discovery), and S17 (visual handoff). No IDs are retired. The earlier proposal was not an approved QRSPI spec.

Changes from that proposal: pinning is included; task-specific stages and terminal failure/abandonment are supported; failed tasks join Completed and abandoned tasks disappear; scrubber discovery can occur automatically or on successful access; the demonstration scene is explicitly named. Journal-only single-task pinning, keyboard defaults, journal pause and scene-scoped retention are draft product decisions made concrete here for review.

## Scope and current behavior

Before: the atmospheric scrubber derives its current objective from equipment state, displays it in its equipment screen, and briefly announces completion. There is no shared task journal, task instance registry, pinning, or durable task history in the searched first-party code. The equipment screen blocks gameplay input without pausing simulation. The interaction Triggered event can follow a Blocked result. Production saves exclude scrubber progress.

After: discovered tasks remain readable while their source scope exists. Gameplay systems can contribute/update their own tasks independently of whether the journal is open. The player can browse Active and Completed, inspect a task, and pin/unpin one active task. Automatic and player-initiated discovery are supported. The first playable integration is `Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity`, using its existing scrubber, inventory and repair journey. The owning scene builder must preserve that integration when regenerated. Wakeup and shipping scene lists are unchanged.

The journal observes confirmed source state and never performs repairs, grants rewards, or derives completion from inventory possession alone.

## Behavioral changes

### Discovery and progression

Loading/binding a source may register undiscovered records. Undiscovered records are absent from lists, counts, pinning, and player notifications. Discovery is irreversible for an occurrence and exposes its latest state, not a replay of every earlier stage.

A task can be Active, Completed, Failed, or Abandoned. Active entries occupy Active. Completed and Failed entries occupy Completed, labeled distinctly COMPLETED and FAILED. Abandoned entries are removed from player-facing state, counts, pinning and pending notices. Abandonment is source-controlled; there is no player Abandon action in this slice.

A source supplies its own stage key, current instruction and condition. Stages may differ between tasks; the journal does not impose the scrubber's sequence on other contributors. Active stage changes may reflect the source's current valid state, including a changed instruction or blocker. Stage presentation is a current snapshot rather than a required checklist/history.

Completed and Failed are terminal for an occurrence. They cannot reopen or become abandoned. Abandoned is also terminal; publishing the same identity cannot rediscover it. A new occurrence requires a distinct task key or a new source scope. Terminal metadata can be corrected without changing outcome or replaying terminal notifications.

### Pinning

At most one discovered Active task is pinned. Pinning promotes it to the first Active row and adds a pin icon plus PINNED text. Pinning another active task transfers the pin atomically. Unpin restores first-discovery ordering without changing selection, task progression or condition.

Pin state survives close/reopen within the scope lifetime. Completion, failure, abandonment or scope removal automatically clears that task's pin. Completed/Failed/hidden/removed tasks cannot be pinned. Temporary source detachment does not clear a pin; it is a presentation preference and can still be toggled while the last confirmed active record remains.

Pinning affects only the journal in this slice. It adds no permanent HUD tracker, world marker or route. This preserves the UI guide's exploration readouts. The mockup demonstrates the already-pinned state; the selected Active task's control reads PIN TASK when unpinned and UNPIN TASK when pinned.

### Lifetime and persistence

Records belong to explicit source scopes. The scrubber scope is its scene lifetime. Closing UI retains records; disabling a source detaches its writer and marks its remaining records SOURCE UNAVAILABLE without completing them. Reattaching reconciles authoritative source state. Releasing the owning scope removes its records, registrations, pin, selection references, and notices.

Scene reload/re-entry reconstructs the scrubber's tasks from fresh scene state. New game/session disposal clears all journal state. There is no disk save or journal migration in this slice. Completed history is available within the owning scope, not across app restarts or scene resets. Demo sessions and production sessions remain isolated. Saving a journal completion independently of unsaved repair state is outside scope.

### Notifications

One journal-owned lane presents TASK ADDED, TASK COMPLETE and TASK FAILED with task/location text and a labeled outcome. Discovery and observed Active → Completed/Failed transitions produce notices; ordinary stage, condition, blocker and pin changes produce none. First discovery of an already terminal task produces one added notice with its current outcome, not a fabricated active phase or second terminal notice.

Notices never steal focus. They wait while exploration is blocked by a primary interface or pause. Visible dwell is provisionally four seconds of unpaused exploration, excluding hidden time. A visible notice hides when exploration ends and resumes remaining dwell afterward. FIFO follows accepted visible changes. A pending added notice replaced by terminal progress for the same task coalesces into one latest-outcome notice. Scope removal/abandonment drops that task's notices; late UI subscriptions do not replay expired notices.

The scrubber's former completion presentation is superseded in the integrated scene so completion appears once. Generic interaction feedback remains independent. The notification lane respects the UI guide's placement and leaves the current interaction prompt readable.

## Contribution API contracts

The following names describe observable operations; concrete type layout is a Plan concern.

| Operation | Required observable contract |
| --- | --- |
| RegisterSource(source identity, scope) | Returns a source-qualified publisher; only one live registration can own an identity. Duplicate ownership yields Conflict without replacement. |
| Publish(publisher, local task identity, full snapshot) | Inserts or updates that source's task atomically and returns Applied, Unchanged, Invalid, Conflict, Unavailable, or Busy. Accepted writes are independent of open views. |
| ReadSnapshot | Returns immutable player-visible records, pin, availability, and a journal revision; hidden/abandoned records are excluded. |
| ObserveWithSnapshot | Establishes a current snapshot and observation stream without a gap or duplicate accepted revisions. Late consumers see current state, not replayed toasts. |
| SetPinned(task key or none) | Changes the player's pin under the eligibility rules; cannot change source-owned stage/outcome. |
| ReleasePublisher | Invalidates that writer and marks retained records unavailable while their scope exists. |
| ReleaseScope | Removes scope-owned records and invalidates its writers and dependent UI references; repeat release is harmless. |

Task identity is `(SourceId, LocalTaskId)` within a session. SourceId identifies an authored/spawned source instance; title, display name and item ID do not establish identity. LocalTaskId identifies an occurrence within that source. Two scrubbers with local key `restore-atmosphere` remain distinct. A stale publisher cannot affect a replacement registration of the same SourceId.

Publish is a full-snapshot upsert. Repeated identical valid content is Unchanged with no event/toast/order change. Validate ownership, identity, stage, required text and outcome transitions before mutation. Invalid/unsupported inputs leave prior records intact. Active requires title, stage key and next step; Completed/Failed requires title and result. Optional location/condition are supported, with UNKNOWN for absent metadata. Abandoned requires identity/outcome but no display copy. Discovery cannot be revoked.

Mutating calls are synchronous and serialized through the session. Reentrant mutations during change delivery return Busy without mutation. Read-only observation of the accepted state remains possible. No asynchronous/network transport is required.

Accepted visible changes notify observers with task identity, immutable before/after visible state, journal revision and cause: Discovered, Updated, Completed, Failed, Removed, AvailabilityChanged, or PinChanged. Hidden-only updates produce no player-facing events. Events describe accepted state; they are not fire-and-forget commands whose success depends on a view being subscribed.

A failing observer is isolated and diagnosable. It cannot undo an accepted write, block other observers or roll back the gameplay transaction that supplied the fact. Rejection by the journal likewise cannot turn a committed repair into a failed inventory transaction. Sources can reconcile by republishing current state after temporary unavailability.

Example inputs and outcomes:

- Source `scrubber-a`, local `restore-atmosphere`, hidden Active snapshot: internal record created, visible count remains zero.
- Same record discovered with step `replace-fuse`: Active count becomes one; one TASK ADDED notice.
- Republish identical snapshot: Unchanged; no additional notice.
- Pin it, then publish step `restart`: same pin and row identity, updated instruction.
- Publish Completed with result `Room atmosphere restored`: Completed count increases, Active count decreases, pin clears, one completion notice.
- Source `scrubber-b` publishes the same local key: a separate task, not an overwrite.
- A different test source publishes Failed: Completed contains a FAILED entry and its result; a source publishes Abandoned while Active: that occurrence disappears and cannot be resurrected.

## Data changes

Each contributed snapshot supplies local identity, title, discovered flag, outcome, stage key, location metadata, confirmed condition classification/text, Active next step, optional blocker, and terminal result. These records hold no Unity scene/view references or executable callbacks. Rendered text is literal; arbitrary rich-text markup is not an authoring contract.

The journal adds source identity/availability, first-discovery order, terminal order, record/journal revision and the single pinned key. Ordering values are assigned by the accepted journal history rather than source clocks. Active sorts pinned first, then earliest discovery; Completed sorts most recent terminal transition first with stable tie ordering. Updates and availability changes do not reorder entries.

No production save schema, item identity, container representation, or restoration transaction schema changes.

## Scrubber integration

Automatic discovery occurs when the normal interaction system first gives eligible primary focus to an active, bound scrubber access target whose confirmed state is faulty. This is the slice's precise meaning of noticing the broken scrubber: eligible in-range focus, not merely loading the room or seeing its sprite at a distance. A successful equipment-screen access also discovers the task if it was not already discovered. Both paths converge on the same occurrence. A Blocked access or generic Triggered event alone does not establish discovery. After repair, rediscovery cannot create another task.

Discovery reads current state. Repair may advance before discovery; the first visible snapshot then shows the current step/outcome. Automatically focusing an already operational source does not invent a new fault task; successful access may expose its already-completed task.

| Authoritative condition | Journal outcome | Stage key | Instruction / result |
| --- | --- | --- | --- |
| Fuse missing, scrubber stopped | Active | replace-fuse | Replace the scrubber fuse. |
| Fuse installed, scrubber stopped | Active | restart | Restart the scrubber. |
| Running, room air recovering | Active | recover-air | Wait for room air to recover. |
| Operational / room condition NORMAL | Completed | complete | Room atmosphere restored. |

Title: RESTORE ROOM ATMOSPHERE. Location comes from authored encounter metadata; the reference mockup's deck/room labels are illustrative, not required scene facts. Fault remains Blown scrubber fuse until installation; telemetry follows the equipment's confirmed state. Missing fuse, unavailable binding, blocked restart or repeated activation do not advance the task. Completion follows Operational, never Restart success alone. No scrubber terminal failure or abandonment branch is authored in the initial example.

Five seconds of active atmosphere recovery remains unchanged. The existing equipment screen remains unpaused. Opening the new journal pauses simulation; closing resumes only its owned pause contribution. There is no requirement for a percentage bar or continuously updating recovery value.

## UI reference and handoff — solitude-ui design mode

![Pinned Active task journal reference](design/task-journal-pinned-active.png)

The reference is an image-generated design mockup, not an actual Unity screenshot. It defines hierarchy, semantic separation and the pinned Active state. The following written state contract governs behavior and other states; tiny texture/shading/font differences produced by image generation are not production requirements. Use the existing UI guide and actual project fonts/palette. Exact pixel values and typeface are to be resolved against those assets and checked at gameplay scale, rather than sampled from the bitmap.

Target: the AtmosphericScrubberEncounter demo. The journal is shared presentation; task authoring/progression belongs to the scrubber. Reuse the guide's components without adding simulated hardware, a new theme, decorative framing, or a competing UI guide.

| Component | Layout and behavior |
| --- | --- |
| Header | TASK JOURNAL upper left; restrained metadata upper right. No presentation captions in runtime. |
| Tabs | ACTIVE and COMPLETED with live counts; active tab gets amber underline. Completed count includes failure. |
| Task list | Left column, approximately two-fifths of content width; scrolls independently. Row title/location/outcome, optional pin icon+word. Selected row gains amber rule/outline. |
| Details | Larger right column separated by a thin rule; title and breadcrumb, confirmed condition, then NEXT STEP for Active or RESULT for terminal entries. Optional blocker below instruction. |
| Pin control | PIN TASK / UNPIN TASK for an Active selection. Keyboard/pointer activation changes pin; navigating alone does not. Hidden or unavailable on terminal/empty selection with a clear reason if rendered disabled. |
| Footer | Navigation/tab hints and actual Pin/Close/Back bindings. Filled amber keycaps with dark glyphs; no fabricated supported-device binding. |
| Notice | Shared guide's exploration notice lane, separate from the primary journal interface. |

Default keyboard controls: J toggles the journal, P toggles selected Active task pin, Escape closes. Existing UI navigation, pointer, scroll and Submit apply. Show actual bindings; add no standalone input scheme. At default navigation focus, task rows select details; Submit does not pin unless the Pin control is focused. Pointer activation of Pin is explicit. Closing consumes activation and cannot trigger world interaction.

Open only during ready exploration. A request while inventory, transfer, equipment UI, another primary interface, transitions, unready state or unrelated pause is active returns unavailable without closing that screen. The journal blocks world movement, interaction, hotbar activation and primary world prompts, while permitting its own UI controls. Failed open, close, disable, destroy and scene release restore policy by releasing only journal-owned state. Existing pause/base speed is preserved.

Default tab is Active; default selection is its first ordered row. Retain tab, task identity, focus and scroll on reopen when valid. Pin promotion preserves selected identity. A row removed by completion/removal selects the next row at that index, otherwise the preceding row, otherwise clears details. Do not switch tabs automatically. A retained Completed tab remains there even if empty.

State rules:

- Selected faulty Active: amber focus/pin remains separate from red FAULT/OFFLINE. Next step stays readable; Unpin is available.
- Unpinned Active: no PINNED badge; Pin control available.
- Completed: green icon plus COMPLETED, confirmed condition and result; pin cleared/unavailable.
- Failed: red icon plus FAILED in Completed, failure result; no false OPERATIONAL/COMPLETED label.
- Source detached: last confirmed task plus separate gray SOURCE UNAVAILABLE; no invented success. Pin stays a usable journal preference for an Active record.
- Empty Active / Completed: NO ACTIVE TASKS / NO COMPLETED TASKS, no selectable fake rows; empty details read NO TASK SELECTED.
- Hidden journal: no UI input received. Ordinary journal screens contain no cyan AI annotations.

Give text clear contrast, padding, wrapping/scrolling and focus visibility. A long title/location/instruction cannot cover status, pin controls or footer. Preserve column hierarchy without stretching text. Check 1920×1080 and 1280×720 plus a narrower supported aspect ratio; wrap/scroll rather than clip. Selection, pin and outcome must remain distinguishable without color.

## Integration points and compatibility

Integrate with current composition and source lifecycle, shared input/primary-screen/pause policies, EventSystem navigation, project visual primitives, and scrubber authoritative state. Existing assembly and feature ownership rules still apply. No parallel interaction bus, input framework, modal policy or repair path is introduced for the journal.

Keep inventory/hotbar/locker transactions, fuse costs, restart prerequisites, recovery timing, world warning/audio, equipment-screen pause behavior, save recovery and demo isolation unchanged. A journal failure cannot prevent an otherwise valid repair. Existing asset GUIDs and serialized references must be preserved. Validate source identity uniqueness, required text, binding references and required journal controls through existing authoring validation conventions.

## Success criteria

- **S1 — Visible records:** discovered Active/Completed/Failed snapshots appear with title, location, labeled condition and step/result; hidden or Abandoned records do not affect player-facing lists, counts, pinning or notices.
- **S2 — Identity/idempotency:** first publish produces one record; identical republish returns Unchanged without another notice/reorder. Two sources sharing a local key remain distinct; duplicate live source registration is rejected without ownership loss.
- **S3 — Observation:** changes while UI is closed appear on its next snapshot/observation with no revision gap. A throwing observer cannot undo the accepted change or prevent another observer; reentrant writes return Busy.
- **S4 — Validation:** invalid identity/required fields/outcome transitions, stale publishers and released scopes preserve accepted state. Discovery cannot be revoked; terminal occurrences cannot reopen; disposal is idempotent.
- **S5 — Scrubber journey:** successful access discovers the current authoritative task and the four-state table matches actual fuse installation, restart, recovery and Operational. Failed repair attempts do not advance it; completion is not declared merely on restart.
- **S6 — Journal input/pause:** opening pauses simulation/recovery, gates world controls/prompts and supports UI navigation. J/Escape close safely, consume activation and preserve unrelated pause/base-speed state. Equipment-screen unpaused behavior remains unchanged.
- **S7 — Conflict/cleanup:** conflicting primary screens, transitions and unrelated pause reject opening without affecting them. Failed view open, destruction or scene release leaves no journal-owned pause/input state.
- **S8 — Lists/navigation:** live counts, deterministic order, tab/selection/focus retention, scroll and empty states work. Completion/removal of the selected row applies the specified fallback without tab switching; long content cannot obscure controls.
- **S9 — Notices:** each new discovery and observed completion/failure emits the specified single notice; ordinary stage/pin/idempotent updates emit none. Hidden/paused exploration time is excluded from provisional four-second dwell; queued discovery coalesces into terminal state; no duplicate scrubber completion presentation remains.
- **S10 — Lifetimes:** source detach retains records/availability/pin; reattach reconciles state. Scope release removes records/notices/pin and stale publishers cannot affect replacement ownership. Scene reload/new session contains no leaked tasks or completion.
- **S11 — Late discovery:** first discovery of already-completed/failed state yields one terminal record and one added notice, without fabricated earlier stages or a second terminal notice.
- **S12 — Compatibility:** existing saves need no migration; demo/production sessions remain isolated. Inventory, hotbar, transfers, repair authority, warning/audio, equipment screen and generic feedback behavior remain qualified by regression checks.
- **S13 — Diagnostics:** identity/text/binding/control errors yield useful authoring/runtime diagnostics. UI/observer failures cannot change committed inventory/repair outcomes.
- **S14 — Pinning:** only one visible Active task is pinned; pinning another transfers it, promotion preserves selection, unpin restores discovery order. Terminal outcome/removal clears pin; hidden/terminal tasks reject pinning. Pin persists across journal close and source detach without adding a HUD tracker.
- **S15 — General outcomes/stages:** two task sources can publish different stage keys/instructions without sharing a fixed progression. Failed tasks appear in Completed as FAILED with a result. Abandoned Active tasks disappear and cannot be rediscovered under the same occurrence identity. These contracts are checked independently of the scrubber's success-only demonstration.
- **S16 — Automatic discovery:** first eligible primary focus of a faulty bound scrubber discovers it once; successful later access yields no duplicate. Room loading alone, blocked access alone and a generic Triggered event alone do not establish discovery. The two authorized discovery paths agree on identity and current state.
- **S17 — Visual handoff:** actual demo UI follows the reference's hierarchy and project guide. Combined selected+pinned+fault, unpinned, completed, failed, unavailable and empty states preserve readable text, independent focus/outcome colors, correct key glyphs and unclipped controls at specified verification sizes.

## Required evidence and limits

The current deliverables are this draft and one generated mockup. The image has been visually inspected for pinned labeling, tab counts, fault/focus separation, control labeling and obvious overlaps. It is not a runtime visual pass, and cannot prove event wiring, input, scaling, pause behavior or state transitions. No code or scene changed, and no tests were run at Spec.

Future qualification must link outcomes to these criteria: pure Domain/Application checks for S1–S4, S9–S11, S14–S15; Unity source/lifecycle/input checks and the actual player journey for S5–S7, S10, S13, S16; gameplay-scale screenshots/navigation for S8/S17 and the notice lane S9. Use the existing full isolated regression gate for S12 and applicable existing scrubber tests; reading them in Research does not establish passing results. The guide's provisional notice duration remains subject to readability testing.

## Out of scope

Durable task/scrubber saves and migration; AI integration in this slice; authored failure/abandonment branches for the scrubber; player abandonment; multiple pinned tasks; permanent task HUD/waypoints/routes; rewards; quest graph/scripting framework; complete stage histories; asynchronous/networked task transport; continuous recovery bars; localization infrastructure; new shipping scenes; broader scene/UI migration.

## Review decision

Accept or revise this exact spec and its linked mockup before Plan. In particular, review the explicit draft choices: journal-only single pin, paused journal with unchanged unpaused equipment UI, eligible focus as automatic discovery, J/P/Escape defaults, and scene-scoped history. There are no outstanding discovery questions and no test qualification yet. No implementation plan is created by this step.
