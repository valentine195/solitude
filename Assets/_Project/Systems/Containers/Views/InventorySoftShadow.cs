using UnityEngine;
using UnityEngine.UI;

namespace SOLITUDE.Containers
{
    /// <summary>Feathered UI geometry; no texture allocation or per-frame mesh rebuild.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InventorySoftShadow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            const int segments = 32, rings = 12;
            for (int ring = 0; ring <= rings; ring++)
            {
                float radius = 28f * ring / rings;
                float opacity = .42f * Mathf.Pow(1f - (float)ring / rings, 2);
                for (int point = 0; point < segments; point++)
                {
                    int corner = point / 8;
                    float angle = (corner * 90 + point % 8 * 90f / 7) * Mathf.Deg2Rad;
                    var center = new Vector2(corner == 0 || corner == 3 ? rect.xMax : rect.xMin,
                        corner < 2 ? rect.yMax : rect.yMin);
                    var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.AddVert(position, new Color(0,0,0,opacity), Vector2.zero);
                }
            }
            for (int i = 1; i < segments - 1; i++) mesh.AddTriangle(0,i,i+1);
            for (int ring = 0; ring < rings; ring++)
                for (int i = 0; i < segments; i++)
                {
                    int a=ring*segments+i, b=ring*segments+(i+1)%segments;
                    mesh.AddTriangle(a,b,a+segments);mesh.AddTriangle(b,b+segments,a+segments);
                }
        }
    }
}
