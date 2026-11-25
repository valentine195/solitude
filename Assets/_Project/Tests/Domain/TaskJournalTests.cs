using System;
using NUnit.Framework;
using SOLITUDE.Tasks;

namespace SOLITUDE.Tests
{
    public class TaskJournalTests
    {
        private TaskJournal journal;
        private ITaskPublisher source;
        private TaskKey Key(string local = "repair", string owner = "a") => new TaskKey(owner, local);
        private TaskSnapshot Active(bool discovered = true, string stage = "fuse", string next = "Replace fuse") =>
            new TaskSnapshot(discovered, TaskOutcome.Active, "Restore atmosphere", stage, "Room A", TaskCondition.Fault, "OFFLINE", next);
        private TaskSnapshot Terminal(TaskOutcome outcome = TaskOutcome.Completed, bool discovered = true, string result = "Restored") =>
            new TaskSnapshot(discovered, outcome, "Restore atmosphere", result: result);
        [SetUp] public void SetUp()
        {
            journal = new TaskJournal();
            Assert.AreEqual(TaskWriteResult.Applied, journal.RegisterSource("a", "room", out source));
        }
        [TearDown] public void TearDown() => journal.Dispose();

        [Test] public void HiddenProgressAndLateTerminalDiscoveryExposeOnlyCurrentFact()
        {
            int events = 0; TaskJournalChange delivered = null;
            var watch = journal.ObserveWithSnapshot(change => { events++; delivered = change; }, out var empty);
            Assert.AreEqual(0, empty.Revision);
            source.Publish("repair", Active(false));
            source.Publish("repair", Active(false, "restart", "Restart"));
            source.Publish("repair", Terminal(discovered: false));
            Assert.AreEqual(0, events); Assert.AreEqual(0, journal.ReadSnapshot().Active.Count);
            Assert.AreEqual(0, journal.ReadSnapshot().Completed.Count);

            source.Publish("repair", Terminal());
            Assert.AreEqual(1, events); Assert.AreEqual(TaskChangeCause.Discovered, delivered.Records[0].Cause);
            Assert.IsNull(delivered.Records[0].Before); Assert.AreEqual("Restored", delivered.Records[0].After.State.Result);
            Assert.AreEqual(1, journal.ReadSnapshot().Completed.Count); watch.Dispose();
        }
        [Test] public void RepeatedWritesAndInvalidTransitionsPreserveImmutableAcceptedSnapshot()
        {
            source.Publish("repair", Active());
            var accepted = journal.ReadSnapshot();
            Assert.AreEqual(TaskWriteResult.Unchanged, source.Publish("repair", Active()));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", Active(false)));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", new TaskSnapshot(true, TaskOutcome.Active, "Title", nextStep: "Do")));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", new TaskSnapshot(true, TaskOutcome.Completed, "Title")));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", new TaskSnapshot(true, (TaskOutcome)99, "Title")));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish(" ", Active()));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", null));
            Assert.AreEqual(accepted.Revision, journal.ReadSnapshot().Revision);
            source.Publish("repair", Terminal());
            Assert.AreEqual(TaskOutcome.Active, accepted.Active[0].State.Outcome);
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", Active()));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", Terminal(TaskOutcome.Failed)));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("repair", new TaskSnapshot(true, TaskOutcome.Abandoned)));
            Assert.AreEqual(TaskWriteResult.Applied, source.Publish("repair", Terminal(result: "Corrected result")));
            Assert.AreEqual("Corrected result", journal.ReadSnapshot().Completed[0].State.Result);
        }
        [Test] public void SourceOwnershipAndGenerationsPreventStaleOrCrossScopeWrites()
        {
            Assert.AreEqual(TaskWriteResult.Conflict, journal.RegisterSource("a", "room", out var conflict)); Assert.IsNull(conflict);
            Assert.AreEqual(TaskWriteResult.Invalid, journal.RegisterSource("", "room", out conflict));
            journal.RegisterSource("b", "other", out var second);
            source.Publish("repair", Active()); second.Publish("repair", Active());
            Assert.AreEqual(2, journal.ReadSnapshot().Active.Count);
             source.Release();
            Assert.IsFalse(journal.ReadSnapshot().Find(Key()).SourceAvailable);

            Assert.AreEqual(TaskWriteResult.Conflict, journal.RegisterSource("a", "other", out conflict));
            journal.RegisterSource("a", "room", out var replacement);
            Assert.IsTrue(journal.ReadSnapshot().Find(Key()).SourceAvailable);
            Assert.AreEqual(TaskWriteResult.Unavailable, source.Publish("repair", Terminal()));
            Assert.AreEqual(TaskWriteResult.Unavailable, source.Release());
            Assert.AreEqual(TaskWriteResult.Unchanged, replacement.Publish("repair", Active()));
            journal.ReleaseScope("room");
            Assert.AreEqual(1, journal.ReadSnapshot().Active.Count);
            Assert.AreEqual(TaskWriteResult.Unavailable, replacement.Publish("repair", Active()));
            Assert.AreEqual(TaskWriteResult.Unchanged, journal.ReleaseScope("room"));
            journal.RegisterSource("a", "room", out var fresh);
            Assert.AreEqual(TaskWriteResult.Applied, fresh.Publish("repair", Active()));
        }
        [Test] public void ActiveUpdatesAndAvailabilityKeepDiscoveryOrder()
        {
            source.Publish("first", Active()); source.Publish("second", Active());
            source.Publish("first", Active(stage: "restart", next: "Restart")); source.Release();
            var snapshot = journal.ReadSnapshot();
            Assert.AreEqual(Key("first"), snapshot.Active[0].Key);
            Assert.AreEqual(Key("second"), snapshot.Active[1].Key);
            Assert.IsFalse(snapshot.Active[0].SourceAvailable);
        }
        [Test] public void StagesAreSourceOwnedAndFailureAndAbandonmentRemainTerminal()
        {
            source.Publish("repair", Active(stage: "restart", next: "Restart"));
            source.Publish("navigation", Active(stage: "ask-ai", next: "Talk to AI"));
            Assert.AreEqual("ask-ai", journal.ReadSnapshot().Find(Key("navigation")).State.StageKey);

            source.Publish("navigation", new TaskSnapshot(true, TaskOutcome.Abandoned));
            Assert.IsNull(journal.ReadSnapshot().Find(Key("navigation")));
            Assert.AreEqual(TaskWriteResult.Invalid, source.Publish("navigation", Active()));
            source.Publish("repair", Terminal(TaskOutcome.Failed, result: "Unable to restore"));
            Assert.AreEqual(TaskOutcome.Failed, journal.ReadSnapshot().Completed[0].State.Outcome);
            Assert.AreEqual("Unable to restore", journal.ReadSnapshot().Completed[0].State.Result);
            source.Publish("new-occurrence", Active());
            source.Publish("new-occurrence", Terminal());
            Assert.AreEqual(Key("new-occurrence"), journal.ReadSnapshot().Completed[0].Key);
            source.Publish("repair", Terminal(TaskOutcome.Failed, result: "Correction"));
            Assert.AreEqual(Key("new-occurrence"), journal.ReadSnapshot().Completed[0].Key);
        }
        [Test] public void ObservationIsGapFreeIsolatedAndRejectsEveryReentrantMutation()
        {
            int delivered = 0, diagnosed = 0, lateEvents = 0; long lateRevision = -1;
            journal.ObserverError += _ => throw new Exception("diagnostic observer");
            journal.ObserverError += _ => diagnosed++;
            journal.ObserveWithSnapshot(_ => throw new Exception("observer"), out _);
            journal.ObserveWithSnapshot(change =>
            {
                delivered++;
                Assert.AreEqual(change.Revision, journal.ReadSnapshot().Revision);
                Assert.AreEqual(TaskWriteResult.Busy, source.Publish("repair", Terminal()));
                Assert.AreEqual(TaskWriteResult.Busy, source.Release());

                Assert.AreEqual(TaskWriteResult.Busy, journal.ReleaseScope("room"));
                Assert.AreEqual(TaskWriteResult.Busy, journal.RegisterSource("b", "room", out _));
                if (delivered == 1)
                {
                    journal.ObserveWithSnapshot(later => { lateEvents++; Assert.AreEqual(lateRevision + 1, later.Revision); lateRevision = later.Revision; }, out var snapshot);
                    lateRevision = snapshot.Revision;
                }
            }, out var initial);
            Assert.AreEqual(0, initial.Revision);
            source.Publish("repair", Active());
            Assert.AreEqual(1, delivered); Assert.AreEqual(1, diagnosed); Assert.AreEqual(0, lateEvents);
            source.Publish("repair", Active(stage: "restart", next: "Restart"));
            Assert.AreEqual(2, delivered); Assert.AreEqual(2, diagnosed); Assert.AreEqual(1, lateEvents);
        }
        [Test] public void DetachmentRetainsRecordsAndSessionDisposalReleasesOnlyItsOwnState()
        {
            source.Publish("repair", Active()); source.Release();

            using (var other = new TaskJournal())
            {
                other.RegisterSource("a", "room", out var independent); independent.Publish("repair", Active());
                journal.Dispose(); journal.Dispose();
                Assert.AreEqual(0, journal.ReadSnapshot().Active.Count);
                Assert.AreEqual(TaskWriteResult.Unavailable, source.Publish("repair", Active()));
                Assert.AreEqual(TaskWriteResult.Unavailable, journal.RegisterSource("c", "room", out _));
                Assert.AreEqual(1, other.ReadSnapshot().Active.Count);
            }
        }
    }
}
