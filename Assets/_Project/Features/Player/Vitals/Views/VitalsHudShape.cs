using UnityEngine;

namespace SOLITUDE.Player.UI
{
    // The HUD uses convex, authored polygons in normalized, top-left coordinates.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VitalsHudShape : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private Vector2[] points;

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (points == null || points.Length < 3 || !base.Raycast(screenPoint, eventCamera)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return false;

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return false;
            var point = new Vector2((local.x - rect.xMin) / rect.width, (rect.yMax - local.y) / rect.height);
            bool positive = false, negative = false;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 edge = points[(i + 1) % points.Length] - points[i];
                Vector2 relative = point - points[i];
                float side = edge.x * relative.y - edge.y * relative.x;
                positive |= side > 0;
                negative |= side < 0;
                if (positive && negative) return false;
            }
            return true;
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if (points == null || points.Length < 3) return;

            var rect = GetPixelAdjustedRect();
            foreach (var point in points)
                mesh.AddVert(new Vector3(rect.xMin + point.x * rect.width, rect.yMax - point.y * rect.height),
                    color, Vector2.zero);

            for (int i = 2; i < points.Length; i++)
                mesh.AddTriangle(0, i - 1, i);
        }
    }
}
