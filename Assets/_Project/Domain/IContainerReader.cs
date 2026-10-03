using System;
using System.Collections.Generic;
using SOLITUDE.Items;
namespace SOLITUDE.Containers
{
    public readonly struct ContainerHandle : IEquatable<ContainerHandle>
    {
        public string Id { get; }
        public Guid Generation { get; }
        internal ContainerHandle(string id) { Id = id; Generation = Guid.NewGuid(); }
        public bool Equals(ContainerHandle other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object obj) => obj is ContainerHandle other && Equals(other);
        public override int GetHashCode() => Generation.GetHashCode();
    }
    public readonly struct SlotAddress
    {
        public ContainerHandle Container { get; }
        public int Index { get; }
        public long Revision { get; }
        internal SlotAddress(ContainerHandle container, int index, long revision)
        { Container = container; Index = index; Revision = revision; }
    }
    public sealed class SlotSnapshot
    {
        public SlotAddress Address { get; }
        public ItemStack Stack { get; }
        public ItemSpec Definition => Stack?.Definition;
        public bool IsEmpty => Stack == null;
        internal SlotSnapshot(SlotAddress address, ItemStack stack) { Address = address; Stack = stack; }
    }
    public interface IContainerReader
    {
        ContainerHandle Handle { get; }
        bool IsAvailable { get; }
        int Capacity { get; }
        event Action<int> SlotChanged;
        event Action CapacityChanged;
        SlotSnapshot GetSlot(int index);
        IReadOnlyList<SlotSnapshot> GetSlots();
        bool CanAccept(ItemSpec item);
    }
    public interface ITransferCommands
    {
        IContainerReader Read(ContainerHandle handle);
        CommandResult Transfer(SlotAddress source, SlotAddress target);
    }
    public interface IGrantCommands { CommandResult Grant(ContainerHandle container, string itemId, int quantity); }
    public enum CommandStatus { Invalid, InvalidQuantity, InvalidSlot, UnknownItem, Applied, PartialMerged, NoOp, Unavailable, Stale, Rejected, InsufficientSpace, InsufficientQuantity, Busy }
    public readonly struct CommandResult
    {
        public CommandStatus Status { get; }
        public int Moved { get; }
        public int Remaining { get; }
        public IReadOnlyList<SlotAddress> Addresses { get; }
        public bool Succeeded => Status == CommandStatus.Applied || Status == CommandStatus.PartialMerged || Status == CommandStatus.NoOp;
        internal CommandResult(CommandStatus status, int moved = 0, int remaining = 0, IReadOnlyList<SlotAddress> addresses = null)
        { Status = status; Moved = moved; Remaining = remaining; Addresses = addresses ?? Array.AsReadOnly(new SlotAddress[0]); }
    }
}
