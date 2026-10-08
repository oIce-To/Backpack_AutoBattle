using System.Collections.Generic;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeShopSlotView : MonoBehaviour
    {
        private readonly List<GameObject> _shapeCells = new();
        private BackpackPrototypeController _controller;
        private Image _colorBadge;
        private Text _itemText;
        private RectTransform _shapeRoot;

        public ItemData ItemData { get; private set; }

        public void Initialize(BackpackPrototypeController controller, int slotIndex)
        {
            _controller = controller;
            gameObject.name = $"Shop Slot {slotIndex + 1}";

            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.13f, 0.16f, 0.21f, 1f);

            var button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;
            button.onClick.AddListener(InspectItem);

            var badgeObject = new GameObject("Color Badge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(transform, false);
            _colorBadge = badgeObject.GetComponent<Image>();
            RectTransform badgeRect = _colorBadge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(8f, 0f);
            badgeRect.sizeDelta = new Vector2(6f, 58f);
            _colorBadge.raycastTarget = false;

            var shapeObject = new GameObject("Shape", typeof(RectTransform));
            shapeObject.transform.SetParent(transform, false);
            _shapeRoot = (RectTransform)shapeObject.transform;
            _shapeRoot.anchorMin = new Vector2(0f, 0.5f);
            _shapeRoot.anchorMax = new Vector2(0f, 0.5f);
            _shapeRoot.pivot = new Vector2(0.5f, 0.5f);
            _shapeRoot.anchoredPosition = new Vector2(46f, 0f);
            _shapeRoot.sizeDelta = new Vector2(58f, 58f);

            _itemText = BackpackPrototypeController.CreateText(
                transform,
                "Item Text",
                string.Empty,
                16,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);
            RectTransform textRect = _itemText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(82f, 5f);
            textRect.offsetMax = new Vector2(-8f, -5f);
            _itemText.raycastTarget = false;
        }

        public void Bind(ItemData itemData)
        {
            ItemData = itemData;
            _colorBadge.color = itemData.DisplayColor;
            _itemText.text = $"{itemData.DisplayName}\n<size=13>{GetCategoryName(itemData.Category)}  ·  {itemData.PurchasePrice} G</size>";
            RebuildShape(itemData);
        }

        private void InspectItem()
        {
            if (ItemData != null)
            {
                _controller.InspectShopItem(ItemData);
            }
        }

        private void RebuildShape(ItemData itemData)
        {
            foreach (GameObject shapeCell in _shapeCells)
            {
                if (shapeCell != null)
                {
                    shapeCell.SetActive(false);
                    Destroy(shapeCell);
                }
            }

            _shapeCells.Clear();
            IReadOnlyList<Vector2Int> cells = itemData.OccupiedCells;
            int width = 0;
            int height = 0;

            foreach (Vector2Int cell in cells)
            {
                width = Mathf.Max(width, cell.x + 1);
                height = Mathf.Max(height, cell.y + 1);
            }

            const float cellSize = 13f;
            const float cellPitch = 15f;
            Vector2 shapeOffset = new(
                -(width - 1) * cellPitch / 2f,
                (height - 1) * cellPitch / 2f);

            foreach (Vector2Int cell in cells)
            {
                var cellObject = new GameObject($"Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(_shapeRoot, false);
                var cellRect = (RectTransform)cellObject.transform;
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = Vector2.one * cellSize;
                cellRect.anchoredPosition = shapeOffset + new Vector2(cell.x * cellPitch, -cell.y * cellPitch);

                var image = cellObject.GetComponent<Image>();
                image.color = itemData.DisplayColor;
                image.raycastTarget = false;
                _shapeCells.Add(cellObject);
            }
        }

        private static string GetCategoryName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Weapon => "무기",
                ItemCategory.Armor => "방어구",
                ItemCategory.Accessory => "장신구",
                ItemCategory.Consumable => "소모품",
                ItemCategory.Material => "재료",
                ItemCategory.Pet => "펫",
                ItemCategory.Bag => "가방",
                _ => category.ToString()
            };
        }
    }
}
