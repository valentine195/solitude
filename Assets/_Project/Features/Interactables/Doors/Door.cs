using UnityEngine;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Player;
using System.Collections;

namespace SOLITUDE.Features.Interactables
{
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Press E";
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Collider2D blocker;

        public string GetPrompt()
        {
            return promptText;
        }

        public bool CanInteract(PlayerInteractor player)
        {
            // Later you can add:
            // - locked state
            // - quest checks
            // - time of day
            return true;
        }

        public InteractionResult Interact(PlayerInteractor player)
        {
            Debug.Log("Door interacted by: " + player.name);
            if (isOpen)
            {
                SetOpened(false);
                return InteractionResult.Success("Door closed...");
            }
            return InteractionResult.Success("Door opened...");

            // Example future behavior:
            // SceneLoader.Load(...)
            // or teleport player
            // or play animation
        }

        private void SetOpened(bool opened)
        {
            sprite.enabled = !opened;
            blocker.enabled = !opened;
            isOpen = opened;
        }
        private void SetLocked(bool locked)
        {
            this.isLocked = locked;
        }

    }

}