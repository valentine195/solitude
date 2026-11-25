#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Containers;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class StartingLoadoutRecoveryTests
    {
        [UnitySetUp] public IEnumerator Prepare() { yield return WeaponSystemTestFixtures.Load(); }
        [UnityTearDown] public IEnumerator Cleanup() { yield return WeaponSystemTestFixtures.Unload(); }
        private static int Wrenches(SOLITUDE.Containers.IContainerReader reader) => reader.GetSlots()
            .Where(s => s.Stack?.ItemId == WeaponSystemTestFixtures.Wrench.ItemId).Sum(s => s.Stack.Quantity);
        private static SaveGameService Service => WeaponSystemTestFixtures.One<SaveGameService>();
        private static string Path => WeaponSystemTestFixtures.Field<SOLITUDE.Application.Persistence.SaveRecoveryStore>(Service, "store").Path;
        private static SaveJsonCodec Codec => new SaveJsonCodec(Resources.Load<ItemDatabase>("Database").BuildRuntimeCatalog());
        private static IEnumerator Restart()
        {
            string destination = Codec.Decode(File.ReadAllText(Path)).survivor.scenePath;
            Object.Destroy(WeaponSystemTestFixtures.Root.gameObject); yield return null;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            float deadline = Time.realtimeSinceStartup + 5;
            while (WeaponSystemTestFixtures.Root.State == RuntimeStartupState.Unready && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, WeaponSystemTestFixtures.Root.State);
            Assert.IsTrue(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(destination).isLoaded, "Restore must finish loading the saved destination.");
        }
        [UnityTest] public IEnumerator MovedWrenchSurvivesArrivalAndReloadWithoutAnotherGrant()
        {
            var inventory = WeaponSystemTestFixtures.One<PlayerInventory>().Container;
            var slot = WeaponSystemTestFixtures.Bar.Container.GetSlot(0);
            Assert.AreEqual(CommandStatus.Applied, WeaponSystemTestFixtures.Session.Commands.Transfer(slot.Address, inventory.GetSlot(0).Address).Status);
            Assert.IsTrue(Service.TryFlush());
            yield return WeaponSystemTestFixtures.Root.TransitionTo("Assets/_Project/Tests/Fixtures/LifecycleResume.unity"); yield return null;
            Assert.AreEqual(SurvivorSceneEntry.SceneArrival, WeaponSystemTestFixtures.One<SurvivorSuccessionComposition>().EntryKind);
            Assert.AreEqual(1, Wrenches(WeaponSystemTestFixtures.One<PlayerInventory>().Container));
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.AreEqual(-1, WeaponSystemTestFixtures.Bar.ActiveIndex);
            yield return Restart();
            Assert.AreEqual(1, Wrenches(WeaponSystemTestFixtures.One<PlayerInventory>().Container));
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
        }
        [UnityTest] public IEnumerator LostWrenchAndLegacyEmptyEquipmentNeverReceiveAnotherGrant()
        {
            var slot = WeaponSystemTestFixtures.Bar.Container.GetSlot(0);
            Assert.AreEqual(CommandStatus.Applied, WeaponSystemTestFixtures.Session.Commands.Remove(slot.Address, 1).Status);
            Assert.IsTrue(Service.TryFlush()); yield return Restart();
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.AreEqual(-1, WeaponSystemTestFixtures.Bar.ActiveIndex);
            var legacy = Codec.Decode(File.ReadAllText(Path)); legacy.version = 3;
            string path = Path;
            Object.Destroy(WeaponSystemTestFixtures.Root.gameObject); yield return null;
            File.WriteAllText(path, Codec.Encode(legacy));
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, WeaponSystemTestFixtures.Root.State);
            Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.AreEqual(4, Codec.Decode(File.ReadAllText(path)).version);
        }
        [UnityTest] public IEnumerator RejectedThenCommittedSuccessionKeepsGrantCompleteAndReleasesTheSwing()
        {
            Time.timeScale = 0;
            var actions = WeaponSystemTestFixtures.Actions; Assert.IsTrue(actions.Request()); actions.Advance(.05);
            var runner = actions.Runner; var feature = WeaponSystemTestFixtures.One<SurvivorSuccessionComposition>();
            var survivor = feature.Coordinator.Current; var prefab = feature.RecoveryPrefab;
            feature.ConfigureRecovery(null);
            try { Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(survivor.Id)); }
            finally { feature.ConfigureRecovery(prefab); }
            Assert.AreSame(survivor, feature.Coordinator.Current); Assert.IsFalse(runner.IsExecuting);
            Assert.AreEqual(1, Wrenches(WeaponSystemTestFixtures.Bar.Container));
            Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(survivor.Id));
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.AreEqual(-1, WeaponSystemTestFixtures.Bar.ActiveIndex);
            Assert.AreEqual(1, feature.RecoveryContainers.Sum(c => Wrenches(c.Container)));
            Assert.AreNotSame(runner, actions.Runner); Assert.IsFalse(actions.Runner.IsExecuting); Assert.AreEqual(0, actions.Runner.LockoutRemaining);
            Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
            Time.timeScale = 1; Assert.IsTrue(Service.TryFlush()); yield return Restart();
            Assert.AreEqual(0, Wrenches(WeaponSystemTestFixtures.Bar.Container)); Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
        }
    }
}
#endif
