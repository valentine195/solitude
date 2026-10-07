using System;

namespace SOLITUDE.Application
{
    public interface IScrubberScreenDisplay
    {
        void Show();
        void Hide();
    }

    public sealed class ScrubberScreenCoordinator : IDisposable
    {
        private readonly InputPolicyCoordinator policy;
        private readonly IScrubberScreenDisplay display;
        private IDisposable lease;
        private bool disposed;
        public bool IsOpen => lease != null;
        public ScrubberScreenCoordinator(InputPolicyCoordinator policy, IScrubberScreenDisplay display)
        { this.policy = policy; this.display = display; }
        public OpenResult Open()
        {
            if (disposed) return OpenResult.Unavailable;
            if (IsOpen) return OpenResult.Opened;
            lease = policy.AcquirePrimaryScreen();
            if (lease == null) return OpenResult.Unavailable;
            try { display.Show(); return IsOpen ? OpenResult.Opened : OpenResult.Unavailable; }
            catch { Close(); return OpenResult.BindingFailed; }
        }
        public bool Back()
        {
            if (!IsOpen) return false;
            Close(); return true;
        }
        public void Close()
        {
            if (lease == null) return;
            var owned = lease;
            lease = null;
            try { display.Hide(); }
            finally { owned.Dispose(); }
        }
        public void Dispose() { if (disposed) return; disposed = true; Close(); }
    }
}
