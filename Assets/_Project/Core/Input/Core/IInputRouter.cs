
namespace SOLITUDE.Core.Input
{
    public interface IInputRouter
    {
        InputMode Mode { get; }
        bool ActiveInMode(InputMode mode);
        void Enable();
        void Disable();
    }
}