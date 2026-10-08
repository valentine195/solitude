using System;
using System.Reflection;
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
            host = new GameObject("Task source fixture");
            equipment = host.AddComponent<ScrubberEquipment>(); access = host.AddComponent<ScrubberAccess>(); source = host.AddComponent<ScrubberTaskSource>();
            player = new GameObject("Player").AddComponent<PlayerInteractor>(); player.transform.SetParent(host.transform);
            var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(host.transform); panel.SetActive(false);
            TMP_Text Text(string name) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(panel.transform); return go.AddComponent<TextMeshProUGUI>(); }
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button)); close.transform.SetParent(panel.transform);
            screen = host.AddComponent<ScrubberScreenView>();
            screen.Configure(panel, equipment, Text("Telemetry"), Text("Objective"), Text("Feedback"), close.GetComponent<Button>());
            access.Configure(screen); source.Configure("fixture-scrubber", "Fixture room", equipment, access);
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Features/World/ScrubberEncounter/Demo/ScrubberItemDatabase.asset");
            commands = new ContainerCommandService(database.BuildRuntimeCatalog()); inventory = commands.Register(ContainerSaveSession.PlayerInventoryId, 2);
            equipment.Initialize(commands, inventory, database.BuildPresentationCatalog());
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true); screen.Initialize(policy, null);
            journal = new TaskJournal(); source.Initialize(journal, "fixture-scope", policy, player, true);
        }
        [TearDown] public void TearDown()
        {
            source.ReleaseBinding(); screen.Release(); equipment.ReleaseBinding(); journal.Dispose(); policy.Dispose(); UnityEngine.Object.DestroyImmediate(host);
        }
        private void Focus() => InteractionEventBus.Publish(new InteractionFocusChangedEvent(access));
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
                InteractionEventBus.Publish(new InteractionTriggeredEvent(access, player)); Assert.IsNull(Record);
            }
            access.enabled = false; Focus(); Assert.IsNull(Record); access.enabled = true;
            source.Initialize(journal, "fixture-scope", policy, player, false); Focus(); Assert.IsNull(Record);
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
            Focus(); Invoke(source, "OnDisable"); source.enabled = false;
            Assert.IsFalse(Record.SourceAvailable);
            commands.Grant(inventory.Handle, ScrubberEquipment.FuseItemId, 1); equipment.Install();
            Assert.AreEqual("replace-fuse", Record.State.StageKey);
            source.enabled = true; Invoke(source, "OnEnable"); Assert.IsTrue(Record.SourceAvailable);
            Assert.AreEqual("restart", Record.State.StageKey);
            source.ReleaseBinding(); journal.ReleaseScope("fixture-scope"); Assert.IsNull(Record);
            source.Initialize(journal, "fixture-scope-new", policy, player, true); Assert.IsNull(Record); Focus(); Assert.AreEqual("restart", Record.State.StageKey);
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
            source.Initialize(journal, "fixture-scope", policy, player, true); Assert.AreEqual("recover-air", Record.State.StageKey);
        }
    }
}
