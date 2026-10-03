using System;
using System.IO;
using NUnit.Framework;
using SOLITUDE.Application.Persistence;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
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
