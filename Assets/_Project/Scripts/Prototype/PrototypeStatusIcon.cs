using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Code-native status glyph; the stack label belongs to the slot, not the glyph.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeStatusGlyph : MaskableGraphic
    {
        public StatusEffectKind Kind { get; set; }
        public static Color ColorFor(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Precision => new Color(1f, 0.52f, 0.18f),
            StatusEffectKind.Agility => new Color(0.36f, 0.88f, 0.48f),
            StatusEffectKind.Blindness => new Color(0.62f, 0.65f, 0.69f),
            StatusEffectKind.Freeze => new Color(0.40f, 0.80f, 1f),
            StatusEffectKind.Burn => new Color(1f, 0.25f, 0.20f),
            StatusEffectKind.Poison => new Color(0.71f, 0.53f, 0.96f),
            _ => Color.white
        };
        public PrototypeStatusGlyph() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 20f;
            void Line(Vector2 start, Vector2 end, float thickness = 1.7f)
            {
                Vector2 normal = new Vector2(-(end - start).y, (end - start).x).normalized * thickness / 2f;
                int offset = vh.currentVertCount;
                foreach (Vector2 point in new[] { start - normal, start + normal, end + normal, end - normal })
                    vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                vh.AddTriangle(offset, offset + 1, offset + 2);
                vh.AddTriangle(offset, offset + 2, offset + 3);
            }
            Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
            {
                float u = 1 - t;
                return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
            }
            void Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, List<Vector2> outline = null)
            {
                Vector2 previous = a;
                for (int i = 1; i <= 12; i++)
                {
                    Vector2 point = Bezier(a, b, c, d, i / 12f);
                    if (outline != null) outline.Add(point); else Line(previous, point, 1.7f);
                    previous = point;
                }
            }
            if (Kind == StatusEffectKind.Burn)
            {
                // Same Lucide flame boundary and 0.83 horizontal scale as the approved preview.
                // The preview uses this single curled tip, not the previous hand-drawn three-tip approximation.
                // Source/license: Docs/THIRD_PARTY_NOTICES.md (Lucide, ISC).
                Vector2 FlamePoint(Vector2 p) => new((p.x - 12) * .83f, 12.5f - p.y);
                var points = new List<Vector2> { FlamePoint(new(12, 3)) };
                void SourceCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
                    => Curve(FlamePoint(a), FlamePoint(b), FlamePoint(c), FlamePoint(d), points);
                void Quadratic(Vector2 a, Vector2 control, Vector2 b)
                    => SourceCurve(a, a + (control - a) * (2f / 3), b + (control - b) * (2f / 3), b);
                void Arc(Vector2 center, float radius, float start, float sweep, int steps)
                {
                    for (int i = 1; i <= steps; i++)
                    {
                        float angle = start + sweep * i / steps;
                        points.Add(FlamePoint(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius));
                    }
                }
                Quadratic(new(12, 3), new(13, 7), new(16, 9.5f));
                Quadratic(new(16, 9.5f), new(19, 12), new(19, 15));
                Arc(new(12, 15), 7, 0, Mathf.PI, 24);
                Arc(new(10, 15), 5, Mathf.PI, Mathf.Atan2(3, 4), 12);
                Arc(new(8.5f, 12), 2.5f, Mathf.PI, -Mathf.PI, 16);
                SourceCurve(new(11, 12), new(11, 10), new(9.5f, 9), new(9.5f, 7));
                Quadratic(new(9.5f, 7), new(9.5f, 5), new(12, 3));
                points.RemoveAt(points.Count - 1); // Closing point is already the first vertex.
                FillPolygon(vh, points, rect.center, scale, color);
            }
            else if (Kind == StatusEffectKind.Poison)
            {
                // Rounded skull outline with eyes, nose and three teeth. All holes stay transparent.
                // Cheeks taper directly into the narrow jaw, without outward lower-face lobes.
                Curve(new(-4, -6), new(-4, -4.2f), new(-7, -5), new(-7, -3));
                Curve(new(-7, -3), new(-9, -2), new(-9, 1), new(-7.5f, 3));
                Curve(new(-7.5f, 3), new(-6.5f, 7), new(-3.5f, 9), new(0, 9));
                Curve(new(0, 9), new(3.5f, 9), new(6.5f, 7), new(7.5f, 3));
                Curve(new(7.5f, 3), new(9, 1), new(9, -2), new(7, -3));
                Curve(new(7, -3), new(7, -5), new(4, -4.2f), new(4, -6));
                Line(new(-4, -6), new(-4, -8.5f)); Line(new(-4, -8.5f), new(4, -8.5f)); Line(new(4, -8.5f), new(4, -6));
                Line(new(-1.4f, -6.5f), new(-1.4f, -8.5f), 1.2f); Line(new(1.4f, -6.5f), new(1.4f, -8.5f), 1.2f);
                for (int eye = -1; eye <= 1; eye += 2)
                {
                    int first = vh.currentVertCount;
                    Vector2 center = new(eye * 3, 1);
                    vh.AddVert(rect.center + center * scale, color, Vector2.zero);
                    for (int i = 0; i < 20; i++)
                    {
                        float angle = i * Mathf.PI / 10;
                        vh.AddVert(rect.center + (center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.6f) * scale, color, Vector2.zero);
                    }
                    for (int i = 0; i < 20; i++) vh.AddTriangle(first, first + i + 1, first + (i + 1) % 20 + 1);
                }
                Line(new(0, -2.5f), new(-.8f, -4), 1.1f); Line(new(-.8f, -4), new(.8f, -4), 1.1f);
                Line(new(.8f, -4), new(0, -2.5f), 1.1f);
            }
            else if (Kind == StatusEffectKind.Haste)
            {
                // Three overlapping feathers share a tapered root; transparent gaps separate the tips.
                void Feather(params Vector2[] points)
                {
                    int offset = vh.currentVertCount;
                    foreach (Vector2 point in points) vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                    for (int index = 1; index < points.Length - 1; index++) vh.AddTriangle(offset, offset + index, offset + index + 1);
                }
                Feather(new(-8f, -7f), new(-6f, 1f), new(0f, 6f), new(9f, 9f), new(5f, 3f), new(-1f, -2f));
                Feather(new(-8f, -7f), new(-2f, -2f), new(6f, 1f), new(4f, -4f), new(0f, -6f));
                Feather(new(-8f, -7f), new(-3f, -4f), new(3f, -3f), new(1f, -8f), new(-3f, -9f));
            }
            else if (Kind == StatusEffectKind.Freeze)
            {
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * Mathf.PI / 3f;
                    Vector2 axis = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 normal = new(-axis.y, axis.x);
                    Line(Vector2.zero, axis * 8f, 1.4f);
                    Line(axis * 4.5f, axis * 6.5f + normal * 2f, 1.3f);
                    Line(axis * 4.5f, axis * 6.5f - normal * 2f, 1.3f);
                }
            }
            else if (Kind == StatusEffectKind.Precision)
            {
                // Sharp four-point glint, distinct from the weapon-stat critical damage glyph.
                Vector2[] points = { new(0, 9), new(2, 2), new(9, 0), new(2, -2), new(0, -9), new(-2, -2), new(-9, 0), new(-2, 2) };
                int first = vh.currentVertCount;
                vh.AddVert(rect.center, color, Vector2.zero);
                foreach (var point in points) vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                for (int i = 0; i < points.Length; i++) vh.AddTriangle(first, first + i + 1, first + (i + 1) % points.Length + 1);
            }
            else if (Kind == StatusEffectKind.Focus || Kind == StatusEffectKind.Blindness)
            {
                Vector2[] eye = { new(-9, 0), new(-5, 4), new(0, 5.5f), new(5, 4), new(9, 0), new(5, -4), new(0, -5.5f), new(-5, -4) };
                for (int i = 0; i < eye.Length; i++) Line(eye[i], eye[(i + 1) % eye.Length], 1.3f);
                for (int i = 0; i < 16; i++)
                {
                    float a = Mathf.PI * i / 8f, b = Mathf.PI * (i + 1) / 8f;
                    Line(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.2f, new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 2.2f, 1.4f);
                }
                if (Kind == StatusEffectKind.Blindness) Line(new(-8, 8), new(8, -8), 2.1f);
                else { Line(new(0, 8.5f), new(0, 6.5f), 1.3f); Line(new(0, -8.5f), new(0, -6.5f), 1.3f); }
            }
            else if (Kind == StatusEffectKind.Agility)
            {
                // Split the concave boot silhouette at the ankle into two convex polygons.
                // This keeps its tapered shaft and rounded, right-facing toe cleanly filled.
                void BootPart(params Vector2[] points)
                {
                    int first = vh.currentVertCount;
                    foreach (Vector2 point in points) vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                    for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(first, first + i, first + i + 1);
                }
                BootPart(new(-1, 9), new(6, 8), new(2, 1), new(-5, -5.5f));
                BootPart(new(-5, -5.5f), new(2, 1), new(5, 0), new(7, -2), new(8.5f, -5), new(8.5f, -7.5f), new(3.5f, -7.5f));
                Line(new(-10, 4), new(-4.5f, 4), 1.3f);
                Line(new(-10, 0), new(-5.5f, 0), 1.3f);
                Line(new(-10, -4), new(-6.5f, -4), 1.3f);
            }
        }

        private static void FillPolygon(VertexHelper vh, List<Vector2> points, Vector2 center, float scale, Color tint)
        {
            // Ear clipping preserves the curved flame's inward notches instead of filling them with a triangle fan.
            float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            float area = 0;
            for (int i = 0; i < points.Count; i++)
                area += points[i].x * points[(i + 1) % points.Count].y - points[(i + 1) % points.Count].x * points[i].y;
            if (area < 0) points.Reverse();
            int first = vh.currentVertCount;
            var remaining = new List<int>();
            for (int i = 0; i < points.Count; i++)
            { vh.AddVert(center + points[i] * scale, tint, Vector2.zero); remaining.Add(i); }
            while (remaining.Count > 2)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count], b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (Cross(points[a], points[b], points[c]) <= 1e-6f) continue;
                    bool contains = false;
                    foreach (int p in remaining)
                        if (p != a && p != b && p != c && Cross(points[a], points[b], points[p]) >= -1e-6f
                            && Cross(points[b], points[c], points[p]) >= -1e-6f && Cross(points[c], points[a], points[p]) >= -1e-6f)
                        { contains = true; break; }
                    if (contains) continue;
                    vh.AddTriangle(first + a, first + b, first + c);
                    remaining.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) break;
            }
        }
    }

    public sealed class PrototypeStatusIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public const float Size = 42f;
        private CombatSimulation _combat;
        private PrototypeItemTooltip _tooltip;
        private Text _count;
        public CombatSide Side { get; private set; }
        public StatusEffectKind Kind { get; private set; }
        public long Stacks => _combat.GetStatusStacks(Side, Kind);
        public bool IsDebuff => _combat.GetStatusDefinition(Kind).IsDebuff;

        public static PrototypeStatusIcon Create(Transform parent, CombatSimulation combat, CombatSide side,
            StatusEffectKind kind, PrototypeItemTooltip tooltip)
        {
            var obj = new GameObject($"{side} {kind} Status", typeof(RectTransform), typeof(Image), typeof(PrototypeStatusIcon));
            obj.transform.SetParent(parent, false);
            PrototypeStatusIcon icon = obj.GetComponent<PrototypeStatusIcon>();
            icon._combat = combat; icon._tooltip = tooltip; icon.Side = side; icon.Kind = kind;
            Image background = obj.GetComponent<Image>();
            background.color = Color.clear; // Invisible hover target; only the glyph and stack count are drawn.
            background.raycastTarget = true;
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = Vector2.one * Size;
            var glyphObject = new GameObject("Status Glyph", typeof(RectTransform), typeof(PrototypeStatusGlyph));
            glyphObject.transform.SetParent(obj.transform, false);
            PrototypeStatusGlyph glyph = glyphObject.GetComponent<PrototypeStatusGlyph>();
            glyph.Kind = kind; glyph.color = PrototypeStatusGlyph.ColorFor(kind); glyph.raycastTarget = false;
            BackpackPrototypeController.Stretch(glyph.rectTransform, 6f);
            icon._count = BackpackPrototypeController.CreateText(obj.transform, "Status Stacks", string.Empty, 20,
                FontStyle.Bold, TextAnchor.LowerRight);
            icon._count.raycastTarget = false;
            icon._count.resizeTextForBestFit = true;
            icon._count.resizeTextMinSize = 12;
            icon._count.resizeTextMaxSize = 20;
            BackpackPrototypeController.Stretch(icon._count.rectTransform, 1.5f);
            Outline outline = icon._count.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.03f, 0.05f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            icon.Refresh();
            return icon;
        }

        public void Refresh()
        {
            _count.text = Stacks.ToString();
            gameObject.SetActive(Stacks > 0);
        }
        public void OnPointerEnter(PointerEventData pointer)
            => _tooltip.ShowStatus(this, () => StatusTooltipContent.FromCombat(_combat, Side, Kind), pointer.position, pointer.enterEventCamera);
        public void OnPointerMove(PointerEventData pointer) => _tooltip.Move(this, pointer.position, pointer.enterEventCamera);
        public void OnPointerExit(PointerEventData pointer) => _tooltip.Hide(this);
        private void OnDisable() { if (_tooltip != null) _tooltip.Hide(this); }
    }
}
