using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeBattleScreen
    {
        private readonly PrototypeBattleBagView _playerBag;
        private readonly PrototypeBattleBagView _enemyBag;
        private readonly Image _playerHealthFill;
        private readonly Image _enemyHealthFill;
        private readonly Text _playerHealthText;
        private readonly Text _enemyHealthText;
        private readonly Text _playerName;
        private readonly Text _enemyName;
        private readonly Text _title;
        private readonly Text _timer;
        private readonly Text _inspection;
        private readonly Text _logText;
        private readonly Text _playerDamage;
        private readonly Text _enemyDamage;
        private readonly Button _continueButton;
        private readonly Text _continueText;
        private readonly Queue<string> _log = new();
        private CombatSimulation _combat;
        private int _round;
        private int _income;
        private float _playerDamageExpiry;
        private float _enemyDamageExpiry;

        public RectTransform Root { get; }

        public PrototypeBattleScreen(Transform parent, Action continueAction)
        {
            Image background = Image(parent, "Battle Screen", new Color(0.04f, 0.065f, 0.11f));
            Root = background.rectTransform;
            BackpackPrototypeController.Stretch(Root);
            _title = Label(Root, "Battle Title", new Vector2(0f, 320f), new Vector2(800f, 44f), 28, TextAnchor.MiddleCenter);
            _timer = Label(Root, "Combat Timer", new Vector2(0f, 286f), new Vector2(900f, 28f), 17, TextAnchor.MiddleCenter);
            Image playerFrame = Frame(Root, "Player Battle Frame", -310f, new Color(0.07f, 0.13f, 0.20f));
            Image enemyFrame = Frame(Root, "Enemy Battle Frame", 310f, new Color(0.17f, 0.10f, 0.10f));
            _playerName = Label(playerFrame.transform, "Player Name", new Vector2(0f, 203f), new Vector2(440f, 34f), 22, TextAnchor.MiddleCenter);
            _enemyName = Label(enemyFrame.transform, "Enemy Name", new Vector2(0f, 203f), new Vector2(440f, 34f), 22, TextAnchor.MiddleCenter);
            _playerBag = new PrototypeBattleBagView(playerFrame.transform, "Player Battle Backpack");
            _enemyBag = new PrototypeBattleBagView(enemyFrame.transform, "Enemy Battle Backpack");
            _playerHealthFill = HealthBar(playerFrame.transform, "Player Health", new Color(0.20f, 0.67f, 0.37f));
            _enemyHealthFill = HealthBar(enemyFrame.transform, "Enemy Health", new Color(0.80f, 0.27f, 0.25f));
            _playerHealthText = Label(playerFrame.transform, "Player Health Text", new Vector2(0f, -192f), new Vector2(410f, 30f), 18, TextAnchor.MiddleCenter);
            _enemyHealthText = Label(enemyFrame.transform, "Enemy Health Text", new Vector2(0f, -192f), new Vector2(410f, 30f), 18, TextAnchor.MiddleCenter);
            _playerDamage = Label(playerFrame.transform, "Player Damage", new Vector2(0f, -155f), new Vector2(350f, 28f), 23, TextAnchor.MiddleCenter);
            _enemyDamage = Label(enemyFrame.transform, "Enemy Damage", new Vector2(0f, -155f), new Vector2(350f, 28f), 23, TextAnchor.MiddleCenter);
            _inspection = Label(Root, "Battle Item Details", new Vector2(0f, -194f), new Vector2(1180f, 30f), 16, TextAnchor.MiddleCenter);

            Image logPanel = Image(Root, "Battle Log Panel", new Color(0.08f, 0.11f, 0.17f));
            BackpackPrototypeController.SetRect(logPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120f, -276f), new Vector2(950f, 120f));
            _logText = BackpackPrototypeController.CreateText(logPanel.transform, "Combat Log", string.Empty, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            _logText.raycastTarget = false;
            BackpackPrototypeController.Stretch(_logText.rectTransform, 12f);

            Image buttonImage = Image(Root, "Combat Action Button", new Color(0.23f, 0.46f, 0.68f));
            BackpackPrototypeController.SetRect(buttonImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(500f, -276f), new Vector2(190f, 56f));
            buttonImage.raycastTarget = true;
            _continueButton = buttonImage.gameObject.AddComponent<Button>();
            _continueButton.onClick.AddListener(() => continueAction());
            _continueText = BackpackPrototypeController.CreateText(buttonImage.transform, "Label", string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _continueText.raycastTarget = false;
            BackpackPrototypeController.Stretch(_continueText.rectTransform);
        }

        public void Begin(CombatSimulation combat, int round, int income)
        {
            _combat = combat;
            _round = round;
            _income = income;
            _log.Clear();
            _playerBag.Build(combat.Player.Loadout, CombatSide.Player, item => InspectItem(item, CombatSide.Player));
            _enemyBag.Build(combat.Enemy.Loadout, CombatSide.Enemy, item => InspectItem(item, CombatSide.Enemy));
            _playerName.text = $"{combat.Player.DisplayName} · {combat.Player.Loadout.Width}×{combat.Player.Loadout.Height}";
            _enemyName.text = $"{combat.Enemy.DisplayName} · {combat.Enemy.Loadout.Width}×{combat.Enemy.Loadout.Height}";
            _title.text = $"{combat.Player.DisplayName} VS {combat.Enemy.DisplayName}";
            _inspection.text = "아이템을 클릭하면 전투 스탯을 확인할 수 있습니다. 전투 중 가방은 변경할 수 없습니다.";
            _logText.text = "첫 공격을 준비하고 있습니다…";
            _playerDamage.text = _enemyDamage.text = string.Empty;
            _continueButton.interactable = false;
            _continueText.text = "전투 진행 중…";
            Render(combat);
        }

        public void Render(CombatSimulation combat)
        {
            _timer.text = $"ROUND {_round} · {combat.ElapsedSeconds:0.0}초 · 자동 전투";
            SetHealth(_playerHealthFill, _playerHealthText, combat.PlayerHealth, combat.PlayerMaxHealth);
            SetHealth(_enemyHealthFill, _enemyHealthText, combat.EnemyHealth, combat.EnemyMaxHealth);
            _playerBag.RenderCooldowns(combat.PlayerWeapons, combat.ElapsedSeconds);
            _enemyBag.RenderCooldowns(combat.EnemyWeapons, combat.ElapsedSeconds);
            if (Time.unscaledTime > _playerDamageExpiry) _playerDamage.text = string.Empty;
            if (Time.unscaledTime > _enemyDamageExpiry) _enemyDamage.text = string.Empty;
        }

        public void ShowAttack(CombatAttackEvent attack)
        {
            bool fromPlayer = attack.Side == CombatSide.Player;
            (fromPlayer ? _playerBag : _enemyBag).ShowAttack(attack);
            Text damage = fromPlayer ? _enemyDamage : _playerDamage;
            damage.text = !attack.Hit ? "MISS" : $"{(attack.Critical ? "CRIT " : "")}-{attack.Damage:0.#}";
            damage.color = attack.Critical ? new Color(1f, 0.80f, 0.20f) : Color.white;
            if (fromPlayer) _enemyDamageExpiry = Time.unscaledTime + 0.65f;
            else _playerDamageExpiry = Time.unscaledTime + 0.65f;
            string attacker = fromPlayer ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
            string outcome = !attack.Hit ? "빗나감" : $"{(attack.Critical ? "치명타" : "명중")} {attack.Damage:0.#} 피해";
            _log.Enqueue($"{attack.Time:0.0}s {attacker} · {attack.DisplayName} → {outcome}");
            while (_log.Count > 4) _log.Dequeue();
            _logText.text = string.Join("\n", _log);
        }

        public void ShowResult(PrototypePhase phase)
        {
            _title.text = phase switch
            {
                PrototypePhase.Victory => "승리!",
                PrototypePhase.Defeat => "패배 · 가방을 재정비하고 재도전하세요",
                PrototypePhase.Draw => "시간 초과",
                _ => "12라운드 완료!"
            };
            _continueButton.interactable = phase != PrototypePhase.Completed;
            _continueText.text = phase switch
            {
                PrototypePhase.Victory => $"상점으로 · +{_income}G",
                PrototypePhase.Defeat or PrototypePhase.Draw => "상점으로 돌아가기",
                _ => "모든 라운드 완료"
            };
        }

        private void InspectItem(CombatItemSnapshot item, CombatSide side)
        {
            IReadOnlyList<CombatWeapon> weapons = side == CombatSide.Player ? _combat.PlayerWeapons : _combat.EnemyWeapons;
            CombatWeapon weapon = null;
            foreach (CombatWeapon candidate in weapons) if (candidate.InstanceId == item.InstanceId) weapon = candidate;
            if (weapon == null)
            {
                _inspection.text = $"{item.DisplayName} · 이 아이템의 전투 효과는 아직 구현되지 않았습니다.";
                return;
            }
            RuntimeWeaponStats stats = weapon.Stats;
            _inspection.text = $"{item.DisplayName} · 공격력 {stats.AttackDamage:0.#} · 쿨타임 {stats.CooldownSeconds:0.##}초 · 명중 {stats.AccuracyPercent:0.#}% · 치명타 {stats.CriticalChancePercent:0.#}% · 치명타 피해 {stats.CriticalDamagePercent:0.#}%";
        }

        private static void SetHealth(Image fill, Text text, double health, double maximum)
        {
            fill.rectTransform.anchorMax = new Vector2((float)(health / maximum), 1f);
            text.text = $"체력 {health:0.#} / {maximum:0.#}";
        }

        private static Image HealthBar(Transform parent, string name, Color color)
        {
            Image background = Image(parent, name, new Color(0.14f, 0.17f, 0.22f));
            BackpackPrototypeController.SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -192f), new Vector2(410f, 30f));
            Image fill = Image(background.transform, "Fill", color);
            BackpackPrototypeController.Stretch(fill.rectTransform);
            return fill;
        }

        private static Image Frame(Transform parent, string name, float x, Color color)
        {
            Image frame = Image(parent, name, color);
            BackpackPrototypeController.SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 50f), new Vector2(480f, 460f));
            return frame;
        }

        private static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            Text text = BackpackPrototypeController.CreateText(parent, name, string.Empty, fontSize, FontStyle.Bold, alignment);
            BackpackPrototypeController.SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            text.raycastTarget = false;
            return text;
        }

        private static Image Image(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
