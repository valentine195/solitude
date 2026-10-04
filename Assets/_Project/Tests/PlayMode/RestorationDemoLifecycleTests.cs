#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Items;
using SOLITUDE.SaveLoad;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.SolitudeStart;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class RestorationDemoLifecycleTests
    {
        private const string Demo = "Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity";
        private static GameCompositionRoot Root => Object.FindFirstObjectByType<GameCompositionRoot>();
        private static T Field<T>(object o, string n) => (T)o.GetType().GetField(n, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private string savePath; private byte[] original;
        [UnitySetUp] public IEnumerator Prepare()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-solitudeSaveDirectory");
            Assert.GreaterOrEqual(i, 0, "Requires isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            savePath = Path.Combine(args[i + 1], "solitude-save.json");
            original = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
            if (original == null) { if (File.Exists(savePath)) File.Delete(savePath); }
            else File.WriteAllBytes(savePath, original);
        }
        private static AsyncOperation LoadDemo() => EditorSceneManager.LoadSceneAsyncInPlayMode(Demo, new LoadSceneParameters(LoadSceneMode.Single));
        [UnityTest] public IEnumerator DemoCompletesReloadsAndNeverReadsOrWritesNormalSave()
        {
            File.WriteAllText(savePath, "DEMO-SAVE-ISOLATION-SENTINEL");
            yield return LoadDemo(); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.IsTrue(Root.UsesDisposableSession);
            var files = All<RestorationDemoSession>().Single().Files;
            Assert.IsNotNull(files); Assert.Greater(files.Reads, 0);
            var main = All<RestorationJunction>().Single(j => j.name == "Auxiliary power socket");
            Assert.IsFalse(main.Interact(null).IsSuccess);
            var cells = All<WorldPickup>().Where(p => p.Definition.Item.ItemId == "c756a156ba7f34cb2b65e32264413a60").ToArray();
            Assert.AreEqual(2, cells.Length); Assert.IsTrue(cells[0].Interact(null).IsSuccess);
            Assert.IsTrue(main.Interact(null).IsSuccess);
            Assert.AreEqual(1, All<RestorableSystem>().Count(s => s.IsRestored));
            var door = All<SolitudeSlidingDoor>().Single(); Assert.IsFalse(door.IsOpen); Assert.IsTrue(door.Interact(null).IsSuccess);
            yield return new WaitForSeconds(1f); Assert.IsTrue(door.IsOpen);
            Assert.AreEqual(1, All<RestorationTerminal>().Count(t => t.Interact(null).IsSuccess));
            Assert.IsTrue(cells[1].Interact(null).IsSuccess);
            Assert.IsTrue(All<RestorationJunction>().Single(j => j != main).Interact(null).IsSuccess);
            Assert.AreEqual(2, All<RestorationTerminal>().Count(t => t.Interact(null).IsSuccess));
            Field<SaveGameService>(Root, "persistence").Flush(); Assert.Greater(files.Writes, 0);
            Assert.AreEqual("DEMO-SAVE-ISOLATION-SENTINEL", File.ReadAllText(savePath));
            var previous = Root;
            yield return LoadDemo(); yield return null;
            Assert.AreSame(previous, Root); Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored));
            Assert.IsTrue(All<RestorationTerminal>().All(t => !t.IsPowered));
            Assert.IsTrue(All<WorldPickup>().Where(p => p.Definition.Item.ItemId == "c756a156ba7f34cb2b65e32264413a60").All(p => p.gameObject.activeSelf));
            var inventory = All<SOLITUDE.Player.PlayerInventory>().Single().Container;
            Assert.IsTrue(inventory.GetSlots().All(s => s.IsEmpty));
            Assert.IsTrue(All<WorldPickup>().First(p => p.Definition.Item.ItemId == "c756a156ba7f34cb2b65e32264413a60").Interact(null).IsSuccess);
            Assert.IsTrue(All<RestorationJunction>().First().Interact(null).IsSuccess);
            Assert.AreEqual("DEMO-SAVE-ISOLATION-SENTINEL", File.ReadAllText(savePath));
        }
        [UnityTest] public IEnumerator ExistingProductionSessionRejectsDemoWithoutReset()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Wakeup.unity", LoadSceneMode.Single); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.IsFalse(Root.UsesDisposableSession);
            var session = Field<ContainerSaveSession>(Root, "session");
            var pickup = All<WorldPickup>().First(p => p.gameObject.activeSelf); string id = pickup.PickupId;
            Assert.IsTrue(pickup.Interact(null).IsSuccess); Field<SaveGameService>(Root, "persistence").Flush();
            var bytes = File.ReadAllBytes(savePath);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Demo and production sessions cannot be mixed"));
            yield return LoadDemo(); yield return null;
            Assert.AreEqual(RuntimeStartupState.Failed, Root.State); Assert.AreSame(session, Field<ContainerSaveSession>(Root, "session"));
            Assert.IsTrue(session.IsPickupCollected(id)); Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(savePath));
        }
    }
}
#endif
