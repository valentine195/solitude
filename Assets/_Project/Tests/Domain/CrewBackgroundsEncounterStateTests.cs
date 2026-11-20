using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Restoration;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.CrewBackgrounds;

namespace SOLITUDE.Tests
{
    public sealed class CrewBackgroundsEncounterStateTests
    {
        [Test]
        public void BreakerStartsAirflowOnlyForPowerLossAndRecoveryStopsAtFiveSeconds()
        {
            var powerLoss = new CrewBackgroundsEncounterState(CrewBackgroundsFault.LossOfPower);
            Assert.IsFalse(powerLoss.NormalFeedAvailable);
            Assert.IsTrue(powerLoss.IntakeLinkageIntact);
            powerLoss.Advance(20);
            Assert.Zero(powerLoss.RecoverySeconds);
            Assert.AreEqual(RestorationOutcome.Unavailable,
                powerLoss.TryRepair(CrewRepairOperation.FitIntakeLinkage, null, null, null));

            Assert.AreEqual(RestorationOutcome.Restored,
                powerLoss.TryRepair(CrewRepairOperation.ResetBreaker, null, null, null));
            Assert.IsTrue(powerLoss.Airflow);
            powerLoss.Advance(4.75);
            Assert.IsFalse(powerLoss.RoomRecovered);
            Assert.AreEqual(RestorationOutcome.AlreadyRestored,
                powerLoss.TryRepair(CrewRepairOperation.ResetBreaker, null, null, null));
            powerLoss.Advance(.25);
            powerLoss.Advance(20);
            Assert.IsTrue(powerLoss.RoomRecovered);
            Assert.AreEqual(5, powerLoss.RecoverySeconds);
        }

        [Test]
        public void JumperOwnershipDeterminesBypassInTheSameTransferNotification()
        {
            var catalog = new ItemCatalog(new[] { new ItemSpec("jumper", false, 1), new ItemSpec("filler", true, 5) });
            var commands = new ContainerCommandService(catalog);
            var inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 1);
            var panel = commands.Register("panel", 1, filter: item => item.ItemId == "jumper");
            var state = new CrewBackgroundsEncounterState(CrewBackgroundsFault.LossOfPower, panel, "jumper");
            commands.Grant(inventory.Handle, "jumper", 1);
            var jumper = inventory.GetSlot(0).Stack;
            bool? observedBypass = null, observedLights = null, observedAirflow = null, observedPanelItem = null;
            commands.Completed += _ =>
            {
                observedBypass = state.BypassActive;
                observedLights = state.CorridorLightsOn;
                observedAirflow = state.Airflow;
                observedPanelItem = panel.GetSlot(0).Stack == jumper;
            };

            Assert.AreEqual(CommandStatus.Applied, state.TryMoveJumper(CrewRepairOperation.InstallJumper, commands, inventory));
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(true, observedBypass);
            Assert.AreEqual(false, observedLights);
            Assert.AreEqual(true, observedAirflow);
            Assert.AreEqual(true, observedPanelItem);
            Assert.AreEqual(CommandStatus.Unavailable, state.TryMoveJumper(CrewRepairOperation.ReclaimJumper, commands, inventory));

            state.Advance(2);
            Assert.AreEqual(RestorationOutcome.Restored, state.TryRepair(CrewRepairOperation.ResetBreaker, commands, inventory, null));
            Assert.AreEqual(2, state.RecoverySeconds);
            Assert.IsFalse(state.BypassActive);
            Assert.IsTrue(state.CorridorLightsOn);
            Assert.IsTrue(state.Airflow);
            Assert.AreSame(jumper, panel.GetSlot(0).Stack);

            commands.Grant(inventory.Handle, "filler", 1);
            Assert.AreEqual(CommandStatus.InsufficientSpace, state.TryMoveJumper(CrewRepairOperation.ReclaimJumper, commands, inventory));
            Assert.AreSame(jumper, panel.GetSlot(0).Stack);
            commands.Remove(inventory.GetSlot(0).Address, 1);
            Assert.AreEqual(CommandStatus.Applied, state.TryMoveJumper(CrewRepairOperation.ReclaimJumper, commands, inventory));
            Assert.AreEqual(false, observedBypass);
            Assert.AreEqual(true, observedLights);
            Assert.AreEqual(true, observedAirflow);
            Assert.AreEqual(false, observedPanelItem);
            Assert.AreSame(jumper, inventory.GetSlot(0).Stack);
            Assert.IsTrue(panel.GetSlot(0).IsEmpty);
            Assert.AreEqual(CommandStatus.Unavailable, state.TryMoveJumper(CrewRepairOperation.ReclaimJumper, commands, inventory));
        }

        [Test]
        public void CorrectPartCommitsOnceAndObserversSeeRepairedLinkage()
        {
            var catalog = new ItemCatalog(new[] { new ItemSpec("part", true, 5), new ItemSpec("wrong", true, 5) });
            var commands = new ContainerCommandService(catalog);
            var inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 3);
            var state = new CrewBackgroundsEncounterState(CrewBackgroundsFault.BrokenIntakeLinkage);
            commands.Grant(inventory.Handle, "wrong", 1);

            Assert.IsTrue(state.NormalFeedAvailable);
            Assert.IsFalse(state.IntakeLinkageIntact);
            Assert.AreEqual(RestorationOutcome.Unavailable,
                state.TryRepair(CrewRepairOperation.ResetBreaker, commands, inventory, null));
            Assert.AreEqual(RestorationOutcome.MissingItem,
                state.TryRepair(CrewRepairOperation.FitIntakeLinkage, commands, inventory, "part"));

            commands.Grant(inventory.Handle, "part", 2);
            bool? observedAirflow = null;
            var nested = RestorationOutcome.Unavailable;
            inventory.SlotChanged += _ =>
            {
                observedAirflow = state.Airflow;
                nested = state.TryRepair(CrewRepairOperation.FitIntakeLinkage, commands, inventory, "part");
            };
            Assert.AreEqual(RestorationOutcome.Restored,
                state.TryRepair(CrewRepairOperation.FitIntakeLinkage, commands, inventory, "part"));
            Assert.AreEqual(true, observedAirflow);
            Assert.AreEqual(RestorationOutcome.AlreadyRestored, nested);
            Assert.AreEqual(1, inventory.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(1, inventory.GetSlot(1).Stack.Quantity);
        }
    }
}
