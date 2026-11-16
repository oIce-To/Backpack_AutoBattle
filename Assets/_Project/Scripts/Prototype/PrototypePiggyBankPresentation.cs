using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypePiggyBankPresentation : IDisposable
    {
        private const float Duration = .75f;
        private static readonly Color Gold = new(1f, .84f, .34f);
        private readonly RectTransform _root;
        private readonly List<(RectTransform item, Vector2 position, Vector3 scale, Text label)> _banks = new();
        private float _elapsed;

        internal PrototypePiggyBankPresentation(RectTransform shop, IEnumerable<BackpackItemView> banks)
        {
            var blocker = SelectionUi.Image(shop, "Piggy Bank Reward", Color.clear);
            blocker.raycastTarget = true;
            _root = blocker.rectTransform;
            BackpackPrototypeController.Stretch(_root);
            foreach (var bank in banks)
            {
                RectTransform item = bank.RectTransform;
                var label = SelectionUi.Label(_root, "Piggy Bank Gold", "+1G", 19, Vector2.zero, new Vector2(60f, 24f));
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.12f, .08f, .04f, .95f);
                outline.effectDistance = new Vector2(1f, -1f);
                _banks.Add((item, item.anchoredPosition, item.localScale, label));
            }
            Render();
        }

        internal bool Tick(float deltaSeconds)
        {
            _elapsed = Mathf.Min(_elapsed + deltaSeconds, Duration);
            Render();
            return _elapsed >= Duration;
        }

        private void Render()
        {
            float progress = _elapsed / Duration;
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .23f, progress));
            float shrink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.23f, .60f, progress));
            float pulse = 1f + .18f * grow * (1f - shrink);
            Color tint = Gold;
            tint.a = 1f - Mathf.InverseLerp(.64f, 1f, progress);
            foreach (var bank in _banks)
            {
                if (bank.item == null) continue;
                bank.item.localScale = bank.scale * pulse;
                Vector2 center = Vector3.Scale(bank.item.rect.center, bank.scale);
                bank.item.anchoredPosition = bank.position + center * (1f - pulse);
                Rect rect = bank.item.rect;
                Vector3 top = bank.item.TransformPoint(new Vector2(rect.center.x, rect.yMax));
                bank.label.rectTransform.anchoredPosition = (Vector2)_root.InverseTransformPoint(top)
                    + Vector2.up * Mathf.Lerp(10f, 24f, progress);
                bank.label.color = tint;
            }
        }

        public void Dispose()
        {
            foreach (var bank in _banks)
            {
                if (bank.item == null) continue;
                bank.item.anchoredPosition = bank.position;
                bank.item.localScale = bank.scale;
            }
            _root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_root.gameObject);
        }
    }
}
