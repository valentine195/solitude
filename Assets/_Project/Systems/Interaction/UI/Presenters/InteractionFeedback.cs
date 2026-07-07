using UnityEngine;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.UI
{
    public static class InteractionFeedback
    {
        private static InteractionFeedbackView view;

        // Called once during UI bootstrap
        public static void Initialize(InteractionFeedbackView feedbackView)
        {
            view = feedbackView;

            if (view == null)
            {
                // Without this, a missing inspector reference means every
                // Handle() call silently no-ops for the rest of the game -
                // feedback text just never appears, with zero diagnostic
                // pointing at why.
                Debug.LogWarning($"{nameof(InteractionFeedback)}: initialized with a null view - feedback messages will not be shown.");
            }
        }

        public static void Handle(InteractionResult result)
        {
            if (result.IsSuccess)
            {
                // Optional: only show success if you want feedback noise
                if (!string.IsNullOrEmpty(result.Message))
                    view?.Show(result.Message, Color.green);

                return;
            }

            // Fail / Blocked / NoTarget all treated as feedback
            var message = result.Message;

            if (string.IsNullOrEmpty(message))
                message = "Cannot interact";

            view?.Show(message, Color.red);
        }
    }
}