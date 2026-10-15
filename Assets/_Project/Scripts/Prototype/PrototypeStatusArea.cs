using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Minimal outer-edge status column, independent of bags, characters and the HP row.</summary>
    public sealed class PrototypeStatusArea
    {
        private readonly List<PrototypeStatusIcon> _icons = new();
        private const float ViewportHeight = 488f;
        private const float Spacing = 12f;
        private readonly RectTransform _content;
        public RectTransform Root { get; }

        public PrototypeStatusArea(Transform parent, string side, bool leftEdge)
        {
            Root = Rect(parent, $"{side} Status Area");
            BackpackPrototypeController.SetRect(Root, new Vector2(leftEdge ? 0f : 1f, 0.5f),
                new Vector2(leftEdge ? 72f : -72f, -14f), new Vector2(64f, 500f));
            RectTransform viewport = Rect(Root, $"{side} Status Viewport");
            BackpackPrototypeController.Stretch(viewport, 6f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            _content = Rect(viewport, $"{side} Status Content");
            _content.anchorMin = new Vector2(0f, 1f); _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = new Vector2(0f, ViewportHeight);
            ScrollRect scroll = Root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = _content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = PrototypeStatusIcon.Size + Spacing;
        }

        public void Bind(CombatSimulation combat, CombatSide side, PrototypeItemTooltip tooltip)
        {
            foreach (PrototypeStatusIcon icon in _icons)
            { icon.gameObject.SetActive(false); UnityEngine.Object.Destroy(icon.gameObject); }
            _icons.Clear();
            foreach (StatusEffectKind kind in Enum.GetValues(typeof(StatusEffectKind)))
                _icons.Add(PrototypeStatusIcon.Create(_content, combat, side, kind, tooltip));
            _icons.Sort((a, b) => a.IsDebuff.CompareTo(b.IsDebuff));
            _content.anchoredPosition = Vector2.zero;
            Render();
        }

        public void Render()
        {
            int count = 0;
            foreach (PrototypeStatusIcon icon in _icons)
            {
                icon.Refresh();
                if (icon.Stacks == 0) continue;
                ((RectTransform)icon.transform).anchoredPosition = new Vector2(5f, -count * (PrototypeStatusIcon.Size + Spacing));
                count++;
            }
            float height = Mathf.Max(ViewportHeight, count * (PrototypeStatusIcon.Size + Spacing) - Spacing);
            _content.sizeDelta = new Vector2(0f, height);
            Vector2 position = _content.anchoredPosition;
            position.y = Mathf.Clamp(position.y, 0f, height - ViewportHeight);
            _content.anchoredPosition = position;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }
    }
}
