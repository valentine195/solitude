using UnityEngine;
using SOLITUDE.Features.Player;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.Interaction
{
    public class InteractableProximitySensor : MonoBehaviour
    {
        private IInteractable interactable;

        private void Awake()

        {

            interactable = GetComponentInParent<IInteractable>();

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("OnTriggerEnter2D fired");
            var player = other.GetComponent<PlayerInteractor>();
            if (player != null && interactable != null)
            {
                Debug.Log("Registering interactable");
                player.Register(interactable);
            } else
            {
                if (player == null)
                {
                    Debug.Log("Failed to register due to null player");
                } else
                {
                    Debug.Log("Failed to register due to null interactable");

                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Debug.Log("OnTriggerExit2D fired");
            var player = other.GetComponent<PlayerInteractor>();
            if (player != null && interactable != null)
            {
                Debug.Log("Unregistering interactable");
                player.Unregister(interactable);
            }
        }
    }
}