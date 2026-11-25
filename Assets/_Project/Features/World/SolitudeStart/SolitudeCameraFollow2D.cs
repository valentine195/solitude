using UnityEngine;

namespace SOLITUDE.World.SolitudeStart
{
    /// <summary>Pixel-snapped horizontal camera follow for the SOLITUDE exploration level.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class SolitudeCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 xBounds = new Vector2(4.7f, 20.3f);
        [SerializeField] private Vector2 yBounds = new Vector2(4.5f, 4.5f);
        [SerializeField] private int pixelsPerUnit = 32;

        public void Configure(Transform followTarget, Vector2 horizontalBounds, Vector2 verticalBounds)
        {
            target = followTarget;
            xBounds = horizontalBounds;
            yBounds = verticalBounds;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 next = transform.position;
            next.x = Mathf.Clamp(target.position.x, xBounds.x, xBounds.y);
            next.y = Mathf.Clamp(target.position.y, yBounds.x, yBounds.y);
            next.x = Mathf.Round(next.x * pixelsPerUnit) / pixelsPerUnit;
            next.y = Mathf.Round(next.y * pixelsPerUnit) / pixelsPerUnit;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }
}
