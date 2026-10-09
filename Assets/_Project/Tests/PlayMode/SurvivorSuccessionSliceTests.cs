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
        [UnityTest] public IEnumerator EquippedDeathIsRejectedAndCancelsGestureWithoutAnOpenModal()
        {
            var feature = Feature; var first = feature.Coordinator.Current;
            var item = "c756a156ba7f34cb2b65e32264413a60";
            Assert.AreEqual(CommandStatus.Applied, Commands.Grant(Inventory.Handle, item, 1).Status);
            var held = Inventory.GetSlots().First(s => !s.IsEmpty); var gestures = Field<ContainerGestureCoordinator>(Root, "gestures");
            gestures.Begin("hotbar-test", held); Assert.IsTrue(gestures.Interaction.IsHolding);
            Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(first.Id));
            Assert.AreSame(first, feature.Coordinator.Current); Assert.AreEqual(SurvivorLifeStatus.Active, first.Status);
            Assert.IsFalse(gestures.Interaction.IsHolding); Assert.AreSame(held.Stack, Inventory.GetSlot(held.Address.Index).Stack);
            Assert.AreEqual(0, feature.AcknowledgmentCount); Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
            Assert.AreEqual(CommandStatus.Applied, Commands.Transfer(held.Address, One<SOLITUDE.Hotbar.Hotbar>().Container.GetSlot(0).Address).Status);
            Assert.AreEqual(SuccessionOutcome.Failed, feature.RequestDeath(first.Id)); Assert.AreSame(first, feature.Coordinator.Current); yield return null;
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
            var stack = Inventory.GetSlots().First(s => !s.IsEmpty);
            Assert.AreEqual(CommandStatus.Applied, Commands.Remove(stack.Address, stack.Stack.Quantity).Status);
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
        }
        [UnityTest] public IEnumerator OpenJournalClosesWithoutRebindingUnconfiguredDemo()
        {
            if (Root != null) { Object.Destroy(Root.gameObject); yield return null; }
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity", new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.IsEmpty(Object.FindObjectsByType<SurvivorSuccessionComposition>(FindObjectsSortMode.None));
            var feature = ComposeTestSuccession();
            Keys(Key.J); Keys(); yield return null; Assert.IsTrue(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen);
            Assert.AreEqual(SuccessionOutcome.Completed, feature.RequestDeath(feature.Coordinator.Current.Id));
            Assert.IsFalse(One<SOLITUDE.Tasks.UI.TaskJournalView>().IsOpen); Assert.AreEqual(1, Time.timeScale);
            Assert.IsTrue(Field<InputPolicyCoordinator>(Root, "policy").Gameplay);
        }
        private static SurvivorSuccessionComposition ComposeTestSuccession()
        {
            var scope = One<SceneBindings>(); var point = new GameObject("TestPoint").AddComponent<SurvivorAwakeningPoint>();
            point.transform.position = One<PlayerInteractor>().transform.position;
            var avatar = scope.gameObject.AddComponent<SurvivorAvatarAdapter>(); avatar.Configure(One<PlayerInteractor>());
            var feature = scope.gameObject.AddComponent<SurvivorSuccessionComposition>(); feature.Configure(point, avatar);
            var session = Field<ContainerSaveSession>(Root, "session");
            var interaction = Field<SceneInteractionScope>(scope, "interactions");
            feature.Initialize(new SceneFeatureContext(session.Commands, Inventory, null, Field<InputPolicyCoordinator>(Root, "policy"), One<UnityInputHost>(),
                null, "fixture", One<PlayerInteractor>(), interaction.Events, One<SOLITUDE.Hotbar.Hotbar>().Container,
                Field<HotbarSelectionState>(scope, "selection"), scope.CloseForTransition, interaction.Feedback));
            return feature;
        }
    }
}
#endif
