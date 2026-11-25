# QRSPI Explain: survivor-succession — phase-1

## What this change does

Phase 1 introduces distinct, permanently retired survivor identities while reusing the authored player avatar: an empty-equipment debug death awakens a successor at the cryobay, resets character state, and displays an AI acknowledgment without reloading the scene. Equipped deaths are deliberately rejected until the later recovery-container phase; this phase explains both basic-succession steps, not the complete equipment and persistence journey.

## Background

The useful distinction is between the colonist and the Unity objects representing that colonist. Inventory and hotbar readers, movement, interaction, camera tracking, and vital views already belong to a composed player scene. Replacing that entire composition would also disturb consumers and live world state. This change instead replaces the survivor record and explicitly resets the retained avatar.

`SceneBindings` initializes scene features using `SceneFeatureContext`. The context now supplies the hotbar reader, selection, a screen-closing callback, and interaction feedback alongside the existing inventory, input, and policy services. Succession therefore reaches the player endpoints it needs through the existing composition boundary. Its Domain and Application classes contain no Unity or ship-system endpoints, and its state lasts only for the live scene.

## Walkthrough

**Identity comes first.** `SurvivorState` owns a nonempty GUID and a one-way lifecycle, `Pending → Active → Dead`. `TryActivate()` accepts only Pending; `TryRetire()` accepts only Active. The record is distinct from the player GameObject, so keeping the avatar does not make a dead survivor active again. Automatic successor choice is a replaceable function in the coordinator, currently producing a new pending record.

**A death is a guarded transition.** `SurvivorSuccessionCoordinator.RequestDeath(expectedSurvivor)` checks the requested identity before running the transition. A completed A→B handoff makes a later request for A return `StaleSurvivor`; reentrant requests return `Busy`. The coordinator also remembers used identities so a choice function cannot recycle A's identity later.

The transition proceeds through `Ready`, `Prepared`, `EquipmentCommitted`, and `AvatarActivated`. Preparation can reject configuration or equipment without retiring A. Commit records a transition-specific receipt, retires A, clears `Current`, and reserves the successor identity. Activation resets the avatar before making B active. Acknowledgment and removal of the gameplay gate finish the transition.

The receipt matters even though this phase hands off no items. It marks the boundary after which retry must continue forward. Before commit, failure cancels preparation and ungates the current survivor. After commit, the coordinator retains `Pending`, `Receipt`, and its stage, keeps gameplay gated, and retries the same successor without recommitting. A failure after avatar activation retries acknowledgment rather than activation. `LastError` exposes the failure reason.

**Wakeup opts in through authored references.** `SurvivorSuccessionComposition` implements the narrow port interface and derives from the existing scene feature base. Its initialization validates the retained reader/input bindings, creates and activates the first survivor, awakens the avatar, and clears selection with `Select(-1)`. The scene contains a `CryobayAwakeningPoint`, a `SurvivorAvatarAdapter` bound to the existing player, and a succession feature registered in `SceneBindings.features`.

`SurvivorSuccessionAuthoring.AuthorWakeup()` creates these endpoints if absent, taking the original player placement as the cryobay point, then validates and saves the scene. It refuses existing dirty scene edits. Runtime validation rejects missing, inactive, wrong-scene, duplicate, or mismatched avatar configuration. The component exposes context-menu actions to retire the current survivor and retry a pending awakening.

**Equipment is a phase boundary.** `TryPrepare` requires both inventory and hotbar to be available and every slot to be empty. `TryCommitEquipment` checks that condition again before returning a receipt. Thus no container clearing, owner replacement, or recovery object creation happens in this phase. If either container holds equipment, A stays active and the operation returns failure with `Equipped death requires the recovery-container phase.`

**Reset is explicit and coordinated.** At the start of an accepted transition, the composition calls `Policy.SetTransition(true)`, `Input.ResetSurvivorInput()`, and the scene's screen-closing callback. `SceneBindings.CloseForTransition()` clears interaction focus and feedback, closes the journal and feature screens, closes the container modal, and cancels gestures even when no modal is open. Those transient resets also happen on a subsequently rejected equipped death; rejection preserves survivor identity and items, not the old open screen or gesture.

Input reset records actuated controls in `heldAtReset` and refuses callbacks for those actions until their controls have been released. It also blocks UI click/submit delivery while reset-time activation controls remain held and consumes activation in the reset frame. This prevents a key or click held by A from becoming B's first action when policy re-enables input. New input after release uses the existing bindings.

`SurvivorAvatarAdapter.Awaken()` zeros movement input, linear velocity, and angular velocity, moves both the Transform and Rigidbody2D to the authored point, synchronizes physics, rebuilds nearby interaction targets, and resets vitals. `PlayerInteractor.RebuildProximityAfterRelocation()` clears the old proximity cache and scans active scene sensors for overlaps with the relocated player. The vital reset calls `Set` on the existing Health and Oxygen objects using the controller's authored current-value baseline, preserving subscriptions instead of replacing the objects. Selection is cleared again for the successor. The player and camera target remain the same objects.

**Feedback completes the visible handoff.** The composition sends `AI: Survivor lost. New colonist awakened. Restoration continues.` through existing interaction feedback. A set of transition IDs reserves acknowledgment before presentation so retry cannot replay that transition's message. Nothing in the succession path releases the scene scope or resets first-contact/world objects; their existing state remains in the live scene.

**The source includes checks at each boundary.** Domain tests describe permanent retirement; Application tests exercise stale and reentrant requests, successor selection, preparation/commit rejection, retained receipts, activation retry, and uncertain acknowledgment. Authoring tests cover repeated authoring and invalid points. PlayMode tests describe retained readers/camera/vitals, repeated empty deaths, held movement, modal and gesture cleanup, equipped rejection, first-contact/powered-door continuity, and closing a journal in a demo configured by a test fixture. These descriptions come from test source; this explanation did not run tests or consult verification reports.

## Where to focus

The commit boundary deserves the closest reading: the receipt and stage determine whether a failure may return to the old active survivor or must remain gated and resume the pending successor. Later equipment recovery will rely on that same distinction, including the port contract that a commit cannot throw after transferring ownership.

The other concentrated area is transient reset ordering across policy, held controls, UI leases, gestures, physics relocation, proximity caches, and vital subscriptions. Those parts collectively explain why retaining an avatar can still produce a fresh successor while keeping ship objects live.
