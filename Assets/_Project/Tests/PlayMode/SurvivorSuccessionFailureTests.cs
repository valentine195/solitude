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
using SOLITUDE.Core.UI;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public class SurvivorSuccessionFailureTests
    {
        private const string Battery = "c756a156ba7f34cb2b65e32264413a60";
        private Keyboard keyboard;
        private Mouse mouse;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static GameCompositionRoot Root => Object.FindFirstObjectByType<GameCompositionRoot>();
        private static SceneBindings Scope => One<SceneBindings>();
        private static SurvivorSuccessionComposition Feature => One<SurvivorSuccessionComposition>();
        private static ContainerCommandService Commands => Field<ContainerSaveSession>(Root, "session").Commands;
        private static IContainerReader Inventory => One<PlayerInventory>().Container;
        private static IContainerReader Hotbar => One<SOLITUDE.Hotbar.Hotbar>().Container;
        private static InputPolicyCoordinator Policy => Field<InputPolicyCoordinator>(Root, "policy");
        private static ContainerModalCoordinator Modal => Field<ContainerModalCoordinator>(Scope, "modal");
        private static ContainerGestureCoordinator Gestures => Field<ContainerGestureCoordinator>(Root, "gestures");
        private static int Quantity(IContainerReader reader) => reader.GetSlots().Sum(s => s.Stack?.Quantity ?? 0);
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        private void Click(bool pressed)
        {
            InputSystem.QueueStateEvent(mouse, pressed ? new MouseState().WithButton(MouseButton.Left) : new MouseState());
            InputSystem.Update();
        }
        private static void Grant(IContainerReader reader, int quantity) => Assert.AreEqual(CommandStatus.Applied, Commands.Grant(reader.Handle, Battery, quantity).Status);

        // Test-only port adapter exercises real Unity preparation/commit/reset/feedback.
        private sealed class FaultPorts : ISurvivorSuccessionPorts
        {
            private readonly SurvivorSuccessionComposition feature;
            public bool RejectCommit, RejectActivation, ThrowAfterAcknowledgment;
            public Action AfterPreparation;
            public int Committed, Activated;
            public FaultPorts(SurvivorSuccessionComposition feature) => this.feature = feature;
            public void SetTransitionGated(bool gated) => feature.SetTransitionGated(gated);
            public bool TryPrepare(SurvivorTransition transition, out string error)
            {
                if (!feature.TryPrepare(transition, out error)) return false;
                AfterPreparation?.Invoke(); return true;
            }
            public void CancelPreparation(SurvivorTransition transition) => feature.CancelPreparation(transition);
            public bool TryCommitEquipment(SurvivorTransition transition, out SurvivorEquipmentReceipt receipt, out string error)
            {
                if (RejectCommit) { receipt = null; error = "Injected rejection before handoff."; return false; }
                var result = feature.TryCommitEquipment(transition, out receipt, out error);
                if (result) Committed++; return result;
            }
            public bool TryActivate(SurvivorTransition transition, SurvivorEquipmentReceipt receipt, out string error)
            {
                if (RejectActivation) { error = "Injected interruption before activation."; return false; }
                var result = feature.TryActivate(transition, receipt, out error);
                if (result) Activated++; return result;
            }
            public bool TryAcknowledge(SurvivorTransition transition, out string error)
            {
                var result = feature.TryAcknowledge(transition, out error);
                if (result && ThrowAfterAcknowledgment) throw new InvalidOperationException("Injected uncertain acknowledgment delivery.");
                return result;
            }
        }

        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Requires isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Root.StartNewSave(); yield return null; yield return null;
            WeaponSystemTestFixtures.ClearStartingWeaponForSeededScenario();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private IEnumerator OpenRecovery(SurvivorRecoveryContainer cache)
        {
            // Separate test-only relocation from awakening's physics step.
            yield return new WaitForFixedUpdate();
            One<PlayerInteractor>().GetComponent<Rigidbody2D>().position = cache.DeathPosition;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.AreSame(cache, Field<IInteractable>(One<PlayerInteractor>(), "currentTarget"));
            Keys(Key.E); Keys(); yield return null;
            Assert.AreEqual(ContainerModalKind.Transfer, Modal.Kind); Assert.AreEqual(cache.Container.Handle, Modal.Target.Value);
        }

        [UnityTest] public IEnumerator RejectedPreparedHandoffReleasesOnlyItsEmptyOwnerAndCanTryAgain()
        {
            Grant(Inventory, 3); var a = Feature.Coordinator.Current;
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(a.Id));
            var older = Feature.RecoveryContainers.Single(); var olderReader = older.Container;
            Grant(Inventory, 4); Grant(Hotbar, 2);
            var inventoryStack = Inventory.GetSlot(0).Stack; var hotbarStack = Hotbar.GetSlot(0).Stack;
            var b = Feature.Coordinator.Current; var ports = new FaultPorts(Feature) { RejectCommit = true };
            var flow = new SurvivorSuccessionCoordinator(b, ports); ContainerHandle prepared = default;
            ports.AfterPreparation = () =>
            {
                var cache = Feature.RecoveryContainers.Single(c => c != older); prepared = cache.Container.Handle;
                Assert.IsFalse(cache.IsPublished); Assert.AreEqual(0, Quantity(cache.Container)); Assert.IsFalse(Policy.Gameplay);
            };
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(b.Id));
            Assert.IsNotEmpty(flow.LastError); Assert.AreSame(b, flow.Current); Assert.AreEqual(SurvivorLifeStatus.Active, b.Status);
            Assert.IsNull(flow.Pending); Assert.IsNull(flow.Receipt); Assert.IsNull(Commands.Read(prepared));
            Assert.AreSame(inventoryStack, Inventory.GetSlot(0).Stack); Assert.AreSame(hotbarStack, Hotbar.GetSlot(0).Stack);
            Assert.AreSame(olderReader, Commands.Read(olderReader.Handle)); Assert.AreEqual(3, Quantity(olderReader));
            Assert.AreEqual(1, Feature.RecoveryContainers.Count); Assert.AreEqual(1, Feature.AcknowledgmentCount);
            Assert.AreEqual(0, ports.Committed); Assert.AreEqual(0, ports.Activated); Assert.IsTrue(Policy.Gameplay);
            ports.RejectCommit = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(b.Id));
            Assert.AreEqual(SurvivorLifeStatus.Dead, b.Status); Assert.AreNotEqual(b.Id, flow.Current.Id);
            Assert.AreEqual(9, Feature.RecoveryContainers.Sum(c => Quantity(c.Container)));
            Assert.AreEqual(2, Feature.RecoveryContainers.Count); Assert.AreEqual(1, ports.Committed); Assert.AreEqual(1, ports.Activated);
            Assert.AreEqual(2, Feature.AcknowledgmentCount); yield return null;
        }

        [UnityTest] public IEnumerator RevisionChangeBetweenPreparationAndCommitRejectsTheWholeHandoff()
        {
            Grant(Inventory, 4); Grant(Hotbar, 2); var first = Feature.Coordinator.Current;
            var ports = new FaultPorts(Feature) { AfterPreparation = () => Grant(Hotbar, 1) };
            var flow = new SurvivorSuccessionCoordinator(first, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(first.Id));
            StringAssert.Contains("Stale", flow.LastError);
            Assert.AreSame(first, flow.Current); Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            Assert.AreEqual(4, Quantity(Inventory)); Assert.AreEqual(3, Quantity(Hotbar));
            Assert.IsEmpty(Feature.RecoveryContainers); Assert.AreEqual(0, Feature.AcknowledgmentCount);
            Assert.IsTrue(Policy.Gameplay); Assert.IsNull(flow.Pending); Assert.AreEqual(0, ports.Committed);
            ports.AfterPreparation = null;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.RequestDeath(first.Id));
            Assert.AreEqual(7, Quantity(Feature.RecoveryContainers.Single().Container)); yield return null;
        }

        [UnityTest] public IEnumerator TransferDragAndHeldPointerCannotActAfterCommittedFailureOrRetry()
        {
            Grant(Inventory, 3); Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(Feature.Coordinator.Current.Id));
            var older = Feature.RecoveryContainers.Single(); Grant(Inventory, 4); Grant(Hotbar, 2);
            yield return OpenRecovery(older);
            var view = One<ContainerTransferModalView>();
            var sourceView = view.TargetController.GetComponent<ContainerView>();
            var targetView = view.InventoryController.GetComponent<ContainerView>();
            var sourceSlot = Field<List<ContainerSlotView>>(sourceView, "slotViews")[0];
            var targetSlot = Field<List<ContainerSlotView>>(targetView, "slotViews")[0];
            var oldBegin = Field<Action<SlotIntent>>(sourceView, "BeginDrag");
            var oldDrop = Field<Action<SlotIntent>>(targetView, "Drop");
            var sourceIntent = new SlotIntent(0, Field<long>(sourceSlot, "generation"));
            var targetIntent = new SlotIntent(0, Field<long>(targetSlot, "generation"));
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(sourceSlot.gameObject, pointer, ExecuteEvents.beginDragHandler);
            Assert.IsTrue(Gestures.Interaction.IsHolding); Click(true);
            var b = Feature.Coordinator.Current; var ports = new FaultPorts(Feature) { RejectActivation = true };
            var flow = new SurvivorSuccessionCoordinator(b, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(b.Id));
            var pending = flow.Pending; var receipt = flow.Receipt; var newer = Feature.RecoveryContainers.Single(c => c != older);
            Assert.IsNull(flow.Current); Assert.AreEqual(SurvivorLifeStatus.Dead, b.Status);
            Assert.AreEqual(SurvivorLifeStatus.Pending, pending.Successor.Status); Assert.IsFalse(Policy.Gameplay);
            Assert.IsFalse(One<InputSystemUIInputModule>().enabled); Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind);
            Assert.IsFalse(Gestures.Interaction.IsHolding); Assert.IsFalse(newer.IsPublished);
            Assert.AreEqual(0, Quantity(Inventory)); Assert.AreEqual(0, Quantity(Hotbar));
            oldBegin(sourceIntent); oldDrop(targetIntent);
            Assert.AreEqual(3, Quantity(older.Container)); Assert.AreEqual(6, Quantity(newer.Container));
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(pending.Successor.Id));
            Assert.AreEqual(SuccessionOutcome.Failed, flow.Retry()); Assert.AreSame(receipt, flow.Receipt);
            Assert.IsTrue(Field<ContainerSaveSession>(Root, "session").StartingLoadoutInitialized);
            Assert.AreSame(pending, flow.Pending); Assert.AreEqual(1, ports.Committed);
            ports.RejectActivation = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.Retry()); Assert.AreSame(pending.Successor, flow.Current);
            Assert.IsTrue(Field<ContainerSaveSession>(Root, "session").StartingLoadoutInitialized); Assert.AreEqual(0, Quantity(Hotbar), "Successor retry must not refresh starting equipment.");
            Grant(Inventory, 1); oldBegin(sourceIntent); oldDrop(targetIntent);
            Assert.AreEqual(1, Quantity(Inventory)); Assert.AreEqual(3, Quantity(older.Container)); Assert.AreEqual(6, Quantity(newer.Container));
            Assert.IsFalse(Gestures.Interaction.IsHolding); Assert.IsFalse(One<InputSystemUIInputModule>().enabled);
            Click(false); yield return null; Assert.IsTrue(One<InputSystemUIInputModule>().enabled);
            Assert.AreEqual(1, ports.Committed); Assert.AreEqual(1, ports.Activated); Assert.AreEqual(2, Feature.AcknowledgmentCount);
        }

        [UnityTest] public IEnumerator UncertainAcknowledgmentRetriesWithoutReactivationOrAnotherMessage()
        {
            Grant(Inventory, 4); Grant(Hotbar, 2); var first = Feature.Coordinator.Current;
            var ports = new FaultPorts(Feature) { ThrowAfterAcknowledgment = true };
            var flow = new SurvivorSuccessionCoordinator(first, ports);
            Assert.AreEqual(SuccessionOutcome.Failed, flow.RequestDeath(first.Id));
            Assert.AreEqual(SuccessionStage.AvatarActivated, flow.Stage); Assert.IsFalse(Policy.Gameplay);
            var successor = flow.Current; var cache = Feature.RecoveryContainers.Single(); var handle = cache.Container.Handle;
            Assert.AreEqual(SurvivorLifeStatus.Dead, first.Status); Assert.IsTrue(cache.IsPublished);
            Assert.AreEqual(6, Quantity(cache.Container)); Assert.AreEqual(1, Feature.AcknowledgmentCount);
            var label = Field<TMPro.TextMeshProUGUI>(One<InteractionFeedbackView>(), "label");
            StringAssert.Contains("New colonist awakened", label.text); Assert.IsTrue(label.gameObject.activeInHierarchy);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(successor.Id));
            ports.ThrowAfterAcknowledgment = false;
            Assert.AreEqual(SuccessionOutcome.Completed, flow.Retry()); Assert.AreSame(successor, flow.Current);
            Assert.AreEqual(1, Feature.AcknowledgmentCount); Assert.AreEqual(1, ports.Committed); Assert.AreEqual(1, ports.Activated);
            Assert.AreEqual(handle, cache.Container.Handle); Assert.AreEqual(6, Quantity(cache.Container)); Assert.IsTrue(Policy.Gameplay);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.RequestDeath(first.Id));
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, flow.Retry()); yield return null;
        }

        [UnityTest] public IEnumerator PartialCachesEmptyDeathAndRejectedAttemptConserveEveryItem()
        {
            const string oxygen = "8d158110df66c4994b8d575233f619c5";
            Grant(Inventory, 5); Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Hotbar.Handle, oxygen, 3).Status);
            var a = Feature.Coordinator.Current; Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(a.Id));
            var aCache = Feature.RecoveryContainers.Single(); Grant(Inventory, 98);
            Assert.AreEqual(CommandStatus.PartialMerged, Commands.Transfer(aCache.Container.GetSlot(0).Address, Inventory.GetSlot(0).Address).Status);
            Assert.AreEqual(4, aCache.Container.GetSlot(0).Stack.Quantity);
            var b = Feature.Coordinator.Current; Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(b.Id));
            var bCache = Feature.RecoveryContainers.Single(c => c.SourceSurvivorId == b.Id);
            var c = Feature.Coordinator.Current; Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(c.Id));
            Assert.AreEqual(2, Feature.RecoveryContainers.Count, "An empty death creates no cache.");
            var d = Feature.Coordinator.Current; Grant(Inventory, 2); var prefab = Feature.RecoveryPrefab;
            Feature.ConfigureRecovery(null); Assert.AreEqual(SuccessionOutcome.Failed, Feature.RequestDeath(d.Id));
            Assert.AreSame(d, Feature.Coordinator.Current); Assert.AreEqual(SurvivorLifeStatus.Active, d.Status);
            Assert.AreEqual(2, Quantity(Inventory)); Assert.AreEqual(99, Quantity(bCache.Container));
            Assert.AreEqual(3, Feature.AcknowledgmentCount); Assert.AreEqual(2, Feature.RecoveryContainers.Count);
            Feature.ConfigureRecovery(prefab);
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(d.Id));
            var caches = Feature.RecoveryContainers; Assert.AreEqual(3, caches.Count);
            Assert.AreEqual(3, caches.Select(x => x.Container.Handle).Distinct().Count());
            Assert.AreEqual(105, caches.Sum(x => x.Container.GetSlots().Where(y => y.Stack?.ItemId == Battery).Sum(y => y.Stack.Quantity)));
            Assert.AreEqual(3, caches.Sum(x => x.Container.GetSlots().Where(y => y.Stack?.ItemId == oxygen).Sum(y => y.Stack.Quantity)));
            var identities = new[] { a, b, c, d, Feature.Coordinator.Current };
            Assert.AreEqual(5, identities.Select(x => x.Id).Distinct().Count());
            foreach (var dead in identities.Take(4))
            { Assert.AreEqual(SurvivorLifeStatus.Dead, dead.Status); Assert.AreEqual(SuccessionOutcome.StaleSurvivor, Feature.RequestDeath(dead.Id)); }
            Assert.AreEqual(4, Feature.AcknowledgmentCount); Assert.AreEqual(0, Quantity(Inventory)); Assert.AreEqual(0, Quantity(Hotbar));
            yield return OpenRecovery(aCache); Assert.IsTrue(aCache.CanInteract(One<PlayerInteractor>())); One<ContainerTransferModalView>().Close();
        }

        [UnityTest] public IEnumerator ReleasedCacheDropsFocusSynchronouslyWithoutReleasingOtherOwners()
        {
            Grant(Inventory, 3); Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(Feature.Coordinator.Current.Id));
            var cache = Feature.RecoveryContainers.Single(); yield return OpenRecovery(cache);
            var player = One<PlayerInteractor>(); var nearby = Field<HashSet<IInteractable>>(player, "nearby");
            Assert.IsTrue(nearby.Contains(cache)); var owner = cache.Container.Handle;
            cache.gameObject.SetActive(false); yield return null;
            Assert.IsNotNull(Commands.Read(owner)); Assert.AreEqual(3, Quantity(Commands.Read(owner)));
            cache.gameObject.SetActive(true); yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsTrue(nearby.Contains(cache)); cache.Release();
            Assert.IsFalse(nearby.Contains(cache), "Release must unregister before deferred Unity destruction.");
            Assert.IsNull(Commands.Read(owner)); Assert.IsFalse(cache.gameObject.activeSelf);
            Assert.AreSame(Inventory, Commands.Read(Inventory.Handle)); Assert.AreSame(Hotbar, Commands.Read(Hotbar.Handle));
            yield return null; Assert.AreNotSame(cache, Field<IInteractable>(player, "currentTarget"));
        }

        [UnityTest] public IEnumerator DeathClosesJournalAndInventoryAndSuppressesHeldMovementUntilRelease()
        {
            Object.Destroy(Root.gameObject); yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            WeaponSystemTestFixtures.ClearStartingWeaponForSeededScenario();
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State); ComposeDemoSuccession();
            Grant(Inventory, 4); Grant(Hotbar, 2);
            Keys(Key.D, Key.J); yield return null;
            Assert.IsTrue(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen);
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(Feature.Coordinator.Current.Id));
            Assert.IsFalse(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen);
            var body = One<PlayerInteractor>().GetComponent<Rigidbody2D>(); var position = body.position;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Less(Vector2.Distance(position, body.position), .01f); Assert.AreEqual(6, Quantity(Feature.RecoveryContainers.Single().Container));
            Keys(); yield return null; Modal.ToggleInventory(); Assert.AreEqual(ContainerModalKind.Inventory, Modal.Kind);
            Assert.AreEqual(SuccessionOutcome.Completed, Feature.RequestDeath(Feature.Coordinator.Current.Id));
            Assert.AreEqual(ContainerModalKind.Closed, Modal.Kind); Assert.AreEqual(1, Time.timeScale);
            Assert.AreEqual(1, Feature.RecoveryContainers.Count); Assert.AreEqual(2, Feature.AcknowledgmentCount);
            yield return null;
            Keys(Key.D); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Greater(body.position.x, position.x); Keys();
        }
        private static void ComposeDemoSuccession()
        {
            if (Scope.GetComponent<SurvivorSuccessionComposition>() != null) return;
            var point = new GameObject("TestPoint").AddComponent<SurvivorAwakeningPoint>(); point.transform.position = One<PlayerInteractor>().transform.position;
            var avatar = Scope.gameObject.AddComponent<SurvivorAvatarAdapter>(); avatar.Configure(One<PlayerInteractor>());
            var feature = Scope.gameObject.AddComponent<SurvivorSuccessionComposition>(); feature.Configure(point, avatar);
            feature.ConfigureRecovery(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab").GetComponent<SurvivorRecoveryContainer>());
            var interactions = Field<SceneInteractionScope>(Scope, "interactions");
            feature.Initialize(new SceneFeatureContext(Commands, Inventory, null, Policy, One<UnityInputHost>(), null, "failure-tests",
                One<PlayerInteractor>(), interactions.Events, Hotbar, Field<HotbarSelectionState>(Scope, "selection"),
                Scope.CloseForTransition, interactions.Feedback, Scope.Open, Scope.Close));
        }
    }
}
#endif
