using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeBlueprintHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private PrototypeItemTooltip _tooltip;
        private string _title, _quantity;

        internal void Bind(PrototypeItemTooltip tooltip, string title, long quantity)
        {
            _tooltip = tooltip;
            _title = title;
            _quantity = $"보유 수량  {CompactNumber.Exact(quantity)}";
        }

        public void OnPointerEnter(PointerEventData pointer)
            => _tooltip.ShowDescription(this, _title, _quantity, new Color(.52f, .72f, .90f), pointer.position, pointer.enterEventCamera);
        public void OnPointerMove(PointerEventData pointer) => _tooltip.Move(this, pointer.position, pointer.enterEventCamera);
        public void OnPointerExit(PointerEventData pointer) => _tooltip.Hide(this);
        private void OnDisable() => _tooltip?.Hide(this);
    }
}
