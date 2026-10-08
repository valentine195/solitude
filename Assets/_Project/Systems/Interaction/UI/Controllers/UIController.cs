using UnityEngine;
using SOLITUDE.Core.Events;
using SOLITUDE.Core.Interaction;

namespace SOLITUDE.Core.UI
{
    public class UIController : MonoBehaviour
    {
        private IInteractable focused;
        private string shownPrompt;
        private SceneInteractionEvents events;
        private InteractionFeedbackService feedback;
        private bool listening;

        [Header("Views")]
        [SerializeField] private InteractionPromptView interactionPrompt;
        [SerializeField] private InteractionFeedbackView feedbackView;
        public InteractionFeedbackView FeedbackView => feedbackView;

        public void ValidateAuthoring()
        {
            if (interactionPrompt == null || feedbackView == null ||
                interactionPrompt.gameObject.scene != gameObject.scene || feedbackView.gameObject.scene != gameObject.scene)
                throw new System.InvalidOperationException("Interaction UI views are missing or belong to another scene.");
            interactionPrompt.ValidateAuthoring(); feedbackView.ValidateAuthoring();
        }
        public void Bind(SceneInteractionEvents source, InteractionFeedbackService service)
        { Release(); events = source; feedback = service; Attach(); }
        private void Attach()
        {
            if (listening || events == null || !isActiveAndEnabled) return;
            events.FocusChanged += HandleFocusChanged; listening = true;
            focused = events.CurrentFocus; RefreshPrompt();
        }
        private void Detach()
        {
            if (listening) events.FocusChanged -= HandleFocusChanged;
            listening = false; focused = null; shownPrompt = null;
            if (interactionPrompt != null) interactionPrompt.Hide();
            feedback?.Clear();
        }
        public void Release() { Detach(); events = null; feedback = null; }
        private void OnEnable() => Attach();
        private void OnDisable() => Detach();
        private void OnDestroy() => Release();
        private void HandleFocusChanged(InteractionFocusChangedEvent evt)
        { focused = evt.interactable; RefreshPrompt(); }
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
            if (interactionPrompt == null) return;
            interactionPrompt.SetTarget((focused as MonoBehaviour)?.transform);
            if (current == shownPrompt) return;
            shownPrompt = current; interactionPrompt.Show(current);
        }
    }
}
