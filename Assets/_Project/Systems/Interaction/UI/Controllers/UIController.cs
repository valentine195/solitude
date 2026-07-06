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
        }

        private void HandleFocusChanged(InteractionFocusChangedEvent evt)
        {
            if (evt.interactable == null)
            {
                interactionPrompt.Hide();
                return;
            }

            interactionPrompt.Show(evt.interactable.GetPrompt());
        }
    }
}