using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeCurrencyHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private RectTransform _canvas, _tooltip;
        private Text _amount;
        private Func<long> _balance;
        private Vector2 _pointer;
        private Camera _camera;

        internal void Configure(RectTransform canvas, string currency, Func<long> balance)
        {
            _canvas = canvas; _balance = balance;
            var border = SelectionUi.Image(canvas, currency + " Currency Tooltip", new Color(.48f, .56f, .66f));
            _tooltip = border.rectTransform;
            _tooltip.anchorMin = _tooltip.anchorMax = new Vector2(.5f, .5f);
            _tooltip.pivot = new Vector2(0, 1);
            _tooltip.sizeDelta = new Vector2(220, 76);
            _tooltip.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var background = SelectionUi.Image(_tooltip, "Background", new Color(.07f, .095f, .14f, .99f));
            BackpackPrototypeController.Stretch(background.rectTransform, 1);
            var title = SelectionUi.Label(_tooltip, "Currency Tooltip Name", currency, 18, new Vector2(14, -22), new Vector2(192, 26));
            _amount = SelectionUi.Label(_tooltip, "Currency Tooltip Amount", "", 18, new Vector2(14, -51), new Vector2(192, 26));
            foreach (var text in new[] { title, _amount })
            {
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0, 1);
                text.rectTransform.pivot = new Vector2(0, .5f);
            }
            _tooltip.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData pointer)
        {
            _pointer = pointer.position; _camera = pointer.enterEventCamera;
            _tooltip.gameObject.SetActive(true); _tooltip.SetAsLastSibling(); Refresh();
        }
        public void OnPointerMove(PointerEventData pointer)
        {
            _pointer = pointer.position; _camera = pointer.enterEventCamera;
            if (_tooltip != null && _tooltip.gameObject.activeSelf) Position();
        }
        public void OnPointerExit(PointerEventData pointer) => Hide();
        private void OnDisable() => Hide();
        private void Hide() { if (_tooltip != null) _tooltip.gameObject.SetActive(false); }
        private void LateUpdate() { if (_tooltip != null && _tooltip.gameObject.activeSelf) Refresh(); }
        private void Refresh()
        {
            _amount.text = CompactNumber.Exact(_balance());
            float width = Mathf.Max(220, _amount.preferredWidth + 28);
            _tooltip.sizeDelta = new Vector2(width, 76);
            _amount.rectTransform.sizeDelta = new Vector2(width - 28, 26);
            Position();
        }
        private void Position()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, _pointer, _camera, out var point);
            Rect area = _canvas.rect;
            _tooltip.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x + 14, area.xMin + 8, area.xMax - _tooltip.sizeDelta.x - 8),
                Mathf.Clamp(point.y - 18, area.yMin + _tooltip.sizeDelta.y + 8, area.yMax - 8));
        }
    }
}
