using System;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Tests
{
    public class RestorationTests
    {
        private ContainerCommandService commands;
        private Container inventory;
        private RestorableSystemState system;
        [SetUp] public void SetUp()
        {
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("cell", true, 10), new ItemSpec("other", true, 10) }));
            inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 3);
            system = new RestorableSystemState();
        }
        private RestorationOutcome Restore() => system.TryRestore(commands, inventory, "cell");
        [Test] public void InstallationCommitsBothFactsBeforeObserversAndOnlyOnce()
        {
            commands.Grant(inventory.Handle, "cell", 2); commands.Grant(inventory.Handle, "other", 3);
            bool inventorySawState = false, restorationSawInventory = false;
            int changes = 0; RestorationOutcome reentry = RestorationOutcome.Restored;
            inventory.SlotChanged += _ => { inventorySawState = system.IsRestored && inventory.GetSlot(0).Stack.Quantity == 1; reentry = Restore(); };
            system.Changed += () => { changes++; restorationSawInventory = inventory.GetSlot(0).Stack.Quantity == 1; };
            Assert.AreEqual(RestorationOutcome.Restored, Restore());
            Assert.IsTrue(inventorySawState); Assert.IsTrue(restorationSawInventory);
            Assert.AreEqual(RestorationOutcome.Busy, reentry);
            Assert.AreEqual(RestorationOutcome.AlreadyRestored, Restore());
            Assert.AreEqual(1, changes); Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(3, inventory.GetSlot(1).Stack.Quantity);
        }
        [Test] public void MissingWrongAndNonInventoryItemsCannotRestore()
        {
            commands.Grant(inventory.Handle, "other", 1);
            var hotbar = commands.Register(ContainerSaveSession.PlayerHotbarId, 1);
            var locker = commands.Register("locker", 1);
            commands.Grant(hotbar.Handle, "cell", 1); commands.Grant(locker.Handle, "cell", 1);
            Assert.AreEqual(RestorationOutcome.MissingItem, Restore());
            Assert.AreEqual(RestorationOutcome.Unavailable, system.TryRestore(commands, hotbar, "cell"));
            Assert.AreEqual(RestorationOutcome.Unavailable, system.TryRestore(commands, locker, "cell"));
            Assert.IsFalse(system.IsRestored); Assert.AreEqual(1, hotbar.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(1, locker.GetSlot(0).Stack.Quantity);
            commands.Transfer(locker.GetSlot(0).Address, inventory.GetSlot(1).Address);
            Assert.AreEqual(RestorationOutcome.Restored, Restore()); Assert.IsTrue(inventory.GetSlot(1).IsEmpty);
        }
        [Test] public void BusyAndUnavailableAttemptsDoNotConsume()
        {
            RestorationOutcome duringGrant = RestorationOutcome.Restored;
            inventory.SlotChanged += _ => duringGrant = Restore();
            commands.Grant(inventory.Handle, "cell", 2);
            Assert.AreEqual(RestorationOutcome.Busy, duringGrant); Assert.IsFalse(system.IsRestored);
            Assert.AreEqual(2, inventory.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(RestorationOutcome.Unavailable, system.TryRestore(null, inventory, "cell"));
            Assert.AreEqual(RestorationOutcome.Unavailable, system.TryRestore(commands, inventory, ""));
            commands.Unregister(inventory.Handle);
            Assert.AreEqual(RestorationOutcome.Unavailable, Restore()); Assert.IsFalse(system.IsRestored);
        }
        [Test] public void ObserverFailuresAndReentryDoNotStopOtherConsumers()
        {
            commands.Grant(inventory.Handle, "cell", 2);
            int delivered = 0, errors = 0; RestorationOutcome reentry = RestorationOutcome.Restored;
            system.Changed += () => throw new InvalidOperationException("observer");
            system.ObserverError += _ => throw new InvalidOperationException("reporter");
            system.ObserverError += _ => errors++;
            system.Changed += () => { delivered++; reentry = Restore(); };
            Assert.AreEqual(RestorationOutcome.Restored, Restore());
            Assert.AreEqual(1, errors); Assert.AreEqual(1, delivered); Assert.AreEqual(RestorationOutcome.Busy, reentry);
            Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
        }
        [Test] public void SystemsAreIndependentAndPickupRewardCanBeInstalled()
        {
            var session = new ContainerSaveSession(new SaveGameData(), new ItemCatalog(new[] { new ItemSpec("cell", true, 10) }));
            var player = session.Register(ContainerSaveSession.PlayerInventoryId, 2).Container;
            var pickups = new PickupCollectionService(session); pickups.Register("pickup", new PickupReward("cell", 1));
            Assert.AreEqual(CollectResult.Collected, pickups.TryCollect("pickup", ContainerSaveSession.PlayerInventoryId, "cell", 1));
            var second = new RestorableSystemState();
            Assert.AreEqual(RestorationOutcome.Restored, system.TryRestore(session.Commands, player, "cell"));
            Assert.IsFalse(second.IsRestored); Assert.IsTrue(player.GetSlot(0).IsEmpty);
        }
        [Test] public void StaleRemovalCannotChangeInventory()
        {
            commands.Grant(inventory.Handle, "cell", 2); var stale = inventory.GetSlot(0).Address;
            commands.Remove(stale, 1);
            Assert.AreEqual(CommandStatus.Stale, commands.Remove(stale, 1).Status);
            Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity); Assert.IsFalse(system.IsRestored);
        }
    }
}
