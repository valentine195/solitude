using System.Collections.Generic;
using UnityEngine;
using SOLITUDE.Features.Player;

namespace SOLITUDE.Core.Interaction
{
    public class InteractableProximitySensor : MonoBehaviour
    {
        private IInteractable interactable;

        // Tracks every player currently registered with this interactable so
        // OnDestroy can proactively unregister them. Without this, an
        // interactable that self-destructs (Destroy(gameObject) on pickup,
        // on being broken, etc.) never fires OnTriggerExit2D, and would
        // otherwise sit in PlayerInteractor.nearby as a dead entry for the
        // rest of the play session. This way cleanup doesn't depend on every
        // IInteractable implementation remembering to unregister itself.
        private readonly List<PlayerInteractor> registeredWith = new();

        private void Awake()
        {
            interactable = GetComponentInParent<IInteractable>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerInteractor>();
            if (player == null || interactable == null) return;

            player.Register(interactable);

            if (!registeredWith.Contains(player))
                registeredWith.Add(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerInteractor>();
            if (player == null || interactable == null) return;

            player.Unregister(interactable);
            registeredWith.Remove(player);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < registeredWith.Count; i++)
            {
                if (registeredWith[i] != null)
                    registeredWith[i].Unregister(interactable);
            }

            registeredWith.Clear();
        }
    }
}