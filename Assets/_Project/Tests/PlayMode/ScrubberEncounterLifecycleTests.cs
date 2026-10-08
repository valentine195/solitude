#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.ScrubberEncounter;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class ScrubberEncounterLifecycleTests
    {
        private const string Demo="Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private string savePath; private byte[] original;
        private static T[] All<T>() where T:Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        private static AsyncOperation Load(string path) => EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
        [UnitySetUp] public IEnumerator Prepare()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-solitudeSaveDirectory");Assert.GreaterOrEqual(i,0,"Requires disposable save directory.");
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);yield return null;
            savePath=Path.Combine(args[i+1],"solitude-save.json");original=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var root in All<GameCompositionRoot>()) Object.Destroy(root.gameObject);yield return null;Time.timeScale=1;
            if(original==null) { if(File.Exists(savePath)) File.Delete(savePath); } else File.WriteAllBytes(savePath,original);
        }
        [UnityTest] public IEnumerator ReloadResetsEveryStateTogetherAndReleasesOldEndpoints()
        {
            File.WriteAllText(savePath,"SCRUBBER-LIFECYCLE-SAVE-SENTINEL");var bytes=File.ReadAllBytes(savePath);
            for(int stage=0;stage<5;stage++)
            {
                yield return Load(Demo);yield return null;
                var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsTrue(root.UsesDisposableSession);
                var equipment=All<ScrubberEquipment>().Single();var screen=All<ScrubberScreenView>().Single();var room=All<ScrubberRoomFeedback>().Single();
                if(stage>=1) Assert.IsTrue(All<WorldPickup>().Single().Interact(null).IsSuccess);
                if(stage>=2) { equipment.Install();Assert.IsTrue(equipment.State.FuseInstalled); }
                if(stage>=3) { equipment.Restart();yield return new WaitForSeconds(.1f);Assert.Greater(equipment.State.RecoverySeconds,0); }
                if(stage>=4) { equipment.State.Advance(5);Assert.IsTrue(equipment.State.Operational); }
                Assert.IsTrue(screen.Open());Field<SaveGameService>(root,"persistence").Flush();CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
                var oldRunning=room.RunningSound;var policy=Field<InputPolicyCoordinator>(root,"policy");Assert.IsTrue(policy.PrimaryScreenActive);
                yield return Load(Demo);yield return null;
                Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.IsTrue(equipment==null);Assert.IsTrue(screen==null);Assert.IsTrue(oldRunning==null);Assert.IsFalse(policy.PrimaryScreenActive);Assert.IsTrue(policy.Gameplay);
                var fresh=All<ScrubberEquipment>().Single();Assert.IsFalse(fresh.State.FuseInstalled);Assert.IsFalse(fresh.State.Running);Assert.AreEqual(0,fresh.State.RecoverySeconds);Assert.AreEqual("ELEVATED",fresh.State.RoomCondition);
                Assert.IsTrue(All<WorldPickup>().Single().gameObject.activeSelf);Assert.IsTrue(All<PlayerInventory>().Single().Container.GetSlots().All(slot=>slot.IsEmpty));
                Assert.IsTrue(All<ScrubberRoomFeedback>().Single().IsWarningVisible);Assert.IsTrue(All<ScrubberRoomFeedback>().Single().Alert.isPlaying);Assert.IsFalse(All<ScrubberRoomFeedback>().Single().RunningSound.isPlaying);
                var notification=All<ScrubberTaskNotificationView>().Single();Assert.IsFalse(notification.IsVisible);Assert.AreEqual(0,notification.PresentationCount);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
            }
        }
        [UnityTest] public IEnumerator ScrubberSessionRejectsProductionWithoutWritingEncounterItems()
        {
            File.WriteAllText(savePath,"SCRUBBER-TO-PRODUCTION-SENTINEL");var bytes=File.ReadAllBytes(savePath);
            yield return Load(Demo);yield return null;var root=All<GameCompositionRoot>().Single();var session=Field<ContainerSaveSession>(root,"session");
            Assert.IsTrue(All<WorldPickup>().Single().Interact(null).IsSuccess);
            LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Demo and production sessions cannot be mixed"));
            yield return Load("Assets/_Project/Scenes/Wakeup.unity");yield return null;
            Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.IsTrue(root.UsesDisposableSession);Assert.AreEqual(RuntimeStartupState.Failed,root.State);Assert.AreSame(session,Field<ContainerSaveSession>(root,"session"));
            Assert.IsNull(All<PlayerInventory>().Single().Container);CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
        }
        [UnityTest] public IEnumerator ProductionSessionRejectsScrubberWithoutChangingInventoryOrSave()
        {
            File.WriteAllText(savePath,JsonUtility.ToJson(new SaveGameData()));
            yield return Load("Assets/_Project/Scenes/Wakeup.unity");yield return null;
            var root=All<GameCompositionRoot>().Single();Assert.AreEqual(RuntimeStartupState.Ready,root.State);Assert.IsFalse(root.UsesDisposableSession);
            var session=Field<ContainerSaveSession>(root,"session");var inventory=All<PlayerInventory>().Single().Container;
            session.Commands.Grant(inventory.Handle,"c756a156ba7f34cb2b65e32264413a60",1);Field<SaveGameService>(root,"persistence").Flush();var bytes=File.ReadAllBytes(savePath);
            LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("Demo and production sessions cannot be mixed"));
            yield return Load(Demo);yield return null;Assert.AreSame(root,All<GameCompositionRoot>().Single());Assert.AreEqual(RuntimeStartupState.Failed,root.State);
            Assert.AreSame(session,Field<ContainerSaveSession>(root,"session"));Assert.IsFalse(All<ScrubberEquipment>().Single().State.FuseInstalled);Assert.IsFalse(All<ScrubberScreenView>().Single().Open());CollectionAssert.AreEqual(bytes,File.ReadAllBytes(savePath));
        }
    }
}
#endif
