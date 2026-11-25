using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;

namespace SOLITUDE.Tests
{
    public sealed class EquippedItemViewTests
    {
        private ContainerCommandService commands;
        private Container hotbar;
        private HotbarSelectionState selection;
        private EquippedItemView view;
        [SetUp] public void Setup()
        {
            commands = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("tool", false, 1), new ItemSpec("food", true, 10) }));
            hotbar = commands.Register("hotbar", 3);
            selection = new HotbarSelectionState(hotbar);
            view = new EquippedItemView(commands, hotbar, selection);
        }
        [TearDown] public void Cleanup() { view.Dispose(); selection.Dispose(); }
        private EquippedItemBinding Equip()
        {
            commands.Grant(hotbar.Handle, "tool", 1); selection.Select(0);
            Assert.IsTrue(view.TryCapture(out var binding)); return binding;
        }
        [Test] public void SelectionIsTheOnlyEquipmentSourceAndReturningDoesNotReviveBinding()
        {
            Assert.IsFalse(view.TryCapture(out _)); var binding = Equip();
            Assert.AreEqual("tool", binding.ItemId); Assert.IsTrue(view.IsCurrent(binding));
            selection.Select(2); Assert.IsFalse(view.TryCapture(out _)); Assert.IsFalse(view.IsCurrent(binding));
            selection.Select(0); Assert.IsFalse(view.IsCurrent(binding)); Assert.IsTrue(view.TryCapture(out _));
            selection.Select(-1); Assert.IsFalse(view.TryCapture(out _));
        }
        [Test] public void RemovingOrTransferringSelectedItemInvalidatesBinding()
        {
            var binding = Equip(); var pack = commands.Register("pack", 1);
            Assert.IsTrue(commands.Transfer(binding.Address, pack.GetSlot(0).Address).Succeeded);
            Assert.IsFalse(view.IsCurrent(binding)); Assert.IsFalse(view.TryCapture(out _));
            Assert.AreEqual(-1, selection.ActiveIndex);
            commands.Grant(hotbar.Handle, "tool", 1); selection.Select(0); view.TryCapture(out binding);
            commands.Remove(binding.Address, 1); Assert.IsFalse(view.IsCurrent(binding));
        }
        [TestCase("tool")]
        [TestCase("food")]
        public void OccupiedReplacementInvalidatesEvenForSameDefinition(string replacement)
        {
            var binding = Equip();
            var data = new ContainerSaveData(); data.slots.Add(new ContainerSlotSaveData { index = 0, itemId = replacement, quantity = 1 });
            Assert.IsTrue(commands.Restore(hotbar.Handle, data).Succeeded);
            Assert.AreEqual(0, selection.ActiveIndex); Assert.IsFalse(view.IsCurrent(binding));
            Assert.IsTrue(view.TryCapture(out var next)); Assert.AreEqual(replacement, next.ItemId);
            Assert.AreNotEqual(binding.Address.Revision, next.Address.Revision);
        }
        [Test] public void OtherSlotChangesDoNotInvalidateEquipmentButResizeDoes()
        {
            var binding = Equip(); commands.Grant(hotbar.Handle, "food", 1);
            Assert.IsTrue(view.IsCurrent(binding)); commands.Resize(hotbar.Handle, 4);
            Assert.IsFalse(view.IsCurrent(binding)); Assert.IsTrue(view.TryCapture(out binding));
            Assert.AreEqual(CommandStatus.InsufficientSpace, commands.Resize(hotbar.Handle, 1).Status);
            Assert.IsTrue(view.IsCurrent(binding));
            Assert.AreEqual(CommandStatus.Applied, commands.Remove(hotbar.GetSlot(1).Address, 1).Status);
            Assert.AreEqual(CommandStatus.Applied, commands.Resize(hotbar.Handle, 1).Status);
            Assert.IsFalse(view.IsCurrent(binding));
        }
        [Test] public void UnregisterAndReregisterCannotReviveOldOwner()
        {
            var binding = Equip(); commands.Unregister(hotbar.Handle);
            var replacement = commands.Register("hotbar", 3); commands.Grant(replacement.Handle, "tool", 1);
            Assert.AreNotEqual(binding.Address.Container.Generation, replacement.Handle.Generation);
            Assert.IsFalse(view.IsCurrent(binding)); Assert.IsFalse(view.TryCapture(out _));
            using var nextSelection = new HotbarSelectionState(replacement); nextSelection.Select(0);
            using var nextView = new EquippedItemView(commands, replacement, nextSelection);
            Assert.IsTrue(nextView.TryCapture(out var next)); Assert.IsFalse(nextView.IsCurrent(binding)); Assert.IsFalse(view.IsCurrent(next));
        }
        [Test] public void ForeignReaderAndReleaseAreUnavailable()
        {
            var binding = Equip(); view.Dispose(); view.Dispose();
            Assert.IsFalse(view.IsCurrent(binding)); Assert.IsFalse(view.TryCapture(out _));
            var other = new ContainerCommandService(new ItemCatalog(new[] { new ItemSpec("tool", false, 1) }));
            using var foreign = new EquippedItemView(other, hotbar, selection);
            Assert.IsFalse(foreign.TryCapture(out _));
        }
    }
}
