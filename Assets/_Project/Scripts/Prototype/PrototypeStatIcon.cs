using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public enum WeaponStatIcon { Attack, Cooldown, Accuracy, CriticalChance, CriticalDamage }

    /// <summary>Code-native stat glyphs with the user's original raster silhouette for critical damage.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeStatIcon : MaskableGraphic
    {
        private WeaponStatIcon _kind;
        private RawImage _criticalDamageSource;
        public PrototypeStatIcon() { useLegacyMeshGeneration = false; }
        public void SetKind(WeaponStatIcon kind)
        {
            _kind = kind;
            if (kind == WeaponStatIcon.CriticalDamage && _criticalDamageSource == null)
            {
                var obj = new GameObject("Original Critical Damage", typeof(RectTransform), typeof(RawImage));
                obj.transform.SetParent(transform, false);
                _criticalDamageSource = obj.GetComponent<RawImage>();
                _criticalDamageSource.texture = Resources.Load<Texture2D>("UI/CriticalDamageSource");
                _criticalDamageSource.material = Resources.Load<Material>("UI/CriticalDamageWhite");
                _criticalDamageSource.raycastTarget = false;
                RectTransform rect = _criticalDamageSource.rectTransform;
                // Retain the source's square framing and leave room for the common ring.
                rect.anchorMin = Vector2.one * 0.23f;
                rect.anchorMax = Vector2.one * 0.77f;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            if (_criticalDamageSource != null)
                _criticalDamageSource.gameObject.SetActive(kind == WeaponStatIcon.CriticalDamage);
            SetVerticesDirty();
        }

        public override void SetVerticesDirty()
        {
            // Propagate tint when it changes, not from inside the canvas mesh-rebuild loop.
            if (_criticalDamageSource != null) _criticalDamageSource.color = color;
            base.SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float scale = Mathf.Min(rect.width, rect.height) / 32f;
            Ring(vh, center, 14f * scale, 1.4f * scale, new Color(0.42f, 0.53f, 0.67f));
            Color ink = color;
            switch (_kind)
            {
                case WeaponStatIcon.Attack:
                    Sword(vh, center, scale, ink);
                    break;
                case WeaponStatIcon.Cooldown:
                    Ring(vh, center, 8f * scale, 1.8f * scale, ink);
                    Line(vh, center, scale, Vector2.zero, new Vector2(0f, 6f), 1.8f, ink);
                    Line(vh, center, scale, Vector2.zero, new Vector2(5f, -2f), 1.8f, ink);
                    break;
                case WeaponStatIcon.Accuracy:
                    Ring(vh, center, 7f * scale, 1.7f * scale, ink);
                    Ring(vh, center, 2f * scale, 1.7f * scale, ink);
                    Line(vh, center, scale, new Vector2(-10f, 0f), new Vector2(-4f, 0f), 1.6f, ink);
                    Line(vh, center, scale, new Vector2(4f, 0f), new Vector2(10f, 0f), 1.6f, ink);
                    Line(vh, center, scale, new Vector2(0f, -10f), new Vector2(0f, -4f), 1.6f, ink);
                    Line(vh, center, scale, new Vector2(0f, 4f), new Vector2(0f, 10f), 1.6f, ink);
                    break;
                case WeaponStatIcon.CriticalChance:
                    Star(vh, center, scale, ink);
                    break;
                case WeaponStatIcon.CriticalDamage:
                    break;
            }
        }

        private static void Sword(VertexHelper vh, Vector2 center, float scale, Color ink)
        {
            // Sword-local X is across the blade; Y runs from the pommel to the tip.
            Vector2 BladePoint(float x, float y) => new Vector2((y - x) * 0.70710678f, (y + x) * 0.70710678f);
            Polygon(vh, center, scale, BladePoint(0f, 3f), ink,
                BladePoint(-2f, -2f), BladePoint(-2f, 7.8f), BladePoint(0f, 12f),
                BladePoint(2f, 7.8f), BladePoint(2f, -2f));
            Polygon(vh, center, scale, BladePoint(0f, -2.9f), ink,
                BladePoint(-4.5f, -3.8f), BladePoint(-4.5f, -2f),
                BladePoint(4.5f, -2f), BladePoint(4.5f, -3.8f));
            Polygon(vh, center, scale, BladePoint(0f, -6f), ink,
                BladePoint(-1.1f, -8.3f), BladePoint(-1.1f, -3.8f),
                BladePoint(1.1f, -3.8f), BladePoint(1.1f, -8.3f));
            Polygon(vh, center, scale, BladePoint(0f, -9f), ink,
                BladePoint(-1.7f, -9f), BladePoint(0f, -7.3f),
                BladePoint(1.7f, -9f), BladePoint(0f, -10.7f));
        }

        private static void Star(VertexHelper vh, Vector2 center, float scale, Color ink)
        {
            var points = new Vector2[16];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = index * Mathf.PI / 8f;
                // All eight tips have the same radial length and sharpness.
                float radius = index % 2 == 0 ? 10.8f : 2.8f;
                points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            Polygon(vh, center, scale, Vector2.zero, ink, points);
        }

        // The polygons are star-shaped around fanCenter, including the concave bursts.
        private static void Polygon(VertexHelper vh, Vector2 center, float scale, Vector2 fanCenter, Color ink,
            params Vector2[] points)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center + fanCenter * scale, ink, Vector2.zero);
            foreach (Vector2 point in points) vh.AddVert(center + point * scale, ink, Vector2.zero);
            for (int index = 0; index < points.Length; index++)
                vh.AddTriangle(start, start + index + 1, start + (index + 1) % points.Length + 1);
        }

        private static void Line(VertexHelper vh, Vector2 center, float scale, Vector2 from, Vector2 to, float width, Color ink)
        {
            from = center + from * scale;
            to = center + to * scale;
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * width * scale * 0.5f;
            Quad(vh, from - normal, from + normal, to + normal, to - normal, ink);
        }

        private static void Ring(VertexHelper vh, Vector2 center, float radius, float width, Color ink)
        {
            const int segments = 32;
            for (int index = 0; index < segments; index++)
            {
                float a = index * Mathf.PI * 2f / segments;
                float b = (index + 1) * Mathf.PI * 2f / segments;
                Vector2 u = new(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 v = new(Mathf.Cos(b), Mathf.Sin(b));
                Quad(vh, center + u * (radius - width), center + u * radius,
                    center + v * radius, center + v * (radius - width), ink);
            }
        }

        private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ink)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, ink, Vector2.zero);
            vh.AddVert(b, ink, Vector2.zero);
            vh.AddVert(c, ink, Vector2.zero);
            vh.AddVert(d, ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
