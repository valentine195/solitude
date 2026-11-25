#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SOLITUDE.Application;
using NUnit.Framework;
using SOLITUDE.Combat;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.Application.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public sealed class StartingWeaponTests
    {
        private Gamepad pad;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        [UnitySetUp] public IEnumerator Prepare()
        {
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); yield return WeaponSystemTestFixtures.Load();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            InputSystem.RemoveDevice(pad); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            yield return WeaponSystemTestFixtures.Unload();
        }
        private sealed class Files : ISaveFiles
        {
            private readonly Dictionary<string, string> data = new();
            public bool FailComplete = true;
            public bool Exists(string path) => data.ContainsKey(path);
            public string Read(string path) => data[path];
            public void WriteDurable(string path, string text)
            {
                if (FailComplete && text.Contains("\"startingLoadoutInitialized\": true")) throw new IOException("Fixture starting equipment write failed");
                data[path] = text;
            }
            public void Delete(string path) => data.Remove(path);
            public void Move(string from, string to) { data[to] = data[from]; data.Remove(from); }
            public void Replace(string from, string to, string backup) { data[backup] = data[to]; Move(from, to); }
        }
        [UnityTest] public IEnumerator FailedDurableGrantKeepsControlsClosedAndRetrySelectsWithoutDuplicating()
        {
            var files = new Files(); var service = Object.FindFirstObjectByType<SaveGameService>();
            var codec = new SaveJsonCodec(Resources.Load<ItemDatabase>("Database").BuildRuntimeCatalog());
            service.Configure(new SaveRecoveryStore("starting-equipment-fixture", files, codec));
            LogAssert.Expect(LogType.Error, "[SaveGameService] Failed to save: Fixture starting equipment write failed");
            LogAssert.Expect(LogType.Error, "[GameCompositionRoot] Runtime unavailable; save preserved: Starting equipment initialization failed: Starting equipment could not be saved.");
            WeaponSystemTestFixtures.Root.StartNewSave(); yield return null; yield return null;
            Assert.AreEqual(SOLITUDE.Composition.RuntimeStartupState.Failed, WeaponSystemTestFixtures.Root.State);
            Assert.IsFalse(WeaponSystemTestFixtures.Policy.Ready); Assert.IsFalse(WeaponSystemTestFixtures.Actions.Request());
            Assert.IsTrue(WeaponSystemTestFixtures.Session.StartingLoadoutInitialized);
            Assert.AreEqual(-1, WeaponSystemTestFixtures.Bar.ActiveIndex);
            Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlots().Where(s => !s.IsEmpty).Sum(s => s.Stack.Quantity));
            Assert.IsFalse(codec.Decode(files.Read("starting-equipment-fixture")).startingLoadoutInitialized);
            files.FailComplete = false; Assert.IsTrue(WeaponSystemTestFixtures.Root.RetryStartingEquipment());
            Assert.IsTrue(WeaponSystemTestFixtures.Policy.Gameplay); Assert.AreEqual(0, WeaponSystemTestFixtures.Bar.ActiveIndex);
            Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlots().Where(s => !s.IsEmpty).Sum(s => s.Stack.Quantity));
            Assert.IsTrue(codec.Decode(files.Read("starting-equipment-fixture")).startingLoadoutInitialized);
        }
        [UnityTest] public IEnumerator TemporaryLoadoutConfigurationSelectsItsConfiguredSlotAfterAwakening()
        {
            var loadout = ScriptableObject.CreateInstance<StartingLoadoutDefinition>();
            try
            {
                loadout.Hotbar = new[] { new StartingLoadoutDefinition.Grant { Item = WeaponSystemTestFixtures.Wrench, Quantity = 2 } }; loadout.SelectedSlot = 1;
                var spec = loadout.Compile(Resources.Load<ItemDatabase>("Database").BuildRuntimeCatalog());
                typeof(SOLITUDE.Composition.GameCompositionRoot).GetField("startingLoadout", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(WeaponSystemTestFixtures.Root, spec);
                WeaponSystemTestFixtures.Root.StartNewSave(); yield return null; yield return null;
                Assert.AreEqual(SOLITUDE.Composition.RuntimeStartupState.Ready, WeaponSystemTestFixtures.Root.State);
                Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.ActiveIndex);
                Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(0).Stack.Quantity);
                Assert.AreEqual(1, WeaponSystemTestFixtures.Bar.Container.GetSlot(1).Stack.Quantity);
                Assert.IsTrue(WeaponSystemTestFixtures.Actions.Request());
            }
            finally { Object.DestroyImmediate(loadout); }
        }
        [UnityTest] public IEnumerator NewRunIsDurablyEquippedAndActualPrimaryUsesConfiguredWrench()
        {
            var bar = WeaponSystemTestFixtures.Bar; var session = WeaponSystemTestFixtures.Session;
            Assert.AreEqual(0, bar.ActiveIndex); Assert.AreEqual(1, bar.Container.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(WeaponSystemTestFixtures.Wrench.ItemId, bar.Container.GetSlot(0).Stack.ItemId);
            Assert.IsTrue(session.StartingLoadoutInitialized);
            var service = Object.FindFirstObjectByType<SaveGameService>();
            var store = WeaponSystemTestFixtures.Field<SaveRecoveryStore>(service, "store");
            var codec = new SaveJsonCodec(Resources.Load<ItemDatabase>("Database").BuildRuntimeCatalog());
            var durable = codec.Decode(File.ReadAllText(store.Path)); Assert.AreEqual(4, durable.version); Assert.IsTrue(durable.startingLoadoutInitialized);
            Assert.AreEqual(WeaponSystemTestFixtures.Wrench.ItemId, durable.containers.Find(c => c.saveableId == ContainerSaveSession.PlayerHotbarId).state.slots[0].itemId);
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); InputSystem.Update(); yield return null; yield return null;
            Assert.IsTrue(WeaponSystemTestFixtures.Actions.Runner.IsExecuting);
            var wrenchAttack = (SOLITUDE.Items.MeleeAttackAction)WeaponSystemTestFixtures.Wrench.Actions[0].Action;
            Assert.AreSame(wrenchAttack.Configuration.PosedWeaponFrames[0], WeaponSystemTestFixtures.Actions.GetComponent<MeleeSwingView>().HeldRenderer.sprite);
        }
    }
}
#endif
