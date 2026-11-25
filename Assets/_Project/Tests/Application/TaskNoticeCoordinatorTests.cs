using System;
using NUnit.Framework;
using SOLITUDE.Application;
using SOLITUDE.Tasks;

namespace SOLITUDE.Tests
{
    public sealed class TaskNoticeCoordinatorTests
    {
        private sealed class Display : ITaskNoticeDisplay
        {
            internal TaskNoticePresentation Last;
            internal bool Fail;
            internal int Shown;
            public void Render(TaskNoticePresentation value)
            { if (Fail && value != null) throw new InvalidOperationException("display failed"); Last = value; if (value != null) Shown++; }
        }
        private TaskJournal journal;
        private ITaskPublisher source;
        private PauseCoordinator pause;
        private InputPolicyCoordinator policy;
        private TaskNoticeCoordinator notices;
        private Display display;
        private static TaskSnapshot State(TaskOutcome outcome = TaskOutcome.Active, string step = "Replace fuse") =>
            new TaskSnapshot(true, outcome, "Scrubber", "repair", nextStep: step, result: outcome == TaskOutcome.Active ? null : "Outcome details");
        [SetUp] public void SetUp()
        {
            journal = new TaskJournal(); journal.RegisterSource("source", "scene", out source);
            pause = new PauseCoordinator(); policy = new InputPolicyCoordinator(pause); policy.SetReady(true);
            notices = new TaskNoticeCoordinator(journal, policy); display = new Display(); notices.BindDisplay(display);
        }
        [TearDown] public void TearDown() { notices.Dispose(); journal.Dispose(); policy.Dispose(); }
        [Test] public void DiscoveryAndActiveTerminalTransitionsHaveOneNoticeEach()
        {
            source.Publish("a", State()); Assert.AreEqual("TASK ADDED", display.Last.Heading);
            Assert.AreEqual("UNKNOWN", display.Last.Location); Assert.AreEqual("ACTIVE", display.Last.Outcome);
            source.Publish("a", State()); source.Publish("a", State(step: "Restart")); source.Release();
            Assert.AreEqual(1, notices.PendingCount); Assert.AreEqual(1, display.Shown);
            journal.RegisterSource("source", "scene", out source);
            source.Publish("a", State(TaskOutcome.Completed));
            Assert.AreEqual(2, notices.PendingCount); notices.Advance(4);
            Assert.AreEqual("TASK COMPLETE", display.Last.Heading); Assert.AreEqual("COMPLETED", display.Last.Outcome);
            notices.Advance(4); source.Publish("b", State()); source.Publish("b", State(TaskOutcome.Failed));
            notices.Advance(4); Assert.AreEqual("TASK FAILED", display.Last.Heading); Assert.AreEqual("FAILED", display.Last.Outcome);
            notices.Advance(4); Assert.IsNull(display.Last); Assert.AreEqual(4, display.Shown);
        }
        [Test] public void HiddenAndPausedTimeDoesNotConsumeVisibleDwell()
        {
            source.Publish("a", State()); notices.Advance(1);
            using (policy.AcquirePrimaryScreen())
            { Assert.IsNull(display.Last); notices.Advance(200); Assert.AreEqual(3, notices.RemainingSeconds); }
            Assert.AreEqual("TASK ADDED", display.Last.Heading);
            using (pause.Acquire()) { notices.Advance(200); Assert.IsNull(display.Last); }
            policy.SetTransition(true); notices.Advance(200); Assert.IsNull(display.Last);
            policy.SetTransition(false); policy.SetReady(false); notices.Advance(200); Assert.IsNull(display.Last);
            policy.SetReady(true); notices.Advance(3); Assert.IsNull(display.Last); Assert.AreEqual(0, notices.PendingCount);
        }
        [Test] public void UnpresentedDiscoveryCoalescesAndKeepsCrossTaskFifo()
        {
            using (pause.Acquire())
            {
                source.Publish("a", State()); source.Publish("b", State());
                source.Publish("a", State(TaskOutcome.Completed)); source.Publish("b", State(step: "Latest step"));
                Assert.AreEqual(2, notices.PendingCount); Assert.IsNull(display.Last);
            }
            Assert.AreEqual("a", display.Last.Key.LocalTaskId); Assert.AreEqual("TASK COMPLETE", display.Last.Heading);
            Assert.AreEqual("Outcome details", display.Last.Text);
            notices.Advance(4); Assert.AreEqual("b", display.Last.Key.LocalTaskId); Assert.AreEqual("Latest step", display.Last.Text);
            notices.Advance(4); Assert.AreEqual(2, display.Shown);
        }
        [TestCase(TaskOutcome.Completed)] [TestCase(TaskOutcome.Failed)]
        public void LateTerminalDiscoveryIsAddedWithOutcome(TaskOutcome outcome)
        {
            source.Publish("a", State(outcome)); Assert.AreEqual("TASK ADDED", display.Last.Heading);
            Assert.AreEqual(outcome.ToString().ToUpperInvariant(), display.Last.Outcome);
            Assert.AreEqual(1, notices.PendingCount); notices.Advance(4); Assert.IsNull(display.Last);
        }
        [Test] public void RebindingDoesNotReplayExpiredOrPreexistingRecords()
        {
            source.Publish("a", State()); notices.Advance(4);
            var replacement = new Display(); notices.BindDisplay(replacement); Assert.IsNull(replacement.Last);
            using (var late = new TaskNoticeCoordinator(journal, policy))
            { late.BindDisplay(replacement); Assert.AreEqual(0, late.PendingCount); Assert.IsNull(replacement.Last); }
            source.Publish("b", State()); notices.Advance(1); notices.BindDisplay(null); notices.Advance(100);
            Assert.AreEqual(3, notices.RemainingSeconds); notices.BindDisplay(replacement);
            Assert.AreEqual("b", replacement.Last.Key.LocalTaskId); notices.Advance(3); Assert.IsNull(replacement.Last);
        }
        [Test] public void AbandonmentScopeAndSessionReleaseDropCurrentAndQueuedNotices()
        {
            source.Publish("a", State()); source.Publish("b", State()); source.Publish("a", new TaskSnapshot(true, TaskOutcome.Abandoned));
            Assert.AreEqual("b", display.Last.Key.LocalTaskId); Assert.AreEqual(1, notices.PendingCount);
            journal.ReleaseScope("scene"); Assert.IsNull(display.Last); Assert.AreEqual(0, notices.PendingCount);
            journal.RegisterSource("next", "next-scene", out source); source.Publish("a", State());
            journal.Dispose(); Assert.IsNull(display.Last); Assert.AreEqual(0, notices.PendingCount);
        }
        [Test] public void ThrowingDisplayAndDiagnosticsDoNotUndoAcceptedFactsOrOtherObservers()
        {
            int errors = 0, observed = 0;
            notices.PresentationError += _ => { errors++; throw new Exception("diagnostic failed"); };
            display.Fail = true;
            using (journal.ObserveWithSnapshot(_ => observed++, out _))
            {
                Assert.AreEqual(TaskWriteResult.Applied, source.Publish("a", State()));
                Assert.AreEqual(TaskWriteResult.Applied, source.Publish("a", State(TaskOutcome.Completed)));
            }
            Assert.AreEqual(2, observed); Assert.AreEqual(2, errors); Assert.AreEqual(1, notices.PendingCount);
            Assert.AreEqual(1, journal.ReadSnapshot().Completed.Count); notices.Advance(100); Assert.AreEqual(4, notices.RemainingSeconds);
            display.Fail = false; notices.Advance(0); Assert.AreEqual("TASK COMPLETE", display.Last.Heading);
            notices.Dispose(); Assert.IsNull(display.Last); source.Publish("b", State()); Assert.AreEqual(0, notices.PendingCount);
        }
    }
}
