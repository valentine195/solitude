namespace SOLITUDE.Containers.Views
{
    public interface IContainerView

    {
        ContainerUIType Type { get; }
        bool IsOpen { get; }

        void Open(IContainerSource source);
        void Close();

    }
}
