# World pickups

Create an Item Definition, add it to Resources/Database.asset, then create a Pickup Definition
through the SOLITUDE/Items asset menu.
The pickup definition selects the item, quantity and optional success text. Add WorldPickup
to a prefab with its visuals, collider and interaction prompt, and assign that definition.
The required SaveableId stays empty on the prefab asset. Each placed scene instance needs
its own serialized ID. SaveableId assigns empty scene IDs during validation; duplication
retains an ID, so clear the duplicated instance's ID to generate a new one.

Run SOLITUDE/Validation/Validate World Pickups before committing scenes. The validator
checks all project scenes, including inactive objects, and runs before player builds.
Never change an existing instance ID to rename or move it: saved collections use that ID.

SaveGameService binds scene pickups after Awake and loads their collection facts before
Start. Runtime spawn systems must assign a deterministic per-world/per-spawn identity via
SaveableId.AssignRuntimeIdentity, then call SaveGameService.BindPickup. They must recreate
the same identity on reload. Pickup objects do not locate inventory or the save host.

PickupCollectionService snapshots the configured reward at binding time. It validates
requests, grants the entire quantity or nothing, and commits the collected fact before
publishing inventory notifications. Collection facts and containers share one save file.
Suppressed pickup objects remain registered until scene unload, preventing duplicate grants.
Start New Save clears session state and reloads the active gameplay scene.

Save schema 2 explicitly migrates schema 1 without changing container records or world seed.
Old saves did not identify collected world pickups, so migration starts with an empty set:
previously collected pickups can appear once more. Unknown versions or malformed collection
IDs preserve the existing file and disable collection/writes until a new save is requested.

Item-use effects (energy, oxygen, health) are separate from pickup collection. Adding a new
ordinary pickup requires configuration, not a new MonoBehaviour subclass.
