#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Composition;
using SOLITUDE.Core.Input;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.Tasks;
using SOLITUDE.Tasks.UI;
using SOLITUDE.World.ScrubberEncounter;
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
    public class TaskJournalSliceTests
    {
        private const string Scene = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
        [UnitySetUp] public IEnumerator Prepare()
        {
            foreach(var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null;
            background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            foreach(var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private void Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
        }
        private static IEnumerator Approach(Component target)
        {
            One<PlayerInteractor>().GetComponent<Rigidbody2D>().position = (Vector2)target.transform.position + Vector2.down*.3f;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator ActualDemoDiscoversRepairsPausesCompletesAndResets()
        {
            var root = One<GameCompositionRoot>(); var view = One<TaskJournalView>(); var access = One<ScrubberAccess>(); var source = One<ScrubberTaskSource>();
            Assert.AreEqual(RuntimeStartupState.Ready,root.State); Assert.AreEqual(0,root.Tasks.ReadSnapshot().Active.Count);
            Press(Key.J); yield return null; Assert.IsTrue(view.IsOpen); Assert.AreEqual("NO ACTIVE TASKS",view.Presenter.Presentation.EmptyText);
            Press(Key.J); yield return null; Assert.IsFalse(view.IsOpen);
            yield return Approach(access); Assert.AreEqual(1,root.Tasks.ReadSnapshot().Active.Count);
            Press(Key.J); Assert.IsTrue(view.IsOpen); Assert.AreEqual(0,Time.timeScale);
            Assert.AreEqual(One<UnityInputHost>().BindingLabel("Container/Cancel"),view.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(t=>t.transform.parent.name=="BackKey").text);
             yield return null;
            var selected = EventSystem.current.currentSelectedGameObject; Assert.IsNotNull(selected);
            var row = One<TaskJournalRowView>(); row.Button.onClick.Invoke(); Assert.AreEqual(source.Key, view.Presenter.Presentation.SelectedKey);

            Press(Key.Escape,Key.E); yield return null; Assert.IsFalse(view.IsOpen); Assert.IsFalse(One<ScrubberScreenView>().IsOpen);
            Press(Key.J); yield return null; Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject); Press(Key.J); yield return null;
            yield return Approach(One<WorldPickup>()); Press(Key.E); yield return null; Assert.IsFalse(One<WorldPickup>().gameObject.activeSelf);
            yield return Approach(access); Press(Key.E); yield return null;
            var screen = One<ScrubberScreenView>(); Assert.IsTrue(screen.IsOpen); Press(Key.J); Assert.IsFalse(view.IsOpen,"Equipment owner rejects journal opening.");
            var buttons = screen.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            buttons.Single(b=>b.name=="InstallFuse").onClick.Invoke(); Assert.IsTrue(One<ScrubberEquipment>().State.FuseInstalled); screen.Close(); yield return null;
            Press(Key.J); yield return null; Assert.That(view.Presenter.Presentation.StepText,Does.Contain("Restart")); Press(Key.J); yield return null;
            Press(Key.E); yield return null; Assert.IsTrue(screen.IsOpen); buttons.Single(b=>b.name=="Restart").onClick.Invoke(); screen.Close(); yield return null;
            var equipment = One<ScrubberEquipment>(); Assert.IsTrue(equipment.State.Running);
            Press(Key.J); yield return null; var recovery = equipment.State.RecoverySeconds;
            yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(recovery,equipment.State.RecoverySeconds,"Journal pause stops actual recovery.");
            Press(Key.J); yield return new WaitForSeconds(5.1f); Assert.IsTrue(equipment.State.Operational);
            Assert.AreEqual(0,root.Tasks.ReadSnapshot().Active.Count); Assert.AreEqual(1,root.Tasks.ReadSnapshot().Completed.Count);
            Press(Key.J); yield return null; Assert.AreEqual("NO ACTIVE TASKS",view.Presenter.Presentation.EmptyText);
            view.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Completed").onClick.Invoke();
            Assert.AreEqual("COMPLETED",view.Presenter.Presentation.Outcome);
            One<SceneBindings>().CloseForTransition(); Assert.IsFalse(view.IsOpen); Assert.AreEqual(1,Time.timeScale);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.AreEqual(0,One<GameCompositionRoot>().Tasks.ReadSnapshot().Completed.Count); Assert.IsFalse(One<TaskJournalView>().IsOpen);
        }
        [UnityTest] public IEnumerator UnavailableSourceAndFailedFixtureStayLiteralWhileViewTeardownReleasesPause()
        {
            yield return Approach(One<ScrubberAccess>()); var source = One<ScrubberTaskSource>(); var view = One<TaskJournalView>(); var journal = (TaskJournal)One<GameCompositionRoot>().Tasks;
            Press(Key.J);  source.enabled = false; yield return null;
            Assert.AreEqual("SOURCE UNAVAILABLE",view.Presenter.Presentation.Availability);
            Assert.AreEqual(TaskWriteResult.Applied,journal.RegisterSource("test-fixture","test-scope",out var publisher));
            publisher.Publish("active",new TaskSnapshot(true,TaskOutcome.Active,"Another task",stageKey:"inspect",nextStep:"Inspect this fixture."));
            var second = view.GetComponentsInChildren<TaskJournalRowView>(true).Single(r=>r.Key.SourceId=="test-fixture");
            EventSystem.current.SetSelectedGameObject(second.Button.gameObject);
            Assert.AreEqual(second.Key,view.Presenter.Presentation.SelectedKey,"Navigation focus selects details.");

            publisher.Publish("active",new TaskSnapshot(true,TaskOutcome.Abandoned));
            publisher.Publish("failed",new TaskSnapshot(true,TaskOutcome.Failed,"<b>Literal failure</b>",result:"Unable to finish.",condition:TaskCondition.Fault));
            view.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Completed").onClick.Invoke();
            Assert.AreEqual("FAILED",view.Presenter.Presentation.Outcome); Assert.AreEqual("<b>Literal failure</b>",view.Presenter.Presentation.Title);
            Assert.IsTrue(view.GetComponentsInChildren<TMPro.TMP_Text>(true).All(label=>!label.richText));
            view.enabled = false; Assert.AreEqual(1,Time.timeScale); Press(Key.J); Assert.IsFalse(view.IsOpen);
            source.enabled = true; Assert.IsTrue(journal.ReadSnapshot().Find(source.Key).SourceAvailable);
            journal.ReleaseScope("test-scope"); Assert.AreEqual(0,journal.ReadSnapshot().Completed.Count);
        }
        [UnityTest] public IEnumerator KeyboardNavigationRevealsOverflowRowsAndRetainsScrollOnReopen()
        {
            var journal = (TaskJournal)One<GameCompositionRoot>().Tasks;
            journal.RegisterSource("overflow", "overflow-scope", out var writer);
            for (int i = 0; i < 12; i++) writer.Publish("task-" + i,
                new TaskSnapshot(true, TaskOutcome.Active, "Maintenance task " + i, "inspect", nextStep: "Inspect the equipment."));
            var view = One<TaskJournalView>(); Press(Key.J); yield return null;
            Assert.IsNull(((SOLITUDE_InputActions)typeof(UnityInputHost).GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(One<UnityInputHost>())).asset.FindAction("Gameplay/JournalPin", false));
            Assert.IsFalse(view.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Pin" || t.name == "PinKey" || t.name == "PinIcon"));
            for (int i = 0; i < 10; i++)
            {
                var current = EventSystem.current.currentSelectedGameObject;
                var move = new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down, moveVector = Vector2.down };
                ExecuteEvents.Execute(current, move, ExecuteEvents.moveHandler);
                yield return null;
            }
            var selected = EventSystem.current.currentSelectedGameObject;
            var row = selected.GetComponent<TaskJournalRowView>(); Assert.IsNotNull(row);
            Assert.AreEqual("task-10", row.Key.LocalTaskId);
            var scroll = view.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true).Single(x => x.name == "TaskList");
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, (RectTransform)row.transform);
            Assert.GreaterOrEqual(bounds.min.y, scroll.viewport.rect.yMin - 1);
            Assert.LessOrEqual(bounds.max.y, scroll.viewport.rect.yMax + 1);
            var retained = scroll.verticalNormalizedPosition; Assert.Less(retained, .9f);
            Press(Key.J); yield return null; Press(Key.J); yield return null;
            Assert.AreSame(selected, EventSystem.current.currentSelectedGameObject);
            Assert.AreEqual(retained, scroll.verticalNormalizedPosition, .01f);
        }
        [UnityTest] public IEnumerator ConcreteRenderFailureClosesJournalAndPreservesAcceptedSourceWrites()
        {
            var view = One<TaskJournalView>();
            var field = typeof(TaskJournalView).GetField("title", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var original = field.GetValue(view); field.SetValue(view, null);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("NullReferenceException"));
            Press(Key.J); yield return null;
            Assert.IsFalse(view.IsOpen); Assert.IsFalse(view.Presenter.IsVisible); Assert.AreEqual(1, Time.timeScale);
            Assert.IsFalse(view.transform.Find("Panel").gameObject.activeSelf);
            field.SetValue(view, original); Press(Key.J); yield return null; Assert.IsTrue(view.IsOpen);
            field.SetValue(view, null);
            var journal = (TaskJournal)One<GameCompositionRoot>().Tasks;
            journal.RegisterSource("render-failure", "render-scope", out var writer);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("NullReferenceException"));
            Assert.AreEqual(TaskWriteResult.Applied, writer.Publish("task", new TaskSnapshot(true, TaskOutcome.Active, "Accepted task", "inspect", nextStep: "Inspect")));
            yield return null;
            Assert.IsFalse(view.IsOpen); Assert.AreEqual(1, Time.timeScale);
            Assert.AreEqual(1, journal.ReadSnapshot().Active.Count);
            field.SetValue(view, original);
        }
    }
}
#endif
