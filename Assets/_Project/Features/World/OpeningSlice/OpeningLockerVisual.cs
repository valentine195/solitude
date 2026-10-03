using SOLITUDE.Containers;
using UnityEngine;
namespace SOLITUDE.World.OpeningSlice
{
    // Presentation only; the existing modal still owns opening/closing and pause policy.
    public sealed class OpeningLockerVisual : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Sprite closedSprite, openSprite;
        public ContainerTransferModalView modal;
        private void LateUpdate() { if(visual != null) visual.sprite = modal != null && modal.IsOpen ? openSprite : closedSprite; }
    }
}
