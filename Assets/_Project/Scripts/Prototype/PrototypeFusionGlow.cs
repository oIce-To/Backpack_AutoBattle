using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeFusionGlow : MaskableGraphic
    {
        public PrototypeFusionGlow() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            vh.AddVert(center, color, Vector2.zero);
            Color transparent = color; transparent.a = 0;
            const int segments = 48;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, transparent, Vector2.zero);
                if (i > 0) vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
