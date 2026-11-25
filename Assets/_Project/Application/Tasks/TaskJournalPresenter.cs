using System;
using System.Collections.Generic;
using SOLITUDE.Tasks;

namespace SOLITUDE.Application
{
    public enum TaskJournalTab { Active, Completed }
    public sealed class TaskRowPresentation
    {
        public TaskKey Key { get; }
        public string Title { get; }
        public string Location { get; }
        public string Outcome { get; }
        public string Availability { get; }
        public bool Selected { get; }
        public TaskCondition Condition { get; }
        internal TaskRowPresentation(TaskRecord record, bool selected)
        { Key = record.Key; Title = record.State.Title; Location = Copy(record.State.Location); Outcome = record.State.Outcome.ToString().ToUpperInvariant();
            Availability = record.SourceAvailable ? "" : "SOURCE UNAVAILABLE"; Selected = selected; Condition = record.State.Condition; }
        internal static string Copy(string text) => string.IsNullOrWhiteSpace(text) ? "UNKNOWN" : text;
    }
    public sealed class TaskJournalPresentation
    {
        public TaskJournalTab Tab { get; }
        public int ActiveCount { get; }
        public int CompletedCount { get; }
        public IReadOnlyList<TaskRowPresentation> Rows { get; }
        public TaskKey? SelectedKey { get; }
        public string Title { get; }
        public string Location { get; }
        public string Outcome { get; }
        public string ConditionText { get; }
        public TaskCondition Condition { get; }
        public string Availability { get; }
        public string StepLabel { get; }
        public string StepText { get; }
        public string Blocker { get; }
        public string EmptyText { get; }
        public string FocusToken { get; }
        public float ListScroll { get; }
        public float DetailScroll { get; }
        internal TaskJournalPresentation(TaskJournalSnapshot snapshot, TaskJournalTab tab, TaskKey? selected,
            string focusToken, float listScroll, float detailScroll)
        {
            Tab = tab; ActiveCount = snapshot.Active.Count; CompletedCount = snapshot.Completed.Count;
            var records = tab == TaskJournalTab.Active ? snapshot.Active : snapshot.Completed;
            var rows = new TaskRowPresentation[records.Count];
            for (int i = 0; i < rows.Length; i++) rows[i] = new TaskRowPresentation(records[i], records[i].Key == selected);
            Rows = Array.AsReadOnly(rows); SelectedKey = selected;
            var record = selected.HasValue ? snapshot.Find(selected.Value) : null;
            Title = record?.State.Title ?? "NO TASK SELECTED"; Location = record == null ? "" : TaskRowPresentation.Copy(record.State.Location);
            Outcome = record?.State.Outcome.ToString().ToUpperInvariant() ?? "";
            Condition = record?.State.Condition ?? TaskCondition.Unknown;
            ConditionText = record == null ? "" : TaskRowPresentation.Copy(record.State.ConditionText);
            Availability = record == null || record.SourceAvailable ? "" : "SOURCE UNAVAILABLE";
            StepLabel = record == null ? "" : record.State.Outcome == TaskOutcome.Active ? "NEXT STEP" : "RESULT";
            StepText = record == null ? "" : record.State.Outcome == TaskOutcome.Active ? record.State.NextStep : record.State.Result;
            Blocker = record?.State.Blocker ?? "";
            EmptyText = records.Count == 0 ? tab == TaskJournalTab.Active ? "NO ACTIVE TASKS" : "NO COMPLETED TASKS" : "";
            FocusToken = focusToken; ListScroll = listScroll; DetailScroll = detailScroll;
        }
    }
    public interface ITaskJournalDisplay { void Render(TaskJournalPresentation presentation); }
    public sealed class TaskJournalPresenter : IDisposable
    {
        private readonly ITaskJournalDisplay display;
        private readonly IDisposable observation;
        private TaskJournalSnapshot snapshot;
        private TaskKey? selected;
        private TaskJournalTab tab;
        private bool visible, disposed;
        private string focusToken;
        private float listScroll = 1, detailScroll = 1;
        public event Action<Exception> PresentationError;
        public TaskJournalPresentation Presentation => new TaskJournalPresentation(snapshot, tab, selected, focusToken, listScroll, detailScroll);
        public bool IsVisible => visible;
        public TaskJournalPresenter(ITaskJournalReader reader, ITaskJournalDisplay display)
        {
            this.display = display;
            observation = reader.ObserveWithSnapshot(Changed, out snapshot);
            Reconcile(null);
        }
        private IReadOnlyList<TaskRecord> Rows(TaskJournalSnapshot state) => tab == TaskJournalTab.Active ? state.Active : state.Completed;
        private void Changed(TaskJournalChange change)
        {
            var old = snapshot; snapshot = change.After; Reconcile(old); Render();
        }
        private void Reconcile(TaskJournalSnapshot old)
        {
            var rows = Rows(snapshot);
            foreach (var row in rows) if (selected == row.Key) return;
            int oldIndex = 0;
            if (selected.HasValue && old != null)
            {
                var previous = Rows(old);
                for (int i = 0; i < previous.Count; i++) if (previous[i].Key == selected.Value) { oldIndex = i; break; }
            }
            selected = rows.Count == 0 ? (TaskKey?)null : rows[Math.Min(oldIndex, rows.Count - 1)].Key;
            if (focusToken != null && focusToken.StartsWith("row:", StringComparison.Ordinal)) focusToken = null;
        }
        public void SetVisible(bool value, bool requireRender = false)
        {
            if (disposed) return;
            visible = value;
            if (!value) return;
            if (!requireRender) { Render(); return; }
            try { display?.Render(Presentation); }
            catch { visible = false; throw; }
        }
        public void SelectTab(TaskJournalTab value)
        {
            if (!visible || disposed || !Enum.IsDefined(typeof(TaskJournalTab), value) || tab == value) return;
            tab = value; selected = null; focusToken = null; listScroll = detailScroll = 1; Reconcile(null); Render();
        }
        public void Select(TaskKey key)
        {
            if (!visible || disposed) return;
            foreach (var row in Rows(snapshot)) if (row.Key == key) { selected = key; Render(); return; }
        }
        public void RememberPosition(string focus, float list, float detail)
        {
            if (!visible || disposed) return;
            focusToken = focus;
            if (!float.IsNaN(list) && !float.IsInfinity(list)) listScroll = Math.Max(0, Math.Min(1, list));
            if (!float.IsNaN(detail) && !float.IsInfinity(detail)) detailScroll = Math.Max(0, Math.Min(1, detail));
        }
        private void Render()
        {
            if (!visible || disposed || display == null) return;
            try { display.Render(Presentation); }
            catch (Exception error)
            {
                if (PresentationError == null) return;
                foreach (Action<Exception> observer in PresentationError.GetInvocationList()) try { observer(error); } catch { }
            }
        }
        public void Dispose() { if (disposed) return; disposed = true; visible = false; observation.Dispose(); }
    }
}
