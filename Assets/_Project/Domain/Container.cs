using System;
using System.Collections.Generic;
using SOLITUDE.Items;
namespace SOLITUDE.Containers
{
    internal sealed class ContainerState
    {
        internal readonly ContainerHandle Handle;
        internal readonly HashSet<string> Accepted;
        internal ItemStack[] Slots;
        internal long[] Revisions;
        internal long Clock;
        internal bool Available = true;
        internal readonly Container Reader;
        internal ContainerState(string id, int capacity, HashSet<string> accepted)
        { Handle = new ContainerHandle(id); Accepted = accepted; Slots = new ItemStack[capacity]; Revisions = new long[capacity]; Reader = new Container(this); }
        internal bool Accepts(ItemSpec item) => item != null && (Accepted == null || Accepted.Contains(item.ItemId));
    }
    public sealed class Container : IContainerReader
    {
        private readonly ContainerState state;
        internal Container(ContainerState state) => this.state = state;
        public ContainerHandle Handle => state.Handle;
        public bool IsAvailable => state.Available;
        public int Capacity => state.Slots.Length;
        public event Action<int> SlotChanged;
        public event Action CapacityChanged;
        public bool CanAccept(ItemSpec item) => state.Accepts(item);
        public bool IsEmpty(int index) => GetSlot(index)?.IsEmpty ?? true;
        public SlotSnapshot GetSlot(int index) => !IsAvailable || index < 0 || index >= Capacity ? null :
            new SlotSnapshot(new SlotAddress(Handle, index, state.Revisions[index]), state.Slots[index]);
        public IReadOnlyList<SlotSnapshot> GetSlots()
        {
            var result = new SlotSnapshot[IsAvailable ? Capacity : 0];
            for (int i = 0; i < result.Length; i++) result[i] = GetSlot(i);
            return Array.AsReadOnly(result);
        }
        internal void PublishSlot(int index, Action<Exception> report) => Publish(SlotChanged, index, report);
        internal void PublishCapacity(Action<Exception> report)
        {
            if (CapacityChanged == null) return;
            foreach (Action observer in CapacityChanged.GetInvocationList())
                try { observer(); } catch (Exception error) { report(error); }
        }
        private static void Publish(Action<int> observers, int index, Action<Exception> report)
        {
            if (observers == null) return;
            foreach (Action<int> observer in observers.GetInvocationList())
                try { observer(index); } catch (Exception error) { report(error); }
        }
    }
}
