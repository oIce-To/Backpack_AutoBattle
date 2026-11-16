using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Shared by every button factory, including dynamic item cards and popup actions.
    public sealed class PrototypeAudioButton : Button
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                PrototypeUiClickSound.Play(this);
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable()) PrototypeUiClickSound.Play(this);
            base.OnSubmit(eventData);
        }
    }
}
