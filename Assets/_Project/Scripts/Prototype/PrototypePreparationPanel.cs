using System.Linq;
using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypePreparationPanel
    {
        private readonly Text _enemyText;
        private readonly Button _startButton;

        public PrototypePreparationPanel(BackpackPrototypeController controller, Transform parent)
        {
            var panelObject = new GameObject("Preparation Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 1f);
            BackpackPrototypeController.SetRect((RectTransform)panelObject.transform, new Vector2(0.5f, 0.5f), new Vector2(500f, 34f), new Vector2(220f, 472f));
            Text title = Label(panelObject.transform, "Title", "전투 준비", -30f, 42f, 22);
            title.alignment = TextAnchor.MiddleCenter;
            _enemyText = Label(panelObject.transform, "Next Enemy", string.Empty, -192f, 270f, 15);

            var buttonObject = new GameObject("Start Battle Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);
            buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.48f, 0.70f, 1f);
            BackpackPrototypeController.SetRect((RectTransform)buttonObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(194f, 46f));
            _startButton = buttonObject.GetComponent<Button>();
            _startButton.onClick.AddListener(() => controller.TryStartBattle());
            Text label = BackpackPrototypeController.CreateText(buttonObject.transform, "Label", "전투 시작", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            BackpackPrototypeController.Stretch(label.rectTransform);
        }

        public void Render(string stageName, int round, CombatantSnapshot enemy, string error, bool isBoss, bool isDebug)
        {
            _startButton.interactable = enemy != null;
            _enemyText.text = enemy == null ? $"적 설정 오류\n{error}" :
                $"{stageName}\nROUND {round}{(isBoss ? " · 보스" : "")}\n상대: {enemy.DisplayName}\n"
                + $"체력 {enemy.MaximumHealth:0.#} · 가방 {enemy.Loadout.Width}×{enemy.Loadout.Height}\n"
                + (isDebug ? "[시험 적 사용 중]\n" : "")
                + "\n" + string.Join("\n", enemy.Loadout.Items.Select(item => item.Activation.HasEffect(BackpackAutoBattle.Items.ItemEffectKind.Attack)
                    ? $"{item.DisplayName}\n공격력 {item.BaseWeaponStats.AttackDamage:0.#} · {item.Activation.CooldownSeconds:0.0#}s"
                    : item.DisplayName));
        }

        public void SetInputEnabled(bool enabled) => _startButton.interactable = enabled;

        private static Text Label(Transform parent, string name, string content, float y, float height, int fontSize)
        {
            Text text = BackpackPrototypeController.CreateText(parent, name, content, fontSize, FontStyle.Normal, TextAnchor.UpperLeft);
            BackpackPrototypeController.SetRect(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(194f, height));
            text.raycastTarget = false;
            return text;
        }
    }
}
