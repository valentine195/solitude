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
            Debug.Log("[InteractionFeedback] Initialized with view: " + (view != null ? view.name : "null"));
        }

        public static void Handle(InteractionResult result)
        {

            Debug.Log($"[InteractionFeedback] Handling result: IsSuccess={result.IsSuccess}, Message='{result.Message}'");

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