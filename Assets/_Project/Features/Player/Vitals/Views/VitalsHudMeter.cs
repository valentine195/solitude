using UnityEngine;

namespace SOLITUDE.Player.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VitalsHudMeter : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private Color trackColor;
        [SerializeField] private float segmentGap = 7;
        [SerializeField] private Color sweepColor = new Color32(234, 255, 240, 255);

        private float value = 1;
        private int segments = 1;
        private float sweep = -1;

        internal void Show(float normalized, int segmentCount, float sweepPosition = -1)
        {
            normalized = Mathf.Clamp01(normalized);
            segmentCount = Mathf.Max(1, segmentCount);
            if (value == normalized && segments == segmentCount && sweep == sweepPosition) return;
            value = normalized;
            segments = segmentCount;
            sweep = sweepPosition;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float slant = rect.height * .5f;
            float span = rect.width - slant;
            if (span <= 0 || rect.height <= 0) return;

            // Keep the authored gap at ordinary counts. Scale it down if a scene uses many segments.
            float gap = Mathf.Min(segmentGap, span / segments * .25f);
            float width = (span - (segments - 1) * gap) / segments;
            int completed = Mathf.RoundToInt(value * segments) - 1;
            for (int index = 0; index < segments; index++)
            {
                float left = rect.xMin + (width + gap) * index;
                float amount = Mathf.Clamp01(value * segments - index) * width;
                Quad(mesh, rect, slant, left, left + width, trackColor, trackColor);
                if (amount <= 0) continue;
                Quad(mesh, rect, slant, left, left + amount, color, color);
                if (index == completed && sweep >= 0)
                    DrawSweep(mesh, rect, slant, left, amount);
            }
        }

        private void DrawSweep(UnityEngine.UI.VertexHelper mesh, Rect rect, float slant, float left, float width)
        {
            const float halfWidth = 12;
            float head = Mathf.Lerp(-halfWidth, width + halfWidth, sweep);
            float first = Mathf.Clamp(head - halfWidth, 0, width);
            float last = Mathf.Clamp(head + halfWidth, 0, width);
            float peak = Mathf.Clamp(head, first, last);
            Color At(float x) => Color.Lerp(color, sweepColor,
                .63f * Mathf.Clamp01(1 - Mathf.Abs(x - head) / halfWidth));

            if (first < peak)
                Quad(mesh, rect, slant, left + first, left + peak, At(first), At(peak));
            if (peak < last)
                Quad(mesh, rect, slant, left + peak, left + last, At(peak), At(last));
        }

        private static void Quad(UnityEngine.UI.VertexHelper mesh, Rect rect, float slant,
            float left, float right, Color leftColor, Color rightColor)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, rect.yMin), leftColor, Vector2.zero);
            mesh.AddVert(new Vector3(left + slant, rect.yMax), leftColor, Vector2.zero);
            mesh.AddVert(new Vector3(right + slant, rect.yMax), rightColor, Vector2.zero);
            mesh.AddVert(new Vector3(right, rect.yMin), rightColor, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
