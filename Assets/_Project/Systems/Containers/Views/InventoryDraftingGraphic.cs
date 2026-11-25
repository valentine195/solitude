using UnityEngine;
using UnityEngine.UI;

namespace SOLITUDE.Containers
{
    /// <summary>Registration grid behind the selected item preview.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InventoryDraftingGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            DrawPlate(mesh, rectTransform.rect);
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddVert(d, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 offset = Vector2.Perpendicular((b - a).normalized) * (width * .5f);
            Quad(mesh, a + offset, b + offset, b - offset, a - offset, tint);
        }

        private static void DrawPlate(VertexHelper mesh, Rect area)
        {
            Color grid = new Color(.16f, .22f, .23f);
            Color marks = new Color(.39f, .46f, .45f);
            Vector2 center = area.center;
            for (int x = 0; x < area.width; x += 24)
                Line(mesh, new Vector2(area.xMin + x, area.yMin), new Vector2(area.xMin + x, area.yMax), 1, grid);
            for (int y = 0; y < area.height; y += 24)
                Line(mesh, new Vector2(area.xMin, area.yMin + y), new Vector2(area.xMax, area.yMin + y), 1, grid);
            // Open registration marks frame the sprite without introducing a target reticle.
            foreach (int xSign in new[] { -1, 1 })
                foreach (int ySign in new[] { -1, 1 })
                {
                    Vector2 corner = center + new Vector2(xSign * 61, ySign * 55);
                    Line(mesh, corner, corner - new Vector2(12 * xSign, 0), 2, marks);
                    Line(mesh, corner, corner - new Vector2(0, 9 * ySign), 2, marks);
                }
        }
    }
}
