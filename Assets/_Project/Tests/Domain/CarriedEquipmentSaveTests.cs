using System.Linq;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Tests
{
    public sealed class CarriedEquipmentSaveTests
    {
        private static ItemCatalog Catalog() => new ItemCatalog(new[]
        {
            new ItemSpec("tool", false, 1),
            new ItemSpec("cell", true, 4)
        });
        private static ContainerSaveRecord Record(string id, string item, int quantity)
        {
            var result = new ContainerSaveRecord { saveableId = id, state = new ContainerSaveData() };
            result.state.slots.Add(new ContainerSlotSaveData { itemId = item, index = 0, quantity = quantity });
            return result;
        }

        [Test]
        public void MigrationMergesAndPreservesOverflowWhileBlockingIncomingItems()
        {
            var data = new SaveGameData { startingLoadoutInitialized = true };
            data.containers.Add(Record(ContainerSaveSession.PlayerInventoryId, "cell", 3));
            var bar = Record(ContainerSaveSession.PlayerHotbarId, "cell", 4);
            bar.state.slots.Add(new ContainerSlotSaveData { itemId = "tool", index = 1, quantity = 1 });
            data.containers.Add(bar);
            var session = new ContainerSaveSession(data, Catalog());
            session.PrepareEquipmentInventory(1, id => id == "tool" ? PlayerEquipmentSlot.Tool : PlayerEquipmentSlot.None);
            using var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 1);
            using var gear = session.Register(ContainerSaveSession.PlayerHotbarId, 9);
            var carried = inventory.Container;
            Assert.AreEqual("tool", session.Equipment.ActiveItemId);
            Assert.AreEqual(7, carried.GetSlots().Where(slot => slot.Stack?.ItemId == "cell").Sum(slot => slot.Stack.Quantity));
            Assert.AreEqual(2, carried.Capacity);
            Assert.AreEqual(CommandStatus.InsufficientSpace, session.Commands.Grant(carried.Handle, "cell", 1).Status);
            Assert.AreEqual(CommandStatus.Rejected, session.Commands.Transfer(carried.GetSlot(0).Address, carried.GetSlot(1).Address).Status);
            Assert.IsTrue(session.Commands.Remove(carried.GetSlot(0).Address, 4).Succeeded);
            Assert.IsTrue(session.Commands.Transfer(carried.GetSlot(1).Address, carried.GetSlot(0).Address).Succeeded);
            Assert.AreEqual("tool", session.Equipment.ActiveItemId);
            var migrated = session.Capture();
            var restored = new ContainerSaveSession(migrated, Catalog());
            restored.PrepareEquipmentInventory(1, _ => PlayerEquipmentSlot.None);
            using var rebound = restored.Register(ContainerSaveSession.PlayerInventoryId, 1);
            using var oldBar = restored.Register(ContainerSaveSession.PlayerHotbarId, 9);
            Assert.AreEqual(1, rebound.Container.Capacity);
            Assert.AreEqual(3, rebound.Container.GetSlot(0).Stack.Quantity);
            Assert.AreEqual("tool", oldBar.Container.GetSlot(1).Stack.ItemId);
            Assert.IsTrue(restored.Commands.Remove(rebound.Container.GetSlot(0).Address, 1).Succeeded);
            Assert.AreEqual(PlayerEquipmentSlot.Tool, restored.Equipment.activeSlot);
        }

        [Test]
        public void AssignedBackpackGearMigratesExactlyOnceAndRetainsSpareCopies()
        {
            var save = new SaveGameData { startingLoadoutInitialized = true,
                playerEquipment = new PlayerEquipmentRecord { weaponItemId = "tool", activeSlot = PlayerEquipmentSlot.Weapon } };
            var backpack = Record(ContainerSaveSession.PlayerInventoryId, "tool", 1);
            backpack.state.slots.Add(new ContainerSlotSaveData { index = 1, itemId = "tool", quantity = 1 });
            save.containers.Add(backpack);
            var session = new ContainerSaveSession(save, Catalog());
            session.PrepareEquipmentInventory(2, _ => PlayerEquipmentSlot.None);
            var restored = new ContainerSaveSession(session.Capture(), Catalog());
            restored.PrepareEquipmentInventory(2, _ => PlayerEquipmentSlot.None);
            using var inventory = restored.Register(ContainerSaveSession.PlayerInventoryId, 2);
            using var gear = restored.Register(ContainerSaveSession.PlayerHotbarId, 9);
            Assert.IsTrue(inventory.Container.GetSlot(0).IsEmpty);
            Assert.AreEqual("tool", inventory.Container.GetSlot(1).Stack.ItemId);
            Assert.AreEqual("tool", gear.Container.GetSlot(0).Stack.ItemId);
            Assert.AreEqual("tool", restored.Equipment.ActiveItemId);
        }

        [Test]
        public void SingleItemSwapWorksWithFullBackpackAndUnequipKeepsGearWhenBlocked()
        {
            var session = new ContainerSaveSession(new SaveGameData(), Catalog());
            session.PrepareEquipmentInventory(1, _ => PlayerEquipmentSlot.None);
            using var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 1);
            using var gear = session.Register(ContainerSaveSession.PlayerHotbarId, 9);
            var carried = inventory.Container;
            var weapon = gear.Container;
            var commands = session.Commands;
            Assert.IsTrue(session.InitializeStartingLoadout(carried.Handle, weapon.Handle, new[] { new PickupReward("tool", 1) }, 0).Succeeded);
            Assert.IsTrue(commands.Grant(carried.Handle, "cell", 2).Succeeded);
            Assert.AreEqual(CommandStatus.InsufficientSpace, commands.EquipOne(carried.GetSlot(0).Address, weapon.GetSlot(0).Address, carried.Handle).Status);
            Assert.AreEqual(2, carried.GetSlot(0).Stack.Quantity);
            Assert.AreEqual("tool", session.Equipment.ActiveItemId);
            Assert.IsTrue(commands.Remove(carried.GetSlot(0).Address, 1).Succeeded);
            Assert.AreEqual(1, commands.EquipOne(carried.GetSlot(0).Address, weapon.GetSlot(0).Address, carried.Handle).Moved);
            Assert.AreEqual("tool", carried.GetSlot(0).Stack.ItemId);
            Assert.AreEqual("cell", session.Equipment.ActiveItemId);
            Assert.AreEqual(CommandStatus.InsufficientSpace, commands.TransferToContainer(weapon.GetSlot(0).Address, carried.Handle).Status);
            Assert.AreEqual("cell", session.Equipment.ActiveItemId);
        }

        [Test]
        public void FreshLoadoutPublishesEquippedWeaponOnlyOnce()
        {
            var session = new ContainerSaveSession(new SaveGameData(), Catalog());
            session.PrepareEquipmentInventory(2, _ => PlayerEquipmentSlot.None);
            using var inventory = session.Register(ContainerSaveSession.PlayerInventoryId, 2);
            using var bar = session.Register(ContainerSaveSession.PlayerHotbarId, 9);
            var grants = new[] { new PickupReward("tool", 1) };
            var commands = session.InitializeStartingLoadout(inventory.Container.Handle, bar.Container.Handle, grants, 0);
            Assert.IsTrue(commands.Succeeded);
            Assert.AreEqual("tool", session.Equipment.weaponItemId);
            Assert.AreEqual(PlayerEquipmentSlot.Weapon, session.Equipment.activeSlot);
            Assert.IsTrue(inventory.Container.GetSlot(0).IsEmpty);
            Assert.AreEqual("tool", bar.Container.GetSlot(0).Stack.ItemId);
            Assert.AreEqual(CommandStatus.NoOp, session.InitializeStartingLoadout(inventory.Container.Handle, bar.Container.Handle, grants, 0).Status);
        }
    }
}
