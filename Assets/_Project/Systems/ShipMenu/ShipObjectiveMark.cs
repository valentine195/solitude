using UnityEngine;
using UnityEngine.UI;

namespace SOLITUDE.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShipObjectiveMark : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .48f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 start = Direction(i);
                Vector2 end = Direction(i + 1);
                int offset = mesh.currentVertCount;
                AddVertex(mesh, rect.center, start, radius);
                AddVertex(mesh, rect.center, end, radius);
                AddVertex(mesh, rect.center, end, radius * .66f);
                AddVertex(mesh, rect.center, start, radius * .66f);
                mesh.AddTriangle(offset, offset + 1, offset + 2);
                mesh.AddTriangle(offset, offset + 2, offset + 3);
            }

            int center = mesh.currentVertCount;
            for (int i = 0; i < 4; i++)
                AddVertex(mesh, rect.center, Direction(i), radius * .27f);
            mesh.AddTriangle(center, center + 1, center + 2);
            mesh.AddTriangle(center, center + 2, center + 3);
        }

        private void AddVertex(VertexHelper mesh, Vector2 center, Vector2 direction, float radius) =>
            mesh.AddVert(center + direction * radius, color, Vector2.zero);

        private static Vector2 Direction(int index) => (index % 4) switch
        {
            0 => Vector2.up,
            1 => Vector2.right,
            2 => Vector2.down,
            _ => Vector2.left
        };
    }
}
