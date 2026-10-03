using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Tests
{
    public class ContainerBoundaryTests
    {
        private ItemCatalog catalog;
        private ContainerSaveSession session;
        private Container a, b;
        private ContainerCommandService commands => session.Commands;
        [SetUp] public void SetUp()
        {
            catalog = new ItemCatalog(new[] { new ItemSpec("a", true, 10), new ItemSpec("b", true, 10), new ItemSpec("single", false, 999) });
            session = new ContainerSaveSession(new SaveGameData(), catalog);
            a = session.Register("a", 4).Container; b = session.Register("b", 4).Container;
            session.MarkSaved();
        }
        private static ContainerSaveData Saved(params (int index, string id, int quantity)[] values)
        {
            var result = new ContainerSaveData();
            foreach (var value in values) result.slots.Add(new ContainerSlotSaveData { index = value.index, itemId = value.id, quantity = value.quantity });
            return result;
        }
        [Test] public void InvalidCatalogAndLootAuthoringAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new ItemSpec("", true, 10));
            Assert.Throws<ArgumentException>(() => new ItemSpec("a", true, 0));
            Assert.Throws<ArgumentException>(() => new ItemCatalog(new[] { new ItemSpec("a", true, 1), new ItemSpec("a", true, 2) }));
            Assert.Throws<ArgumentException>(() => new ItemCatalog(new ItemSpec[] { null }));
            Assert.Throws<ArgumentException>(() => new RuntimeLootTable(catalog, new[] { new LootEntry("unknown", 1, 1, 1) }, 1, 1));
            Assert.Throws<ArgumentException>(() => new RuntimeLootTable(catalog, new[] { new LootEntry("a", float.NaN, 1, 1) }, 1, 1));
        }
        [Test] public void InvalidGrantsAndRemovalsLeaveRevisionsAndDirtyStateUnchanged()
        {
            int callbacks = 0; a.SlotChanged += _ => callbacks++;
            var before = a.GetSlot(0).Address;
            foreach (int quantity in new[] { 0, -1, int.MaxValue }) Assert.IsFalse(commands.Grant(a.Handle, "a", quantity).Succeeded);
            Assert.IsFalse(commands.Grant(a.Handle, "unknown", 1).Succeeded);
            Assert.IsFalse(commands.Remove(before, -1).Succeeded);
            Assert.IsFalse(commands.Remove(before, 1).Succeeded);
            Assert.AreEqual(before.Revision, a.GetSlot(0).Address.Revision);
            Assert.AreEqual(0, callbacks); Assert.IsFalse(session.IsDirty);
        }
        [Test] public void AcceptancePolicyIsFrozenAndPlanningRejectsReentrantCommands()
        {
            bool allow = true; CommandStatus reentrant = CommandStatus.Invalid;
            var owner = commands.Register("policy", 1, filter: item =>
            {
                reentrant = commands.Grant(a.Handle, "a", 1).Status;
                return allow && item.ItemId == "a";
            });
            Assert.AreEqual(CommandStatus.Busy, reentrant);
            Assert.IsTrue(a.GetSlot(0).IsEmpty);
            allow = false;
            Assert.IsTrue(commands.Grant(owner.Handle, "a", 1).Succeeded);
            Assert.AreEqual(CommandStatus.Rejected, commands.Grant(owner.Handle, "b", 1).Status);
        }
        [Test] public void FailedInitialLootNeverPublishesAnOwner()
        {
            Assert.Throws<ArgumentException>(() => session.Register("invalid.loot", 2, initialLoot: new[] { new PickupReward("a", 1), new PickupReward("unknown", 1) }));
            Assert.IsNull(commands.Find("invalid.loot")); Assert.IsFalse(session.IsDirty);
            using var valid = session.Register("invalid.loot", 2, initialLoot: new[] { new PickupReward("a", 1) });
            Assert.AreEqual(1, valid.Container.GetSlot(0).Stack.Quantity);
        }
        [Test] public void PartialMergeReportsRemainderAndCommittedAddresses()
        {
            commands.Grant(a.Handle, "a", 5); commands.Grant(b.Handle, "a", 8);
            int transactions = 0; ContainerChangeSet change = null;
            commands.Completed += value => { transactions++; change = value; };
            var result = commands.Transfer(a.GetSlot(0).Address, b.GetSlot(0).Address);
            Assert.AreEqual(CommandStatus.PartialMerged, result.Status); Assert.AreEqual(2, result.Moved); Assert.AreEqual(3, result.Remaining);
            Assert.AreEqual(1, transactions); Assert.AreEqual(2, change.Slots.Count); Assert.AreEqual(2, result.Addresses.Count);
            Assert.AreEqual(a.GetSlot(0).Address.Revision, result.Addresses.Single(x => x.Container.Equals(a.Handle)).Revision);
        }
        [Test] public void NonstackableGrantUsesOnePerSlotAndIsAtomic()
        {
            Assert.IsTrue(commands.Grant(a.Handle, "single", 3).Succeeded);
            Assert.AreEqual(3, a.GetSlots().Count(s => !s.IsEmpty));
            Assert.IsTrue(a.GetSlots().Where(s => !s.IsEmpty).All(s => s.Stack.Quantity == 1 && s.Stack.MaxStack == 1));
            session.MarkSaved();
            Assert.IsFalse(commands.Grant(a.Handle, "single", 2).Succeeded);
            Assert.IsFalse(session.IsDirty); Assert.IsTrue(a.GetSlot(3).IsEmpty);
        }
        [Test] public void ObserversSeeBothCommittedSidesAndCannotReenterOrCapture()
        {
            commands.Grant(a.Handle, "a", 7); commands.Grant(b.Handle, "b", 3); session.MarkSaved();
            int notifications = 0;
            var observerErrors = new List<Exception>(); commands.ObserverError += observerErrors.Add;
            Action<int> observer = _ =>
            {
                notifications++;
                Assert.AreEqual("b", a.GetSlot(0).Stack.ItemId); Assert.AreEqual("a", b.GetSlot(0).Stack.ItemId);
                Assert.IsTrue(session.IsDirty);
                Assert.AreEqual(CommandStatus.Busy, commands.Grant(a.Handle, "a", 1).Status);
                Assert.Throws<InvalidOperationException>(() => session.Capture());
                Assert.Throws<InvalidOperationException>(() => session.MarkSaved());
            };
            a.SlotChanged += observer; b.SlotChanged += observer;
            Assert.IsTrue(commands.Transfer(a.GetSlot(0).Address, b.GetSlot(0).Address).Succeeded);
            Assert.AreEqual(2, notifications); Assert.IsEmpty(observerErrors);
        }
        [Test] public void ThrowingObserverDoesNotSuppressOtherObserversOrPersistence()
        {
            int observed = 0, errors = 0;
            commands.ObserverError += _ => { errors++; throw new Exception("reporter"); };
            commands.Completed += _ => throw new Exception("completed");
            a.SlotChanged += _ => throw new Exception("view");
            a.SlotChanged += _ => observed++;
            Assert.IsTrue(commands.Grant(a.Handle, "a", 12).Succeeded);
            Assert.AreEqual(2, observed); Assert.AreEqual(3, errors); Assert.IsTrue(session.IsDirty);
            Assert.AreEqual(12, session.Capture().containers.Single(x => x.saveableId == "a").state.slots.Sum(x => x.quantity));
        }
        [Test] public void SnapshotsAndDtosCannotMutateLiveState()
        {
            commands.Grant(a.Handle, "a", 3);
            var slots = a.GetSlots(); var original = slots[0];
            Assert.Throws<NotSupportedException>(() => ((IList<SlotSnapshot>)slots)[0] = null);
            var dto = session.Capture(); dto.containers.Single(x => x.saveableId == "a").state.slots[0].quantity = 999;
            Assert.AreEqual(3, a.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(3, session.Capture().containers.Single(x => x.saveableId == "a").state.slots[0].quantity);
            commands.Remove(a.GetSlot(0).Address, 1); Assert.AreEqual(3, original.Stack.Quantity);
        }
        [Test] public void StaleSourceAndTargetAddressesCannotChangeOwnership()
        {
            commands.Grant(a.Handle, "a", 3); var source = a.GetSlot(0).Address; var target = b.GetSlot(0).Address;
            commands.Remove(source, 1);
            Assert.AreEqual(CommandStatus.Stale, commands.Transfer(source, target).Status);
            source = a.GetSlot(0).Address; commands.Grant(b.Handle, "b", 1);
            Assert.AreEqual(CommandStatus.Stale, commands.Transfer(source, target).Status);
            Assert.AreEqual(2, a.GetSlot(0).Stack.Quantity);
        }
        [Test] public void RegistrationGenerationPreventsOldHandleFromAddressingReplacement()
        {
            var binding = session.Register("reload", 1); var old = binding.Container.Handle;
            commands.Grant(old, "a", 1); var stale = binding.Container.GetSlot(0).Address;
            binding.Dispose(); using var replacement = session.Register("reload", 1);
            Assert.IsFalse(old.Equals(replacement.Container.Handle)); Assert.IsNull(commands.Read(old));
            Assert.AreEqual(CommandStatus.Unavailable, commands.Transfer(stale, a.GetSlot(0).Address).Status);
            Assert.AreEqual(1, replacement.Container.GetSlot(0).Stack.Quantity);
        }
        [Test] public void ResizeRefusesTruncationAndRegrowthDoesNotReviveOldAddresses()
        {
            commands.Restore(a.Handle, Saved((3, "a", 1))); session.MarkSaved();
            var old = a.GetSlot(3).Address;
            Assert.AreEqual(CommandStatus.InsufficientSpace, commands.Resize(a.Handle, 3).Status); Assert.IsFalse(session.IsDirty);
            commands.Remove(old, 1); old = a.GetSlot(3).Address;
            commands.Resize(a.Handle, 2); commands.Resize(a.Handle, 4);
            Assert.AreEqual(CommandStatus.Stale, commands.Transfer(b.GetSlot(0).Address, old).Status);
            Assert.IsFalse(commands.Resize(a.Handle, -1).Succeeded);
        }
        [Test] public void RestoreResizeAndUnregisterCancelSourceGesture()
        {
            commands.Grant(a.Handle, "a", 1); var drag = new ContainerInteractionController(commands);
            drag.BeginDrag(a.GetSlot(0)); commands.Restore(a.Handle, Saved((0, "a", 1))); Assert.IsFalse(drag.IsHolding);
            drag.BeginDrag(a.GetSlot(0)); commands.Resize(a.Handle, 5); Assert.IsFalse(drag.IsHolding);
            drag.BeginDrag(a.GetSlot(0)); commands.Unregister(a.Handle); Assert.IsFalse(drag.IsHolding);
        }
        [Test] public void OversizedRestorePreservesAnchorsAndSplitsDeterministically()
        {
            var state = Saved((2, "b", 4), (0, "a", 14), (1, "a", 3));
            Assert.IsTrue(commands.Restore(a.Handle, state).Succeeded);
            CollectionAssert.AreEqual(new[] { 10, 7, 4, 0 }, a.GetSlots().Select(s => s.Stack?.Quantity ?? 0).ToArray());
            Assert.AreEqual("b", a.GetSlot(2).Stack.ItemId);
            var normalized = ContainerSaveSerializer.Capture(a);
            Assert.IsTrue(commands.Restore(a.Handle, normalized).Succeeded);
            CollectionAssert.AreEqual(new[] { 10, 7, 4, 0 }, a.GetSlots().Select(s => s.Stack?.Quantity ?? 0).ToArray());
            Assert.AreEqual(14, state.slots.Single(s => s.index == 0).quantity);
        }
        [Test] public void FormerlyStackableItemsSplitWithoutLosingReservedSlots()
        {
            Assert.IsTrue(commands.Restore(a.Handle, Saved((0, "single", 3), (2, "b", 2))).Succeeded);
            CollectionAssert.AreEqual(new[] { "single", "single", "b", "single" }, a.GetSlots().Select(s => s.Stack.ItemId).ToArray());
        }
        [Test] public void FailedRestoreDoesNotReplaceLiveStateOrDirtySession()
        {
            commands.Grant(a.Handle, "b", 1); session.MarkSaved(); var revision = a.GetSlot(0).Address.Revision;
            foreach (var invalid in new[] { Saved((0, "a", 41)), Saved((0, "unknown", 1)), Saved((0, "a", 0)), Saved((4, "a", 1)), Saved((0, "a", 1), (0, "b", 1)) })
                Assert.IsFalse(commands.Restore(a.Handle, invalid).Succeeded);
            Assert.AreEqual("b", a.GetSlot(0).Stack.ItemId); Assert.AreEqual(revision, a.GetSlot(0).Address.Revision); Assert.IsFalse(session.IsDirty);
        }
        [Test] public void FailedOwnerRestorePreservesUnloadedRecordAndBlocksAutosave()
        {
            var data = new SaveGameData(); data.containers.Add(new ContainerSaveRecord { saveableId = "small", state = Saved((0, "a", 25)) });
            var loaded = new ContainerSaveSession(data, catalog);
            Assert.Throws<InvalidDataException>(() => loaded.Register("small", 2));
            Assert.IsTrue(loaded.HasIntegrityFailure); Assert.IsNull(loaded.Commands.Find("small")); Assert.IsFalse(loaded.IsDirty);
            Assert.AreEqual(25, loaded.Capture().containers[0].state.slots[0].quantity); Assert.AreEqual(25, data.containers[0].state.slots[0].quantity);
            using var fixedOwner = loaded.Register("small", 3); Assert.AreEqual(25, fixedOwner.Container.GetSlots().Sum(s => s.Stack.Quantity));
            // Only an explicit new save clears the unresolved failure flag in this session.
            loaded.Reset(1); Assert.IsFalse(loaded.HasIntegrityFailure);
        }
        [Test] public void NormalizationDirtiesOnlyChangedSavedRecords()
        {
            var data = new SaveGameData(); data.containers.Add(new ContainerSaveRecord { saveableId = "saved", state = Saved((0, "a", 14)) });
            var first = new ContainerSaveSession(data, catalog); first.Register("saved", 3); Assert.IsTrue(first.IsDirty);
            var second = new ContainerSaveSession(first.Capture(), catalog); second.Register("saved", 3); Assert.IsFalse(second.IsDirty);
        }
        [Test] public void InvalidUnloadedSchemaFailsBeforeAnyOwnerIsPublished()
        {
            foreach (var invalid in new[] { Saved((0, "unknown", 1)), Saved((0, "a", -1)), Saved((0, "a", 1), (0, "b", 1)) })
            {
                var data = new SaveGameData(); data.containers.Add(new ContainerSaveRecord { saveableId = "absent", state = invalid });
                Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(data, catalog));
            }
            var duplicate = new SaveGameData(); duplicate.containers.Add(new ContainerSaveRecord { saveableId = "x", state = Saved() }); duplicate.containers.Add(new ContainerSaveRecord { saveableId = "x", state = Saved() });
            Assert.Throws<InvalidDataException>(() => new ContainerSaveSession(duplicate, catalog));
        }
        [Test] public void RejectedLootCandidateCannotPolluteLaterCandidateCapacity()
        {
            var tiny = commands.Register("tiny", 1);
            var result = commands.InitializeLoot(tiny.Handle, new[] { new PickupReward("a", 11), new PickupReward("b", 1) });
            Assert.IsTrue(result.Succeeded); Assert.AreEqual("b", tiny.GetSlot(0).Stack.ItemId); Assert.AreEqual(1, tiny.GetSlot(0).Stack.Quantity);
            var other = commands.Register("other", 1);
            Assert.IsFalse(commands.InitializeLoot(other.Handle, new[] { new PickupReward("a", 1), new PickupReward("unknown", 1) }).Succeeded);
            Assert.IsTrue(other.GetSlot(0).IsEmpty);
        }
        [Test] public void CompiledLootPreservesSeededAlgorithmAndCandidateOrder()
        {
            var entries = new[] { new LootEntry("a", 2, 1, 5), new LootEntry("b", 1, 2, 4) };
            var table = new RuntimeLootTable(catalog, entries, 1, 5); entries[0] = new LootEntry("single", 99, 1, 1);
            for (int seed = 0; seed < 50; seed++)
            {
                var random = new System.Random(seed); int count = random.Next(1, 6); var rolled = table.Roll(new System.Random(seed)); Assert.AreEqual(count, rolled.Count);
                for (int i = 0; i < count; i++)
                {
                    bool first = random.NextDouble() * 3 <= 2;
                    int quantity = random.Next(first ? 1 : 2, first ? 6 : 5);
                    Assert.AreEqual(first ? "a" : "b", rolled[i].ItemId); Assert.AreEqual(quantity, rolled[i].Quantity);
                }
            }
        }
        [Test] public void NewWorldReleaseCannotRetainEmptyLockerRecordsOrOldOwners()
        {
            using var locker = session.Register("world.locker", 2, initialLoot: new[] { new PickupReward("a", 3) });
            var oldHandle = locker.Container.Handle;
            session.Reset(123, releaseOwners: true);
            Assert.IsFalse(locker.Container.IsAvailable); Assert.IsNull(commands.Read(oldHandle));
            Assert.IsEmpty(session.Capture().containers);
            locker.Dispose(); Assert.IsEmpty(session.Capture().containers);
            using var newLocker = session.Register("world.locker", 2, initialLoot: new[] { new PickupReward("b", 2) });
            Assert.AreEqual("b", newLocker.Container.GetSlot(0).Stack.ItemId);
            Assert.IsFalse(oldHandle.Equals(newLocker.Container.Handle));
        }
        [Test] public void NoOpsDoNotDirtyOrPublish()
        {
            commands.Grant(a.Handle, "a", 1); commands.Grant(b.Handle, "a", 10); session.MarkSaved(); int changes = 0;
            commands.Completed += _ => changes++;
            Assert.AreEqual(CommandStatus.NoOp, commands.Transfer(a.GetSlot(0).Address, a.GetSlot(0).Address).Status);
            Assert.AreEqual(CommandStatus.NoOp, commands.Transfer(a.GetSlot(0).Address, b.GetSlot(0).Address).Status);
            Assert.AreEqual(CommandStatus.NoOp, commands.Resize(a.Handle, 4).Status);
            Assert.AreEqual(0, changes); Assert.IsFalse(session.IsDirty);
        }
        [Test] public void DomainHasNoUnityReferencesOrPublicStorageWrites()
        {
            var assembly = typeof(ContainerCommandService).Assembly;
            Assert.IsFalse(assembly.GetReferencedAssemblies().Any(n => n.Name.StartsWith("Unity")));
            Assert.IsEmpty(typeof(Container).GetConstructors()); Assert.IsEmpty(typeof(ItemStack).GetConstructors());
            Assert.IsFalse(typeof(ItemStack).GetProperties().Any(p => p.SetMethod?.IsPublic == true));
            Assert.IsFalse(typeof(Container).GetMethods().Any(m => new[] { "Clear", "TryAdd", "TryAddAll", "TrySetSlot", "Resize" }.Contains(m.Name)));
            Assert.IsFalse(typeof(ITransferCommands).GetMethods().Any(m => m.Name == "Grant" || m.Name == "Remove" || m.Name == "Restore"));
        }
    }
}
