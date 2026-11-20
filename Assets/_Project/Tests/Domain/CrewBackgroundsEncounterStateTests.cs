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
