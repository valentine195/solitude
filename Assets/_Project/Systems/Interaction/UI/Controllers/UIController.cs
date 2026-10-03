using UnityEngine;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.UI
{
    /// <summary>
    /// Listens to interaction events and drives UI views.
    /// Fully decoupled from gameplay systems.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        private IInteractable focused;
        private string shownPrompt;

        [Header("Views")]
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private InteractionFeedbackView feedbackView;

        private void Awake()

        {

            InteractionFeedback.Initialize(feedbackView);

        }

        private void OnEnable()
        {
            InteractionEventBus.OnFocusChanged += HandleFocusChanged;
        }

        private void OnDisable()
        {
            InteractionEventBus.OnFocusChanged -= HandleFocusChanged;
            focused = null; shownPrompt = null;
            if (interactionPrompt != null) interactionPrompt.Hide();
        }

        private void HandleFocusChanged(InteractionFocusChangedEvent evt)
        {
            focused = evt.interactable;
            RefreshPrompt();
        }

        private void LateUpdate() => RefreshPrompt();

        private void RefreshPrompt()
        {
            if (focused == null || (focused is Object target && target == null))
            {
                focused = null; shownPrompt = null;
                if (interactionPrompt != null) interactionPrompt.Hide();
                return;
            }
            string current = focused.GetPrompt();
            if (current == shownPrompt || interactionPrompt == null) return;
            shownPrompt = current;
            interactionPrompt.Show(current);
        }
    }
}
