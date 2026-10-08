using System;
using System.Reflection;
using System.Collections.Generic;
using SOLITUDE.Composition;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Containers;
using SOLITUDE.Core.Events;
using SOLITUDE.Items;
using SOLITUDE.Player;
using SOLITUDE.SaveLoad;
using SOLITUDE.Tasks;
using SOLITUDE.World.ScrubberEncounter;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SOLITUDE.Tests
{
    public class ScrubberTaskSourceTests
    {
        private Scene fixtureScene;
        private SceneInteractionEvents events;
        private ItemPresentationCatalog presentations;
        private GameObject host;
        private ScrubberEquipment equipment;
        private ScrubberAccess access;
        private ScrubberTaskSource source;
        private ScrubberScreenView screen;
        private PlayerInteractor player;
        private ContainerCommandService commands;
        private Container inventory;
        private TaskJournal journal;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private TaskRecord Record => journal.ReadSnapshot().Find(source.Key);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
        [SetUp] public void SetUp()
        {
            fixtureScene = EditorSceneManager.NewPreviewScene();
            host = new GameObject("Task source fixture");
            equipment = host.AddComponent<ScrubberEquipment>(); access = host.AddComponent<ScrubberAccess>(); source = host.AddComponent<ScrubberTaskSource>();
            player = new GameObject("Player").AddComponent<PlayerInteractor>(); player.transform.SetParent(host.transform);
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(host.transform); panel.SetActive(false);
            TMP_Text Text(string name) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(panel.transform); return go.AddComponent<TextMeshProUGUI>(); }
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button)); close.transform.SetParent(panel.transform);
            screen = host.AddComponent<ScrubberScreenView>();
            screen.Configure(panel, equipment, Text("Telemetry"), Text("Objective"), Text("Feedback"), close.GetComponent<Button>());
            SceneManager.MoveGameObjectToScene(host, fixtureScene);
            access.Configure(screen); source.Configure("fixture-scrubber", "Fixture room", equipment, access);
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberItemDatabase.asset");
            commands = new ContainerCommandService(database.BuildRuntimeCatalog()); inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 2);
            presentations = database.BuildPresentationCatalog();
            equipment.Initialize(commands, inventory, presentations);
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true); screen.Initialize(policy, null);
            events = new SceneInteractionEvents();
            journal = new TaskJournal(); source.Initialize(journal, "fixture-scope", policy, player, events, true);
        }
        [TearDown] public void TearDown()
        {
            source.ReleaseBinding(); screen.Release(); equipment.ReleaseBinding(); journal.Dispose(); events.Dispose(); policy.Dispose(); UnityEngine.Object.DestroyImmediate(host); EditorSceneManager.ClosePreviewScene(fixtureScene);
        }
        private ScrubberComposition Composition()
        {
            var composition = host.AddComponent<ScrubberComposition>();
            composition.Configure(new[] { equipment }, new[] { screen }, new[] { source });
            return composition;
        }
        private SceneFeatureContext Context(IContainerReader reader = null) => new SceneFeatureContext(
            commands, reader ?? inventory, presentations, policy, null, journal, "fixture-scope", player, events);
        [Test] public void FeatureRebindActivationAndRepeatedReleaseLeaveNoDuplicateEndpoints()
        {
            var composition = Composition();
            for (int i = 0; i < 3; i++)
            {
                source.ReleaseBinding(); screen.Release(); equipment.ReleaseBinding();
                host.SetActive(false); composition.enabled = false;
                composition.Initialize(Context());
                Assert.IsFalse(source.IsRegistered, "Inactive task sources attach on activation.");
                host.SetActive(true); Invoke(source, "OnEnable"); // Edit Mode fixture explicitly drives the lifecycle callback.
                Assert.IsTrue(source.IsRegistered); Focus(); Assert.IsNotNull(Record);
                composition.enabled = true; composition.enabled = false; composition.enabled = true;
                var listeners = (Delegate)typeof(ScrubberEquipment).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(equipment);
                Assert.AreEqual(2, listeners.GetInvocationList().Length, "Exactly one screen and one task subscription.");
                Assert.IsTrue(screen.Open()); Assert.IsTrue(composition.TryBack()); Assert.IsFalse(composition.TryBack());
                Assert.IsTrue(screen.Open()); composition.CloseForTransition(); Assert.IsFalse(screen.IsOpen);
                Assert.IsTrue(screen.Open()); composition.Release(); composition.Release();
                Assert.IsFalse(screen.IsOpen); Assert.IsFalse(policy.PrimaryScreenActive); Assert.IsTrue(policy.Gameplay);
                Assert.IsFalse(source.IsRegistered); Assert.IsFalse(screen.Open());
                Assert.That(equipment.Install(), Does.Contain("unavailable"));
                Assert.IsFalse(Record.SourceAvailable);
                // Scope ownership remains with SceneBindings, never the feature.
                journal.ReleaseScope("fixture-scope"); Assert.IsNull(Record);
            }
        }
        private sealed class FailingReader : IContainerReader
        {
            private readonly IContainerReader inner;
            private Action<int> slots;
            public int ListenerCount => slots?.GetInvocationList().Length ?? 0;
            public FailingReader(IContainerReader reader) { inner = reader; }
            public ContainerHandle Handle => inner.Handle;
            public bool IsAvailable => inner.IsAvailable;
            public int Capacity => inner.Capacity;
            public event Action<int> SlotChanged { add => slots += value; remove => slots -= value; }
            public event Action CapacityChanged { add => throw new InvalidOperationException("Injected bind failure"); remove { } }
            public SlotSnapshot GetSlot(int index) => inner.GetSlot(index);
            public IReadOnlyList<SlotSnapshot> GetSlots() => inner.GetSlots();
            public bool CanAccept(ItemSpec item) => inner.CanAccept(item);
        }
        [Test] public void FeaturePartialEquipmentFailureUnwindsAndCanBindAgain()
        {
            source.ReleaseBinding(); screen.Release(); equipment.ReleaseBinding();
            var composition = Composition(); var broken = new FailingReader(inventory);
            Assert.That(Assert.Throws<InvalidOperationException>(() => composition.Initialize(Context(broken))).Message,
                Does.Contain("Injected bind failure"));
            composition.Release(); composition.Release(); Assert.AreEqual(0, broken.ListenerCount);
            Assert.IsFalse(source.IsRegistered); Assert.IsFalse(screen.Open()); Assert.IsFalse(policy.PrimaryScreenActive);
            composition.Initialize(Context()); Focus(); Assert.IsNotNull(Record); Assert.IsTrue(screen.Open());
            composition.Release(); Assert.IsTrue(policy.Gameplay);
        }
        [Test] public void FeatureKeepsMissingFuseCatalogTaskEligibilityFailClosed()
        {
            source.ReleaseBinding(); screen.Release(); equipment.ReleaseBinding();
            var composition = Composition();
            var production = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/Items/Resources/Database.asset");
            composition.Initialize(new SceneFeatureContext(commands, inventory, production.BuildPresentationCatalog(),
                policy, null, journal, "fixture-scope", player, events));
            Assert.IsFalse(source.IsRegistered); Focus(); Assert.IsNull(Record);
            Assert.That(equipment.Install(), Does.Contain("unavailable")); composition.Release();
        }
        private void Focus() => events.Publish(new InteractionFocusChangedEvent(access));
        [Test] public void TaskSourceRebindingSwitchesEventScopesWithoutDuplicateSubscriptions()
        {
            using var other = new SceneInteractionEvents();
            source.Initialize(journal, "fixture-scope", policy, player, other, true);
            Focus(); Assert.IsNull(Record, "The previous event source is detached.");
            for (int i = 0; i < 4; i++)
            {
                source.enabled = false; Invoke(source, "OnDisable");
                Assert.IsNull(typeof(SceneInteractionEvents).GetField("FocusChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(other));
                source.enabled = true; Invoke(source, "OnEnable"); Invoke(source, "OnEnable");
                source.Initialize(journal, "fixture-scope", policy, player, other, true);
                var handlers = (Delegate)typeof(SceneInteractionEvents).GetField("FocusChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(other);
                Assert.AreEqual(1, handlers.GetInvocationList().Length);
            }
            other.Publish(new InteractionFocusChangedEvent(access)); Assert.IsNotNull(Record);
            source.ReleaseBinding(); source.ReleaseBinding();
            Assert.IsNull(typeof(SceneInteractionEvents).GetField("FocusChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(other));
        }
        [Test] public void EligibleFaultFocusAndSuccessfulAccessShareOneOccurrence()
        {
            Assert.IsNull(Record); Focus(); Assert.AreEqual("replace-fuse", Record.State.StageKey);
            var order = Record.DiscoveryOrder; Focus(); Assert.AreEqual(order, Record.DiscoveryOrder);
            Assert.IsTrue(access.Interact(player).IsSuccess); Assert.AreEqual(1, journal.ReadSnapshot().Active.Count);
            Assert.AreEqual(order, Record.DiscoveryOrder); screen.Close();
        }
        [Test] public void BlockedAccessTriggeredAndIneligibleFocusDoNotDiscover()
        {
            policy.SetReady(false); Focus(); Assert.IsNull(Record); policy.SetReady(true);
            using (policy.AcquirePrimaryScreen())
            {
                Focus(); Assert.IsFalse(access.Interact(player).IsSuccess);
                events.Publish(new InteractionTriggeredEvent(access, player)); Assert.IsNull(Record);
            }
            access.enabled = false; Focus(); Assert.IsNull(Record); access.enabled = true;
            source.Initialize(journal, "fixture-scope", policy, player, events, false); Focus(); Assert.IsNull(Record);
        }
        [Test] public void ConfirmedRepairStagesAndLateCompletedAccessFollowEquipment()
        {
            Assert.That(equipment.Install(), Does.Contain("Requires")); Assert.IsNull(Record);
            commands.Grant(inventory.Handle, ScrubberEquipment.FuseItemId, 1);
            equipment.Install(); Assert.IsNull(Record);
            Assert.IsTrue(access.Interact(player).IsSuccess); Assert.AreEqual("restart", Record.State.StageKey); screen.Close();
             equipment.Restart(); Assert.AreEqual("recover-air", Record.State.StageKey);
            equipment.State.Advance(4.9f); Assert.AreEqual(TaskOutcome.Active, Record.State.Outcome);
            equipment.State.Advance(.2f); Assert.AreEqual(TaskOutcome.Completed, Record.State.Outcome);
             Assert.AreEqual("Room atmosphere restored.", Record.State.Result);
            equipment.Restart(); equipment.Install(); Focus(); Assert.AreEqual(1, journal.ReadSnapshot().Completed.Count);
        }
        [Test] public void OperationalFocusStaysHiddenUntilSuccessfulAccess()
        {
            commands.Grant(inventory.Handle, ScrubberEquipment.FuseItemId, 1); equipment.Install(); equipment.Restart(); equipment.State.Advance(5);
            Focus(); Assert.IsNull(Record);
            Assert.IsTrue(access.Interact(player).IsSuccess); Assert.AreEqual(TaskOutcome.Completed, Record.State.Outcome);
            Assert.AreEqual(0, journal.ReadSnapshot().Active.Count); Assert.AreEqual(1, journal.ReadSnapshot().Completed.Count);
        }
        [Test] public void DisableRetainsUnavailableTaskAndReattachmentReconcilesCurrentStage()
        {
            Focus();  Invoke(source, "OnDisable"); source.enabled = false;
            Assert.IsFalse(Record.SourceAvailable);
            commands.Grant(inventory.Handle, ScrubberEquipment.FuseItemId, 1); equipment.Install();
            Assert.AreEqual("replace-fuse", Record.State.StageKey);
            source.enabled = true; Invoke(source, "OnEnable"); Assert.IsTrue(Record.SourceAvailable);
            Assert.AreEqual("restart", Record.State.StageKey);
            source.ReleaseBinding(); journal.ReleaseScope("fixture-scope"); Assert.IsNull(Record);
            source.Initialize(journal, "fixture-scope-new", policy, player, events, true); Assert.IsNull(Record); Focus(); Assert.AreEqual("restart", Record.State.StageKey);
        }
        [Test] public void ObserverErrorsAndDetachedSourceCannotUndoEquipmentTransactions()
        {
            Focus(); int errors = 0; journal.ObserverError += _ => errors++;
            journal.ObserveWithSnapshot(_ => throw new Exception("journal observer"), out _);
            commands.Grant(inventory.Handle, ScrubberEquipment.FuseItemId, 1);
            Assert.AreEqual("Fuse replaced. Restart scrubber.", equipment.Install());
            Assert.IsTrue(equipment.State.FuseInstalled); Assert.IsTrue(inventory.GetSlot(0).IsEmpty); Assert.Greater(errors, 0);
            access.AccessSucceeded += _ => throw new InvalidOperationException("access observer");
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: access observer");
            Assert.IsTrue(access.Interact(player).IsSuccess); screen.Close();
            source.ReleaseBinding(); Assert.That(equipment.Restart(), Does.Contain("restarted")); Assert.IsTrue(equipment.State.Running);
            source.Initialize(journal, "fixture-scope", policy, player, events, true); Assert.AreEqual("recover-air", Record.State.StageKey);
        }
    }
}
