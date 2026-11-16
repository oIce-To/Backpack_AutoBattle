using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Used on non-button controls (toggles/dropdown); never plays on programmatic value changes.
    public sealed class PrototypeUiClickSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        internal static void Play(Component target)
            => target.GetComponentInParent<BackpackPrototypeController>()?.PlaySound(PrototypeSound.ButtonClick);

        private void PlayIfEnabled()
        {
            var control = GetComponent<Selectable>();
            if (control == null || (control.IsActive() && control.IsInteractable())) Play(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging && eventData.button == PointerEventData.InputButton.Left) PlayIfEnabled();
        }
        public void OnSubmit(BaseEventData eventData) => PlayIfEnabled();
    }
}
