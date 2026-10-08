using System;
using System.Collections.Generic;
using SOLITUDE.Tasks;

namespace SOLITUDE.Application
{
    public enum TaskNoticeKind { Added, Complete, Failed }
    public sealed class TaskNoticePresentation
    {
        public TaskKey Key { get; }
        public TaskNoticeKind Kind { get; }
        public string Heading => Kind == TaskNoticeKind.Added ? "TASK ADDED" : Kind == TaskNoticeKind.Complete ? "TASK COMPLETE" : "TASK FAILED";
        public string Title { get; }
        public string Location { get; }
        public string Outcome { get; }
        public string Text { get; }
        public long Revision { get; }
        internal TaskNoticePresentation(TaskRecord record, TaskNoticeKind kind)
        {
            Key = record.Key; Kind = kind; Title = record.State.Title;
            Location = TaskRowPresentation.Copy(record.State.Location);
            Outcome = record.State.Outcome.ToString().ToUpperInvariant();
            Text = record.State.Outcome == TaskOutcome.Active ? record.State.NextStep : record.State.Result;
            Revision = record.Revision;
        }
    }
    // A null presentation hides the lane. Rendering must never acquire focus or input leases.
    public interface ITaskNoticeDisplay { void Render(TaskNoticePresentation presentation); }

    public sealed class TaskNoticeCoordinator : IDisposable
    {
        private sealed class Pending
        {
            internal TaskNoticePresentation Presentation;
            internal double Remaining = VisibleSeconds;
            internal bool Presented;
        }
        public const double VisibleSeconds = 4;
        private readonly InputPolicyCoordinator policy;
        private readonly IDisposable observation;
        private readonly List<Pending> queue = new List<Pending>();
        private ITaskNoticeDisplay display;
        private TaskNoticePresentation rendered;
        private bool visible, disposed;
        public event Action<Exception> PresentationError;
        public bool IsVisible => visible;
        public TaskNoticePresentation Current => queue.Count == 0 ? null : queue[0].Presentation;
        public double RemainingSeconds => queue.Count == 0 ? 0 : queue[0].Remaining;
        public int PendingCount => queue.Count;

        public TaskNoticeCoordinator(ITaskJournalReader reader, InputPolicyCoordinator policy)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            // Observe future accepted changes only; the snapshot is not a notice history.
            observation = reader.ObserveWithSnapshot(Changed, out _);
            policy.Changed += Refresh;
        }
        public void BindDisplay(ITaskNoticeDisplay value)
        {
            if (disposed || ReferenceEquals(display, value)) return;
            Render(null); display = value; rendered = null; visible = false; Refresh();
        }
        private void Changed(TaskJournalChange change)
        {
            if (disposed) return;
            queue.RemoveAll(item => change.After.Find(item.Presentation.Key) == null);
            foreach (var record in change.Records)
            {
                if (record.After == null) continue;
                var kind = record.Cause == TaskChangeCause.Discovered ? TaskNoticeKind.Added :
                    record.Cause == TaskChangeCause.Completed ? TaskNoticeKind.Complete :
                    record.Cause == TaskChangeCause.Failed ? TaskNoticeKind.Failed : (TaskNoticeKind?)null;
                var unshown = queue.Find(item => item.Presentation.Key == record.Key && !item.Presented);
                if (unshown != null)
                {
                    // Preserve the queue slot/revision order across different tasks.
                    var latestKind = kind.HasValue && kind != TaskNoticeKind.Added ? kind.Value : unshown.Presentation.Kind;
                    unshown.Presentation = new TaskNoticePresentation(record.After, latestKind);
                }
                else if (kind.HasValue)
                    queue.Add(new Pending { Presentation = new TaskNoticePresentation(record.After, kind.Value) });
            }
            Refresh();
        }
        // The caller supplies elapsed seconds. Only successfully presented, eligible exploration counts.
        public void Advance(double elapsedSeconds)
        {
            if (disposed) return;
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (visible && policy.Gameplay && queue.Count != 0)
            {
                queue[0].Remaining -= elapsedSeconds;
                if (queue[0].Remaining <= 0) queue.RemoveAt(0);
            }
            Refresh();
        }
        private void Refresh()
        {
            if (disposed) return;
            var next = policy.Gameplay ? Current : null;
            if (ReferenceEquals(rendered, next) && (next == null || visible)) return;
            if (Render(next) && next != null && queue.Count != 0) queue[0].Presented = true;
        }
        private bool Render(TaskNoticePresentation next)
        {
            visible = false;
            if (display == null) { rendered = null; return false; }
            try { display.Render(next); rendered = next; visible = next != null; return true; }
            catch (Exception error)
            {
                rendered = null;
                // Attempt to hide stale content after a failing render; errors remain isolated.
                try { display.Render(null); } catch { }
                if (PresentationError != null)
                    foreach (Action<Exception> observer in PresentationError.GetInvocationList())
                        try { observer(error); } catch { }
                return false;
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; policy.Changed -= Refresh; observation.Dispose(); queue.Clear();
            Render(null); display = null; rendered = null; PresentationError = null;
        }
    }
}
