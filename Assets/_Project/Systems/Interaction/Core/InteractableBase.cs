using UnityEngine;
using SOLITUDE.Features.Player;

namespace SOLITUDE.Core.Interaction
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string prompt = "Interact";

        public virtual string GetPrompt() => prompt;

        public virtual bool CanInteract(PlayerInteractor player) => true;

        public abstract InteractionResult Interact(PlayerInteractor player);
    }
}