using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeMenuPopup
    {
        internal enum CloseStyle { Text, Pixel, None }
        private readonly RectTransform _root;
        public RectTransform Panel { get; }
        public Text Title { get; }
        public bool IsOpen => _root.gameObject.activeInHierarchy;

        public PrototypeMenuPopup(Transform parent, string name, string title, Vector2 size, Action close, CloseStyle closeStyle = CloseStyle.Text)
        {
            var backdrop = SelectionUi.Image(parent, name, new Color(0f, 0f, 0f, .72f));
            _root = backdrop.rectTransform;
            BackpackPrototypeController.Stretch(_root);
            backdrop.raycastTarget = true;
            var dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
            dismiss.onClick.AddListener(() => close());

            var panel = SelectionUi.Image(_root, name + " Panel", new Color(.10f, .13f, .19f));
            Panel = panel.rectTransform;
            SelectionUi.Place(Panel, Vector2.zero, size);
            panel.raycastTarget = true;
            // Consume panel background clicks before they reach the backdrop.
            panel.gameObject.AddComponent<EventTrigger>();
            Title = SelectionUi.Label(Panel, name + " Title", title, 27, new Vector2(0, size.y / 2 - 39), new Vector2(size.x - 115, 42));
            if (closeStyle == CloseStyle.Pixel)
                PrototypeSettingsScreen.CreateCloseButton(Panel, name + " Close", new Vector2(size.x / 2 - 36, size.y / 2 - 34), close);
            else if (closeStyle == CloseStyle.Text)
            {
                var button = SelectionUi.Button(Panel, name + " Close", "닫기", new Vector2(size.x / 2 - 53, size.y / 2 - 34),
                    new Vector2(76, 34), close);
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            Hide();
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            if (IsOpen) EventSystem.current?.SetSelectedGameObject(null);
            _root.gameObject.SetActive(false);
        }
    }
}
