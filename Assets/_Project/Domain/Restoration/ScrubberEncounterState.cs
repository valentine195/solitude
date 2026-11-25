using System;
using SOLITUDE.Containers;
namespace SOLITUDE.Restoration
{
    // Scene-owned state; never included in production save data.
    public sealed class ScrubberEncounterState
    {
        private readonly RestorableSystemState repair = new RestorableSystemState();
        private double recoverySeconds;
        public bool FuseInstalled => repair.IsRestored;
        public bool Running { get; private set; }
        public bool Operational => Running && recoverySeconds >= 5;
        public double RecoverySeconds => recoverySeconds;
        public event Action Changed;
        public ScrubberEncounterState() { repair.Changed += Publish; }
        public string OperatingStatus => Running ? "RUNNING" : "OFFLINE";
        public string FuseStatus => FuseInstalled ? "INTACT" : "BLOWN";
        public string AirflowStatus => Running ? "ACTIVE" : "STOPPED";
        public string RoomCondition => Operational ? "NORMAL" : Running ? "RECOVERING" : "ELEVATED";
        public string Objective => Operational ? "Room atmosphere restored — complete" : Running ? "Wait for room air to recover" : FuseInstalled ? "Restart the scrubber" : "Replace the scrubber fuse";
        public string Fault => FuseInstalled ? "None" : "Blown fuse";
        public string RestartFeedback => Running ? "Scrubber already running." : "Restart failed: blown fuse. Replace fuse first.";
        public RestorationOutcome Install(ContainerCommandService commands, IContainerReader inventory, string itemId) => repair.TryRestore(commands,inventory,itemId);
        public bool TryRestart()
        {
            if(!FuseInstalled || Running) return false;
            Running=true; Publish(); return true;
        }
        public void Advance(float activeSeconds)
        {
            if(float.IsNaN(activeSeconds) || float.IsInfinity(activeSeconds) || activeSeconds<0) throw new ArgumentOutOfRangeException(nameof(activeSeconds));
            if(!Running || Operational) return;
            recoverySeconds=Math.Min(5,recoverySeconds+activeSeconds);
            if(Operational) Publish();
        }
        private void Publish()
        {
            if(Changed==null) return;
            foreach(Action observer in Changed.GetInvocationList())
                try { observer(); } catch { /* Observers cannot undo committed state or prevent other consumers. */ }
        }
    }
}
