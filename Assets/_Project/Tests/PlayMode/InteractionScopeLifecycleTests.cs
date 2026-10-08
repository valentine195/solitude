#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using SOLITUDE.Player;
using SOLITUDE.World.OpeningSlice;
using SOLITUDE.World.SolitudeStart;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SOLITUDE.Tests
{
    public sealed class InteractionLifetimeTarget : MonoBehaviour, IInteractable
    {
        public bool Allows = true;
        public int Attempts;
        public InteractionResult Result = InteractionResult.Success("Restored");
        public string GetPrompt() => "Test interaction";
        public bool CanInteract(PlayerInteractor actor) => Allows;
        public InteractionResult Interact(PlayerInteractor actor) { Attempts++; return Result; }
    }
    public class InteractionScopeLifecycleTests
    {
        private Scene scene;
        private GameObject host, promptRoot, feedbackRoot;
        private PlayerInteractor player;
        private UIController ui;
        private TextMeshProUGUI label;
        private SceneInteractionScope scope;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        [SetUp] public void Prepare()
        {
            scene = SceneManager.CreateScene("Interaction fixture " + Guid.NewGuid().ToString("N"));
            host = new GameObject("Interaction lifetime fixture"); SceneManager.MoveGameObjectToScene(host, scene);
            var actor = new GameObject("Player"); actor.transform.SetParent(host.transform); player = actor.AddComponent<PlayerInteractor>();
            var uiHost = new GameObject("UI"); uiHost.SetActive(false); uiHost.transform.SetParent(host.transform);
            promptRoot = new GameObject("Prompt", typeof(RectTransform)); promptRoot.transform.SetParent(uiHost.transform);
            var prompt = uiHost.AddComponent<InteractionPromptView>();
            Set(prompt, "root", promptRoot); Set(prompt, "promptText", promptRoot.AddComponent<TextMeshProUGUI>());
            feedbackRoot = new GameObject("Feedback", typeof(RectTransform)); feedbackRoot.transform.SetParent(uiHost.transform);
            var view = uiHost.AddComponent<InteractionFeedbackView>(); label = feedbackRoot.AddComponent<TextMeshProUGUI>();
            Set(view, "root", feedbackRoot); Set(view, "label", label); feedbackRoot.SetActive(false);
            ui = uiHost.AddComponent<UIController>(); Set(ui, "interactionPrompt", prompt); Set(ui, "feedbackView", view);
            uiHost.SetActive(true); scope = new SceneInteractionScope();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            scope.Dispose(); if (host != null) Object.Destroy(host);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator PlayerFocusTriggerAndFeedbackKeepTheirExistingSemantics()
        {
            scope.Initialize(scene, player);
            var target = new GameObject("Target").AddComponent<InteractionLifetimeTarget>(); target.transform.SetParent(host.transform);
            player.Register(target); player.SetInputAllowed(true);
            int focus = 0, triggers = 0; scope.Events.FocusChanged += evt => { if (evt.interactable == target) focus++; };
            scope.Events.Triggered += _ => triggers++;
            yield return null; yield return null;
            Assert.AreEqual(1, focus); Assert.IsTrue(promptRoot.activeSelf);
            target.Allows = false; player.TryInteract();
            Assert.AreEqual("Cannot interact", label.text); Assert.AreEqual(Color.red, label.color); Assert.AreEqual(0, triggers); Assert.AreEqual(0, target.Attempts);
            target.Allows = true; player.TryInteract();
            Assert.AreEqual("Restored", label.text); Assert.AreEqual((Color)new Color32(220,235,238,255), label.color);
            Assert.AreEqual(1, triggers); Assert.AreEqual(1, target.Attempts);
            target.Result = InteractionResult.Success(); player.TryInteract(); Assert.AreEqual("Restored", label.text);
            player.SetInputAllowed(false); Assert.IsFalse(promptRoot.activeSelf); Assert.IsNull(scope.Events.CurrentFocus);
            player.SetInputAllowed(true); yield return null; Assert.IsTrue(promptRoot.activeSelf);
            for (int i = 0; i < 3; i++)
            {
                ui.enabled = false; Assert.IsFalse(promptRoot.activeSelf);
                ui.enabled = true; Assert.IsTrue(promptRoot.activeSelf);
                player.enabled = false; Assert.IsFalse(promptRoot.activeSelf);
                player.enabled = true; yield return null; Assert.IsTrue(promptRoot.activeSelf);
            }
            var oldEvents = scope.Events; var oldFeedback = scope.Feedback;
            scope.Dispose(); scope.Dispose(); Assert.IsFalse(promptRoot.activeSelf); Assert.IsFalse(feedbackRoot.activeSelf);
            oldEvents.Publish(new InteractionFocusChangedEvent(target)); oldFeedback.Handle(InteractionResult.Fail("Stale"));
            Assert.IsFalse(promptRoot.activeSelf); Assert.IsFalse(feedbackRoot.activeSelf); Assert.IsNull(Field<InteractionFeedbackView>(oldFeedback, "view"));
        }
        [UnityTest] public IEnumerator DestroyedUIUnsubscribesBeforeTheSceneScopeIsReleased()
        {
            scope.Initialize(scene, player); var events = scope.Events;
            Object.Destroy(ui); yield return null;
            var target = new GameObject("Target").AddComponent<InteractionLifetimeTarget>(); target.transform.SetParent(host.transform);
            events.Publish(new InteractionFocusChangedEvent(target)); Assert.IsFalse(promptRoot.activeSelf);
            var listeners = typeof(SceneInteractionEvents).GetField("FocusChanged", Private).GetValue(events) as Delegate;
            Assert.IsNull(listeners); scope.Dispose(); Assert.IsNull(events.CurrentFocus);
        }
        [UnityTest] public IEnumerator AuthoredInactiveFeedbackRootActivatesAndCanShowAgainAfterHide()
        {
            var notification = new GameObject("Inactive notification", typeof(RectTransform)); notification.transform.SetParent(host.transform);
            var text = new GameObject("Text", typeof(RectTransform)); text.transform.SetParent(notification.transform);
            var message = text.AddComponent<TextMeshProUGUI>(); var view = notification.AddComponent<InteractionFeedbackView>();
            Set(view, "root", notification); Set(view, "label", message); notification.SetActive(false);
            Set(ui, "feedbackView", view); scope.Initialize(scene, player);
            scope.Feedback.Handle(InteractionResult.Success("First")); yield return null;
            Assert.IsTrue(notification.activeSelf); Assert.AreEqual("First", message.text);
            scope.Feedback.Clear(); Assert.IsFalse(notification.activeSelf);
            scope.Feedback.Handle(InteractionResult.Fail("Second")); Assert.IsTrue(notification.activeSelf); Assert.AreEqual("Second", message.text);
            scope.Dispose(); Assert.IsFalse(notification.activeSelf);
        }
        [UnityTest] public IEnumerator FeedbackServicesNeverRenderIntoEachOthersViews()
        {
            scope.Initialize(scene, player);
            var other = new GameObject("Other feedback"); other.transform.SetParent(host.transform);
            var otherRoot = new GameObject("Text", typeof(RectTransform)); otherRoot.transform.SetParent(other.transform);
            var otherLabel = otherRoot.AddComponent<TextMeshProUGUI>(); var view = other.AddComponent<InteractionFeedbackView>();
            Set(view, "root", otherRoot); Set(view, "label", otherLabel);
            using var service = new InteractionFeedbackService(view);
            scope.Feedback.Handle(InteractionResult.Fail("First")); service.Handle(InteractionResult.Success("Second"));
            yield return null;
            Assert.AreEqual("First", label.text); Assert.AreEqual("Second", otherLabel.text);
            scope.Dispose(); Assert.IsFalse(feedbackRoot.activeSelf); Assert.IsTrue(otherRoot.activeSelf);
            service.Dispose(); Assert.IsFalse(otherRoot.activeSelf); Assert.IsNull(Field<InteractionFeedbackView>(service, "view"));
        }
        [UnityTest] public IEnumerator InactiveFeatureFeedbackReceiversBindAndSceneReleaseStopsContactFragments()
        {
            var door = new GameObject("Door").AddComponent<SolitudeSlidingDoor>(); door.transform.SetParent(host.transform); Set(door, "open", true);
            var socket = new GameObject("Socket").AddComponent<OpeningPowerSocket>(); socket.transform.SetParent(host.transform); socket.door = door;
            Set(socket, "<Powered>k__BackingField", true);
            var contactHost = new GameObject("Contact"); contactHost.SetActive(false); contactHost.transform.SetParent(host.transform);
            var contact = contactHost.AddComponent<OpeningFirstContact>(); contact.socket = socket;
            var completionHost = new GameObject("Completion"); completionHost.SetActive(false); completionHost.transform.SetParent(host.transform);
            var completion = completionHost.AddComponent<SolitudeStartCompletion>(); completion.Configure(door);
            scope.Initialize(scene, player);
            Assert.AreSame(scope.Feedback, Field<InteractionFeedbackService>(contact, "feedback"));
            Assert.AreSame(scope.Feedback, Field<InteractionFeedbackService>(completion, "feedback"));
            var collider = player.gameObject.AddComponent<CircleCollider2D>();
            completionHost.SetActive(true); completion.GetType().GetMethod("OnTriggerEnter2D", Private).Invoke(completion, new object[] { collider });
            Assert.AreEqual("Access granted", label.text);
            contactHost.SetActive(true); contact.GetType().GetMethod("OnTriggerEnter2D", Private).Invoke(contact, new object[] { collider });
            Assert.AreEqual("...caretaker?", label.text); Assert.IsTrue(contact.Heard);
            scope.Dispose(); Assert.IsNull(Field<InteractionFeedbackService>(contact, "feedback"));
            Assert.IsNull(Field<InteractionFeedbackService>(completion, "feedback")); Assert.IsFalse(feedbackRoot.activeSelf);
            yield return new WaitForSeconds(3.3f); Assert.AreEqual("...caretaker?", label.text, "Released contact cannot publish its next fragment.");
        }
        [UnityTest] public IEnumerator StandaloneDirectEntryOwnsAndReleasesItsFeedbackWithoutSessionBootstrap()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Features/World/SolitudeStart/Scenes/SOLITUDE_Start.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var owner = Object.FindObjectsByType<SceneInteractionBindings>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var ownedScope = Field<SceneInteractionScope>(owner, "scope"); Assert.IsNotNull(ownedScope);
            var actor = Object.FindObjectsByType<PlayerInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var completion = Object.FindObjectsByType<SolitudeStartCompletion>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            var door = Field<SolitudeSlidingDoor>(completion, "door"); Set(door, "open", true);
            var collider = actor.GetComponent<Collider2D>() ?? actor.gameObject.AddComponent<CircleCollider2D>();
            completion.enabled = false; completion.GetType().GetMethod("OnTriggerEnter2D", Private).Invoke(completion, new object[] { collider });
            var feedback = ownedScope.Feedback; var events = ownedScope.Events;
            var view = Object.FindObjectsByType<InteractionFeedbackView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            Assert.AreEqual("Access granted", Field<TextMeshProUGUI>(view, "label").text);
            owner.Release(); owner.Release(); Assert.IsNull(Field<InteractionFeedbackView>(feedback, "view")); Assert.IsNull(events.CurrentFocus);
            Assert.IsNull(Field<InteractionFeedbackService>(completion, "feedback"));
        }
    }
}
#endif
