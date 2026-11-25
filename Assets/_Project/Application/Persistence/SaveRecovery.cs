using System;
using SOLITUDE.SaveLoad;
namespace SOLITUDE.Application.Persistence
{
    public enum SaveLoadStatus { Fresh, LoadedPrimary, RecoveredBackup, BlockedCorrupt, BlockedIncompatible, IOFailure }
    public sealed class SaveCandidateException : Exception
    {
        public bool Incompatible { get; }
        public SaveCandidateException(string message, bool incompatible = false) : base(message) => Incompatible = incompatible;
    }
    public interface ISnapshotCodec { SaveGameData Decode(string json); string Encode(SaveGameData data); }
    public interface ISaveFiles
    {
        bool Exists(string path);
        string Read(string path);
        void WriteDurable(string path, string text);
        void Replace(string source, string destination, string backup);
        void Move(string source, string destination);
        void Delete(string path);
    }
    public sealed class SaveLoadResult
    {
        public SaveLoadStatus Status { get; }
        public SaveGameData Data { get; }
        public string Diagnostic { get; }
        public bool Success => Data != null;
        public SaveLoadResult(SaveLoadStatus status, SaveGameData data = null, string diagnostic = null)
        { Status = status; Data = data; Diagnostic = diagnostic; }
    }
    /// <summary>Complete-snapshot selection; no Unity, scene, catalog or filesystem discovery.</summary>
    public sealed class SaveRecoveryStore
    {
        private readonly ISaveFiles files;
        private readonly ISnapshotCodec codec;
        public string Path { get; }
        private string pending;
        public bool RecoveryPending => pending != null;
        public bool NoticePending => files.Exists(Path + ".recovery");
        public SaveRecoveryStore(string path, ISaveFiles files, ISnapshotCodec codec)
        { Path = path ?? throw new ArgumentNullException(nameof(path)); this.files = files; this.codec = codec; }
        public SaveLoadResult Load(int seed)
        {
            pending = null;
            try
            {
                bool primary = files.Exists(Path), backup = files.Exists(Path + ".bak");
                if (primary)
                {
                    try { return new SaveLoadResult(SaveLoadStatus.LoadedPrimary, codec.Decode(files.Read(Path))); }
                    catch (SaveCandidateException error)
                    { if (error.Incompatible) return new SaveLoadResult(SaveLoadStatus.BlockedIncompatible, diagnostic: error.Message); }
                }
                if (backup)
                {
                    try
                    {
                        var text = files.Read(Path + ".bak"); var data = codec.Decode(text); pending = text;
                        return new SaveLoadResult(SaveLoadStatus.RecoveredBackup, data, "Restored the previous complete backup; recent progress may be missing.");
                    }
                    catch (SaveCandidateException error)
                    { return new SaveLoadResult(error.Incompatible ? SaveLoadStatus.BlockedIncompatible : SaveLoadStatus.BlockedCorrupt, diagnostic: error.Message); }
                }
                if (primary || files.Exists(Path + ".tmp"))
                    return new SaveLoadResult(SaveLoadStatus.BlockedCorrupt, diagnostic: "No valid committed save or backup exists. Files were preserved.");
                return new SaveLoadResult(SaveLoadStatus.Fresh, new SaveGameData { worldSeed = seed });
            }
            catch (Exception error) { return new SaveLoadResult(SaveLoadStatus.IOFailure, diagnostic: error.Message); }
        }
        public void CompleteRecovery()
        {
            if (pending == null) return;
            // Write the receipt first so an interrupted promotion cannot lose the user notice.
            files.WriteDurable(Path + ".recovery", "Previous backup restored; recent progress may be missing.");
            files.WriteDurable(Path + ".tmp", pending);
            if (files.Exists(Path))
                files.Replace(Path + ".tmp", Path, Path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            else files.Move(Path + ".tmp", Path);
            pending = null;
        }
        public void CancelPendingRecovery() => pending = null;
        public void AcknowledgeNotice() { if (NoticePending) files.Delete(Path + ".recovery"); }
        public void Write(SaveGameData data)
        {
            if (RecoveryPending) throw new InvalidOperationException("Recovery must finish before saving.");
            string text = codec.Encode(data); codec.Decode(text);
            files.WriteDurable(Path + ".tmp", text);
            if (files.Exists(Path))
            {
                // An explicit new save may replace a blocked candidate. Preserve the
                // last complete backup instead of rotating invalid bytes into it.
                string previous = Path + ".bak";
                try { codec.Decode(files.Read(Path)); }
                catch (SaveCandidateException) { previous = Path + ".corrupt-" + Guid.NewGuid().ToString("N"); }
                files.Replace(Path + ".tmp", Path, previous);
            }
            else files.Move(Path + ".tmp", Path);
        }
    }
}
