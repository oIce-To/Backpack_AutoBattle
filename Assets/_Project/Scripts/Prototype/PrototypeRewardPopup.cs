using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeRewardPopup
    {
        private readonly RectTransform _root;
        private readonly Text _title, _rewards;
        internal bool IsOpen => _root.gameObject.activeSelf;
        internal PrototypeRewardPopup(Transform canvas)
        {
            var backdrop = SelectionUi.Image(canvas, "Reward Popup", new Color(0, 0, 0, .72f));
            _root = backdrop.rectTransform; BackpackPrototypeController.Stretch(_root);
            backdrop.raycastTarget = true;
            var panel = SelectionUi.Image(_root, "Reward Popup Panel", new Color(.10f, .13f, .19f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(470, 290));
            _title = SelectionUi.Label(panel.transform, "Reward Popup Title", "", 27, new Vector2(0, 100), new Vector2(420, 42));
            _rewards = SelectionUi.Label(panel.transform, "Reward Popup Contents", "", 21, new Vector2(0, 5), new Vector2(420, 140));
            _rewards.lineSpacing = 1.3f;
            SelectionUi.Button(panel.transform, "Reward Popup Close", "확인", new Vector2(0, -101), new Vector2(210, 42), Hide);
            Hide();
        }
        internal void Show(string title, string rewards)
        {
            _title.text = title; _rewards.text = rewards;
            _root.gameObject.SetActive(true); _root.SetAsLastSibling();
            EventSystem.current?.SetSelectedGameObject(null);
        }
        internal void Hide()
        {
            _root.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
