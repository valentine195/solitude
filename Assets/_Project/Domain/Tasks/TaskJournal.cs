using System;
using System.Collections.Generic;

namespace SOLITUDE.Tasks
{
    public sealed class TaskJournal : ITaskJournalReader, IDisposable
    {
        private sealed class Source
        {
            internal string Id, Scope;
            internal Publisher Writer;
        }
        private sealed class Entry
        {
            internal TaskKey Key;
            internal Source Source;
            internal TaskSnapshot State;
            internal long Discovery, Terminal, Revision;
        }
        private sealed class Publisher : ITaskPublisher
        {
            internal readonly TaskJournal Journal;
            internal readonly Source Source;
            internal Publisher(TaskJournal journal, Source source) { Journal = journal; Source = source; }
            public TaskWriteResult Publish(string id, TaskSnapshot state) => Journal.Publish(this, id, state);
            public TaskWriteResult Release() => Journal.ReleasePublisher(this);
        }
        private sealed class Subscription : IDisposable
        {
            private TaskJournal journal;
            internal readonly Action<TaskJournalChange> Observer;
            internal Subscription(TaskJournal journal, Action<TaskJournalChange> observer) { this.journal = journal; Observer = observer; }
            public void Dispose() { journal?.observers.Remove(this); journal = null; }
            internal bool Active => journal != null;
        }
        private readonly Dictionary<string, Source> sources = new Dictionary<string, Source>(StringComparer.Ordinal);
        private readonly Dictionary<TaskKey, Entry> entries = new Dictionary<TaskKey, Entry>();
        private readonly List<Subscription> observers = new List<Subscription>();
        private long revision, order;
        private bool delivering, disposed;
        public event Action<Exception> ObserverError;

