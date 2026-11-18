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
        private const int MaxSegments = 64;
        private readonly Vector2[] _cordPoints = new Vector2[MaxSegments + 1];
        private readonly Vector2[] _wispPoints = new Vector2[MaxSegments + 1];
        private int _labelCount;
        private Transform _hiddenItemName;
        private Transform _itemNameToHide;
        public int ReadyLinkCount { get; private set; }
        public int HintLinkCount { get; private set; }
        public PrototypeFusionOverlay() { useLegacyMeshGeneration = false; }
        public void Begin()
        {
            foreach (var link in _links) link.Requested = false;
            _labelCount = ReadyLinkCount = HintLinkCount = 0;
            _itemNameToHide = null;
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
            AddLabel(position + Vector2.down * 16f, new Vector2(180, 26), name, new Color(1f, .86f, .12f));
        }
        public void AddProgressLabel(RectTransform item, Vector2 position, string name, int prepared, long required)
        {
            _itemNameToHide = item.Find("Label");
            AddLabel(position + Vector2.down * 9f, new Vector2(200, 46), $"{name}\n{prepared} / {required}", Color.white);
        }
        private void AddLabel(Vector2 position, Vector2 size, string content, Color color)
        {
            if (_labelCount == _labels.Count)
            {
                var label = BackpackPrototypeController.CreateText(transform, "Fusion Result", "", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.raycastTarget = false;
                label.gameObject.AddComponent<Outline>().effectColor = new Color(.04f, .05f, .08f, .95f);
                _labels.Add(label);
            }
            var text = _labels[_labelCount++];
            text.gameObject.SetActive(true); text.text = content; text.color = color;
            BackpackPrototypeController.SetRect(text.rectTransform, Vector2.one * .5f, position, size);
        }
        public void End()
        {
            for (int i = _labelCount; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
            SetHiddenItemName(_itemNameToHide);
            foreach (var link in _links)
                link.Opacity = link.Ready && !link.Requested ? 0f
                    : Mathf.MoveTowards(link.Opacity, link.Requested ? 1f : 0f, Time.unscaledDeltaTime / .12f);
            _links.RemoveAll(link => !link.Requested && link.Opacity <= 0f);
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            SetHiddenItemName(null);
            base.OnDisable();
        }

        private void SetHiddenItemName(Transform itemName)
        {
            if (_hiddenItemName == itemName) return;
            if (_hiddenItemName != null) _hiddenItemName.gameObject.SetActive(true);
            _hiddenItemName = itemName;
            if (itemName != null) itemName.gameObject.SetActive(false);
        }

        public static Vector2 CurvePoint(Vector2 from, Vector2 to, float t)
        {
            Vector2 delta = to - from;
            return Vector2.Lerp(from, to, t) + BendNormal(delta) * (4f * t * (1f - t) * Bend(delta.magnitude));
        }

        private static Vector2 BendNormal(Vector2 delta)
        {
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            return normal.y < 0f || (Mathf.Approximately(normal.y, 0f) && normal.x < 0f) ? -normal : normal;
        }

        private static float Bend(float length) => Mathf.Min(length * .26f, Mathf.Clamp(length * .16f, 12f, 38f));

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float cycle = (float)(Time.unscaledTimeAsDouble % 3.4 / 3.4);
            foreach (var link in _links) SmoothCord(vh, link, cycle);
        }

        private void SmoothCord(VertexHelper vh, Link link, float cycle)
        {
            Vector2 delta = link.To - link.From;
            float length = delta.magnitude;
            if (length < 1f || link.Opacity <= 0f) return;

            Vector2 normal = BendNormal(delta);
            float bend = Bend(length);
            float amplitude = Mathf.Clamp01(length / 48f);
            int segments = Mathf.Clamp(Mathf.CeilToInt(length / 10f), 16, MaxSegments);
            _cordPoints[0] = _wispPoints[0] = link.From;
            _cordPoints[segments] = _wispPoints[segments] = link.To;
            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                float envelope = Mathf.Sin(Mathf.PI * t) * amplitude;
                float phase = 2f * Mathf.PI * (1.24f * t - cycle);
                float ripple = Mathf.Sin(phase) * (link.Ready ? 2.4f : 1.8f) + .6f * Mathf.Sin(2f * phase + .7f);
                float lift = 4f * t * (1f - t) * bend + envelope * ripple;
                _cordPoints[i] = link.From + delta * t + normal * lift;
                float separation = Mathf.Sin(phase + .95f) * (link.Ready ? 4.1f : 3.1f);
                _wispPoints[i] = _cordPoints[i] + normal * (envelope * separation);
            }

            bool gold = link.Ready;
            Color Tint(uint rgb, float alpha)
            {
                Color tint = PrototypePixelSkin.Hex(rgb);
                tint.a = link.Opacity * alpha;
                return tint;
            }

            Stroke(vh, _cordPoints, segments, gold ? 1.8f : .9f, Tint(gold ? 0xffb32fu : 0xd3e7eeu, gold ? .60f : .32f), gold ? 4.8f : 3.1f);
            Stroke(vh, _cordPoints, segments, gold ? 1.55f : .65f, Tint(gold ? 0xffbf48u : 0xccd8dcu, .89f), .5f);
            Stroke(vh, _cordPoints, segments, gold ? .6f : .25f, Tint(gold ? 0xfff1bau : 0xfffaf2u, 1f), gold ? .45f : .35f);
            Stroke(vh, _wispPoints, segments, gold ? .3f : .13f, Tint(gold ? 0xffd777u : 0xe2eff4u, gold ? .72f : .5f), .4f);
        }

        private static void Stroke(VertexHelper vh, Vector2[] points, int segments, float radius, Color tint, float feather)
        {
            Color transparent = tint; transparent.a = 0f;
            int start = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                Vector2 point = points[i];
                Vector2 tangent = (points[Mathf.Min(i + 1, segments)] - points[Mathf.Max(i - 1, 0)]).normalized;
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
            Cap(vh, points[0], (points[0] - points[1]).normalized, radius, feather, tint);
            Cap(vh, points[segments], (points[segments] - points[segments - 1]).normalized, radius, feather, tint);
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
