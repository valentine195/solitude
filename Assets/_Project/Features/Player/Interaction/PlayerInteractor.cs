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

        private SceneInteractionEvents events;
        private InteractionFeedbackService feedback;
        private bool inputAllowed;
        public void Bind(SceneInteractionEvents source, InteractionFeedbackService service)
        { Release(); events = source; feedback = service; }
        public void Release()
        {
            SetInputAllowed(false);
            events = null; feedback = null;
        }
        private void OnDestroy() => Release();
        public void SetInputAllowed(bool allowed)
        {
            inputAllowed = allowed;
            if (!allowed) ClearFocus();
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

                events?.Publish(
                    new InteractionFocusChangedEvent(target)
                );
            }
        }

        public void ClearFocus()
        {
            if (currentTarget == null) return;
            currentTarget = null; events?.Publish(new InteractionFocusChangedEvent(null));
        }
        public void RebuildProximityAfterRelocation()
        {
            ClearFocus(); nearby.Clear();
            var playerColliders = GetComponents<Collider2D>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var sensor in root.GetComponentsInChildren<InteractableProximitySensor>())
                {
                    if (!sensor.isActiveAndEnabled) continue;
                    var target = sensor.GetComponentInParent<IInteractable>();
                    if (target == null) continue;
                    foreach (var trigger in sensor.GetComponents<Collider2D>())
                        if (trigger.enabled && trigger.isTrigger)
                            foreach (var player in playerColliders)
                                if (player.enabled && Physics2D.Distance(trigger, player).isOverlapped) nearby.Add(target);
                }
        }
        private void OnDisable() => ClearFocus();

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
                feedback?.Handle(InteractionResult.Blocked());
                return;
            }

            var result = currentTarget.Interact(this);

            feedback?.Handle(result);

            // "Triggered" means an interact attempt actually reached a
            // target that allowed it - a blocked attempt above never
            // publishes this, so listeners (achievements, quest tracking,
            // etc.) can treat Triggered as "something really happened"
            // rather than "the button was pressed".
            events?.Publish(
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