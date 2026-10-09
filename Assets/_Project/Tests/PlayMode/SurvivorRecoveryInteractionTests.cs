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
using SOLITUDE.Core.Interaction;
using SOLITUDE.Features.Interactables;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class SurvivorRecoveryInteractionTests
    {
        private const string Item = "c756a156ba7f34cb2b65e32264413a60";
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static GameCompositionRoot Root => One<GameCompositionRoot>();
        private static SceneBindings Scope => One<SceneBindings>();
        private static ContainerCommandService Commands => Field<ContainerSaveSession>(Root, "session").Commands;
        private static IContainerReader Inventory => One<PlayerInventory>().Container;
        private static ContainerModalCoordinator Modal => Field<ContainerModalCoordinator>(Scope, "modal");
        private static ContainerGestureCoordinator Gestures => Field<ContainerGestureCoordinator>(Root, "gestures");
        private static SurvivorRecoveryContainer Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab").GetComponent<SurvivorRecoveryContainer>();
        private static SceneFeatureContext Context => new SceneFeatureContext(Commands, Inventory, null,
            Field<InputPolicyCoordinator>(Root, "policy"), One<UnityInputHost>(), null, "recovery-tests",
            One<PlayerInteractor>(), null, openContainer: Scope.Open, closeContainer: Scope.Close);
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }

        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Requires isolated verification runner.");
            var root = Object.FindFirstObjectByType<GameCompositionRoot>();
            if (root != null) { Object.Destroy(root.gameObject); yield return null; }
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Root.StartNewSave(); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var cache in Object.FindObjectsByType<SurvivorRecoveryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            { cache.Release(); Object.Destroy(cache.gameObject); }
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            var root = Object.FindFirstObjectByType<GameCompositionRoot>(); if (root != null) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private static SurvivorRecoveryContainer Seed(int quantity = 3)
        {
            var cache = SurvivorRecoveryContainer.Prepare(Prefab, Context, Guid.NewGuid(),
                One<PlayerInteractor>().transform.position + Vector3.right * .7f, 2);
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(cache.Container.Handle, Item, quantity).Status);
            cache.Publish(); return cache;
        }
        private IEnumerator ApproachAndOpen(SurvivorRecoveryContainer cache)
        {
            var player = One<PlayerInteractor>(); player.GetComponent<Rigidbody2D>().position = cache.transform.position;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.AreSame(cache, Field<IInteractable>(player, "currentTarget"), "Real proximity must focus recovery.");
            Keys(Key.E); Keys(); yield return null;
            Assert.AreEqual(ContainerModalKind.Transfer, Modal.Kind);
            Assert.AreEqual(cache.Container.Handle, Modal.Target.Value);
            Assert.AreEqual(cache.Label, One<ContainerTransferModalView>().TargetController.CurrentSource.Label);
        }

        [UnityTest] public IEnumerator RecoveryUsesRealFocusAndTransferUIAndConservesPartialAndRepeatedTransfers()
        {
            var sourceId = Guid.NewGuid(); var position = One<PlayerInteractor>().transform.position + Vector3.right * .7f;
            var cache = SurvivorRecoveryContainer.Prepare(Prefab, Context, sourceId, position, 2);
            Assert.IsFalse(cache.gameObject.activeInHierarchy); Assert.IsFalse(cache.CanInteract(One<PlayerInteractor>()));
            Assert.AreEqual(sourceId, cache.SourceSurvivorId); Assert.AreEqual(position, cache.DeathPosition);
            Assert.Throws<InvalidOperationException>(() => cache.Publish());
            Commands.Grant(cache.Container.Handle, Item, 3); Commands.Grant(Inventory.Handle, Item, 98); cache.Publish();
            yield return ApproachAndOpen(cache);
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); Gestures.Drop(Inventory.GetSlot(0));
            Assert.AreEqual(99, Inventory.GetSlot(0).Stack.Quantity); Assert.AreEqual(2, cache.Container.GetSlot(0).Stack.Quantity);
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); Gestures.Drop(Inventory.GetSlot(0));
            Assert.AreEqual(2, cache.Container.GetSlot(0).Stack.Quantity, "Full target retains remaining recovery contents.");
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); Gestures.Drop(Inventory.GetSlot(1));
            Assert.IsTrue(cache.Container.GetSlot(0).IsEmpty);
            Assert.AreEqual(101, Inventory.GetSlots().Sum(s => s.Stack?.Quantity ?? 0));
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); Gestures.Drop(Inventory.GetSlot(1));
            Assert.AreEqual(101, Inventory.GetSlots().Sum(s => s.Stack?.Quantity ?? 0));
            Keys(Key.Escape); Keys(); yield return null;
            Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind); Assert.AreEqual(1, Time.timeScale);
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            yield return ApproachAndOpen(cache); Assert.IsTrue(cache.Container.GetSlots().All(s => s.IsEmpty));
        }

        [UnityTest] public IEnumerator FullRecoveryCapacityCanScrollAndTransferTheLastSlot()
        {
            var capacity = Inventory.Capacity + One<SOLITUDE.Hotbar.Hotbar>().Container.Capacity;
            var cache = SurvivorRecoveryContainer.Prepare(Prefab, Context, Guid.NewGuid(),
                One<PlayerInteractor>().transform.position + Vector3.right * .7f, capacity);
            var saved = new ContainerSaveData();
            saved.slots.Add(new ContainerSlotSaveData { index = capacity - 1, itemId = Item, quantity = 7 });
            Assert.AreEqual(CommandStatus.Applied, Commands.Restore(cache.Container.Handle, saved).Status);
            cache.Publish(); yield return ApproachAndOpen(cache); yield return null;
            Canvas.ForceUpdateCanvases();
            var modalView = One<ContainerTransferModalView>();
            var view = modalView.TargetController.GetComponent<ContainerView>();
            var scroll = view.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            Assert.IsNotNull(scroll); Assert.IsFalse(scroll.horizontal); Assert.IsTrue(scroll.vertical);
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height);
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { scrollDelta = Vector2.down };
            for (var i = 0; i < capacity; i++)
                UnityEngine.EventSystems.ExecuteEvents.Execute(scroll.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.scrollHandler);
            yield return null; Canvas.ForceUpdateCanvases();
            var slots = Field<System.Collections.Generic.List<ContainerSlotView>>(view, "slotViews");
            var last = slots[capacity - 1]; var corners = new Vector3[4];
            last.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var local = scroll.viewport.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(scroll.viewport.rect.xMin - .01f, scroll.viewport.rect.xMax + .01f));
                Assert.That(local.y, Is.InRange(scroll.viewport.rect.yMin - .01f, scroll.viewport.rect.yMax + .01f));
            }
            var inventoryView = modalView.InventoryController.GetComponent<ContainerView>();
            var destination = Field<System.Collections.Generic.List<ContainerSlotView>>(inventoryView, "slotViews")[0];
            UnityEngine.EventSystems.ExecuteEvents.Execute(last.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(destination.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.dropHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(last.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
            Assert.IsTrue(cache.Container.GetSlot(capacity - 1).IsEmpty);
            Assert.AreEqual(7, Inventory.GetSlot(0).Stack.Quantity);
            modalView.Close(); yield return null; yield return ApproachAndOpen(cache); yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0).Within(.01f), "Reopen begins at the first slots.");
        }

        [UnityTest] public IEnumerator LeavingAndClosingCancelGesturesAndReturnInput()
        {
            var cache = Seed(); yield return ApproachAndOpen(cache);
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); Assert.IsTrue(Gestures.Interaction.IsHolding);
            One<ContainerTransferModalView>().Close();
            Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind); Assert.IsFalse(Gestures.Interaction.IsHolding);
            yield return ApproachAndOpen(cache);
            Gestures.Begin("recovery", cache.Container.GetSlot(0));
            // The modal pauses ordinary physics. Simulate an externally caused relocation explicitly.
            var prior = Physics2D.simulationMode;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                One<PlayerInteractor>().GetComponent<Rigidbody2D>().position += Vector2.right * 5;
                Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            }
            finally { Physics2D.simulationMode = prior; }
            yield return null;
            Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind); Assert.IsFalse(Gestures.Interaction.IsHolding);
            Assert.AreEqual(3, cache.Container.GetSlot(0).Stack.Quantity);
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay); Assert.AreEqual(1, Time.timeScale);
        }

        [UnityTest] public IEnumerator RecoveryCleanupPreservesOtherOwnersAndLockerLazyLoot()
        {
            var cache = Seed(); var second = Seed();
            Assert.AreNotEqual(cache.Container.Handle, second.Container.Handle);
            Assert.AreNotEqual(cache.SourceSurvivorId, second.SourceSurvivorId);
            var locker = Object.FindObjectsByType<LockerContainer>(FindObjectsSortMode.None).First();
            Assert.IsNull(locker.Container, "Unvisited lockers remain lazy.");
            Assert.IsTrue(Scope.Open(locker)); var lockerReader = locker.Container;
            Assert.AreEqual("Locker", One<ContainerTransferModalView>().TargetController.CurrentSource.Label);
            Assert.IsTrue(lockerReader.GetSlots().Any(s => !s.IsEmpty), "Locker still generates its own loot."); Scope.Close(locker);
            yield return ApproachAndOpen(cache);
            var removed = cache.Container.Handle; var inventory = Inventory; var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            Gestures.Begin("recovery", cache.Container.GetSlot(0)); cache.Release(); cache.Release();
            Assert.IsNull(Commands.Read(removed)); Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind);
            Assert.IsFalse(Gestures.Interaction.IsHolding); Assert.AreSame(inventory, Commands.Read(inventory.Handle));
            Assert.AreSame(hotbar, Commands.Read(hotbar.Handle)); Assert.AreSame(lockerReader, Commands.Read(lockerReader.Handle));
            Assert.AreSame(second.Container, Commands.Read(second.Container.Handle));
            Assert.AreEqual(1, Object.FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None).Length);
            var state = Field<ContainerSaveSession>(Root, "session").Capture();
            Assert.IsFalse(state.containers.Any(r => r.saveableId.StartsWith("survivor.recovery.")), "Runtime caches are not authored saveable lockers.");
        }

        [UnityTest] public IEnumerator MissingPrefabConfigurationCreatesNoUsablePartialOwner()
        {
            var bad = Object.Instantiate(Prefab); bad.Configure(null, bad.GetComponentInChildren<SpriteRenderer>(true));
            var owners = Field<System.Collections.IDictionary>(Commands, "owners").Count;
            var count = Object.FindObjectsByType<SurvivorRecoveryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.Throws<InvalidOperationException>(() => SurvivorRecoveryContainer.Prepare(bad, Context, Guid.NewGuid(), Vector3.zero, 2));
            Assert.AreEqual(owners, Field<System.Collections.IDictionary>(Commands, "owners").Count);
            Assert.AreEqual(count, Object.FindObjectsByType<SurvivorRecoveryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.IsFalse(bad.CanInteract(One<PlayerInteractor>())); Object.Destroy(bad.gameObject); yield return null;
        }
    }
}
#endif
