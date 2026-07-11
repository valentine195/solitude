using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Containers;
using SOLITUDE.Player;

namespace SOLITUDE.Features.Interactables
{
    public class Locker : MonoBehaviour, IInteractable
    {

        [SerializeField] private ContainerModalView view;
        public string GetPrompt() => "Open locker";

        public bool CanInteract(PlayerInteractor player) => true;

        public InteractionResult Interact(PlayerInteractor player)

        {

            view.Toggle();
            return InteractionResult.Success();
        }
    }
}