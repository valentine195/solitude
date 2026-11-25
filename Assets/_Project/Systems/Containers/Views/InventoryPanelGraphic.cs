using UnityEngine;
using UnityEngine.UI;
namespace SOLITUDE.Containers
{
    /// <summary>Authored cut-corner surface with a restrained vertical lighting falloff.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InventoryPanelGraphic : MaskableGraphic
    {
        [SerializeField] private Color top = new Color(.13f,.18f,.20f);
        [SerializeField] private Color bottom = new Color(.055f,.085f,.10f);
        [SerializeField] private float cut = 20;
        [SerializeField] private Color topRule;
        [SerializeField] private bool squareLowerCorner;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float corner = Mathf.Clamp(cut, 0f, Mathf.Min(rect.width, rect.height) * (squareLowerCorner ? .5f : .2f));
            float lowerCut = squareLowerCorner ? 0f : corner;
            Vector2[] points =
            {
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax - corner, rect.yMax),
                new Vector2(rect.xMax, rect.yMax - corner),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMin + lowerCut, rect.yMin),
                new Vector2(rect.xMin, rect.yMin + lowerCut)
            };
            mesh.AddVert(rect.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            foreach (var point in points)
                mesh.AddVert(point, Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, point.y)), Vector2.zero);
            for (int i = 0; i < points.Length; i++)
                mesh.AddTriangle(0, i + 1, (i + 1) % points.Length + 1);

            if (topRule.a > 0f)
            {
                AddRule(mesh, points[0], points[1]);
                AddRule(mesh, points[1], points[2]);
            }
        }

        private void AddRule(VertexHelper mesh, Vector2 start, Vector2 end)
        {
            var inside = Vector2.Perpendicular(start - end).normalized;
            int first = mesh.currentVertCount;
            mesh.AddVert(start, topRule, Vector2.zero);
            mesh.AddVert(end, topRule, Vector2.zero);
            mesh.AddVert(end + inside, topRule, Vector2.zero);
            mesh.AddVert(start + inside, topRule, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
