using SOLITUDE.Player;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using UnityEngine;

namespace SOLITUDE.World.SolitudeStart
{
    public sealed class SolitudeStartCompletion : MonoBehaviour
    {
        [SerializeField] private SolitudeSlidingDoor door;
        private bool completed;
        public void Configure(SolitudeSlidingDoor target) => door = target;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || door == null || !door.IsOpen || other.GetComponent<PlayerInteractor>() == null) return;
            completed = true;
            InteractionFeedback.Handle(InteractionResult.Success("Access granted"));
            Debug.Log("SOLITUDE start: access granted. The next area is not built yet.");
        }
    }
}
