using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeStorageItemView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler,
        IPointerClickHandler
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
            _rectTransform.anchoredPosition = storedItem.StoragePosition;
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            RefreshShape();
        }

        public void RefreshShape() => PrototypeItemShapeVisual.Rebuild(_rectTransform, StoredItem.ItemData, StoredItem.Rotation);

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

        public void OnPointerEnter(PointerEventData eventData) => _controller.ShowItemTooltip(this, StoredItem.ItemData, eventData);
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && !eventData.dragging
                && BackpackPrototypeController.FusionLockModifier) _controller.TryToggleFusionLock(StoredItem.InstanceId);
        }
        public void OnPointerMove(PointerEventData eventData) => _controller.MoveItemTooltip(this, eventData);
        public void OnPointerExit(PointerEventData eventData) => _controller.HideItemTooltip(this);
        private void OnDisable() { if (_controller != null) _controller.HideItemTooltip(this); }

    }
}