        private TaskWriteResult? Guard => delivering ? TaskWriteResult.Busy : disposed ? TaskWriteResult.Unavailable : (TaskWriteResult?)null;
        public TaskWriteResult RegisterSource(string sourceId, string scopeId, out ITaskPublisher publisher)
        {
            publisher = null;
            if (Guard.HasValue) return Guard.Value;
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(scopeId)) return TaskWriteResult.Invalid;
            if (sources.TryGetValue(sourceId, out var source))
            {
                if (source.Writer != null || source.Scope != scopeId) return TaskWriteResult.Conflict;
            }
            else { source = new Source { Id = sourceId, Scope = scopeId }; sources.Add(sourceId, source); }
            var before = ReadSnapshot();
            source.Writer = new Publisher(this, source); publisher = source.Writer;
            Notify(before, TaskChangeCause.AvailabilityChanged);
            return TaskWriteResult.Applied;
        }
        private TaskWriteResult Publish(Publisher writer, string localId, TaskSnapshot state)
        {
            if (Guard.HasValue) return Guard.Value;
            if (!Owns(writer)) return TaskWriteResult.Unavailable;
            var key = new TaskKey(writer.Source.Id, localId);
            if (!key.IsValid || !Valid(state)) return TaskWriteResult.Invalid;
            entries.TryGetValue(key, out var entry);
            if (entry != null)
            {
                if (entry.State.Discovered && !state.Discovered) return TaskWriteResult.Invalid;
                if (entry.State.Outcome != TaskOutcome.Active && state.Outcome != entry.State.Outcome) return TaskWriteResult.Invalid;
                if (entry.State.Equals(state)) return TaskWriteResult.Unchanged;
            }
            var before = ReadSnapshot();
            if (entry == null) { entry = new Entry { Key = key, Source = writer.Source }; entries.Add(key, entry); }
            var old = entry.State;
            entry.State = state;
            if (state.Discovered && entry.Discovery == 0) entry.Discovery = ++order;
            if ((state.Outcome == TaskOutcome.Completed || state.Outcome == TaskOutcome.Failed) && entry.Terminal == 0) entry.Terminal = ++order;
            var cause = old == null || !old.Discovered ? TaskChangeCause.Discovered :
                state.Outcome == TaskOutcome.Abandoned ? TaskChangeCause.Removed :
                old.Outcome == TaskOutcome.Active && state.Outcome == TaskOutcome.Completed ? TaskChangeCause.Completed :
                old.Outcome == TaskOutcome.Active && state.Outcome == TaskOutcome.Failed ? TaskChangeCause.Failed : TaskChangeCause.Updated;
            Notify(before, cause);
            return TaskWriteResult.Applied;
        }
        private static bool Valid(TaskSnapshot state)
        {
            if (state == null || !Enum.IsDefined(typeof(TaskOutcome), state.Outcome) || !Enum.IsDefined(typeof(TaskCondition), state.Condition)) return false;
            if (state.Outcome == TaskOutcome.Abandoned) return true;
            if (string.IsNullOrWhiteSpace(state.Title)) return false;
            return state.Outcome == TaskOutcome.Active ? !string.IsNullOrWhiteSpace(state.StageKey) && !string.IsNullOrWhiteSpace(state.NextStep) : !string.IsNullOrWhiteSpace(state.Result);
        }
        private bool Owns(Publisher writer) => writer != null && writer.Journal == this &&
            sources.TryGetValue(writer.Source.Id, out var current) && ReferenceEquals(current, writer.Source) && ReferenceEquals(current.Writer, writer);
        private TaskWriteResult ReleasePublisher(Publisher writer)
        {
            if (Guard.HasValue) return Guard.Value;
            if (!Owns(writer)) return TaskWriteResult.Unavailable;
            var before = ReadSnapshot(); writer.Source.Writer = null;
            Notify(before, TaskChangeCause.AvailabilityChanged); return TaskWriteResult.Applied;
        }
        public TaskWriteResult ReleaseScope(string scopeId)
        {
            if (Guard.HasValue) return Guard.Value;
            if (string.IsNullOrWhiteSpace(scopeId)) return TaskWriteResult.Invalid;
            var ids = new List<string>();
            foreach (var source in sources.Values) if (source.Scope == scopeId) ids.Add(source.Id);
            if (ids.Count == 0) return TaskWriteResult.Unchanged;
            var before = ReadSnapshot();
            foreach (var id in ids) { sources[id].Writer = null; sources.Remove(id); }
            var keys = new List<TaskKey>();
            foreach (var pair in entries) if (pair.Value.Source.Scope == scopeId) keys.Add(pair.Key);
            foreach (var key in keys) entries.Remove(key);
            Notify(before, TaskChangeCause.Removed); return TaskWriteResult.Applied;
        }
        private static bool Visible(Entry entry) => entry.State.Discovered && entry.State.Outcome != TaskOutcome.Abandoned;
        private TaskRecord Record(Entry entry) => new TaskRecord(entry.Key, entry.Source.Scope, entry.State,
            entry.Source.Writer != null, entry.Discovery, entry.Terminal, entry.Revision);
        public TaskJournalSnapshot ReadSnapshot()
        {
            var active = new List<TaskRecord>(); var completed = new List<TaskRecord>();
            foreach (var entry in entries.Values)
                if (Visible(entry)) (entry.State.Outcome == TaskOutcome.Active ? active : completed).Add(Record(entry));
            active.Sort((a, b) => a.DiscoveryOrder.CompareTo(b.DiscoveryOrder));
            completed.Sort((a, b) => b.TerminalOrder.CompareTo(a.TerminalOrder));
            return new TaskJournalSnapshot(revision, active.ToArray(), completed.ToArray());
        }
        public IDisposable ObserveWithSnapshot(Action<TaskJournalChange> observer, out TaskJournalSnapshot snapshot)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            var subscription = new Subscription(this, observer);
            if (!disposed) observers.Add(subscription);
            snapshot = ReadSnapshot(); return subscription;
        }
        private static bool Same(TaskRecord a, TaskRecord b) => a != null && b != null && a.State.Equals(b.State) &&
            a.SourceAvailable == b.SourceAvailable;
        private void Notify(TaskJournalSnapshot before, TaskChangeCause cause)
        {
            var candidate = ReadSnapshot();
            var keys = new HashSet<TaskKey>();
            foreach (var record in before.Active) keys.Add(record.Key);
            foreach (var record in before.Completed) keys.Add(record.Key);
            foreach (var record in candidate.Active) keys.Add(record.Key);
            foreach (var record in candidate.Completed) keys.Add(record.Key);
            var changed = new List<TaskKey>();
            foreach (var key in keys) if (!Same(before.Find(key), candidate.Find(key))) changed.Add(key);
            if (changed.Count == 0) return;
            revision++;
            foreach (var key in changed) if (entries.TryGetValue(key, out var entry)) entry.Revision = revision;
            var after = ReadSnapshot(); var records = new List<TaskRecordChange>();
            foreach (var key in changed) records.Add(new TaskRecordChange(key, before.Find(key), after.Find(key), cause));
            var change = new TaskJournalChange(before, after, records.ToArray());
            delivering = true;
            try
            {
                foreach (var subscription in observers.ToArray())
                    if (subscription.Active) try { subscription.Observer(change); } catch (Exception error) { Report(error); }
            }
            finally { delivering = false; }
        }
        private void Report(Exception error)
        {
            if (ObserverError == null) return;
            foreach (Action<Exception> observer in ObserverError.GetInvocationList()) try { observer(error); } catch { }
        }
        public void Dispose()
        {
            if (disposed || delivering) return;
            var before = ReadSnapshot();
            foreach (var source in sources.Values) source.Writer = null;
            sources.Clear(); entries.Clear(); disposed = true;
            Notify(before, TaskChangeCause.Removed);
            foreach (var observer in observers.ToArray()) observer.Dispose();
        }
    }
}
