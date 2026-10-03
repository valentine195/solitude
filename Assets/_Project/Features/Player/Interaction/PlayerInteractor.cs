using System.Collections.Generic;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Input;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.Systems;
using SOLITUDE.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SOLITUDE.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactRange = 2f;

        [SerializeField] private bool debugInteraction = false;

        private readonly HashSet<IInteractable> nearby = new();

        private IInteractable currentTarget;

        private bool inputAllowed;
        public void SetInputAllowed(bool allowed)
        {
            inputAllowed = allowed;
            if (!allowed && currentTarget != null)
            { currentTarget = null; InteractionEventBus.Publish(new InteractionFocusChangedEvent(null)); }
        }
        void Update()
        {
            if (!inputAllowed) return;
            IInteractable target = GetBestFromCache();


            if (target != currentTarget)
            {
                if (debugInteraction)
                {
                    string name = target == null
                        ? "null"
                        : (target as MonoBehaviour)?.gameObject.name ?? target.GetType().Name;
                    string cname = target == null
                        ? "null"
                        : (currentTarget as MonoBehaviour)?.gameObject.name ?? currentTarget.GetType().Name;

                    Debug.Log($"[Interactor] Focus change -> {name} -> {cname}");
                }
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
        }

        private void OnDisable()
        {
            // Update() stops running while disabled (e.g. during a
            // cutscene), so without this the last-focused prompt would
            // stay on screen with no further FocusChanged event to hide
            // it. Explicitly clear focus so the UI hides the prompt, and
            // let OnEnable's next Update() naturally recompute it.
            if (currentTarget != null)
            {
                currentTarget = null;
                InteractionEventBus.Publish(new InteractionFocusChangedEvent(null));
            }

        }

        public void TryInteract()
        {
            if (!inputAllowed) return;
            if (currentTarget == null)
            {
                Debug.Log("[PlayerInteractor] No target");
                return;
            }

            // CanInteract exists specifically so an interactable can be
            // temporarily blocked (locked door, quest gate, etc.) - it must
            // be checked here, not just declared on the interface, or a
            // locked door opens anyway the moment CanInteract is ever wired
            // up to return false.
            if (!currentTarget.CanInteract(this))
            {
                InteractionFeedback.Handle(InteractionResult.Blocked());
                return;
            }

            var result = currentTarget.Interact(this);

            InteractionFeedback.Handle(result);

            // "Triggered" means an interact attempt actually reached a
            // target that allowed it - a blocked attempt above never
            // publishes this, so listeners (achievements, quest tracking,
            // etc.) can treat Triggered as "something really happened"
            // rather than "the button was pressed".
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