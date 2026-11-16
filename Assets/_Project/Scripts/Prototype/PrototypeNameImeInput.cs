using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // uGUI owns composition, commits, selection and editing through the native input backend.
    // Active Input Handling must be Both: gameplay stays on Input System, while InputField
    // receives its native IME/IMGUI text events. Never append onTextInput a second time.
    internal sealed class PrototypeNameImeInput : MonoBehaviour
    {
        private InputField _field;

        private void OnEnable() => _field = GetComponentInChildren<InputField>();

        private void OnApplicationFocus(bool focused)
        {
            if (focused && _field != null && _field.isFocused)
                Input.imeCompositionMode = IMECompositionMode.On;
        }
    }
}
