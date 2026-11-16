using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeFusionOverlay : MaskableGraphic
    {
        private sealed class Link
        {
            public Vector2 From, To;
            public int First, Second;
            public bool Ready, Requested;
            public float Opacity;
        }
        private readonly List<Link> _links = new();
        private readonly List<Text> _labels = new();
        private int _labelCount;
        public int ReadyLinkCount { get; private set; }
        public int HintLinkCount { get; private set; }
        public PrototypeFusionOverlay() { useLegacyMeshGeneration = false; }
        public void Begin()
        {
            foreach (var link in _links) link.Requested = false;
            _labelCount = ReadyLinkCount = HintLinkCount = 0;
        }
        public void Clear()
        {
            _links.Clear(); Begin(); End();
        }
        public void AddLink(Vector2 from, Vector2 to, bool ready, int fromId, int toId)
        {
            if (fromId > toId) { (fromId, toId) = (toId, fromId); (from, to) = (to, from); }
            var link = _links.Find(entry => entry.First == fromId && entry.Second == toId && entry.Ready == ready);
            if (link == null)
            {
                link = new Link { First = fromId, Second = toId, Ready = ready, Opacity = ready ? 1f : 0f };
                _links.Add(link);
            }
            link.From = from; link.To = to; link.Requested = true;
            if (ready) ReadyLinkCount++; else HintLinkCount++;
        }
        public void AddLabel(Vector2 position, string name)
        {
            if (_labelCount == _labels.Count)
            {
                var label = BackpackPrototypeController.CreateText(transform, "Fusion Result", "", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                label.color = new Color(1f, .86f, .12f);
                label.gameObject.AddComponent<Outline>().effectColor = new Color(.04f, .05f, .08f, .95f);
                _labels.Add(label);
            }
            var text = _labels[_labelCount++];
            text.gameObject.SetActive(true); text.text = name;
            BackpackPrototypeController.SetRect(text.rectTransform, Vector2.one * .5f, position + Vector2.down * 16f, new Vector2(180, 26));
        }
        public void End()
        {
            for (int i = _labelCount; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
            foreach (var link in _links)
                link.Opacity = link.Ready && !link.Requested ? 0f
                    : Mathf.MoveTowards(link.Opacity, link.Requested ? 1f : 0f, Time.unscaledDeltaTime / .12f);
            _links.RemoveAll(link => !link.Requested && link.Opacity <= 0f);
            SetVerticesDirty();
        }

        public static Vector2 CurvePoint(Vector2 from, Vector2 to, float t)
        {
            Vector2 delta = to - from;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            if (normal.y < 0f || (Mathf.Approximately(normal.y, 0f) && normal.x < 0f)) normal = -normal;
            float bend = Mathf.Min(delta.magnitude * .26f, Mathf.Clamp(delta.magnitude * .16f, 12f, 38f));
            return Vector2.Lerp(from, to, t) + normal * (4 * t * (1 - t) * bend);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var link in _links)
            {
                SmoothCord(vh, link);
            }
        }

        private static void SmoothCord(VertexHelper vh, Link link)
        {
            float length = Vector2.Distance(link.From, link.To);
            if (length < 1 || link.Opacity <= 0) return;
            Color tint = PrototypePixelSkin.Hex(link.Ready ? 0xe8c46au : 0xd4d3c1u);
            tint.a = link.Opacity * (link.Ready ? 1 : .8f);
            Color shadow = PrototypePixelSkin.Hex(0x30271d); shadow.a = tint.a;
            Color highlight = PrototypePixelSkin.Hex(link.Ready ? 0xffeab2u : 0xf5efe0u); highlight.a = tint.a;
            // Connected ribbon geometry follows the curve without pixel snapping or gaps.
            shadow.a *= .35f;
            Stroke(vh, link, link.Ready ? 2.2f : 1.3f, shadow, .8f);
            Stroke(vh, link, link.Ready ? 1.4f : .75f, tint, .85f);
            shadow.a = tint.a;
            Vector2 Snap(Vector2 p) => new(Mathf.Round(p.x / 2) * 2, Mathf.Round(p.y / 2) * 2);
            void Dot(Vector2 p, float size, Color c) => PrototypeFantasyTrim.Quad(vh, new Rect(p - Vector2.one * size * .5f, Vector2.one * size), c);
            void Rune(Vector2 center, int radius)
            {
                center = Snap(center);
                for (int row = -radius; row <= radius; row++)
                {
                    int width = radius - Mathf.Abs(row);
                    for (int col = -width; col <= width; col++)
                        Dot(center + new Vector2(col * 2, row * 2), 2,
                            Mathf.Abs(col) == width ? shadow : row > 0 ? highlight : tint);
                }
            }
            Rune(CurvePoint(link.From, link.To, .5f), link.Ready ? 4 : 3);
        }

        private static void Stroke(VertexHelper vh, Link link, float radius, Color tint, float feather)
        {
            float length = Vector2.Distance(link.From, link.To);
            if (length < .01f || tint.a <= 0f) return;
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / 6f), 8, 160);
            Color transparent = tint; transparent.a = 0f;
            int start = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector2 point = CurvePoint(link.From, link.To, t);
                Vector2 tangent = (CurvePoint(link.From, link.To, Mathf.Min(1f, t + .01f))
                    - CurvePoint(link.From, link.To, Mathf.Max(0f, t - .01f))).normalized;
                Vector2 normal = new(-tangent.y, tangent.x);
                vh.AddVert(point - normal * (radius + feather), transparent, Vector2.zero);
                vh.AddVert(point - normal * radius, tint, Vector2.zero);
                vh.AddVert(point + normal * radius, tint, Vector2.zero);
                vh.AddVert(point + normal * (radius + feather), transparent, Vector2.zero);
                if (i == 0) continue;
                for (int band = 0; band < 3; band++)
                {
                    int previous = start + (i - 1) * 4 + band, next = previous + 4;
                    vh.AddTriangle(previous, next, next + 1); vh.AddTriangle(previous, next + 1, previous + 1);
                }
            }
            Cap(vh, link.From, (link.From - CurvePoint(link.From, link.To, .01f)).normalized, radius, feather, tint);
            Cap(vh, link.To, (link.To - CurvePoint(link.From, link.To, .99f)).normalized, radius, feather, tint);
        }

        private static void Cap(VertexHelper vh, Vector2 center, Vector2 outward, float radius, float feather, Color tint)
        {
            Vector2 normal = new(-outward.y, outward.x);
            int start = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            Color transparent = tint; transparent.a = 0f;
            const int segments = 10;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -Mathf.PI * .5f + Mathf.PI * i / segments;
                Vector2 direction = outward * Mathf.Cos(angle) + normal * Mathf.Sin(angle);
                int inner = vh.currentVertCount;
                vh.AddVert(center + direction * radius, tint, Vector2.zero);
                vh.AddVert(center + direction * (radius + feather), transparent, Vector2.zero);
                if (i == 0) continue;
                vh.AddTriangle(start, inner - 2, inner);
                vh.AddTriangle(inner - 2, inner - 1, inner + 1); vh.AddTriangle(inner - 2, inner + 1, inner);
            }
        }
    }
}
