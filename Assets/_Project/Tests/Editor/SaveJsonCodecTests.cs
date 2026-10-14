using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using SOLITUDE.Expeditions;
namespace SOLITUDE.Tests
{
    public class SaveJsonCodecTests
    {
        private SaveJsonCodec codec;
        [SetUp] public void Setup() => codec = new SaveJsonCodec(new ItemCatalog(new[] { new ItemSpec("a",true,10) }));
        [TestCase("{}")] [TestCase("{\"version\":2}")] [TestCase("{\"version\":2,\"version\":2}")] [TestCase("null")] [TestCase("{\"version\":2")]
        public void IncompleteSnapshotsDoNotBecomeEmptyWorlds(string json) => Assert.Throws<SaveCandidateException>(()=>codec.Decode(json));
        [Test] public void TrailingContentAndWrongTypesAreRejected()
        {
            string text=codec.Encode(new SaveGameData());Assert.Throws<SaveCandidateException>(()=>codec.Decode(text+"{}"));
            Assert.Throws<SaveCandidateException>(()=>codec.Decode(text.Replace("\"containers\": []","\"containers\": null")));
        }
        [Test] public void UnknownItemsAndFutureVersionsAreIncompatible()
        {
            var data=new SaveGameData();data.containers.Add(new ContainerSaveRecord {saveableId="inventory",state=new ContainerSaveData {slots=new System.Collections.Generic.List<ContainerSlotSaveData> {new ContainerSlotSaveData {index=0,itemId="removed",quantity=1}}}});
            Assert.IsTrue(Assert.Throws<SaveCandidateException>(()=>codec.Decode(codec.Encode(data))).Incompatible);
            data=new SaveGameData {version=99};Assert.IsTrue(Assert.Throws<SaveCandidateException>(()=>codec.Decode(codec.Encode(data))).Incompatible);
        }
        [Test] public void CompleteSnapshotRoundTripsInventoryAndWorldFactsTogether()
        {
            var data=new SaveGameData {worldSeed=123};data.collectedPickupIds.Add("pickup");data.containers.Add(new ContainerSaveRecord {saveableId="inventory",state=new ContainerSaveData {slots=new System.Collections.Generic.List<ContainerSlotSaveData> {new ContainerSlotSaveData {index=0,itemId="a",quantity=2}}}});
            var restored=codec.Decode(codec.Encode(data));Assert.AreEqual(123,restored.worldSeed);Assert.AreEqual(2,restored.containers[0].state.slots[0].quantity);Assert.AreEqual("pickup",restored.collectedPickupIds[0]);
        }
        private static SaveGameData LivingSnapshot() => new SaveGameData
        {
            survivor = new SurvivorSnapshot
            {
                id = Guid.NewGuid().ToString(), scenePath = "Assets/_Project/Scenes/Wakeup.unity", x = 1.25, y = -7,
                health = new SavedVital { current = 30, max = 100 }, oxygen = new SavedVital { current = 90, max = 100 },
                hunger = new SavedVital { current = 60, max = 100 }, recoveryDelayRemaining = 1.5
            },
            expedition = new ExpeditionState { id = Guid.NewGuid().ToString() }
        };
        // Explicit wire fixtures test missing fields without adding a JSON-library dependency
        // to the EditMode assembly or letting serializer defaults fill the missing values.
        private static string JsonObject(params string[] fields) => "{" + string.Join(",", fields.Where(field => field != null)) + "}";
        private static string Field(string path, string value, string omit) => path == omit ? null :
            "\"" + path.Substring(path.LastIndexOf('.') + 1) + "\":" + value;
        private static string LivingJson(string omit = null, string healthCurrentJson = "30", string statusJson = "1",
            string expeditionJson = null, string repairsJson = "[]")
        {
            var health = JsonObject(Field("survivor.health.current", healthCurrentJson, omit), Field("survivor.health.max", "100", omit));
            var survivor = JsonObject(
                Field("survivor.id", "\"11111111-1111-1111-1111-111111111111\"", omit),
                Field("survivor.status", statusJson, omit), Field("survivor.scenePath", "\"Assets/_Project/Scenes/Wakeup.unity\"", omit),
                Field("survivor.x", "1.25", omit), Field("survivor.y", "-7", omit), Field("survivor.z", "0", omit),
                Field("survivor.health", health, omit), Field("survivor.oxygen", "{\"current\":90,\"max\":100}", omit),
                Field("survivor.hunger", "{\"current\":60,\"max\":100}", omit), Field("survivor.recoveryDelayRemaining", "1.5", omit));
            var expedition = expeditionJson ?? JsonObject(
                Field("expedition.id", "\"22222222-2222-2222-2222-222222222222\"", omit),
                Field("expedition.defeatedEncounterIds", "[]", omit), Field("expedition.collectedSupplySourceIds", "[]", omit));
            var ship = JsonObject(Field("ship.repairs", repairsJson, omit), Field("ship.doors", "[]", omit),
                Field("ship.criticalSources", "[]", omit), Field("ship.permanentDiscoveryIds", "[]", omit));
            return JsonObject(Field("version", "3", omit), Field("worldSeed", "0", omit), Field("containers", "[]", omit),
                Field("collectedPickupIds", "[]", omit), Field("survivor", survivor, omit), Field("expedition", expedition, omit),
                Field("ship", ship, omit), Field("pendingDeath", "null", omit));
        }
        [Test]
        public void ExplicitLivingWireFixtureIsValidBeforeRemovingFields()
        {
            var decoded = codec.Decode(LivingJson());
            Assert.AreEqual("11111111-1111-1111-1111-111111111111", decoded.survivor.id);
            Assert.AreEqual(30, decoded.survivor.health.current); Assert.AreEqual(1.25, decoded.survivor.x);
        }
        [TestCase(1)] [TestCase(2)]
        public void LegacyJsonRetainsContainersAndDoesNotInventAColonist(int version)
        {
            var json = "{\"version\":" + version + ",\"worldSeed\":0,\"containers\":[{\"saveableId\":\"locker\",\"state\":{\"slots\":[{\"index\":0,\"itemId\":\"a\",\"quantity\":2}]}}]"
                + (version == 2 ? ",\"collectedPickupIds\":[\"picked\"]" : "") + "}";
            var migrated = codec.Decode(json);
            Assert.AreEqual(3, migrated.version); Assert.IsNull(migrated.survivor); Assert.IsNull(migrated.expedition);
            Assert.AreEqual(2, migrated.containers[0].state.slots[0].quantity);
            Assert.AreEqual(version == 1 ? 0 : 1, migrated.collectedPickupIds.Count);
            Assert.IsNull(codec.Decode(codec.Encode(migrated)).survivor);
        }
        [Test]
        public void VersionThreeRoundTripsLivingShipAndExpeditionStateTogether()
        {
            var data = LivingSnapshot(); data.expedition.defeatedEncounterIds.Add("bot");
            data.expedition.collectedSupplySourceIds.Add("ration");
            data.ship.repairs.Add(new RepairSaveRecord { id = "scrubber", installed = true, running = true, recoverySeconds = 2.75 });
            data.ship.doors.Add(new DoorSaveRecord { id = "door", powered = true, unlocked = true, open = true });
            data.ship.criticalSources.Add(new CriticalSourceSaveRecord { id = "fuse", objectiveId = "scrubber" });
            data.ship.permanentDiscoveryIds.Add("log"); data.collectedPickupIds.Add("picked");
            var loaded = codec.Decode(codec.Encode(data));
            Assert.AreEqual(data.survivor.id, loaded.survivor.id); Assert.AreEqual(data.expedition.id, loaded.expedition.id);
            Assert.AreEqual(1.25, loaded.survivor.x); Assert.AreEqual(30, loaded.survivor.health.current);
            Assert.AreEqual(1.5, loaded.survivor.recoveryDelayRemaining); Assert.AreEqual("bot", loaded.expedition.defeatedEncounterIds[0]);
            Assert.AreEqual("ration", loaded.expedition.collectedSupplySourceIds[0]); Assert.AreEqual(2.75, loaded.ship.repairs[0].recoverySeconds);
            Assert.IsTrue(loaded.ship.doors[0].open); Assert.IsFalse(loaded.ship.criticalSources[0].available);
            Assert.AreEqual("log", loaded.ship.permanentDiscoveryIds[0]); Assert.AreEqual("picked", loaded.collectedPickupIds[0]);
        }
        [TestCase("ship")] [TestCase("pendingDeath")] [TestCase("expedition")]
        [TestCase("survivor.health.current")] [TestCase("survivor.status")]
        [TestCase("expedition.collectedSupplySourceIds")] [TestCase("collectedPickupIds")]
        public void MissingVersionThreeFieldsDoNotDefaultToAUsableWorld(string path)
        {
            Assert.Throws<SaveCandidateException>(() => codec.Decode(LivingJson(omit: path)));
        }
        [TestCase("NaN")] [TestCase("Infinity")]
        public void NonNumericVitalsAreRejected(string value)
        {
            Assert.Throws<SaveCandidateException>(() => codec.Decode(LivingJson(healthCurrentJson: "\"" + value + "\"")));
        }
        [Test]
        public void WrongRecordTypesAndPartialLifecycleAreRejected()
        {
            Assert.Throws<SaveCandidateException>(() => codec.Decode(LivingJson(repairsJson: "[42]")));
            Assert.Throws<SaveCandidateException>(() => codec.Decode(LivingJson(expeditionJson: "null")));
            Assert.Throws<SaveCandidateException>(() => codec.Decode(LivingJson(statusJson: "66")));
        }
        [Test]
        public void VersionThreeStillRequiresTypedPermanentCollectionFacts()
        {
            var json = codec.Encode(new SaveGameData()).Replace("\"collectedPickupIds\": []", "\"collectedPickupIds\": [42]");
            Assert.Throws<SaveCandidateException>(() => codec.Decode(json));
        }
        [Test] public void RealFilesRecoverBackupWithoutOverwritingItWithCorruption()
        {
            string directory=Path.Combine(Path.GetTempPath(),"solitude-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string path=Path.Combine(directory,"save");
            try
            {
                string valid=codec.Encode(new SaveGameData {worldSeed=55});File.WriteAllText(path,"broken");File.WriteAllText(path+".bak",valid);
                var store=new SaveRecoveryStore(path,new PhysicalSaveFiles(),codec);Assert.AreEqual(SaveLoadStatus.RecoveredBackup,store.Load(0).Status);store.CompleteRecovery();
                Assert.AreEqual(valid,File.ReadAllText(path));Assert.AreEqual(valid,File.ReadAllText(path+".bak"));Assert.AreEqual("broken",File.ReadAllText(Directory.GetFiles(directory,"save.corrupt-*")[0]));
            }
            finally {Directory.Delete(directory,true);}
        }
    }
}
