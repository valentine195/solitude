using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Core.Input;
using SOLITUDE.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace SOLITUDE.Tests
{
    public class TaskJournalInputTests
    {
        private sealed class Display : ITaskJournalDisplay, ITaskJournalScreenDisplay
        {
            public TaskJournalPresenter Presenter;
            public bool Fail;
            public void Render(TaskJournalPresentation model) { }
            public void Show() { if (Fail) throw new System.Exception("show"); Presenter.SetVisible(true); }
            public void Hide() => Presenter.SetVisible(false);
        }
        private static void Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
        [UnityTest] public IEnumerator SharedKeyboardRoutesJournalBackAndPreservesOtherOwners()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode(); yield return Verify(); yield return new ExitPlayMode();
        }
        private static IEnumerator Verify()
        {
            var pause = new PauseCoordinator(); pause.SetBaseScale(.5f);
            var policy = new InputPolicyCoordinator(pause); var journal = new TaskJournal();
            journal.RegisterSource("a", "scope", out var writer);
            var key = new TaskKey("a", "task");
            writer.Publish("task", new TaskSnapshot(true, TaskOutcome.Active, "Task", "stage", nextStep: "Instruction"));
            var display = new Display(); var presenter = new TaskJournalPresenter(journal, display); display.Presenter = presenter;
            var screen = new TaskJournalScreenCoordinator(policy, pause, display);
            var inputObject = new GameObject("Journal Input"); var input = inputObject.AddComponent<UnityInputHost>(); input.Initialize(policy);
            var uiObject = new GameObject("Journal EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = uiObject.GetComponent<InputSystemUIInputModule>(); var uiLease = input.ConnectUI(module);
            int inventories = 0, fallbacks = 0;
            input.Bind(null, null, null, () => inventories++, () => false, () => fallbacks++, screen);
            var background = InputSystem.settings.backgroundBehavior; var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); policy.SetReady(true);

                Press(keyboard, Key.J); Assert.IsTrue(screen.IsOpen); Assert.AreEqual(0, pause.Scale); Assert.IsTrue(module.enabled);

                yield return null;
                var actions = (SOLITUDE_InputActions)typeof(UnityInputHost).GetField("actions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(input);
                Assert.IsNull(actions.asset.FindAction("Gameplay/JournalPin", false));
                Assert.IsFalse(actions.Gameplay.Move.enabled); Assert.IsFalse(actions.Gameplay.Interact.enabled); Assert.IsFalse(actions.Gameplay.Hotbar1.enabled);
                Assert.IsTrue(actions.asset.FindAction("UI/Navigate").enabled); Assert.IsTrue(actions.asset.FindAction("UI/Submit").enabled);
                Assert.AreEqual("J", input.BindingLabel("Gameplay/Journal"));
                Press(keyboard, Key.Tab); Assert.AreEqual(0, inventories);

                presenter.Select(key);
                Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(.5f, pause.Scale);
                Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen, "Closing activation is consumed for the frame.");
                yield return null;
                Press(keyboard, Key.J); Assert.IsTrue(screen.IsOpen); yield return null;
                Press(keyboard, Key.Escape); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(0, fallbacks);

                Press(keyboard, Key.E); Press(keyboard, Key.Tab); Assert.AreEqual(0, inventories, "Inventory remains unavailable in close frame.");
                yield return null;
                using (var other = policy.AcquirePrimaryScreen()) { Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); Assert.IsTrue(policy.PrimaryScreenActive); }
                using (pause.Acquire()) { Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(0, pause.Scale); }
                policy.SetTransition(true); Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); policy.SetTransition(false);
                display.Fail = true; Press(keyboard, Key.J); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(.5f, pause.Scale); Assert.IsFalse(policy.PrimaryScreenActive);
                display.Fail = false; Press(keyboard, Key.J); Assert.IsTrue(screen.IsOpen);
                writer.Publish("task", new TaskSnapshot(true, TaskOutcome.Completed, "Task", result: "Done"));
                yield return null;
                input.Bind(null, null, null, null, null, null); Assert.IsFalse(screen.IsOpen); Assert.AreEqual(.5f, pause.Scale);
                Assert.DoesNotThrow(() => { screen.Dispose(); input.Release(); uiLease.Dispose(); });
            }
            finally
            {
                screen.Dispose(); presenter.Dispose(); input.Release(); uiLease.Dispose(); policy.Dispose(); journal.Dispose();
                InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
                Object.Destroy(uiObject); Object.Destroy(inputObject);
            }
            yield return null;
        }
    }
}
