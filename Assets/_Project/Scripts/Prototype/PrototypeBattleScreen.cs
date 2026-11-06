using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeBattleScreen
    {
        private const float IllustrationCenterY = -92f;
        private readonly PrototypeBattleBagView _playerBag;
        private readonly PrototypeBattleBagView _enemyBag;
        private readonly Image _playerHealthFill;
        private readonly Image _enemyHealthFill;
        private readonly Text _playerHealthText;
        private readonly Text _enemyHealthText;
        private readonly Text _playerEvasion;
        private readonly Text _enemyEvasion;
        private readonly PrototypeArmorBadge _playerArmor;
        private readonly PrototypeArmorBadge _enemyArmor;
        private readonly PrototypeStatusArea _playerStatuses;
        private readonly PrototypeStatusArea _enemyStatuses;
        private readonly Image _playerBagFrame;
        private readonly Image _enemyBagFrame;
        private readonly Text _playerName;
        private readonly Text _enemyName;
        private readonly Text _title;
        private readonly Text _timer;
        private readonly Text _inspection;
        private readonly Text _logText;
        private readonly Image _playerIllustration;
        private readonly Image _enemyIllustration;
        private readonly PrototypeDamagePopups _playerDamage;
        private readonly PrototypeDamagePopups _enemyDamage;
        private readonly Button _continueButton;
        private readonly Text _continueText;
        private readonly Button _statisticsButton;
        private readonly PrototypeBattlePlaybackControls _playbackControls;
        private readonly Queue<string> _log = new();
        private readonly PrototypeItemTooltip _tooltip;
        private CombatSimulation _combat;
        private int _round;
        private int _income;

        public RectTransform Root { get; }

        public PrototypeBattleScreen(Transform parent, Action continueAction, PrototypeItemTooltip tooltip, Action statisticsAction,
            Action<float> speedAction, Action pauseAction)
        {
            _tooltip = tooltip;
            Image background = Image(parent, "Battle Screen", new Color(0.04f, 0.065f, 0.11f));
            Root = background.rectTransform;
            BackpackPrototypeController.Stretch(Root);
            Image statisticsImage = Image(Root, "Battle Statistics Button", new Color(.23f, .46f, .68f));
            BackpackPrototypeController.SetRect(statisticsImage.rectTransform, Vector2.one * .5f, new Vector2(0, 220), new Vector2(180, 36));
            statisticsImage.raycastTarget = true;
            _statisticsButton = statisticsImage.gameObject.AddComponent<Button>();
            _statisticsButton.onClick.AddListener(() => statisticsAction());
            var statisticsText = BackpackPrototypeController.CreateText(statisticsImage.transform, "Battle Statistics Label", "전투 통계", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            statisticsText.raycastTarget = false; BackpackPrototypeController.Stretch(statisticsText.rectTransform);
            _timer = Label(Root, "Combat Timer", new Vector2(0f, 318f), new Vector2(900f, 44f), 28, TextAnchor.MiddleCenter);
            _title = Label(Root, "Battle Result", new Vector2(0f, 270f), new Vector2(900f, 36f), 24, TextAnchor.MiddleCenter);
            _title.gameObject.SetActive(false);
            _playbackControls = new PrototypeBattlePlaybackControls(Root, speedAction, pauseAction);
            RectTransform playerGroup = CombatantGroup(Root, "Player Combatant", -310f);
            RectTransform enemyGroup = CombatantGroup(Root, "Enemy Combatant", 310f);
            _playerBagFrame = Frame(playerGroup, "Player Battle Frame", new Color(0.07f, 0.13f, 0.20f));
            _enemyBagFrame = Frame(enemyGroup, "Enemy Battle Frame", new Color(0.17f, 0.10f, 0.10f));
            _playerBag = new PrototypeBattleBagView(playerGroup, "Player Battle Backpack");
            _enemyBag = new PrototypeBattleBagView(enemyGroup, "Enemy Battle Backpack");
            _playerName = Label(playerGroup, "Player Name", new Vector2(0f, 27f), new Vector2(280f, 30f), 22, TextAnchor.MiddleCenter);
            _enemyName = Label(enemyGroup, "Enemy Name", new Vector2(0f, 27f), new Vector2(280f, 30f), 22, TextAnchor.MiddleCenter);
            _playerIllustration = CharacterSpace(playerGroup, "Player Character Illustration");
            _enemyIllustration = CharacterSpace(enemyGroup, "Enemy Character Illustration");
            _playerEvasion = EvasionReadout(playerGroup, "Player", -166f);
            _enemyEvasion = EvasionReadout(enemyGroup, "Enemy", 166f);
            _playerHealthFill = HealthBar(playerGroup, "Player Health", new Color(0.20f, 0.67f, 0.37f));
            _enemyHealthFill = HealthBar(enemyGroup, "Enemy Health", new Color(0.80f, 0.27f, 0.25f));
            _playerHealthText = Label(playerGroup, "Player Health Text", new Vector2(0f, -214f), new Vector2(280f, 28f), 18, TextAnchor.MiddleCenter);
            _enemyHealthText = Label(enemyGroup, "Enemy Health Text", new Vector2(0f, -214f), new Vector2(280f, 28f), 18, TextAnchor.MiddleCenter);
            _playerArmor = PrototypeArmorBadge.Create(playerGroup, "Player Armor Badge");
            _enemyArmor = PrototypeArmorBadge.Create(enemyGroup, "Enemy Armor Badge");
            _playerStatuses = new PrototypeStatusArea(Root, "Player", true);
            _enemyStatuses = new PrototypeStatusArea(Root, "Enemy", false);
            var glyphs = Resources.Load<Texture2D>("UI/BattleDamageGlyphs");
            var damageAreaCenter = new Vector2(0, IllustrationCenterY);
            var damageAreaSize = new Vector2(220, 168);
            _playerDamage = new PrototypeDamagePopups(playerGroup, "Player Damage", damageAreaCenter, damageAreaSize, glyphs);
            _enemyDamage = new PrototypeDamagePopups(enemyGroup, "Enemy Damage", damageAreaCenter, damageAreaSize, glyphs);
            _inspection = Label(Root, "Battle Item Details", new Vector2(0f, -272f), new Vector2(1180f, 24f), 14, TextAnchor.MiddleCenter);

            Image logPanel = Image(Root, "Battle Log Panel", new Color(0.08f, 0.11f, 0.17f));
            BackpackPrototypeController.SetRect(logPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120f, -320f), new Vector2(950f, 62f));
            _logText = BackpackPrototypeController.CreateText(logPanel.transform, "Combat Log", string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            _logText.raycastTarget = false;
            BackpackPrototypeController.Stretch(_logText.rectTransform, 6f);

            Image buttonImage = Image(Root, "Combat Action Button", new Color(0.23f, 0.46f, 0.68f));
            BackpackPrototypeController.SetRect(buttonImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(500f, -320f), new Vector2(190f, 50f));
            buttonImage.raycastTarget = true;
            _continueButton = buttonImage.gameObject.AddComponent<Button>();
            _continueButton.onClick.AddListener(() => continueAction());
            _continueText = BackpackPrototypeController.CreateText(buttonImage.transform, "Label", string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            _continueText.raycastTarget = false;
            BackpackPrototypeController.Stretch(_continueText.rectTransform);
        }

        public void Begin(CombatSimulation combat, int round, int income)
        {
            _statisticsButton.gameObject.SetActive(false);
            _combat = combat;
            _round = round;
            _income = income;
            _log.Clear();
            _playerBag.Build(combat.Player.Loadout, CombatSide.Player, item => InspectItem(item, CombatSide.Player),
                _tooltip, item => TooltipContent(item, CombatSide.Player));
            _enemyBag.Build(combat.Enemy.Loadout, CombatSide.Enemy, item => InspectItem(item, CombatSide.Enemy),
                _tooltip, item => TooltipContent(item, CombatSide.Enemy));
            _playerStatuses.Bind(combat, CombatSide.Player, _tooltip);
            _enemyStatuses.Bind(combat, CombatSide.Enemy, _tooltip);
            _playerBagFrame.rectTransform.sizeDelta = _playerBag.Root.sizeDelta + Vector2.one * 24f;
            _enemyBagFrame.rectTransform.sizeDelta = _enemyBag.Root.sizeDelta + Vector2.one * 24f;
            _playerName.text = combat.Player.DisplayName;
            _enemyName.text = combat.Enemy.DisplayName;
            SetIllustration(_playerIllustration, combat.Player, facesLeft: false);
            SetIllustration(_enemyIllustration, combat.Enemy, facesLeft: true);
            _title.text = string.Empty;
            _title.gameObject.SetActive(false);
            _inspection.text = "아이템 위에 마우스를 올리면 전투 스탯을 확인합니다. 색이 위까지 차오르면 아이템을 사용합니다.";
            _logText.text = "첫 공격을 준비하고 있습니다…";
            _playerDamage.Clear();
            _enemyDamage.Clear();
            // Each battle starts at zero armor; item activations then feed the real HUD values.
            _playerArmor.SetValue(0d);
            _enemyArmor.SetValue(0d);
            _continueButton.interactable = false;
            _continueText.text = "전투 진행 중…";
            Render(combat);
        }

        public void Render(CombatSimulation combat)
        {
            _timer.text = $"ROUND {_round} · {combat.ElapsedSeconds:0.0}초";
            SetHealth(_playerHealthFill, _playerHealthText, combat.PlayerHealth, combat.PlayerMaxHealth);
            SetHealth(_enemyHealthFill, _enemyHealthText, combat.EnemyHealth, combat.EnemyMaxHealth);
            _playerArmor.SetValue(combat.PlayerArmor);
            _enemyArmor.SetValue(combat.EnemyArmor);
            _playerEvasion.text = $"{combat.GetEvasionPercent(CombatSide.Player):0.##}%";
            _enemyEvasion.text = $"{combat.GetEvasionPercent(CombatSide.Enemy):0.##}%";
            _playerStatuses.Render();
            _enemyStatuses.Render();
            _playerBag.RenderCooldowns(combat.PlayerItems, combat.ElapsedSeconds);
            _enemyBag.RenderCooldowns(combat.EnemyItems, combat.ElapsedSeconds);
        }

        internal void TickDamagePopups()
        {
            double now = Time.unscaledTimeAsDouble;
            _playerDamage.Tick(now);
            _enemyDamage.Tick(now);
        }

        public void ShowAttack(CombatItemEvent attack)
        {
            bool fromPlayer = attack.Side == CombatSide.Player;
            (fromPlayer ? _playerBag : _enemyBag).ShowAttack(attack);
            string attacker = fromPlayer ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
            if (attack.Kind == ItemEffectKind.ApplyStatus)
            {
                string target = attack.TargetSide == CombatSide.Player ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
                AppendLog($"{attack.Time:0.0}s {attacker} · {attack.DisplayName} → {target} "
                    + $"{_combat.GetStatusDefinition(attack.StatusKind).DisplayName} +{attack.StatusStacks}");
                return;
            }
            if (attack.Kind == ItemEffectKind.GainArmor)
            {
                AppendLog($"{attack.Time:0.0}s {attacker} · {attack.DisplayName} → 방어도 +{attack.ArmorGained:0.#}");
                return;
            }
            DamagePopups(attack.TargetSide).Show(attack.Damage,
                attack.Critical ? new Color(1f, .81f, .28f) : Color.white, attack.Critical);
            string outcome = !attack.Hit ? (attack.AttackOutcome == AttackOutcome.Evaded ? "회피" : "명중 실패 · 회피 효과 발동")
                : $"{(attack.Critical ? "치명타" : "명중")} {attack.Damage:0.#} 피해";
            if (attack.ArmorAbsorbed > 0d) outcome += $" (방어도 {attack.ArmorAbsorbed:0.#} 흡수 · 체력 {attack.HealthDamage:0.#})";
            AppendLog($"{attack.Time:0.0}s {attacker} · {attack.DisplayName} → {outcome}");
        }

        public void ShowStatusDamage(PeriodicStatusTickEvent tick)
        {
            bool targetPlayer = tick.Target == CombatSide.Player;
            DamagePopups(tick.Target).Show(tick.Damage, PrototypeStatusGlyph.ColorFor(tick.Kind));
            string target = targetPlayer ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
            AppendLog($"{tick.Time:0.0}s {target} · {_combat.GetStatusDefinition(tick.Kind).DisplayName} {tick.Stacks}중첩 → {tick.Damage:0.##} 피해"
                + (tick.Resolution.ArmorAbsorbed > 0 ? $" (방어도 {tick.Resolution.ArmorAbsorbed:0.##} 흡수 · 체력 {tick.Resolution.HealthDamage:0.##})" : ""));
        }

        public void ShowReaction(CombatReactionEvent reaction)
        {
            if (reaction.Kind == CombatReactionKind.HealSelf && reaction.HealthRestored <= 0) return;
            bool targetPlayer = reaction.Target == CombatSide.Player;
            string target = targetPlayer ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
            if (reaction.Kind == CombatReactionKind.HealSelf)
            {
                AppendLog($"{reaction.Time:0.0}s {target} · 흡혈 → 체력 +{reaction.HealthRestored:0.##}");
            }
            else
            {
                DamagePopups(reaction.Target).Show(reaction.Amount, PrototypeStatusGlyph.ColorFor(reaction.Status));
                string owner = reaction.Owner == CombatSide.Player ? _combat.Player.DisplayName : _combat.Enemy.DisplayName;
                AppendLog($"{reaction.Time:0.0}s {owner} · 가시 → {target} {reaction.Amount:0.##} 피해"
                    + (reaction.Damage.ArmorAbsorbed > 0 ? $" (방어도 {reaction.Damage.ArmorAbsorbed:0.##} 흡수 · 체력 {reaction.Damage.HealthDamage:0.##})" : ""));
            }
        }

        private PrototypeDamagePopups DamagePopups(CombatSide side) => side == CombatSide.Player ? _playerDamage : _enemyDamage;

        private void AppendLog(string message)
        {
            _log.Enqueue(message);
            while (_log.Count > 4) _log.Dequeue();
            _logText.text = string.Join("\n", _log);
        }

        public void Clear()
        {
            SetPlaybackState(false, 1f, false);
            _statisticsButton.gameObject.SetActive(false);
            _tooltip.HideAll();
            _playerBag.Clear(); _enemyBag.Clear(); _playerStatuses.Clear(); _enemyStatuses.Clear();
            _combat = null; _log.Clear();
            _logText.text = _inspection.text = _title.text = "";
            _playerDamage.Clear();
            _enemyDamage.Clear();
            _title.gameObject.SetActive(false); _continueButton.interactable = false;
        }

        public void ShowResult(PrototypePhase phase)
        {
            SetPlaybackState(false, 1f, false);
            _statisticsButton.gameObject.SetActive(true);
            _title.gameObject.SetActive(true);
            _title.text = phase switch
            {
                PrototypePhase.Victory => "승리!",
                PrototypePhase.Defeat => _combat.IsForcedDefeat ? "90초 경과 · 패배 · 라이프 -1" : "패배 · 라이프 -1",
                PrototypePhase.Draw => "시간 초과",
                PrototypePhase.GameOver => "게임 오버",
                _ => "스테이지 클리어!"
            };
            _continueButton.interactable = phase is not (PrototypePhase.Completed or PrototypePhase.GameOver);
            _continueText.text = phase switch
            {
                PrototypePhase.Victory => $"상점으로 · +{_income}G",
                PrototypePhase.Defeat or PrototypePhase.Draw => "상점으로 돌아가기",
                PrototypePhase.GameOver => "라이프 소진",
                _ => "모든 라운드 완료"
            };
        }

        public void SetPlaybackState(bool running, float speed, bool paused)
            => _playbackControls.SetState(running, speed, paused);

        internal void SetDamageNumbersVisible(bool visible)
        {
            _playerDamage.SetVisible(visible);
            _enemyDamage.SetVisible(visible);
        }

        public void ShowConfigurationError(string error)
        {
            _inspection.text = error.Split('\n')[0];
            _continueText.text = "적 설정 확인 후 재시도";
        }

        private void InspectItem(CombatItemSnapshot item, CombatSide side)
        {
            IReadOnlyList<CombatWeapon> weapons = side == CombatSide.Player ? _combat.PlayerWeapons : _combat.EnemyWeapons;
            CombatWeapon weapon = null;
            foreach (CombatWeapon candidate in weapons) if (candidate.InstanceId == item.InstanceId) weapon = candidate;
            if (weapon == null)
            {
                foreach (CombatItem runtime in _combat.GetItems(side))
                    if (runtime.InstanceId == item.InstanceId)
                    {
                        _inspection.text = $"{item.DisplayName} · {ItemTooltipContent.FromCombat(item, runtime).EffectDescription}";
                        return;
                    }
                _inspection.text = item.BattleStartStatuses.Count > 0 || item.PlacementEffects.Count > 0
                    ? $"{item.DisplayName} · {ItemTooltipContent.FromCombat(item, null).EffectDescription}"
                    : $"{item.DisplayName} · 이 아이템의 전투 효과는 아직 구현되지 않았습니다.";
                return;
            }
            RuntimeWeaponStats stats = weapon.Stats;
            _inspection.text = $"{item.DisplayName} · 공격력 {stats.AttackDamage:0.#} · 쿨타임 {stats.CooldownSeconds:0.##}초 · 명중 {stats.AccuracyPercent:0.#}% · 치명타 {stats.CriticalChancePercent:0.#}% · 치명타 피해 {stats.CriticalDamagePercent:0.#}%";
        }

        private ItemTooltipContent TooltipContent(CombatItemSnapshot item, CombatSide side)
        {
            foreach (CombatItem runtime in _combat.GetItems(side))
                if (runtime.InstanceId == item.InstanceId) return ItemTooltipContent.FromCombat(item, runtime);
            return ItemTooltipContent.FromCombat(item, null);
        }

        private static void SetHealth(Image fill, Text text, double health, double maximum)
        {
            fill.rectTransform.anchorMax = new Vector2((float)(health / maximum), 1f);
            text.text = $"체력 {health:0.#} / {maximum:0.#}";
        }

        private static Image HealthBar(Transform parent, string name, Color color)
        {
            Image background = Image(parent, name, new Color(0.14f, 0.17f, 0.22f));
            BackpackPrototypeController.SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -214f), new Vector2(280f, 28f));
            Image fill = Image(background.transform, "Fill", color);
            BackpackPrototypeController.Stretch(fill.rectTransform);
            return fill;
        }

        private static RectTransform CombatantGroup(Transform parent, string name, float x)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            BackpackPrototypeController.SetRect(rect, Vector2.one * 0.5f, new Vector2(x, 0f), new Vector2(480f, 510f));
            return rect;
        }

        private static Image Frame(Transform parent, string name, Color color)
        {
            Image frame = Image(parent, name, color);
            BackpackPrototypeController.SetRect(frame.rectTransform, Vector2.one * .5f,
                new Vector2(0f, PrototypeBattleBagView.CenterY), Vector2.zero);
            return frame;
        }

        private static Text EvasionReadout(Transform parent, string side, float x)
        {
            var obj = new GameObject($"{side} Evasion", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            BackpackPrototypeController.SetRect((RectTransform)obj.transform, Vector2.one * 0.5f,
                new Vector2(x, IllustrationCenterY), new Vector2(104f, 36f));
            var glyphObject = new GameObject("Evasion Glyph", typeof(RectTransform), typeof(PrototypeStatusGlyph));
            glyphObject.transform.SetParent(obj.transform, false);
            PrototypeStatusGlyph glyph = glyphObject.GetComponent<PrototypeStatusGlyph>();
            glyph.Kind = StatusEffectKind.Agility;
            glyph.color = PrototypeStatusGlyph.ColorFor(StatusEffectKind.Agility);
            glyph.raycastTarget = false;
            BackpackPrototypeController.SetRect(glyph.rectTransform, Vector2.one * 0.5f,
                new Vector2(-35f, 0f), new Vector2(30f, 30f));
            return Label(obj.transform, "Evasion Value", new Vector2(18f, 0f), new Vector2(64f, 36f), 21, TextAnchor.MiddleLeft);
        }

        private static Image CharacterSpace(Transform parent, string name)
        {
            Image illustration = Image(parent, name, Color.white);
            SelectionUi.Place(illustration.rectTransform, new Vector2(0, IllustrationCenterY), new Vector2(224, 200));
            illustration.preserveAspect = true;
            illustration.enabled = false;
            return illustration;
        }

        private static void SetIllustration(Image image, CombatantSnapshot combatant, bool facesLeft)
        {
            image.sprite = combatant.Illustration;
            image.enabled = image.sprite != null;
            image.rectTransform.localScale = new Vector3(combatant.IllustrationFacesLeft == facesLeft ? 1 : -1, 1, 1);
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
