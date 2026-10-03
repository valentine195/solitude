using System;
using System.Collections.Generic;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
namespace SOLITUDE.Containers
{
    public sealed class ContainerChangeSet
    {
        public IReadOnlyList<ContainerHandle> Containers { get; }
        public IReadOnlyList<SlotAddress> Slots { get; }
        internal ContainerChangeSet(List<ContainerHandle> containers, IReadOnlyList<SlotAddress> slots)
        { Containers = containers.AsReadOnly(); Slots = slots; }
    }
    public sealed class ContainerCommandService : ITransferCommands, IGrantCommands
    {
        private readonly ItemCatalog catalog;
        private readonly Dictionary<string, ContainerState> owners = new Dictionary<string, ContainerState>(StringComparer.Ordinal);
        private bool executing, publishing;
        public bool IsBusy => executing || publishing;
        public event Action<ContainerChangeSet> Completed;
        public event Action<Exception> ObserverError;
        public ContainerCommandService(IItemCatalog catalog) => this.catalog = new ItemCatalog((catalog ?? throw new ArgumentNullException(nameof(catalog))).Items);
        public Container Register(string id, int capacity, ContainerSaveData saved = null, Func<ItemSpec, bool> filter = null, IReadOnlyList<PickupReward> initialLoot = null)
        {
            if (IsBusy) throw new InvalidOperationException("Command publication in progress.");
            executing = true;
            try { return RegisterCore(id, capacity, saved, filter, initialLoot); }
            finally { executing = false; }
        }
        private Container RegisterCore(string id, int capacity, ContainerSaveData saved, Func<ItemSpec, bool> filter, IReadOnlyList<PickupReward> initialLoot)
        {
            if (string.IsNullOrWhiteSpace(id) || capacity < 0) throw new ArgumentException("Invalid container identity or capacity.");
            if (owners.ContainsKey(id)) throw new InvalidOperationException("Container identity already registered: " + id);
            HashSet<string> accepted = null;
            if (filter != null)
            {
                accepted = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in catalog.Items) if (filter(item)) accepted.Add(item.ItemId);
            }
            var state = new ContainerState(id, capacity, accepted);
            if (saved != null)
            {
                var result = PlanRestore(state, saved, out var slots);
                if (result != CommandStatus.Applied) throw new System.IO.InvalidDataException("Cannot restore container '" + id + "': " + result);
                state.Slots = slots;
            }
            if (saved == null && initialLoot != null)
            {
                var status = PlanLoot(state, initialLoot, out var plan, out _);
                if (status != CommandStatus.Applied) throw new ArgumentException("Invalid initial loot: " + status);
                state.Slots = plan;
            }
            owners.Add(id, state);
            return state.Reader;
        }
        public IContainerReader Read(ContainerHandle handle) => Resolve(handle, out var state) ? state.Reader : null;
        public IContainerReader Find(string id) => id != null && owners.TryGetValue(id, out var state) ? state.Reader : null;
        public void Unregister(ContainerHandle handle)
        {
            if (IsBusy) throw new InvalidOperationException("Command publication in progress.");
            if (!Resolve(handle, out var state)) return;
            owners.Remove(handle.Id); state.Available = false;
            publishing = true;
            try { state.Reader.PublishCapacity(Report); } finally { publishing = false; }
        }
        private CommandResult Execute(Func<CommandResult> operation)
        {
            if (IsBusy) return new CommandResult(CommandStatus.Busy);
            executing = true;
            try { return operation(); } finally { executing = false; }
        }
        private bool Resolve(ContainerHandle handle, out ContainerState state)
        { state = null; return handle.Id != null && owners.TryGetValue(handle.Id, out state) && state.Handle.Equals(handle) && state.Available; }
        private CommandStatus Address(SlotAddress address, out ContainerState state)
        {
            if (!Resolve(address.Container, out state)) return CommandStatus.Unavailable;
            if (address.Index < 0 || address.Index >= state.Slots.Length) return CommandStatus.InvalidSlot;
            return state.Revisions[address.Index] == address.Revision ? CommandStatus.Applied : CommandStatus.Stale;
        }
        public CommandResult Grant(ContainerHandle container, string itemId, int quantity) => Grant(container, itemId, quantity, null);
        internal CommandResult Grant(ContainerHandle container, string itemId, int quantity, Action worldCommit) =>
            Execute(() => GrantCore(container, itemId, quantity, worldCommit));
        private CommandResult GrantCore(ContainerHandle container, string itemId, int quantity, Action worldCommit)
        {
            if (!Resolve(container, out var state)) return new CommandResult(CommandStatus.Unavailable);
            if (quantity < 1) return new CommandResult(CommandStatus.InvalidQuantity);
            if (!catalog.TryGet(itemId, out var item)) return new CommandResult(CommandStatus.UnknownItem);
            if (!state.Accepts(item)) return new CommandResult(CommandStatus.Rejected);
            var plan = (ItemStack[])state.Slots.Clone();
            if (!Add(plan, item, quantity)) return new CommandResult(CommandStatus.InsufficientSpace);
            Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan }, worldCommit);
            return new CommandResult(CommandStatus.Applied, quantity, addresses: lastChanges);
        }
        private static bool Add(ItemStack[] plan, ItemSpec item, int quantity)
        {
            for (int i = 0; i < plan.Length && quantity > 0; i++)
            {
                if (plan[i] == null || plan[i].ItemId != item.ItemId) continue;
                int amount = Math.Min(item.MaxStackSize - plan[i].Quantity, quantity);
                if (amount == 0) continue;
                plan[i] = new ItemStack(item, plan[i].Quantity + amount); quantity -= amount;
            }
            for (int i = 0; i < plan.Length && quantity > 0; i++)
            {
                if (plan[i] != null) continue;
                int amount = Math.Min(item.MaxStackSize, quantity);
                plan[i] = new ItemStack(item, amount); quantity -= amount;
            }
            return quantity == 0;
        }
        public CommandResult Remove(SlotAddress address, int quantity) => Remove(address, quantity, null);
        internal CommandResult Remove(SlotAddress address, int quantity, Action worldCommit) => Execute(() => RemoveCore(address, quantity, worldCommit));
        private CommandResult RemoveCore(SlotAddress address, int quantity, Action worldCommit)
        {
            var status = Address(address, out var state);
            if (status != CommandStatus.Applied) return new CommandResult(status);
            if (quantity < 1) return new CommandResult(CommandStatus.InvalidQuantity);
            var stack = state.Slots[address.Index];
            if (stack == null || quantity > stack.Quantity) return new CommandResult(CommandStatus.InsufficientQuantity);
            var plan = (ItemStack[])state.Slots.Clone();
            plan[address.Index] = quantity == stack.Quantity ? null : new ItemStack(stack.Definition, stack.Quantity - quantity);
            Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan }, worldCommit);
            return new CommandResult(CommandStatus.Applied, quantity, stack.Quantity - quantity, lastChanges);
        }
        public CommandResult Transfer(SlotAddress source, SlotAddress target) => Execute(() => TransferCore(source, target));
        private CommandResult TransferCore(SlotAddress source, SlotAddress target)
        {
            var status = Address(source, out var from);
            if (status != CommandStatus.Applied) return new CommandResult(status);
            status = Address(target, out var to);
            if (status != CommandStatus.Applied) return new CommandResult(status);
            if (from == to && source.Index == target.Index) return new CommandResult(CommandStatus.NoOp);
            var a = from.Slots[source.Index]; var b = to.Slots[target.Index];
            if (a == null) return new CommandResult(CommandStatus.NoOp);
            if (!to.Accepts(a.Definition) || (b != null && b.ItemId != a.ItemId && !from.Accepts(b.Definition)))
                return new CommandResult(CommandStatus.Rejected);
            int moved = a.Quantity; int remaining = 0;
            var fromPlan = (ItemStack[])from.Slots.Clone();
            var toPlan = from == to ? fromPlan : (ItemStack[])to.Slots.Clone();
            if (b != null && b.ItemId == a.ItemId)
            {
                moved = Math.Min(a.Quantity, b.MaxStack - b.Quantity); remaining = a.Quantity - moved;
                if (moved == 0) return new CommandResult(CommandStatus.NoOp, 0, remaining);
                toPlan[target.Index] = new ItemStack(b.Definition, b.Quantity + moved);
                fromPlan[source.Index] = remaining == 0 ? null : new ItemStack(a.Definition, remaining);
            }
            else { toPlan[target.Index] = a; fromPlan[source.Index] = b; }
            var plans = new Dictionary<ContainerState, ItemStack[]> { [from] = fromPlan };
            plans[to] = toPlan; Commit(plans);
            return new CommandResult(remaining > 0 ? CommandStatus.PartialMerged : CommandStatus.Applied, moved, remaining, lastChanges);
        }
        public CommandResult Resize(ContainerHandle handle, int capacity) => Execute(() => ResizeCore(handle, capacity));
        private CommandResult ResizeCore(ContainerHandle handle, int capacity)
        {
            if (!Resolve(handle, out var state)) return new CommandResult(CommandStatus.Unavailable);
            if (capacity < 0) return new CommandResult(CommandStatus.Invalid);
            if (capacity == state.Slots.Length) return new CommandResult(CommandStatus.NoOp);
            for (int i = capacity; i < state.Slots.Length; i++)
                if (state.Slots[i] != null) return new CommandResult(CommandStatus.InsufficientSpace);
            var plan = new ItemStack[capacity]; Array.Copy(state.Slots, plan, Math.Min(capacity, state.Slots.Length));
            Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan });
            return new CommandResult(CommandStatus.Applied, addresses: lastChanges);
        }
        public CommandResult Restore(ContainerHandle handle, ContainerSaveData saved) => Execute(() => RestoreCore(handle, saved));
        private CommandResult RestoreCore(ContainerHandle handle, ContainerSaveData saved)
        {
            if (!Resolve(handle, out var state)) return new CommandResult(CommandStatus.Unavailable);
            var status = PlanRestore(state, saved, out var plan);
            if (status != CommandStatus.Applied) return new CommandResult(status);
            Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan }, force: true);
            return new CommandResult(CommandStatus.Applied, addresses: lastChanges);
        }
        private CommandStatus PlanRestore(ContainerState state, ContainerSaveData saved, out ItemStack[] plan)
        {
            plan = new ItemStack[state.Slots.Length];
            if (saved?.slots == null) return CommandStatus.Invalid;
            var indices = new HashSet<int>(); var ordered = new List<ContainerSlotSaveData>();
            foreach (var slot in saved.slots)
            {
                if (slot == null || slot.index < 0 || slot.index >= plan.Length || !indices.Add(slot.index) || slot.quantity < 1 || !catalog.TryGet(slot.itemId, out var item)) return CommandStatus.Invalid;
                if (!state.Accepts(item)) return CommandStatus.Rejected;
                plan[slot.index] = new ItemStack(item, Math.Min(slot.quantity, item.MaxStackSize)); ordered.Add(slot);
            }
            ordered.Sort((a,b) => a.index.CompareTo(b.index));
            foreach (var slot in ordered)
            {
                var item = plan[slot.index].Definition;
                int overflow = slot.quantity - Math.Min(slot.quantity, item.MaxStackSize);
                if (overflow > 0 && !Add(plan, item, overflow)) return CommandStatus.InsufficientSpace;
            }
            return CommandStatus.Applied;
        }
        public CommandResult InitializeLoot(ContainerHandle handle, IReadOnlyList<PickupReward> candidates) => Execute(() => InitializeLootCore(handle, candidates));
        private CommandResult InitializeLootCore(ContainerHandle handle, IReadOnlyList<PickupReward> candidates)
        {
            if (!Resolve(handle, out var state)) return new CommandResult(CommandStatus.Unavailable);
            var status = PlanLoot(state, candidates, out var plan, out int accepted);
            if (status != CommandStatus.Applied) return new CommandResult(status);
            Commit(new Dictionary<ContainerState, ItemStack[]> { [state] = plan });
            return new CommandResult(accepted == 0 ? CommandStatus.NoOp : CommandStatus.Applied, addresses: lastChanges);
        }
        private CommandStatus PlanLoot(ContainerState state, IReadOnlyList<PickupReward> candidates, out ItemStack[] plan, out int accepted)
        {
            plan = (ItemStack[])state.Slots.Clone(); accepted = 0;
            if (candidates == null) return CommandStatus.Invalid;
            foreach (var candidate in candidates)
            {
                if (candidate.Quantity < 1 || !catalog.TryGet(candidate.ItemId, out var item)) return CommandStatus.Invalid;
                if (!state.Accepts(item)) return CommandStatus.Rejected;
                var proposed = (ItemStack[])plan.Clone();
                if (!Add(proposed, item, candidate.Quantity)) continue;
                plan = proposed; accepted++;
            }
            return CommandStatus.Applied;
        }
        internal void Reset(Action worldCommit)
        {
            if (IsBusy) throw new InvalidOperationException("Command publication in progress.");
            var plans = new Dictionary<ContainerState, ItemStack[]>();
            foreach (var state in owners.Values) plans.Add(state, new ItemStack[state.Slots.Length]);
            Commit(plans, worldCommit, true);
        }
        private sealed class Publication
        {
            internal ContainerState State; internal ItemStack[] Slots; internal long[] Revisions;
            internal long Clock; internal bool Resized; internal List<int> Changed;
        }
        private IReadOnlyList<SlotAddress> lastChanges = Array.AsReadOnly(new SlotAddress[0]);
        private void Commit(Dictionary<ContainerState, ItemStack[]> plans, Action worldCommit = null, bool force = false)
        {
            var publications = new List<Publication>(); var handles = new List<ContainerHandle>();
            foreach (var pair in plans)
            {
                var state = pair.Key; var slots = pair.Value;
                bool resized = slots.Length != state.Slots.Length;
                var changed = new List<int>(); var revisions = new long[slots.Length]; long clock = state.Clock;
                for (int i = 0; i < slots.Length; i++)
                {
                    bool differs = force || resized || i >= state.Slots.Length || !Equal(state.Slots[i], slots[i]);
                    revisions[i] = differs ? checked(++clock) : state.Revisions[i];
                    if (differs) changed.Add(i);
                }
                if (!resized && changed.Count == 0 && !force) continue;
                publications.Add(new Publication { State = state, Slots = slots, Revisions = revisions, Clock = clock, Resized = resized, Changed = changed });
                handles.Add(state.Handle);
            }
            var addresses = new List<SlotAddress>();
            foreach (var p in publications) foreach (int index in p.Changed)
                addresses.Add(new SlotAddress(p.State.Handle, index, p.Revisions[index]));
            lastChanges = addresses.AsReadOnly();
            if (publications.Count == 0 && worldCommit == null) return;
            publishing = true;
            try
            {
                foreach (var p in publications) { p.State.Slots = p.Slots; p.State.Revisions = p.Revisions; p.State.Clock = p.Clock; }
                worldCommit?.Invoke();
                var change = new ContainerChangeSet(handles, lastChanges);
                if (Completed != null)
                    foreach (Action<ContainerChangeSet> observer in Completed.GetInvocationList())
                        try { observer(change); } catch (Exception error) { Report(error); }
                foreach (var p in publications)
                {
                    if (p.Resized) p.State.Reader.PublishCapacity(Report);
                    foreach (int index in p.Changed) p.State.Reader.PublishSlot(index, Report);
                }
            }
            finally { publishing = false; }
        }
        private static bool Equal(ItemStack a, ItemStack b) => ReferenceEquals(a,b) || (a != null && b != null && a.ItemId == b.ItemId && a.Quantity == b.Quantity);
        private void Report(Exception error)
        {
            if (ObserverError == null) return;
            foreach (Action<Exception> observer in ObserverError.GetInvocationList())
                try { observer(error); } catch { /* Reporting cannot interrupt publication. */ }
        }
    }
}
