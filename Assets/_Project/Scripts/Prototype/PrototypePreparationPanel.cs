using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypePreparationPanel
    {
        private readonly Text _error, _enemyName, _health;
        private readonly Button _startButton, _infoButton;
        private readonly PrototypeMenuPopup _enemyPopup;
        private readonly PrototypeBattleBagView _bag;
        private readonly Image _portrait;
        private readonly Image _playerPortrait;
        private readonly Text _playerName;
        private readonly PrototypeItemTooltip _tooltip;

        internal PrototypePreparationPanel(BackpackPrototypeController controller, Transform shop, Transform canvas, PrototypeItemTooltip tooltip)
        {
            _tooltip = tooltip;
            var characterFrame = SelectionUi.Image(shop, "Preparation Character Frame", Color.clear);
            SelectionUi.Place(characterFrame.rectTransform, new Vector2(500, 80), new Vector2(194, 340));
            characterFrame.raycastTarget = false;
            _playerName = SelectionUi.Label(characterFrame.transform, "Preparation Character Name", "", 21,
                new Vector2(0, 170), new Vector2(220, 34));
            _playerName.color = PrototypePixelSkin.Ink;
            _playerPortrait = SelectionUi.Image(characterFrame.transform, "Preparation Character Illustration", Color.white);
            SelectionUi.Place(_playerPortrait.rectTransform, new Vector2(0, -26), new Vector2(252, 354));
            _playerPortrait.preserveAspect = true;
            _playerPortrait.raycastTarget = false;
            _infoButton = SelectionUi.Button(shop, "Enemy Info Button", "적 정보",
                new Vector2(500f, -205f), new Vector2(194f, 46f), () => controller.TryOpenEnemyPreview());
            _startButton = SelectionUi.Button(shop, "Start Battle Button", "전투 시작",
                new Vector2(500f, -270f), new Vector2(194f, 64f), () => controller.TryStartBattle());
            PrototypePixelSkin.Apply(_infoButton);
            PrototypePixelSkin.Apply(_startButton, primary: true);
            _error = SelectionUi.Label(shop, "Enemy Configuration Error", "", 13, new Vector2(500f, -151f), new Vector2(214f, 48f));
            _error.color = new Color(1f, .55f, .48f);

            _enemyPopup = new PrototypeMenuPopup(canvas, "Enemy Preview Popup", "적 정보", new Vector2(652, 454), controller.CloseEnemyPreview, pixelSurface: true);
            RectTransform panel = _enemyPopup.Panel;
            _enemyName = SelectionUi.Label(panel, "Enemy Preview Name", "", 22, new Vector2(-149, 117), new Vector2(242, 35));
            _portrait = SelectionUi.Image(panel, "Enemy Preview Illustration", Color.white);
            SelectionUi.Place(_portrait.rectTransform, new Vector2(-149, -22), new Vector2(248, 223));
            _portrait.preserveAspect = true;

            var healthBar = SelectionUi.Image(panel, "Enemy Preview Health Bar", new Color(.80f, .27f, .25f));
            SelectionUi.Place(healthBar.rectTransform, new Vector2(-149, -164), new Vector2(242, 30));
            PrototypePixelSkin.Apply(healthBar, PrototypePixelSkin.Surface.Cell, preserveTint: true);
            _health = SelectionUi.Label(healthBar.transform, "Enemy Preview Health", "", 16, Vector2.zero, new Vector2(230, 26));

            var bagPanel = SelectionUi.Image(panel, "Enemy Preview Bag Panel", new Color(.075f, .09f, .13f));
            SelectionUi.Place(bagPanel.rectTransform, new Vector2(146, -29), new Vector2(264, 264));
            PrototypePixelSkin.Apply(bagPanel, PrototypePixelSkin.Surface.Leather);
            _bag = new PrototypeBattleBagView(bagPanel.transform, "Enemy Preview Backpack", pixelSurface: true);
            _bag.Root.anchoredPosition = Vector2.zero;
            var bagTitle = SelectionUi.Label(panel, "Enemy Preview Bag Title", "상대 가방", 14, new Vector2(146, -180), new Vector2(264, 26));
            bagTitle.color = PrototypePixelSkin.Muted;
        }

        internal bool IsEnemyPreviewOpen => _enemyPopup.IsOpen;

        internal void ShowEnemy(CombatantSnapshot enemy)
        {
            _enemyName.text = enemy.DisplayName;
            _portrait.sprite = enemy.Illustration;
            _portrait.enabled = _portrait.sprite != null;
            _health.text = $"체력 {enemy.MaximumHealth:0.#} / {enemy.MaximumHealth:0.#}";
            _bag.Build(enemy.Loadout, CombatSide.Enemy, _tooltip, item => ItemTooltipContent.FromCombat(item, null),
                animateCooldown: false);
            _enemyPopup.Show();
        }

        internal void HideEnemy()
        {
            _enemyPopup.Hide();
            _bag.Clear();
        }

        internal void Render(string error, CharacterData character)
        {
            _error.text = string.IsNullOrEmpty(error) ? "" : "적 설정 오류\n" + error.Split('\n')[0];
            _playerName.text = character != null ? character.DisplayName : "플레이어";
            _playerPortrait.sprite = character != null ? character.Portrait : null;
            _playerPortrait.enabled = _playerPortrait.sprite != null;
        }

        public void SetInputEnabled(bool enabled) => _startButton.interactable = _infoButton.interactable = enabled;
    }
}
