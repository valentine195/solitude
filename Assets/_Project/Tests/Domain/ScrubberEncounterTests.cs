using NUnit.Framework;
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
    }
}
