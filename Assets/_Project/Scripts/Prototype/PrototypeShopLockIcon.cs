using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Small white padlock glyph, with a transparent keyhole. Never intercepts pointer input.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeShopLockIcon : MaskableGraphic
    {
        public PrototypeShopLockIcon() { useLegacyMeshGeneration = false; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 28f;
            Vector2 center = rect.center;
            // Four body rectangles leave a transparent keyhole rather than painting over the background.
            Quad(vh, center, scale, -9f, -12f, -1.6f, 3f);
            Quad(vh, center, scale, 1.6f, -12f, 9f, 3f);
            Quad(vh, center, scale, -1.6f, -12f, 1.6f, -6f);
            Quad(vh, center, scale, -1.6f, -1f, 1.6f, 3f);
            Quad(vh, center, scale, -7f, 1f, -5f, 5f);
            Quad(vh, center, scale, 5f, 1f, 7f, 5f);
            const int segments = 16;
            for (int index = 0; index < segments; index++)
            {
                float a = Mathf.PI * index / segments;
                float b = Mathf.PI * (index + 1) / segments;
                int first = vh.currentVertCount;
                foreach (Vector2 point in new[]
                {
                    new Vector2(Mathf.Cos(a) * 7f, 5f + Mathf.Sin(a) * 7f),
                    new Vector2(Mathf.Cos(b) * 7f, 5f + Mathf.Sin(b) * 7f),
                    new Vector2(Mathf.Cos(b) * 5f, 5f + Mathf.Sin(b) * 5f),
                    new Vector2(Mathf.Cos(a) * 5f, 5f + Mathf.Sin(a) * 5f)
                }) vh.AddVert(center + point * scale, color, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first, first + 2, first + 3);
            }
        }

        private void Quad(VertexHelper vh, Vector2 center, float scale, float left, float bottom, float right, float top)
        {
            int first = vh.currentVertCount;
            vh.AddVert(center + new Vector2(left, bottom) * scale, color, Vector2.zero);
            vh.AddVert(center + new Vector2(left, top) * scale, color, Vector2.zero);
            vh.AddVert(center + new Vector2(right, top) * scale, color, Vector2.zero);
            vh.AddVert(center + new Vector2(right, bottom) * scale, color, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
