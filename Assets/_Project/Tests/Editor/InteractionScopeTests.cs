using System;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using SOLITUDE.Player;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class FailingFeedbackReceiver : MonoBehaviour, IInteractionFeedbackReceiver
    {
        public InteractionFeedbackService LastService;
        public int Releases;
        public void BindFeedback(InteractionFeedbackService feedback)
        { LastService = feedback; throw new InvalidOperationException("Injected feedback bind failure"); }
        public void ReleaseFeedback() { Releases++; }
    }
    public class InteractionScopeTests
    {
        private Scene scene;
        private GameObject host, uiHost, promptRoot;
        private PlayerInteractor player;
        private UIController ui;
        private SceneInteractionScope scope;
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        private sealed class Target : IInteractable
        {
            public int Reads;
            public string GetPrompt() { Reads++; return "Scoped prompt"; }
            public bool CanInteract(PlayerInteractor actor) => true;
            public InteractionResult Interact(PlayerInteractor actor) => InteractionResult.Success();
        }
        [SetUp] public void Prepare()
        {
            scene = EditorSceneManager.NewPreviewScene(); host = new GameObject("Interaction scope fixture");
            SceneManager.MoveGameObjectToScene(host, scene);
            var actor = new GameObject("Player"); actor.transform.SetParent(host.transform); player = actor.AddComponent<PlayerInteractor>();
            uiHost = new GameObject("UI"); uiHost.transform.SetParent(host.transform);
            promptRoot = new GameObject("Prompt", typeof(RectTransform)); promptRoot.transform.SetParent(uiHost.transform);
            var prompt = uiHost.AddComponent<InteractionPromptView>();
            Set(prompt, "root", promptRoot); Set(prompt, "promptText", promptRoot.AddComponent<TextMeshProUGUI>());
            var feedbackRoot = new GameObject("Feedback", typeof(RectTransform)); feedbackRoot.transform.SetParent(uiHost.transform);
            var feedback = uiHost.AddComponent<InteractionFeedbackView>();
            Set(feedback, "root", feedbackRoot); Set(feedback, "label", feedbackRoot.AddComponent<TextMeshProUGUI>());
            feedbackRoot.SetActive(false);
            ui = uiHost.AddComponent<UIController>(); Set(ui, "interactionPrompt", prompt); Set(ui, "feedbackView", feedback);
            scope = new SceneInteractionScope();
        }
        [TearDown] public void Cleanup()
        { scope.Dispose(); Object.DestroyImmediate(host); EditorSceneManager.ClosePreviewScene(scene); }
        [Test] public void IndependentEventScopesNeverShareFocusOrTriggerDispatch()
        {
            using var first = new SceneInteractionEvents(); using var second = new SceneInteractionEvents();
            var target = new Target(); int firstFocus = 0, secondFocus = 0, firstTrigger = 0, secondTrigger = 0;
            first.FocusChanged += _ => firstFocus++; second.FocusChanged += _ => secondFocus++;
            first.Triggered += _ => firstTrigger++; second.Triggered += _ => secondTrigger++;
            first.Publish(new InteractionFocusChangedEvent(target)); first.Publish(new InteractionTriggeredEvent(target, player));
            Assert.AreEqual(1, firstFocus); Assert.AreEqual(1, firstTrigger); Assert.AreEqual(0, secondFocus); Assert.AreEqual(0, secondTrigger);
            Assert.AreSame(target, first.CurrentFocus); Assert.IsNull(second.CurrentFocus);
            first.Dispose(); first.Publish(new InteractionFocusChangedEvent(target)); first.Publish(new InteractionTriggeredEvent(target, player));
            Assert.AreEqual(1, firstFocus); Assert.AreEqual(1, firstTrigger); Assert.IsNull(first.CurrentFocus);
            second.Publish(new InteractionFocusChangedEvent(target)); Assert.AreEqual(1, secondFocus);
        }
        [Test] public void TwoSceneScopesBindIndependentPlayersAndPromptViews()
        {
            var otherScene = EditorSceneManager.NewPreviewScene(); var otherHost = Object.Instantiate(host);
            SceneManager.MoveGameObjectToScene(otherHost, otherScene);
            using var other = new SceneInteractionScope();
            try
            {
                scope.Initialize(scene, player);
                other.Initialize(otherScene, otherHost.GetComponentInChildren<PlayerInteractor>());
                var otherPrompt = otherHost.transform.Find("UI/Prompt").gameObject;
                var firstTarget = new Target(); var secondTarget = new Target();
                scope.Events.Publish(new InteractionFocusChangedEvent(firstTarget));
                Assert.AreEqual(1, firstTarget.Reads); Assert.AreEqual(0, secondTarget.Reads);
                Assert.IsTrue(promptRoot.activeSelf); Assert.IsFalse(otherPrompt.activeSelf); Assert.IsNull(other.Events.CurrentFocus);
                other.Events.Publish(new InteractionFocusChangedEvent(secondTarget));
                Assert.AreEqual(1, firstTarget.Reads); Assert.AreEqual(1, secondTarget.Reads);
                scope.Dispose(); Assert.IsFalse(promptRoot.activeSelf); Assert.IsTrue(otherPrompt.activeSelf);
                other.Events.Publish(new InteractionFocusChangedEvent(secondTarget)); Assert.AreEqual(2, secondTarget.Reads);
            }
            finally { other.Dispose(); Object.DestroyImmediate(otherHost); EditorSceneManager.ClosePreviewScene(otherScene); }
        }
        [Test] public void StandaloneOwnerRejectsDuplicateAndComposedOwnership()
        {
            var owner = host.AddComponent<SceneInteractionBindings>(); owner.Validate();
            var duplicate = uiHost.AddComponent<SceneInteractionBindings>();
            Assert.Throws<InvalidOperationException>(() => owner.Validate()); Object.DestroyImmediate(duplicate);
            var composed = host.AddComponent<SOLITUDE.Composition.SceneBindings>();
            Assert.Throws<InvalidOperationException>(() => owner.Validate()); Object.DestroyImmediate(composed);
            owner.Validate(); owner.Release(); owner.Release();
        }
        [Test] public void InactiveBindLateEnableAndRebindDeliverEachFocusOnce()
        {
            uiHost.SetActive(false); scope.Initialize(scene, player);
            var target = new Target(); scope.Events.Publish(new InteractionFocusChangedEvent(target));
            Assert.AreEqual(0, target.Reads); Assert.IsFalse(promptRoot.activeInHierarchy);
            uiHost.SetActive(true); Invoke(ui, "OnEnable"); Assert.IsTrue(promptRoot.activeSelf);
            for (int i = 0; i < 4; i++)
            {
                Invoke(ui, "OnDisable"); ui.enabled = false;
                int disabledReads = target.Reads; scope.Events.Publish(new InteractionFocusChangedEvent(target));
                Assert.AreEqual(disabledReads, target.Reads); Assert.IsFalse(promptRoot.activeSelf);
                ui.enabled = true; Invoke(ui, "OnEnable"); Invoke(ui, "OnEnable");
                ui.Bind(scope.Events, scope.Feedback); Invoke(ui, "OnEnable");
                int previous = target.Reads; scope.Events.Publish(new InteractionFocusChangedEvent(target));
                Assert.AreEqual(previous + 1, target.Reads);
            }
            var oldEvents = scope.Events; var oldFeedback = scope.Feedback;
            scope.Dispose(); scope.Dispose(); Assert.IsFalse(promptRoot.activeSelf);
            Assert.IsNull(typeof(InteractionFeedbackService).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(oldFeedback));
            int count = target.Reads; oldEvents.Publish(new InteractionFocusChangedEvent(target)); Assert.AreEqual(count, target.Reads);
            ui.enabled = false; ui.enabled = true; Invoke(ui, "OnEnable"); Assert.IsFalse(promptRoot.activeSelf);
            scope.Initialize(scene, player); scope.Events.Publish(new InteractionFocusChangedEvent(target));
            Assert.IsTrue(promptRoot.activeSelf); Assert.AreNotSame(oldEvents, scope.Events);
        }
        [Test] public void InputDisableAndPlayerDisableClearFocusAndPrompt()
        {
            scope.Initialize(scene, player); var target = new Target();
            // A cached target models the player's accepted focus without changing target selection.
            Set(player, "currentTarget", target); scope.Events.Publish(new InteractionFocusChangedEvent(target));
            Assert.IsTrue(promptRoot.activeSelf); player.SetInputAllowed(false);
            Assert.IsFalse(promptRoot.activeSelf); Assert.IsNull(scope.Events.CurrentFocus);
            Set(player, "currentTarget", target); scope.Events.Publish(new InteractionFocusChangedEvent(target));
            Invoke(player, "OnDisable"); Assert.IsFalse(promptRoot.activeSelf); Assert.IsNull(scope.Events.CurrentFocus);
        }
        [Test] public void PartialReceiverBindingFailureUnwindsUIPlayerAndFeedback()
        {
            var receiver = host.AddComponent<FailingFeedbackReceiver>();
            Assert.Throws<InvalidOperationException>(() => scope.Initialize(scene, player));
            Assert.AreEqual(1, receiver.Releases); Assert.IsNull(scope.Events); Assert.IsNull(scope.Feedback);
            Assert.IsNull(typeof(InteractionFeedbackService).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(receiver.LastService));
            scope.Dispose(); Assert.AreEqual(1, receiver.Releases); Assert.IsFalse(promptRoot.activeSelf);
            Object.DestroyImmediate(receiver); scope.Initialize(scene, player); Assert.IsNotNull(scope.Events);
        }
        [Test] public void MissingDuplicateAndWrongSceneUIBindingsFailClosed()
        {
            var originalView = ui.FeedbackView;
            Set(ui, "feedbackView", null); Assert.Throws<InvalidOperationException>(() => scope.Initialize(scene, player));
            Set(ui, "feedbackView", originalView);
            var duplicate = uiHost.AddComponent<UIController>(); Assert.Throws<InvalidOperationException>(() => scope.Initialize(scene, player));
            Object.DestroyImmediate(duplicate);
            var otherScene = EditorSceneManager.NewPreviewScene(); var otherHost = new GameObject("Other view");
            SceneManager.MoveGameObjectToScene(otherHost, otherScene);
            try
            {
                Set(ui, "feedbackView", otherHost.AddComponent<InteractionFeedbackView>());
                Assert.Throws<InvalidOperationException>(() => scope.Initialize(scene, player));
                Assert.IsNull(scope.Events); Assert.IsNull(scope.Feedback);
            }
            finally { Set(ui, "feedbackView", originalView); Object.DestroyImmediate(otherHost); EditorSceneManager.ClosePreviewScene(otherScene); }
        }
        [Test] public void DestroyedListenerAndFailedValidationLeaveNoDanglingBindings()
        {
            scope.Initialize(scene, player); var oldEvents = scope.Events; var target = new Target();
            Invoke(ui, "OnDestroy"); Object.DestroyImmediate(ui); oldEvents.Publish(new InteractionFocusChangedEvent(target)); Assert.AreEqual(0, target.Reads);
            Assert.Throws<InvalidOperationException>(() => scope.Initialize(scene, player));
            Assert.IsNull(scope.Events); Assert.IsNull(scope.Feedback); Assert.IsNull(oldEvents.CurrentFocus);
        }
    }
}
