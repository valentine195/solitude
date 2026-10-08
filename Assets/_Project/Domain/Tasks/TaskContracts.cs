using System;

namespace SOLITUDE.Tasks
{
    public interface ITaskJournalReader
    {
        TaskJournalSnapshot ReadSnapshot();
        IDisposable ObserveWithSnapshot(Action<TaskJournalChange> observer, out TaskJournalSnapshot snapshot);
    }
    public interface ITaskPinCommands { TaskWriteResult SetPinned(TaskKey? key); }
    public interface ITaskPublisher
    {
        TaskWriteResult Publish(string localTaskId, TaskSnapshot snapshot);
        TaskWriteResult Release();
    }
}
