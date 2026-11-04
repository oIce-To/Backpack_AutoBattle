using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>The same small experience emblem is used for wallet balances and upgrade costs.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeExperienceIcon : MaskableGraphic
    {
        public PrototypeExperienceIcon() { useLegacyMeshGeneration = false; }
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .48f;
            void Star(float size, Color tint)
            {
                int start = vh.currentVertCount;
                vh.AddVert(rect.center, tint * color, Vector2.zero);
                for (int i = 0; i < 10; i++)
                {
                    float angle = (90 + i * 36) * Mathf.Deg2Rad;
                    float distance = size * (i % 2 == 0 ? 1 : .48f);
                    vh.AddVert(rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance, tint * color, Vector2.zero);
                }
                for (int i = 0; i < 10; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 10);
            }
            Star(radius, new Color(.39f, .66f, .19f));
            Star(radius * .84f, new Color(.73f, .96f, .40f));

            // Rounded, outlined lettering stays identical at both icon sizes without relying on an installed font.
            float scale = Mathf.Min(rect.width, rect.height) / 20f * .88f;
            Vector2 letteringOffset = new(1.35f, 0);
            var strokes = new System.Collections.Generic.List<(Vector2 a, Vector2 b)>();
            void Stroke(float ax, float ay, float bx, float by) => strokes.Add((new(ax, ay), new(bx, by)));
            Stroke(-10.6f, 3.2f, -10.6f, -3.2f);
            Stroke(-10.6f, 3.2f, -6.2f, 3.2f); Stroke(-10.6f, 0, -6.8f, 0); Stroke(-10.6f, -3.2f, -6.2f, -3.2f);
            Stroke(-3.8f, 3.2f, .7f, -3.2f); Stroke(-3.8f, -3.2f, .7f, 3.2f);
            Stroke(3.5f, -3.2f, 3.5f, 3.2f); Stroke(3.5f, 3.2f, 6.2f, 3.2f);
            for (int i = 0; i < 10; i++)
            {
                float a = (90 - i * 18) * Mathf.Deg2Rad, b = (90 - (i + 1) * 18) * Mathf.Deg2Rad;
                strokes.Add((new Vector2(6.2f + Mathf.Cos(a) * 1.7f, 1.5f + Mathf.Sin(a) * 1.7f),
                    new Vector2(6.2f + Mathf.Cos(b) * 1.7f, 1.5f + Mathf.Sin(b) * 1.7f)));
            }
            Stroke(6.2f, -.2f, 3.5f, -.2f);
            void Dot(Vector2 center, float size, Color tint)
            {
                int start = vh.currentVertCount;
                vh.AddVert(rect.center + (center + letteringOffset) * scale, tint, Vector2.zero);
                for (int i = 0; i < 10; i++)
                {
                    float angle = i * Mathf.PI / 5;
                    vh.AddVert(rect.center + (center + letteringOffset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size) * scale, tint, Vector2.zero);
                }
                for (int i = 0; i < 10; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 10);
            }
            void Lettering(float thickness, Color tint)
            {
                foreach (var stroke in strokes)
                {
                    Vector2 normal = new Vector2(-(stroke.b - stroke.a).y, (stroke.b - stroke.a).x).normalized * thickness / 2;
                    int start = vh.currentVertCount;
                    foreach (var p in new[] { stroke.a - normal, stroke.a + normal, stroke.b + normal, stroke.b - normal })
                        vh.AddVert(rect.center + (p + letteringOffset) * scale, tint, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                    Dot(stroke.a, thickness / 2, tint); Dot(stroke.b, thickness / 2, tint);
                }
            }
            Lettering(2.7f, new Color(0, 0, 0, color.a));
            Lettering(1.3f, color);
        }
    }

    /// <summary>A rounded UI button face without a generated texture or extra asset dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeRoundedPanel : MaskableGraphic
    {
        public bool DrawBorder { get; set; } = true;
        public PrototypeRoundedPanel() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            void Fill(float inset, Color tint)
            {
                Rect area = new(rect.xMin + inset, rect.yMin + inset, rect.width - inset * 2, rect.height - inset * 2);
                float radius = Mathf.Min(12 - inset, Mathf.Min(area.width, area.height) / 2);
                int start = vh.currentVertCount;
                vh.AddVert(area.center, tint, Vector2.zero);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 center = new(corner is 0 or 3 ? area.xMax - radius : area.xMin + radius,
                        corner < 2 ? area.yMax - radius : area.yMin + radius);
                    for (int step = 0; step <= 8; step++)
                    {
                        float angle = (corner * 90 + step * 90f / 8) * Mathf.Deg2Rad;
                        vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                    }
                }
                const int points = 36;
                for (int i = 0; i < points; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points);
            }
            if (DrawBorder)
            {
                Fill(0, new Color(Mathf.Min(1, color.r * 1.35f), Mathf.Min(1, color.g * 1.35f), Mathf.Min(1, color.b * 1.35f), color.a));
                Fill(1.5f, color);
            }
            else Fill(0, color);
        }
    }
}
