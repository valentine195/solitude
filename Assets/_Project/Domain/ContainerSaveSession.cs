using System;
using System.IO;
using System.Collections.Generic;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;
using SOLITUDE.Restoration;
using SOLITUDE.GameplayTags;
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
            Commands.Completed += change =>
            {
                IsDirty = true;
                foreach (var handle in change.Containers)
                    if (handle.Id == PlayerInventoryId || handle.Id == PlayerHotbarId) { ReconcileEquipment(); break; }
            };
        }
        public PlayerEquipmentRecord Equipment => data.playerEquipment?.Clone();
        public event Action EquipmentChanged;
        private bool HasCarriedItem(string id)
        {
            if (id == null || !bindings.TryGetValue(PlayerInventoryId, out var binding)) return false;
            for (int i = 0; i < binding.Container.Capacity; i++)
                if (binding.Container.GetSlot(i)?.Stack?.ItemId == id) return true;
            return false;
        }

        private void ReconcileEquipment()
        {
            if (data.playerEquipment == null || !bindings.ContainsKey(PlayerInventoryId)) return;
            var next = data.playerEquipment.Clone();
            if (next.separateStorage && bindings.TryGetValue(PlayerHotbarId, out var gear))
            {
                next.weaponItemId = gear.Container.GetSlot(0)?.Stack?.ItemId;
                next.toolItemId = gear.Container.GetSlot(1)?.Stack?.ItemId;
            }
            for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
                if (!HasCarriedItem(next.QuickUseItem(i))) next.SetQuickUseItem(i, null);
            PublishEquipment(next);
        }

        private void PublishEquipment(PlayerEquipmentRecord next)
        {
            next.ResolveActiveSlot();
            var old = data.playerEquipment;
            data.playerEquipment = next;
            if (old != null && old.activeSlot == next.activeSlot && old.weaponItemId == next.weaponItemId &&
                old.toolItemId == next.toolItemId && SameQuickUse(old, next)) return;
            IsDirty = true;
            EquipmentChanged?.Invoke();
        }

        private static bool SameQuickUse(PlayerEquipmentRecord old, PlayerEquipmentRecord next)
        {
            for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
                if (old.QuickUseItem(i) != next.QuickUseItem(i)) return false;
            return true;
        }
        private readonly Dictionary<string, MechanicalRepairBinding> repairBindings = new Dictionary<string, MechanicalRepairBinding>(StringComparer.Ordinal);
        public MechanicalRepairBinding BindMechanicalRepair(MechanicalRepairRequirement requirement)
        {
            if (Commands.IsBusy || HasIntegrityFailure || requirement == null || !catalog.TryGet(requirement.PartItemId, out _)
                || repairBindings.ContainsKey(requirement.TargetId)) throw new InvalidOperationException("Repair binding unavailable or duplicated.");
            // Validate the authored key with the existing save contract before publishing ownership.
            var ship = data.ship.Clone();
            ship.repairs.RemoveAll(record => record.id == requirement.TargetId);
            ship.repairs.Add(new RepairSaveRecord { id = requirement.TargetId }); ship.Validate();
            var binding = new MechanicalRepairBinding(this, requirement); repairBindings.Add(requirement.TargetId, binding); return binding;
        }
        public sealed class MechanicalRepairBinding : IDisposable
        {
            private ContainerSaveSession session;
            private readonly MechanicalRepairRequirement requirement;
            public event Action Changed;
            internal MechanicalRepairBinding(ContainerSaveSession session, MechanicalRepairRequirement requirement)
            { this.session = session; this.requirement = requirement; }
            public bool IsCompleted
            {
                get
                {
                    if (session == null) return false;
                    foreach (var record in session.data.ship.repairs)
                        if (record.id == requirement.TargetId) return record.installed && record.running && record.recoverySeconds == 0;
                    return false;
                }
            }
            public MechanicalRepairOutcome TryRepair(ContainerHandle inventory, ContainerHandle hotbar, Func<string, GameplayTagSet> tags)
            {
                var owner = session;
                if (owner == null || owner.HasIntegrityFailure || owner.Commands.IsBusy || tags == null) return MechanicalRepairOutcome.Unavailable;
                var outcome = MechanicalRepairOutcome.Unavailable;
                SaveGameData candidate = null;
                var result = owner.Commands.RemoveForMechanicalRepair(() =>
                {
                    if (!owner.bindings.TryGetValue(PlayerInventoryId, out var carried) || !carried.Container.Handle.Equals(inventory)
                        || !owner.bindings.TryGetValue(PlayerHotbarId, out var bar) || !bar.Container.Handle.Equals(hotbar)
                        || !ReferenceEquals(owner.Commands.Read(inventory), carried.Container)
                        || !ReferenceEquals(owner.Commands.Read(hotbar), bar.Container)) return null;
                    if (IsCompleted) { outcome = MechanicalRepairOutcome.AlreadyRepaired; return null; }
                    try
                    {
                        bool compatible = false;
                        foreach (var container in new[] { carried.Container, bar.Container })
                            foreach (var slot in container.GetSlots())
                            {
                                if (slot.IsEmpty) continue;
                                var capabilities = tags(slot.Stack.ItemId);
                                if (capabilities == null) return null;
                                if (requirement.ToolQuery.Matches(capabilities)) compatible = true;
                            }
                        if (!compatible) { outcome = MechanicalRepairOutcome.MissingTool; return null; }
                        SlotAddress? cost = null;
                        foreach (var slot in carried.Container.GetSlots())
                            if (!slot.IsEmpty && slot.Stack.ItemId == requirement.PartItemId) { cost = slot.Address; break; }
                        if (!cost.HasValue) { outcome = MechanicalRepairOutcome.MissingPart; return null; }
                        candidate = Clone(owner.data);
                        candidate.ship.repairs.RemoveAll(record => record.id == requirement.TargetId);
                        candidate.ship.repairs.Add(new RepairSaveRecord { id = requirement.TargetId, installed = true, running = true });
                        candidate.ValidateLifecycle();
                        return cost;
                    }
                    catch (Exception) { return null; } // Unavailable catalog/invalid candidate: nothing has mutated.
                }, () =>
                {
                    // Only trusted non-failing writes occur after inventory mutation.
                    owner.data = candidate; owner.IsDirty = true; owner.RequiresImmediateSave = true;
                    if (Changed != null)
                        foreach (Action observer in Changed.GetInvocationList())
                            try { observer(); } catch { /* Observers cannot invalidate a committed outcome. */ }
                });
                return result.Status == CommandStatus.Applied ? MechanicalRepairOutcome.Committed : outcome;
            }
            public void Dispose()
            {
                if (session == null) return;
                if (session.Commands.IsBusy) throw new InvalidOperationException("Cannot release repair during publication.");
                session.repairBindings.Remove(requirement.TargetId); session = null; Changed = null;
            }
        }
        public bool TrySetEquipment(PlayerEquipmentRecord next)
        {
            if (next == null || data.playerEquipment == null || IsCollecting || HasIntegrityFailure ||
                !Enum.IsDefined(typeof(PlayerEquipmentSlot), next.activeSlot) ||
                (next.additionalQuickUseItems != null && next.additionalQuickUseItems.Length != PlayerEquipmentRecord.QuickUseCount - 1) ||
                (next.weaponItemId != null && next.weaponItemId == next.toolItemId)) return false;
            if (next.separateStorage != data.playerEquipment.separateStorage ||
                next.weaponItemId != data.playerEquipment.weaponItemId || next.toolItemId != data.playerEquipment.toolItemId) return false;
            for (int i = 0; i < PlayerEquipmentRecord.QuickUseCount; i++)
            {
                var id = next.QuickUseItem(i);
                if (id != null && !HasCarriedItem(id)) return false;
            }
            PublishEquipment(next.Clone());
            return true;
        }

        public void PrepareEquipmentInventory(int normalCapacity, Func<string, PlayerEquipmentSlot> preferredSlot)
        {
            if (bindings.ContainsKey(PlayerInventoryId) || IsCollecting || normalCapacity < 1)
                throw new InvalidOperationException("Prepare equipment before binding inventory.");
            if (data.playerEquipment?.separateStorage == true) return;

            TryGetContainer(PlayerInventoryId, out var carried);
            TryGetContainer(PlayerHotbarId, out var legacy);
            carried ??= new ContainerSaveData();
            bool hasAssignments = data.playerEquipment != null;
            var equipment = data.playerEquipment?.Clone() ?? new PlayerEquipmentRecord();
            var gear = new ContainerSaveData();
            var occupied = new Dictionary<int, ContainerSlotSaveData>();
            foreach (var slot in carried.slots)
            {
                if (!hasAssignments && slot.index >= normalCapacity)
                    throw new InvalidDataException("Legacy carried storage exceeds its authored capacity.");
                if (!catalog.TryGet(slot.itemId, out var item))
                    throw new InvalidDataException("Missing carried item.");
                slot.itemId = item.ItemId;
                occupied.Add(slot.index, slot);
            }
            if (hasAssignments)
            {
                equipment.weaponItemId = ExtractEquipped(carried, occupied, gear, equipment.weaponItemId, 0);
                equipment.toolItemId = ExtractEquipped(carried, occupied, gear, equipment.toolItemId, 1);
            }

            if (legacy != null)
            {
                legacy.slots.Sort((a, b) => a.index.CompareTo(b.index));
                int overflow = normalCapacity;
                foreach (int index in occupied.Keys) overflow = Math.Max(overflow, checked(index + 1));
                foreach (var slot in legacy.slots)
                {
                    if (!catalog.TryGet(slot.itemId, out var item))
                        throw new InvalidDataException("Missing legacy hotbar item.");
                    int remaining = slot.quantity;
                    if (!hasAssignments && equipment.activeSlot == PlayerEquipmentSlot.None)
                    {
                        equipment.activeSlot = preferredSlot(item.ItemId);
                        if (equipment.activeSlot != PlayerEquipmentSlot.None)
                        {
                            bool weapon = equipment.activeSlot == PlayerEquipmentSlot.Weapon;
                            if (weapon) equipment.weaponItemId = item.ItemId;
                            else equipment.toolItemId = item.ItemId;
                            gear.slots.Add(new ContainerSlotSaveData { index = weapon ? 0 : 1, itemId = item.ItemId, quantity = 1 });
                            remaining--;
                        }
                    }
                    for (int i = 0; i < normalCapacity && remaining > 0; i++)
                    {
                        if (!occupied.TryGetValue(i, out var existing) || existing.itemId != item.ItemId) continue;
                        int count = Math.Min(remaining, Math.Max(0, item.MaxStackSize - existing.quantity));
                        existing.quantity += count;
                        remaining -= count;
                    }
                    for (int i = 0; remaining > 0; i++)
                    {
                        if (i < normalCapacity && occupied.ContainsKey(i)) continue;
                        int index = i < normalCapacity ? i : overflow++;
                        int count = Math.Min(item.MaxStackSize, remaining);
                        var added = new ContainerSlotSaveData { index = index, itemId = item.ItemId, quantity = count };
                        carried.slots.Add(added);
                        occupied.Add(index, added);
                        remaining -= count;
                    }
                }
            }

            Store(PlayerInventoryId, carried);
            // Keep the existing ownership container so survivor handoffs collect equipped gear too.
            Store(PlayerHotbarId, gear);
            equipment.separateStorage = true;
            equipment.ResolveActiveSlot();
            data.playerEquipment = equipment;
            IsDirty = true;
            RequiresImmediateSave = true;
        }

        private static string ExtractEquipped(ContainerSaveData carried, Dictionary<int, ContainerSlotSaveData> occupied,
            ContainerSaveData gear, string id, int destination)
        {
            if (id == null) return null;
            ContainerSlotSaveData source = null;
            foreach (var slot in carried.slots)
                if (slot.itemId == id && (source == null || slot.index < source.index)) source = slot;
            if (source == null) return null;
            if (--source.quantity == 0)
            {
                carried.slots.Remove(source);
                occupied.Remove(source.index);
            }
            gear.slots.Add(new ContainerSlotSaveData { index = destination, itemId = id, quantity = 1 });
            return id;
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
            return Commands.InitializeStartingLoadout(inventory, hotbar, grants, selectedSlot, id =>
            {
                data.startingLoadoutInitialized = true;
                if (data.playerEquipment != null)
                {
                    var next = data.playerEquipment.Clone();
                    next.weaponItemId = id;
                    next.activeSlot = PlayerEquipmentSlot.Weapon;
                    PublishEquipment(next);
                }
                IsDirty = true;
                RequiresImmediateSave = true;
            }, data.playerEquipment != null);
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
            int actualCapacity = capacity;
            bool equipmentInventory = id == PlayerInventoryId && data.playerEquipment != null;
            if (equipmentInventory && saved != null)
                foreach (var slot in saved.slots) actualCapacity = Math.Max(actualCapacity, checked(slot.index + 1));
            try { container = Commands.Register(id, actualCapacity, saved, filter, initialLoot, equipmentInventory ? capacity : null); }
            catch (InvalidDataException) { HasIntegrityFailure = true; throw; }
            var binding = new ContainerBinding(this, id, container); bindings.Add(id, binding);
            var normalized = ContainerSaveSerializer.Capture(container);
            if (!existing || !Equivalent(saved, normalized)) IsDirty = true;
            Store(id, normalized);
            if (equipmentInventory || id == PlayerHotbarId) ReconcileEquipment();
            return binding;
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
                startingLoadoutInitialized = source.startingLoadoutInitialized, playerEquipment = source.playerEquipment?.Clone(),
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
