#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Hotbar;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    // Opt-in fixture: authored Wakeup + actual production root/input/HUD, isolated saves only.
    public static class WeaponSystemTestFixtures
    {
        public static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        public static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Single();
        public static GameCompositionRoot Root => One<GameCompositionRoot>();
        public static ContainerSaveSession Session => Field<ContainerSaveSession>(Root, "session");
        public static InputPolicyCoordinator Policy => Field<InputPolicyCoordinator>(Root, "policy");
        public static UnityInputHost Input => One<UnityInputHost>();
        public static PlayerItemActionComposition Actions => One<PlayerItemActionComposition>();
        public static SOLITUDE.Hotbar.Hotbar Bar => One<SOLITUDE.Hotbar.Hotbar>();
        public static ItemDefinition Wrench => AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Features/Items/Weapons/MaintenanceWrench/MaintenanceWrench.asset");
        public static IEnumerator Load()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Weapon fixture requires explicitly isolated saves.");
            foreach (var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Root.StartNewSave(); yield return null; yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.IsTrue(Policy.Gameplay);
            Assert.AreEqual(Wrench.ItemId, Bar.Container.GetSlot(0).Stack.ItemId);
            Assert.AreEqual(1, Bar.Container.GetSlot(0).Stack.Quantity);
            Bar.SetActive(0); yield return null;
        }
        public static IEnumerator LoadSec0Encounter()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Weapon fixture requires explicitly isolated saves.");
            foreach (var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/Sec0Patrol.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Assert.IsTrue(Root.UsesDisposableSession); Assert.IsTrue(Policy.Gameplay);
            Assert.AreEqual(Wrench.ItemId, Bar.Container.GetSlot(0).Stack.ItemId);
            Assert.AreEqual(1, Bar.Container.GetSlot(0).Stack.Quantity);
            Bar.SetActive(0); yield return null;
        }
        // Unrelated handoff/resume fixtures seed precise equipment sets after proving real initialization.
        public static void ClearStartingWeaponForSeededScenario()
        {
            Assert.IsTrue(Session.StartingLoadoutInitialized);
            var slot = Bar.Container.GetSlot(0); Assert.AreEqual(Wrench.ItemId, slot.Stack.ItemId); Assert.AreEqual(1, slot.Stack.Quantity);
            Assert.AreEqual(CommandStatus.Applied, Session.Commands.Remove(slot.Address, 1).Status); Bar.SetActive(-1);
            Assert.IsTrue(One<SaveGameService>().TryFlush());
        }
        public static IEnumerator Unload()
        {
            foreach (var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
        }
    }
}
#endif
