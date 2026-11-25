using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;

namespace SOLITUDE.Tests
{
    public class SurvivorEquipmentHandoffTests
    {
        private ContainerCommandService commands;
        private Container inventory, hotbar, recovery;
        private Guid transition;

        [SetUp] public void SetUp()
        {
            commands = new ContainerCommandService(new ItemCatalog(new[]
            { new ItemSpec("fuse", true, 10), new ItemSpec("tool", false, 1), new ItemSpec("food", true, 5) }));
            inventory = commands.Register("player.inventory", 4);
            hotbar = commands.Register("player.hotbar", 2);
            recovery = commands.Register("recovery.a", 6);
            transition = Guid.NewGuid();
            commands.Grant(inventory.Handle, "fuse", 17);
            commands.Grant(inventory.Handle, "tool", 1);
            commands.Grant(inventory.Handle, "food", 5);
            commands.Grant(hotbar.Handle, "fuse", 10);
            commands.Grant(hotbar.Handle, "tool", 1);
        }

        private ContainerHandoffPlan Prepare()
        {
            Assert.AreEqual(CommandStatus.Applied, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, recovery.Handle, out var plan));
            return plan;
        }
        private static string Snapshot(IContainerReader reader) => string.Join("|", reader.GetSlots().Select(s =>
            s.Address.Revision + ":" + s.Stack?.ItemId + ":" + s.Stack?.Quantity));
        private string AllState() => Snapshot(inventory) + "/" + Snapshot(hotbar) + "/" + Snapshot(recovery);

        [Test] public void BothFullSourcesMoveTogetherWithoutMergingOrChangingReaders()
        {
            var stacks = inventory.GetSlots().Concat(hotbar.GetSlots()).Select(s => s.Stack).ToArray();
            var before = AllState();
            var plan = Prepare();
            Assert.AreEqual(before, AllState(), "Preparation must not mutate ownership or revisions.");
            var result = commands.CommitHandoff(plan);
            Assert.AreEqual(CommandStatus.Applied, result.Status);
            Assert.IsTrue(inventory.GetSlots().All(s => s.IsEmpty));
            Assert.IsTrue(hotbar.GetSlots().All(s => s.IsEmpty));
            for (int i = 0; i < stacks.Length; i++) Assert.AreSame(stacks[i], recovery.GetSlot(i).Stack);
            Assert.AreEqual(27, recovery.GetSlots().Where(s => s.Stack.ItemId == "fuse").Sum(s => s.Stack.Quantity));
            Assert.AreSame(inventory, commands.Read(inventory.Handle));
            Assert.AreSame(hotbar, commands.Read(hotbar.Handle));
            Assert.AreEqual(transition, result.Receipt.TransitionId);
            Assert.AreEqual(recovery.Handle, result.Receipt.Destination);
        }

        [Test] public void EveryObserverSeesCompleteOwnershipAndReceiptWithReentrancyBlocked()
        {
            var plan = Prepare(); var errors = new List<Exception>(); int completed = 0, slots = 0;
            commands.ObserverError += errors.Add;
            Action observe = () =>
            {
                Assert.IsTrue(inventory.GetSlots().All(s => s.IsEmpty));
                Assert.IsTrue(hotbar.GetSlots().All(s => s.IsEmpty));
                Assert.AreEqual(34, recovery.GetSlots().Sum(s => s.Stack.Quantity));
                Assert.IsNotNull(commands.FindHandoffReceipt(transition));
                Assert.AreEqual(CommandStatus.Busy, commands.CommitHandoff(plan).Status);
                Assert.AreEqual(CommandStatus.Busy, commands.PrepareHandoff(Guid.NewGuid(),
                    inventory.Handle, hotbar.Handle, recovery.Handle, out _));
                Assert.AreEqual(CommandStatus.Busy, commands.Grant(inventory.Handle, "tool", 1).Status);
            };
            commands.Completed += change => { completed++; Assert.AreEqual(3, change.Containers.Count); observe(); };
            inventory.SlotChanged += _ => { slots++; observe(); };
            hotbar.SlotChanged += _ => { slots++; observe(); };
            recovery.SlotChanged += _ => { slots++; observe(); };
            Assert.AreEqual(CommandStatus.Applied, commands.CommitHandoff(plan).Status);
            Assert.AreEqual(1, completed); Assert.AreEqual(12, slots); Assert.IsEmpty(errors);
        }

