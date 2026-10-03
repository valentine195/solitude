using System.Collections;
using SOLITUDE.Core.Interaction;
using SOLITUDE.Core.UI;
using SOLITUDE.Player;
using UnityEngine;
namespace SOLITUDE.World.OpeningSlice
{
    public sealed class OpeningFirstContact : MonoBehaviour
    {
        public OpeningPowerSocket socket;
        public bool Heard { get; private set; }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Heard || socket == null || !socket.Powered || !socket.door.IsOpen || other.GetComponent<PlayerInteractor>() == null) return;
            Heard = true;
            StartCoroutine(Contact());
        }
        private IEnumerator Contact()
        {
            foreach (var fragment in new[] { "...caretaker?", "Local systems... responding.", "Please remain... I am trying to remember." })
            {
                InteractionFeedback.Handle(InteractionResult.Success(fragment));
                Debug.Log("[SOLITUDE first contact] " + fragment);
                yield return new WaitForSeconds(3.2f);
            }
        }
    }
}
