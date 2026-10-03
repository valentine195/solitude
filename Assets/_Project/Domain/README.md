# Item and container domain

`SOLITUDE.Domain` has no Unity references. `ItemDefinition`, `PickupDefinition`, and
`LootTable` remain designer authoring assets; the save host compiles item rules,
presentation data, and loaded loot tables when it composes the session. Rules and
acceptance policies are copied and frozen. Asset edits take effect in a new session.

A container has a persistent ID and a fresh registration generation. Unity owners
register capacity and receive an `IContainerReader`; failed restoration never
publishes an owner. Readers return immutable stacks and detached slot snapshots.
A slot address carries the owning handle, index, and expected revision.

`ContainerCommandService` owns storage. It validates and plans every grant,
removal, transfer, resize, restore, and loot initialization before committing.
UI receives `ITransferCommands`; it submits source and target addresses and cannot
create items or write storage. Trusted composition and persistence retain the full
service. Results describe failure, partial merge quantities, and committed addresses.

Transactions commit all affected containers and pickup facts before notifying
listeners. The session becomes dirty before view callbacks. Commands attempted
during planning or publication return `Busy`; snapshots cannot be captured then.
Observer errors are reported separately without interrupting other observers.

Drags retain source ownership and hold only an address plus immutable display data.
Source changes, explicit restore/reset, resize, and unregistration invalidate the
gesture. Self-drops, full matching destinations, and unchanged capacities are no-ops.
An explicit restore/reset refreshes revisions even when item values are identical.

Save JSON remains version 2 with the existing item ID, index, and quantity fields.
Oversized stacks retain their original occupied slots, then split excess in index
order into matching partial stacks and empty slots. Unknown items, malformed records,
rejected items, or insufficient capacity reject the whole restore. The host blocks
automatic writes after an integrity failure so it cannot replace an incompatible save.
Unloaded records remain intact. A corrected configuration can load a new session;
only an explicit new save clears the failure flag in an existing session. A new
world releases old owner generations and discards their cleared records before
scene reload, allowing world containers to roll fresh loot.

Loot initialization considers each complete candidate against a temporary plan.
A candidate that cannot fit leaves that plan unchanged; subsequent candidates still
get a chance. Saved lockers do not roll again. Item-use effects, charge, durability,
and per-instance item state are outside this change.

Regression tests are under `Tests/Editor`. They include PlayMode scene reload,
quantity conservation, frozen authoring and policies, transaction visibility,
reentrancy, stale addresses, lossless normalization, detached saves, and loot planning.
