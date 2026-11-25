# QRSPI Explain: survivor-succession — final

## What this change does

An opted-in scene can permanently retire its current survivor, leave carried equipment at the death location, and awaken a distinct successor at an authored point while the ship stays live. The avatar, camera target and active-player container readers remain in place; survivor identity and transient player state change.

This is an unverified source-reading narrative, with no correctness verdict or claimed test outcome. Its scope is product changes from `4de49500a28f6506f18eb25eda65437cf4e6715c` to `69afe43795ee8510dff48b745c70826e0bdc3d5c`, using the supplied exact diff command excluding `.qrspi`. HEAD matched the expected revision and the product working tree was clean when inspected. The spec and normalized overview/three phase-plan digests matched the supplied revisions; normalization changed only status-marker tokens and overview Status cells.

## Background

Existing composition registers inventory and hotbar as `player.inventory` and `player.hotbar`. Restoration, pickups, UI and feature adapters already consume those container readers. Succession treats those fixed IDs as access to whoever currently occupies the retained avatar. It changes their contents through the command service rather than releasing and registering replacement readers.

Ship state remains owned by existing scene systems. The new Domain identity model and Application coordinator contain no ship endpoints or Unity calls; feature-owned Unity adapters implement the narrow succession ports. This separation keeps automatic successor choice independent from equipment transactions, avatar activation and ship progression.

## Walkthrough

`SurvivorState` carries a GUID and progresses from `Pending` to `Active` to `Dead`; a dead record cannot activate again. `SurvivorSuccessionCoordinator` accepts `RequestDeath(expectedSurvivor)`, rejects stale identities, blocks reentrant execution and remembers used identities. Its default chooser makes a new pending survivor, while an injected chooser provides a distinct seam for future selection without adding a roster now. Current identity, pending transition, receipt, stage and diagnostic error are inspectable.

`SurvivorSuccessionComposition` opts into the existing `SceneFeatureComposition` lifecycle. Wakeup adds a cryobay point at `(-0.89, -2.4, 0)`; the scrubber demo adds its test point at `(23, 4.5, 0)`. Both bind the existing player through `SurvivorAvatarAdapter` and reference an inactive recovery prefab. Authoring validates one composition per scene, same-scene avatar/point bindings, and recovery presentation/proximity prerequisites. Initial composition creates an active identity, awakens the avatar and clears hotbar selection. It does not clear initially loaded carried contents.

A death begins by setting the transition policy gate. `UnityInputHost.ResetSurvivorInput()` records actuated controls, neutralizes movement and suppresses held gameplay actions until release. Held UI click/submit controls temporarily disable UI delivery. `SceneBindings.CloseForTransition()` clears focus/feedback, closes journal and container screens, asks features to close their screens, and cancels pending gestures even when no modal is open.

Preparation captures the avatar’s physics position and, for nonempty equipment, creates an inactive cache with capacity equal to inventory plus hotbar capacity. It registers a unique `survivor.recovery.<guid>` owner directly with the existing command service. The cache records its departing survivor GUID and death position. Empty equipment still receives an Application receipt but creates no cache.

`PrepareHandoff` builds one plan covering both carried sources and an empty independent destination. It copies individual stacks into destination slots, reserves both full source capacities, checks filters, and records owner generations, clocks and capacities. `CommitHandoff` rechecks those inputs, updates both sources to empty and the destination to their combined contents, and stores a transition receipt before notifying observers. Repeating a committed plan returns its prior receipt rather than transferring new contents. Ordinary UI keeps the smaller transfer-command interface.

After that irreversible boundary, the coordinator retains the receipt, retires the departing survivor and sets `Current` to null. Activation resets movement and Rigidbody velocity, relocates the same Transform and Rigidbody, synchronizes physics, rebuilds nearby interaction targets, resets the existing Vital objects to authored values, and selects no hotbar slot. The prepared cache publishes only after conserved contents exist. The successor then becomes active. `VitalBarView` waits for resources to exist and synchronizes when bound or re-enabled; displaying a vital never initializes or heals the player.

Failure before equipment commit cancels the empty preparation and restores access to the existing survivor. Failure after commit preserves the dead survivor, pending successor, cache ownership and gate. `Retry()` resumes from the retained stage, without choosing another survivor or transferring equipment again. A failure after avatar activation resumes acknowledgment without awakening again. Diagnostic information is kept in `LastError`; the debug retirement context action logs rejection.

Published caches use normal proximity/focus and open the existing transfer UI under the label “Recovered equipment.” SceneBindings now accepts a registered reader and label while preserving locker initialization and lazy loot. Existing slot-transfer behavior handles partial merges, swaps and capacity limits. The shared container panel gains a bounded vertical scrolling viewport, sized from capacity and reset on each bind, so the combined carried capacity remains reachable. Older caches remain independent through later deaths and release their own owners when the feature/scene is released.

Successful activation routes a brief AI acknowledgment through the existing interaction feedback service. A transition-ID set reserves delivery before presentation to suppress replay after uncertain delivery. The existing `OpeningFirstContact.Heard` flag and coroutine remain on their live scene object; succession neither recreates that object nor resets its knowledge. Likewise, the death path contains no scene reload, save reset or ship-scope release, so repair, power, scrubber, locker and collection state remain under their existing owners.

Persistence remains deliberately limited. `ContainerSaveSession.Capture()` serializes its registered bindings and collected pickup facts. Recovery registers directly with `Commands`, has no saveable locker identity and is absent from those bindings; survivor identities, receipts and cache positions are runtime state. Player and locker saving continue under the existing contract, but this change does not reconstruct dead survivors, dropped equipment locations or live ship modifications after unload/restart. The new test sources describe identity, ownership, interruption, real interaction, scene compatibility and vital-readiness scenarios; their presence is not execution evidence.

## Where to focus

Spend attention on the receipt boundary: preparation is reversible, equipment commit makes death irreversible, and activation/acknowledgment retries must advance from preserved ownership while the gate stays held. In particular, the port contract forbids a commit adapter from throwing after committing before returning its receipt.

Also inspect retained bindings and scene lifetime together: held input, stale gestures/focus, reader identity and HUD subscriptions must all serve the successor, while recovery owners and AI/ship continuity belong to the still-live scene. Saving active-player containers does not save the runtime recovery locations or survivor history.
