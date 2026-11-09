using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypePromotionScreen
    {
        private readonly RectTransform _root, _content;
        private readonly ScrollRect _scroll;
        private readonly Transform _resultSlot;
        private readonly Button[] _materialSlots = new Button[2];
        private readonly Button _confirm;
        private readonly Text _count, _error;
        private readonly BackpackPrototypeController _controller;
        private readonly Dictionary<string, Button> _tiles = new();
        private readonly HashSet<string> _eligible = new(StringComparer.Ordinal);
        private readonly List<string> _selected = new();
        private string _centerId;
        internal bool IsOpen => _root.gameObject.activeSelf;

        internal PrototypePromotionScreen(Transform parent, BackpackPrototypeController controller, Action close)
        {
            _controller = controller;
            var backdrop = SelectionUi.Image(parent, "Promotion Material Picker", new Color(0, 0, 0, .72f));
            _root = backdrop.rectTransform; BackpackPrototypeController.Stretch(_root); backdrop.raycastTarget = true;
            var panel = SelectionUi.Image(_root, "Promotion Material Panel", new Color(.10f, .13f, .19f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(680, 630));
            SelectionUi.Label(panel.transform, "Promotion Material Title", "승급 재료로 사용할 아이템을 선택해 주세요", 23,
                new Vector2(0, 270), new Vector2(630, 40));
            _resultSlot = SelectionUi.Image(panel.transform, "Promotion Result Slot", SelectionUi.Idle).transform;
            SelectionUi.Place((RectTransform)_resultSlot, new Vector2(-233, 166), Vector2.one * 100);
            SelectionUi.Label(panel.transform, "Promotion Result Caption", "승급 결과", 15, new Vector2(-233, 99), new Vector2(120, 24));
            SelectionUi.Label(panel.transform, "Promotion Direction", "←", 55, new Vector2(-72, 165), new Vector2(90, 60));
            for (int i = 0; i < _materialSlots.Length; i++)
            {
                int index = i;
                _materialSlots[i] = SelectionUi.Button(panel.transform, "Promotion Selected Material " + i, "+",
                    new Vector2(79 + i * 132, 166), Vector2.one * 100, () =>
                    {
                        if (index < _selected.Count) ToggleMaterial(_selected[index]);
                    });
            }
            _count = SelectionUi.Label(panel.transform, "Promotion Material Count", "", 15, new Vector2(144, 99), new Vector2(264, 24));

            var viewport = SelectionUi.Image(panel.transform, "Promotion Inventory Viewport", new Color(.065f, .08f, .115f));
            SelectionUi.Place(viewport.rectTransform, new Vector2(-7, -67), new Vector2(622, 306));
            viewport.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Promotion Inventory", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _content = (RectTransform)content.transform;
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = Vector2.one; _content.pivot = new Vector2(.5f, 1);
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport.rectTransform; _scroll.content = _content; _scroll.horizontal = false;
            _scroll.scrollSensitivity = 36; _scroll.movementType = ScrollRect.MovementType.Clamped;
            var track = SelectionUi.Image(panel.transform, "Promotion Scrollbar", new Color(.06f, .07f, .10f));
            SelectionUi.Place(track.rectTransform, new Vector2(315, -67), new Vector2(12, 306));
            var handle = SelectionUi.Image(track.transform, "Handle", new Color(.42f, .50f, .61f));
            BackpackPrototypeController.Stretch(handle.rectTransform);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;
            track.raycastTarget = handle.raycastTarget = true;
            _scroll.verticalScrollbar = bar; _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            _error = SelectionUi.Label(panel.transform, "Promotion Error", "", 14, new Vector2(0, -237), new Vector2(620, 25));
            _error.color = new Color(1, .45f, .4f);
            SelectionUi.Button(panel.transform, "Cancel Promotion Materials", "취소", new Vector2(-140, -278), new Vector2(236, 44), close);
            _confirm = SelectionUi.Button(panel.transform, "Confirm Promotion Materials", "승급", new Vector2(140, -278),
                new Vector2(236, 44), () =>
                {
                    if (_selected.Count == 2) _controller.TryPromoteUniqueGear(_centerId, _selected.ToArray());
                });
            _confirm.image.color = new Color(.60f, .43f, .78f);
            _confirm.gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        internal void Show(UniqueGearInstance center)
        {
            _centerId = center.InstanceId; _selected.Clear(); _tiles.Clear(); _eligible.Clear(); _error.text = "";
            foreach (var item in _controller.Progress.PromotionMaterials(_centerId)) _eligible.Add(item.InstanceId);
            foreach (Transform child in _content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            var items = _controller.Progress.UniqueGear.OrderByDescending(item => _eligible.Contains(item.InstanceId))
                .ThenBy(item => item.GearId, StringComparer.Ordinal).ThenBy(item => item.Enhancement)
                .ThenBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
            _content.sizeDelta = new Vector2(0, Mathf.Max(306, Mathf.CeilToInt(items.Length / 5f) * 118 + 12));
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var definition = _controller.FindGearDefinition(item.GearId);
                bool eligible = _eligible.Contains(item.InstanceId) && definition != null;
                var tile = SelectionUi.Button(_content, "Promotion Candidate " + item.InstanceId, "", Vector2.zero,
                    Vector2.one * 108, () => ToggleMaterial(item.InstanceId));
                BackpackPrototypeController.SetRect((RectTransform)tile.transform, new Vector2(.5f, 1),
                    new Vector2((i % 5 - 2) * 121, -61 - i / 5 * 118), Vector2.one * 108);
                PrototypeGearBadge.Render(tile.transform, definition, item);
                tile.interactable = eligible;
                tile.gameObject.AddComponent<CanvasGroup>().alpha = eligible ? 1 : .32f;
                if (!eligible)
                {
                    foreach (var image in tile.GetComponentsInChildren<Image>()) image.color = Color.gray;
                    var icon = tile.transform.Find("Gear Badge/Gear Sprite")?.GetComponent<Image>();
                    if (icon != null) icon.material = Resources.Load<Material>("UI/DisabledItem");
                }
                _tiles.Add(item.InstanceId, tile);
            }
            var gearDefinition = _controller.FindGearDefinition(center.GearId);
            PrototypeGearBadge.Render(_resultSlot, gearDefinition, center);
            var result = _resultSlot.Find("Gear Badge");
            result.Find("Gear Rank").GetComponent<Text>().text = ItemRarity.Uncommon.GetDisplayName();
            result.Find("Gear Rank").GetComponent<Text>().color = ItemRarity.Uncommon.GetDisplayColor();
            result.Find("Gear Enhancement").GetComponent<Text>().text = "+0";
            PrototypeGearBadge.RenderBorder(result, ItemRarity.Uncommon);
            _root.gameObject.SetActive(true); _root.SetAsLastSibling();
            _content.anchoredPosition = Vector2.zero; _scroll.verticalNormalizedPosition = 1;
            RefreshSelection();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ToggleMaterial(string id)
        {
            if (!_eligible.Contains(id)) return;
            if (!_selected.Remove(id) && _selected.Count < 2) _selected.Add(id);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (var pair in _tiles)
                pair.Value.image.color = _selected.Contains(pair.Key) ? new Color(.56f, .43f, .20f) : SelectionUi.Idle;
            for (int i = 0; i < _materialSlots.Length; i++)
            {
                var item = i < _selected.Count ? _controller.Progress.UniqueGear.FirstOrDefault(gear => gear.InstanceId == _selected[i]) : null;
                PrototypeGearBadge.Render(_materialSlots[i].transform, item == null ? null : _controller.FindGearDefinition(item.GearId), item);
                _materialSlots[i].GetComponentInChildren<Text>().text = item == null ? "+" : "";
            }
            _count.text = $"재료 {_selected.Count} / 2";
            _confirm.interactable = _selected.Count == 2;
            _confirm.GetComponent<CanvasGroup>().alpha = _confirm.interactable ? 1 : .38f;
        }

        internal void ShowError(string error) => _error.text = error ?? "";
        internal void Hide() => _root.gameObject.SetActive(false);
    }
}
