using System.Globalization;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeDamagePopups
    {
        private const int Capacity = 8;
        private const int PositionAttempts = 3;
        private const float Duration = .95f;
        internal static readonly Color HealingColor = new(.38f, 1f, .52f);

        private sealed class Slot
        {
            public PrototypeDamageNumber Graphic;
            public Vector2 Origin;
            public Vector2 Size;
            public double Started;
            public long Sequence;
            public Vector2 Drift;
            public Vector2 HalfBounds;
            public float Rise, Lifetime, Tilt;
            public bool Critical;
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

        public void ShowHealing(double restored) => Show(restored, HealingColor, healing: true);

        public void Show(double damage, Color tint, bool critical = false, bool healing = false)
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
                ? (healing ? "+<0.01" : "<0.01") : (healing ? "+" : "-") + damage.ToString("0.##", CultureInfo.InvariantCulture);
            slot.Size = slot.Graphic.SetNumber(value, critical, tint);
            slot.Critical = critical;
            slot.Rise = critical ? 54 : healing ? 44 : 38;
            slot.Lifetime = critical ? 1.1f : Duration;
            slot.HalfBounds = slot.Size * (critical ? .62f : .5f) + Vector2.one * (critical ? 8 : 2);
            slot.Origin = PickPosition(slot);
            float drift = ((float)_random.NextDouble() * 2 - 1) * (critical ? 26 : 18);
            slot.Drift = new Vector2(Mathf.Clamp(slot.Origin.x + drift,
                _root.rect.xMin + slot.HalfBounds.x, _root.rect.xMax - slot.HalfBounds.x) - slot.Origin.x, 0);
            slot.Tilt = (float)(_random.NextDouble() < .5 ? -8 : 8);
            slot.Started = now;
            slot.Sequence = ++_sequence;
            slot.Graphic.rectTransform.sizeDelta = slot.Size;
            slot.Graphic.rectTransform.anchoredPosition = slot.Origin;
            slot.Graphic.rectTransform.localScale = Vector3.one;
            slot.Graphic.rectTransform.localRotation = Quaternion.identity;
            slot.Graphic.transform.SetAsLastSibling();
            slot.Graphic.gameObject.SetActive(true);
            slot.Graphic.canvasRenderer.SetAlpha(1);
        }

        private Vector2 PickPosition(Slot incoming)
        {
            Rect bounds = _root.rect;
            Vector2 half = incoming.HalfBounds;
            Vector2 best = Vector2.zero;
            float bestScore = float.NegativeInfinity;
            for (int attempt = 0; attempt < PositionAttempts; attempt++)
            {
                Vector2 candidate = new(
                    Mathf.Round(Mathf.Lerp(bounds.xMin + half.x, bounds.xMax - half.x, (float)_random.NextDouble())),
                    Mathf.Round(Mathf.Lerp(bounds.yMin + half.y, bounds.yMax - half.y - incoming.Rise, (float)_random.NextDouble())));
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
                float progress = Mathf.Max(0, (float)((now - slot.Started) / slot.Lifetime));
                if (progress >= 1)
                {
                    slot.Graphic.gameObject.SetActive(false);
                    continue;
                }
                float eased = 1 - (1 - progress) * (1 - progress);
                slot.Graphic.rectTransform.anchoredPosition = slot.Origin + slot.Drift * eased + Vector2.up * (slot.Rise * eased);
                float punch = slot.Critical ? .22f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress / .28f)) : 0;
                slot.Graphic.rectTransform.localScale = Vector3.one * (1 + punch);
                float tilt = slot.Critical ? slot.Tilt * Mathf.Sin(progress * Mathf.PI * 4) * (1 - progress) * (1 - progress) : 0;
                slot.Graphic.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
                slot.Graphic.canvasRenderer.SetAlpha(1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.52f, 1, progress)));
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
