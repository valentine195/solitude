namespace SOLITUDE.Restoration
{
    // Scene-owned inspection state; never included in production save data.
    public sealed class ScrubberEncounterState
    {
        public string OperatingStatus => "OFFLINE";
        public string FuseStatus => "BLOWN";
        public string AirflowStatus => "STOPPED";
        public string RoomCondition => "ELEVATED";
        public string Objective => "Replace the scrubber fuse";
        public string Fault => "Blown fuse";
    }
}
