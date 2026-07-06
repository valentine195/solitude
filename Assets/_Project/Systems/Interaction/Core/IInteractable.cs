using UnityEngine;
using SOLITUDE.Features.Player;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.Interaction
{
    public interface IInteractable
    {
        InteractionResult Interact(PlayerInteractor player);
        string GetPrompt();
        bool CanInteract(PlayerInteractor player);
    }

}