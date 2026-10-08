using SOLITUDE.Player;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using UnityEngine;

namespace SOLITUDE.World.SolitudeStart
{
    public sealed class SolitudeStartCompletion : MonoBehaviour, IInteractionFeedbackReceiver
    {
        [SerializeField] private SolitudeSlidingDoor door;
        private bool completed;
        private InteractionFeedbackService feedback;
        public void BindFeedback(InteractionFeedbackService service) => feedback = service;
        public void ReleaseFeedback() => feedback = null;
        private void OnDestroy() => ReleaseFeedback();
        public void Configure(SolitudeSlidingDoor target) => door = target;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || door == null || !door.IsOpen || other.GetComponent<PlayerInteractor>() == null) return;
            completed = true;
            feedback?.Handle(InteractionResult.Success("Access granted"));
            Debug.Log("SOLITUDE start: access granted. The next area is not built yet.");
        }
    }
}
