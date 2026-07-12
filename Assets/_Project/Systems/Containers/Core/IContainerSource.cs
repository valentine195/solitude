namespace SOLITUDE.Containers
{
    /// <summary>
    /// Implemented by any MonoBehaviour that owns a Container purely so it can
    /// be wired into the Inspector (Unity can't serialize an IContainer
    /// reference directly). The MonoBehaviour's only job is exposing the real
    /// Container - it doesn't need to re-implement IContainer's members itself.
    /// </summary>
    public interface IContainerSource
    {

        public string Label { get; }
        Container Container { get; }
    }
}