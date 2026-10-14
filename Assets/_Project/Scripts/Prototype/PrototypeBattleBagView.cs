using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeBattleBagView
    {
        private readonly RectTransform _root;
        private readonly Dictionary<string, PrototypeBattleItemView> _items = new();
        public RectTransform Root => _root;

        public PrototypeBattleBagView(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            _root = (RectTransform)obj.transform;
            BackpackPrototypeController.SetRect(_root, new Vector2(0.5f, 0.5f), new Vector2(0f, 117f), Vector2.zero);
        }

        public void Build(CombatLoadoutSnapshot loadout, CombatSide side, Action<CombatItemSnapshot> inspect,
            PrototypeItemTooltip tooltip, Func<CombatItemSnapshot, ItemTooltipContent> content)
        {
            foreach (Transform child in _root)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            _items.Clear();
            float pitch = Mathf.Min(50f, 228f / Mathf.Max(loadout.Width, loadout.Height));
            Vector2 boardSize = new(loadout.Width * pitch, loadout.Height * pitch);
            _root.sizeDelta = boardSize;
            for (int y = 0; y < loadout.Height; y++)
            {
                for (int x = 0; x < loadout.Width; x++)
                {
                    var cellObject = new GameObject($"Battle Grid Cell {x},{y}", typeof(RectTransform), typeof(Image));
                    cellObject.transform.SetParent(_root, false);
                    Image cell = cellObject.GetComponent<Image>();
                    cell.color = (x + y) % 2 == 0 ? new Color(0.16f, 0.20f, 0.27f) : new Color(0.20f, 0.25f, 0.32f);
                    cell.raycastTarget = false;
                    BackpackPrototypeController.SetRect(cell.rectTransform, new Vector2(0.5f, 0.5f),
                        new Vector2(-boardSize.x / 2f + (x + 0.5f) * pitch, boardSize.y / 2f - (y + 0.5f) * pitch), Vector2.one * (pitch - 6f));
                }
            }

            foreach (CombatItemSnapshot item in loadout.Items)
            {
                var obj = new GameObject(item.DisplayName, typeof(RectTransform), typeof(PrototypeBattleItemView));
                obj.transform.SetParent(_root, false);
                RectTransform rect = (RectTransform)obj.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-boardSize.x / 2f + item.Origin.x * pitch + 3f,
                    boardSize.y / 2f - item.Origin.y * pitch - 3f);
                PrototypeBattleItemView view = obj.GetComponent<PrototypeBattleItemView>();
                view.Initialize(item, side, pitch, inspect, tooltip, () => content(item));
                _items.Add(item.InstanceId, view);
            }
        }

        public void RenderCooldowns(IReadOnlyList<CombatWeapon> weapons, double elapsed)
        {
            foreach (CombatWeapon weapon in weapons)
                if (_items.TryGetValue(weapon.InstanceId, out var view)) view.RenderCooldown(weapon, elapsed);
        }

        public void ShowAttack(CombatAttackEvent attack)
        {
            if (_items.TryGetValue(attack.InstanceId, out var view)) view.PulseAttack(attack.Hit, attack.Critical);
        }
    }
}
