using System;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Items;
using SOLITUDE.Expeditions;
using SOLITUDE.SaveLoad;
using SOLITUDE.Application.Persistence;
namespace SOLITUDE.Tests
{
    public class MechanicalRepairSaveTests
    {
        private SaveJsonCodec codec;
        [SetUp] public void Setup() => codec = new SaveJsonCodec(new ItemCatalog(new[] { new ItemSpec("battery", true, 10) }));
        [Test] public void CompletedRepairAndUnrelatedFactsRoundTripWithoutIndependentDoorPermission()
        {
            var data = new SaveGameData { startingLoadoutInitialized = true };
            data.ship.repairs.Add(new RepairSaveRecord { id="aux.socket", installed=true, running=true });
            data.ship.repairs.Add(new RepairSaveRecord { id="other", installed=true });
            data.ship.doors.Add(new DoorSaveRecord { id="other.door", powered=true, unlocked=true });
            data.ship.permanentDiscoveryIds.Add("log"); data.collectedPickupIds.Add("picked");
            var decoded = codec.Decode(codec.Encode(data));
            Assert.AreEqual(2, decoded.ship.repairs.Count); Assert.IsTrue(decoded.ship.repairs[0].running);
            Assert.AreEqual(0, decoded.ship.repairs[0].recoverySeconds);
            Assert.AreEqual("other.door", decoded.ship.doors.Single().id);
            CollectionAssert.AreEqual(data.ship.permanentDiscoveryIds, decoded.ship.permanentDiscoveryIds);
            CollectionAssert.AreEqual(data.collectedPickupIds, decoded.collectedPickupIds);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void LegacyMissingRepairDoesNotInventRepairOrRefund(int version)
        {
            var data = new SaveGameData { version=version, startingLoadoutInitialized=true };
            var decoded = codec.Decode(codec.Encode(data));
            Assert.IsEmpty(decoded.ship.repairs); Assert.IsEmpty(decoded.containers); Assert.IsTrue(decoded.startingLoadoutInitialized);
        }
        [TestCase(false)] [TestCase(true)]
        public void DuplicateOrContradictoryRepairRecordsAreRejected(bool duplicate)
        {
            var data = new SaveGameData { startingLoadoutInitialized=true };
            data.ship.repairs.Add(new RepairSaveRecord { id="aux.socket", installed=duplicate, running=true });
            if (duplicate) data.ship.repairs.Add(data.ship.repairs[0].Clone());
            Assert.Throws<SaveCandidateException>(() => codec.Decode(codec.Encode(data)));
        }
    }
}
