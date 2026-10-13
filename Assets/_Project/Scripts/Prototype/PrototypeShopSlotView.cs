using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeShopSlotView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private readonly List<GameObject> _shapeCells = new();
        private BackpackPrototypeController _controller;
        private Image _rarityBadge;
        private Text _itemText;
        private RectTransform _shapeRoot;
        private Text _originalPriceText;
        private Text _priceText;
        private Image _priceStrike;

        public ShopOffer Offer { get; private set; }
        public ItemData ItemData => Offer?.ItemData;
        public bool CanPurchase => Offer != null && !Offer.IsPurchased;
        public int PurchasePrice => Offer?.PurchasePrice ?? 0;

        public void Initialize(BackpackPrototypeController controller, int slotIndex)
        {
            _controller = controller;
            gameObject.name = $"Shop Slot {slotIndex + 1}";
            gameObject.AddComponent<Image>().color = new Color(0.13f, 0.16f, 0.21f, 1f);

            var badgeObject = new GameObject("Rarity Badge", typeof(RectTransform), typeof(Image));
            badgeObject.transform.SetParent(transform, false);
            _rarityBadge = badgeObject.GetComponent<Image>();
            RectTransform badgeRect = _rarityBadge.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 0f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(6f, 0f);
            _rarityBadge.raycastTarget = false;

            var shapeObject = new GameObject("Shape", typeof(RectTransform));
            shapeObject.transform.SetParent(transform, false);
            _shapeRoot = (RectTransform)shapeObject.transform;
            BackpackPrototypeController.SetRect(_shapeRoot, new Vector2(0f, 0.5f), new Vector2(47f, 0f), new Vector2(58f, 58f));

            _itemText = BackpackPrototypeController.CreateText(transform, "Item Text", string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
            RectTransform textRect = _itemText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(82f, 22f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            _itemText.raycastTarget = false;

            _originalPriceText = BackpackPrototypeController.CreateText(transform, "Original Price", string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetPriceRect(_originalPriceText.rectTransform, 82f, 56f);
            _originalPriceText.raycastTarget = false;

            var strikeObject = new GameObject("Price Strike", typeof(RectTransform), typeof(Image));
            strikeObject.transform.SetParent(_originalPriceText.transform, false);
            _priceStrike = strikeObject.GetComponent<Image>();
            _priceStrike.color = new Color(1f, 0.28f, 0.28f, 1f);
            _priceStrike.raycastTarget = false;
            BackpackPrototypeController.SetRect(_priceStrike.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(36f, 2f));
            _priceStrike.rectTransform.pivot = new Vector2(0f, 0.5f);

            _priceText = BackpackPrototypeController.CreateText(transform, "Purchase Price", string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetPriceRect(_priceText.rectTransform, 82f, 188f);
            _priceText.raycastTarget = false;
        }

        public void Bind(ShopOffer offer)
        {
            Offer = offer;
            ItemData itemData = offer.ItemData;
            _shapeRoot.gameObject.SetActive(true);
            _rarityBadge.gameObject.SetActive(true);
            _rarityBadge.color = itemData.Rarity.GetDisplayColor();
            string rarityColor = ColorUtility.ToHtmlStringRGB(itemData.Rarity.GetDisplayColor());
            _itemText.text = $"{itemData.DisplayName}\n<size=12><color=#{rarityColor}>{itemData.Rarity.GetDisplayName()}</color></size>";

            _originalPriceText.gameObject.SetActive(offer.IsOnSale);
            _originalPriceText.text = $"{offer.OriginalPrice} G";
            float originalWidth = _originalPriceText.preferredWidth;
            _priceStrike.rectTransform.sizeDelta = new Vector2(originalWidth, 2f);
            SetPriceRect(_originalPriceText.rectTransform, 82f, originalWidth);
            float priceX = offer.IsOnSale ? 82f + originalWidth + 9f : 82f;
            SetPriceRect(_priceText.rectTransform, priceX, 270f - priceX);
            _priceText.gameObject.SetActive(true);
            _priceText.color = offer.IsOnSale ? new Color(1f, 0.28f, 0.28f, 1f) : Color.white;
            _priceText.text = offer.IsOnSale ? $"{offer.PurchasePrice} G  Sale!" : $"{offer.PurchasePrice} G";
            RebuildShape(itemData);
        }

        public void ShowPurchased()
        {
            _shapeRoot.gameObject.SetActive(false);
            _rarityBadge.gameObject.SetActive(false);
            _originalPriceText.gameObject.SetActive(false);
            _priceText.gameObject.SetActive(false);
            _itemText.text = "<color=#8995A6>구매 완료</color>";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CanPurchase && eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.InspectShopItem(Offer);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.BeginShopDrag(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.DragShopItem(this, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.EndShopDrag(this, eventData);
            }
        }

        private void RebuildShape(ItemData itemData)
        {
            foreach (GameObject shapeCell in _shapeCells)
            {
                if (shapeCell != null)
                {
                    Destroy(shapeCell);
                }
            }

            _shapeCells.Clear();
            IReadOnlyList<Vector2Int> cells = itemData.OccupiedCells;
            int width = cells.Max(cell => cell.x) + 1;
            int height = cells.Max(cell => cell.y) + 1;
            const float cellSize = 12f;
            const float cellPitch = 14f;
            Vector2 offset = new(-(width - 1) * cellPitch / 2f, (height - 1) * cellPitch / 2f);

            foreach (Vector2Int cell in cells)
            {
                var cellObject = new GameObject($"Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(_shapeRoot, false);
                BackpackPrototypeController.SetRect(
                    (RectTransform)cellObject.transform,
                    new Vector2(0.5f, 0.5f),
                    offset + new Vector2(cell.x * cellPitch, -cell.y * cellPitch),
                    Vector2.one * cellSize);
                Image image = cellObject.GetComponent<Image>();
                image.color = itemData.DisplayColor;
                image.raycastTarget = false;
                _shapeCells.Add(cellObject);
            }
        }

        private static void SetPriceRect(RectTransform rect, float x, float width)
        {
            BackpackPrototypeController.SetRect(rect, Vector2.zero, new Vector2(x, 11f), new Vector2(width, 20f));
            rect.pivot = new Vector2(0f, 0.5f);
        }
    }
}
