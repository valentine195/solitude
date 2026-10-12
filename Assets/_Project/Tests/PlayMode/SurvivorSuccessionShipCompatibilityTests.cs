#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Application.Survivors;
using SOLITUDE.Composition;
using SOLITUDE.Containers;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using SOLITUDE.World.Restoration;
using SOLITUDE.World.ScrubberEncounter;
using SOLITUDE.World.SolitudeStart;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class SurvivorSuccessionShipCompatibilityTests
    {
        private const string Battery = "c756a156ba7f34cb2b65e32264413a60";
        private const string RestorationScene = "Assets/_Project/Scenes/Demos/ShipSystemRestoration.unity";
        private const string ScrubberScene = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static T One<T>() where T : Object => All<T>().Single();
        private static T Field<T>(object o, string n) => (T)o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
        private static GameCompositionRoot Root => All<GameCompositionRoot>().SingleOrDefault();
        private static SceneBindings Scope => One<SceneBindings>();
        private static PlayerInteractor Player => One<PlayerInteractor>();
        private static SurvivorSuccessionComposition Feature => One<SurvivorSuccessionComposition>();
        private static ContainerCommandService Commands => Field<ContainerSaveSession>(Root, "session").Commands;
        private static IContainerReader Inventory => One<PlayerInventory>().Container;
        private static IContainerReader Hotbar => One<SOLITUDE.Hotbar.Hotbar>().Container;
        private static Dictionary<string, int> Contents(params IContainerReader[] readers) => readers.SelectMany(r => r.GetSlots())
            .Where(s => !s.IsEmpty).GroupBy(s => s.Stack.ItemId).ToDictionary(g => g.Key, g => g.Sum(s => s.Stack.Quantity));
        private static void SameContents(Dictionary<string, int> expected, params IContainerReader[] readers)
        { var actual = Contents(readers); CollectionAssert.AreEquivalent(expected.Keys, actual.Keys); foreach (var item in expected) Assert.AreEqual(item.Value, actual[item.Key]); }
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        private static AsyncOperation Load(string path) => EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Requires isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Load("Assets/_Project/Scenes/Wakeup.unity"); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Root.StartNewSave(); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private IEnumerator Focus(Component target)
        {
            yield return new WaitForFixedUpdate();
            Player.GetComponent<Rigidbody2D>().position = (Vector2)target.transform.position + Vector2.down * .6f;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.AreSame(target, Field<IInteractable>(Player, "currentTarget"), "Actual proximity must select the intended target.");
        }
        private IEnumerator Use(Component target)
        { yield return Focus(target); Keys(Key.E); Keys(); yield return null; }
        private IEnumerator Recover(SurvivorRecoveryContainer cache)
        {
            yield return Use(cache);
            var modal = Field<ContainerModalCoordinator>(Scope, "modal"); Assert.AreEqual(ContainerModalKind.Transfer, modal.Kind);
            Assert.AreEqual(cache.Container.Handle, modal.Target.Value);
            var view = One<ContainerTransferModalView>();
            var sources = Field<List<ContainerSlotView>>(view.TargetController.GetComponent<ContainerView>(), "slotViews");
            var targets = Field<List<ContainerSlotView>>(view.InventoryController.GetComponent<ContainerView>(), "slotViews");
            var pointer = new PointerEventData(EventSystem.current);
            for (var i = 0; i < cache.Container.Capacity; i++)
            {
                if (cache.Container.GetSlot(i).IsEmpty) continue;
                var target = Inventory.GetSlots().First(s => s.IsEmpty).Address.Index;
                ExecuteEvents.Execute(sources[i].gameObject, pointer, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(targets[target].gameObject, pointer, ExecuteEvents.dropHandler);
                ExecuteEvents.Execute(sources[i].gameObject, pointer, ExecuteEvents.endDragHandler);
            }
            Assert.IsTrue(cache.Container.GetSlots().All(s => s.IsEmpty)); view.Close(); yield return null;
            Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest] public IEnumerator WakeupPickupLockerAndEquippedSuccessionUseRetainedSceneAndUI()
        {
            var root = Root; var scope = Scope; var scene = Player.gameObject.scene;
            var first = Feature.Coordinator.Current; var inventory = Inventory; var hotbar = Hotbar;
            Assert.AreEqual((Vector2)Feature.AwakeningPoint.transform.position, Player.GetComponent<Rigidbody2D>().position);
            Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            var pickups = All<WorldPickup>().Where(p => p.CanInteract(Player)).Take(2).ToArray(); Assert.AreEqual(2, pickups.Length);
            yield return Use(pickups[0]); Assert.IsFalse(pickups[0].gameObject.activeSelf);
            Assert.AreEqual(CommandStatus.Applied, Commands.Transfer(Inventory.GetSlots().First(s => !s.IsEmpty).Address, Hotbar.GetSlot(0).Address).Status);
            yield return Use(pickups[1]); Assert.IsFalse(pickups[1].gameObject.activeSelf);
            var locker = All<LockerContainer>().First(); yield return Use(locker.GetComponent<Locker>());
            var lockerReader = locker.Container; Assert.IsNotNull(lockerReader); var lockerContents = Contents(lockerReader);
            One<ContainerTransferModalView>().Close(); yield return null;
            var expected = Contents(Inventory, Hotbar); Assert.IsTrue(Inventory.GetSlots().Any(s => !s.IsEmpty)); Assert.IsTrue(Hotbar.GetSlots().Any(s => !s.IsEmpty));
            Player.GetComponent<Rigidbody2D>().position = (Vector2)Feature.AwakeningPoint.transform.position + Vector2.right * .7f;
            var death = Player.GetComponent<Rigidbody2D>().position;
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(first.Id));
            Assert.AreEqual(SurvivorLifeStatus.Dead, first.Status); Assert.AreNotEqual(first.Id, Feature.Coordinator.Current.Id);
            Assert.AreSame(root, Root); Assert.AreSame(scope, Scope); Assert.AreEqual(scene, Player.gameObject.scene);
            Assert.AreSame(inventory, Inventory); Assert.AreSame(hotbar, Hotbar); Assert.AreEqual(0, Contents(Inventory, Hotbar).Count);
            Assert.AreEqual(100, One<PlayerVitalsController>().Vitals.Health.Current); Assert.AreEqual(100, One<PlayerVitalsController>().Vitals.Oxygen.Current);
            Assert.AreEqual(-1, Field<HotbarSelectionState>(scope, "selection").ActiveIndex);
            Assert.IsTrue(pickups.All(p => !p.gameObject.activeSelf)); Assert.AreSame(lockerReader, locker.Container); SameContents(lockerContents, lockerReader);
            var cache = Feature.RecoveryContainers.Single(); Assert.AreEqual(death, (Vector2)cache.DeathPosition); SameContents(expected, cache.Container);
            yield return Recover(cache); SameContents(expected, Inventory, Hotbar);
            var second = Feature.Coordinator.Current;
            Player.GetComponent<Rigidbody2D>().position = (Vector2)Feature.AwakeningPoint.transform.position + Vector2.left * .7f;
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(second.Id));
            Assert.AreEqual(2, Feature.RecoveryContainers.Count); Assert.AreEqual(2, Feature.AcknowledgmentCount);
            Assert.AreSame(cache.Container, Commands.Read(cache.Container.Handle));
            SameContents(expected, Feature.RecoveryContainers.Single(c => c.SourceSurvivorId == second.Id).Container);
            Assert.IsTrue(pickups.All(p => !p.gameObject.activeSelf)); SameContents(lockerContents, lockerReader);
        }

        [UnityTest] public IEnumerator RestorationTerminalDoorAndLaterRepairRemainLiveUntilSceneReload()
        {
            Object.Destroy(Root.gameObject); yield return null; yield return Load(RestorationScene); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); ComposeRestorationFixture();
            var root = Root; var inventory = Inventory; var main = All<RestorationJunction>().Single(j => j.name == "Auxiliary power socket");
            var other = All<RestorationJunction>().Single(j => j != main);
            var pickups = All<WorldPickup>().Where(p => p.Definition.Item.ItemId == Battery && p.gameObject.activeSelf).ToArray();
            yield return Use(pickups[0]); yield return Use(main);
            var system = All<RestorableSystem>().Single(s => s.IsRestored); var state = system.State;
            var terminal = All<RestorationTerminal>().Single(t => t.IsPowered); var door = One<SolitudeSlidingDoor>();
            yield return Use(pickups[1]); Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Hotbar.Handle, Battery, 2).Status);
            yield return Use(door); yield return new WaitForSeconds(1); Assert.IsTrue(door.IsOpen);
            var blocker = Field<Collider2D>(door, "blocker"); Assert.IsFalse(blocker.enabled);
            var locker = One<LockerContainer>(); Assert.IsTrue(locker.EnsureInitialized()); var lockerReader = locker.Container; var stored = Contents(lockerReader);
            var expected = Contents(Inventory, Hotbar); var first = Feature.Coordinator.Current;
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(first.Id));
            Assert.AreSame(root, Root); Assert.AreSame(inventory, Inventory); Assert.AreSame(state, system.State);
            Assert.IsTrue(system.IsRestored); Assert.IsTrue(terminal.IsPowered); Assert.IsTrue(door.IsOpen); Assert.IsFalse(blocker.enabled);
            Assert.IsTrue(pickups.All(p => !p.gameObject.activeSelf)); SameContents(stored, lockerReader);
            var cache = Feature.RecoveryContainers.Single(); var handle = cache.Container.Handle; SameContents(expected, cache.Container);
            yield return Recover(cache); SameContents(expected, Inventory, Hotbar);
            yield return Use(other); Assert.AreEqual(2, All<RestorableSystem>().Count(s => s.IsRestored));
            Assert.AreEqual(2, All<RestorationTerminal>().Count(t => t.IsPowered)); Assert.AreSame(inventory, Inventory);
            Player.GetComponent<Rigidbody2D>().position = (Vector2)door.transform.position + Vector2.down * 1.8f; Physics2D.SyncTransforms(); yield return null;
            Keys(Key.W); for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate(); Keys();
            Assert.Greater(Player.transform.position.y, door.transform.position.y + 1.2f, "Successor traverses the powered doorway with production movement.");
            yield return Load(RestorationScene); yield return null;
            Assert.AreSame(root, Root); Assert.IsTrue(All<RestorableSystem>().All(s => !s.IsRestored));
            Assert.IsTrue(All<RestorationTerminal>().All(t => !t.IsPowered)); Assert.IsFalse(One<SolitudeSlidingDoor>().IsOpen);
            Assert.IsNull(Commands.Read(handle)); Assert.IsEmpty(All<SurvivorSuccessionComposition>());
            Assert.AreEqual(2, All<WorldPickup>().Count(p => p.Definition.Item.ItemId == Battery && p.gameObject.activeSelf));
        }

        [UnityTest] public IEnumerator AuthoredScrubberKeepsRunningAndReaderBindingsAcrossDeathButResetsOnReload()
        {
            Object.Destroy(Root.gameObject); yield return null; yield return Load(ScrubberScene); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); Feature.Validate();
            var root = Root; var inventory = Inventory; var equipment = One<ScrubberEquipment>(); var state = equipment.State;
            var pickup = One<WorldPickup>(); yield return Use(pickup); equipment.Install(); equipment.Restart();
            yield return new WaitForSeconds(.15f); Assert.IsTrue(state.FuseInstalled); Assert.IsTrue(state.Running); var progress = state.RecoverySeconds;
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, ScrubberEquipment.FuseItemId, 4).Status);
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Hotbar.Handle, ScrubberEquipment.FuseItemId, 2).Status);
            Player.GetComponent<Rigidbody2D>().position = new Vector2(23, 5.5f);
            var first = Feature.Coordinator.Current; Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(first.Id));
            var second = Feature.Coordinator.Current; Assert.AreNotEqual(first.Id, second.Id); Assert.AreSame(root, Root);
            Assert.AreSame(state, equipment.State); Assert.IsTrue(state.FuseInstalled); Assert.IsTrue(state.Running);
            Assert.AreSame(inventory, Inventory); Assert.AreSame(inventory, Field<IContainerReader>(equipment, "inventory"));
            Assert.IsTrue(inventory.IsAvailable); Assert.IsFalse(pickup.gameObject.activeSelf);
            yield return new WaitForSeconds(.1f); Assert.Greater(state.RecoverySeconds, progress);
            var cache = Feature.RecoveryContainers.Single(); var handle = cache.Container.Handle;
            Assert.AreEqual(6, cache.Container.GetSlots().Sum(s => s.Stack?.Quantity ?? 0)); yield return Recover(cache);
            Assert.AreEqual(6, Inventory.GetSlots().Sum(s => s.Stack?.Quantity ?? 0));
            Assert.IsTrue(One<ScrubberScreenView>().Open()); One<ScrubberScreenView>().Close();
            Assert.AreEqual(1, Feature.AcknowledgmentCount);
            yield return Load(ScrubberScene); yield return null;
            Assert.AreSame(root, Root); Assert.IsFalse(One<ScrubberEquipment>().State.FuseInstalled); Assert.IsFalse(One<ScrubberEquipment>().State.Running);
            Assert.AreEqual(0, One<ScrubberEquipment>().State.RecoverySeconds); Assert.IsNull(Commands.Read(handle));
            Assert.IsEmpty(Feature.RecoveryContainers); Assert.AreNotEqual(second.Id, Feature.Coordinator.Current.Id);
            Assert.AreEqual(0, Feature.AcknowledgmentCount); Assert.IsTrue(One<WorldPickup>().gameObject.activeSelf);
        }
        private static void ComposeRestorationFixture()
        {
            var point = new GameObject("RestorationTestAwakeningPoint").AddComponent<SurvivorAwakeningPoint>(); point.transform.position = Player.transform.position;
            var avatar = Scope.gameObject.AddComponent<SurvivorAvatarAdapter>(); avatar.Configure(Player);
            var feature = Scope.gameObject.AddComponent<SurvivorSuccessionComposition>(); feature.Configure(point, avatar);
            feature.ConfigureRecovery(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab").GetComponent<SurvivorRecoveryContainer>());
            var interactions = Field<SceneInteractionScope>(Scope, "interactions");
            feature.Initialize(new SceneFeatureContext(Commands, Inventory, null, Field<InputPolicyCoordinator>(Root, "policy"), One<UnityInputHost>(),
                null, "ship-tests", Player, interactions.Events, Hotbar, Field<HotbarSelectionState>(Scope, "selection"),
                Scope.CloseForTransition, interactions.Feedback, Scope.Open, Scope.Close));
        }
    }
}
#endif
