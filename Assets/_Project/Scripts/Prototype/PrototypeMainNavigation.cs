using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public enum MainMenuTab { Home, Characters, Stages }

    /// <summary>Persistent main-menu navigation, independent of each tab's presentation and input.</summary>
    public sealed class PrototypeMainNavigation
    {
        public RectTransform Root { get; }
        private readonly Dictionary<MainMenuTab, Button> _tabs = new();
        private readonly RectTransform _wallet;
        private readonly PrototypeExperienceIcon _experienceIcon;
        private readonly Text _experience;
        public PrototypeMainNavigation(Transform canvas, Action<MainMenuTab> select, Action openSettings)
        {
            var obj = new GameObject("Main Navigation", typeof(RectTransform)); obj.transform.SetParent(canvas, false);
            Root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(Root);
            var tabs = new GameObject("Main Tabs", typeof(RectTransform)); tabs.transform.SetParent(Root, false);
            SelectionUi.Place((RectTransform)tabs.transform, new Vector2(0, -329), new Vector2(650, 38));
            var wallet = new GameObject("Main Experience Wallet", typeof(RectTransform), typeof(PrototypeRoundedPanel));
            wallet.transform.SetParent(Root, false); _wallet = (RectTransform)wallet.transform;
            var walletFace = wallet.GetComponent<PrototypeRoundedPanel>();
            walletFace.color = new Color(.16f, .19f, .24f, .55f); walletFace.DrawBorder = false; walletFace.raycastTarget = false;
            var icon = new GameObject("Main Experience Icon", typeof(RectTransform), typeof(PrototypeExperienceIcon));
            icon.transform.SetParent(_wallet, false); _experienceIcon = icon.GetComponent<PrototypeExperienceIcon>();
            _experience = SelectionUi.Label(_wallet, "Main Experience Amount", "0", 22, Vector2.zero, new Vector2(120, 32));
            _experience.alignment = TextAnchor.MiddleRight;
            PrototypeSettingsScreen.GearButton(Root, "Main Settings Button", new Vector2(529, 329), openSettings);
            string[] names = { "메인", "캐릭터", "스테이지" };
            MainMenuTab[] order = { MainMenuTab.Characters, MainMenuTab.Home, MainMenuTab.Stages };
            for (int i = 0; i < order.Length; i++)
            {
                MainMenuTab tab = order[i];
                Button button = SelectionUi.Button(tabs.transform, "Main Tab " + tab, names[(int)tab],
                    new Vector2((i - 1) * 204, 0), new Vector2(192, 38), () => select(tab));
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                _tabs.Add(tab, button);
            }
            Hide();
        }
        public void Show(MainMenuTab selected, long experience)
        {
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            SetExperience(experience);
            foreach (var tab in _tabs) tab.Value.GetComponent<Image>().color = tab.Key == selected ? SelectionUi.Selected : SelectionUi.Idle;
        }
        public void SetExperience(long experience)
        {
            _experience.text = $"{experience:N0}";
            float width = Mathf.Clamp(_experience.preferredWidth + 72, 100, 420);
            SelectionUi.Place(_wallet, new Vector2(-550 + width / 2, 329), new Vector2(width, 38));
            SelectionUi.Place(_experienceIcon.rectTransform, new Vector2(-width / 2 + 25, 0), new Vector2(28, 28));
            SelectionUi.Place(_experience.rectTransform, new Vector2(18, 0), new Vector2(width - 68, 32));
            _experience.resizeTextForBestFit = true; _experience.resizeTextMinSize = 14; _experience.resizeTextMaxSize = 22;
        }
        public void Hide() => Root.gameObject.SetActive(false);
    }
}
