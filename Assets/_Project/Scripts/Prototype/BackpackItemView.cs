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
        IEndDragHandler
    {
        private readonly List<Image> _cellImages = new();
        private BackpackPrototypeController _controller;
        private Color _baseColor;
        private Text _label;

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
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _controller.RotateItem(this);
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
                _controller.EndDrag(this);
            }
        }

        private void RebuildVisual()
        {
            foreach (Image cellImage in _cellImages)
            {
                if (cellImage != null)
                {
                    Destroy(cellImage.gameObject);
                }
            }

            _cellImages.Clear();

            if (_label != null)
            {
                Destroy(_label.gameObject);
            }

            IReadOnlyList<Vector2Int> cells = Shape.GetCells(Rotation);
            int width = 0;
            int height = 0;

            foreach (Vector2Int cell in cells)
            {
                width = Mathf.Max(width, cell.x + 1);
                height = Mathf.Max(height, cell.y + 1);

                var cellObject = new GameObject($"Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                var cellRect = (RectTransform)cellObject.transform;
                cellRect.SetParent(transform, false);
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0f, 1f);
                cellRect.anchoredPosition = new Vector2(
                    cell.x * BackpackPrototypeController.CellPitch,
                    -cell.y * BackpackPrototypeController.CellPitch);
                cellRect.sizeDelta = Vector2.one * BackpackPrototypeController.ItemCellSize;

                var image = cellObject.GetComponent<Image>();
                image.color = _baseColor;
                _cellImages.Add(image);
            }

            RectTransform.sizeDelta = new Vector2(
                width * BackpackPrototypeController.CellPitch,
                height * BackpackPrototypeController.CellPitch);

            _label = BackpackPrototypeController.CreateText(
                transform,
                "Label",
                DisplayName,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            _label.color = Color.white;
            _label.raycastTarget = false;
            RectTransform labelRect = _label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label.transform.SetAsLastSibling();
        }
    }
}
