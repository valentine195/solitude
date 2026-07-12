namespace SOLITUDE.Containers.Views
{
    public interface IContainerView

    {
        ContainerUIType Type { get; }

        void Open(IContainerSource source);
        void Close();

    }
}