namespace SOLITUDE.Items
{
    public enum ItemActionSlot { Primary, Secondary, Contextual }

    // Immutable configuration crosses the Unity/pure runtime boundary.
    public interface IItemActionConfiguration { }

    public interface IItemActionContext
    {
        object Actor { get; }
        string ItemId { get; }
        ItemActionSlot Slot { get; }
        float DirectionX { get; }
        float DirectionY { get; }
    }

    // The actor owns each execution, never its shared definition asset.
    public interface IItemActionExecution
    {
        bool IsComplete { get; }
        void Advance(double gameplaySeconds);
        void Interrupt();
    }
}
