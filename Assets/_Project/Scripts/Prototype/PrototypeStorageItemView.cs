using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeStorageItemView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private BackpackPrototypeController _controller;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        public PrototypeStoredItem StoredItem { get; private set; }

        public void Initialize(BackpackPrototypeController controller, PrototypeStoredItem storedItem)
        {
            _controller = controller;
            StoredItem = storedItem;
            gameObject.name = $"Stored {storedItem.ItemData.DisplayName}";
            _rectTransform = (RectTransform)transform;
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(68f, 54f);
            _rectTransform.anchoredPosition = storedItem.StoragePosition;

            gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 0.98f);
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image rarityBar = CreateImage("Rarity", storedItem.ItemData.Rarity.GetDisplayColor());
            RectTransform rarityRect = rarityBar.rectTransform;
            rarityRect.anchorMin = new Vector2(0f, 0f);
            rarityRect.anchorMax = new Vector2(0f, 1f);
            rarityRect.pivot = new Vector2(0f, 0.5f);
            rarityRect.anchoredPosition = Vector2.zero;
            rarityRect.sizeDelta = new Vector2(5f, 0f);
            rarityBar.raycastTarget = false;

            Image itemColor = CreateImage("Item Color", storedItem.ItemData.DisplayColor);
            BackpackPrototypeController.SetRect(itemColor.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -13f), new Vector2(50f, 11f));
            itemColor.raycastTarget = false;

            Text label = BackpackPrototypeController.CreateText(transform, "Label", storedItem.ItemData.DisplayName, 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(5f, 1f);
            label.rectTransform.offsetMax = new Vector2(-2f, -16f);
        }

        public void SetPosition(Vector2 position)
        {
            StoredItem.StoragePosition = position;
            _rectTransform.anchoredPosition = position;
        }

        public void SetDragging(bool dragging)
        {
            _canvasGroup.alpha = dragging ? 0.30f : 1f;
            _canvasGroup.blocksRaycasts = !dragging;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.InspectStoredItem(StoredItem);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.BeginStorageDrag(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.DragStorageItem(this, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.EndStorageDrag(this, eventData);
            }
        }

        private Image CreateImage(string objectName, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
