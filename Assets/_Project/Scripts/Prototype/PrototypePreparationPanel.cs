using BackpackAutoBattle.Combat;
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
        private readonly PrototypeItemTooltip _tooltip;

        internal PrototypePreparationPanel(BackpackPrototypeController controller, Transform shop, Transform canvas, PrototypeItemTooltip tooltip)
        {
            _tooltip = tooltip;
            _infoButton = SelectionUi.Button(shop, "Enemy Info Button", "적 정보",
                new Vector2(500f, -113f), new Vector2(194f, 42f), () => controller.TryOpenEnemyPreview());
            _startButton = SelectionUi.Button(shop, "Start Battle Button", "전투 시작",
                new Vector2(500f, -164f), new Vector2(194f, 46f), () => controller.TryStartBattle());
            _startButton.image.color = new Color(.24f, .48f, .70f);
            _error = SelectionUi.Label(shop, "Enemy Configuration Error", "", 14, new Vector2(500f, -66f), new Vector2(194f, 52f));
            _error.color = new Color(1f, .55f, .48f);

            _enemyPopup = new PrototypeMenuPopup(canvas, "Enemy Preview Popup", "적 정보", new Vector2(652, 454), controller.CloseEnemyPreview);
            RectTransform panel = _enemyPopup.Panel;
            _enemyName = SelectionUi.Label(panel, "Enemy Preview Name", "", 22, new Vector2(-149, 117), new Vector2(242, 35));
            _portrait = SelectionUi.Image(panel, "Enemy Preview Illustration", Color.white);
            SelectionUi.Place(_portrait.rectTransform, new Vector2(-149, -22), new Vector2(248, 223));
            _portrait.preserveAspect = true;

            var healthBar = SelectionUi.Image(panel, "Enemy Preview Health Bar", new Color(.80f, .27f, .25f));
            SelectionUi.Place(healthBar.rectTransform, new Vector2(-149, -164), new Vector2(242, 30));
            _health = SelectionUi.Label(healthBar.transform, "Enemy Preview Health", "", 16, Vector2.zero, new Vector2(230, 26));

            var bagPanel = SelectionUi.Image(panel, "Enemy Preview Bag Panel", new Color(.075f, .09f, .13f));
            SelectionUi.Place(bagPanel.rectTransform, new Vector2(146, -29), new Vector2(264, 264));
            _bag = new PrototypeBattleBagView(bagPanel.transform, "Enemy Preview Backpack");
            _bag.Root.anchoredPosition = Vector2.zero;
        }

        internal bool IsEnemyPreviewOpen => _enemyPopup.IsOpen;

        internal void ShowEnemy(CombatantSnapshot enemy)
        {
            _enemyName.text = enemy.DisplayName;
            _portrait.sprite = enemy.Illustration;
            _portrait.enabled = _portrait.sprite != null;
            _health.text = $"체력 {enemy.MaximumHealth:0.#} / {enemy.MaximumHealth:0.#}";
            _bag.Build(enemy.Loadout, CombatSide.Enemy, null, _tooltip, item => ItemTooltipContent.FromCombat(item, null),
                animateCooldown: false);
            _enemyPopup.Show();
        }

        internal void HideEnemy()
        {
            _enemyPopup.Hide();
            _bag.Clear();
        }

        internal void Render(string error)
        {
            _error.text = string.IsNullOrEmpty(error) ? "" : "적 설정 오류\n" + error.Split('\n')[0];
        }

        public void SetInputEnabled(bool enabled) => _startButton.interactable = _infoButton.interactable = enabled;
    }
}
