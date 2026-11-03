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
        public PrototypeMainNavigation(Transform canvas, Action<MainMenuTab> select)
        {
            var obj = new GameObject("Main Navigation", typeof(RectTransform)); obj.transform.SetParent(canvas, false);
            Root = (RectTransform)obj.transform;
            SelectionUi.Place(Root, new Vector2(0, 329), new Vector2(650, 38));
            string[] names = { "메인", "캐릭터", "스테이지" };
            MainMenuTab[] order = { MainMenuTab.Characters, MainMenuTab.Home, MainMenuTab.Stages };
            for (int i = 0; i < order.Length; i++)
            {
                MainMenuTab tab = order[i];
                Button button = SelectionUi.Button(Root, "Main Tab " + tab, names[(int)tab],
                    new Vector2((i - 1) * 204, 0), new Vector2(192, 38), () => select(tab));
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                _tabs.Add(tab, button);
            }
            Hide();
        }
        public void Show(MainMenuTab selected)
        {
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            foreach (var tab in _tabs) tab.Value.GetComponent<Image>().color = tab.Key == selected ? SelectionUi.Selected : SelectionUi.Idle;
        }
        public void Hide() => Root.gameObject.SetActive(false);
    }
}
