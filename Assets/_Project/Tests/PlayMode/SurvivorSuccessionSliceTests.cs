#if UNITY_EDITOR
using System;
using System.Collections;
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
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Player.Succession;
using SOLITUDE.SaveLoad;
using SOLITUDE.Survivors;
using SOLITUDE.World.OpeningSlice;
using SOLITUDE.World.SolitudeStart;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace SOLITUDE.Tests
{
    public class SurvivorSuccessionSliceTests
    {
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static GameCompositionRoot Root => Object.FindFirstObjectByType<GameCompositionRoot>();
        private static SurvivorSuccessionComposition Feature => One<SurvivorSuccessionComposition>();
        private static ContainerCommandService Commands => Field<ContainerSaveSession>(Root, "session").Commands;
        private static IContainerReader Inventory => One<PlayerInventory>().Container;
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); }
        [UnitySetUp] public IEnumerator Prepare()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-solitudeSaveDirectory"), "Requires isolated verification runner.");
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Wakeup.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            Root.StartNewSave(); yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            if (Root != null) Object.Destroy(Root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        [UnityTest] public IEnumerator EmptyDeathResetsRetainedAvatarVitalsSelectionReadersAndFeedback()
        {
            var feature = Feature; var player = One<PlayerInteractor>(); var first = feature.Coordinator.Current;
            var inventory = Inventory; var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            var vitals = One<PlayerVitalsController>().Vitals; var health = vitals.Health;
            var camera = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(c => c.GetType().Name == "CinemachineCamera");
            var cameraTarget = new UnityEditor.SerializedObject(camera).FindProperty("Target.TrackingTarget").objectReferenceValue;
            Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            Assert.LessOrEqual(Vector3.Distance(feature.AwakeningPoint.transform.position, player.transform.position), .000001f,
                "Interpolated rendering must remain at the authored awakening location.");
            vitals.Health.Set(12); vitals.Oxygen.Set(9);
            var selection = Field<HotbarSelectionState>(One<SceneBindings>(), "selection"); selection.Select(2);
            player.GetComponent<Rigidbody2D>().position += Vector2.right * 3;
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(first.Id));
            Assert.AreEqual(feature.AwakeningPoint.transform.position, player.transform.position,
                "Synchronous awakening must place the avatar exactly.");
            yield return null;
            Assert.AreEqual(SurvivorLifeStatus.Dead, first.Status); Assert.AreNotEqual(first.Id, feature.Coordinator.Current.Id);
            Assert.AreEqual((Vector2)feature.AwakeningPoint.transform.position, player.GetComponent<Rigidbody2D>().position,
                "The physics body must remain exactly at the awakening point.");
            Assert.LessOrEqual(Vector3.Distance(feature.AwakeningPoint.transform.position, player.transform.position), .000001f,
                "Rigidbody interpolation may round the rendered Transform by one float unit.");
            Assert.AreSame(inventory, Inventory); Assert.AreSame(hotbar, One<SOLITUDE.Hotbar.Hotbar>().Container);
            Assert.IsTrue(inventory.GetSlots().All(s => s.IsEmpty)); Assert.IsTrue(hotbar.GetSlots().All(s => s.IsEmpty));
            Assert.AreSame(health, One<PlayerVitalsController>().Vitals.Health);
            Assert.AreEqual(100, vitals.Health.Current); Assert.AreEqual(100, vitals.Oxygen.Current);
            Assert.AreEqual(-1, selection.ActiveIndex); Assert.AreSame(cameraTarget, new UnityEditor.SerializedObject(camera).FindProperty("Target.TrackingTarget").objectReferenceValue);
            Assert.AreEqual(1, feature.AcknowledgmentCount);
            var label = Field<TMPro.TextMeshProUGUI>(One<InteractionFeedbackView>(), "label");
            Assert.IsTrue(label.gameObject.activeInHierarchy); StringAssert.Contains("New colonist awakened", label.text);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, feature.RequestDeath(first.Id)); Assert.AreEqual(1, feature.AcknowledgmentCount);
            var second = feature.Coordinator.Current;
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(second.Id));
            Assert.AreEqual(SurvivorLifeStatus.Dead, second.Status); Assert.AreNotEqual(second.Id, feature.Coordinator.Current.Id);
            Assert.AreNotEqual(first.Id, feature.Coordinator.Current.Id); Assert.AreEqual(2, feature.AcknowledgmentCount);
        }
        [UnityTest] public IEnumerator HeldMovementAndInventoryAreNeutralUntilReleaseThenNewInputWorks()
        {
            var feature = Feature; var body = One<PlayerInteractor>().GetComponent<Rigidbody2D>();
            Keys(Key.D); yield return new WaitForFixedUpdate();
            var modal = Field<ContainerModalCoordinator>(One<SceneBindings>(), "modal"); modal.ToggleInventory();
            Assert.AreEqual(0, Time.timeScale);
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(feature.Coordinator.Current.Id));
            var position = body.position; Assert.AreEqual(ContainerModalKind.Closed, modal.Kind); Assert.AreEqual(1, Time.timeScale);
            yield return null; yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Less(Vector2.Distance(position, body.position), .01f, "Held old movement cannot move the successor.");
            Keys(); yield return null;
            Keys(Key.D); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Greater(body.position.x, position.x, "Fresh movement works with the retained avatar."); Keys();
        }
        [UnityTest] public IEnumerator EquippedDeathCommitsBothSourcesAndCancelsGestureWithoutAnOpenModal()
        {
            var feature = Feature; var first = feature.Coordinator.Current;
            var item = "c756a156ba7f34cb2b65e32264413a60";
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, item, 4).Status);
            var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(hotbar.Handle, item, 2).Status);
            var held = Inventory.GetSlots().First(s => !s.IsEmpty); var gestures = Field<ContainerGestureCoordinator>(Root, "gestures");
            gestures.Begin("hotbar-test", held); Assert.IsTrue(gestures.Interaction.IsHolding);
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(first.Id));
            Assert.AreNotEqual(first.Id, feature.Coordinator.Current.Id); Assert.AreEqual(SurvivorLifeStatus.Dead, first.Status);
            Assert.IsFalse(gestures.Interaction.IsHolding);
            Assert.IsTrue(Inventory.GetSlots().All(s => s.IsEmpty)); Assert.IsTrue(hotbar.GetSlots().All(s => s.IsEmpty));
            var cache = feature.RecoveryContainers.Single();
            Assert.AreSame(held.Stack, cache.Container.GetSlot(0).Stack);
            Assert.AreEqual(6, cache.Container.GetSlots().Sum(s => s.Stack?.Quantity ?? 0));
            Assert.AreEqual(first.Id, cache.SourceSurvivorId); Assert.IsTrue(cache.IsPublished);
            Assert.AreEqual(1, feature.AcknowledgmentCount); Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            Assert.AreEqual(SuccessionOutcome.StaleSurvivor, feature.RequestDeath(first.Id));
            Assert.AreEqual(1, feature.RecoveryContainers.Count); yield return null;
        }
        [UnityTest] public IEnumerator RepeatedEquippedDeathsKeepPartialCachesAndRecoverThroughInteraction()
        {
            var feature = Feature; var first = feature.Coordinator.Current;
            var item = "c756a156ba7f34cb2b65e32264413a60";
            var player = One<PlayerInteractor>(); var body = player.GetComponent<Rigidbody2D>();
            var inventory = Inventory; var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            Commands.Grant(inventory.Handle, item, 5); Commands.Grant(hotbar.Handle, item, 2);
            body.position += Vector2.right * 2; var firstPosition = body.position;
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(first.Id));
            var a = feature.RecoveryContainers.Single();
            Assert.AreEqual(firstPosition, (Vector2)a.DeathPosition);
            Assert.AreEqual(7, a.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Commands.Grant(inventory.Handle, item, 98);
            yield return OpenRecovery(a);
            var gestures = Field<ContainerGestureCoordinator>(Root, "gestures");
            gestures.Begin("recovery", a.Container.GetSlot(0)); gestures.Drop(inventory.GetSlot(0));
            Assert.AreEqual(4, a.Container.GetSlot(0).Stack.Quantity);
            One<ContainerTransferModalView>().Close();
            var second = feature.Coordinator.Current;
            body.position += Vector2.up * 2; var secondPosition = body.position;
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(second.Id));
            var b = feature.RecoveryContainers.Single(x => x.SourceSurvivorId == second.Id);
            Assert.AreEqual(secondPosition, (Vector2)b.DeathPosition);
            Assert.AreEqual(2, feature.RecoveryContainers.Count);
            Assert.AreNotEqual(a.Container.Handle, b.Container.Handle);
            Assert.AreEqual(6, a.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Assert.AreEqual(99, b.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Assert.AreSame(inventory, Inventory); Assert.AreSame(hotbar, One<SOLITUDE.Hotbar.Hotbar>().Container);
            // Let physics observe awakening before the fixture teleports back to A.
            // Otherwise an uninterrupted old overlap cannot generate a new enter event.
            yield return new WaitForFixedUpdate();
            yield return OpenRecovery(a);
            gestures.Begin("recovery", a.Container.GetSlot(0)); gestures.Drop(inventory.GetSlot(0));
            gestures.Begin("recovery", a.Container.GetSlot(inventory.Capacity)); gestures.Drop(inventory.GetSlot(0));
            Assert.IsTrue(a.Container.GetSlots().All(x => x.IsEmpty));
            Assert.AreEqual(6, inventory.GetSlot(0).Stack.Quantity);
            Assert.AreEqual(99, b.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Assert.AreEqual(2, feature.AcknowledgmentCount); One<ContainerTransferModalView>().Close();
        }
        private IEnumerator OpenRecovery(SurvivorRecoveryContainer cache)
        {
            var player = One<PlayerInteractor>(); player.GetComponent<Rigidbody2D>().position = cache.DeathPosition;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            var focused = Field<IInteractable>(player, "currentTarget");
            Assert.AreSame(cache, focused, $"Requested source {cache.SourceSurvivorId} at {cache.DeathPosition}; " +
                $"body {player.GetComponent<Rigidbody2D>().position}, transform {player.transform.position}; " +
                $"focused {(focused as SurvivorRecoveryContainer)?.SourceSurvivorId} at {(focused as Component)?.transform.position}");
            Keys(Key.E); Keys(); yield return null;
            var modal = Field<ContainerModalCoordinator>(One<SceneBindings>(), "modal");
            Assert.AreEqual(ContainerModalKind.Transfer, modal.Kind); Assert.AreEqual(cache.Container.Handle, modal.Target.Value);
        }
        [UnityTest] public IEnumerator MissingRecoveryRejectsWithoutLosingCarriedEquipment()
        {
            var feature = Feature; var first = feature.Coordinator.Current; var prefab = feature.RecoveryPrefab;
            Commands.Grant(Inventory.Handle, "c756a156ba7f34cb2b65e32264413a60", 5);
            var stack = Inventory.GetSlot(0).Stack; feature.ConfigureRecovery(null);
            Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(first.Id));
            Assert.AreSame(first, feature.Coordinator.Current); Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            Assert.AreSame(stack, Inventory.GetSlot(0).Stack); Assert.IsEmpty(feature.RecoveryContainers);
            Assert.AreEqual(0, feature.AcknowledgmentCount); Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            feature.ConfigureRecovery(prefab); yield return null;
        }
        [UnityTest] public IEnumerator CommittedEquipmentSurvivesActivationFailureAndRetryDoesNotTransferAgain()
        {
            var feature = Feature; var first = feature.Coordinator.Current; var point = feature.AwakeningPoint;
            var inventory = Inventory; var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            Commands.Grant(inventory.Handle, "c756a156ba7f34cb2b65e32264413a60", 4);
            Commands.Grant(hotbar.Handle, "c756a156ba7f34cb2b65e32264413a60", 2);
            Action<int> fault = _ => feature.Configure(null, One<SurvivorAvatarAdapter>());
            inventory.SlotChanged += fault;
            try { Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(first.Id)); }
            finally { inventory.SlotChanged -= fault; }
            Assert.AreEqual(SurvivorLifeStatus.Dead, first.Status); Assert.IsNull(feature.Coordinator.Current);
            Assert.AreEqual(SuccessionStage.EquipmentCommitted, feature.Coordinator.Stage);
            var pending = feature.Coordinator.Pending; var receipt = feature.Coordinator.Receipt;
            var cache = feature.RecoveryContainers.Single(); var domainReceipt = Commands.FindHandoffReceipt(pending.Id);
            Assert.IsNotNull(domainReceipt); Assert.IsFalse(cache.IsPublished);
            Assert.AreEqual(6, cache.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Assert.IsTrue(inventory.GetSlots().All(x => x.IsEmpty)); Assert.IsTrue(hotbar.GetSlots().All(x => x.IsEmpty));
            Assert.IsFalse(Field<InputPolicyCoordinator>(Root, "policy").Gameplay); Assert.AreEqual(0, feature.AcknowledgmentCount);
            Assert.AreEqual(SuccessionOutcome.Failed, feature.Coordinator.Retry()); Assert.AreSame(receipt, feature.Coordinator.Receipt);
            feature.Configure(point, One<SurvivorAvatarAdapter>());
            Assert.AreEqual(SuccessionOutcome.Completed, feature.Coordinator.Retry());
            Assert.AreSame(pending.Successor, feature.Coordinator.Current); Assert.IsTrue(cache.IsPublished);
            Assert.AreSame(domainReceipt, Commands.FindHandoffReceipt(pending.Id));
            Assert.AreEqual(6, cache.Container.GetSlots().Sum(x => x.Stack?.Quantity ?? 0));
            Assert.AreEqual(1, feature.RecoveryContainers.Count); Assert.AreEqual(1, feature.AcknowledgmentCount);
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            yield return OpenRecovery(cache); One<ContainerTransferModalView>().Close();
        }
        [UnityTest] public IEnumerator InvalidPointRejectsDeathWithoutActivatingSuccessor()
        {
            var feature = Feature; var first = feature.Coordinator.Current; var point = feature.AwakeningPoint;
            feature.Configure(null, One<SurvivorAvatarAdapter>());
            Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(first.Id));
            Assert.AreSame(first, feature.Coordinator.Current); Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            Assert.AreEqual(0, feature.AcknowledgmentCount); Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            feature.Configure(point, One<SurvivorAvatarAdapter>()); yield return null;
        }
        [UnityTest] public IEnumerator PoweredDoorFirstContactAndPickupRemainLiveAcrossDeath()
        {
            Object.Destroy(Root.gameObject); yield return null;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Art/SOLITUDE/ReferenceScenes/SOLITUDE_OpeningSlice.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            // OpeningReviewSession already starts this reference scene with a fresh in-memory store.
            Assert.AreEqual(RuntimeStartupState.Ready, Root.State);
            var feature = ComposeTestSuccession(); var player = One<PlayerInteractor>(); var socket = One<OpeningPowerSocket>();
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, socket.batteryItemId, 1).Status);
            Assert.IsTrue(socket.Interact(player).IsSuccess); yield return new WaitForSeconds(1);
            Assert.IsTrue(socket.Powered); Assert.IsTrue(socket.door.IsOpen);
            var pickup = Object.FindObjectsByType<WorldPickup>(FindObjectsSortMode.None).First(x => x.CanInteract(player));
            var pickupId = pickup.PickupId; Assert.IsTrue(pickup.Interact(player).IsSuccess);
            Assert.IsFalse(pickup.gameObject.activeSelf);
            var hotbar = One<SOLITUDE.Hotbar.Hotbar>().Container;
            Assert.AreEqual(CommandStatus.Applied, Commands.Transfer(Inventory.GetSlots().First(x => !x.IsEmpty).Address, hotbar.GetSlot(0).Address).Status);
            Commands.Grant(Inventory.Handle, socket.batteryItemId, 2);
            var expected = Inventory.GetSlots().Concat(hotbar.GetSlots()).Where(x => !x.IsEmpty)
                .GroupBy(x => x.Stack.ItemId).ToDictionary(x => x.Key, x => x.Sum(y => y.Stack.Quantity));
            var contact = One<OpeningFirstContact>();
            player.GetComponent<Rigidbody2D>().position = contact.transform.position; Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.IsTrue(contact.Heard); yield return new WaitForSeconds(10);
            var scope = One<SceneBindings>(); var interaction = Field<SceneInteractionScope>(scope, "interactions");
            var door = socket.door; var root = Root;
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(feature.Coordinator.Current.Id)); yield return null;
            Assert.AreSame(root, Root); Assert.AreSame(interaction, Field<SceneInteractionScope>(scope, "interactions"));
            Assert.IsTrue(socket.Powered); Assert.IsTrue(door.IsOpen); Assert.IsTrue(contact.Heard);
            Assert.AreEqual(1, feature.AcknowledgmentCount); Assert.IsNull(Field<IInteractable>(player, "currentTarget"));
            Assert.AreEqual(1, Object.FindObjectsByType<PlayerInteractor>(FindObjectsSortMode.None).Length);
            Assert.IsFalse(pickup.gameObject.activeSelf); Assert.AreEqual(pickupId, pickup.PickupId);
            var cache = feature.RecoveryContainers.Single(); yield return OpenRecovery(cache);
            var gestures = Field<ContainerGestureCoordinator>(Root, "gestures"); var destination = 0;
            for (var i = 0; i < cache.Container.Capacity; i++)
                if (!cache.Container.GetSlot(i).IsEmpty)
                { gestures.Begin("recovery", cache.Container.GetSlot(i)); gestures.Drop(Inventory.GetSlot(destination++)); }
            Assert.IsTrue(cache.Container.GetSlots().All(x => x.IsEmpty));
            foreach (var item in expected) Assert.AreEqual(item.Value, Inventory.GetSlots().Where(x => x.Stack?.ItemId == item.Key).Sum(x => x.Stack.Quantity));
            One<ContainerTransferModalView>().Close();
            Assert.IsTrue(socket.Powered); Assert.IsTrue(door.IsOpen);
        }
        [UnityTest] public IEnumerator OpenJournalClosesWithoutRebindingAuthoredDemo()
        {
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            var feature = Feature; feature.Validate();
            var equipment = One<SOLITUDE.World.ScrubberEncounter.ScrubberEquipment>(); var state = equipment.State;
            var fuse = SOLITUDE.World.ScrubberEncounter.ScrubberEquipment.FuseItemId;
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, fuse, 3).Status);
            equipment.Install(); equipment.Restart(); yield return new WaitForSeconds(.1f);
            Assert.IsTrue(state.FuseInstalled); Assert.IsTrue(state.Running); var progress = state.RecoverySeconds;
            Assert.AreEqual(CommandStatus.Applied, Commands.Transfer(Inventory.GetSlot(0).Address,
                One<SOLITUDE.Hotbar.Hotbar>().Container.GetSlot(0).Address).Status);
            Keys(Key.J); Keys(); yield return null; Assert.IsTrue(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen);
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(feature.Coordinator.Current.Id));
            Assert.IsFalse(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen); Assert.AreEqual(1, Time.timeScale);
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            Assert.AreSame(state, equipment.State); Assert.IsTrue(state.FuseInstalled); Assert.IsTrue(state.Running);
            Assert.AreEqual(2, feature.RecoveryContainers.Single().Container.GetSlots().Sum(s => s.Stack?.Quantity ?? 0));
            yield return new WaitForSeconds(.1f); Assert.Greater(state.RecoverySeconds, progress);
            Assert.IsTrue(One<SOLITUDE.World.ScrubberEncounter.ScrubberScreenView>().Open());
            One<SOLITUDE.World.ScrubberEncounter.ScrubberScreenView>().Close();
        }
        private static SurvivorSuccessionComposition ComposeTestSuccession()
        {
            var scope = One<SceneBindings>();
            var authored = scope.GetComponent<SurvivorSuccessionComposition>(); if (authored != null) return authored;
            var point = new GameObject("TestPoint").AddComponent<SurvivorAwakeningPoint>();
            point.transform.position = One<PlayerInteractor>().transform.position;
            var avatar = scope.gameObject.AddComponent<SurvivorAvatarAdapter>(); avatar.Configure(One<PlayerInteractor>());
            var feature = scope.gameObject.AddComponent<SurvivorSuccessionComposition>(); feature.Configure(point, avatar);
            feature.ConfigureRecovery(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Features/Player/Succession/SurvivorRecoveryContainer.prefab").GetComponent<SurvivorRecoveryContainer>());
            var session = Field<ContainerSaveSession>(Root, "session");
            var interaction = Field<SceneInteractionScope>(scope, "interactions");
            feature.Initialize(new SceneFeatureContext(session.Commands, Inventory, null, Field<InputPolicyCoordinator>(Root, "policy"), One<UnityInputHost>(),
                null, "fixture", One<PlayerInteractor>(), interaction.Events, One<SOLITUDE.Hotbar.Hotbar>().Container,
                Field<HotbarSelectionState>(scope, "selection"), scope.CloseForTransition, interaction.Feedback, scope.Open, scope.Close));
            return feature;
        }
    }
}
#endif
