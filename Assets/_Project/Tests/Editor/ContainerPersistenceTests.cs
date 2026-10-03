using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using UnityEngine;

namespace SOLITUDE.Tests
{
    public class ContainerPersistenceTests
    {
        private ItemSpec battery;
        private ItemSpec oxygen;
        private Container source;
        private Container target;
        private ContainerInteractionController drag;

        private ItemCatalog catalog;
        private ContainerSaveSession session;
        private ContainerCommandService commands => session.Commands;
        [SetUp]
        public void SetUp()
        {
            battery = new ItemSpec("battery", true, 99);
            oxygen = new ItemSpec("oxygen", true, 99);
            catalog = new ItemCatalog(new[] { battery, oxygen });
            session = Session();
            source = session.Register("locker", 2).Container;
            target = session.Register(ContainerSaveSession.PlayerInventoryId, 2).Container;
            drag = new ContainerInteractionController(commands);
        }
        [TearDown] public void TearDown() => drag.Cancel();
        private ContainerSaveSession Session(SaveGameData data = null) => new ContainerSaveSession(data ?? new SaveGameData(), catalog);
        private void Grant(Container owner, ItemSpec item, int quantity = 1) => Assert.IsTrue(commands.Grant(owner.Handle, item.ItemId, quantity).Succeeded);
        private static ContainerSaveData Saved(params (int index, string id, int quantity)[] entries)
        {
            var state = new ContainerSaveData();
            foreach (var entry in entries) state.slots.Add(new ContainerSlotSaveData { index = entry.index, itemId = entry.id, quantity = entry.quantity });
            return state;
        }
        private static SaveGameData RoundTrip(ContainerSaveSession session) =>
            JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(session.Capture()));

        [Test]
        public void PreviewAndCancelKeepSourceOwnership()
        {
            Grant(source, battery, 3);
            var original = source.GetSlot(0).Stack;
            drag.BeginDrag(source.GetSlot(0));
            Assert.AreSame(original, source.GetSlot(0).Stack);
            Assert.AreSame(original, drag.HeldItem);
            drag.Cancel();
            Assert.AreSame(original, source.GetSlot(0).Stack);
            Assert.IsFalse(drag.IsHolding);
        }

        [Test]
        public void SwapAndEndDragPreserveBothStacks()
        {
            Grant(source, battery, 3);
            Grant(target, oxygen, 5);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            drag.Cancel(); // IEndDragHandler always runs after OnDrop.
            Assert.AreSame(oxygen, source.GetSlot(0).Definition);
            Assert.AreEqual(5, source.GetSlot(0).Stack.Quantity);
            Assert.AreSame(battery, target.GetSlot(0).Definition);
            Assert.AreEqual(3, target.GetSlot(0).Stack.Quantity);
            Assert.IsFalse(drag.IsHolding);
        }

        [Test]
        public void MoveToEmptySlotPreservesStack()
        {
            Grant(source, battery, 7);
            var stack = source.GetSlot(0).Stack;
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            Assert.IsTrue(source.GetSlot(0).IsEmpty);
            Assert.AreSame(stack, target.GetSlot(0).Stack);
        }

