using System;
using System.Collections.Generic;

namespace SOLITUDE.Tasks
{
    public enum TaskOutcome { Active, Completed, Failed, Abandoned }
    public enum TaskWriteResult { Applied, Unchanged, Invalid, Conflict, Unavailable, Busy }
    public enum TaskChangeCause { Discovered, Updated, Completed, Failed, Removed, AvailabilityChanged, PinChanged }
    public enum TaskCondition { Unknown, Fault, Elevated, Recovering, Normal }

    public readonly struct TaskKey : IEquatable<TaskKey>
    {
        public string SourceId { get; }
        public string LocalTaskId { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(SourceId) && !string.IsNullOrWhiteSpace(LocalTaskId);
        public TaskKey(string sourceId, string localTaskId) { SourceId = sourceId; LocalTaskId = localTaskId; }
        public bool Equals(TaskKey other) => SourceId == other.SourceId && LocalTaskId == other.LocalTaskId;
        public override bool Equals(object obj) => obj is TaskKey other && Equals(other);
        public override int GetHashCode() => ((SourceId?.GetHashCode() ?? 0) * 397) ^ (LocalTaskId?.GetHashCode() ?? 0);
        public static bool operator ==(TaskKey a, TaskKey b) => a.Equals(b);
        public static bool operator !=(TaskKey a, TaskKey b) => !a.Equals(b);
    }

    // Full authoritative copy. Strings are immutable; no view or scene references are retained.
    public sealed class TaskSnapshot : IEquatable<TaskSnapshot>
    {
        public bool Discovered { get; }
        public TaskOutcome Outcome { get; }
        public string Title { get; }
        public string StageKey { get; }
        public string Location { get; }
        public TaskCondition Condition { get; }
        public string ConditionText { get; }
        public string NextStep { get; }
        public string Blocker { get; }
        public string Result { get; }
        public TaskSnapshot(bool discovered, TaskOutcome outcome, string title = null, string stageKey = null,
            string location = null, TaskCondition condition = TaskCondition.Unknown, string conditionText = null,
            string nextStep = null, string blocker = null, string result = null)
        {
            Discovered = discovered; Outcome = outcome; Title = title; StageKey = stageKey; Location = location;
            Condition = condition; ConditionText = conditionText; NextStep = nextStep; Blocker = blocker; Result = result;
        }
        public bool Equals(TaskSnapshot other) => other != null && Discovered == other.Discovered && Outcome == other.Outcome &&
            Title == other.Title && StageKey == other.StageKey && Location == other.Location && Condition == other.Condition &&
            ConditionText == other.ConditionText && NextStep == other.NextStep && Blocker == other.Blocker && Result == other.Result;
        public override bool Equals(object obj) => Equals(obj as TaskSnapshot);
        public override int GetHashCode() => Title?.GetHashCode() ?? 0;
    }

    public sealed class TaskRecord
    {
        public TaskKey Key { get; }
        public string ScopeId { get; }
        public TaskSnapshot State { get; }
        public bool SourceAvailable { get; }
        public bool Pinned { get; }
        public long DiscoveryOrder { get; }
        public long TerminalOrder { get; }
        public long Revision { get; }
        internal TaskRecord(TaskKey key, string scopeId, TaskSnapshot state, bool available, bool pinned,
            long discoveryOrder, long terminalOrder, long revision)
        { Key = key; ScopeId = scopeId; State = state; SourceAvailable = available; Pinned = pinned;
            DiscoveryOrder = discoveryOrder; TerminalOrder = terminalOrder; Revision = revision; }
    }

    public sealed class TaskJournalSnapshot
    {
        public long Revision { get; }
        public TaskKey? PinnedKey { get; }
        public IReadOnlyList<TaskRecord> Active { get; }
        public IReadOnlyList<TaskRecord> Completed { get; }
        internal TaskJournalSnapshot(long revision, TaskKey? pinned, TaskRecord[] active, TaskRecord[] completed)
        { Revision = revision; PinnedKey = pinned; Active = Array.AsReadOnly(active); Completed = Array.AsReadOnly(completed); }
        public TaskRecord Find(TaskKey key)
        {
            foreach (var row in Active) if (row.Key == key) return row;
            foreach (var row in Completed) if (row.Key == key) return row;
            return null;
        }
    }

    public sealed class TaskRecordChange
    {
        public TaskKey Key { get; }
        public TaskRecord Before { get; }
        public TaskRecord After { get; }
        public TaskChangeCause Cause { get; }
        internal TaskRecordChange(TaskKey key, TaskRecord before, TaskRecord after, TaskChangeCause cause)
        { Key = key; Before = before; After = after; Cause = cause; }
    }

    // One notification per accepted visible revision, including atomic multi-record pin/scope changes.
    public sealed class TaskJournalChange
    {
        public long Revision => After.Revision;
        public TaskJournalSnapshot Before { get; }
        public TaskJournalSnapshot After { get; }
        public IReadOnlyList<TaskRecordChange> Records { get; }
        internal TaskJournalChange(TaskJournalSnapshot before, TaskJournalSnapshot after, TaskRecordChange[] records)
        { Before = before; After = after; Records = Array.AsReadOnly(records); }
    }
}
