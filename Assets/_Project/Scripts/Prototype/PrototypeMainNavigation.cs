using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BackpackAutoBattle.Progression;

namespace BackpackAutoBattle.Prototype
{
    public enum MainMenuTab { Home, Characters, Stages, Equipment }

    /// <summary>Persistent main-menu navigation, independent of each tab's presentation and input.</summary>
    public sealed class PrototypeMainNavigation
    {
        private const float WalletWidth = 144f;
        public RectTransform Root { get; }
        private readonly Dictionary<MainMenuTab, Button> _tabs = new();
        private readonly RectTransform _wallet;
        private readonly PrototypeExperienceIcon _experienceIcon;
        private readonly Text _experience;
        private readonly RectTransform _shardWallet;
        private readonly Text _shards, _tickets, _ticketTime;
        private long _experienceBalance, _shardBalance;
        public PrototypeMainNavigation(Transform canvas, Action<MainMenuTab> select, Action openSettings)
        {
            var obj = new GameObject("Main Navigation", typeof(RectTransform)); obj.transform.SetParent(canvas, false);
            Root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(Root);
            var tabs = new GameObject("Main Tabs", typeof(RectTransform)); tabs.transform.SetParent(Root, false);
            SelectionUi.Place((RectTransform)tabs.transform, new Vector2(0, -329), new Vector2(1100, 38));
            var wallet = new GameObject("Main Experience Wallet", typeof(RectTransform), typeof(PrototypeRoundedPanel));
            wallet.transform.SetParent(Root, false); _wallet = (RectTransform)wallet.transform;
            var walletFace = wallet.GetComponent<PrototypeRoundedPanel>();
            walletFace.color = new Color(.16f, .19f, .24f, .55f); walletFace.DrawBorder = false; walletFace.raycastTarget = true;
            var icon = new GameObject("Main Experience Icon", typeof(RectTransform), typeof(PrototypeExperienceIcon));
            icon.transform.SetParent(_wallet, false); _experienceIcon = icon.GetComponent<PrototypeExperienceIcon>();
            _experienceIcon.raycastTarget = false;
            _experience = SelectionUi.Label(_wallet, "Main Experience Amount", "0", 22, Vector2.zero, new Vector2(120, 32));
            _experience.alignment = TextAnchor.MiddleRight;
            var shardWallet = new GameObject("Main Origin Shard Wallet", typeof(RectTransform), typeof(PrototypeRoundedPanel));
            shardWallet.transform.SetParent(Root, false); _shardWallet = (RectTransform)shardWallet.transform;
            var shardFace = shardWallet.GetComponent<PrototypeRoundedPanel>();
            shardFace.color = new Color(.16f, .19f, .24f, .55f); shardFace.DrawBorder = false; shardFace.raycastTarget = true;
            AddEconomyIcon(_shardWallet, "Origin Shard Icon", EconomyIconKind.OriginShard, new Vector2(-49, 0), new Vector2(31, 31));
            _shards = SelectionUi.Label(_shardWallet, "Main Origin Shard Amount", "0", 22, new Vector2(18, 0), new Vector2(80, 32));
            _shards.alignment = TextAnchor.MiddleRight;
            _shards.resizeTextForBestFit = true; _shards.resizeTextMinSize = 14; _shards.resizeTextMaxSize = 22;
            wallet.AddComponent<PrototypeCurrencyHover>().Configure(Root, "경험치", () => _experienceBalance);
            shardWallet.AddComponent<PrototypeCurrencyHover>().Configure(Root, "기원의 파편", () => _shardBalance);
            AddEconomyIcon(Root, "Mine Ticket Icon", EconomyIconKind.MineTicket, new Vector2(350, 329), new Vector2(34, 27));
            _tickets = SelectionUi.Label(Root, "Main Mine Tickets", "", 22, new Vector2(413, 329), new Vector2(94, 29));
            _ticketTime = SelectionUi.Label(Root, "Main Mine Ticket Timer", "", 13, new Vector2(397, 304), new Vector2(156, 21));
            _ticketTime.color = new Color(.67f, .75f, .84f);
            PrototypeSettingsScreen.GearButton(Root, "Main Settings Button", new Vector2(529, 329), openSettings);
            string[] names = { "메인", "캐릭터", "스테이지", "장비" };
            MainMenuTab[] order = { MainMenuTab.Equipment, MainMenuTab.Characters, MainMenuTab.Home, MainMenuTab.Stages };
            for (int i = 0; i < order.Length; i++)
            {
                MainMenuTab tab = order[i];
                Button button = SelectionUi.Button(tabs.transform, "Main Tab " + tab, names[(int)tab],
                    new Vector2((i - 2) * 204, 0), new Vector2(192, 38), () => select(tab));
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
            _experienceBalance = experience;
            _experience.text = CompactNumber.Format(experience);
            _experience.resizeTextForBestFit = true; _experience.resizeTextMinSize = 14; _experience.resizeTextMaxSize = 22;
            LayoutWallets();
        }
        public void SetEconomy(PlayerProgress progress, DateTimeOffset now)
        {
            SetExperience(progress.SharedExperience);
            _shardBalance = progress.OriginShards;
            _shards.text = CompactNumber.Format(_shardBalance);
            LayoutWallets();
            TimeSpan remaining = progress.UntilNextMineTicket(now);
            _tickets.text = $"{progress.MineTickets} / {PlayerProgress.MaximumMineTickets}";
            _ticketTime.text = remaining == TimeSpan.Zero ? "충전 완료"
                : $"다음 충전 {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
        private void LayoutWallets()
        {
            // Use one fixed cell width so currency text and balance changes cannot resize either panel.
            const float width = WalletWidth;
            SelectionUi.Place(_wallet, new Vector2(-550 + width / 2, 329), new Vector2(width, 38));
            SelectionUi.Place(_shardWallet, new Vector2(-550 + width + 12 + width / 2, 329), new Vector2(width, 38));
            SelectionUi.Place(_experienceIcon.rectTransform, new Vector2(-width / 2 + 24, 0), new Vector2(31, 31));
            SelectionUi.Place((RectTransform)_shardWallet.Find("Origin Shard Icon"), new Vector2(-width / 2 + 24, 0), new Vector2(31, 31));
            foreach (var amount in new[] { _experience, _shards })
                SelectionUi.Place(amount.rectTransform, new Vector2(18, 0), new Vector2(width - 64, 32));
        }
        private static void AddEconomyIcon(Transform parent, string name, EconomyIconKind kind, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(PrototypeEconomyIcon));
            obj.transform.SetParent(parent, false);
            var icon = obj.GetComponent<PrototypeEconomyIcon>();
            icon.SetKind(kind); icon.raycastTarget = false;
            SelectionUi.Place(icon.rectTransform, position, size);
        }
        public void Hide() => Root.gameObject.SetActive(false);
    }
}
