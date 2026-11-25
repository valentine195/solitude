namespace SOLITUDE.Containers
{
    /// <summary>Inspector-facing owner adapter exposing only a registered reader.</summary>
    public interface IContainerSource
    {
        string Label { get; }
        IContainerReader Container { get; }
    }
}
