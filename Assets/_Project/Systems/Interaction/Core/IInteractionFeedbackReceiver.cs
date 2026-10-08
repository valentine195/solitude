using SOLITUDE.Core.UI;

namespace SOLITUDE.Core.Interaction
{
    public interface IInteractionFeedbackReceiver
    {
        void BindFeedback(InteractionFeedbackService feedback);
        void ReleaseFeedback();
    }
}
