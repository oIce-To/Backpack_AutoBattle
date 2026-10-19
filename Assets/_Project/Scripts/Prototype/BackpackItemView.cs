using System.Collections.Generic;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class BackpackItemView : MonoBehaviour,
        IPointerDownHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler
    {
        private readonly List<Image> _cellImages = new();
        private BackpackPrototypeController _controller;
        private Color _baseColor;

        public string InstanceId { get; private set; }

        public string DisplayName { get; private set; }

        public ItemData Data { get; private set; }

        public ItemShape Shape { get; private set; }

        public ItemRotation Rotation { get; private set; }

        public Vector2Int Origin { get; private set; }

        public RectTransform RectTransform { get; private set; }

        public void Initialize(
            BackpackPrototypeController controller,
            string instanceId,
            ItemData data,
            Vector2Int origin,
            ItemRotation rotation)
        {
            _controller = controller;
            InstanceId = instanceId;
            Data = data;
            DisplayName = data.DisplayName;
            Shape = data.CreateShape();
            Origin = origin;
            Rotation = rotation;
            _baseColor = data.DisplayColor;
            RectTransform = (RectTransform)transform;
            RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform.pivot = new Vector2(0f, 1f);

            RebuildVisual();
        }

        public void SetPlacement(Vector2Int origin, ItemRotation rotation)
        {
            Origin = origin;
            Rotation = rotation;
            RebuildVisual();
            _controller.PositionItem(this);
        }

        public void SetPreviewRotation(ItemRotation rotation)
        {
            Rotation = rotation;
            RebuildVisual();
        }

        public void SetTint(Color color)
        {
            foreach (Image cellImage in _cellImages)
            {
                cellImage.color = color;
            }
        }

        public void ResetTint()
        {
            SetTint(_baseColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _controller.SelectItem(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && !eventData.dragging)
            {
                if (BackpackPrototypeController.FusionLockModifier) _controller.TryToggleFusionLock(InstanceId);
                else _controller.RotateItem(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.BeginDrag(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.Drag(this, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.EndDrag(this, eventData);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => _controller.ShowItemTooltip(this, Data, eventData);
        public void OnPointerMove(PointerEventData eventData) => _controller.MoveItemTooltip(this, eventData);
        public void OnPointerExit(PointerEventData eventData) => _controller.HideItemTooltip(this);
        private void OnDisable() { if (_controller != null) _controller.HideItemTooltip(this); }

        private void RebuildVisual()
        {
            _cellImages.Clear();
            _cellImages.AddRange(PrototypeItemShapeVisual.Rebuild(RectTransform, Data, Rotation));
        }
    }
}
