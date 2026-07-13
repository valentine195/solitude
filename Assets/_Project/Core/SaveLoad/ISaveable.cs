namespace SOLITUDE.SaveLoad
{
    /// <summary>
    /// A small, serialization-agnostic seam for a component that can capture
    /// and restore its own data. The save service owns file IO and record lookup.
    /// </summary>
    public interface ISaveable<TData>
    {
        TData CaptureState();
        void RestoreState(TData state);
    }
}
