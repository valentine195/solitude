using System;
using NUnit.Framework;
using SOLITUDE.Tasks;
using SOLITUDE.Application;

namespace SOLITUDE.Tests
{
    public class TaskJournalPresenterTests
    {
        private sealed class Display : ITaskJournalDisplay
        {
            public TaskJournalPresentation Last;
            public bool Fail;
            public void Render(TaskJournalPresentation model) { if (Fail) throw new Exception("view"); Last = model; }
        }
        private TaskJournal journal;
        private ITaskPublisher source;
        private Display display;
        private TaskJournalPresenter presenter;
        private TaskKey Key(string id) => new TaskKey("a", id);
        private TaskSnapshot Active(string title = "Title") => new TaskSnapshot(true, TaskOutcome.Active, title, "stage", nextStep: "Instruction");
        [SetUp] public void SetUp()
        {
            journal = new TaskJournal(); journal.RegisterSource("a", "scope", out source);
            display = new Display(); presenter = new TaskJournalPresenter(journal, display);
        }
        [TearDown] public void TearDown() { presenter.Dispose(); journal.Dispose(); }
        [Test] public void SelectionAndReopenRetainIdentityFocusAndScroll()
        {
            source.Publish("first", Active()); source.Publish("second", Active());
            presenter.SetVisible(true); presenter.Select(Key("second"));
            presenter.RememberPosition("row:a/second", .4f, .7f);
            presenter.SetVisible(false); presenter.Select(Key("first")); presenter.SetVisible(true);
            Assert.AreEqual(Key("second"), display.Last.SelectedKey);
            Assert.AreEqual("row:a/second", display.Last.FocusToken);
            Assert.AreEqual(.4f, display.Last.ListScroll); Assert.AreEqual(.7f, display.Last.DetailScroll);
            Assert.AreEqual(Key("first"), display.Last.Rows[0].Key);
        }
        [Test] public void RequiredFirstRenderPropagatesFailureAndClearsVisibility()
        {
            display.Fail = true;
            Assert.Throws<Exception>(() => presenter.SetVisible(true, requireRender: true));
            Assert.IsFalse(presenter.IsVisible);
            display.Fail = false; presenter.SetVisible(true, requireRender: true);
            Assert.IsTrue(presenter.IsVisible);
        }
        [Test] public void RemovalChoosesNextThenPreviousThenNoneAndKeepsTab()
        {
            source.Publish("a", Active()); source.Publish("b", Active()); source.Publish("c", Active());
            presenter.SetVisible(true); presenter.Select(Key("b"));
            source.Publish("b", new TaskSnapshot(true, TaskOutcome.Completed, "Done", result: "Restored"));
            Assert.AreEqual(Key("c"), display.Last.SelectedKey.Value);
            source.Publish("c", new TaskSnapshot(true, TaskOutcome.Abandoned));
            Assert.AreEqual(Key("a"), display.Last.SelectedKey.Value);
            source.Publish("a", new TaskSnapshot(true, TaskOutcome.Abandoned));
            Assert.IsNull(display.Last.SelectedKey); Assert.AreEqual("NO TASK SELECTED", display.Last.Title);
            Assert.AreEqual("NO ACTIVE TASKS", display.Last.EmptyText); Assert.AreEqual(TaskJournalTab.Active, display.Last.Tab);
            presenter.SelectTab(TaskJournalTab.Completed); journal.ReleaseScope("scope");
            Assert.AreEqual("NO COMPLETED TASKS", display.Last.EmptyText);
            presenter.SetVisible(false); presenter.SetVisible(true);
            Assert.AreEqual(TaskJournalTab.Completed, display.Last.Tab);
        }
        [Test] public void LabelsKeepFailureConditionAvailabilityAndCopyLiteral()
        {
            source.Publish("a", Active("<color=red>Literal</color>")); presenter.SetVisible(true);
            Assert.AreEqual("<color=red>Literal</color>", display.Last.Title); Assert.AreEqual("UNKNOWN", display.Last.Location);
            Assert.AreEqual("UNKNOWN", display.Last.ConditionText);
            source.Release(); Assert.AreEqual("SOURCE UNAVAILABLE", display.Last.Availability);

            journal.RegisterSource("a", "scope", out source);
            source.Publish("a", new TaskSnapshot(true, TaskOutcome.Failed, "Failed task", condition: TaskCondition.Fault, conditionText: "OFFLINE", result: "Unable to restore"));
            presenter.SelectTab(TaskJournalTab.Completed);
            Assert.AreEqual("FAILED", display.Last.Outcome); Assert.AreEqual("FAILED", display.Last.Rows[0].Outcome);
            Assert.AreEqual("RESULT", display.Last.StepLabel); Assert.AreEqual("Unable to restore", display.Last.StepText);
            Assert.AreEqual("OFFLINE", display.Last.ConditionText);

            source.Publish("b", new TaskSnapshot(true, TaskOutcome.Completed, "Complete", result: "Success"));
            presenter.Select(Key("b")); Assert.AreEqual("COMPLETED", display.Last.Outcome);
        }
        [Test] public void MissingAndFailingDisplayCannotUndoSourceWritesOrLeakObservation()
        {
            int errors = 0; presenter.PresentationError += _ => errors++;
            display.Fail = true; presenter.SetVisible(true);
            Assert.AreEqual(TaskWriteResult.Applied, source.Publish("a", Active()));
            Assert.AreEqual(2, errors); Assert.AreEqual(1, journal.ReadSnapshot().Active.Count);
            using (var missing = new TaskJournalPresenter(journal, null)) { missing.SetVisible(true); Assert.AreEqual(1, missing.Presentation.ActiveCount); }
            presenter.Dispose(); presenter.Dispose();
            source.Publish("b", Active()); Assert.AreEqual(2, errors);
        }
    }
}
