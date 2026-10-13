namespace SOLITUDE.Vitals
{
    /// <summary>A detached input/output snapshot, never an authoritative value store.</summary>
    public readonly struct VitalsValues
    {
        public double Health { get; }
        public double Hunger { get; }
        public VitalsValues(double health, double hunger) { Health = health; Hunger = hunger; }
    }

    /// <summary>The only recovery history: remaining eligible scaled seconds after damage.</summary>
    public readonly struct VitalsRecoveryState
    {
        public double DelayRemaining { get; }
        public VitalsRecoveryState(double delayRemaining) { DelayRemaining = delayRemaining; }
    }

    public enum RecoveryReason { Unavailable, AtCeiling, Paused, Hungry, Delayed, Recovering }
    public enum VitalsRecoveryOutcome { Rejected, Applied, Unchanged }

    /// <summary>Prepared values and timing for the runtime to apply before publishing observers.</summary>
    public readonly struct VitalsRecoveryResult
    {
        public VitalsRecoveryOutcome Outcome { get; }
        public bool Accepted => Outcome != VitalsRecoveryOutcome.Rejected;
        public VitalsValues Values { get; }
        public VitalsRecoveryState State { get; }
        public RecoveryReason Reason { get; }
        public double RecoveryCeiling { get; }

        internal VitalsRecoveryResult(VitalsRecoveryOutcome outcome, VitalsValues values,
            VitalsRecoveryState state, RecoveryReason reason, double ceiling)
        { Outcome = outcome; Values = values; State = state; Reason = reason; RecoveryCeiling = ceiling; }
    }
}
