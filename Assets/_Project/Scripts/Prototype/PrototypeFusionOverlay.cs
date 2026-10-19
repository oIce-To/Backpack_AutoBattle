using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeFusionOverlay : MaskableGraphic
    {
        public readonly struct Link
        {
            public readonly Vector2 From, To;
            public readonly bool Ready;
            public Link(Vector2 from, Vector2 to, bool ready) { From = from; To = to; Ready = ready; }
        }
        private readonly List<Link> _links = new();
        private readonly List<Text> _labels = new();
        private readonly List<PrototypeShopLockIcon> _locks = new();
        private int _labelCount, _lockCount;
        public int ReadyLinkCount { get; private set; }
        public int HintLinkCount { get; private set; }
        public PrototypeFusionOverlay() { useLegacyMeshGeneration = false; }
        public void Begin()
        { _links.Clear(); _labelCount = _lockCount = ReadyLinkCount = HintLinkCount = 0; }
        public void AddLink(Vector2 from, Vector2 to, bool ready)
        {
            _links.Add(new Link(from, to, ready));
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
        public void AddLock(Vector2 position)
        {
            if (_lockCount == _locks.Count)
            {
                var obj = new GameObject("Fusion Lock", typeof(RectTransform), typeof(PrototypeShopLockIcon));
                obj.transform.SetParent(transform, false);
                var icon = obj.GetComponent<PrototypeShopLockIcon>();
                icon.raycastTarget = false; icon.color = new Color(1f, .86f, .25f);
                _locks.Add(icon);
            }
            var badge = _locks[_lockCount++];
            badge.gameObject.SetActive(true);
            BackpackPrototypeController.SetRect(badge.rectTransform, Vector2.one * .5f, position, new Vector2(18, 22));
        }
        public void End()
        {
            for (int i = _labelCount; i < _labels.Count; i++) _labels[i].gameObject.SetActive(false);
            for (int i = _lockCount; i < _locks.Count; i++) _locks[i].gameObject.SetActive(false);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var link in _links)
            {
                Vector2 delta = link.To - link.From;
                if (delta.sqrMagnitude < .01f) continue;
                Vector2 side = new Vector2(-delta.y, delta.x).normalized * (link.Ready ? 2.5f : 1f);
                Color tint = link.Ready ? new Color(1f, .85f, .08f, 1f) : new Color(.68f, .71f, .76f, .7f);
                int start = vh.currentVertCount;
                vh.AddVert(link.From - side, tint, Vector2.zero); vh.AddVert(link.From + side, tint, Vector2.zero);
                vh.AddVert(link.To + side, tint, Vector2.zero); vh.AddVert(link.To - side, tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
