using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;

namespace SOLITUDE.Core.Events
{
    public struct InteractionFocusChangedEvent
    {
        public IInteractable interactable;

        public InteractionFocusChangedEvent(IInteractable interactable)
        {
            this.interactable = interactable;
        }
    }

    public struct InteractionTriggeredEvent
    {
        public IInteractable interactable;
        public PlayerInteractor player;

        public InteractionTriggeredEvent(IInteractable interactable, PlayerInteractor player)
        {
            this.interactable = interactable;
            this.player = player;
        }
    }
}