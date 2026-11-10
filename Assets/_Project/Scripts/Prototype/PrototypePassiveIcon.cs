using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypePassiveIcon : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private CharacterPassiveData _passive;
        private PrototypeItemTooltip _tooltip;
        private Image _icon;
        internal void Configure(PrototypeItemTooltip tooltip)
        {
            _tooltip = tooltip;
            _icon = SelectionUi.Image(transform, "Character Passive Artwork", Color.white);
            _icon.preserveAspect = true;
            BackpackPrototypeController.Stretch(_icon.rectTransform, 2);
        }
        internal void Bind(CharacterPassiveData passive)
        {
            if (_passive != passive) _tooltip.Hide(this);
            _passive = passive; _icon.sprite = passive?.Icon;
            gameObject.SetActive(passive != null);
        }
        public void OnPointerEnter(PointerEventData pointer)
        {
            if (!enabled || _passive == null) return;
            _tooltip.ShowDescription(this, _passive.DisplayName, _passive.Description,
                new Color(.85f, .73f, .40f), pointer.position, pointer.enterEventCamera);
        }
        public void OnPointerMove(PointerEventData pointer) => _tooltip.Move(this, pointer.position, pointer.enterEventCamera);
        public void OnPointerExit(PointerEventData pointer) => _tooltip.Hide(this);
        private void OnDisable() => _tooltip?.Hide(this);
    }
}
