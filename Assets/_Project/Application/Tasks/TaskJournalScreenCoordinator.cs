using System;

namespace SOLITUDE.Application
{
    public interface ITaskJournalScreenDisplay { void Show(); void Hide(); }
    public sealed class TaskJournalScreenCoordinator : IDisposable
    {
        private readonly InputPolicyCoordinator policy;
        private readonly PauseCoordinator pause;
        private readonly ITaskJournalScreenDisplay display;
        private IDisposable screenLease, pauseLease;
        private bool disposed, opening, closing;
        public bool IsOpen => screenLease != null;
        public event Action<Exception> PresentationError;
        public TaskJournalScreenCoordinator(InputPolicyCoordinator policy, PauseCoordinator pause, ITaskJournalScreenDisplay display)
        { this.policy = policy; this.pause = pause; this.display = display; policy.Changed += PolicyChanged; }
        public OpenResult Toggle() { if (IsOpen) { Close(); return OpenResult.Closed; } return Open(); }
        public OpenResult Open()
        {
            if (disposed || display == null || opening || closing) return OpenResult.Unavailable;
            if (IsOpen) return OpenResult.Opened;
            opening = true;
            try
            {
                screenLease = policy.AcquirePrimaryScreen();
                if (screenLease == null) return OpenResult.Unavailable;
                pauseLease = pause.Acquire();
                if (!policy.Ready) { Close(); return OpenResult.Unavailable; }
                display.Show();
                if (!policy.Ready) Close();
                return IsOpen ? OpenResult.Opened : OpenResult.Unavailable;
            }
            catch (Exception error) { Report(error); Close(); return OpenResult.BindingFailed; }
            finally { opening = false; }
        }
        private void PolicyChanged() { if (!opening && !closing && IsOpen && !policy.Ready) Close(); }
        public bool Back() { if (!IsOpen) return false; Close(); return true; }
        public void Close()
        {
            if (closing || (screenLease == null && pauseLease == null)) return;
            closing = true;
            var screen = screenLease; var ownedPause = pauseLease; screenLease = pauseLease = null;
            try { try { display?.Hide(); } catch (Exception error) { Report(error); } }
            finally
            {
                // Keep primary gating until the journal's pause is gone.
                try { ownedPause?.Dispose(); }
                finally { try { screen?.Dispose(); } finally { closing = false; } }
            }
        }
        private void Report(Exception error)
        {
            if (PresentationError == null) return;
            foreach (Action<Exception> observer in PresentationError.GetInvocationList()) try { observer(error); } catch { }
        }
        public void Dispose() { if (disposed) return; disposed = true; policy.Changed -= PolicyChanged; Close(); }
    }
}
