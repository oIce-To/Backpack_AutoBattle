using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Route both the current composition and the committed characters through the project's input backend.
    internal sealed class PrototypeNameImeInput : BaseInput
    {
        private InputSystemUIInputModule _module;
        private BaseInput _previous;
        private PrototypeImeTextField _field;
        private string _composition = "";
        private IMECompositionMode _mode;
        private Vector2 _cursor;

        public override string compositionString => _composition;
        public override bool touchSupported => Touchscreen.current != null;
        public override IMECompositionMode imeCompositionMode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (value != IMECompositionMode.On) _composition = "";
                SetImeEnabled(value == IMECompositionMode.On);
            }
        }
        public override Vector2 compositionCursorPos
        {
            get => _cursor;
            set { _cursor = value; Keyboard.current?.SetIMECursorPosition(value); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _module = EventSystem.current?.GetComponent<InputSystemUIInputModule>();
            if (_module == null) return;
            _previous = _module.inputOverride;
            _module.inputOverride = this;
            _field = GetComponentInChildren<PrototypeImeTextField>();
            foreach (var device in InputSystem.devices)
                if (device is Keyboard keyboard) Attach(keyboard);
            InputSystem.onDeviceChange += DeviceChanged;
        }

        protected override void OnDisable()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            foreach (var device in InputSystem.devices)
                if (device is Keyboard keyboard) Detach(keyboard);
            SetImeEnabled(false);
            if (_module != null && _module.inputOverride == this) _module.inputOverride = _previous;
            _mode = IMECompositionMode.Auto;
            _composition = "";
            base.OnDisable();
        }

        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (device is not Keyboard keyboard) return;
            if (change == InputDeviceChange.Added) Attach(keyboard);
            else if (change == InputDeviceChange.Removed) Detach(keyboard);
        }

        private void Attach(Keyboard keyboard)
        {
            keyboard.onIMECompositionChange += CompositionChanged;
            keyboard.onTextInput += TextEntered;
            if (_mode == IMECompositionMode.On) keyboard.SetIMEEnabled(true);
        }

        private void Detach(Keyboard keyboard)
        {
            keyboard.onIMECompositionChange -= CompositionChanged;
            keyboard.onTextInput -= TextEntered;
        }

        private static void SetImeEnabled(bool enabled)
        {
            foreach (var device in InputSystem.devices)
                if (device is Keyboard keyboard) keyboard.SetIMEEnabled(enabled);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled && _mode == IMECompositionMode.On)
                SetImeEnabled(true);
        }

        private void CompositionChanged(IMECompositionString text)
        {
            if (!_field.isFocused) return;
            _composition = text.ToString();
            _field.ForceLabelUpdate();
        }

        private void TextEntered(char character)
        {
            if (!_field.isFocused || char.IsControl(character) || char.IsSurrogate(character)) return;
            _composition = "";
            _field.AppendCommittedCharacter(character);
        }
    }

    internal sealed class PrototypeImeTextField : InputField
    {
        private readonly Event _editEvent = new();

        internal void AppendCommittedCharacter(char character)
        {
            Append(character);
            ForceLabelUpdate();
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused) return;
            while (isFocused && Event.PopEvent(_editEvent))
            {
                // Keep uGUI's caret, selection, and clipboard shortcuts. Text arrives via onTextInput.
                if (_editEvent.rawType == EventType.KeyDown)
                {
                    var input = EventSystem.current?.currentInputModule?.input;
                    if (!string.IsNullOrEmpty(input?.compositionString)) continue;
                    _editEvent.character = '\0';
                    if (KeyPressed(_editEvent) == EditState.Finish)
                    {
                        if (_editEvent.keyCode != KeyCode.Escape) SendOnSubmit();
                        DeactivateInputField();
                    }
                    ForceLabelUpdate();
                }
                else if (_editEvent.type == EventType.ExecuteCommand && _editEvent.commandName == "SelectAll")
                {
                    SelectAll();
                    ForceLabelUpdate();
                }
            }
            eventData.Use();
        }
    }
}
