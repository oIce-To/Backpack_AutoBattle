using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Keep the hit rectangle fixed while the caption presses down three reference pixels.
    public sealed class PrototypePixelButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Button _button;
        private Text _label;
        private Vector2 _labelPosition;
        private bool _primary, _wood, _selected, _hover, _pressed, _focused, _wasEnabled;

        internal void Initialize(Button button, bool primary, bool wood = false)
        {
            _button = button;
            _primary = primary;
            _wood = wood;
            Refresh();
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            Refresh();
        }

        private void Start()
        {
            _label = GetComponentInChildren<Text>();
            if (_label != null) _labelPosition = _label.rectTransform.anchoredPosition;
            Refresh();
        }

        private void Update()
        {
            if (_wasEnabled != _button.IsInteractable()) Refresh();
        }

        private void Refresh()
        {
            if (_button == null) return;
            _wasEnabled = _button.IsInteractable();
            _button.image.sprite = PrototypePixelSkin.SpriteFor(!_wasEnabled && !_selected ? PrototypePixelSkin.Surface.Disabled :
                _wood ? (_selected ? PrototypePixelSkin.Surface.WoodSignSelected : PrototypePixelSkin.Surface.WoodSign) :
                _primary || _selected ? PrototypePixelSkin.Surface.Primary : PrototypePixelSkin.Surface.Secondary,
                pressed: _pressed && _wasEnabled, focused: _focused && _wasEnabled);
            _button.image.color = !_wasEnabled ? Color.white : _pressed ? new Color(.8f, .8f, .8f) :
                _hover || _focused || _selected ? Color.white : new Color(.9f, .9f, .9f);
            if (_label == null) return;
            _label.rectTransform.anchoredPosition = _labelPosition + (_pressed && _wasEnabled ? Vector2.down * 3 : Vector2.zero);
            _label.color = !_wasEnabled && !_selected ? PrototypePixelSkin.Hex(0x8c968c) :
                _primary || _selected ? PrototypePixelSkin.Hex(0x1e2928) : PrototypePixelSkin.Ink;
        }

        public void OnPointerEnter(PointerEventData e) { _hover = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _hover = false; _pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) _pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { _pressed = false; Refresh(); }
        public void OnSelect(BaseEventData e) { _focused = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { _focused = false; _pressed = false; Refresh(); }
        private void OnDisable() { _hover = _pressed = _focused = false; Refresh(); }
    }
}