        [Test] public void ReceiptPreventsRepeatingConsumedTransitionEvenAfterNewEquipmentArrives()
        {
            var plan = Prepare(); var alternative = Prepare();
            var receipt = commands.CommitHandoff(plan).Receipt;
            commands.Grant(inventory.Handle, "tool", 1);
            var before = AllState(); int notifications = 0; commands.Completed += _ => notifications++;
            var retry = commands.CommitHandoff(alternative);
            Assert.AreEqual(CommandStatus.NoOp, retry.Status); Assert.AreSame(receipt, retry.Receipt);
            Assert.AreEqual(CommandStatus.NoOp, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, recovery.Handle, out var repeat));
            Assert.IsNull(repeat); Assert.AreEqual(before, AllState()); Assert.AreEqual(0, notifications);
            commands.Unregister(recovery.Handle);
            Assert.AreSame(receipt, commands.CommitHandoff(plan).Receipt, "Committed receipt survives target disposal.");
        }

        [Test] public void InvalidAndInsufficientTargetsLeaveAllOwnersUnchanged()
        {
            var tiny = commands.Register("tiny", 5);
            var occupied = commands.Register("occupied", 6); commands.Grant(occupied.Handle, "food", 1);
            var filtered = commands.Register("filtered", 6, filter: item => item.ItemId != "tool");
            var before = AllState();
            Assert.AreEqual(CommandStatus.InsufficientSpace, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, tiny.Handle, out _));
            Assert.AreEqual(CommandStatus.Rejected, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, occupied.Handle, out _));
            Assert.AreEqual(CommandStatus.Rejected, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, filtered.Handle, out _));
            Assert.AreEqual(CommandStatus.Invalid, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, inventory.Handle, out _));
            Assert.AreEqual(CommandStatus.Invalid, commands.PrepareHandoff(transition,
                inventory.Handle, inventory.Handle, recovery.Handle, out _));
            Assert.AreEqual(CommandStatus.Invalid, commands.PrepareHandoff(Guid.Empty,
                inventory.Handle, hotbar.Handle, recovery.Handle, out _));
            Assert.AreEqual(CommandStatus.Unavailable, commands.PrepareHandoff(transition,
                inventory.Handle, hotbar.Handle, default, out _));
            Assert.AreEqual(CommandStatus.Invalid, commands.CommitHandoff(null).Status);
            Assert.AreEqual(before, AllState()); Assert.IsNull(commands.FindHandoffReceipt(transition));
            Assert.IsTrue(tiny.GetSlots().All(s => s.IsEmpty));
            Assert.IsTrue(filtered.GetSlots().All(s => s.IsEmpty));
            Assert.AreEqual(1, occupied.GetSlot(0).Stack.Quantity);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ChangedThenRestoredSlotsRejectStalePlansWithoutMutation(int ownerIndex)
        {
            var plan = Prepare(); var owner = new[] { inventory, hotbar, recovery }[ownerIndex];
            if (owner == recovery)
            { commands.Grant(owner.Handle, "food", 1); commands.Remove(owner.GetSlot(0).Address, 1); }
            else
            { commands.Remove(owner.GetSlot(0).Address, 1); commands.Grant(owner.Handle, "fuse", 1); }
            var before = AllState();
            Assert.AreEqual(CommandStatus.Stale, commands.CommitHandoff(plan).Status);
            Assert.AreEqual(before, AllState()); Assert.IsNull(commands.FindHandoffReceipt(transition));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ReplacedOwnerGenerationRejectsPreparedHandoff(int ownerIndex)
        {
            var plan = Prepare(); var owner = new[] { inventory, hotbar, recovery }[ownerIndex];
            commands.Unregister(owner.Handle); var replacement = commands.Register(owner.Handle.Id, owner.Capacity);
            var before = AllState();
            Assert.AreEqual(CommandStatus.Unavailable, commands.CommitHandoff(plan).Status);
            Assert.AreEqual(before, AllState()); Assert.IsTrue(replacement.GetSlots().All(s => s.IsEmpty));
            Assert.IsNull(commands.FindHandoffReceipt(transition));
        }

        [Test] public void ResizeRejectsPreparedHandoffAndPlanCannotCrossServices()
        {
            var plan = Prepare(); commands.Resize(recovery.Handle, 7); var before = AllState();
            Assert.AreEqual(CommandStatus.Stale, commands.CommitHandoff(plan).Status);
            var other = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("fuse", true, 10) }));
            Assert.AreEqual(CommandStatus.Invalid, other.CommitHandoff(plan).Status);
            Assert.AreEqual(before, AllState());
        }

        [Test] public void ThrowingObserversCannotLoseReceiptOrSuppressOtherNotifications()
        {
            var plan = Prepare(); int errors = 0, observed = 0;
            commands.ObserverError += _ => errors++;
            commands.Completed += _ => throw new Exception("observer");
            recovery.SlotChanged += _ => throw new Exception("view");
            recovery.SlotChanged += _ => observed++;
            var result = commands.CommitHandoff(plan);
            Assert.AreEqual(CommandStatus.Applied, result.Status);
            Assert.AreEqual(7, errors); Assert.AreEqual(6, observed);
            Assert.AreSame(result.Receipt, commands.FindHandoffReceipt(transition));
            Assert.AreEqual(CommandStatus.NoOp, commands.CommitHandoff(plan).Status);
            Assert.AreEqual(34, recovery.GetSlots().Sum(s => s.Stack.Quantity));
        }

        [Test] public void EmptyZeroCapacitySourcesCanCommitOnceWithoutItems()
        {
            var first = commands.Register("empty.first", 0); var second = commands.Register("empty.second", 0);
            var target = commands.Register("empty.target", 0);
            Assert.AreEqual(CommandStatus.Applied, commands.PrepareHandoff(transition,
                first.Handle, second.Handle, target.Handle, out var plan));
            Assert.AreEqual(CommandStatus.Applied, commands.CommitHandoff(plan).Status);
            Assert.AreEqual(CommandStatus.NoOp, commands.CommitHandoff(plan).Status);
            Assert.IsEmpty(target.GetSlots());
        }
    }
}
