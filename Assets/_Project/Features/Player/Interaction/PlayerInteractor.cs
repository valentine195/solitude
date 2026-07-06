using System.Collections.Generic;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.Systems;
using SOLITUDE.Core.UI;
using UnityEngine;

namespace SOLITUDE.Features.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactRange = 2f;

        [SerializeField] private bool debugInteraction = false;

        private readonly HashSet<IInteractable> nearby = new();

        private IInteractable currentTarget;

        void Update()
        {
            IInteractable target = GetBestFromCache();

            if (target != currentTarget)
            {
                currentTarget = target;

                if (debugInteraction)
                {
                    string name = target == null
                        ? "null"
                        : (target as MonoBehaviour)?.gameObject.name ?? target.GetType().Name;

                    Debug.Log($"[Interactor] Focus -> {name}");
                }

                InteractionEventBus.Publish(
                    new InteractionFocusChangedEvent(target)
                );
            }

            if (InputRouter.Instance != null &&
                InputRouter.Instance.InteractPressed)
            {
                TryInteract();
            }
        }
        private void TryInteract()

        {

            if (currentTarget == null)

                return;

            var result = currentTarget.Interact(this);

            InteractionFeedback.Handle(result);

            InteractionEventBus.Publish(
                new InteractionTriggeredEvent(currentTarget, this)
            );

        }

        private IInteractable GetBestFromCache()
        {
            IInteractable best = null;
            float bestDist = float.MaxValue;

            foreach (var i in nearby)
            {
                if (i == null) continue;

                var mono = i as MonoBehaviour;
                if (mono == null) continue;

                float dist = (mono.transform.position - transform.position).sqrMagnitude;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }

            return best;
        }

        // Called by interactables
        public void Register(IInteractable interactable)
        {
            if (debugInteraction)
            {
                string name = (interactable as MonoBehaviour)?.gameObject.name ?? interactable.GetType().Name;
                Debug.Log($"[Interactor] Registering interactable -> {name}");
            }

            nearby.Add(interactable);
        }

        public void Unregister(IInteractable interactable)
        {
            if (debugInteraction)
            {
                string name = (interactable as MonoBehaviour)?.gameObject.name ?? interactable.GetType().Name;
                Debug.Log($"[Interactor] Unregistering interactable -> {name}");
            }

            nearby.Remove(interactable);
        }
    }
}