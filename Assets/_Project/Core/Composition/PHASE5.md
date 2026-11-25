# Assembly and build contract

Domain contains item specifications, container commands/invariants, and snapshot
models. Application depends only on Domain and contains sessions, input/modal
policy, presenters, and complete-file recovery policy. Both compile without Unity.
Runtime depends on both and adapts Unity authoring, input, files, and existing views.
Editor owns migrations, validators, and build entry points. Tests are explicit
EditMode, PlayMode, Domain, and Application assemblies. The standalone verification
driver has a separate define-constrained assembly and is excluded from shipping.

`Editor/WakeupBuildManifest.asset` selects Wakeup and its runtime configuration.
Enabled build scenes must match that manifest. Validation checks catalog and loot
rules, composition references, input actions, scene identities, reachable prefab
scripts, metadata, and assembly direction. It writes sorted findings with stable
codes, asset paths, fields, and descriptions to JSON and text. Validation uses
preview scenes and does not assign identities or modify designer assets.

Assign missing scene identities explicitly using the SaveableId context command
or the world pickup migration. Pickup templates remain unidentified; scene prefab
instances store their own identity overrides. Duplicate IDs fail validation.

# Save recovery

A valid committed primary wins. A corrupt or missing primary may recover a valid
backup; an incompatible primary, permission/read error, or unknown catalog item
blocks startup. Temporary files are never promoted as committed snapshots. Owner
registration and capacity validation must succeed before recovery is committed.
Unknown items and insufficient capacity continue to block; oversized stacks split
only when there is room.

Recovery writes a durable receipt, then a durable temporary snapshot, then promotes
it atomically. A corrupt primary is quarantined and the valid backup is retained.
The receipt survives restart until the visible notice is dismissed. Normal saves
rotate the valid primary into the backup with atomic replacement. Failed writes
leave the session dirty for retry. Explicitly starting a new game preserves any
valid backup when replacing a blocked primary.

Call `GameCompositionRoot.TransitionTo` for player scene transitions so gameplay
input and modal sessions are closed before loading. Context scene unload releases
only that scene's owners and closes its active transfer; unrelated scene unload
leaves the transfer intact. Lifecycle verification uses the actual Wakeup root.

Run the repository gate described in `Tools/Verification/README.md` for evidence.
