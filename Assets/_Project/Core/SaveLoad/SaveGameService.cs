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
        private SaveRecoveryStore store;
        public SaveLoadResult LastLoad { get; private set; }
        public bool RecoveryNoticePending => store?.NoticePending ?? false;
        public void Configure(SaveRecoveryStore store) => this.store = store;
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
        { this.session = session; this.newSave = newSave; this.canWrite = canWrite; }
        public void Flush()
        {
            if (!canWrite || session == null || session.HasIntegrityFailure || session.IsCollecting || !session.IsDirty || store == null || store.RecoveryPending) return;
            try { store.Write(session.Capture()); session.MarkSaved(); }
            catch (Exception failure) { Debug.LogError("[SaveGameService] Failed to save: " + failure.Message); }
        }
        public void Release() { Flush(); session = null; newSave = null; canWrite = false; }
        [ContextMenu("Start New Save")] public void StartNewSave() => newSave?.Invoke();
        private void LateUpdate() => Flush();
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
        private void OnDestroy() => Flush();
    }
}
