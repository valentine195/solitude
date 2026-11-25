using System;
using System.IO;
using UnityEngine;
using SOLITUDE.Items;
using SOLITUDE.Application.Persistence;
namespace SOLITUDE.SaveLoad
{
    /// <summary>Injected complete-file persistence and Unity lifecycle host.</summary>
    public class SaveGameService : MonoBehaviour
    {
        private ContainerSaveSession session;
        private bool canWrite;
        private Action newSave;
        private Action captureLivingState;
        private SaveRecoveryStore store;
        private const double AutosaveDelaySeconds = .25;
        private double autosaveAt = double.NaN;
        public string LastSaveError { get; private set; }
        public SaveLoadResult LastLoad { get; private set; }
        public bool RecoveryNoticePending => store?.NoticePending ?? false;
        public void Configure(SaveRecoveryStore store) => this.store = store;
        public void ConfigureCapture(Action capture) => captureLivingState = capture;
        public SaveGameData Load(int seed, IItemCatalog catalog, out string error)
        {
            if (store == null)
            {
                string directory = UnityEngine.Application.persistentDataPath;
#if UNITY_EDITOR || SOLITUDE_VERIFICATION
                var args = System.Environment.GetCommandLineArgs();
                int index = System.Array.IndexOf(args, "-solitudeSaveDirectory");
                if (index >= 0 && index + 1 < args.Length) directory = args[index + 1];
#endif
                store = new SaveRecoveryStore(Path.Combine(directory, "solitude-save.json"), new PhysicalSaveFiles(), new SaveJsonCodec(catalog));
            }
            LastLoad = store.Load(seed); error = LastLoad.Diagnostic;
            return LastLoad.Data;
        }
        public void BeginNewSave() { store?.CancelPendingRecovery(); if (store?.NoticePending == true) store.AcknowledgeNotice(); }
        public void CompleteRecovery() => store?.CompleteRecovery();
        public void RejectRestore(string diagnostic) => LastLoad = new SaveLoadResult(SaveLoadStatus.BlockedIncompatible, diagnostic: diagnostic);
        public void AcknowledgeRecoveryNotice() => store?.AcknowledgeNotice();
        public void Initialize(ContainerSaveSession session, Action newSave, bool canWrite = true)
        { this.session = session; this.newSave = newSave; this.canWrite = canWrite; autosaveAt = double.NaN; retryAt = 0; LastSaveError = null; }
        public void Flush() => TryFlush();
        public bool TryFlush() => Flush(true);
        private bool Flush(bool checkpoint)
        {
            if (!canWrite || session == null || session.HasIntegrityFailure || session.IsCollecting || store == null || store.RecoveryPending)
            { LastSaveError = "Save session is unavailable or awaiting recovery."; return false; }
            if (!checkpoint && !session.IsDirty) return true;
            try
            {
                captureLivingState?.Invoke();
                if (session.IsDirty) { store.Write(session.Capture()); session.MarkSaved(); }
                autosaveAt = double.NaN; retryAt = 0; LastSaveError = null;
                return true;
            }
            catch (Exception failure)
            {
                LastSaveError = failure.Message;
                Debug.LogError("[SaveGameService] Failed to save: " + failure.Message);
                return false;
            }
        }
        public void Release() { Flush(); session = null; newSave = null; captureLivingState = null; canWrite = false; }
        [ContextMenu("Start New Save")] public void StartNewSave() => newSave?.Invoke();
        private void LateUpdate() => Autosave(Time.realtimeSinceStartupAsDouble);
        private void Autosave(double now)
        {
            if (session == null || !session.IsDirty) { autosaveAt = double.NaN; return; }
            if (double.IsNaN(autosaveAt)) autosaveAt = now + AutosaveDelaySeconds;
            if (!session.RequiresImmediateSave && now < autosaveAt) return;
            // Bound retries as well as normal writes; keep dirty/urgent state on failure.
            if (now < retryAt) return;
            if (!Flush(false)) retryAt = now + AutosaveDelaySeconds;
            else retryAt = 0;
        }
        private double retryAt;
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
        private void OnDestroy() => Flush();
    }
}
