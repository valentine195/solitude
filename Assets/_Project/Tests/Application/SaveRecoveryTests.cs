using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SOLITUDE.Application.Persistence;
using SOLITUDE.SaveLoad;
namespace SOLITUDE.Tests
{
    public class SaveRecoveryTests
    {
        private sealed class Files : ISaveFiles
        {
            public readonly Dictionary<string,string> Bytes = new();
            public bool FailRead, FailWrite, FailReplace;
            public bool Exists(string path) => Bytes.ContainsKey(path);
            public string Read(string path) { if (FailRead) throw new UnauthorizedAccessException(); return Bytes[path]; }
            public void WriteDurable(string path,string text) { if (FailWrite) throw new IOException(); Bytes[path] = text; }
            public void Replace(string source,string destination,string backup)
            { if (FailReplace) throw new IOException(); Bytes[backup] = Bytes[destination]; Move(source,destination); }
            public void Move(string source,string destination) { Bytes[destination] = Bytes[source]; Bytes.Remove(source); }
            public void Delete(string path) => Bytes.Remove(path);
        }
        private sealed class Codec : ISnapshotCodec
        {
            public SaveGameData Decode(string json)
            {
                if (json == "incompatible") throw new SaveCandidateException("Removed item", true);
                if (!int.TryParse(json,out int seed)) throw new SaveCandidateException("Corrupt");
                return new SaveGameData { worldSeed = seed, collectedPickupIds = new List<string> { "fact." + seed } };
            }
            public string Encode(SaveGameData data) => data.worldSeed.ToString();
        }
        private Files files;
        private SaveRecoveryStore store;
        [SetUp] public void Setup() { files = new Files(); store = new SaveRecoveryStore("save",files,new Codec()); }
        [Test] public void NoCommittedFilesStartsFreshButTemporaryAloneDoesNot()
        {
            Assert.AreEqual(SaveLoadStatus.Fresh,store.Load(123).Status);
            files.Bytes["save.tmp"]="10"; Assert.AreEqual(SaveLoadStatus.BlockedCorrupt,store.Load(123).Status);
        }
        [Test] public void ValidPrimaryWinsOverBackupAndTemporary()
        {
            files.Bytes["save"]="10";files.Bytes["save.bak"]="9";files.Bytes["save.tmp"]="broken";
            var result=store.Load(0); Assert.AreEqual(SaveLoadStatus.LoadedPrimary,result.Status); Assert.AreEqual(10,result.Data.worldSeed);
            Assert.AreEqual("fact.10",result.Data.collectedPickupIds[0]);
        }
        [TestCase(false)] [TestCase(true)] public void RecoveryPreservesBackupAndQuarantinesCorruptPrimary(bool corruptPrimary)
        {
            if (corruptPrimary) files.Bytes["save"]="broken";files.Bytes["save.bak"]="9";
            var result=store.Load(0);Assert.AreEqual(SaveLoadStatus.RecoveredBackup,result.Status);
            Assert.AreEqual("fact.9",result.Data.collectedPickupIds[0]);Assert.IsTrue(store.RecoveryPending);
            Assert.Throws<InvalidOperationException>(()=>store.Write(new SaveGameData()));
            store.CompleteRecovery();Assert.AreEqual("9",files.Bytes["save"]);Assert.AreEqual("9",files.Bytes["save.bak"]);
            if (corruptPrimary) Assert.IsTrue(files.Bytes.ContainsValue("broken"));
            Assert.IsTrue(store.NoticePending);store.AcknowledgeNotice();Assert.IsFalse(store.NoticePending);
        }
        [Test] public void IncompatiblePrimaryCannotSilentlyRollBack()
        { files.Bytes["save"]="incompatible";files.Bytes["save.bak"]="9";Assert.AreEqual(SaveLoadStatus.BlockedIncompatible,store.Load(0).Status);Assert.AreEqual("incompatible",files.Bytes["save"]); }
        [Test] public void InvalidBackupAndPrimaryRemainIntact()
        { files.Bytes["save"]="broken";files.Bytes["save.bak"]="broken backup";Assert.AreEqual(SaveLoadStatus.BlockedCorrupt,store.Load(0).Status);Assert.AreEqual("broken backup",files.Bytes["save.bak"]); }
        [Test] public void ReadErrorsNeverBecomeFreshOrBackupRecovery()
        { files.Bytes["save"]="10";files.Bytes["save.bak"]="9";files.FailRead=true;Assert.AreEqual(SaveLoadStatus.IOFailure,store.Load(0).Status); }
        [Test] public void FailedPromotionIsRetryableAndKeepsAllCommittedFiles()
        {
            files.Bytes["save"]="broken";files.Bytes["save.bak"]="9";store.Load(0);files.FailReplace=true;
            Assert.Throws<IOException>(()=>store.CompleteRecovery());Assert.AreEqual("broken",files.Bytes["save"]);Assert.AreEqual("9",files.Bytes["save.bak"]);Assert.IsTrue(store.RecoveryPending);
            files.FailReplace=false;var restarted=new SaveRecoveryStore("save",files,new Codec());Assert.AreEqual(SaveLoadStatus.RecoveredBackup,restarted.Load(0).Status);restarted.CompleteRecovery();Assert.IsTrue(restarted.NoticePending);
        }
        [Test] public void FailureBeforeTemporaryWriteCannotTouchPrimary()
        { files.Bytes["save"]="10";files.FailWrite=true;Assert.Throws<IOException>(()=>store.Write(new SaveGameData {worldSeed=11}));Assert.AreEqual("10",files.Bytes["save"]); }
        [Test] public void ExplicitResetPreservesValidBackupOfBlockedPrimary()
        {
            files.Bytes["save"]="incompatible";files.Bytes["save.bak"]="9";
            Assert.AreEqual(SaveLoadStatus.BlockedIncompatible,store.Load(0).Status);
            store.CancelPendingRecovery();store.Write(new SaveGameData {worldSeed=20});
            Assert.AreEqual("20",files.Bytes["save"]);Assert.AreEqual("9",files.Bytes["save.bak"]);
            Assert.IsTrue(files.Bytes.ContainsValue("incompatible"));
        }
        [Test] public void FailedCommitLeavesSessionDirtyUntilRetry()
        {
            var session=new ContainerSaveSession(new SaveGameData(),new SOLITUDE.Items.ItemCatalog(new[] {new SOLITUDE.Items.ItemSpec("a",true,10)}));
            session.Register("inventory",2);files.Bytes["save"]="10";files.FailReplace=true;
            Assert.Throws<IOException>(()=>{store.Write(session.Capture());session.MarkSaved();});Assert.IsTrue(session.IsDirty);Assert.AreEqual("10",files.Bytes["save"]);
            files.FailReplace=false;store.Write(session.Capture());session.MarkSaved();Assert.IsFalse(session.IsDirty);Assert.AreEqual("10",files.Bytes["save.bak"]);
        }
    }
}
