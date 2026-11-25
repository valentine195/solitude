using UnityEngine;
namespace SOLITUDE.World.OpeningSlice
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class OpeningDepthSort : MonoBehaviour
    {
        private SpriteRenderer visual;
        private void Awake() => visual = GetComponent<SpriteRenderer>();
        private void LateUpdate() => visual.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 32);
    }
}
