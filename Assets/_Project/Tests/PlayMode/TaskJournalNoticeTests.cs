#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Composition;
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
    public class TaskJournalNoticeTests
    {
        private const string Scene = "Assets/_Project/Scenes/Demos/AtmosphericScrubberEncounter.unity";
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private static T One<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single();
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(target);
        [UnitySetUp] public IEnumerator Prepare()
        {
            foreach(var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; background = InputSystem.settings.backgroundBehavior; editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            foreach(var root in Object.FindObjectsByType<GameCompositionRoot>(FindObjectsSortMode.None)) Object.Destroy(root.gameObject);
            yield return null; Time.timeScale = 1;
        }
        private void Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
        }
        private IEnumerator Approach(Component target)
        {
            One<PlayerInteractor>().GetComponent<Rigidbody2D>().position = (Vector2)target.transform.position + Vector2.down*.3f;
            Physics2D.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null; yield return null;
        }
        [UnityTest] public IEnumerator ActualFocusAndRepairProduceSingleNoticesWithoutFocusTheft()
        {
            var notice = One<TaskNoticeView>(); Assert.IsFalse(notice.IsVisible);
            var legacy = One<ScrubberTaskNotificationView>(); Assert.IsFalse(legacy.enabled);
            var selected = EventSystem.current.currentSelectedGameObject;
            yield return Approach(One<ScrubberAccess>());
            Assert.IsTrue(notice.IsVisible); Assert.AreEqual("TASK ADDED",notice.Presentation.Heading);
            Assert.AreEqual(1,notice.PresentationCount); Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject);
            var coordinator = Field<TaskNoticeCoordinator>(One<SceneBindings>(),"taskNotices"); coordinator.Advance(4);
            Assert.IsFalse(notice.IsVisible);
            yield return Approach(One<WorldPickup>()); Press(Key.E); yield return null;
            yield return Approach(One<ScrubberAccess>()); Press(Key.E); yield return null;
            var screen = One<ScrubberScreenView>(); Assert.IsTrue(screen.IsOpen);
            screen.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="InstallFuse").onClick.Invoke();
            screen.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b=>b.name=="Restart").onClick.Invoke();
            var equipment = One<ScrubberEquipment>(); Assert.IsTrue(equipment.State.Running);
            selected = EventSystem.current.currentSelectedGameObject; yield return new WaitForSeconds(5.1f);
            Assert.IsTrue(equipment.State.Operational); Assert.IsFalse(notice.IsVisible); Assert.AreEqual(1,notice.PresentationCount);
            Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject); Assert.IsFalse(legacy.IsVisible);
            screen.Close(); yield return null;
            Assert.IsTrue(notice.IsVisible); Assert.AreEqual("TASK COMPLETE",notice.Presentation.Heading);
            Assert.AreEqual("COMPLETED",notice.Presentation.Outcome); Assert.AreEqual(2,notice.PresentationCount);
            Assert.IsNull(EventSystem.current.currentSelectedGameObject); Assert.AreEqual(0,legacy.PresentationCount);
            coordinator.Advance(4); Assert.IsFalse(notice.IsVisible); notice.enabled = false; notice.enabled = true;
            yield return null; Assert.IsFalse(notice.IsVisible); Assert.AreEqual(2,notice.PresentationCount);
        }
        [UnityTest] public IEnumerator JournalInventoryTransferAndPauseHideAndResumeRemainingDwell()
        {
            yield return Approach(One<ScrubberAccess>()); var notice = One<TaskNoticeView>(); Assert.IsTrue(notice.IsVisible);
            var scope = One<SceneBindings>(); var coordinator = Field<TaskNoticeCoordinator>(scope,"taskNotices");
            coordinator.Advance(1); double remaining = coordinator.RemainingSeconds;
            Press(Key.J); yield return null; Assert.IsTrue(One<TaskJournalView>().IsOpen); Assert.IsFalse(notice.IsVisible);
            var selected = EventSystem.current.currentSelectedGameObject; coordinator.Advance(100); Assert.AreEqual(remaining,coordinator.RemainingSeconds);
            Assert.AreSame(selected,EventSystem.current.currentSelectedGameObject); Press(Key.J); yield return null; Assert.IsTrue(notice.IsVisible);
            Press(Key.Tab); yield return null; Assert.IsFalse(notice.IsVisible); remaining = coordinator.RemainingSeconds;
            coordinator.Advance(100); Assert.AreEqual(remaining,coordinator.RemainingSeconds); Press(Key.Escape); yield return null;
            Assert.IsTrue(notice.IsVisible);
            var modal = Field<ContainerModalCoordinator>(scope,"modal");
            using (var target = Field<SOLITUDE.SaveLoad.ContainerSaveSession>(One<GameCompositionRoot>(),"session").Register("notice-transfer",1))
            {
                Assert.AreEqual(OpenResult.Opened,modal.OpenTarget(target.Container)); Assert.IsFalse(notice.IsVisible);
                remaining = coordinator.RemainingSeconds; coordinator.Advance(100); Assert.AreEqual(remaining,coordinator.RemainingSeconds);
                modal.Close(); Assert.IsTrue(notice.IsVisible);
            }
            var pause = Field<PauseCoordinator>(One<GameCompositionRoot>(),"pause");
            using (pause.Acquire()) { Assert.IsFalse(notice.IsVisible); remaining = coordinator.RemainingSeconds; coordinator.Advance(100); Assert.AreEqual(remaining,coordinator.RemainingSeconds); }
            Assert.IsTrue(notice.IsVisible); coordinator.Advance(remaining); Assert.IsFalse(notice.IsVisible);
        }
        [UnityTest] public IEnumerator FailedFixtureCoalescesAndSceneReleaseDropsPendingLane()
        {
            var scope = One<SceneBindings>(); var coordinator = Field<TaskNoticeCoordinator>(scope,"taskNotices");
            var journal = (TaskJournal)One<GameCompositionRoot>().Tasks; journal.RegisterSource("notice-fixture","notice-scope",out var publisher);
            var pause = Field<PauseCoordinator>(One<GameCompositionRoot>(),"pause");
            using (pause.Acquire())
            {
                publisher.Publish("a",new TaskSnapshot(true,TaskOutcome.Active,"Fixture", "inspect",nextStep:"Inspect"));
                publisher.Publish("a",new TaskSnapshot(true,TaskOutcome.Failed,"Fixture",result:"Unable to restore"));
            }
            var notice = One<TaskNoticeView>(); Assert.IsTrue(notice.IsVisible); Assert.AreEqual("TASK FAILED",notice.Presentation.Heading);
            Assert.AreEqual(1,notice.PresentationCount); Assert.AreEqual("UNKNOWN",notice.Presentation.Location);
            Assert.That(notice.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="Outcome").text,Does.Contain("! FAILED"));
            coordinator.Advance(4); publisher.Publish("b",new TaskSnapshot(true,TaskOutcome.Active,"Other", "inspect",nextStep:"Inspect"));
            Assert.IsTrue(notice.IsVisible); journal.ReleaseScope("notice-scope"); Assert.IsFalse(notice.IsVisible);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene,new LoadSceneParameters(LoadSceneMode.Single)); yield return null;
            Assert.IsFalse(One<TaskNoticeView>().IsVisible); Assert.AreEqual(0,Field<TaskNoticeCoordinator>(One<SceneBindings>(),"taskNotices").PendingCount);
        }
    }
}
#endif
