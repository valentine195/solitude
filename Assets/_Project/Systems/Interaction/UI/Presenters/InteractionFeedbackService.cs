using System;
using UnityEngine;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.UI
{
    public sealed class InteractionFeedbackService : IDisposable
    {
        private InteractionFeedbackView view;
        public InteractionFeedbackService(InteractionFeedbackView feedbackView)
        {
            view = feedbackView;
            if (view == null) Debug.LogWarning("InteractionFeedbackService: initialized with a null view - feedback messages will not be shown.");
        }
        public void Handle(InteractionResult result)
        {
            if (view == null) return;
            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.Message)) view.Show(result.Message, Color.green);
                return;
            }
            view.Show(string.IsNullOrEmpty(result.Message) ? "Cannot interact" : result.Message, Color.red);
        }
        public void Clear() { if (view != null) view.Hide(); }
        public void Dispose() { Clear(); view = null; }
    }
}
