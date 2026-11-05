using System.Globalization;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeDamagePopups
    {
        private const int Capacity = 8;
        private const int PositionAttempts = 3;
        private const float Duration = .72f;
        private const float Rise = 22f;

        private sealed class Slot
        {
            public PrototypeDamageNumber Graphic;
            public Vector2 Origin;
            public Vector2 Size;
            public double Started;
            public long Sequence;
        }

        private readonly RectTransform _root;
        private readonly Slot[] _slots = new Slot[Capacity];
        private readonly System.Random _random = new();
        private long _sequence;

        public PrototypeDamagePopups(Transform parent, string name, Vector2 position, Vector2 size, Texture2D atlas)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            _root = (RectTransform)obj.transform;
            SelectionUi.Place(_root, position, size);
            for (int i = 0; i < _slots.Length; i++)
            {
                var popup = new GameObject($"Damage Popup {i + 1}", typeof(RectTransform), typeof(PrototypeDamageNumber));
                popup.transform.SetParent(_root, false);
                var graphic = popup.GetComponent<PrototypeDamageNumber>();
                graphic.Initialize(atlas);
                SelectionUi.Place(graphic.rectTransform, Vector2.zero, Vector2.zero);
                _slots[i] = new Slot { Graphic = graphic };
                popup.SetActive(false);
            }
        }

        public void Show(double damage, Color tint, bool critical = false)
        {
            if (!_root.gameObject.activeSelf || damage <= 0) return;
            double now = Time.unscaledTimeAsDouble;
            Tick(now);
            Slot slot = _slots[0];
            foreach (Slot candidate in _slots)
            {
                if (!candidate.Graphic.gameObject.activeSelf)
                {
                    slot = candidate;
                    break;
                }
                if (candidate.Sequence < slot.Sequence) slot = candidate;
            }

            string value = damage < .005
                ? "<0.01" : "-" + damage.ToString("0.##", CultureInfo.InvariantCulture);
            slot.Size = slot.Graphic.SetNumber(value, critical, tint);
            slot.Origin = PickPosition(slot);
            slot.Started = now;
            slot.Sequence = ++_sequence;
            slot.Graphic.rectTransform.sizeDelta = slot.Size;
            slot.Graphic.rectTransform.anchoredPosition = slot.Origin;
            slot.Graphic.transform.SetAsLastSibling();
            slot.Graphic.gameObject.SetActive(true);
            slot.Graphic.canvasRenderer.SetAlpha(1);
        }

        private Vector2 PickPosition(Slot incoming)
        {
            Rect bounds = _root.rect;
            Vector2 half = incoming.Size / 2;
            Vector2 best = Vector2.zero;
            float bestScore = float.NegativeInfinity;
            for (int attempt = 0; attempt < PositionAttempts; attempt++)
            {
                Vector2 candidate = new(
                    Mathf.Round(Mathf.Lerp(bounds.xMin + half.x, bounds.xMax - half.x, (float)_random.NextDouble())),
                    Mathf.Round(Mathf.Lerp(bounds.yMin + half.y, bounds.yMax - half.y - Rise, (float)_random.NextDouble())));
                float score = float.PositiveInfinity;
                foreach (Slot other in _slots)
                {
                    if (other == incoming || !other.Graphic.gameObject.activeSelf) continue;
                    Vector2 offset = candidate - other.Graphic.rectTransform.anchoredPosition;
                    Vector2 spacing = (incoming.Size + other.Size) / 2 + new Vector2(5, 4);
                    float clearance = Mathf.Max(Mathf.Abs(offset.x) / spacing.x, Mathf.Abs(offset.y) / spacing.y);
                    score = Mathf.Min(score, clearance);
                }
                if (score >= 1) return candidate;
                if (score <= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        public void Tick(double now)
        {
            foreach (Slot slot in _slots)
            {
                if (!slot.Graphic.gameObject.activeSelf) continue;
                float progress = (float)((now - slot.Started) / Duration);
                if (progress >= 1)
                {
                    slot.Graphic.gameObject.SetActive(false);
                    continue;
                }
                slot.Graphic.rectTransform.anchoredPosition = slot.Origin + Vector2.up * Mathf.Round(Rise * progress);
                slot.Graphic.canvasRenderer.SetAlpha(1 - Mathf.InverseLerp(.62f, 1, progress));
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root.gameObject.activeSelf == visible) return;
            Clear();
            _root.gameObject.SetActive(visible);
        }

        public void Clear()
        {
            foreach (Slot slot in _slots) slot.Graphic.gameObject.SetActive(false);
            _sequence = 0;
        }
    }
}
