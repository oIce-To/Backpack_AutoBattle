using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypePreparationPanel
    {
        private readonly Text _enemyText;
        private readonly Text _statsText;

        public PrototypePreparationPanel(BackpackPrototypeController controller, Transform parent)
        {
            var panelObject = new GameObject("Preparation Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 1f);
            BackpackPrototypeController.SetRect((RectTransform)panelObject.transform, new Vector2(0.5f, 0.5f), new Vector2(500f, 34f), new Vector2(220f, 472f));
            Text title = Label(panelObject.transform, "Title", "전투 준비", -30f, 42f, 22);
            title.alignment = TextAnchor.MiddleCenter;
            _enemyText = Label(panelObject.transform, "Next Enemy", string.Empty, -100f, 90f, 16);
            _statsText = Label(panelObject.transform, "Item Details", string.Empty, -252f, 190f, 15);

            var buttonObject = new GameObject("Start Battle Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);
            buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.48f, 0.70f, 1f);
            BackpackPrototypeController.SetRect((RectTransform)buttonObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(194f, 46f));
            buttonObject.GetComponent<Button>().onClick.AddListener(() => controller.TryStartBattle());
            Text label = BackpackPrototypeController.CreateText(buttonObject.transform, "Label", "전투 시작", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            BackpackPrototypeController.Stretch(label.rectTransform);
        }

        public void Render(int round, EnemyData enemy)
        {
            _enemyText.text = enemy == null ? "적 데이터가 없습니다." : $"ROUND {round}\n상대: {enemy.DisplayName}\n체력 {enemy.MaximumHealth} · 가방 {enemy.BackpackWidth}×{enemy.BackpackHeight}";
            _statsText.text = "아이템을 선택하면\n기본 스탯을 확인합니다.\n\n전투 시작 시 전용 화면으로\n이동합니다.\n\n보관함 아이템은\n전투에 참여하지 않습니다.";
        }

        public void ShowItemStats(ItemData item)
        {
            if (item.Category != ItemCategory.Weapon)
            {
                _statsText.text = $"{item.DisplayName}\n\n이번 버전에서는 무기만\n공격합니다. 이 아이템의\n효과는 추후 추가됩니다.";
                return;
            }
            WeaponStats stats = item.BaseWeaponStats;
            _statsText.text = $"{item.DisplayName}\n\n공격력 {stats.AttackDamage:0.#}\n쿨타임 {stats.CooldownSeconds:0.##}초\n명중률 {stats.AccuracyPercent:0.#}%\n치명타 확률 {stats.CriticalChancePercent:0.#}%\n치명타 피해 {stats.CriticalDamagePercent:0.#}%";
        }

        private static Text Label(Transform parent, string name, string content, float y, float height, int fontSize)
        {
            Text text = BackpackPrototypeController.CreateText(parent, name, content, fontSize, FontStyle.Normal, TextAnchor.UpperLeft);
            BackpackPrototypeController.SetRect(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(194f, height));
            text.raycastTarget = false;
            return text;
        }
    }
}
