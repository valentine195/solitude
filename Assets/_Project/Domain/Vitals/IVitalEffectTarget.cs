namespace SOLITUDE.Vitals
{
    // Trusted composition adapter, not an arbitrary user callback/effects framework.
    // Reserve validates eligibility, revision and bounded values and blocks mutations.
    public interface IVitalEffectTarget
    {
        VitalUseOutcome TryReserve(VitalConsumableSpec effect, out IVitalEffectReservation reservation);
    }
    public interface IVitalEffectReservation
    {
        // Non-failing prepared field writes only: no observers, allocations, validation,
        // arbitrary callbacks or new effects. Called once inside the coordinated commit.
        void ApplySilently();
        // Releases the reservation; publishes only if applied. Observer faults must be
        // isolated by the adapter. Always called, including rejected inventory commits.
        void Complete();
    }
}
