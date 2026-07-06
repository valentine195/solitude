namespace SOLITUDE.Modals
{
    public interface IModalView

    {

        void Open();

        void Close();

        void Toggle();

        bool IsOpen { get; }

    }

}