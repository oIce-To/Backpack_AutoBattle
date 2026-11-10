using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeDismantleScreen
    {
        private readonly BackpackPrototypeController _controller;
        private readonly RectTransform _root, _content, _rewards;
        private readonly ScrollRect _scroll, _rewardScroll;
        private readonly Text _count, _error, _shards;
        private readonly Button _confirm;
        private readonly HashSet<string> _selected = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (Button tile, UniqueGearInstance gear, GearDismantleValue value)> _items = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (RectTransform row, Text amount, string name)> _blueprintRows = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _blueprintTotals = new(StringComparer.Ordinal);
        private long _totalShards;
        internal bool IsOpen => _root.gameObject.activeSelf;

        internal PrototypeDismantleScreen(Transform canvas, BackpackPrototypeController controller, Action close)
        {
            _controller = controller;
            var backdrop = SelectionUi.Image(canvas, "Gear Dismantle Screen", new Color(0, 0, 0, .72f));
            _root = backdrop.rectTransform; BackpackPrototypeController.Stretch(_root); backdrop.raycastTarget = true;
            var panel = SelectionUi.Image(_root, "Gear Dismantle Panel", new Color(.10f, .13f, .19f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(680, 630));
            SelectionUi.Label(panel.transform, "Dismantle Title", "분해할 장비를 선택해 주세요", 25, new Vector2(0, 270), new Vector2(620, 42));
            _count = SelectionUi.Label(panel.transform, "Dismantle Selection Count", "", 16, new Vector2(0, 230), new Vector2(620, 26));

            var viewport = SelectionUi.Image(panel.transform, "Dismantle Inventory Viewport", new Color(.065f, .08f, .115f));
            SelectionUi.Place(viewport.rectTransform, new Vector2(-7, 43), new Vector2(622, 330));
            _scroll = ScrollArea(viewport, "Dismantle Inventory", out _content);
            var track = SelectionUi.Image(panel.transform, "Dismantle Scrollbar", new Color(.06f, .07f, .10f));
            SelectionUi.Place(track.rectTransform, new Vector2(315, 43), new Vector2(12, 330));
            var handle = SelectionUi.Image(track.transform, "Handle", new Color(.42f, .50f, .61f));
            BackpackPrototypeController.Stretch(handle.rectTransform);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;
            track.raycastTarget = handle.raycastTarget = true;
            _scroll.verticalScrollbar = bar; _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            var rewardViewport = SelectionUi.Image(panel.transform, "Dismantle Rewards Viewport", Color.clear);
            SelectionUi.Place(rewardViewport.rectTransform, new Vector2(-7, -182), new Vector2(622, 96));
            _rewardScroll = ScrollArea(rewardViewport, "Dismantle Rewards", out _rewards);
            _shards = RewardRow("Dismantle Shard Reward", EconomyIconKind.OriginShard).amount;
            _error = SelectionUi.Label(panel.transform, "Dismantle Error", "", 14, new Vector2(0, -245), new Vector2(620, 20));
            _error.color = new Color(1, .45f, .4f);
            SelectionUi.Button(panel.transform, "Cancel Gear Dismantle", "취소", new Vector2(-140, -278), new Vector2(236, 44), close);
            _confirm = SelectionUi.Button(panel.transform, "Confirm Gear Dismantle", "분해", new Vector2(140, -278), new Vector2(236, 44),
                () => { if (_confirm.interactable) _controller.TryDismantleUniqueGear(_selected.ToArray()); });
            _confirm.image.color = new Color(.72f, .28f, .27f);
            _confirm.gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        private static ScrollRect ScrollArea(Image viewport, string name, out RectTransform content)
        {
            viewport.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(viewport.transform, false);
            content = (RectTransform)obj.transform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform; scroll.content = content; scroll.horizontal = false;
            scroll.scrollSensitivity = 36; scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private (RectTransform row, Text amount) RewardRow(string name, EconomyIconKind kind)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(_rewards, false);
            var row = (RectTransform)obj.transform;
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon)); icon.transform.SetParent(row, false);
            var glyph = icon.GetComponent<PrototypeEconomyIcon>(); glyph.SetKind(kind); glyph.raycastTarget = false;
            SelectionUi.Place(glyph.rectTransform, new Vector2(-288, 0), new Vector2(24, 24));
            var amount = SelectionUi.Label(row, name + " Amount", "", 16, new Vector2(10, 0), new Vector2(546, 30));
            amount.alignment = TextAnchor.MiddleLeft;
            return (row, amount);
        }

        internal void Show()
        {
            _selected.Clear(); _items.Clear(); _totalShards = 0; _blueprintTotals.Clear(); _error.text = "";
            foreach (Transform child in _content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            foreach (var row in _blueprintRows.Values) { row.row.gameObject.SetActive(false); UnityEngine.Object.Destroy(row.row.gameObject); }
            _blueprintRows.Clear();
            var items = _controller.Progress.UniqueGear.OrderByDescending(PlayerProgress.CanDismantleGear)
                .ThenBy(item => item.GearId, StringComparer.Ordinal).ThenBy(item => item.Rarity)
                .ThenBy(item => item.Enhancement).ThenBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
            _content.sizeDelta = new Vector2(0, Mathf.Max(330, Mathf.CeilToInt(items.Length / 5f) * 118 + 12));
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i]; var definition = _controller.FindGearDefinition(item.GearId);
                bool eligible = PlayerProgress.CanDismantleGear(item);
                var tile = SelectionUi.Button(_content, "Dismantle Candidate " + item.InstanceId, "", Vector2.zero,
                    Vector2.one * 108, () => Toggle(item.InstanceId));
                BackpackPrototypeController.SetRect((RectTransform)tile.transform, new Vector2(.5f, 1),
                    new Vector2((i % 5 - 2) * 121, -61 - i / 5 * 118), Vector2.one * 108);
                PrototypeGearBadge.Render(tile.transform, definition, item);
                tile.interactable = eligible; tile.gameObject.AddComponent<CanvasGroup>().alpha = eligible ? 1 : .32f;
                if (!eligible)
                {
                    foreach (var image in tile.GetComponentsInChildren<Image>()) image.color = Color.gray;
                    var sprite = tile.transform.Find("Gear Badge/Gear Sprite")?.GetComponent<Image>();
                    if (sprite != null) sprite.material = Resources.Load<Material>("UI/DisabledItem");
                }
                _items.Add(item.InstanceId, (tile, item, PlayerProgress.DismantleValue(item)));
                if (!_blueprintRows.ContainsKey(item.GearId))
                {
                    var row = RewardRow("Dismantle Blueprint " + item.GearId, EconomyIconKind.Blueprint);
                    _blueprintRows.Add(item.GearId, (row.row, row.amount, definition?.Item.DisplayName ?? item.GearId));
                }
            }
            _root.gameObject.SetActive(true); _root.SetAsLastSibling();
            _content.anchoredPosition = Vector2.zero; _scroll.verticalNormalizedPosition = 1;
            _rewards.anchoredPosition = Vector2.zero; _rewardScroll.verticalNormalizedPosition = 1;
            RefreshSelection(); EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Toggle(string id)
        {
            if (!_items.TryGetValue(id, out var item) || !item.tile.interactable) return;
            int sign = _selected.Remove(id) ? -1 : 1;
            if (sign == 1) _selected.Add(id);
            _totalShards += sign * item.value.OriginShards;
            _blueprintTotals.TryGetValue(item.gear.GearId, out long count);
            _blueprintTotals[item.gear.GearId] = count + sign * item.value.Blueprints;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (var item in _items)
                item.Value.tile.image.color = _selected.Contains(item.Key) ? new Color(.56f, .43f, .20f) : SelectionUi.Idle;
            _count.text = $"선택 {_selected.Count}개";
            _shards.text = $"기원의 파편 {CompactNumber.Exact(_totalShards)}";
            PlaceReward((RectTransform)_shards.transform.parent, 0);
            int index = 1;
            foreach (var pair in _blueprintRows)
            {
                _blueprintTotals.TryGetValue(pair.Key, out long count);
                pair.Value.row.gameObject.SetActive(count > 0);
                if (count == 0) continue;
                pair.Value.amount.text = $"{pair.Value.name} 설계도 {CompactNumber.Exact(count)}";
                PlaceReward(pair.Value.row, index++);
            }
            _rewards.sizeDelta = new Vector2(0, Mathf.Max(96, index * 32));
            _confirm.interactable = _selected.Count > 0;
            _confirm.GetComponent<CanvasGroup>().alpha = _confirm.interactable ? 1 : .35f;
        }

        private static void PlaceReward(RectTransform row, int index)
            => BackpackPrototypeController.SetRect(row, new Vector2(.5f, 1), new Vector2(0, -16 - index * 32), new Vector2(622, 30));
        internal void ShowError(string error) => _error.text = error ?? "";
        internal void Hide() => _root.gameObject.SetActive(false);
    }
}
