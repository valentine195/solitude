namespace SOLITUDE.Core.Interaction
{
    /// <summary>
    /// Higher value = higher priority when multiple interactables overlap.
    /// </summary>
    public enum InteractionPriority
    {
        Low = 0,
        Default = 1,
        High = 2,
        Critical = 3
    }
}