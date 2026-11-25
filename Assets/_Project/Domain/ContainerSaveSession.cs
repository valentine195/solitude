using System;
using System.IO;
using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;
namespace SOLITUDE.SaveLoad
{
    public sealed class ContainerSaveSession
    {
        public const string PlayerInventoryId = "player.inventory";
        public const string PlayerHotbarId = "player.hotbar";
        private readonly Dictionary<string, ContainerBinding> bindings = new Dictionary<string, ContainerBinding>(StringComparer.Ordinal);
        private readonly IItemCatalog catalog;
        private SaveGameData data;
        private readonly HashSet<string> collected = new HashSet<string>(StringComparer.Ordinal);
        public ContainerCommandService Commands { get; }
        public bool IsCollecting => Commands.IsBusy;
        public bool IsDirty { get; private set; }
        // Transient urgency; never part of the serialized save format.
        public bool RequiresImmediateSave { get; private set; }
        public bool HasIntegrityFailure { get; private set; }
        public int WorldSeed => data.worldSeed;
        public ContainerSaveSession(SaveGameData source, IItemCatalog catalog)
        {
            this.catalog = new ItemCatalog((catalog ?? throw new ArgumentNullException(nameof(catalog))).Items);
            data = SaveGameData.Migrate(Clone(source ?? throw new ArgumentNullException(nameof(source))));
            IsDirty = source.version != SaveGameData.CurrentVersion;
            RequiresImmediateSave = IsDirty;
            data.containers ??= new List<ContainerSaveRecord>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in data.containers)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.saveableId) || !ids.Add(record.saveableId) || record.state?.slots == null)
                    throw new InvalidDataException("Invalid or duplicate saved container.");
                var indices = new HashSet<int>();
                foreach (var slot in record.state.slots)
                    if (slot == null || slot.index < 0 || !indices.Add(slot.index) || slot.quantity < 1 || !this.catalog.TryGet(slot.itemId, out _))
                        throw new InvalidDataException("Invalid saved item or slot in " + record.saveableId);
            }
            collected.UnionWith(data.collectedPickupIds);
            Commands = new ContainerCommandService(this.catalog);
            Commands.Completed += _ => IsDirty = true;
        }
        public bool StartingLoadoutInitialized => data.startingLoadoutInitialized;
        public CommandResult InitializeStartingLoadout(ContainerHandle inventory, ContainerHandle hotbar,
            IReadOnlyList<PickupReward> grants, int selectedSlot)
        {
            if (Commands.IsBusy) return new CommandResult(CommandStatus.Busy);
            if (HasIntegrityFailure) return new CommandResult(CommandStatus.Unavailable);
            if (data.startingLoadoutInitialized) return new CommandResult(CommandStatus.NoOp);
            if (!bindings.TryGetValue(PlayerInventoryId, out var first) || !first.Container.Handle.Equals(inventory)
                || !bindings.TryGetValue(PlayerHotbarId, out var second) || !second.Container.Handle.Equals(hotbar))
                return new CommandResult(CommandStatus.Unavailable);
            return Commands.InitializeStartingLoadout(inventory, hotbar, grants, selectedSlot, () =>
            { data.startingLoadoutInitialized = true; IsDirty = true; RequiresImmediateSave = true; });
        }
        public bool IsPickupCollected(string id) => collected.Contains(id);
        internal CollectResult Collect(string pickupId, string inventoryId, string itemId, int quantity)
        {
            if (IsCollecting || HasIntegrityFailure || !bindings.TryGetValue(inventoryId, out var inventory)) return CollectResult.NotReady;
            if (collected.Contains(pickupId)) return CollectResult.AlreadyCollected;
            if (quantity < 1 || !catalog.TryGet(itemId, out _)) return CollectResult.InvalidPickup;
            var result = Commands.Grant(inventory.Container.Handle, itemId, quantity, () => collected.Add(pickupId));
            if (result.Status == CommandStatus.Applied) { RequiresImmediateSave = true; return CollectResult.Collected; }
            if (result.Status == CommandStatus.InsufficientSpace) return CollectResult.InventoryFull;
            return result.Status == CommandStatus.Busy ? CollectResult.NotReady : CollectResult.InvalidPickup;
        }
        public bool TryGetContainer(string id, out ContainerSaveData state)
        {
            if (Commands.IsBusy) throw new InvalidOperationException("Cannot capture during a command.");
            if (id != null && bindings.TryGetValue(id, out var live))
            { state = ContainerSaveSerializer.Capture(live.Container); return true; }
            foreach (var record in data.containers)
                if (record.saveableId == id) { state = ContainerSaveSerializer.Clone(record.state); return true; }
            state = null; return false;
        }
        public ContainerBinding Register(string id, int capacity, Func<ItemSpec, bool> filter = null, IReadOnlyList<PickupReward> initialLoot = null)
        {
            if (bindings.ContainsKey(id)) throw new InvalidOperationException("Container identity already registered: " + id);
            bool existing = TryGetContainer(id, out var saved);
            Container container;
            try { container = Commands.Register(id, capacity, saved, filter, initialLoot); }
            catch (InvalidDataException) { HasIntegrityFailure = true; throw; }
            var binding = new ContainerBinding(this, id, container); bindings.Add(id, binding);
            var normalized = ContainerSaveSerializer.Capture(container);
            if (!existing || !Equivalent(saved, normalized)) IsDirty = true;
            Store(id, normalized); return binding;
        }
        public CommandResult Restore(ContainerHandle handle, ContainerSaveData state)
        {
            var result = Commands.Restore(handle, state);
            if (!result.Succeeded && result.Status != CommandStatus.Busy) HasIntegrityFailure = true;
            return result;
        }
        public SaveGameData Capture()
        {
            if (IsCollecting) throw new InvalidOperationException("Cannot capture during a command.");
            var snapshot = Clone(data);
            foreach (var binding in bindings.Values) Store(snapshot, binding.Id, ContainerSaveSerializer.Capture(binding.Container));
            snapshot.collectedPickupIds = new List<string>(collected); snapshot.collectedPickupIds.Sort(StringComparer.Ordinal);
            snapshot.ValidateLifecycle();
            return snapshot;
        }
        public bool TryInitializeLifecycle(SurvivorSnapshot survivor, ExpeditionState expedition)
        {
            if (Commands.IsBusy || HasIntegrityFailure) throw new InvalidOperationException("Session is not ready for lifecycle changes.");
            if (data.survivor != null) return false;
            if (survivor == null || expedition == null) throw new ArgumentNullException("Initial lifecycle state is required.");
            StoreLifecycle(survivor, expedition);
            return true;
        }
        public void StoreLifecycle(SurvivorSnapshot survivor, ExpeditionState expedition, SavedDeathTransition pendingDeath = null)
        {
            if (Commands.IsBusy || HasIntegrityFailure) throw new InvalidOperationException("Session is not ready for lifecycle changes.");
            if (survivor == null || expedition == null) throw new ArgumentNullException("Lifecycle state is required.");
            var candidate = Capture();
            candidate.survivor = survivor.Clone(); candidate.expedition = expedition.Clone();
            candidate.pendingDeath = pendingDeath?.Clone();
            candidate.ValidateLifecycle();
            data = candidate; IsDirty = true; RequiresImmediateSave = true;
        }
        public void StoreShipState(ShipStateSnapshot ship)
        {
            if (Commands.IsBusy || HasIntegrityFailure) throw new InvalidOperationException("Session is not ready for ship changes.");
            if (ship == null) throw new ArgumentNullException(nameof(ship));
            var candidate = Capture(); candidate.ship = ship.Clone(); candidate.ValidateLifecycle();
            data = candidate; IsDirty = true; RequiresImmediateSave = true;
        }
        public void MarkSaved()
        {
            if (Commands.IsBusy) throw new InvalidOperationException("Cannot acknowledge a snapshot during a command.");
            IsDirty = false; RequiresImmediateSave = false;
        }
        public void Reset(int worldSeed, bool releaseOwners = false)
        {
            Commands.Reset(() =>
            {
                data = new SaveGameData { worldSeed = worldSeed };
                collected.Clear(); HasIntegrityFailure = false;
            });
            RequiresImmediateSave = true;
            if (!releaseOwners) return;
            // A new world must not retain cleared locker records or old owner generations.
            // Owners disposed by the ensuing scene reload are now harmless no-ops.
            foreach (var binding in new List<ContainerBinding>(bindings.Values)) binding.Dispose();
            data.containers.Clear();
        }
        private void Store(string id, ContainerSaveData state) => Store(data, id, state);
        private static void Store(SaveGameData target, string id, ContainerSaveData state)
        {
            foreach (var record in target.containers) if (record.saveableId == id) { record.state = state; return; }
            target.containers.Add(new ContainerSaveRecord { saveableId = id, state = state });
        }
        private static bool Equivalent(ContainerSaveData a, ContainerSaveData b)
        {
            if (a?.slots == null || a.slots.Count != b.slots.Count) return false;
            var map = new Dictionary<int, ContainerSlotSaveData>(); foreach (var slot in a.slots) map.Add(slot.index, slot);
            foreach (var slot in b.slots)
                if (!map.TryGetValue(slot.index, out var old) || old.itemId != slot.itemId || old.quantity != slot.quantity) return false;
            return true;
        }
        private static SaveGameData Clone(SaveGameData source)
        {
            var result = new SaveGameData { version = source.version, worldSeed = source.worldSeed,
                startingLoadoutInitialized = source.startingLoadoutInitialized,
                containers = source.containers == null ? null : new List<ContainerSaveRecord>(),
                collectedPickupIds = source.collectedPickupIds == null ? null : new List<string>(source.collectedPickupIds),
                survivor = source.survivor?.Clone(), expedition = source.expedition?.Clone(),
                ship = source.ship?.Clone(), pendingDeath = source.pendingDeath?.Clone() };
            if (source.containers != null) foreach (var record in source.containers)
                result.containers.Add(record == null ? null : new ContainerSaveRecord { saveableId = record.saveableId, state = ContainerSaveSerializer.Clone(record.state) });
            return result;
        }
        public sealed class ContainerBinding : IDisposable
        {
            private ContainerSaveSession session;
            internal string Id { get; }
            public Container Container { get; }
            internal ContainerBinding(ContainerSaveSession session, string id, Container container) { this.session = session; Id = id; Container = container; }
            public void Dispose()
            {
                if (session == null) return;
                if (session.Commands.IsBusy) throw new InvalidOperationException("Cannot unregister during publication.");
                session.Store(Id, ContainerSaveSerializer.Capture(Container)); session.bindings.Remove(Id);
                session.Commands.Unregister(Container.Handle); session = null;
            }
        }
    }
}