        [Test]
        public void PartialMergeLeavesRemainderInSource()
        {
            Grant(source, battery, 10);
            Grant(target, battery, 95);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            drag.Cancel();
            Assert.AreEqual(6, source.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(99, target.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void FullMergeClearsOnlySource()
        {
            Grant(source, battery, 4);
            Grant(target, battery, 95);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            Assert.IsTrue(source.GetSlot(0).IsEmpty);
            Assert.AreEqual(99, target.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void FullTargetLeavesSourceUntouched()
        {
            Grant(source, battery, 4);
            Grant(target, battery, 99);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            Assert.AreEqual(4, source.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(99, target.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void SwapChecksReverseAcceptanceBeforeMutating()
        {
            var restricted = commands.Register("restricted", 1, filter: item => item == battery);
            Grant(restricted, battery);
            Grant(target, oxygen);
            int rejections = 0;
            drag.Rejected += _ => rejections++;
            drag.BeginDrag(restricted.GetSlot(0));
            drag.Drop(target.GetSlot(0));
            Assert.AreSame(battery, restricted.GetSlot(0).Definition);
            Assert.AreSame(oxygen, target.GetSlot(0).Definition);
            Assert.AreEqual(1, rejections);
        }

        [Test]
        public void RejectedTargetAndSelfDropDoNotChangeContents()
        {
            var restricted = commands.Register("restricted", 1, filter: item => item == oxygen);
            Grant(source, battery);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(restricted.GetSlot(0));
            Assert.IsTrue(restricted.GetSlot(0).IsEmpty);
            drag.BeginDrag(source.GetSlot(0));
            drag.Drop(source.GetSlot(0));
            Assert.AreSame(battery, source.GetSlot(0).Definition);
        }

        [Test]
        public void ExternalReplacementInvalidatesGesture()
        {
            Grant(source, battery);
            drag.BeginDrag(source.GetSlot(0));
            commands.Restore(source.Handle, Saved((0, "oxygen", 1)));
            drag.Drop(target.GetSlot(0));
            Assert.AreSame(oxygen, source.GetSlot(0).Definition);
            Assert.IsTrue(target.GetSlot(0).IsEmpty);
            Assert.IsFalse(drag.IsHolding);
        }

        [Test]
        public void ExternalQuantityChangeInvalidatesGesture()
        {
            Grant(source, battery, 3);
            drag.BeginDrag(source.GetSlot(0));
            commands.Remove(source.GetSlot(0).Address, 1);
            Assert.IsFalse(drag.IsHolding);
            Assert.AreEqual(2, source.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void SavingDuringDragRetainsEveryItem()
        {
            Grant(source, battery, 3);
            drag.BeginDrag(source.GetSlot(0));
            var loaded = Session(RoundTrip(session));
            using var restoredBinding = loaded.Register("locker", 2);
            var restored = restoredBinding.Container;
            Assert.AreSame(battery, restored.GetSlot(0).Definition);
            Assert.AreEqual(3, restored.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void LockerInventoryAndHotbarRoundTripTogether()
        {
            using var hotbarBinding = session.Register(ContainerSaveSession.PlayerHotbarId, 9);
            var hotbar = hotbarBinding.Container;
            Grant(source, battery, 3); Grant(source, oxygen, 2);
            commands.Transfer(source.GetSlot(0).Address, target.GetSlot(1).Address);
            commands.Transfer(source.GetSlot(1).Address, hotbar.GetSlot(4).Address);
            var loaded = Session(RoundTrip(session));
            using var l = loaded.Register("locker", 2);
            using var i = loaded.Register(ContainerSaveSession.PlayerInventoryId, 2);
            using var h = loaded.Register(ContainerSaveSession.PlayerHotbarId, 9);
            var newLocker = l.Container; var newInventory = i.Container; var newHotbar = h.Container;
            Assert.IsTrue(newLocker.GetSlot(0).IsEmpty);
            Assert.IsTrue(newLocker.GetSlot(1).IsEmpty);
            Assert.AreEqual(3, newInventory.GetSlot(1).Stack.Quantity);
            Assert.AreEqual(2, newHotbar.GetSlot(4).Stack.Quantity);
            Assert.AreSame(oxygen, newHotbar.GetSlot(4).Definition);
        }

        [Test]
        public void VersionOneSaveKeepsLockerRecordsWhenAddingPlayer()
        {
            Grant(source, battery, 4);
            var data = new SaveGameData { version = 1, worldSeed = 42 };
            data.containers.Add(new ContainerSaveRecord { saveableId = "locker", state = ContainerSaveSerializer.Capture(source) });
            var session = Session(data);
            using var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            var saved = RoundTrip(session);
            Assert.AreEqual(SaveGameData.CurrentVersion, saved.version);
            Assert.AreEqual(42, saved.worldSeed);
            Assert.AreEqual(2, saved.containers.Count);
            Assert.AreEqual(4, saved.containers.Single(x => x.saveableId == "locker").state.slots[0].quantity);
        }

        [Test]
        public void UnregisterRetainsStateForSceneReload()
        {
            var other = Session();
            var binding = other.Register("locker", 2);
            other.Commands.Grant(binding.Container.Handle, "battery", 8);
            binding.Dispose(); binding.Dispose();
            Assert.IsFalse(binding.Container.IsAvailable);
            using var replacement = other.Register("locker", 2);
            Assert.AreEqual(8, replacement.Container.GetSlot(0).Stack.Quantity);
        }

        [Test]
        public void DuplicateIdentityAndDuplicateModelAreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => session.Register("locker", 2));
            Assert.Throws<InvalidOperationException>(() => commands.Register("locker", 2));
        }

        [Test]
        public void InvalidRestoreDoesNotClearLiveOrSavedContents()
        {
            Grant(source, oxygen, 2);
            var data = new SaveGameData();
            data.containers.Add(new ContainerSaveRecord { saveableId = "locker", state = new ContainerSaveData() });
            data.containers[0].state.slots.Add(new ContainerSlotSaveData { index = 0, itemId = "unknown", quantity = 1 });
            Assert.Throws<InvalidDataException>(() => Session(data));
            Assert.AreEqual(CommandStatus.Invalid, commands.Restore(source.Handle, data.containers[0].state).Status);
            Assert.AreSame(oxygen, source.GetSlot(0).Definition);
            Assert.AreEqual("unknown", data.containers[0].state.slots[0].itemId);
        }

        [Test]
        public void DuplicateSavedSlotsAreRejectedBeforeMutation()
        {
            Grant(source, oxygen);
            var saved = new ContainerSaveData();
            saved.slots.Add(new ContainerSlotSaveData { index = 0, itemId = "battery", quantity = 2 });
            saved.slots.Add(new ContainerSlotSaveData { index = 0, itemId = "oxygen", quantity = 2 });
            Assert.AreEqual(CommandStatus.Invalid, commands.Restore(source.Handle, saved).Status);
            Assert.AreSame(oxygen, source.GetSlot(0).Definition);
        }

        [Test]
        public void DomainChangesMarkSessionDirtyAndResetClearsLiveOwners()
        {
            session.Capture();
            session.MarkSaved();
            Assert.IsFalse(session.IsDirty);
            Grant(source, battery);
            Assert.IsTrue(session.IsDirty);
            session.Reset(73);
            Assert.IsTrue(source.GetSlot(0).IsEmpty);
            Assert.AreEqual(73, session.WorldSeed);
        }

        [Test]
        public void RepeatedTransfersConserveQuantities()
        {
            var slots = commands.Register("random", 8);
            commands.Restore(slots.Handle, Saved((0, "battery", 70), (1, "battery", 70), (2, "oxygen", 50), (3, "oxygen", 50)));
            var random = new System.Random(42);
            for (int n = 0; n < 500; n++)
            {
                drag.BeginDrag(slots.GetSlot(random.Next(8)));
                if (random.Next(3) != 0) drag.Drop(slots.GetSlot(random.Next(8)));
                drag.Cancel();
                Assert.AreEqual(140, slots.GetSlots().Where(s => s.Definition == battery).Sum(s => s.Stack.Quantity));
                Assert.AreEqual(100, slots.GetSlots().Where(s => s.Definition == oxygen).Sum(s => s.Stack.Quantity));
            }
        }

        [Test]
        public void FileReplacementRetainsPreviousCompleteSnapshot()
        {
            string directory = Path.Combine(Path.GetTempPath(), "solitude-test-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "save.json");
            try
            {
                SaveFileStore.Write(path, "first");
                SaveFileStore.Write(path, "second");
                SaveFileStore.Write(path, "third");
                Assert.AreEqual("third", File.ReadAllText(path));
                Assert.AreEqual("second", File.ReadAllText(path + ".bak"));
                Assert.IsFalse(File.Exists(path + ".tmp"));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
