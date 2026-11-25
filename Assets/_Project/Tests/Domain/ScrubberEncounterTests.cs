using System;
using NUnit.Framework;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.Restoration;

namespace SOLITUDE.Tests
{
    public class ScrubberEncounterTests
    {
        [Test] public void FreshEncounterDescribesStoppedFlowAndBlownFuse()
        {
            var state = new ScrubberEncounterState();
            Assert.AreEqual("OFFLINE", state.OperatingStatus);
            Assert.AreEqual("BLOWN", state.FuseStatus);
            Assert.AreEqual("STOPPED", state.AirflowStatus);
            Assert.AreEqual("ELEVATED", state.RoomCondition);
            Assert.AreEqual("Replace the scrubber fuse", state.Objective);
            Assert.AreEqual("Blown fuse", state.Fault);
        }
        private static ContainerCommandService Commands() => new ContainerCommandService(new ItemCatalog(new[] {new ItemSpec("fuse",true,10),new ItemSpec("wrong",true,10)}));
        [Test] public void RepairCommitsBothFactsBeforeNotificationsAndRejectsReentry()
        {
            var commands=Commands();var inventory=commands.Register(ContainerSaveSession.PlayerInventoryId,2);var state=new ScrubberEncounterState();commands.Grant(inventory.Handle,"fuse",2);
            int changes=0;RestorationOutcome nested=RestorationOutcome.Restored;
            inventory.SlotChanged+=_=>{Assert.IsTrue(state.FuseInstalled);Assert.AreEqual(1,inventory.GetSlot(0).Stack.Quantity);nested=state.Install(commands,inventory,"fuse");};
            state.Changed+=()=>{changes++;Assert.AreEqual(1,inventory.GetSlot(0).Stack.Quantity);};
            Assert.AreEqual(RestorationOutcome.Restored,state.Install(commands,inventory,"fuse"));Assert.AreEqual(RestorationOutcome.Busy,nested);
            Assert.AreEqual(RestorationOutcome.AlreadyRestored,state.Install(commands,inventory,"fuse"));Assert.AreEqual(1,changes);
            Assert.AreEqual("INTACT",state.FuseStatus);Assert.AreEqual("OFFLINE",state.OperatingStatus);Assert.AreEqual("STOPPED",state.AirflowStatus);Assert.AreEqual("ELEVATED",state.RoomCondition);Assert.AreEqual("Restart the scrubber",state.Objective);
        }
        [Test] public void MissingWrongHotbarAndLockerItemsCannotRepair()
        {
            var commands=Commands();var inventory=commands.Register(ContainerSaveSession.PlayerInventoryId,2);var hotbar=commands.Register(ContainerSaveSession.PlayerHotbarId,1);var locker=commands.Register("locker",1);var state=new ScrubberEncounterState();
            commands.Grant(inventory.Handle,"wrong",1);commands.Grant(hotbar.Handle,"fuse",1);commands.Grant(locker.Handle,"fuse",1);
            Assert.AreEqual(RestorationOutcome.MissingItem,state.Install(commands,inventory,"fuse"));Assert.AreEqual(RestorationOutcome.Unavailable,state.Install(commands,hotbar,"fuse"));Assert.AreEqual(RestorationOutcome.Unavailable,state.Install(commands,locker,"fuse"));
            Assert.IsFalse(state.FuseInstalled);Assert.AreEqual(1,inventory.GetSlot(0).Stack.Quantity);Assert.AreEqual(1,hotbar.GetSlot(0).Stack.Quantity);Assert.AreEqual(1,locker.GetSlot(0).Stack.Quantity);
            Assert.AreEqual("Restart failed: blown fuse. Replace fuse first.",state.RestartFeedback);
        }
        [Test] public void RemovedFuseAndUnavailableOrBusyInventoryDoNotChangeFault()
        {
            var commands=Commands();var inventory=commands.Register(ContainerSaveSession.PlayerInventoryId,2);var state=new ScrubberEncounterState();
            RestorationOutcome nested=RestorationOutcome.Restored;inventory.SlotChanged+=_=>nested=state.Install(commands,inventory,"fuse");
            commands.Grant(inventory.Handle,"fuse",1);Assert.AreEqual(RestorationOutcome.Busy,nested);var stale=inventory.GetSlot(0).Address;commands.Remove(stale,1);
            Assert.AreEqual(CommandStatus.Stale,commands.Remove(stale,1).Status);Assert.AreEqual(RestorationOutcome.MissingItem,state.Install(commands,inventory,"fuse"));
            Assert.AreEqual(RestorationOutcome.Unavailable,state.Install(null,inventory,"fuse"));commands.Unregister(inventory.Handle);Assert.AreEqual(RestorationOutcome.Unavailable,state.Install(commands,inventory,"fuse"));Assert.IsFalse(state.FuseInstalled);
        }
        [Test] public void ObserverFailureCannotUndoRepairOrPreventOtherObservers()
        {
            var commands=Commands();var inventory=commands.Register(ContainerSaveSession.PlayerInventoryId,2);var state=new ScrubberEncounterState();commands.Grant(inventory.Handle,"fuse",1);int delivered=0;
            state.Changed+=()=>throw new InvalidOperationException();state.Changed+=()=>delivered++;
            Assert.AreEqual(RestorationOutcome.Restored,state.Install(commands,inventory,"fuse"));Assert.IsTrue(state.FuseInstalled);Assert.IsTrue(inventory.GetSlot(0).IsEmpty);Assert.AreEqual(1,delivered);
        }
        [Test] public void RestartAndFiveSecondRecoveryAreConfirmedOnce()
        {
            var state=new ScrubberEncounterState();Assert.IsFalse(state.TryRestart());state.Advance(5);Assert.AreEqual(0,state.RecoverySeconds);
            var commands=Commands();var inventory=commands.Register(ContainerSaveSession.PlayerInventoryId,1);commands.Grant(inventory.Handle,"fuse",1);state.Install(commands,inventory,"fuse");
            int events=0;state.Changed+=()=>events++;Assert.IsTrue(state.TryRestart());Assert.AreEqual("RUNNING",state.OperatingStatus);Assert.AreEqual("ACTIVE",state.AirflowStatus);Assert.AreEqual("RECOVERING",state.RoomCondition);
            state.Advance(4);Assert.IsFalse(state.TryRestart());state.Advance(.5f);Assert.IsFalse(state.Operational);Assert.AreEqual(4.5,state.RecoverySeconds);
            state.Advance(0);Assert.AreEqual(4.5,state.RecoverySeconds);state.Advance(.5f);Assert.IsTrue(state.Operational);Assert.AreEqual("NORMAL",state.RoomCondition);Assert.That(state.Objective,Does.Contain("complete"));
            state.Advance(50);Assert.IsFalse(state.TryRestart());Assert.AreEqual(5,state.RecoverySeconds);Assert.AreEqual(2,events);
            Assert.AreEqual(RestorationOutcome.AlreadyRestored,state.Install(commands,inventory,"fuse"));Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
        }
    }
}
