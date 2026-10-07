using System;
using SOLITUDE.Containers;
namespace SOLITUDE.Restoration
{
    // Scene-owned state; never included in production save data.
    public sealed class ScrubberEncounterState
    {
        private readonly RestorableSystemState repair = new RestorableSystemState();
        public bool FuseInstalled => repair.IsRestored;
        public event Action Changed { add => repair.Changed += value; remove => repair.Changed -= value; }
        public string OperatingStatus => "OFFLINE";
        public string FuseStatus => FuseInstalled ? "INTACT" : "BLOWN";
        public string AirflowStatus => "STOPPED";
        public string RoomCondition => "ELEVATED";
        public string Objective => FuseInstalled ? "Restart the scrubber" : "Replace the scrubber fuse";
        public string Fault => FuseInstalled ? "None" : "Blown fuse";
        public string RestartFeedback => FuseInstalled ? "Restart unavailable until recovery is connected." : "Restart failed: blown fuse. Replace fuse first.";
        public RestorationOutcome Install(ContainerCommandService commands, IContainerReader inventory, string itemId) => repair.TryRestore(commands,inventory,itemId);
    }
}
